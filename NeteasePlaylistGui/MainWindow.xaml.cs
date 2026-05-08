using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NeteasePlaylistGui;

public partial class MainWindow : Window
{
    private const string BaseUrl = "https://music.163.com/api";
    private static readonly HttpClient Client = CreateClient();
    private readonly ObservableCollection<PlaylistVm> _playlists = new();
    private readonly ObservableCollection<TrackVm> _tracks = new();
    private List<PlaylistVm> _allPlaylists = [];

    public MainWindow()
    {
        InitializeComponent();
        LstPlaylists.ItemsSource = _playlists;
        DgTracks.ItemsSource = _tracks;
        TxtUserId.Text = "380747545"; // 默认测试ID
    }

    private static HttpClient CreateClient()
    {
        var c = new HttpClient();
        c.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        c.DefaultRequestHeaders.Add("Referer", "https://music.163.com/");
        return c;
    }

    // ============ 查询歌单 ============
    private async void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        var uid = TxtUserId.Text.Trim();
        if (string.IsNullOrEmpty(uid) || !int.TryParse(uid, out _))
        {
            SetStatus("❌ 请输入有效的数字用户ID");
            return;
        }

        BtnLoad.IsEnabled = false;
        ShowProgress(true);
        SetStatus("正在查询...");
        _playlists.Clear();
        _allPlaylists.Clear();
        _tracks.Clear();

        try
        {
            // 获取用户信息
            var user = await GetUserAsync(uid);
            if (user is not null)
            {
                TxtUserName.Text = $"🎵 {user.Nickname}";
                TxtUserStats.Text = $"粉丝: {user.Followeds}  关注: {user.Follows}";
                UserInfoBar.Visibility = Visibility.Visible;
            }

            // 获取歌单
            var playlists = await GetPlaylistsAsync(uid);
            var myUid = int.Parse(uid);
            foreach (var p in playlists)
            {
                var vm = new PlaylistVm
                {
                    Id = p.Id,
                    Name = p.Name,
                    TrackCount = p.TrackCount,
                    PlayCount = p.PlayCount,
                    IsMine = p.Creator?.UserId == myUid,
                    Privacy = p.Privacy
                };
                _allPlaylists.Add(vm);
                _playlists.Add(vm);
            }

            TxtPlaylistCount.Text = $"共 {_playlists.Count} 个歌单";
            SetStatus($"✅ 加载完成，共 {_playlists.Count} 个歌单");
            BtnDetail.IsEnabled = _playlists.Count > 0;
        }
        catch (Exception ex)
        {
            SetStatus($"❌ 查询失败: {ex.Message}");
        }
        finally
        {
            BtnLoad.IsEnabled = true;
            ShowProgress(false);
        }
    }

    // ============ 查看歌曲详情 ============
    private async void BtnDetail_Click(object sender, RoutedEventArgs e)
    {
        var selected = LstPlaylists.SelectedItem as PlaylistVm;
        if (selected is null)
        {
            if (_playlists.Count > 0)
            {
                LstPlaylists.SelectedIndex = 0;
                selected = _playlists[0];
            }
            else return;
        }

        await LoadTracksAsync(selected);
    }

    private async Task LoadTracksAsync(PlaylistVm playlist)
    {
        BtnDetail.IsEnabled = false;
        ShowProgress(true);
        SetStatus($"正在加载「{playlist.Name}」的歌曲...");
        _tracks.Clear();

        try
        {
            var detail = await GetPlaylistDetailAsync(playlist.Id);
            if (detail?.Tracks is null || detail.Tracks.Count == 0)
            {
                SetStatus("⚠️ 无法获取歌曲列表");
                return;
            }

            TxtPlaylistTitle.Text = $"🎵 {playlist.Name}  ({detail.Tracks.Count} 首)";
            for (var i = 0; i < detail.Tracks.Count; i++)
            {
                var t = detail.Tracks[i];
                _tracks.Add(new TrackVm
                {
                    Index = i + 1,
                    Name = t.Name,
                    Artists = string.Join(" / ", t.Artists?.Select(a => a.Name) ?? []),
                    Album = t.Album?.Name ?? "",
                    Duration = FormatDuration(t.Duration)
                });
            }

            var remaining = playlist.TrackCount - detail.Tracks.Count;
            SetStatus(remaining > 0
                ? $"✅ 已加载 {detail.Tracks.Count} 首，还有 {remaining} 首未显示"
                : $"✅ 已加载全部 {detail.Tracks.Count} 首歌曲");
        }
        catch (Exception ex)
        {
            SetStatus($"❌ 加载失败: {ex.Message}");
        }
        finally
        {
            BtnDetail.IsEnabled = true;
            ShowProgress(false);
        }
    }

    // ============ 搜索过滤 ============
    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var keyword = TxtSearch.Text.Trim();
        _playlists.Clear();
        var filtered = string.IsNullOrEmpty(keyword)
            ? _allPlaylists
            : _allPlaylists.Where(p => p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var p in filtered) _playlists.Add(p);
    }

    // ============ 歌单点击加载歌曲 ============
    private async void LstPlaylists_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LstPlaylists.SelectedItem is PlaylistVm playlist)
        {
            await LoadTracksAsync(playlist);
        }
    }

    // ============ API 调用 ============
    private static async Task<UserInfo?> GetUserAsync(string uid)
    {
        try
        {
            var resp = await Client.GetFromJsonAsync<ApiResponse<UserDetailResp>>(
                $"{BaseUrl}/v1/user/detail?uid={uid}");
            return resp?.Code == 200 ? resp.Data?.Profile ?? resp.Data?.User : null;
        }
        catch { return null; }
    }

    private static async Task<List<PlaylistInfo>> GetPlaylistsAsync(string uid)
    {
        var resp = await Client.GetFromJsonAsync<ApiResponse<PlaylistListResp>>(
            $"{BaseUrl}/user/playlist?uid={uid}&limit=100&offset=0&timestamp=0");
        if (resp?.Code != 200) throw new Exception(resp?.Msg ?? "请求失败");
        return resp.Data?.Playlist ?? [];
    }

    private static async Task<PlaylistDetail?> GetPlaylistDetailAsync(int id)
    {
        var resp = await Client.GetFromJsonAsync<ApiResponse<PlaylistDetail>>(
            $"{BaseUrl}/v6/playlist/detail?id={id}&n=100000&s=0");
        return resp?.Code == 200 ? resp.Data : null;
    }

    // ============ 辅助方法 ============
    private void SetStatus(string text) => TxtStatus.Text = text;

    private void ShowProgress(bool show)
    {
        Progress.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        Progress.IsIndeterminate = show;
    }

    private static string FormatDuration(int ms)
    {
        var ts = TimeSpan.FromMilliseconds(ms);
        return $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
    }
}

