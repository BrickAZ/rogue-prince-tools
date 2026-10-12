# CheatMenu 0.1.35

《The Rogue Prince of Persia》Windows x64 社区调试菜单。作者：BrickZhou/青春啊砖在he边看月亮。

[下载预发布](https://github.com/BrickAZ/rogue-prince-tools/releases/tag/cheatmenu-v0.1.35) · [安装指南](https://github.com/BrickAZ/rogue-prince-tools/blob/main/docs/INSTALL.md) · [验证范围](https://github.com/BrickAZ/rogue-prince-tools/blob/main/docs/VALIDATION.md)

## 安装与打开

1. 关闭游戏并备份 `userdata/saves`。先安装兼容的 BepInEx 6 IL2CPP Windows x64，启动一次完成初始化后退出。[前置说明](https://github.com/BrickAZ/rogue-prince-tools/blob/main/prerequisites/README.md)
2. 下载 `02-CheatMenu-0.1.35.zip`，将其中的 `BepInEx` 文件夹合并到游戏目录。主要文件应为 `BepInEx/plugins/RoguePrince.CheatMenu/RoguePrince.CheatMenu.dll`。
3. 进入存档后按 **F9** 打开或关闭。设置页可切换中文/英文、修改快捷键和界面大小。菜单打开时会暂停并接管输入，关闭后归还。
4. 更新时保留一个启用的同名 DLL，旧副本移到游戏目录外；停用可在游戏关闭时将 DLL 改名为 `.dll.disabled`。

日志中的 `[RPCheat] plugin_loaded version=0.1.35` 表示本版已加载。安装不会自动备份存档；永久修改可能被游戏保存，停用插件不会撤销它们。

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

## 0.1.35 变化与已知限制

- 0.1.34词缀页调用的Unity滚动接口在实际运行库里直接抛 `Method unstripping failed`，导致菜单绘制中断。本版改用有原生实现的裁剪接口，保留换行、滚轮和拖动滚动条。
- 绘制失败时配对清理裁剪区域，释放菜单持有的输入/暂停，清空按键捕获并返回角色页，避免重开立即进入同一失败页。
- 词缀显示/效果事务、当前房间Boss战斗候选及其他原生功能保留。本版词缀页、关闭菜单后的Esc恢复和完整Boss自主战斗仍待实机确认。
- 当前房间Boss不是完整存档沙盒；不要把模型或血量出现当作战斗正常。所有Boss/后续阶段、任意房间及其他游戏版本不保证兼容。

最小回归：获得武器→F9→武器词缀→载入主武器→查看/滚动当前和草稿词缀→切换页面→关闭菜单→Esc打开原生暂停。显示检查无需应用词缀。若出现持续不动的Boss或其他异常，停止叠加召唤并保留 `BepInEx/LogOutput.log`。

构建、1051项离线回归、17项实际编译DLL托管生命周期检查和本机安装核验已通过；这些检查没有在游戏中执行。美术工具继续使用原发行包。

## English

Community debug menu for The Rogue Prince of Persia, Windows x64. Install compatible BepInEx 6 IL2CPP x64, launch once to initialize and exit, then merge the release ZIP's `BepInEx` folder into the game directory. Back up `userdata/saves` first. Enter a save and press **F9**; language, shortcuts and scale are in Settings. Keep one enabled plugin DLL. Close the game before updating or disabling it.

Entries cover player/resources, item spawning, current/new-weapon affixes, enemies/Bosses, level travel, practice/timers, favorites/presets, limited current-run ability trials/checkpoints and combat/scene diagnostics. A checkpoint is not a complete world/save snapshot. Persistent operations may be saved by the game.

0.1.35 replaces the affix page's throwing Unity scroll API with supported native clipping and custom scrolling. Drawing failure cleanup balances clipping, returns owned pause/input, clears captures and resets the page. The installed DLL passed build, 1051 offline regression checks and 17 managed compiled-DLL lifecycle checks. The game was not relaunched for this release: affix/Escape recovery and autonomous current-room Boss combat await in-game acceptance. All Bosses, later phases, arbitrary rooms and other game versions are not guaranteed. Boss spawning is not a complete save sandbox.

Regression: obtain a weapon, open F9 → Weapon Affixes, load the main weapon, read/scroll current and draft affixes, switch tabs, close the menu and check Escape opens native pause. No affix application is required for this display check. Preserve `BepInEx/LogOutput.log` if it fails.

## 许可 / License

GPL-3.0-only with the included limited game linking permission. Retain copyright and third-party notices and provide corresponding source when redistributing. Source is available with the GitHub release and its immutable tag. Game binaries/assets, loader binaries, user configuration and saves are not included. Unofficial project; no affiliation with Ubisoft or Evil Empire.

[B站 / Bilibili](https://space.bilibili.com/517390275) · [YouTube](https://www.youtube.com/@Brickzhou)
