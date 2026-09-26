# Player installation and usage

[Home and downloads](../README.en.md#choose-your-download) · [简体中文](INSTALL.md) · [FAQ](FAQ.en.md)

Reference environment: Windows x64 / game v1.1.0 / Steam Build 24299434. This is a prerelease; other environments and a fresh installation on another PC remain unverified.

<a id="cheatmenu"></a>
## CheatMenu: first installation

1. **Find the game and back up saves.** Close the game, then use Steam → Manage → Browse local files to locate `The Rogue Prince of Persia.exe`. Copy `userdata/saves` to a backup location outside the game directory. If no save exists yet, run the game normally to create one, then close it and back it up.
2. **Install the loader.** Follow the [BepInEx walkthrough](../prerequisites/README.en.md): extract the helper, run its CMD, find the new ZIP in the helper's own `Downloads` folder, then extract that original BepInEx archive. The helper ZIP itself is not the loader.
3. **Initialize the loader.** Start the game, wait for initialization, reach the main menu and exit. Check for `BepInEx/interop/Assembly-CSharp.dll`; if missing, resolve the [loader issue](FAQ.en.md#loader) first.
4. **Install the menu.** Download [02-CheatMenu-0.1.8.zip](https://github.com/BrickAZ/rogue-prince-tools/releases/download/v1.0.0-rc.1/02-CheatMenu-0.1.8.zip) and merge its `BepInEx` folder into the game directory. Do not nest it inside another folder named after the ZIP.
5. **Open the menu.** Enter a save and press **F9** to show / hide it. Select **设置/setting → 语言/language → English**. A laptop may require Fn + F9.

Main file layout (other game files omitted):

```text
Game directory/
├─ The Rogue Prince of Persia.exe
├─ winhttp.dll
├─ doorstop_config.ini
├─ dotnet/
├─ userdata/
│  └─ saves/
└─ BepInEx/
   ├─ core/
   ├─ interop/
   │  └─ Assembly-CSharp.dll
   └─ plugins/
      └─ RoguePrince.CheatMenu/
         └─ RoguePrince.CheatMenu.dll
```

### Success checks

- Loader: interop files have been generated. Inspect `BepInEx/LogOutput.log` for initialization errors.
- Plugin: the log contains `[RPCheat] plugin_loaded version=0.1.8`. This proves loading, not that every native action works.
- Interface: F9 displays the menu after entering a save. Some actions require the player and level to be ready.

### Before changing anything

Original item levels are **1–5**. The menu accepts higher levels, but native behavior is not guaranteed; level 0 is rejected. Persistent resource and medallion slot changes may be saved by the game. Disabling the plugin does not undo saved changes.

Selected permanent actions back up disk saves to `BepInEx/CheatMenuBackups`. They cannot capture progress not yet written to disk and do not replace the full backup in step 1.

<a id="update"></a>
## CheatMenu: update, disable or remove

**Update:** Close the game. Back up saves and the current plugin folder, then merge the new package into the game directory. Keep only one enabled `RoguePrince.CheatMenu.dll`; move old copies outside the game directory. A matching BepInEx installation does not need reinstalling. Current settings are in `BepInEx/config/local.rogueprince.cheatmenu.cfg` and can be preserved during ordinary updates.

**Disable temporarily:** Close the game and rename the plugin's `RoguePrince.CheatMenu.dll` to `RoguePrince.CheatMenu.dll.disabled`. Restore the original filename to enable it.

**Remove the menu:** Close the game and move `BepInEx/plugins/RoguePrince.CheatMenu` outside the game directory. Keep any backups you need. Other mods may depend on BepInEx; do not remove the whole loader just to remove this menu.

<a id="art-tool"></a>
## Art tool: install, export and browse

1. Download [03-ArtTool-1.1.1.zip](https://github.com/BrickAZ/rogue-prince-tools/releases/download/v1.0.0-rc.1/03-ArtTool-1.1.1.zip) and extract all contents beside the game EXE. No BepInEx or Python installation is needed.
2. Run `一键导出美术资源.exe` (export images) and let it finish. It reads resources from your own game installation and writes results to `ArtExports`.
3. Your browser opens `ArtExports/index.html` when finished. If it does not open automatically, run `打开美术资源库.cmd` (open the library).
4. Switch languages, categories and filters in the gallery; click an image for its full-resolution PNG. Use the CMD later to view existing results without exporting again.

```text
Game directory/
├─ The Rogue Prince of Persia.exe
├─ 一键导出美术资源.exe
├─ 打开美术资源库.cmd
├─ ArtTool-Licenses/
└─ ArtExports/                 ← generated during export
   ├─ index.html
   ├─ images/
   ├─ report.json
   └─ errors.json
```

**Success checks:** The exporter finishes with a report; `ArtExports/index.html` exists and the gallery opens. Check `report.json` and `errors.json` for failures. An existing gallery does not prove every resource exported successfully.

**Update:** Close the running exporter and extract the new package into the same location. Keep existing exports you need. With a custom output directory, open its `index.html` directly; the CMD always opens `ArtExports/index.html` beside itself. [Advanced usage](../art-tool/README.en.md)

**Remove:** Close the exporter and move the EXE, CMD and `ArtTool-Licenses` out of the game directory. You can keep `ArtExports` separately.

Game artwork belongs to its respective rights holders. The tool's GPL license does not grant permission to redistribute game resources.
