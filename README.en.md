# BrickZhou · Rogue Prince Tools

Community tools for **The Rogue Prince of Persia**, by **BrickZhou/青春啊砖在he边看月亮**.

[Bilibili](https://space.bilibili.com/517390275) · [YouTube](https://www.youtube.com/@Brickzhou) · [简体中文](README.md)

This repository contains **two tools**: CheatMenu **0.1.8**, and the art exporter/offline library **1.1.1**. The export EXE and library launcher are two entry points for the same art tool. BepInEx is a separate third-party prerequisite.

## Install

Download packages from [Releases](https://github.com/BrickAZ/rogue-prince-tools/releases).

**CheatMenu:** Close the game. Obtain BepInEx **6.0.0-be.788+5b766a3, Unity.IL2CPP-win-x64** using [these instructions](prerequisites/README.md). Extract the archive contents next to the game EXE. Start the game once to initialize the loader, then exit. Extract `02-CheatMenu-0.1.8.zip` to the same directory. The plugin belongs at `BepInEx/plugins/RoguePrince.CheatMenu/RoguePrince.CheatMenu.dll`.

Enter a save and press **F9**. Choose **设置/setting → 语言/language → English**. Settings includes the author's name and links to Bilibili and YouTube; external pages open only when clicked. The menu supports player adjustments, resources, item spawning, medallion slots and Wind of the Gods. Original item levels are 1–5; higher levels are not guaranteed to work; level 0 is rejected.

Back up `userdata/saves` with the game closed. Extracting the ZIP does not create a backup. Persistent changes may be saved by the game and are not undone by disabling the plugin. Selected permanent actions create a backup of disk saves in `BepInEx/CheatMenuBackups`. To disable, close the game and rename the plugin DLL to `.dll.disabled`.

**Art tool:** Extract `03-ArtTool-1.1.1.zip` into the game root. Run `一键导出美术资源.exe` to export images. It includes its Python runtime and dependencies. No Python, Codex, BepInEx or development environment is required by players. Results are written to `ArtExports`; the gallery opens automatically. Later, use `打开美术资源库.cmd` to reopen it. The gallery supports Chinese/English, categories, search and pagination.

The tool exports Sprite, Texture2D and Texture2DArray images. It does not export audio, 3D models, skeletal animations or a complete Unity project. Game resources are read-only; hashes are checked before and after export. Exported artwork retains its original ownership.

## Build and verification

Reference environment: Windows x64, Steam Build 24299434, game v1.1.0, Unity 6000.0.64f1. Other versions/platforms are unverified.

Use .NET SDK 8.0.422 and run `cheatmenu/build.ps1 -GameDirectory 'D:\YourGame'` after BepInEx has generated the game's interop assemblies. Game binaries are not included. Pure logic tests can run separately with `dotnet run --project cheatmenu/tests/RoguePrince.CheatMenu.Tests -c Release`.

Art tool builds use Python 3.12 x64; see [art-tool/README.md](art-tool/README.md).
Read [validation details](docs/VALIDATION.md). The new menu attribution and link buttons have not been verified inside the game; the initial public release is marked as a prerelease.

## License

Original code: **GPL-3.0-only**, copyright (C) 2026 **BrickZhou/青春啊砖在he边看月亮**. Commercial redistribution is permitted under GPLv3. Preserve applicable notices and supply corresponding source as required. See [LICENSE](LICENSE) and [COPYRIGHT.md](COPYRIGHT.md).

CheatMenu includes a [narrow game linking permission](cheatmenu/GAME-LINKING-EXCEPTION.txt). It does not waive GPL obligations for CheatMenu modifications or grant rights to distribute game binaries/assets. Third-party dependencies retain their own licenses. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). This project is not affiliated with or endorsed by Ubisoft or Evil Empire.
