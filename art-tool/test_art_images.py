# Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
# SPDX-License-Identifier: GPL-3.0-only

import struct
import tempfile
import unittest
from pathlib import Path

from PIL import Image
from art_exporter import decode_rgba_float, export_image


class ImageTests(unittest.TestCase):
    def test_float_texture_preview_clamps_and_keeps_alpha(self):
        raw = struct.pack('<8f', -1.0, 0.5, 2.0, 0.25, 1.0, 0.0, 0.0, 1.0)
        image = decode_rgba_float(raw, 2, 1)
        self.assertEqual(image.size, (2, 1))
        self.assertEqual([image.getpixel((x, 0)) for x in range(2)], [(0, 128, 255, 64), (255, 0, 0, 255)])

    def test_truncated_float_data_is_an_error(self):
        with self.assertRaises(ValueError):
            decode_rgba_float(b'bad', 2, 2)

    def test_png_export_keeps_full_resolution_and_transparency(self):
        image = Image.new('RGBA', (300, 2), (10, 20, 30, 0))
        image.putpixel((299, 1), (90, 80, 70, 127))
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            record = export_image(image, root, {'type': 'Sprite'}, 'sample', 'image.png')
            with Image.open(root / record['file']) as result:
                self.assertEqual(result.size, (300, 2))
                self.assertEqual(result.tobytes(), image.tobytes())
            with Image.open(root / record['thumbnail']) as thumbnail:
                self.assertLessEqual(max(thumbnail.size), 224)


if __name__ == '__main__':
    unittest.main()
