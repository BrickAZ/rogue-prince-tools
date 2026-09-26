# Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
# SPDX-License-Identifier: GPL-3.0-only

"""Filesystem and cache rules for the image exporter (no Unity dependencies)."""

import hashlib
import re
from pathlib import Path

MARKER = '.art-export-output'
RESERVED = {'CON', 'PRN', 'AUX', 'NUL', *(f'COM{i}' for i in range(1, 10)),
            *(f'LPT{i}' for i in range(1, 10))}


def safe_name(value, limit=72):
    value = re.sub(r'[\x00-\x1f<>:"/\\|?*]', '_', str(value)).strip(' .')
    value = value[:limit].rstrip(' .') or 'unnamed'
    if value.split('.')[0].upper() in RESERVED:
        value = '_' + value
    return value[:limit].rstrip(' .')


def object_filename(name, asset_file, path_id, layer=None):
    identity = f'{asset_file}\0{path_id}\0{layer}'
    suffix = hashlib.sha256(identity.encode()).hexdigest()[:12]
    layer_name = '' if layer is None else f'_layer{layer:03d}'
    return f'{safe_name(name, 60)}{layer_name}_{suffix}.png'


def validate_output(game, output):
    game, output = Path(game).resolve(), Path(output).resolve()
    allowed = game / 'ArtExports'
    if output == game or game.is_relative_to(output):
        raise ValueError('导出目录不能是游戏目录本身或其上级目录。')
    if output.is_relative_to(game) and not output.is_relative_to(allowed):
        raise ValueError('游戏目录内只能导出到 ArtExports 文件夹。')
    if output.exists() and (not output.is_dir() or
                            (any(output.iterdir()) and not (output / MARKER).is_file())):
        raise ValueError('指定目录已有其他文件。请选择空目录，或本工具以前创建的导出目录。')


def reusable(cache, signature, output):
    if not cache.get('complete') or cache.get('signature') != signature:
        return False
    output = Path(output).resolve()
    try:
        for record in cache['records']:
            files = [('file', 'bytes'), ('thumbnail', 'thumbnail_bytes')]
            if 'raw_file' in record:
                files.append(('raw_file', 'raw_bytes'))
            for key, size in files:
                path = (output / record[key]).resolve()
                if not path.is_relative_to(output) or not path.is_file() or path.stat().st_size != record[size]:
                    return False
    except (KeyError, TypeError, OSError):
        return False
    return True
