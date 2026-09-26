# Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
# SPDX-License-Identifier: GPL-3.0-only

"""Content categories based on Unity atlas ownership and original asset paths."""

from collections import Counter, defaultdict
import gc
import json
import ntpath
import re

CLASSIFICATION_VERSION = 1
CATEGORIES = {
    'weapons': '武器', 'characters': '人物', 'medallions': '徽章',
    'challenges': '挑战 / 觉醒', 'talents': '天赋 / 成长',
    'enemies': '敌人 / Boss', 'items': '道具 / 掉落', 'maps': '地图',
    'story': '剧情 / 图鉴', 'environment': '场景 / 建筑', 'effects': '特效',
    'ui': '界面', 'controls': '按键 / 操作提示', 'fonts': '字体 / 标识',
    'other': '其他 / 待确认',
}

ATLAS_CATEGORIES = {
    'atlasui_medaillionsicons': ('medallions', '徽章图标'),
    'atlasui_medaillionspacks': ('medallions', '徽章包'),
    'atlasui_medaillionsassets': ('medallions', '徽章槽 / 配套界面'),
    'atlasui_skilltree_items': ('talents', '天赋图标'),
    'atlasui_skilltree': ('talents', '天赋树界面'),
    'atlasui_upgrades': ('talents', '永久升级 / 解锁'),
    'atlasui_awakeningstones': ('challenges', '觉醒词条'),
    'atlasui_awakeningoasis': ('challenges', '觉醒选择 / 难度界面'),
    'awakeningaltar': ('challenges', '觉醒祭坛 / 场景物件'),
    'arenachallengelooter': ('challenges', '挑战宝箱 / 场景物件'),
    'atlasui_skinsicons': ('characters', '皮肤图标'),
    'atlasui_skinshop': ('characters', '皮肤商店 / 配套界面'),
    'atlasui_mindmapcharaicons': ('characters', '人物关系图 / 头像'),
    'atlasui_archivistmobs': ('enemies', '敌人图鉴立绘'),
    'atlasui_archivistpp': ('enemies', '敌人图鉴头像'),
    'atlasui_eliteicons': ('enemies', '精英标记'),
    'atlasui_worldmapicons': ('maps', '世界地图 / 路线'),
    'atlasui_minimap': ('maps', '小地图标记'),
    'atlasui_persianmetro': ('maps', '地图路线符号'),
    'atlasui_mindmapnodesicons': ('story', '剧情线索'),
    'atlasui_archivist': ('story', '图鉴界面'),
    'atlasui_minorlooters': ('items', '消耗品 / 掉落图标'),
    'atlasui_currency': ('items', '货币 / 资源'),
    'atlasui_affecticons': ('effects', '状态图标'),
    'atlasui_hud': ('ui', 'HUD / 玩家状态栏'),
    'atlasui_common': ('ui', '通用边框 / 装饰'),
    'atlasui_windows': ('ui', '窗口 / 对话框'),
    'atlasui_creditsview': ('fonts', '制作组 / 标识'),
    'atlasui_forgecollecion': ('ui', '锻造 / 收藏界面'),
    'skilltreealtar': ('talents', '天赋祭坛 / 场景物件'),
}


def object_key(asset_file, path_id):
    return f'{ntpath.basename(str(asset_file)).lower()}:{path_id}'


def read_cache(path):
    try:
        return json.loads(path.read_text(encoding='utf-8'))
    except (OSError, ValueError):
        return {}


