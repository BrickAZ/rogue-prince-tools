# FAQ and reporting problems

[Home](../README.en.md) · [简体中文](FAQ.md) · [Full installation](INSTALL.en.md)

<a id="download"></a>
## Which package do I need? Why is there no EXE in the source archive?

Use the [download selector](../README.en.md#choose-your-download). ArtTool alone is enough for artwork; a first CheatMenu installation also needs BepInEx. The release's Source code archives and Code → Download ZIP contain source files rather than ready-to-use programs.

<a id="loader"></a>
## The prerequisite download fails, or interop files are missing

- Download failure: fully extract the helper, keep its CMD and PS1 together, and check connectivity. Alternatively use the [pinned official download](../prerequisites/README.en.md#if-the-helper-cannot-download-get-the-archive-manually) and verify its checksum.
- Missing downloaded ZIP: by default it is in **Downloads beside the helper**, unless you selected another output directory in PowerShell.
- Checksum mismatch: do not install that file. The helper will not overwrite a same-name mismatched archive; retry in a new empty folder.
- Missing `BepInEx/interop/Assembly-CSharp.dll`: confirm you installed the original downloaded BepInEx archive, selected IL2CPP win-x64, and put `winhttp.dll` beside the game EXE. Launch and complete first initialization before checking again.
- Initialization error: retain `BepInEx/LogOutput.log`, record network / dependency-download errors and the game version. Check compatibility with any existing loader first.

<a id="f9"></a>
## F9 does nothing

1. Enter a save and focus the game window. On a laptop, try Fn + F9.
2. Check the [folder layout](INSTALL.en.md#cheatmenu): no extra nesting or duplicate enabled plugin copies.
3. Search `BepInEx/LogOutput.log` for `[RPCheat] plugin_loaded`. If absent, investigate the loader, file placement or logged plugin-loading errors first.
4. If you changed the hotkey, close the game and inspect `ToggleKey` in `BepInEx/config/local.rogueprince.cheatmenu.cfg`. The default is F9; F1–F12 are supported, except F8, which is reserved for the existing HUD.
5. If loading succeeds but the menu still does not appear, report the game version, log and reproduction steps below.

Disabled buttons may mean that the player, level or required native component is not ready; they do not by themselves mean the plugin failed to load.

<a id="saves"></a>
## Does disabling the menu undo changes? Is automatic backup enough?

It does not undo permanent changes already saved. Before use, close the game and copy `userdata/saves` outside the game directory. Selected permanent actions back up disk saves to `BepInEx/CheatMenuBackups`; these only capture what was already on disk. Extracting the installation ZIP does not make a backup.

<a id="levels"></a>
## Why are item levels 1–5 recommended?

These are the original game's levels. The menu accepts 1–100, but native behavior above 5 is not guaranteed; level 0 does not exist. Some items do not use levels. Accepting an input does not mean every level was verified.

<a id="art"></a>
## The gallery launcher says there are no results, or the browser does not open

Run `一键导出美术资源.exe` first. The CMD only opens an existing gallery; it does not export. The default file is `ArtExports/index.html` beside the CMD, and you can open that HTML directly.

If you selected another directory with `--output`, open its `index.html`. Moving only the HTML breaks relative image paths; keep the whole output directory together.

<a id="export"></a>
## Game not found, export errors, missing images or inaccurate categories

- Put the EXE in the game root, or select it with the [--game option](../art-tool/README.en.md).
- Retain the console error. If output exists, inspect `ArtExports/report.json` and `ArtExports/errors.json`.
- Clear search and reset categories / filters to All to rule out hidden results.
- Categories are inferred from names, paths and metadata; some images remain Other / Unclassified. Extracted textures do not include in-game lighting or material compositing.
- Use `--force` when you need to re-export. A custom output directory must be empty or created by this tool. Choose a suitable location if path protection rejects a destination; do not bypass the protection.
- Other game versions' resource layouts remain unverified. Report the actual version and error; game resource bundles are not needed.

<a id="report"></a>
## How do I report a problem?

[Create an issue](https://github.com/BrickAZ/rogue-prince-tools/issues/new?template=bug_report.md) in English or Chinese. Include:

- Component and version, Windows version, game version / Steam Build.
- Steps leading to the issue, expected behavior and actual behavior.
- CheatMenu: BepInEx version, relevant `LogOutput.log` excerpt, and whether the plugin-loading entry appears.
- Art tool: console error and relevant `report.json` / `errors.json` contents; add a UI screenshot if useful.

Check logs for personal usernames and paths before posting. Saves, game DLLs and complete art resources are not required. This remains a prerelease; see the [validation record](VALIDATION.md) for verified and unverified scope.
