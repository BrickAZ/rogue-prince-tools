# 常见问题与反馈

[首页](../README.md) · [English](FAQ.en.md) · [完整安装](INSTALL.md)

<a id="download"></a>
## 我该下载哪个？为什么源码包里没有 EXE？

成品下载见[首页选择表](../README.md#选择下载)。只用美术工具时下载 ArtTool 即可；首次用 CheatMenu 时还需要 BepInEx。发布页的 Source code 和 Code → Download ZIP 提供源码，不能代替成品包。

<a id="loader"></a>
## 前置下载失败，或没有生成 interop 文件

- 下载失败：先完整解压下载助手，确认 CMD 与 PS1 同级、能联网；也可用[官方固定版本直链](../prerequisites/README.md#下载失败时手动获取)手动下载并校验。
- 找不到下载的 ZIP：默认在**助手旁边的 Downloads 文件夹**。PowerShell 的自定义输出参数会改变这个位置。
- 校验不符：不要安装该文件。助手不会覆盖同名但校验不同的包，可在新的空文件夹重新下载。
- 没有 `BepInEx/interop/Assembly-CSharp.dll`：确认安装的是下载得到的原始 BepInEx 包、类型为 IL2CPP win-x64、`winhttp.dll` 与游戏 EXE 同级。启动游戏完成首次初始化后再检查。
- 初始化出错：保留 `BepInEx/LogOutput.log`，记录网络或依赖下载错误及当前游戏版本。已有其他加载器时先核对兼容性。

<a id="f9"></a>
## F9 没反应

1. 进入存档，使游戏窗口获得焦点；笔记本尝试 Fn + F9。
2. 核对[安装目录](INSTALL.md#cheatmenu)，确保 DLL 没有多套一层文件夹，也没有多个启用副本。
3. 在 `BepInEx/LogOutput.log` 搜索 `[RPCheat] plugin_loaded`。没有加载记录时，优先处理前置、文件位置或日志中的插件加载错误。
4. 若此前改过快捷键，关闭游戏后查看 `BepInEx/config/local.rogueprince.cheatmenu.cfg` 的 `ToggleKey`。默认 F9；支持 F1–F12，但 F8 保留给原有 HUD。
5. 若插件已加载但仍无界面，按下方反馈格式提交游戏版本、日志和复现步骤。

菜单按钮呈灰色可能表示角色、关卡或对应原生组件尚未就绪，不等同于插件没有加载。

<a id="saves"></a>
## 停用菜单会撤销修改吗？自动备份够用吗？

不会撤销已经保存的永久修改。使用前关闭游戏，将 `userdata/saves` 复制到游戏目录外。指定永久操作的自动备份位于 `BepInEx/CheatMenuBackups`，只覆盖当时磁盘上的存档；安装 ZIP 本身不会执行备份。

<a id="levels"></a>
## 为什么物品等级建议 1–5？

这是原版等级范围。菜单输入允许 1–100，但超出 1–5 的原生行为未保证；0 级不存在。并非所有物品都使用等级。请勿把“允许输入”理解为已经验证每个等级有效。

<a id="art"></a>
## 美术查看入口说没有结果，或浏览器没有自动打开

先运行 `一键导出美术资源.exe`。查看 CMD 只打开已有图片库，不负责导出。默认文件应为 CMD 旁的 `ArtExports/index.html`，可直接双击它。

若导出时使用 `--output` 指定其他位置，直接打开那个目录的 `index.html`。单独搬走 HTML 会使相对图片路径失效，移动结果时应保留整个输出目录。

<a id="export"></a>
## 找不到游戏、导出失败、没有图片或分类不准确

- 把 EXE 放在游戏根目录，或用[命令行的 --game 参数](../art-tool/README.md)指定目录。
- 保留导出窗口的错误信息。若已产生输出，查看 `ArtExports/report.json` 与 `ArtExports/errors.json`。
- 清空搜索、恢复“全部”分类和筛选，排除筛选条件隐藏图片的情况。
- 内容分类由名称、路径和元数据推断，部分图片会进入“其他 / 待确认”；提取图片也不包含游戏内灯光与材质合成效果。
- 需要重新导出时可使用 `--force`；自定义输出目录须为空或由本工具创建。遇到路径保护错误时选择合适目录，不要绕过保护。
- 其他游戏版本的资源布局未确认。反馈时提供实际版本和错误，不需要上传游戏资源包。

<a id="report"></a>
## 怎么反馈问题？

[创建问题报告](https://github.com/BrickAZ/rogue-prince-tools/issues/new?template=bug_report.md)，可用中文或英文。请提供：

- 出问题的组件及版本、Windows 版本、游戏版本 / Steam Build。
- 从哪些步骤开始出现问题，预期结果与实际结果。
- CheatMenu：BepInEx 版本、相关 `LogOutput.log` 片段、插件加载记录是否存在。
- 美术工具：导出窗口错误、`report.json` / `errors.json` 的相关内容；必要时附界面截图。

发布前检查日志中的用户名和个人路径。无需上传存档、游戏 DLL 或完整美术资源。当前仍是预发布，已确认与未确认的范围见[验证记录](VALIDATION.md)。
