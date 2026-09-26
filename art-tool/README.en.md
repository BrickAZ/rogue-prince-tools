# Art Tool 1.1.1

[Home and downloads](../README.en.md#choose-your-download) · [简体中文](README.md) · [Installation](../docs/INSTALL.en.md#art-tool) · [FAQ](../docs/FAQ.en.md#art)

By **BrickZhou/青春啊砖在he边看月亮** · GPL-3.0-only

`一键导出美术资源.exe` exports images; `打开美术资源库.cmd` opens existing results. The ready-to-use EXE needs no separate Python or BepInEx installation.

## Command-line usage

Open PowerShell in the EXE's directory:

```powershell
& '.\一键导出美术资源.exe' --version
& '.\一键导出美术资源.exe' --no-open
& '.\一键导出美术资源.exe' --game 'D:\Game' --output 'D:\Images'
```

| Option | Purpose |
|---|---|
| `--game PATH` | Select the game directory |
| `--output PATH` | Select output; defaults to ArtExports in the game directory |
| `--no-open` | Do not open the browser after export |
| `--force` | Force a fresh export |
| `--match TEXT` | Filter resource bundles; use a separate output directory for filtered results |
| `--interactive` | Wait for Enter when finished |
| `--version` | Show version and attribution |

Double-clicking the EXE keeps its window open at the end; press Enter to exit. The gallery CMD does not export and always opens `ArtExports/index.html` beside itself. Open the HTML directly when using a custom output directory.

## Output and scope

- A custom output directory must be empty or created by this tool. Inside the game directory, only `ArtExports` is allowed.
- Image types include Sprite, Texture2D and Texture2DArray. Float textures retain raw data alongside 8-bit PNG previews.
- Source resources are read-only, with hashes checked before and after export. See `report.json` for the report and `errors.json` for failures.
- Audio, models, skeletal animations and complete Unity projects are not exported. Images do not include in-game lighting or material compositing.
- Game artwork retains its original ownership; the tool's GPL does not cover game resources.

## Development and licenses

[Build and test instructions](../docs/BUILD.en.md#art-tool) · [Validation record](../docs/VALIDATION.md)

Third-party notices ship with the program and are exported under `ArtExports/third-party-licenses`. Tool notices are `TOOL-LICENSE.txt` and `TOOL-COPYRIGHT.md`. The unused FMOD native audio library is excluded from the release.
