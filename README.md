# 🎵 网易云音乐歌单查询工具

一个基于 .NET 8 的命令行工具，用于查询网易云音乐用户的歌单信息。

## 功能

- 查看用户的歌单列表（自建 + 收藏）
- 显示歌单内歌曲详情（歌名、歌手、专辑、时长）
- 按关键词搜索歌单
- 显示播放量、歌曲数等统计信息
- **GUI 图形界面版本**（WPF 暗色主题）

## 项目结构

```
botcode/
├── README.md
├── NeteasePlaylist/         # 命令行版本
│   ├── NeteasePlaylist.csproj
│   └── Program.cs
├── NeteasePlaylistGui/      # GUI 图形界面版本 (WPF)
│   ├── NeteasePlaylistGui.csproj
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── MainWindow.xaml
│   └── MainWindow.xaml.cs
└── netease_playlist.py      # Python 版本
```

## 环境要求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 或更高版本

## 安装 .NET SDK

### Windows

1. 前往 [dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
2. 下载 **SDK**（不是 Runtime）的 Windows 安装包（x64）
3. 双击安装，一路 Next
4. 打开 PowerShell / CMD 验证：

```powershell
dotnet --version
```

### macOS

**方式一：官网下载**

前往 [dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)，下载 macOS 安装包（x64 或 ARM64 根据你的芯片选择）。

**方式二：Homebrew**

```bash
brew install dotnet@8
```

### Linux

**Ubuntu / Debian：**

```bash
sudo apt update
sudo apt install -y dotnet-sdk-8.0
```

**CentOS / RHEL / Fedora：**

```bash
sudo dnf install -y dotnet-sdk-8.0
```

**Arch Linux：**

```bash
sudo pacman -S dotnet-sdk
```

**通用安装脚本（推荐，适用于所有发行版）：**

```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0
echo 'export DOTNET_ROOT=$HOME/.dotnet' >> ~/.bashrc
echo 'export PATH=$PATH:$DOTNET_ROOT' >> ~/.bashrc
source ~/.bashrc
```

## GUI 图形界面版本

### 运行

```bash
cd NeteasePlaylistGui
dotnet run
```

### 打包成 EXE

```bash
cd NeteasePlaylistGui
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

打包后的文件在 `bin/Release/net8.0-windows/win-x64/publish/` 目录下，是一个独立的 exe，不需要安装 .NET 运行时。

### 界面功能

- 顶部输入用户 ID 点击查询
- 左侧显示歌单列表，支持实时搜索过滤
- 右侧显示选中歌单的歌曲列表
- 点击任意歌单自动加载歌曲
- 暗色主题，类 Spotify 风格

---

## 命令行版本

### 1. 克隆仓库

```bash
git clone https://github.com/luooka/botcode.git
cd botcode/NeteasePlaylist
```

### 2. 运行

```bash
# 查看用户的歌单列表
dotnet run -- <网易云用户ID>

# 查看歌单内歌曲详情（最多显示前5个歌单）
dotnet run -- <用户ID> --detail

# 按关键词搜索歌单
dotnet run -- <用户ID> --search <关键词>

# 组合使用
dotnet run -- <用户ID> --detail --search 电音
```

### 示例

```bash
dotnet run -- 380747545
dotnet run -- 380747545 --detail
dotnet run -- 380747545 --search Avicii
```

### 输出示例

```
🎵 用户: luooka (ID: 380747545)
   粉丝: 100  关注: 50

共 51 个歌单:

#    歌单名                                 歌曲数      播放量          ID
--------------------------------------------------------------------------------
1      luooka喜欢的音乐                       1278     11,435       539665076
2      听着舒服就行了，没有那么多分类                   352      3,606        899549574
3      注入靈魂                              93       401          2562145439
...
```

## 如何获取网易云用户 ID

1. 打开 [网易云音乐网页版](https://music.163.com/)
2. 登录后点击头像进入个人主页
3. 浏览器地址栏中的数字就是你的用户 ID

例如：`https://music.163.com/#/user/home?id=380747545` → 用户 ID 为 `380747545`

## 项目结构

```
NeteasePlaylist/
├── NeteasePlaylist.csproj   # 项目文件
├── Program.cs               # 主程序（API 调用 + 数据模型）
└── README.md
```

## 技术栈

- .NET 8
- `System.Net.Http` — HTTP 请求
- `System.Text.Json` — JSON 反序列化
- 零第三方依赖

## API 说明

本工具使用网易云音乐的公开 API：

- `GET /api/user/playlist` — 获取用户歌单列表
- `GET /api/v1/user/detail` — 获取用户信息
- `GET /api/v6/playlist/detail` — 获取歌单详情

> ⚠️ 以上接口均为非官方 API，可能随时变动。

## License

MIT
