# BrickZhou · Rogue Prince Tools

Two community tools for **The Rogue Prince of Persia**: the **CheatMenu in-game menu** and an **art exporter / offline image library**.

By **BrickZhou/青春啊砖在he边看月亮**

[简体中文](README.md) · [Bilibili](https://space.bilibili.com/517390275) · [YouTube](https://www.youtube.com/@Brickzhou)

> **Windows x64 · CheatMenu 0.1.36 prerelease.** Current source and install package are included. The Escape pause/input fix, current-room Boss combat and fresh installation on another PC await in-game acceptance. Back up saves first. [Validation scope](docs/VALIDATION.md)

## Choose your download

| What do you need? | Ready-to-use packages | Next step |
|---|---|---|
| First CheatMenu installation | [CheatMenu 0.1.36][cheat] + [BepInEx download helper][loader] | [Installation guide](docs/INSTALL.en.md#cheatmenu) |
| Update CheatMenu with a compatible loader already installed | [CheatMenu 0.1.36][cheat] | [Update or disable](docs/INSTALL.en.md#update) |
| Only export or browse artwork | [Art Tool 1.1.1][art] | [Art tool setup](docs/INSTALL.en.md#art-tool) |

**The art tool needs neither BepInEx nor a separate Python installation.** Its export EXE and gallery CMD are two entry points for one tool. BepInEx is developed by a third-party team and loads CheatMenu.

[All release files and notes][release] · [SHA-256 checksums][checksums] · [FAQ](docs/FAQ.en.md)

Players should use the packages above. GitHub's **Code → Download ZIP** and the release's **Source code** archives contain developer source files.

## What the tools do

### CheatMenu · Open with F9

- Player and resources: health/energy adjustments, god mode, infinite energy, medallion slots and resource controls.
- Items and affixes: item search/spawning, affix editing for equipped or newly spawned weapons, favorites and weapon presets.
- Enemies and Bosses: spawn and clear menu-owned enemies; current-room Boss combat startup awaits in-game acceptance.
- Levels and practice: next/specified-level entries, training/timers, limited current-run ability trials and player checkpoints.
- Debug tools: combat/damage statistics, scene reports/overlays, frame observation, tool-use and effect controls.
- Settings: Chinese/English, shortcuts, interface scale/position and author links.

0.1.36 addresses native Escape pause failures encountered after affix editing: preserve EventSystems, capture only input modules and block native pause reentry while the menu owns input. Failed release remains available for retry. Actual pause display and keyboard/mouse/controller recovery await in-game confirmation. See the [CheatMenu guide](cheatmenu/README.md) for scope and limits.

These are the current menu's feature entries; see the compatibility table for native-action verification status. Automatic backups only cover disk saves before selected actions and do not replace a full backup before use.

### Art tool · Export once, browse offline

- Run the EXE to export images and open the gallery in a browser.
- Browse content categories, search names, filter by asset type or source bundle, and switch between Chinese and English.
- Open full-resolution PNGs from thumbnails; reopen existing results with the gallery launcher.
- Supports Sprite, Texture2D and Texture2DArray images. It does not export audio, models, skeletal animations or a complete Unity project.

![English art library showing categories, search and image previews](docs/images/art-library.en.png)

*Actual Art Tool 1.1.1 interface with 6 images from one sample resource bundle; this is not the full game's image count. Game artwork in the screenshot belongs to its respective rights holders.*

## Quick start

**CheatMenu**

1. Close the game. In Steam, use **Manage → Browse local files** to find its directory. Back up `userdata/saves` inside it.
2. Follow the [prerequisite guide](prerequisites/README.en.md): run the helper, then extract the **original BepInEx archive it downloads**. Start the game, allow initialization to finish, reach the main menu, then exit.
3. Merge the CheatMenu ZIP's `BepInEx` folder into the game directory. Enter a save and press **F9**. Select **设置/setting → 语言/language → English** to switch languages.

[Folder layout, success checks, updates and disabling](docs/INSTALL.en.md#cheatmenu) · [F9 does nothing](docs/FAQ.en.md#f9)

**Art tool**

1. Extract the art package contents beside the game EXE.
2. Run `一键导出美术资源.exe` (export images). Results go to `ArtExports` and the gallery opens when finished.
3. Later, run `打开美术资源库.cmd` (open the library) to view existing results without exporting again.

[Detailed setup and usage](docs/INSTALL.en.md#art-tool) · [Command-line options](art-tool/README.en.md)

## Versions and compatibility

Components keep independent versions. This release updates CheatMenu; the art tool and loader helper retain their existing packages.

| Component | Current version / environment | Verification status |
|---|---|---|
| CheatMenu release | cheatmenu-v0.1.36 | Prerelease; art tool retains its original package |
| CheatMenu | 0.1.36 | Build, 1071 offline checks, 17 compiled-DLL lifecycle checks and local installation verified; in-game confirmation pending |
| Art tool | 1.1.1 | Portable export and sample-gallery browser checks passed |
| BepInEx | 6.0.0-be.788+5b766a3, Unity.IL2CPP-win-x64 | Pinned official download and checksum verified |
| Reference game environment | Windows x64, game v1.1.0, Steam Build 24299434 | Other platforms and game versions unverified |

First loader initialization may download dependencies. Fresh installation on another player's PC and direct `file://` gallery use across browsers have not been fully verified. [Full validation record](docs/VALIDATION.md)

## Help and development

[FAQ and reporting](docs/FAQ.en.md) · [Report an issue](https://github.com/BrickAZ/rogue-prince-tools/issues/new?template=bug_report.md) · [Build from source](docs/BUILD.en.md) · [Changelog](CHANGELOG.md)

## Author and license

**BrickZhou/青春啊砖在he边看月亮** · [Bilibili](https://space.bilibili.com/517390275) · [YouTube](https://www.youtube.com/@Brickzhou)

Original code uses **GPL-3.0-only**. Commercial redistribution is allowed; comply with corresponding-source and notice requirements. [LICENSE](LICENSE) · [Copyright](COPYRIGHT.md)

CheatMenu includes a [limited game linking permission](cheatmenu/GAME-LINKING-EXCEPTION.txt). BepInEx and other dependencies retain their authors and licenses; see [third-party notices](THIRD-PARTY-NOTICES.md). The tool license does not cover the game or exported artwork. This project is not affiliated with or endorsed by Ubisoft or Evil Empire.

[cheat]: https://github.com/BrickAZ/rogue-prince-tools/releases/download/cheatmenu-v0.1.36/02-CheatMenu-0.1.36.zip
[loader]: https://github.com/BrickAZ/rogue-prince-tools/releases/download/v1.0.0-rc.1/01-BepInEx-Download-788.zip
[art]: https://github.com/BrickAZ/rogue-prince-tools/releases/download/v1.0.0-rc.1/03-ArtTool-1.1.1.zip
[release]: https://github.com/BrickAZ/rogue-prince-tools/releases/tag/cheatmenu-v0.1.36
[checksums]: https://github.com/BrickAZ/rogue-prince-tools/releases/download/cheatmenu-v0.1.36/SHA256SUMS.txt
