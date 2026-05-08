#!/usr/bin/env python3
"""
网易云音乐歌单查询工具
用法:
  python3 netease_playlist.py <用户ID>
  python3 netease_playlist.py <用户ID> --detail   # 查看歌单内歌曲详情
  python3 netease_playlist.py <用户ID> --search <关键词>  # 搜索歌单
"""

import sys
import json
import urllib.request
import urllib.parse

BASE = "https://music.163.com/api"

HEADERS = {
    "User-Agent": "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36",
    "Referer": "https://music.163.com/",
    "Content-Type": "application/x-www-form-urlencoded",
}


def api_get(path, params=None):
    url = BASE + path
    if params:
        url += "?" + urllib.parse.urlencode(params)
    req = urllib.request.Request(url, headers=HEADERS)
    with urllib.request.urlopen(req, timeout=10) as resp:
        return json.loads(resp.read().decode())


def api_post(path, data):
    body = urllib.parse.urlencode(data).encode()
    req = urllib.request.Request(BASE + path, data=body, headers=HEADERS)
    with urllib.request.urlopen(req, timeout=10) as resp:
        return json.loads(resp.read().decode())


def get_user_info(uid):
    """获取用户基本信息"""
    r = api_get("/v1/user/detail", {"uid": uid})
    if r.get("code") != 200:
        print(f"❌ 获取用户信息失败: {r.get('msg', '未知错误')}")
        return None
    return r.get("profile", r.get("user", {}))


def get_user_playlists(uid, limit=50, offset=0):
    """获取用户歌单列表"""
    r = api_get("/user/playlist", {"uid": uid, "limit": limit, "offset": offset, "timestamp": 0})
    if r.get("code") != 200:
        print(f"❌ 获取歌单失败: {r.get('msg', '未知错误')}")
        return []
    return r.get("playlist", [])


def get_playlist_detail(pid):
    """获取歌单详情（含歌曲列表）"""
    r = api_get("/v6/playlist/detail", {"id": pid, "n": 100000, "s": 0})
    if r.get("code") != 200:
        print(f"❌ 获取歌单详情失败: {r.get('msg', '未知错误')}")
        return None
    return r.get("playlist", {})


def format_duration(ms):
    s = ms // 1000
    m, s = divmod(s, 60)
    return f"{m}:{s:02d}"


def print_playlists(uid, detail=False, search=None):
    # 用户信息
    user = get_user_info(uid)
    if user:
        name = user.get("nickname", "未知")
        print(f"🎵 用户: {name} (ID: {uid})")
        print(f"   粉丝: {user.get('followeds', 0)}  关注: {user.get('follows', 0)}")
        print()

    # 歌单列表
    playlists = get_user_playlists(uid)
    if not playlists:
        print("没有找到歌单")
        return

    if search:
        playlists = [p for p in playlists if search.lower() in p.get("name", "").lower()]
        if not playlists:
            print(f"没有找到包含「{search}」的歌单")
            return

    print(f"共 {len(playlists)} 个歌单:\n")
    print(f"{'#':<4} {'歌单名':<35} {'歌曲数':<8} {'播放量':<12} {'ID'}")
    print("-" * 80)
    for i, p in enumerate(playlists, 1):
        name = p.get("name", "未命名")
        count = p.get("trackCount", 0)
        play = p.get("playCount", 0)
        pid = p.get("id", "")
        privacy = "🔒" if p.get("privacy") == 10 else "  "
        creator = "" if p.get("creator", {}).get("userId") == uid else " ⭐收藏"
        print(f"{i:<4} {privacy}{name:<33} {count:<8} {play:<12,} {pid}{creator}")

    # 显示详情
    if detail and playlists:
        print("\n" + "=" * 80)
        for p in playlists[:5]:  # 最多显示前5个歌单的详情
            pid = p["id"]
            pname = p.get("name", "")
            print(f"\n📋 歌单: {pname} (ID: {pid})")
            print("-" * 60)
            detail_data = get_playlist_detail(pid)
            if not detail_data:
                continue
            tracks = detail_data.get("tracks", [])
            if not tracks:
                # 如果 tracks 为空，尝试用 trackIds 获取
                print("  (歌曲列表需要单独获取，使用 --detail 可能受限)")
                continue
            for j, t in enumerate(tracks, 1):
                artists = " / ".join(a.get("name", "") for a in t.get("ar", []))
                album = t.get("al", {}).get("name", "")
                dur = format_duration(t.get("dt", 0))
                print(f"  {j:>3}. {t.get('name', '')}  -  {artists}  [{album}]  {dur}")
            remaining = detail_data.get("trackCount", 0) - len(tracks)
            if remaining > 0:
                print(f"  ... 还有 {remaining} 首未显示")


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(1)

    uid = sys.argv[1]
    if not uid.isdigit():
        print("❌ 用户ID应该是纯数字")
        sys.exit(1)

    detail = "--detail" in sys.argv
    search = None
    if "--search" in sys.argv:
        idx = sys.argv.index("--search")
        if idx + 1 < len(sys.argv):
            search = sys.argv[idx + 1]

    print_playlists(int(uid), detail=detail, search=search)


if __name__ == "__main__":
    main()
