# BepInEx 前置 / Prerequisite

BepInEx 由 **BepInEx 团队及其贡献者**开发，不属于 BrickZhou 的作品。
本目录提供固定版本获取与安装说明；原始加载器直接从官方服务器下载。
The loader is obtained directly from upstream; this repository does not redistribute its binaries.

- 版本 / Version: **6.0.0-be.788+5b766a3**
- 架构 / Target: **Unity.IL2CPP-win-x64**（不要选 Mono 或 x86）
- [官方构建页 / Official build](https://builds.bepinex.dev/projects/bepinex_be)
- [上游源码 / Upstream source](https://github.com/BepInEx/BepInEx/tree/5b766a3)
- [上游许可证 / LGPL-2.1](https://github.com/BepInEx/BepInEx/blob/5b766a3/LICENSE)
- [官方安装文档 / Installation](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html)
- 原包 SHA-256: `F4CC496BD098A0DF4164B81E3737297707F13A47C2478DBA2F60EEFAB784817A`

可在 PowerShell 运行本目录 `Download-BepInEx.ps1`，下载固定版本并校验 SHA-256。
也可在官方构建页手动选择同名 ZIP。脚本只下载，不修改游戏安装。

```powershell
.\Download-BepInEx.ps1 -OutputDirectory 'D:\Downloads'
```

关闭游戏，将 ZIP 内部全部内容解压到游戏根目录，让 `BepInEx`、`dotnet`、
`winhttp.dll` 和 `doorstop_config.ini` 与游戏 EXE 同级。启动游戏一次，
等待生成 `BepInEx/interop` 等必要文件，进入主菜单后退出，再安装 CheatMenu。
首次初始化可能联网下载依赖。已经正确安装相同版本时无需重复安装。

Close the game, extract the ZIP contents into its root directory, launch once
to initialize (network access may be required), exit, then install CheatMenu.
Do not create an extra nesting level. Existing different loaders require
compatibility checks before replacement. BepInEx is unnecessary for the art tool.
