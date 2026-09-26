# Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
# SPDX-License-Identifier: GPL-3.0-only

"""Read-only Unity art export, with a searchable offline image gallery."""

import argparse
from collections import Counter
from datetime import datetime
import gc
import hashlib
import json
import logging
import math
import os
from pathlib import Path
import shutil
import sys
import struct
import time
import traceback
import webbrowser
from types import ModuleType

# UnityPy eagerly imports its audio exporter even for image-only operations.
# Keep this tool independent of FMOD and make unsupported audio use explicit.
def _audio_not_supported(*args, **kwargs):
    raise NotImplementedError('This image-only tool does not export audio.')

_audio_module = ModuleType('UnityPy.export.AudioClipConverter')
_audio_module.extract_audioclip_samples = _audio_not_supported
sys.modules.setdefault(_audio_module.__name__, _audio_module)

import UnityPy
from PIL import Image
from UnityPy.environment import simplify_name
from UnityPy.enums import TextureFormat
from UnityPy.export import Texture2DConverter

from art_core import MARKER, object_filename, reusable, safe_name, validate_output
from art_categories import collect_metadata, categorize

VERSION = '1.1.1'
AUTHOR = 'BrickZhou/青春啊砖在he边看月亮'
SCHEMA = 1
IMAGE_TYPES = {'Texture2D', 'Sprite', 'Texture2DArray'}
DATA_NAME = 'The Rogue Prince of Persia_Data'
LOG = logging.getLogger('art_exporter')


def decode_rgba_float(image_data, width, height):
    required = width * height * 16
    if len(image_data) < required:
        raise ValueError('RGBAFloat 图片数据不完整。')
    pixels = bytearray()
    for (value,) in struct.iter_unpack('<f', image_data[:required]):
        value = 0.0 if math.isnan(value) else min(1.0, max(0.0, value))
        pixels.append(round(value * 255))
    return Image.frombytes('RGBA', (width, height), bytes(pixels))


# Pillow does not accept UnityPy's RGBAF raw decoder. Keep original floats beside the PNG preview.
Texture2DConverter.CONV_TABLE[TextureFormat.RGBAFloat] = (decode_rgba_float, ())


def read_json(path, default=None):
    try:
        return json.loads(path.read_text(encoding='utf-8'))
    except (OSError, ValueError):
        return {} if default is None else default


