# BrickZhou · Rogue Prince Tools

《波斯王子：Rogue》社区工具集。作者：**BrickZhou/青春啊砖在he边看月亮**。

[Bilibili](https://space.bilibili.com/517390275) · [YouTube](https://www.youtube.com/@Brickzhou) · [English](README.en.md)

本项目包含 **两个工具**。美术资源导出程序与查看入口属于同一个工具；BepInEx 是第三方前置。

| 项目 | 版本 | 用途 | 需要 BepInEx |
|---|---|---|---|
| CheatMenu | 0.1.8 | F9 游戏内调试菜单，中英文切换，角色、资源、物品、徽章栏位与风神之息操作 | 是 |
| 美术资源工具 | 1.1.1 | 独立 EXE 导出图片，CMD 打开离线图片库，支持中英切换、分类、搜索和分页 | 否 |
| BepInEx（第三方前置） | 6.0.0-be.788+5b766a3 / IL2CPP x64 | 加载 CheatMenu | — |

## 下载与安装

在 [Releases](https://github.com/BrickAZ/rogue-prince-tools/releases) 下载需要的包。

### CheatMenu

1. 关闭游戏，通过 Steam → 管理 → 浏览本地文件，找到含 `The Rogue Prince of Persia.exe` 的游戏根目录。
2. 按 [前置说明](prerequisites/README.md) 获取固定版本 BepInEx，将**压缩包里面的内容**解压到游戏根目录。首次启动游戏等待初始化，进入主菜单后退出。
3. 将 `02-CheatMenu-0.1.8.zip` **里面的内容**解压到同一游戏根目录。
4. 启动游戏并进入存档，按 **F9** 开关菜单。进入 **设置/setting → 语言/language** 切换中英文。
5. 署名与 B 站、YouTube 主页按钮位于设置页底部；只有主动点击时才打开系统浏览器。

DLL 的最终位置应为 `BepInEx/plugins/RoguePrince.CheatMenu/RoguePrince.CheatMenu.dll`。
游戏运行时不要替换 DLL。已有其他版本加载器时，先核对兼容性。

安装 ZIP 不会自动备份存档。使用前在游戏关闭时备份 `userdata/saves`。
永久资源与部分进度修改可能随游戏保存；禁用插件不会撤销已保存的变化。
插件在指定永久操作前备份已落盘存档到 `BepInEx/CheatMenuBackups`。
停用：关闭游戏，将插件 DLL 改为 `.dll.disabled`；恢复时改回 `.dll`。

### 美术资源工具

将 `03-ArtTool-1.1.1.zip` 解压到游戏根目录，双击 `一键导出美术资源.exe`。
完成后自动打开 `ArtExports/index.html`；以后运行 `打开美术资源库.cmd` 即可查看已有结果。
两个入口放在同一个游戏根目录。导出 EXE 已包含 Python、图片解析依赖和页面模板，玩家不需要安装 Python、Codex 或开发环境。

图片类型包括 Sprite、Texture2D、Texture2DArray；不导出模型、骨架动画、声音或完整 Unity 工程。
源资源只读，导出前后校验哈希。浮点纹理同时保存原始数据与 8 位 PNG 预览。
命令行与构建说明见 [art-tool/README.md](art-tool/README.md)。

## 兼容性与验证范围

开发基准：Windows x64 / Steam Build `24299434` / 游戏 `v1.1.0` / Unity `6000.0.64f1`。
其他平台或游戏版本尚未确认。离线测试、构建及单文件导出验收见 [验证记录](docs/VALIDATION.md)。
本次署名与链接更新未完成游戏内实机验证，首个公开包以预发布形式提供。

## 从源码构建

CheatMenu：安装 .NET SDK 8.0.422（或兼容的 8.0 补丁版本），准备已完成 BepInEx 首次初始化的自有游戏安装：

```powershell
.\cheatmenu\build.ps1 -GameDirectory 'D:\你的游戏目录' -Dotnet 'dotnet'
```

纯逻辑测试可单独运行，不依赖游戏：

```powershell
dotnet run --project .\cheatmenu\tests\RoguePrince.CheatMenu.Tests -c Release
```

美术工具：使用 Python 3.12 x64 安装 `art-tool/requirements-lock.txt`，再运行 `art-tool/build.ps1`；完整命令见子目录 README。
源码仓库不包含游戏程序集、游戏资源、存档或个人测试日志。

## 授权

自有代码采用 **GPL-3.0-only**，版权署名为 **BrickZhou/青春啊砖在he边看月亮**。
允许收费；再分发须遵守 GPLv3 的对应源码、声明保留等要求。详见 [LICENSE](LICENSE) 与 [COPYRIGHT.md](COPYRIGHT.md)。
CheatMenu 对调用本游戏及随游戏提供的 Unity 运行库增加了 [有限链接许可](cheatmenu/GAME-LINKING-EXCEPTION.txt)；这不免除 CheatMenu 自身及其修改版本的 GPL 源码义务，也不授予分发游戏的权利。

BepInEx 及其他依赖继续遵守各自许可证，详见 [第三方声明](THIRD-PARTY-NOTICES.md)。
本工具的 GPL 许可不覆盖导出的游戏美术资源。项目与 Ubisoft、Evil Empire 无隶属或官方背书关系。
