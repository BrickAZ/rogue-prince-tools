# Third-party software / 第三方软件

BrickZhou's attribution applies only to the original tool code and packaging
scripts. Existing third-party notices and ownership remain unchanged.

Original tool code: [GNU GPLv3](LICENSE), copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮.
This software comes WITHOUT ANY WARRANTY; redistribution and modification are
permitted under the license. See [COPYRIGHT.md](COPYRIGHT.md) for details.

## BepInEx (external prerequisite)

[BepInEx](https://github.com/BepInEx/BepInEx), BepInEx team and contributors.
The pinned revision `5b766a3` supplies LGPL version 2.1 license text.
Download the complete upstream distribution from its [official build page](https://builds.bepinex.dev/projects/bepinex_be).
Its additional libraries retain their respective upstream licenses.
This repository ships a download helper, not a rebranded copy of BepInEx.

## Art tool

The portable build embeds Python and third-party libraries. Versions are
pinned in `art-tool/requirements-lock.txt`; original license texts and notices
are preserved in `art-tool/third-party-licenses/` and included with the release.

Notable projects: [Python](https://www.python.org/),
[UnityPy](https://github.com/K0lb3/UnityPy),
[Pillow](https://github.com/python-pillow/Pillow),
[NumPy](https://github.com/numpy/numpy),
[PyInstaller](https://github.com/pyinstaller/pyinstaller) (GPL with its bundling exception).
UnityPy's optional audio export path and its FMOD wrapper/native dependencies
are **not included** in the portable binary because audio export is outside
this tool's scope. The dependency lock records UnityPy's upstream installation
dependencies, including packages excluded from the image-only release.

The full license texts govern each dependency; this summary does not relicense
them. Exported game images and generated game assemblies are not part of the
tool's licensed source and are not distributed in this repository or releases.
