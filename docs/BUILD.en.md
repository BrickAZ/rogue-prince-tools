# Build from source

[Home](../README.en.md) · [简体中文](BUILD.md) · [Player installation](INSTALL.en.md)

For developers modifying or building the source. Players can use the ready-to-use release packages.

## CheatMenu

Run from the repository root. Requirements: Windows, .NET SDK **8.0.422** (or a compatible 8.0 patch), and your own game installation after BepInEx IL2CPP x64 initialization. Reference environment: game v1.1.0, Steam Build 24299434, Unity 6000.0.64f1.

```powershell
.\cheatmenu\build.ps1 -GameDirectory 'D:\YourGame' -Dotnet 'dotnet'
```

The project targets net6.0; its build SDK and target framework are different versions. Game assemblies are referenced from the selected installation's BepInEx/interop directory and are not distributed here. Initial dependency restoration may need network access.

Pure logic tests do not require the game:

```powershell
dotnet run --project .\cheatmenu\tests\RoguePrince.CheatMenu.Tests -c Release
```

<a id="art-tool"></a>
## Art tool

Start at the repository root with Python **3.12 x64** (reference build: 3.12.14):

```powershell
cd art-tool
python -m pip install --target .build-deps -r requirements-lock.txt
.\build.ps1 -Python python
$env:PYTHONPATH = "$PWD\.build-deps"
$env:ROGUE_TEST_GAME = 'D:\YourGame'
python -m unittest discover -v
```

Output: `art-tool/dist/一键导出美术资源.exe`. Use `-DependencyPath PATH` with the build script to reuse a matching dependency directory.

Portable integration checks require resources from your own game, selected through `ROGUE_TEST_GAME`. They copy the EXE and one resource bundle to a temporary Unicode path, clear Python / FMOD environment variables, and use an unrelated working directory. Without the game fixture, integration checks are not complete verification.

## Packaging and validation record

After both builds, run from the repository root:

```powershell
python scripts/package.py
```

ZIP files and `SHA256SUMS.txt` are written to `release`. Version and source-tag values in the packaging script identify corresponding source; maintainers must update them before a new release. This documentation revision does not replace existing v1.0.0-rc.1 binaries or move their source tag.

Game assemblies, complete game resources and saves are excluded from the source repository. Documentation screenshots show actual tool UI; pictured game artwork retains its original rights. See the [release validation record](VALIDATION.md) for historical build and test results, not a guarantee about arbitrary local builds.
