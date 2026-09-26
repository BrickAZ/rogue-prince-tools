# Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
# SPDX-License-Identifier: GPL-3.0-only

"""Acceptance check: only the shipped EXE and a real game bundle are copied."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


ROOT = Path(__file__).resolve().parent
EXE = ROOT / 'dist' / '一键导出美术资源.exe'
DATA_NAME = 'The Rogue Prince of Persia_Data'
GAME = Path(os.environ.get('ROGUE_TEST_GAME', ''))


class PortableReleaseTests(unittest.TestCase):
    def test_double_click_outside_game_explains_placement_and_keeps_window_open(self):
        self.assertTrue(EXE.is_file(), 'The release must include a self-contained executable')
        with tempfile.TemporaryDirectory(prefix='没有游戏-') as tmp:
            executable = Path(tmp) / EXE.name
            shutil.copy2(EXE, executable)
            result = subprocess.run([str(executable)], cwd=tmp, input='\n',
                                    text=True, encoding='utf-8', errors='replace',
                                    capture_output=True, timeout=90)
            self.assertEqual(result.returncode, 1)
            self.assertIn('请将本工具放到游戏根目录', result.stdout)
            self.assertIn('按回车键关闭此窗口', result.stdout)
            self.assertFalse((Path(tmp) / 'ArtExports').exists())

    def test_only_exe_exports_beside_itself_from_an_unrelated_working_directory(self):
        self.assertTrue(EXE.is_file(), 'The release must include a self-contained executable')
        bundle = GAME / DATA_NAME / 'StreamingAssets/aa/StandaloneWindows64/afm_scenes_scenes_aqueducoverlay.bundle'
        self.assertTrue(bundle.is_file(), 'Real fixture bundle is required')
        with tempfile.TemporaryDirectory(prefix='单文件导出 验收-') as tmp:
            base = Path(tmp)
            game = base / '游戏目录 含空格'
            data = game / DATA_NAME / 'StreamingAssets/aa/StandaloneWindows64'
            data.mkdir(parents=True)
            shutil.copy2(bundle, data / bundle.name)
            executable = game / EXE.name
            shutil.copy2(EXE, executable)
            cwd = base / '其他工作目录'
            cwd.mkdir()
            env = os.environ.copy()
            for key in list(env):
                if key.upper().startswith('PYTHON') or 'FMOD' in key.upper():
                    del env[key]
            env['PATH'] = str(Path(env['SYSTEMROOT']) / 'System32')
            completed = subprocess.run([str(executable), '--no-open'], cwd=cwd, env=env,
                                       input='', text=True, encoding='utf-8', errors='replace',
                                       capture_output=True, timeout=180)
            self.assertEqual(completed.returncode, 0, completed.stdout + completed.stderr)
            output = game / 'ArtExports'
            report = json.loads((output / 'report.json').read_text(encoding='utf-8'))
            self.assertGreater(report['image_count'], 0)
            self.assertEqual(report['error_count'], 0)
            self.assertTrue(report['source_files_unchanged'])
            manifest = json.loads((output / 'manifest.json').read_text(encoding='utf-8'))
            for record in manifest['records']:
                self.assertTrue((output / record['file']).is_file())
                self.assertTrue((output / record['thumbnail']).is_file())
            self.assertTrue((output / 'index.html').is_file())
            self.assertFalse((cwd / 'ArtExports').exists())
            self.assertEqual(sorted(p.name for p in game.iterdir()),
                             sorted([EXE.name, DATA_NAME, 'ArtExports']))
            evidence = ROOT / 'validation'
            evidence.mkdir(exist_ok=True)
            (evidence / 'portable-acceptance.json').write_text(json.dumps({
                'exe_only': True, 'python_environment_cleared': True,
                'fmod_environment_cleared': True,
                'path_system32_only': True, 'unrelated_cwd': True,
                'unicode_and_spaces': True, 'report': report,
            }, ensure_ascii=False, indent=2), encoding='utf-8')


if __name__ == '__main__':
    unittest.main()
