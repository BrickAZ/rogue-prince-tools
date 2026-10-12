// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;

public static partial class MenuText
{
    static MenuText()
    {
        AddCombatTranslations();
        AddSceneTranslations();
        AddBossPhaseTranslations();
        AddRestartTranslations();
        AddWorldReportTranslations();
        AddDamageStatisticsTranslations();
        AddToolLimitTranslations();
        AddBossTargetTranslations();
        var additions=new Dictionary<string,string>
        {
            ["设置"]="Settings", ["预设"]="Presets", ["工具"]="Tools", ["仅收藏"]="Favorites",
            ["收藏"]="Favorite", ["已收藏"]="Added to favorites.", ["已取消收藏"]="Removed from favorites.",
            ["名称请输入 1–48 个字符"]="Enter a name of 1–48 characters.",
            ["预设物品、等级或词缀标识无效"]="Invalid preset item, tier or affix IDs.",
            ["预设文本为空或过大"]="Preset text is empty or too large.", ["预设文本无效"]="Invalid preset text.",
            ["未知快捷操作"]="Unknown shortcut action.",
            ["快捷键冲突；请使用未占用的 F1–F12（除 F8）"]="Key conflict. Use a free F1–F12 key, excluding F8.",
            ["计时已经开始"]="The timer is already running.", ["配置文件过大"]="Configuration file is too large.",
            ["配置版本不支持"]="Unsupported configuration version.", ["配置内容无效或超出容量"]="Invalid configuration or capacity exceeded.",
            ["收藏标识无效"]="Invalid favorite ID.", ["收藏重复"]="Duplicate favorite.", ["预设名称已存在"]="A preset with this name already exists.",
            ["最佳计时无效"]="Invalid best time.", ["配置读取失败；保留原文件，当前禁止保存"]="Configuration could not be read. Original file preserved; saving disabled.",
            ["预设物品在当前版本中不存在"]="Preset item is missing in this game version.", ["预设词缀失效或不兼容"]="Preset affix is missing or incompatible.",
            ["预设已载入草稿：{0}"]="Preset loaded into draft: {0}",
            ["开始 / 停止计时"]="Start / stop timer", ["生命已回满"]="Health refilled.", ["能量已补满"]="Energy refilled.",
            ["请先在怪物页选择可召唤目标"]="Select a supported target on the enemy page first.", ["设置快捷键"]="Set shortcut key",
            ["该按键已绑定独立操作，请先取消绑定"]="This key belongs to an action. Unbind it first.",
            ["武器 / 工具预设"]="Weapon / tool presets", ["预设名称"]="Preset name", ["保存当前词缀草稿"]="Save current affix draft",
            ["保存预设"]="Save preset", ["预设已保存"]="Preset saved.", ["从剪贴板导入"]="Import from clipboard", ["导入预设"]="Import preset",
            ["已导入；请载入或生成进行兼容校验"]="Imported. Load or generate to check compatibility.",
            ["先在词缀页载入武器并选词缀，再保存为预设。"]="Load a weapon and select its affixes on the affix page, then save a preset.",
            ["暂无预设；保存草稿或从剪贴板导入 JSON。"]="No presets yet. Save a draft or import JSON from the clipboard.",
            ["请选择预设"]="Select a preset", ["等级 {0} · 词缀 {1} 个"]="Tier {0} · {1} affixes",
            ["载入词缀草稿"]="Load affix draft", ["载入预设"]="Load preset", ["生成预设武器"]="Generate preset weapon", ["生成预设"]="Generate preset",
            ["复制 JSON"]="Copy JSON", ["导出预设"]="Export preset", ["预设 JSON 已复制"]="Preset JSON copied.",
            ["删除预设"]="Delete preset", ["预设已删除"]="Preset deleted.", ["按输入名称重命名"]="Rename to entered name", ["重命名预设"]="Rename preset", ["预设已重命名"]="Preset renamed.",
            ["预设不会自动装备；失效物品和冲突词缀会拒绝生成。"]="Presets do not auto-equip. Missing items and incompatible affixes are rejected.",
            ["信息与日志"]="Info and logs", ["操作快捷键"]="Action shortcuts", ["练习计时"]="Practice timer",
            ["仅在菜单关闭、游戏就绪且窗口获得焦点时触发。F8 和菜单键保留。"]="Shortcuts require a closed menu, a ready player and window focus. F8 and the menu key are reserved.",
            ["请按 F1–F12 / Esc 取消"]="F1–F12 / Esc cancels", ["未绑定"]="Unbound", ["取消绑定"]="Unbind", ["快捷键已禁用"]="Shortcut disabled.",
            ["怪物快捷键使用清单中选中的目标和 AI 设置；长按不会重复召唤。"]="Enemy shortcuts use the selected target and AI setting. Holding a key does not repeat spawning.",
            ["计时已停止，没有有效练习时间"]="Timer stopped with no active practice time.", ["计时停止：{0} · {1}"]="Timer stopped: {0} · {1}",
            ["最佳记录未保存"]="Best time was not saved", ["默认练习"]="Default practice", ["计时已开始；关闭菜单后累计有效时间"]="Timer started. Close the menu to accumulate active time.",
            ["练习名称"]="Practice name", ["计时中"]="Running", ["计时暂停"]="Timer paused", ["计时未运行"]="Timer stopped",
            ["最佳有效时间"]="Best active time", ["停止并记录成绩"]="Stop and record time", ["开始计时"]="Start timer", ["重置（不记录成绩）"]="Reset without recording", ["重置计时"]="Reset timer", ["计时已重置"]="Timer reset.",
            ["菜单、失焦、暂停、加载或死亡时不计时。手动停止后，同名练习仅保存更快成绩；名称在开始时固定。"]="Time pauses in menus, while unfocused, paused, loading or dead. Stop manually to save a faster best for this practice. The name is fixed at start.",
            ["当前种子"]="Current seed", ["未就绪"]="Not ready", ["复制当前种子"]="Copy current seed", ["复制种子"]="Copy seed", ["当前种子未就绪"]="Current seed is not ready.", ["种子已复制"]="Seed copied.",
            ["版本 {0} · 关卡 {1} · 位置 {2}"]="Version {0} · Level {1} · Position {2}",
            ["自有怪物 {0} · 词缀恢复 {1} · 营救预约 {2}"]="Owned enemies {0} · Affix recovery {1} · Rescue request {2}",
            ["等待"]="Pending", ["最近菜单日志（新到旧）"]="Recent menu logs (newest first)", ["复制最近日志"]="Copy recent logs", ["复制日志"]="Copy logs", ["最近日志已复制"]="Recent logs copied."
        };
        foreach(var pair in additions) English.TryAdd(pair.Key,pair.Value);
        AddTrainingTranslations();
    }
}
