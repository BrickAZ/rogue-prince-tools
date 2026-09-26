# Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
# SPDX-License-Identifier: GPL-3.0-only

import tempfile
import unittest
from pathlib import Path

from art_core import object_filename, reusable, safe_name, validate_output


class ExportSafetyTests(unittest.TestCase):
    def test_asset_names_cannot_escape_or_use_windows_reserved_names(self):
        for value in ['../../escape', 'a/b\\c:*?"<>|', 'CON', 'nul.png', '...', '', 'x' * 400]:
            result = safe_name(value)
            self.assertTrue(result)
            self.assertLessEqual(len(result), 72)
            self.assertFalse(any(c in result for c in '/\\:*?"<>|'))
            self.assertNotIn(result.split('.')[0].upper(), ['CON', 'NUL'])
            self.assertFalse(result.endswith(('.', ' ')))

    def test_duplicate_names_from_distinct_unity_objects_stay_distinct(self):
        names = [object_filename('icon', 'cab-a', 5), object_filename('icon', 'cab-b', 5),
                 object_filename('icon', 'cab-a', 6), object_filename('icon', 'cab-a', 5, 0)]
        self.assertEqual(len(set(names)), 4)
        self.assertTrue(all(n.endswith('.png') for n in names))

    def test_game_and_runtime_folders_are_never_output_targets(self):
        with tempfile.TemporaryDirectory() as tmp:
            game = Path(tmp) / 'game'
            game.mkdir()
            for output in [game, game.parent, game/'BepInEx', game/'The Rogue Prince of Persia_Data'/'exports']:
                with self.assertRaises(ValueError):
                    validate_output(game, output)
            validate_output(game, game/'ArtExports')

    def test_unowned_nonempty_output_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp)
            output = base/'personal'
            output.mkdir()
            (output/'keep.txt').write_text('untouched')
            with self.assertRaises(ValueError):
                validate_output(base/'game', output)
            self.assertEqual((output/'keep.txt').read_text(), 'untouched')

    def test_cache_requires_both_original_png_and_thumbnail(self):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp)
            (base/'a.png').write_bytes(b'png')
            (base/'thumb.webp').write_bytes(b'webp')
            cache = {'complete': True, 'signature': 'abc', 'records': [
                {'file': 'a.png', 'bytes': 3, 'thumbnail': 'thumb.webp', 'thumbnail_bytes': 4}]}
            self.assertTrue(reusable(cache, 'abc', base))
            self.assertFalse(reusable(cache, 'changed', base))
            (base/'thumb.webp').unlink()
            self.assertFalse(reusable(cache, 'abc', base))

    def test_failed_or_truncated_exports_are_retried(self):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp)
            (base/'a.png').write_bytes(b'bad')
            cache = {'complete': True, 'signature': 'abc', 'records': [
                {'file': 'a.png', 'bytes': 100, 'thumbnail': 'thumb.webp', 'thumbnail_bytes': 4}]}
            self.assertFalse(reusable(cache, 'abc', base))
            self.assertFalse(reusable({'complete': False, 'signature': 'abc', 'records': []}, 'abc', base))

    def test_cache_cannot_reference_outside_output(self):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp)
            cache = {'complete': True, 'signature': 'abc', 'records': [
                {'file': '../secret.png', 'bytes': 3, 'thumbnail': '../secret.png', 'thumbnail_bytes': 3}]}
            self.assertFalse(reusable(cache, 'abc', base))


if __name__ == '__main__':
    unittest.main()
