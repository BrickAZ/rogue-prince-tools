# Install the BepInEx prerequisite

[Home](../README.en.md) · [简体中文](README.md) · [CheatMenu setup](../docs/INSTALL.en.md#cheatmenu)

**Only CheatMenu needs this loader. The art tool does not.** BepInEx is developed by the **BepInEx team and contributors**. Our helper obtains the original loader archive from its official server.

| Required version | Required target |
|---|---|
| 6.0.0-be.788+5b766a3 | Unity.IL2CPP-win-x64 (not Mono or x86) |

## Recommended: run the download helper

1. [Download 01-BepInEx-Download-788.zip](https://github.com/BrickAZ/rogue-prince-tools/releases/download/v1.0.0-rc.1/01-BepInEx-Download-788.zip) and extract all of it to an ordinary folder.
2. Double-click **`下载BepInEx前置.cmd`** (download the prerequisite), keeping `Download-BepInEx.ps1` beside it. The helper downloads the official archive and checks SHA-256 automatically. It does not install anything or change the game.
3. After success, open **the `Downloads` folder beside that CMD**. This is the helper's own subfolder, not your Windows user Downloads folder.
4. Locate `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788+5b766a3.zip`. Install **this newly downloaded ZIP** in the next section.

```text
Folder where you extracted the helper/
├─ 下载BepInEx前置.cmd
├─ Download-BepInEx.ps1
└─ Downloads/
   └─ BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788+5b766a3.zip
```

**Extracting package 01 alone does not install BepInEx.** Do not run the CMD from inside the ZIP preview.

## Install the downloaded BepInEx archive

1. Close the game. In Steam, choose the game → Manage → Browse local files.
2. Extract **all contents inside the downloaded BepInEx ZIP** beside the game EXE, matching the layout below. Do not create another folder named after the archive.
3. Start the game once, allow initialization to finish, reach the main menu, then exit. First initialization may download dependencies and take longer than a normal launch. If it fails, see the [FAQ](../docs/FAQ.en.md#loader).
4. Check that `BepInEx/interop/Assembly-CSharp.dll` was generated, then follow [CheatMenu setup](../docs/INSTALL.en.md#cheatmenu).

```text
Game directory/
├─ The Rogue Prince of Persia.exe
├─ winhttp.dll
├─ doorstop_config.ini
├─ dotnet/
└─ BepInEx/
   ├─ core/
   └─ interop/                  ← generated on first initialization
      └─ Assembly-CSharp.dll
```

Skip this if the same version and target are already installed correctly. For other loader versions or installations, record the existing version, back up relevant configuration and check compatibility before replacing anything.

## If the helper cannot download: get the archive manually

[Download the pinned official ZIP](https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788%2B5b766a3.zip) · [Official build list](https://builds.bepinex.dev/projects/bepinex_be)

Check the filename, target and SHA-256, then install as above. In PowerShell, replace the path below with the downloaded file's location:

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath 'D:\Downloads\BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788+5b766a3.zip'
```

Expected SHA-256:

```text
F4CC496BD098A0DF4164B81E3737297707F13A47C2478DBA2F60EEFAB784817A
```

Do not install a file with a different checksum. The helper stops if a same-name file has a mismatched checksum; extract the helper into a new empty folder to retry.

<details>
<summary>Advanced: choose the download directory</summary>

Open PowerShell in the helper folder and run:

```powershell
.\Download-BepInEx.ps1 -OutputDirectory 'D:\Downloads'
```

This overrides the default Downloads folder beside the script. Double-clicking the CMD is sufficient for ordinary use.

</details>

## Upstream and license

[Upstream source](https://github.com/BepInEx/BepInEx/tree/5b766a3) · [LGPL-2.1 license](https://github.com/BepInEx/BepInEx/blob/5b766a3/LICENSE) · [Official IL2CPP installation guide](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_il2cpp.html)

The download helper is by **BrickZhou/青春啊砖在he边看月亮**, under this repository's GPLv3. BepInEx itself retains its upstream authors and license.
