# Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
# SPDX-License-Identifier: GPL-3.0-only
"""Package only explicit release inputs; never include game data or local logs."""
import hashlib
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parent.parent
OUTPUT = ROOT / 'release'
TAG = 'v1.0.0-rc.1'
SOURCE = f'https://github.com/BrickAZ/rogue-prince-tools/tree/{TAG}'
AUTHOR = 'BrickZhou/青春啊砖在he边看月亮'


def write_archive(name, files, texts):
    with zipfile.ZipFile(OUTPUT / name, 'w', zipfile.ZIP_DEFLATED) as archive:
        for source, destination in files:
            if not source.is_file():
                raise FileNotFoundError(source)
            archive.write(source, destination)
        for destination, text in texts.items():
            archive.writestr(destination, text.encode('utf-8-sig'))


def main():
    OUTPUT.mkdir(exist_ok=True)
    source_notice = f'Author: {AUTHOR}\nCorresponding source / 对应源码: {SOURCE}\nLicense: GPL-3.0-only (CheatMenu has a limited game linking permission).\n'
    common = ['LICENSE', 'COPYRIGHT.md', 'THIRD-PARTY-NOTICES.md']
    prerequisite_files = [(p, p.name) for p in (ROOT / 'prerequisites').iterdir() if p.is_file()]
    prerequisite_files += [(ROOT / p, p) for p in common]
    write_archive('01-BepInEx-Download-788.zip', prerequisite_files, {'SOURCE.txt': source_notice})

    plugin = 'BepInEx/plugins/RoguePrince.CheatMenu/'
    cheat_files = [(ROOT / 'cheatmenu/src/RoguePrince.CheatMenu/bin/Release/net6.0/RoguePrince.CheatMenu.dll', plugin + 'RoguePrince.CheatMenu.dll')]
    cheat_files += [(ROOT / p, plugin + p) for p in common]
    cheat_files += [(ROOT / 'cheatmenu/GAME-LINKING-EXCEPTION.txt', plugin + 'GAME-LINKING-EXCEPTION.txt')]
    cheat_install = f'''CheatMenu 0.1.8 — {AUTHOR}

关闭游戏。先按 01 前置包中的说明下载、安装 BepInEx IL2CPP x64 be.788，
启动一次游戏完成初始化后退出。把此 ZIP 中的 BepInEx 文件夹放入游戏根目录。
进入存档，按 F9。设置/setting → 语言/language 可切换中文或 English。
署名、B站和 YouTube 按钮位于设置页底部。

Close the game. Install BepInEx IL2CPP x64 be.788 and launch once to initialize.
Extract this archive's BepInEx folder into the game root. Enter a save and press F9.
Settings includes language options, attribution and author links.

安装 ZIP 不会自动备份存档。使用前备份 userdata/saves。
永久修改可能被游戏保存；停用模组不会撤销它们。
Back up userdata/saves before use. Extracting this ZIP does not back up saves.
Persistent changes may be saved by the game; disabling the mod will not undo them.
停用 / Disable: close the game and rename the plugin DLL to .dll.disabled.

Windows x64 / Steam Build 24299434 / game v1.1.0.
New attribution/link UI has not yet been verified in-game. This is a prerelease.
完整说明 / Full instructions: {SOURCE}
'''
    write_archive('02-CheatMenu-0.1.8.zip', cheat_files,
                  {'02-CheatMenu-README.txt': cheat_install, plugin + 'SOURCE.txt': source_notice})

    art_files = [(ROOT / 'art-tool/dist/一键导出美术资源.exe', '一键导出美术资源.exe'),
                 (ROOT / 'art-tool/打开美术资源库.cmd', '打开美术资源库.cmd')]
    art_files += [(ROOT / p, 'ArtTool-Licenses/' + p) for p in common]
    for p in (ROOT / 'art-tool/third-party-licenses').rglob('*'):
        if p.is_file():
            art_files.append((p, 'ArtTool-Licenses/third-party-licenses/' + p.relative_to(ROOT / 'art-tool/third-party-licenses').as_posix()))
    art_install = f'''Art Tool 1.1.1 — {AUTHOR}

把本 ZIP 全部内容解压到游戏根目录，与 The Rogue Prince of Persia.exe 放在一起。
双击 一键导出美术资源.exe；结果写到 ArtExports，完成后自动打开图片库。
以后双击 打开美术资源库.cmd 查看已有结果。两个入口属于同一个工具。
无需安装 Python、Codex 或 BepInEx。源游戏文件只读。

Extract beside the game EXE. Run 一键导出美术资源.exe to export.
Later, run 打开美术资源库.cmd to reopen the existing offline gallery.
No Python, Codex or BepInEx is required. Game resources are read-only.
This is an image exporter, not an audio/model/animation exporter.

工具采用 GPLv3；导出的游戏图片仍归原权利人所有。
The tool is GPLv3; exported artwork retains its original ownership.
源码与说明 / Source and instructions: {SOURCE}
'''
    write_archive('03-ArtTool-1.1.1.zip', art_files,
                  {'03-ArtTool-README.txt': art_install, 'ArtTool-Licenses/SOURCE.txt': source_notice})
    hashes = []
    for p in sorted(OUTPUT.glob('0[123]-*.zip')):
        hashes.append(f'{hashlib.sha256(p.read_bytes()).hexdigest()}  {p.name}')
    (OUTPUT / 'SHA256SUMS.txt').write_text('\n'.join(hashes) + '\n', encoding='utf-8')
    print('\n'.join(hashes))


if __name__ == '__main__':
    main()