def write_json(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_suffix(path.suffix + '.tmp')
    temp.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
    temp.replace(path)


def file_hash(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def key_hash(value, length=16):
    return hashlib.sha256(value.encode()).hexdigest()[:length]


def find_sources(data):
    sources = list(data.glob('*.assets'))
    sources += [p for p in data.glob('level*') if p.is_file() and p.name[5:].isdigit()]
    sources += list((data / 'StreamingAssets').rglob('*.bundle'))
    return sorted(sources, key=lambda p: str(p).casefold())


def source_snapshot(data, sources):
    paths = set(sources)
    paths.update(p for p in data.glob('*.resS') if p.is_file())
    paths.update(p for p in data.glob('*.resource') if p.is_file())
    return {p.relative_to(data).as_posix(): {'bytes': p.stat().st_size,
            'sha256': file_hash(p)} for p in sorted(paths)}


class ArtEnvironment(UnityPy.Environment):
    def __init__(self, source, cab_map, data):
        self.cab_map = cab_map
        self.data_root = data
        self.loading = set()
        super().__init__(str(source))

    def find_file(self, name, is_dependency=True):
        existing = self.get_cab(name)
        if existing is not None:
            return existing
        dependency = self.cab_map.get(simplify_name(name))
        if dependency and dependency not in self.loading:
            self.loading.add(dependency)
            self.load_file(str(self.data_root / dependency), is_dependency=True)
            existing = self.get_cab(name)
            if existing is not None:
                return existing
        return super().find_file(name, is_dependency=is_dependency)


def scan_sources(data, sources, snapshot, cache_dir):
    index_path = cache_dir / 'source-index.json'
    previous = read_json(index_path)
    entries, cab_map, errors = {}, {}, []
    for i, path in enumerate(sources, 1):
        relative = path.relative_to(data).as_posix()
        signature = snapshot[relative]['sha256'] + UnityPy.__version__
        entry = previous.get(relative, {})
        if entry.get('signature') != signature:
            try:
                env = UnityPy.load(str(path))
                counts = Counter(o.type.name for o in env.objects)
                entry = {'signature': signature, 'cabs': list(env.cabs), 'types': dict(counts),
                         'unity_versions': sorted({a.unity_version for a in env.assets})}
                del env
                gc.collect()
            except Exception as error:
                errors.append({'source': relative, 'stage': 'scan', 'error': str(error)})
                LOG.exception('无法读取资源包 %s', relative)
                continue
        entries[relative] = entry
        for cab in entry['cabs']:
            cab_map.setdefault(cab, relative)
        if i % 30 == 0 or i == len(sources):
            LOG.info('扫描资源包 %s/%s', i, len(sources))
    write_json(index_path, entries)
    return entries, cab_map, errors


def export_image(image, output, record, directory, filename):
    path = output / 'images' / directory / record['type'] / filename
    thumb = output / 'thumbnails' / directory / record['type'] / (Path(filename).stem + '.webp')
    path.parent.mkdir(parents=True, exist_ok=True)
    thumb.parent.mkdir(parents=True, exist_ok=True)
    # Keep UnityPy's orientation and decoded alpha; never resize the original PNG.
    if image.mode not in ('RGB', 'RGBA', 'L', 'LA', 'P', '1', 'I;16'):
        image = image.convert('RGBA')
    temp = path.with_suffix('.part')
    image.save(temp, format='PNG', compress_level=3)
    temp.replace(path)
    preview = image.convert('RGBA')
    preview.thumbnail((224, 224), Image.Resampling.LANCZOS)
    temp = thumb.with_suffix('.part')
    preview.save(temp, format='WEBP', quality=82, method=2)
    temp.replace(thumb)
    preview.close()
    record.update({'file': path.relative_to(output).as_posix(),
                   'thumbnail': thumb.relative_to(output).as_posix(),
                   'width': image.width, 'height': image.height, 'mode': image.mode,
                   'bytes': path.stat().st_size, 'thumbnail_bytes': thumb.stat().st_size})
    return record


def export_source(path, data, output, cab_map, source_hash):
    relative = path.relative_to(data).as_posix()
    directory = f'{safe_name(path.stem, 42)}_{key_hash(relative, 8)}_{source_hash[:8]}'
    records, errors, skipped = [], [], []
    env = ArtEnvironment(path, cab_map, data)
    # Snapshot objects before lazy dependency loading so dependency objects are not exported twice.
    objects = [o for o in env.objects if o.type.name in IMAGE_TYPES]
    for i, obj in enumerate(objects, 1):
        name = str(obj.path_id)
        try:
            item = obj.parse_as_object()
            name = item.m_Name or name
            asset_file = str(obj.assets_file.name)
            base = {'name': name, 'type': obj.type.name, 'source': relative,
                    'asset_file': asset_file, 'path_id': str(obj.path_id)}
            if obj.type.name in ('Texture2D', 'Texture2DArray') and (item.m_Width == 0 or item.m_Height == 0):
                skipped.append({**base, 'reason': '资源宽或高为 0，没有可导出的静态图片数据。'})
                continue
            if obj.type.name == 'Texture2DArray':
                images = enumerate(item.images)
            else:
                images = [(None, item.image)]
            for layer, image in images:
                try:
                    record = dict(base)
                    if layer is not None:
                        record['layer'] = layer
                        record['name'] = f'{name} [layer {layer}]'
                    filename = object_filename(name, asset_file, obj.path_id, layer)
                    if obj.type.name == 'Texture2D' and item.m_TextureFormat == TextureFormat.RGBAFloat:
                        raw = output / 'images' / directory / obj.type.name / (Path(filename).stem + '.rgba32f')
                        raw.parent.mkdir(parents=True, exist_ok=True)
                        raw.write_bytes(item.get_image_data())
                        record.update({'raw_file': raw.relative_to(output).as_posix(),
                                       'raw_bytes': raw.stat().st_size, 'texture_format': 'RGBAFloat',
                                       'png_note': '8-bit preview, values clamped to [0,1]; original little-endian RGBA float32 data includes any mipmaps in the .rgba32f sidecar.'})
                    records.append(export_image(image, output, record, directory, filename))
                finally:
                    image.close()
        except Exception as error:
            errors.append({'source': relative, 'name': name, 'type': obj.type.name,
                           'path_id': str(obj.path_id), 'stage': 'export', 'error': str(error)})
            LOG.warning('导出失败 %s / %s: %s', path.name, name, error)
        if i % 200 == 0:
            LOG.info('  %s：%s/%s 个图片对象', path.name, i, len(objects))
    del objects, env
    gc.collect()
    return records, errors, skipped


def create_gallery(output, records, report):
    template = Path(__file__).with_name('gallery_template.html').read_text(encoding='utf-8')
    data = json.dumps({'records': records, 'report': report}, ensure_ascii=False)
    data = data.replace('&', '\\u0026').replace('<', '\\u003c').replace('>', '\\u003e')
    (output / 'index.html').write_text(template.replace('__GALLERY_DATA__', data), encoding='utf-8')
    notices = Path(__file__).with_name('third-party-licenses')
    if notices.is_dir():
        shutil.copytree(notices, output / 'third-party-licenses', dirs_exist_ok=True)
    for name, target in [('LICENSE', 'TOOL-LICENSE.txt'), ('COPYRIGHT.md', 'TOOL-COPYRIGHT.md')]:
        source = Path(__file__).with_name(name)
        if not source.is_file():
            source = Path(__file__).resolve().parent.parent / name
        shutil.copy2(source, output / target)


def run(args):
    game = args.game.resolve()
    data = game / DATA_NAME
    if not data.is_dir():
        raise ValueError(f'请将本工具放到游戏根目录，与游戏 EXE 放在一起，再双击运行。\n找不到游戏资源目录：{data}')
    output = (args.output or game / 'ArtExports').resolve()
    validate_output(game, output)
    output.mkdir(parents=True, exist_ok=True)
    (output / MARKER).write_text('Rogue Prince Art Exporter\n', encoding='utf-8')
    cache_dir = output / '.cache'
    cache_dir.mkdir(exist_ok=True)
    lock = output / '.running.lock'
    # OS releases this byte lock after both ordinary exits and process crashes.
    lock_file = lock.open('a+b')
    try:
        import msvcrt
        lock_file.seek(0)
        if not lock_file.read(1):
            lock_file.write(b'0')
            lock_file.flush()
        lock_file.seek(0)
        msvcrt.locking(lock_file.fileno(), msvcrt.LK_NBLCK, 1)
    except OSError:
        lock_file.close()
        raise ValueError('这个导出目录已有任务在运行，请等待现有任务结束。')
    try:
        logging.basicConfig(level=logging.INFO, format='%(message)s', force=True,
                            handlers=[logging.StreamHandler(sys.stdout),
                                      logging.FileHandler(output / 'export.log', encoding='utf-8', mode='w')])
        started = time.monotonic()
        LOG.info('波斯王子：Rogue Prince 图片导出工具 %s', VERSION)
        LOG.info('输出目录：%s', output)
        LOG.info('正在检查资源文件…')
        sources = find_sources(data)
        if not sources:
            raise ValueError('没有找到可读取的 Unity 资源文件。')
        snapshot = source_snapshot(data, sources)
        snapshot_id = key_hash(json.dumps(snapshot, sort_keys=True), 64)
        signature = f'{VERSION}/{SCHEMA}/{UnityPy.__version__}/{snapshot_id}'
        entries, cab_map, errors = scan_sources(data, sources, snapshot, cache_dir)
        selected = [p for p in sources if p.relative_to(data).as_posix() in entries]
        if args.match:
            selected = [p for p in selected if args.match.casefold() in p.name.casefold()]
        if not selected:
            raise ValueError('没有匹配的资源包。')
        records, skipped, reused, processed = [], [], 0, 0
        for i, path in enumerate(selected, 1):
            relative = path.relative_to(data).as_posix()
            cache_path = cache_dir / f'{key_hash(relative)}.json'
            cached = read_json(cache_path)
            if not args.force and reusable(cached, signature, output):
                records.extend(cached['records'])
                skipped.extend(cached.get('skipped', []))
                reused += 1
                continue
            types = entries[relative]['types']
            if any(types.get(t, 0) for t in IMAGE_TYPES):
                LOG.info('[%s/%s] %s', i, len(selected), path.name)
                try:
                    exported, failures, omitted = export_source(path, data, output, cab_map, snapshot[relative]['sha256'])
                except Exception as error:
                    exported, failures = [], [{'source': relative, 'stage': 'load', 'error': str(error)}]
                    omitted = []
                    LOG.exception('资源包读取失败 %s', relative)
            else:
                exported, failures, omitted = [], [], []
            records.extend(exported)
            errors.extend(failures)
            skipped.extend(omitted)
            write_json(cache_path, {'complete': not failures, 'signature': signature,
                                   'records': exported, 'errors': failures, 'skipped': omitted})
            processed += 1
        LOG.info('正在整理图片内容分类…')
        category_warnings = collect_metadata(records, data, cab_map, signature,
                                            cache_dir / 'art-metadata.json', ArtEnvironment, LOG)
        classification = categorize(records)
        LOG.info('正在核对原始资源文件…')
        after = source_snapshot(data, find_sources(data))
        unchanged = after == snapshot
        if not unchanged:
            errors.append({'stage': 'source_changed', 'error': '运行期间原始资源发生变化，请在游戏更新结束后重跑。'})
        records.sort(key=lambda r: (r['type'] != 'Sprite', r['name'].casefold(), r['source']))
        report = {'tool_version': VERSION, 'parser_version': UnityPy.__version__,
                  'created': datetime.now().astimezone().isoformat(timespec='seconds'),
                  'source_root': str(data), 'source_count': len(sources),
                  'selected_sources': len(selected), 'processed_sources': processed, 'reused_sources': reused,
                  'image_count': len(records), 'counts': dict(Counter(r['type'] for r in records)),
                  'png_bytes': sum(r['bytes'] for r in records), 'error_count': len(errors),
                  'skipped_empty_images': len(skipped),
                  'classification': classification, 'classification_warning_count': len(category_warnings),
                  'source_files_unchanged': unchanged, 'seconds': round(time.monotonic()-started, 2),
                  'filter': args.match, 'unity_versions': sorted({v for e in entries.values() for v in e['unity_versions']})}
        write_json(output / 'manifest.json', {'report': report, 'records': records})
        write_json(output / 'errors.json', errors)
        write_json(output / 'classification-report.json', {**classification, 'warnings': category_warnings})
        write_json(output / 'skipped.json', skipped)
        write_json(output / 'source-snapshot.json', snapshot)
        write_json(output / 'report.json', report)
        create_gallery(output, records, report)
        LOG.info('完成：%s 张图片；%s 项失败；复用 %s 个资源包。', len(records), len(errors), reused)
        LOG.info('浏览图片：%s', output / 'index.html')
        if not args.no_open:
            webbrowser.open((output / 'index.html').as_uri())
        return 2 if errors else 0
    finally:
        lock_file.close()


def main():
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, 'reconfigure'):
            stream.reconfigure(encoding='utf-8', errors='replace')
    home = Path(sys.executable if getattr(sys, 'frozen', False) else __file__).resolve().parent
    default_game = home
    parser = argparse.ArgumentParser(description='导出波斯王子的贴图、精灵和图集，生成本地图片浏览页。')
    parser.add_argument('--version', action='version', version=f'%(prog)s {VERSION} | {AUTHOR} | GPL-3.0-only')
    parser.add_argument('--game', type=Path, default=default_game)
    parser.add_argument('--output', type=Path)
    parser.add_argument('--match', default='', help='仅导出包名包含此文本的资源；建议搭配独立 --output 目录')
    parser.add_argument('--force', action='store_true', help='重新导出，不复用已有结果')
    parser.add_argument('--no-open', action='store_true', help='结束后不自动打开浏览器')
    parser.add_argument('--interactive', action='store_true', help='结束后等待按回车键')
    args = parser.parse_args()
    print(f'波斯王子美术资源工具 {VERSION} | {AUTHOR} | GPLv3')
    code = 1
    try:
        code = run(args)
    except KeyboardInterrupt:
        print('\n已中断。再次启动会复用已经完成的资源包。')
        code = 130
    except ValueError as error:
        print(f'\n{error}')
        print('\n导出未完成。')
    except Exception:
        traceback.print_exc()
        print('\n导出未完成，请查看上方错误信息。')
    if args.interactive or (getattr(sys, 'frozen', False) and len(sys.argv) == 1):
        try:
            input('\n按回车键关闭此窗口…')
        except (EOFError, KeyboardInterrupt):
            pass
    return code


if __name__ == '__main__':
    raise SystemExit(main())
