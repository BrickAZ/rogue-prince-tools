# CheatMenu 0.1.35 · Windows 预发布 / prerelease

本次发布当前CheatMenu的源码与安装包，更新此前GitHub上的0.1.8快照。美术工具1.1.1、BepInEx下载助手及旧发行保留原版本。

## 下载与安装

- `02-CheatMenu-0.1.35.zip`：玩家安装包，内含插件DLL、双语说明、版权/许可与构建信息。
- `CheatMenu-0.1.35-source.zip`：本次发布提交的完整源码；开发者也可查看 `cheatmenu-v0.1.35` 标签。
- `SHA256SUMS.txt`：以上文件的SHA-256校验值。

关闭游戏、备份存档，准备已初始化的BepInEx 6 IL2CPP Windows x64，将安装包中的 `BepInEx` 合并到游戏目录。进入存档后按F9。更新时保留配置，并只留一个启用的同名插件DLL。[完整安装](https://github.com/BrickAZ/rogue-prince-tools/blob/cheatmenu-v0.1.35/docs/INSTALL.md)

## 更新内容

- 公开自0.1.8以来积累的当前源码，包括词缀编辑、怪物/Boss、关卡、预设、训练及调试工具入口。[功能与边界](https://github.com/BrickAZ/rogue-prince-tools/blob/cheatmenu-v0.1.35/cheatmenu/README.md)
- 0.1.35处理获得武器后打开词缀页导致菜单消失的问题：实际运行库中的Unity滚动接口会抛 `Method unstripping failed`；改用有实现的裁剪接口及自有滚动处理。
- 绘制失败时清理裁剪与按键捕获，归还菜单持有的输入/暂停，重开返回角色页。
- 公开构建脚本与回归夹具。两个Harmony测试的路径改为读取GameDir；实际编译DLL生命周期检查通过参数读取游戏路径，不改变生产源码与安装DLL。

## 验证与限制

发布DLL SHA-256：`F82C2D6A6434D317558721A4CB532AC36369A1EFEE492590AF04CE529388C8BD`。

- 最终生产构建0警告/0错误；15套1051项离线检查、实际编译DLL17项托管生命周期检查通过；97个PDB源码文档与生产源码一致。
- 词缀生产绘制/输入恢复故障夹具：原实现0通过6失败，修正后10通过0失败。
- 发布前定向复核：独立中文路径的Harmony测试BossTargets 228项、ToolLimits 53项通过，参数化生命周期夹具17项通过；生产源码和DLL未变。
- 实际安装已核对0.1.35.0版本及哈希。本次发布未重新启动游戏。词缀页和Esc恢复、当前房间Boss自主行动/攻击/受伤及后续阶段、其他机器首次安装仍待实机验收。
- 当前房间Boss战斗保留候选，不以禁用召唤或转场代替；不是完整存档沙盒，也不声明全部功能完成。

最小词缀回归：获得武器→F9→武器词缀→载入主武器→查看/滚动当前和草稿词缀→切换页面→关闭→Esc打开原生暂停。无需应用词缀。如失败保留 `BepInEx/LogOutput.log`。

## English release notes

This publishes the current CheatMenu source and install package, updating the previously public 0.1.8 snapshot. Art Tool 1.1.1, the BepInEx helper and old releases retain their existing versions.

Download `02-CheatMenu-0.1.35.zip` to install, `CheatMenu-0.1.35-source.zip` for the complete corresponding repository source and `SHA256SUMS.txt` to verify both. Close the game, back up saves, install/initialize compatible BepInEx 6 IL2CPP Windows x64, then merge the ZIP's `BepInEx` folder into the game directory. Enter a save and press F9; keep one enabled plugin DLL.

The source contains the accumulated item/affix, enemy/Boss, level, preset, practice and diagnostic entries. 0.1.35 addresses the menu disappearing on the affix page after obtaining a weapon: the game's Unity scroll API throws `Method unstripping failed`. Supported native clipping and custom scrolling replace it. Drawing failures now balance clipping, return owned input/pause, clear captures and reset to the player tab.

Production build: zero warnings/errors; 1051 offline assertions in 15 suites; 17 managed lifecycle checks against the actual DLL; 97 verified PDB source documents. The actual drawing/input fixture reproduced 0/6 on the old implementation and passes 10/0 after the fix. Publication makes Harmony test references and the lifecycle harness accept a supplied game directory. Targeted checks passed with Harmony in a separate Unicode path: 228 BossTargets, 53 ToolLimits and 17 lifecycle assertions; production source/DLL remain unchanged.

This is a **prerelease**. The installed DLL/version/hash were checked, but the game was not relaunched for publication. Affix rendering/Escape recovery, autonomous current-room Boss combat, later phases and fresh setup on another machine remain unverified. Boss combat remains a candidate, not a complete save sandbox or an all-features-complete claim.

Regression: obtain a weapon, F9 → Weapon Affixes → load main weapon, read/scroll current/draft affixes, switch tabs, close the menu and verify Escape opens native pause. Applying affixes is unnecessary for this display check. Preserve `BepInEx/LogOutput.log` on failure.

GPL-3.0-only with the included limited game linking permission. Corresponding source and third-party notices are included; no game/loader binaries, resources, private logs, configuration, backups or saves are published.
