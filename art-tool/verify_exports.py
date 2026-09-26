# Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
# SPDX-License-Identifier: GPL-3.0-only

"""Verify generated files without launching a browser or modifying game data."""
import json
import re
from pathlib import Path
import sys

from PIL import Image


def verify(root):
    manifest = json.loads((root / 'manifest.json').read_text(encoding='utf-8'))
    report = manifest['report']
    records = manifest['records']
    assert len(records) == report['image_count']
    assert report['error_count'] == 0
    assert report['source_files_unchanged']
    assert json.loads((root / 'errors.json').read_text(encoding='utf-8')) == []
    assert len({r['file'].casefold() for r in records}) == len(records)
    for index, record in enumerate(records, 1):
        for key, byte_key, expected in [('file', 'bytes', 'PNG'), ('thumbnail', 'thumbnail_bytes', 'WEBP')]:
            path = root / record[key]
            assert path.resolve().is_relative_to(root.resolve()), path
            assert path.stat().st_size == record[byte_key], path
            with Image.open(path) as image:
                assert image.format == expected, path
                if key == 'file':
                    assert image.size == (record['width'], record['height']), path
                    assert image.mode == record['mode'], path
                else:
                    assert max(image.size) <= 224, path
                image.verify()
        if 'raw_file' in record:
            assert (root / record['raw_file']).stat().st_size == record['raw_bytes']
        if index % 5000 == 0:
            print(f'Verified {index}/{len(records)}', flush=True)
    html = (root / 'index.html').read_text(encoding='utf-8')
    marker = '<script id="data" type="application/json">'
    data = html.split(marker, 1)[1].split('</script>', 1)[0]
    assert json.loads(data) == manifest
    assert '__GALLERY_DATA__' not in html
    # Explicit author hyperlinks are allowed; gallery assets remain offline.
    assert not re.search(r'(?:src|srcset)\s*=\s*[\"\'](?:https?:)?//', html, re.I)
    assert 'fetch(' not in html and 'XMLHttpRequest' not in html
    script = html.split('</script><script>', 1)[1].split('</script>', 1)[0]
    validation = Path(__file__).parent / 'validation'
    validation.mkdir(exist_ok=True)
    (validation / 'gallery-script.js').write_text(script, encoding='utf-8')
    result = {'png_verified': len(records), 'thumbnails_verified': len(records),
              'distinct_output_paths': len(records), 'gallery_data_matches_manifest': True,
              'source_files_unchanged': True, 'browser_visual_check': 'not_run_by_this_script'}
    (validation / 'file-validation.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    verify(Path(sys.argv[1]).resolve())
