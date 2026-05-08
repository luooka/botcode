using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

const string BaseUrl = "https://music.163.com/api";

var client = new HttpClient();
client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36");
client.DefaultRequestHeaders.Add("Referer", "https://music.163.com/");

// --- 解析命令行参数 ---
if (args.Length < 1)
{
    Console.WriteLine("用法:");
    Console.WriteLine("  dotnet run <用户ID>");
    Console.WriteLine("  dotnet run <用户ID> --detail");
    Console.WriteLine("  dotnet run <用户ID> --search <关键词>");
    return;
}

var uid = args[0];
if (!int.TryParse(uid, out _))
{
    Console.WriteLine("❌ 用户ID应该是纯数字");
    return;
}

var showDetail = args.Contains("--detail");
string? search = null;
var searchIdx = Array.IndexOf(args, "--search");
if (searchIdx >= 0 && searchIdx + 1 < args.Length)
    search = args[searchIdx + 1];

// --- 获取用户信息 ---
var userInfo = await GetUserAsync(uid);
if (userInfo is not null)
{
    Console.WriteLine($"🎵 用户: {userInfo.Nickname} (ID: {uid})");
    Console.WriteLine($"   粉丝: {userInfo.Followeds}  关注: {userInfo.Follows}");
    Console.WriteLine();
}

// --- 获取歌单列表 ---
var playlists = await GetUserPlaylistsAsync(uid);
if (playlists.Count == 0)
{
    Console.WriteLine("没有找到歌单");
    return;
}

if (search is not null)
{
    playlists = playlists.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
    if (playlists.Count == 0)
    {
        Console.WriteLine($"没有找到包含「{search}」的歌单");
        return;
    }
}

Console.WriteLine($"共 {playlists.Count} 个歌单:\n");
Console.WriteLine($"{"#",-4} {"歌单名",-35} {"歌曲数",-8} {"播放量",-12} {"ID"}");
Console.WriteLine(new string('-', 80));

for (var i = 0; i < playlists.Count; i++)
{
    var p = playlists[i];
    var privacy = p.Privacy == 10 ? "🔒" : "  ";
    var creator = p.Creator?.UserId == int.Parse(uid) ? "" : " ⭐收藏";
    Console.WriteLine($"{i + 1,-4} {privacy}{p.Name,-33} {p.TrackCount,-8} {p.PlayCount,-12:N0} {p.Id}{creator}");
}

// --- 歌单详情 ---
if (showDetail)
{
    Console.WriteLine("\n" + new string('=', 80));
    foreach (var p in playlists.Take(5))
    {
        Console.WriteLine($"\n📋 歌单: {p.Name} (ID: {p.Id})");
        Console.WriteLine(new string('-', 60));

        var detail = await GetPlaylistDetailAsync(p.Id);
        if (detail?.Tracks is null || detail.Tracks.Count == 0)
        {
            Console.WriteLine("  (无法获取歌曲列表)");
            continue;
        }

        for (var j = 0; j < detail.Tracks.Count; j++)
        {
            var t = detail.Tracks[j];
            var artists = string.Join(" / ", t.Artists?.Select(a => a.Name) ?? []);
            var album = t.Album?.Name ?? "";
            var dur = FormatDuration(t.Duration);
            Console.WriteLine($"  {j + 1,3}. {t.Name}  -  {artists}  [{album}]  {dur}");
        }

        var remaining = p.TrackCount - detail.Tracks.Count;
        if (remaining > 0)
            Console.WriteLine($"  ... 还有 {remaining} 首未显示");
    }
}

// ==================== API 方法 ====================

async Task<UserInfo?> GetUserAsync(string userId)
{
    try
    {
        var url = $"{BaseUrl}/v1/user/detail?uid={userId}";
        var resp = await client.GetFromJsonAsync<ApiResponse<UserDetailResponse>>(url);
        if (resp?.Code != 200) return null;
        return resp.Data?.Profile ?? resp.Data?.User;
    }
    catch { return null; }
}

async Task<List<PlaylistInfo>> GetUserPlaylistsAsync(string userId)
{
    try
    {
        var url = $"{BaseUrl}/user/playlist?uid={userId}&limit=100&offset=0&timestamp=0";
        var resp = await client.GetFromJsonAsync<ApiResponse<PlaylistListResponse>>(url);
        if (resp?.Code != 200) return [];
        return resp.Data?.Playlist ?? [];
    }
    catch { return []; }
}

async Task<PlaylistDetail?> GetPlaylistDetailAsync(int playlistId)
{
    try
    {
        var url = $"{BaseUrl}/v6/playlist/detail?id={playlistId}&n=100000&s=0";
        var resp = await client.GetFromJsonAsync<ApiResponse<PlaylistDetail>>(url);
        return resp?.Code == 200 ? resp.Data : null;
    }
    catch { return null; }
}

static string FormatDuration(int ms)
{
    var ts = TimeSpan.FromMilliseconds(ms);
    return $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
}

// ==================== 数据模型 ====================

record ApiResponse<T>([property: JsonPropertyName("code")] int Code,
                       [property: JsonPropertyName("msg")] string? Msg,
                       [property: JsonPropertyName("result")] T? Data);

record UserDetailResponse([property: JsonPropertyName("profile")] UserInfo? Profile,
                          [property: JsonPropertyName("user")] UserInfo? User);

record UserInfo([property: JsonPropertyName("nickname")] string Nickname,
                [property: JsonPropertyName("followeds")] int Followeds,
                [property: JsonPropertyName("follows")] int Follows,
                [property: JsonPropertyName("userId")] int UserId);

record PlaylistListResponse([property: JsonPropertyName("playlist")] List<PlaylistInfo> Playlist);

record PlaylistInfo([property: JsonPropertyName("id")] int Id,
                    [property: JsonPropertyName("name")] string Name,
                    [property: JsonPropertyName("trackCount")] int TrackCount,
                    [property: JsonPropertyName("playCount")] long PlayCount,
                    [property: JsonPropertyName("privacy")] int Privacy,
                    [property: JsonPropertyName("creator")] UserInfo? Creator);

record PlaylistDetail([property: JsonPropertyName("trackCount")] int TrackCount,
                      [property: JsonPropertyName("tracks")] List<TrackInfo>? Tracks);

record TrackInfo([property: JsonPropertyName("name")] string Name,
                 [property: JsonPropertyName("dt")] int Duration,
                 [property: JsonPropertyName("ar")] List<ArtistInfo>? Artists,
                 [property: JsonPropertyName("al")] AlbumInfo? Album);

record ArtistInfo([property: JsonPropertyName("name")] string Name);

record AlbumInfo([property: JsonPropertyName("name")] string Name);