def collect_metadata(records, data, cab_map, signature, cache_path, environment_factory, log):
    """Read metadata only. Direct asset paths exclude indirect prefab preload membership."""
    by_source = defaultdict(list)
    for record in records:
        by_source[record['source']].append(record)
    cached = read_cache(cache_path)
    previous = cached.get('sources', {}) if cached.get('signature') == signature else {}
    sources, warnings = {}, []
    for index, (source, rows) in enumerate(sorted(by_source.items()), 1):
        if source in previous:
            sources[source] = previous[source]
            continue
        targets = {object_key(r['asset_file'], r['path_id']) for r in rows}
        entries = {key: {'atlases': [], 'original_paths': []} for key in targets}
        try:
            env = environment_factory(data / source, cab_map, data)
            objects = list(env.objects)
            for obj in objects:
                key = object_key(obj.assets_file.name, obj.path_id)
                if key not in targets:
                    continue
                if obj.type.name == 'Sprite':
                    item = obj.parse_as_object()
                    tags = set(item.m_AtlasTags or [])
                    if item.m_SpriteAtlas:
                        try:
                            tags.add(item.m_SpriteAtlas.deref().peek_name())
                        except Exception as error:
                            warnings.append({'source': source, 'object': key, 'error': str(error)})
                    entries[key]['atlases'] = sorted(tag for tag in tags if tag)
                elif obj.type.name == 'Texture2D':
                    # Unity embeds the atlas name in generated packed-texture names.
                    match = re.match(r'^sactx-\d+-\d+x\d+-.+?-(.+)-[a-f\d]{6,}$', obj.peek_name() or '', re.I)
                    if match:
                        entries[key]['atlases'] = [match[1]]
            for asset in env.assets:
                for original_path, ptr in asset.container.items():
                    target_file = asset.name
                    if ptr.m_FileID:
                        external_index = ptr.m_FileID - 1
                        if external_index >= len(asset.externals):
                            continue
                        target_file = asset.externals[external_index].path
                    key = object_key(target_file, ptr.m_PathID)
                    if key in targets:
                        entries[key]['original_paths'].append(original_path)
            for entry in entries.values():
                entry['original_paths'] = sorted(set(entry['original_paths']))
            sources[source] = entries
            del objects, env
            gc.collect()
        except Exception as error:
            warnings.append({'source': source, 'error': str(error)})
            log.warning('分类元数据读取失败 %s: %s', source, error)
        if index % 15 == 0 or index == len(by_source):
            log.info('读取图片分类依据 %s/%s', index, len(by_source))
    result = {'signature': signature, 'sources': sources, 'warnings': warnings}
    # Preserve warnings if this was a completely cached pass.
    if len(previous) == len(sources) and all(k in previous for k in sources):
        result['warnings'] = cached.get('warnings', [])
    temporary = cache_path.with_suffix('.tmp')
    temporary.write_text(json.dumps(result, ensure_ascii=False), encoding='utf-8')
    temporary.replace(cache_path)
    for record in records:
        key = object_key(record['asset_file'], record['path_id'])
        record.update(sources.get(record['source'], {}).get(key, {'atlases': [], 'original_paths': []}))
    return result['warnings']


