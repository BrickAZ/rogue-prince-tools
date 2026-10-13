# 发布验证 / Release validation

## CheatMenu 0.1.36 — 2026-10-13

Published DLL SHA-256: `F091EBB0CF33FBD2F18CC997BD6D1991F8B1C3D62160BCA9F1ABA37B37BC463E`.

| 检查 / Check | 结果 / Result | 范围 / Scope |
|---|---|---|
| Production Release build | 0 warnings, 0 errors | Same production source/DLL as the verified local installation |
| Offline regression | 1071 passed, 0 failed / 16 suites | Managed fixtures; not in-game acceptance |
| Input ownership fixture | Old implementation 4 passed / 12 failed; fixed 20 / 0 | Actual production InputLease and scoped pause prefix, including failed cleanup retry and close-key handoff |
| Affix drawing regression | 10 passed / 0 failed | Existing native clipping and drawing-failure recovery preserved |
| Compiled-DLL lifecycle | 17 passed / 0 failed | Actual DLL with local native dependencies; no game launch |
| Compiled source | 98 PDB source documents verified | Production source/DLL correspondence |
| Native and compiled wiring | Verified | SetSelected null-current branch, EventSystem/module lifecycle, scoped pause prefix, retry and unload guards |
| Local installation | Version/hash verified | 33 old plugin files backed up; 17 other protected hashes unchanged |
| Runtime | Pending | Native pause display, keyboard/mouse/controller navigation and shared UI-action recovery; existing Boss combat limits remain |

0.1.35日志记录原生PauseInputController→UIContainer.SetSelected空引用，第一处异常早于词缀应用。实际原生方法存在EventSystem.current为空的抛异常分支；旧菜单会禁用EventSystem，夹具复现此路径。0.1.36保留系统并按输入租约阻止原生暂停重入。旧堆栈没有指令地址，因此不能断言它是所有间歇故障的唯一原因；发布时未重新启动游戏验收。

The 0.1.35 log records a native pause SetSelected null reference before the first affix application. The actual method has a null EventSystem.current throw path and the old menu disabled that system; the fixture reproduces this path. 0.1.36 preserves the system and scopes native pause suppression to menu input ownership. The historical stack lacks an instruction address, so a unique cause for every intermittent failure is not established. No new in-game acceptance is claimed.

The first fixture build had CS0649 because its stub bootstrap did not initialize patch availability. That fixture-only issue was corrected; the targeted 20-check suite was rerun without warnings. The production build had zero warnings/errors and was not changed by that correction.

[Current release notes](releases/cheatmenu-v0.1.36.md) · [Build/reproduce](BUILD.md)

## Historical CheatMenu 0.1.35 — 2026-10-12

Published DLL SHA-256: `F82C2D6A6434D317558721A4CB532AC36369A1EFEE492590AF04CE529388C8BD`.

| 检查 / Check | 结果 / Result | 范围 / Scope |
|---|---|---|
| Release build | 0 warnings, 0 errors | Actual local BepInEx/interop references; production DLL unchanged for publication |
| Offline regression | 1051 passed, 0 failed / 15 suites | Managed fixtures; not real-game acceptance |
| Compiled-DLL lifecycle | 17 passed, 0 failed | Real managed DLL with native dependencies; no running game |
| Compiled source | 97 PDB source documents verified | Frozen production source corresponds to the installed DLL |
| Affix regression | Original 0/6; fixed 10/0 | Actual production drawing and InputLease methods linked to fault fixtures |
| Installation | Version 0.1.35.0 and exact DLL hash verified | 32 old plugin files backed up; 17 other protected file hashes unchanged |
| Publication portability | 228 BossTargets + 53 ToolLimits + 17 lifecycle checks passed | Harmony loaded from a separate Unicode path supplied via GameDir; lifecycle harness accepts game directory argument; no production code change |
| Runtime | Pending | Affix rendering/Escape recovery, autonomous current-room Boss combat, later Boss phases, full fresh-PC setup |

