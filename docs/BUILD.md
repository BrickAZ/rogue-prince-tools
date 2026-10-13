# 从源码构建

[首页](../README.md) · [English](BUILD.en.md) · [玩家安装](INSTALL.md)

这里面向修改源码或自行构建的开发者。普通玩家直接使用 Releases 成品包。

## CheatMenu

在仓库根目录运行。需要 Windows、.NET SDK **8.0.422**（或兼容的 8.0 补丁版本），以及完成 BepInEx IL2CPP x64 首次初始化的自有游戏安装。参考环境为游戏 v1.1.0、Steam Build 24299434、Unity 6000.0.64f1。

```powershell
.\cheatmenu\build.ps1 -GameDirectory 'D:\YourGame' -Dotnet 'dotnet'
```

项目目标框架为 net6.0；编译所用 SDK 版本与目标框架不同。游戏程序集从指定安装的 BepInEx/interop 引用，不随仓库分发。首次依赖还原可能联网。

纯逻辑测试无需游戏：

```powershell
dotnet run --project .\cheatmenu\tests\RoguePrince.CheatMenu.Tests -c Release
```

完整构建入口运行16组回归。BossTargets 与 ToolLimits 两组需要自有 BepInEx 中的 Harmony，路径由 `-GameDirectory` 传递，也可用 `ROGUE_GAME_DIR` 指定。其余夹具不启动游戏。

实际编译 DLL 的17项托管生命周期检查（不进入游戏）：

```powershell
dotnet run --project .\cheatmenu\tests\RoguePrince.CompiledLifecycle.Tests -c Release -- '完整路径\RoguePrince.CheatMenu.dll' 'D:\YourGame'
```

只打包 CheatMenu（不重新发布美术工具）：

```powershell
python scripts/package_cheatmenu.py --dll cheatmenu/src/RoguePrince.CheatMenu/bin/Release/net6.0/RoguePrince.CheatMenu.dll
```

打包需在干净的Git提交上执行，输出安装包、该提交的完整源码ZIP和SHA256SUMS.txt到 `release/cheatmenu-0.1.36`。安装包仅含插件、自有说明和许可证。历史验证结果只对应发布说明记录的DLL哈希。

<a id="art-tool"></a>
## 美术资源工具

从仓库根目录开始，使用 Python **3.12 x64**（参考构建版本 3.12.14）：

```powershell
cd art-tool
python -m pip install --target .build-deps -r requirements-lock.txt
.\build.ps1 -Python python
$env:PYTHONPATH = "$PWD\.build-deps"
$env:ROGUE_TEST_GAME = 'D:\YourGame'
python -m unittest discover -v
```

产物为 `art-tool/dist/一键导出美术资源.exe`。构建脚本的 `-DependencyPath PATH` 可复用版本匹配的依赖目录。

便携集成检查使用自己的游戏资源，通过 `ROGUE_TEST_GAME` 指定目录；测试把 EXE 和单个资源包复制到临时 Unicode 路径，清除 Python / FMOD 环境变量并使用无关工作目录。无游戏资源时不能把集成检查称为完整验收。

## 打包与验证记录

以下脚本保留为 v1.0.0-rc.1 两工具发行的打包入口。发布当前 CheatMenu 使用上方独立打包命令，不运行这个旧版入口：

```powershell
python scripts/package.py
```

输出到 `release`，包含 ZIP 与 `SHA256SUMS.txt`。打包脚本内的版本和源码标签用于标识对应源码；维护者发布新版本前应同步更新它们。本次网页说明修订不替换已有 v1.0.0-rc.1 二进制包或移动其源码标签。

源码仓库不包含游戏程序集、完整游戏资源或存档。文档截图展示真实工具界面，其中游戏美术保留原权利。历史构建与测试结果见[发布验证记录](VALIDATION.md)；它不代表任意本地构建已通过验证。
