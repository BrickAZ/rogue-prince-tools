# Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
# SPDX-License-Identifier: GPL-3.0-only

# -*- mode: python ; coding: utf-8 -*-
from PyInstaller.utils.hooks import collect_all
from pathlib import Path
import os

source_root = Path(SPECPATH)
datas = [(str(source_root / 'gallery_template.html'), '.'),
         (str(source_root / 'third-party-licenses'), 'third-party-licenses')]
datas += [(str(source_root.parent / 'LICENSE'), '.'),
          (str(source_root.parent / 'COPYRIGHT.md'), '.')]
binaries = []
hiddenimports = []
tmp_ret = collect_all('UnityPy')
datas += tmp_ret[0]; binaries += tmp_ret[1]; hiddenimports += tmp_ret[2]
tmp_ret = collect_all('texture2ddecoder')
datas += tmp_ret[0]; binaries += tmp_ret[1]; hiddenimports += tmp_ret[2]
tmp_ret = collect_all('archspec')
datas += tmp_ret[0]; binaries += tmp_ret[1]; hiddenimports += tmp_ret[2]
tmp_ret = collect_all('tpk_ar')
datas += tmp_ret[0]; binaries += tmp_ret[1]; hiddenimports += tmp_ret[2]
tmp_ret = collect_all('astc_encoder')
datas += tmp_ret[0]; binaries += tmp_ret[1]; hiddenimports += tmp_ret[2]


a = Analysis(
    [str(source_root / 'art_exporter.py')],
    pathex=[str(source_root), os.environ.get('ROGUE_BUILD_DEPS', str(source_root / '.build-deps'))],
    binaries=binaries,
    datas=datas,
    hiddenimports=hiddenimports,
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=['matplotlib', 'pandas', 'scipy', 'IPython', 'tkinter',
              'UnityPy.export.AudioClipConverter', 'fmod_toolkit', 'pyfmodex'],
    noarchive=False,
    optimize=0,
)
pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.datas,
    [],
    name='一键导出美术资源',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=False,
    console=True,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
)
