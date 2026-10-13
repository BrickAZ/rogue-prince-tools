# CheatMenu 0.1.36

《The Rogue Prince of Persia》Windows x64 社区调试菜单。作者：BrickZhou/青春啊砖在he边看月亮。

[下载预发布](https://github.com/BrickAZ/rogue-prince-tools/releases/tag/cheatmenu-v0.1.36) · [安装指南](https://github.com/BrickAZ/rogue-prince-tools/blob/main/docs/INSTALL.md) · [验证范围](https://github.com/BrickAZ/rogue-prince-tools/blob/main/docs/VALIDATION.md)

## 安装与打开

1. 关闭游戏并备份 `userdata/saves`。先安装兼容的 BepInEx 6 IL2CPP Windows x64，启动一次完成初始化后退出。[前置说明](https://github.com/BrickAZ/rogue-prince-tools/blob/main/prerequisites/README.md)
2. 下载 `02-CheatMenu-0.1.36.zip`，将其中的 `BepInEx` 文件夹合并到游戏目录。主要文件应为 `BepInEx/plugins/RoguePrince.CheatMenu/RoguePrince.CheatMenu.dll`。
3. 进入存档后按 **F9** 打开或关闭。设置页可切换中文/英文、修改快捷键和界面大小。菜单打开时会暂停并接管输入，关闭后归还。
4. 更新时保留一个启用的同名 DLL，旧副本移到游戏目录外；停用可在游戏关闭时将 DLL 改名为 `.dll.disabled`。

日志中的 `[RPCheat] plugin_loaded version=0.1.36` 表示本版已加载。安装不会自动备份存档；永久修改可能被游戏保存，停用插件不会撤销它们。

## 功能入口

| 页面/分组 | 内容与边界 |
|---|---|
| 角色、资源 | 生命/能量、无敌/无限能量、资源与徽章栏位调整；部分原生操作会影响保存 |
| 物品、武器词缀 | 搜索与生成物品，当前武器/工具及菜单新武器的词缀编辑，收藏和预设；原版物品等级1–5 |
| 怪物、Boss | 选择/召唤/清理菜单生成的实例，AI和练习控制；当前房间Boss战斗启动仍待实机验收 |
| 关卡、练习 | 下一关与指定关卡入口、练习计时、原生Boss阶段练习；操作依赖当前游戏状态 |
| 本局试装与检查点 | 已解锁技能的有限组合试装、受限的角色状态检查点；不等同完整世界/存档快照 |
| 调试工具 | 战斗/伤害统计、场景报告与叠加显示、帧观察、速度/效果/工具次数控制等 |
| 设置与预设 | 中英文、快捷操作、缩放/位置、收藏、武器预设JSON、日志与作者主页 |

这些是已实现的功能入口，不代表每个原生玩法都已完成实机验收。操作不可用时应读取菜单提示。

## 0.1.36 变化与已知限制

- 针对修改词缀后偶发Esc无法打开原生暂停的问题，调整菜单的输入接管。旧实现会禁用Unity的EventSystem，使原生暂停界面选中控件时可能遇到空引用；本版保留EventSystem，仅暂时接管原先启用的输入模块。
- 菜单可见或仍持有输入时，暂停游戏的原生输入回调被临时拦截；归还输入后恢复放行。关闭会等待Esc、鼠标确认或手柄确认/返回键松开，避免同一次输入同时触发两个菜单。
- 输入归还失败时保留所有权并重试；输入尚未清理完成时拒绝正常卸载。暂停输入补丁不可用时不打开菜单。
- 保留0.1.35的词缀绘制恢复处理。词缀事务、Boss召唤和其他玩法实现未在本版修改，练习进度写保护仍未实现。
- 本版属于修复候选，尚未完成实机验收。原生暂停显示/开关、键盘/鼠标/手柄导航与Trials暂停共存仍需确认；当前房间Boss完整自主战斗仍未通过验收。Boss召唤不是完整存档沙盒，所有Boss/后续阶段、任意房间及其他游戏版本不保证兼容。

最小回归：获得武器→F9→武器词缀→载入主武器→修改并应用词缀→F9关闭→Esc开关原生暂停；再打开CheatMenu并用Esc关闭，松开后再次按Esc。重复并检查鼠标/手柄关闭；已有原生暂停时F9进出应保留原生暂停。如再次失效，停止反复切换并保留 `BepInEx/LogOutput.log`。

生产构建0警告/0错误，16套1071项离线回归、17项实际编译DLL托管生命周期检查以及本机安装核验已通过；这些检查没有在游戏中执行。美术工具继续使用原发行包。

## English

Community debug menu for The Rogue Prince of Persia, Windows x64. Install compatible BepInEx 6 IL2CPP x64, launch once to initialize and exit, then merge the release ZIP's `BepInEx` folder into the game directory. Back up `userdata/saves` first. Enter a save and press **F9**; language, shortcuts and scale are in Settings. Keep one enabled plugin DLL. Close the game before updating or disabling it.

Entries cover player/resources, item spawning, current/new-weapon affixes, enemies/Bosses, level travel, practice/timers, favorites/presets, limited current-run ability trials/checkpoints and combat/scene diagnostics. A checkpoint is not a complete world/save snapshot. Persistent operations may be saved by the game.

0.1.36 is a candidate fix for Escape intermittently failing to open native pause after affix editing. The old input lease disabled Unity EventSystems, which could leave native pause selection without a current system. This release keeps EventSystems alive and temporarily disables only previously enabled input modules. The native pause callback is blocked while the menu is visible or still owns input, then allowed after release. Closing waits for Escape, mouse confirmation or controller confirmation/back buttons to be released. Failed cleanup retains ownership and retries; normal unload is refused until cleanup completes. The menu cannot open if its pause-input patch is unavailable.

The 0.1.35 affix rendering recovery remains. Affix transactions, Boss spawning and other gameplay implementations are unchanged; practice-progress write protection remains unimplemented. Production build: zero warnings/errors, 1071 offline assertions in 16 suites and 17 managed lifecycle checks against the actual DLL. Installation/version/hash were verified. These checks were not executed inside the game.

This is a prerelease awaiting in-game acceptance of native pause opening/closing, keyboard/mouse/controller navigation and coexistence with Trials pause. Autonomous current-room Boss combat remains unaccepted. Boss spawning is not a complete save sandbox; all Bosses, later phases, arbitrary rooms and other game versions are not guaranteed.

Regression: obtain a weapon, open F9 → Weapon Affixes, load the main weapon, edit/apply affixes, close with F9 and toggle native pause with Escape. Reopen CheatMenu, close it with Escape, release the key and press Escape again. Repeat with mouse/controller closing. Entering/leaving F9 while native pause is already open should preserve native pause. If it fails, stop repeated toggling and preserve `BepInEx/LogOutput.log`.

## 许可 / License

GPL-3.0-only with the included limited game linking permission. Retain copyright and third-party notices and provide corresponding source when redistributing. Source is available with the GitHub release and its immutable tag. Game binaries/assets, loader binaries, user configuration and saves are not included. Unofficial project; no affiliation with Ubisoft or Evil Empire.

[B站 / Bilibili](https://space.bilibili.com/517390275) · [YouTube](https://www.youtube.com/@Brickzhou)
