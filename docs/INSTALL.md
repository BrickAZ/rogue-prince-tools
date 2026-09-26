# 玩家安装与使用

[首页与下载](../README.md#选择下载) · [English](INSTALL.en.md) · [常见问题](FAQ.md)

参考环境：Windows x64 / 游戏 v1.1.0 / Steam Build 24299434。当前是预发布；其他环境与另一台电脑的首次安装尚未确认。

<a id="cheatmenu"></a>
## CheatMenu：第一次安装

1. **找到游戏目录并备份存档。** 关闭游戏，在 Steam → 管理 → 浏览本地文件找到含 `The Rogue Prince of Persia.exe` 的目录。把 `userdata/saves` 复制到游戏目录外的备份位置。若尚未产生存档，先正常运行游戏创建存档，再关闭游戏备份。
2. **安装前置。** 按 [BepInEx 安装步骤](../prerequisites/README.md)下载助手、运行 CMD、找到助手旁 `Downloads` 内的新 ZIP，再解压这个 BepInEx 原包。助手 ZIP 本身不是加载器。
3. **初始化前置。** 启动游戏，等待初始化，进入主菜单后退出。确认 `BepInEx/interop/Assembly-CSharp.dll` 已生成；未生成则先处理[加载器问题](FAQ.md#loader)。
4. **安装菜单。** 下载 [02-CheatMenu-0.1.8.zip](https://github.com/BrickAZ/rogue-prince-tools/releases/download/v1.0.0-rc.1/02-CheatMenu-0.1.8.zip)，将里面的 `BepInEx` 文件夹合并到游戏目录。不要把整个 ZIP 名称文件夹套在外面。
5. **进入游戏。** 进入存档，按 **F9** 打开 / 关闭菜单。点击 **设置/setting → 语言/language** 切换中文或 English。笔记本可能需要 Fn + F9。

正确的主要文件位置如下（其他游戏文件省略）：

```text
游戏目录/
├─ The Rogue Prince of Persia.exe
├─ winhttp.dll
├─ doorstop_config.ini
├─ dotnet/
├─ userdata/
│  └─ saves/
└─ BepInEx/
   ├─ core/
   ├─ interop/
   │  └─ Assembly-CSharp.dll
   └─ plugins/
      └─ RoguePrince.CheatMenu/
         └─ RoguePrince.CheatMenu.dll
```

### 怎样判断成功

- 前置层：已生成 interop 文件；可查看 `BepInEx/LogOutput.log` 中的初始化错误。
- 插件层：日志出现 `[RPCheat] plugin_loaded version=0.1.8`，表示插件已加载，不等于全部玩法操作通过验证。
- 界面层：进入存档后 F9 能显示菜单；部分功能需要角色和关卡状态就绪。

### 使用前须知

原版物品等级为 **1–5**。菜单允许输入更高等级，但不保证原生效果；0 级不可用。永久资源和徽章栏位修改可能随游戏保存，停用插件不会撤销已保存的变化。

指定永久操作会将已落盘存档备份到 `BepInEx/CheatMenuBackups`；它不能捕获尚未写入磁盘的进度，也不能代替第 1 步的完整备份。

<a id="update"></a>
## CheatMenu：更新、停用与移除

**更新：** 关闭游戏，备份存档和当前插件文件夹，再将新版本 ZIP 的内容合并到游戏目录。只保留一个启用的 `RoguePrince.CheatMenu.dll`，旧副本移到游戏目录外。已经匹配的 BepInEx 不需要重复安装。当前版本配置在 `BepInEx/config/local.rogueprince.cheatmenu.cfg`；普通更新可保留它。

**暂时停用：** 关闭游戏，将插件的 `RoguePrince.CheatMenu.dll` 改名为 `RoguePrince.CheatMenu.dll.disabled`；恢复时改回原名。

**移除菜单：** 关闭游戏，把 `BepInEx/plugins/RoguePrince.CheatMenu` 移到游戏目录外。保留所需备份。其他模组可能仍使用 BepInEx，因此不应为移除菜单而直接删除整个加载器。

<a id="art-tool"></a>
## 美术资源工具：安装、导出与查看

1. 下载 [03-ArtTool-1.1.1.zip](https://github.com/BrickAZ/rogue-prince-tools/releases/download/v1.0.0-rc.1/03-ArtTool-1.1.1.zip)，将全部内容解压到游戏根目录。无需安装 BepInEx 或 Python。
2. 双击 `一键导出美术资源.exe`，等待导出结束。它读取自有游戏安装中的资源，结果保存到 `ArtExports`。
3. 成功后浏览器自动打开 `ArtExports/index.html`。若未自动打开，双击 `打开美术资源库.cmd`。
4. 在图片库切换语言、分类与筛选；点击图片查看原尺寸 PNG。以后使用 CMD 查看已有结果，它不会重新导出。

```text
游戏目录/
├─ The Rogue Prince of Persia.exe
├─ 一键导出美术资源.exe
├─ 打开美术资源库.cmd
├─ ArtTool-Licenses/
└─ ArtExports/                 ← 导出时生成
   ├─ index.html
   ├─ images/
   ├─ report.json
   └─ errors.json
```

**成功标志：** 导出程序结束时显示报告，`ArtExports/index.html` 存在且图片库能打开。核对 `report.json` 和 `errors.json` 是否记录失败；有页面并不代表每个资源都成功导出。

**更新：** 关闭正在运行的导出程序，将新包解压到原位置。保留需要的导出结果。若使用自定义输出目录，直接打开该目录的 `index.html`；CMD 固定打开自身旁的 `ArtExports/index.html`。[高级用法](../art-tool/README.md)

**移除：** 关闭导出程序，把上述 EXE、CMD 和 `ArtTool-Licenses` 移走即可。导出的 `ArtExports` 可单独保留。

游戏美术资源归原权利人所有，工具的 GPL 许可不授予再分发游戏资源的权利。