def classify(record):
    name = record['name']
    low = name.casefold()
    source = record['source'].casefold()
    atlases = record.get('atlases', [])
    paths = record.get('original_paths', [])

    def result(category, subcategory, reason, confidence='high'):
        return {'category': category, 'subcategory': subcategory,
                'category_reason': reason, 'category_confidence': confidence}

    # Specific atlas membership takes precedence over words occurring in names.
    for atlas in atlases:
        if atlas.casefold() in ATLAS_CATEGORIES:
            category, subcategory = ATLAS_CATEGORIES[atlas.casefold()]
            return result(category, subcategory, f'所属图集：{atlas}')

    # Use actual directory segments, never loose words like "bow" in "bowl".
    path_rules = [
        (r'/data/weapons/', 'weapons', '武器图标'),
        (r'/data/tools/', 'weapons', '副武器 / 工具图标'),
        (r'/narration/actors/.+/portraits/', 'characters', '人物立绘 / 表情'),
        (r'/ui/(?:inputicons|inputs)/', 'controls', '键鼠 / 手柄按键'),
        (r'/notification/tutorials/', 'ui', '教学图示'),
        (r'/ui/[^/]*meda[il]*l?ions?[^/]*/', 'medallions', '徽章槽 / 配套界面'),
        (r'/ui/skilltree/', 'talents', '天赋树界面'),
        (r'/ui/worldmap/', 'maps', '世界地图 / 地区插图'),
        (r'/ui/(?:minimap|persianmetro)/', 'maps', '地图标记 / 路线'),
        (r'/ui/(?:mindmap|narration)/', 'story', '剧情线索 / 界面'),
        (r'/actors/(?:player|prince|skins)/', 'characters', '人物 / 皮肤贴图'),
        (r'/actors/(?:enemies|mobs|bosses|boss)/', 'enemies', '敌人 / Boss 贴图'),
        (r'/(?:vfx|visualeffects|particles)/', 'effects', '粒子 / 特效贴图'),
        (r'/weapons/', 'weapons', '武器贴图 / 特效'),
        (r'/tools/', 'weapons', '副武器 / 工具贴图'),
        (r'/gamesettings/generation/palette/', 'environment', '关卡编辑 / 地块标记'),
        (r'/biomes/', 'environment', '场景物件 / 建筑'),
        (r'/ui/hud/', 'ui', 'HUD / 玩家状态栏'),
        (r'/ui/loading/', 'ui', '加载 / 过场界面'),
        (r'/ui/prescience/', 'ui', '预知 / 选项界面'),
        (r'/ui/', 'ui', '其他界面'),
        (r'/plugins/doozy/', 'ui', '通用界面组件'),
        (r'/gamesettings/gamecommandmenu/', 'ui', '调试菜单'),
        (r'/cinematic/sponsors/', 'fonts', '制作组 / 标识'),
        (r'/(?:unityuserreporting|com\.tayx\.graphy)/', 'ui', '系统 / 调试界面'),
        (r'/(?:fonts|textmesh pro)/', 'fonts', '字体图集'),
    ]
    for pattern, category, subcategory in path_rules:
        for path in paths:
            if re.search(pattern, path, re.I):
                return result(category, subcategory, f'原始资源路径：{path}')

    # Strong, anchored name conventions cover packed textures lacking direct paths.
    name_rules = [
        (r'^portrait_', 'characters', '人物立绘 / 表情'),
        (r'^charactericon_', 'characters', '人物头像'),
        (r'^alltextureskin$', 'characters', '角色共用贴图'),
        (r'^skinicon_|^skincommunitypackicon$|^communityskinicon$', 'characters', '皮肤图标'),
        (r'^awakeningstone_', 'challenges', '觉醒词条'),
        (r'^medallionpack-', 'medallions', '徽章包'),
        (r'^(?:[rbl]-.*_icon$)', 'medallions', '徽章图标'),
        (r'^skill(?:tree|point)|^tree(?:tiri|atar|haoma|anahita|behram|mithra|vayu)icon$', 'talents', '天赋树 / 天赋点'),
        (r'^(?:lotus|cheetah|nightingale|deer|hippopotamus)-\d-', 'talents', '天赋图标'),
        (r'^(?:anahita|atar|behram|haoma|mithra|tiri|vayu)[_-]', 'talents', '天赋图标'),
        (r'^(?:inputs_|keyboard_)', 'controls', '键鼠 / 手柄按键'),
        (r'^tutorial_', 'ui', '教学图示'),
        (r'^affecticon_', 'effects', '状态图标'),
        (r'^elite_', 'enemies', '精英标记'),
        (r'^node_', 'story', '剧情线索'),
        (r'^link_|^world_map_', 'maps', '世界地图 / 路线'),
        (r'^gt_|spritesheet|vfx|uvmask', 'effects', '粒子 / 渐变 / 动画切片'),
        (r'^hud', 'ui', 'HUD / 玩家状态栏'),
        (r'^ui_', 'ui', '其他界面'),
        (r'font|sdf atlas|^noto|^liberationsans|^logo', 'fonts', '字体 / 标识'),
    ]
    for pattern, category, subcategory in name_rules:
        if re.search(pattern, low):
            return result(category, subcategory, f'资源命名线索：{name}', 'medium')

    for atlas in atlases:
        if atlas.casefold().startswith('atlasui_'):
            return result('ui', '其他界面图集', f'所属图集：{atlas}', 'medium')
        if not atlas.casefold().startswith('atlasui'):
            return result('environment', '场景图集 / 物件', f'场景图集线索：{atlas}', 'medium')
    if '/weapons_assets_' in source:
        return result('weapons', '武器相关贴图', f'武器资源包：{source.rsplit("/",1)[-1]}', 'medium')
    if '/skins_assets_' in source:
        return result('characters', '人物 / 皮肤贴图', f'皮肤资源包：{source.rsplit("/",1)[-1]}', 'medium')
    if '/cutsceneactors_assets_' in source:
        return result('characters', '人物相关贴图', f'人物资源包：{source.rsplit("/",1)[-1]}', 'medium')
    if re.search(r'_(?:biome|rooms)_', source):
        return result('environment', '场景相关贴图', f'场景资源包：{source.rsplit("/",1)[-1]}', 'medium')
    return result('other', '待确认', '尚未匹配到足够明确的图集、路径或命名线索。', 'low')


def categorize(records):
    for record in records:
        record.update(classify(record))
    counts = Counter(record['category'] for record in records)
    return {'version': CLASSIFICATION_VERSION,
            'categories': [{'id': key, 'label': label, 'count': counts[key]} for key, label in CATEGORIES.items()],
            'confidence_counts': dict(Counter(r['category_confidence'] for r in records)),
            'subcategories': {key: dict(Counter(r['subcategory'] for r in records if r['category'] == key)) for key in CATEGORIES}}