// ==================== ViewModel ====================

public class PlaylistVm
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public int TrackCount { get; init; }
    public long PlayCount { get; init; }
    public bool IsMine { get; init; }
    public int Privacy { get; init; }

    public string PrivacyIcon => Privacy == 10 ? "🔒" : (IsMine ? "" : "⭐");
    public string PlayCountFormatted => PlayCount switch
    {
        >= 100_000_000 => $"{PlayCount / 100_000_000.0:F1}亿",
        >= 10_000 => $"{PlayCount / 10_000.0:F1}万",
        _ => PlayCount.ToString("N0")
    };
}

public class TrackVm
{
    public int Index { get; init; }
    public string Name { get; init; } = "";
    public string Artists { get; init; } = "";
    public string Album { get; init; } = "";
    public string Duration { get; init; } = "";
}

// ==================== API 数据模型 ====================

record ApiResponse<T>([property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("msg")] string? Msg,
    [property: JsonPropertyName("result")] T? Data);

record UserDetailResp([property: JsonPropertyName("profile")] UserInfo? Profile,
    [property: JsonPropertyName("user")] UserInfo? User);

record UserInfo([property: JsonPropertyName("nickname")] string Nickname,
    [property: JsonPropertyName("followeds")] int Followeds,
    [property: JsonPropertyName("follows")] int Follows,
    [property: JsonPropertyName("userId")] int UserId);

record PlaylistListResp([property: JsonPropertyName("playlist")] List<PlaylistInfo> Playlist);

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
