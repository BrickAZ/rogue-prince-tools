# CheatMenu 0.1.36 · Windows 预发布 / prerelease

本版针对修改武器词缀后偶发Esc无法打开原生暂停的问题，发布输入接管与清理的修复候选。使用与本机已安装0.1.36相同的生产DLL；尚未新增实机验收结果。

## 下载与安装

- `02-CheatMenu-0.1.36.zip`：玩家安装包，含插件DLL、双语说明、版权/许可与构建信息。
- `CheatMenu-0.1.36-source.zip`：对应发布提交的完整仓库源码，也可查看 `cheatmenu-v0.1.36` 标签。
- `SHA256SUMS.txt`：以上文件的SHA-256校验值。

关闭游戏、备份存档，准备已初始化的BepInEx 6 IL2CPP Windows x64，将安装包中的 `BepInEx` 合并到游戏目录。进入存档后按F9。更新时保留配置，并只留一个启用的同名插件DLL。[完整安装](https://github.com/BrickAZ/rogue-prince-tools/blob/cheatmenu-v0.1.36/docs/INSTALL.md)

## 更新内容

- 输入接管保留原生EventSystem，只暂时禁用原先启用的输入模块，减少原生暂停选择控件时因当前系统为空导致异常的路径。
- 菜单可见或尚持有输入时，临时拦截原生暂停输入回调；归还完成后恢复放行。关闭等待Esc、鼠标确认和手柄确认/返回键松开。
- 输入归还失败时保留所有权并重试；正常卸载前必须清理完成。暂停输入补丁不可用时阻止打开菜单。
- 保留0.1.35词缀绘制恢复，词缀事务、Boss召唤及其他玩法实现未改变；练习进度写保护仍未实现。美术工具1.1.1、BepInEx助手与旧发行保留原版本。

## 验证与限制

发布DLL SHA-256：`F091EBB0CF33FBD2F18CC997BD6D1991F8B1C3D62160BCA9F1ABA37B37BC463E`。

- 最终生产构建0警告/0错误；16套1071项离线检查、实际编译DLL17项托管生命周期检查通过；98个PDB源码文档与生产源码一致。
- 使用生产InputLease的故障夹具：旧实现4通过12失败，修正后的扩展夹具20通过0失败；词缀绘制恢复夹具10通过0失败。
- 已核验实际安装版本0.1.36.0与哈希。本次发布未启动游戏；离线检查不证明实机恢复。
- 日志中首次原生暂停空引用发生在词缀应用之前。原生调用调查确认EventSystem缺失是一条可抛异常的路径，但旧堆栈没有指令地址，不能断言它解释全部偶发空引用。
- 原生暂停开关与导航、共享UI动作、Trials暂停共存、当前房间Boss自主行动/攻击/受伤与后续阶段、另一台机器首次安装仍待实机验收。当前房间Boss召唤保持原实现与未验收状态，不声明全部功能完成。

最小回归：获得武器→F9→武器词缀→载入主武器→修改并应用→F9关闭→Esc开关原生暂停；再用Esc关闭CheatMenu，松开后再次按Esc，重复并检查鼠标/手柄关闭。已有原生暂停时F9进出应保留暂停。如失效停止反复切换并保留 `BepInEx/LogOutput.log`。

## English release notes

0.1.36 publishes a candidate fix for Escape intermittently failing to open native pause after weapon-affix editing. It uses the same production DLL already installed locally. No additional in-game acceptance is claimed.

Download `02-CheatMenu-0.1.36.zip` to install, `CheatMenu-0.1.36-source.zip` for corresponding repository source and `SHA256SUMS.txt` to verify both. Close the game, back up saves, install/initialize compatible BepInEx 6 IL2CPP Windows x64 and merge the ZIP's `BepInEx` folder into the game directory. Enter a save and press F9; retain configuration and keep one enabled plugin DLL.

Input ownership now keeps native EventSystems alive and temporarily disables only previously enabled input modules. The native pause callback is blocked while CheatMenu is visible or still owns input, then allowed after cleanup. Closing waits for Escape, mouse confirmation or controller confirmation/back buttons to be released. Failed cleanup retains ownership and retries; normal unload is refused until cleanup completes. Opening is blocked if the pause-input patch is unavailable.

The 0.1.35 rendering recovery remains. Affix transactions, Boss spawning and other gameplay implementations are unchanged. Practice-progress write protection remains unimplemented. Art Tool 1.1.1, the BepInEx helper and older releases retain their versions.

Production build: zero warnings/errors; 1071 offline assertions in 16 suites; 17 managed lifecycle checks against the actual DLL; 98 verified PDB source documents. The production InputLease fixture reproduces 4 passes/12 failures on the old implementation; the extended fixed fixture passes 20/0. Affix rendering recovery passes 10/0. The installed version/hash were checked. The game was not launched for publication.

The first native-pause null reference in the captured log precedes affix application. Native investigation confirms a missing EventSystem can throw along this call path, but the old stack has no instruction address and cannot uniquely attribute every intermittent failure to it.

This is a **prerelease**. Native pause opening/closing and navigation, shared UI actions, coexistence with Trials pause, autonomous current-room Boss combat/later phases and fresh setup on another machine await in-game acceptance. Boss spawning remains unchanged and unaccepted; it is not a complete save sandbox or an all-features-complete claim.

Regression: obtain a weapon, F9 → Weapon Affixes → load main weapon, edit/apply, close with F9 and toggle native pause using Escape. Reopen CheatMenu, close with Escape, release it and press Escape again. Repeat with mouse/controller closing. F9 entry/exit while native pause is open should preserve pause. On failure, stop repeated toggling and preserve `BepInEx/LogOutput.log`.

GPL-3.0-only with the included limited game linking permission. Corresponding source and third-party notices are included; no game/loader binaries, resources, private logs, configuration, backups or saves are published.