0.1.34的最新实机日志确认词缀页使用的Unity滚动接口抛 `Method unstripping failed`。0.1.35改用有原生实现的裁剪接口并补齐故障恢复，但发布时没有重新启动游戏，不能称为已通过实机验收。Boss保留当前房间战斗启动候选，不保证所有Boss/阶段可用。发布不是完整功能完成声明。

The latest 0.1.34 runtime log confirms the affix scroll API throws `Method unstripping failed`. 0.1.35 replaces it with native clipping and drawing-failure recovery. The game was not relaunched for this release. Current-room Boss combat remains a candidate awaiting in-game confirmation; all Bosses/phases are not guaranteed. Publication is not a declaration of feature completeness.

[Current release notes](releases/cheatmenu-v0.1.35.md) · [Build/reproduce](BUILD.md)

## Historical v1.0.0-rc.1

Date: **2026-09-26**. Components: CheatMenu **0.1.8**, Art Tool **1.1.1**.

| Check / 检查 | Result / 结果 | Evidence scope / 范围 |
|---|---|---|
| CheatMenu Release build | Passed | .NET SDK 8.0.422, real installed BepInEx/interop references; no game binaries packaged |
| CheatMenu offline logic suite | **60 passed, 0 failed** | Numeric boundaries, item categories, language switching, slot policies and real temporary save-backup file operations |
| Art tool suite | **12 passed** | Export path protection, cache integrity, float textures, transparency, missing-game behavior and portable EXE integration |
| Portable isolation | Passed | Only EXE + one copied game bundle; Unicode/spaces in paths; unrelated current directory; Python and FMOD environment variables removed; system-only PATH |
| Real image fixture export | **6 images, 0 failures** | PNGs and thumbnails decoded/verified; gallery data matches manifest; source file SHA-256 unchanged |
| FMOD dependency regression | Failed before fix, passed after fix | Clearing the inherited FMOD path reproduced missing-DLL startup failure; image-only import path and excluded audio modules fix it |
| EXE archive inspection | Passed | GPL text and copyright embedded; no FMOD runtime module/native library entries (historical dependency license notices retained) |
| Art library browser check | Passed | Six-image fixture renders; Chinese/English switch works; author attribution and both exact profile links remain visible; license/copyright links present |
| BepInEx download helper | Passed | Downloaded pinned official archive; SHA-256 matched `F4CC496BD098A0DF4164B81E3737297707F13A47C2478DBA2F60EEFAB784817A` |

The art browser check used a local HTTP preview of files generated by the EXE.
The release still opens the same HTML locally via the CMD launcher. A browser
preview is not proof of every browser's behavior under `file://` URLs.

本次没有启动游戏。**CheatMenu 新署名区域、主页按钮的实机排版/点击、
所有原生游戏操作，以及在另一台玩家电脑上的首次安装仍未验证。**
因此首个公开发行标记为 **pre-release**，不声明游戏内完整验收通过。
未改变 CheatMenu 原有玩法机制。验证环境和独立玩家电脑的首次安装是不同范围。

The game was not launched for this release. New CheatMenu attribution/link UI,
native gameplay actions, and first installation on a different player's PC
remain unverified. Offline tests do not prove those behaviors. This initial
public package is a **prerelease**.

## Reproduce / 复现

See [build and test instructions](BUILD.en.md) / [构建与测试说明](BUILD.md).
Art integration checks require your own game through `ROGUE_TEST_GAME`; no game fixture is distributed.
Use `python scripts/package.py` from the repository root after both builds.
`release/SHA256SUMS.txt` identifies the resulting packages.

This is the verification record for the initial v1.0.0-rc.1 binaries. Later documentation changes do not count as new in-game verification.
本记录对应首次预发布的程序包；后续网页说明修改不代表新增实机验收。
