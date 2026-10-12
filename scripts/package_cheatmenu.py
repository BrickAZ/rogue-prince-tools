# Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
"""Package CheatMenu from a clean source commit; game dependencies stay external."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parent.parent
REPOSITORY = 'https://github.com/BrickAZ/rogue-prince-tools'


def git(*args):
    return subprocess.check_output(['git', '-c', f'safe.directory={ROOT.as_posix()}', *args], cwd=ROOT).decode('utf-8').strip()


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dll', type=Path, default=ROOT/'cheatmenu/src/RoguePrince.CheatMenu/bin/Release/net6.0/RoguePrince.CheatMenu.dll')
    parser.add_argument('--output', type=Path)
    parser.add_argument('--expected-sha256', help='Optional exact binary hash required for this publication')
    args = parser.parse_args()
    version = ET.parse(ROOT/'cheatmenu/src/RoguePrince.CheatMenu/RoguePrince.CheatMenu.csproj').findtext('./PropertyGroup/Version')
    if not version or not re.fullmatch(r'\d+\.\d+\.\d+', version):
        raise ValueError('Invalid project version')
    plugin = (ROOT/'cheatmenu/src/RoguePrince.CheatMenu/Plugin.cs').read_text(encoding='utf-8-sig')
    if f'Version = "{version}"' not in plugin and f'Version="{version}"' not in plugin:
        raise ValueError('Plugin version differs from project version')
    if git('status', '--porcelain', '--untracked-files=normal'):
        raise ValueError('Commit release sources first; packaging requires a clean repository')
    dll = args.dll.absolute()
    if not dll.is_file():
        raise FileNotFoundError(dll)
    binary_hash = digest(dll)
    if args.expected_sha256 and binary_hash != args.expected_sha256.upper():
        raise ValueError('DLL differs from the approved publication hash')
    output = args.output or ROOT/'release'/f'cheatmenu-{version}'
    output.mkdir(parents=True, exist_ok=True)
    commit = git('rev-parse', 'HEAD')
    tag = f'cheatmenu-v{version}'
    source_url = f'{REPOSITORY}/tree/{commit}'
    source_zip = output/f'CheatMenu-{version}-source.zip'
    subprocess.run(['git', '-c', f'safe.directory={ROOT.as_posix()}', 'archive', '--format=zip', f'--output={source_zip.resolve()}', commit], cwd=ROOT, check=True)
    info = dict(version=version, assemblyVersion=version+'.0', dllSha256=binary_hash,
                sourceCommit=commit, sourceUrl=source_url, sourceTag=tag,
                target='Windows x64 / BepInEx 6 IL2CPP / net6.0', prerelease=True,
                runtimeVerified=False, validationRecord=f'{REPOSITORY}/blob/{commit}/docs/VALIDATION.md')
    prefix = 'BepInEx/plugins/RoguePrince.CheatMenu/'
    files = [(dll, 'RoguePrince.CheatMenu.dll'),
             (ROOT/'cheatmenu/README.md', 'README.zh-CN.en.md'),
             (ROOT/'cheatmenu/GAME-LINKING-EXCEPTION.txt', 'GAME-LINKING-EXCEPTION.txt')]
    files += [(ROOT/name, name) for name in ['LICENSE', 'COPYRIGHT.md', 'THIRD-PARTY-NOTICES.md']]
    archive = output/f'02-CheatMenu-{version}.zip'
    with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as z:
        for path, name in files:
            z.write(path, prefix+name)
        z.writestr(prefix+'BUILD-INFO.json', json.dumps(info, ensure_ascii=False, indent=2))
        z.writestr(prefix+'SOURCE.txt', f'Corresponding source: {source_url}\nRelease tag: {tag}\nSource archive: {source_zip.name}\nGPL-3.0-only with included limited game linking permission.\n')
    with zipfile.ZipFile(archive) as z:
        assert z.testzip() is None and len(z.namelist()) == 8
        assert hashlib.sha256(z.read(prefix+'RoguePrince.CheatMenu.dll')).hexdigest().upper() == binary_hash
    with zipfile.ZipFile(source_zip) as z:
        assert z.testzip() is None
        for path in git('ls-files', 'cheatmenu/src', 'cheatmenu/tests', 'cheatmenu/build.ps1', 'cheatmenu/global.json', 'scripts/package_cheatmenu.py').splitlines():
            assert z.read(path) == (ROOT/path).read_bytes(), path
        forbidden = re.compile(r'(^|/)(?:\.git|\.local|bin|obj|validation|userdata|interop|CheatMenuBackups|trials)(/|$)|\.(?:dll|exe|pdb|log|sav|bak)$', re.I)
        assert not any(forbidden.search(name) for name in z.namelist()), 'Source archive contains an unexpected local/binary path'
    sums = ''.join(f'{digest(path)}  {path.name}\n' for path in [archive, source_zip])
    (output/'SHA256SUMS.txt').write_text(sums, encoding='utf-8')
    print(json.dumps(dict(version=version, commit=commit, dllSha256=binary_hash, output=str(output), installFiles=8, sourceArchiveVerified=True, runtimeVerified=False), indent=2))
    print(sums, end='')


if __name__ == '__main__':
    main()
