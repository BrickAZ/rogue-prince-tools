# 美术资源工具 / Art Tool 1.1.1

作者 / Author: **BrickZhou/青春啊砖在he边看月亮** · GPL-3.0-only

`一键导出美术资源.exe` 导出，`打开美术资源库.cmd` 打开已有结果；两个入口属于一个工具。
将两者放在游戏根目录，结果生成到 `ArtExports`。导出后自动打开本地浏览器。
The EXE exports images and the CMD opens the existing gallery. Put both in the game root.

```powershell
& '.\一键导出美术资源.exe' --version
& '.\一键导出美术资源.exe' --no-open
& '.\一键导出美术资源.exe' --game 'D:\Game' --output 'D:\Images'
```

`--force` 重新导出；`--match TEXT` 筛选资源包（建议使用单独输出目录）；`--interactive` 保留窗口。
自定义输出须为空或已由本工具创建；游戏目录内部只允许写 `ArtExports`。
命令行双击自动保留窗口，成功后回车退出。查看入口不会重新导出。

## Build

Python **3.12 x64**, Windows:

```powershell
cd art-tool
python -m pip install --target .build-deps -r requirements-lock.txt
.\build.ps1 -Python python
$env:PYTHONPATH = "$PWD\.build-deps"
$env:ROGUE_TEST_GAME = 'D:\YourGame'
python -m unittest discover -v
```

`dist/一键导出美术资源.exe` is self-contained. `build.ps1 -DependencyPath PATH`
can reuse a matching dependency directory. Build environment: Python 3.12.14.
Portable tests copy only the EXE and one real bundle into a temporary Unicode
path, clear Python environment variables and use an unrelated working directory.

Third-party license texts are bundled and written under `ArtExports/third-party-licenses`.
The GPL text and tool copyright are exported as `TOOL-LICENSE.txt` and `TOOL-COPYRIGHT.md`.
FMOD's proprietary native library is excluded: this tool does not export audio.
Game artwork is not relicensed under the tool's GPL license.
