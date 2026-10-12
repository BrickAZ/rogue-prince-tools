# Changelog

## 2026-10-12 — CheatMenu 0.1.35 prerelease / 预发布

- Published current CheatMenu source and install package, updating the previously public 0.1.8 snapshot.
- Added the accumulated item/affix, enemy/Boss, level, training, preset and debugging entries. Runtime limits remain documented.
- Fixed the confirmed affix drawing failure caused by unsupported Unity scroll APIs; preserve clipping and recover owned input/capture state after drawing errors.
- Made Harmony test paths configurable and included the compiled-DLL lifecycle harness with a game-directory argument.
- 发布现有0.1.35源码与安装包，补齐自0.1.8以来的物品/词缀、怪物/Boss、关卡、练习、预设和调试功能入口。
- 修正词缀页使用不兼容Unity滚动接口导致的菜单消失，并补齐绘制失败后的裁剪与输入状态恢复。
- 测试路径接受游戏目录参数，附实际编译DLL的生命周期检查；当前房间Boss战斗、0.1.35词缀页及Esc恢复仍待实机验收。
- [Detailed release and limits / 详细说明与限制](docs/releases/cheatmenu-v0.1.35.md). Art tool and loader-helper binaries are unchanged.

## 2026-09-26 — documentation update / 文档更新

- Added direct package downloads and matching Chinese/English player guides, FAQ and build instructions.
- Clarified the helper ZIP versus the actual BepInEx archive, including its Downloads subfolder.
- Added actual art-library screenshots and a bilingual issue-report template.
- Explained component versions and verification scope. Existing v1.0.0-rc.1 binaries and source tag are unchanged.
- 新增成品直链、对应的中英文安装 / 故障排查 / 开发文档、真实图片库截图和问题反馈模板。
- 明确前置下载助手与真正安装包的区别，保留预发布状态；程序包和对应源码标签未改变。
## 2026-09-26 — first public prerelease

- CheatMenu 0.1.8: author attribution and explicit Bilibili/YouTube buttons in Settings; portable build configuration.
- Art tool 1.1.1: attribution in EXE output and offline gallery, `--version`, bundled tool license notices; corrected gallery-launcher instructions.
- Excluded unused proprietary FMOD native audio library from the art tool package.
- Two tools in one source repository; pinned BepInEx prerequisite download helper and installation instructions.
- GPLv3 for original code; narrow game linking permission for CheatMenu; preserved third-party notices.
- No gameplay mechanics changed. Menu layout/link actions still need in-game confirmation.
