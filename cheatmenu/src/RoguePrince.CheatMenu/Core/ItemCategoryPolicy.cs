// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
// Additional game linking permission: see GAME-LINKING-EXCEPTION.txt.

namespace RoguePrince.CheatMenu.Core;

public static class ItemCategoryPolicy
{
    public static readonly string[] Categories = {
        "全部", "武器", "工具", "徽章", "天赋", "觉醒", "皮肤",
        "扩展包", "词缀", "蓝图", "资源补给", "永久升级", "调试", "其他"
    };

    // These native containers wrap their reward in EffectItemFeature instead of
    // exposing a Gain* feature. Only IDs verified in the installed assets belong here.
    private static readonly HashSet<string> ResourceContainers = new(StringComparer.Ordinal) {
        "Container-Gold-Medium", "Container-Gold-Big",
        "Container-Shards-Tuto", "Container-Shards-Small", "Container-Shards-Medium", "Container-Shards-Big",
        "Container-CorruptedBlood-Small", "Container-CorruptedBlood-Big"
    };

    public static string Classify(string internalName, IReadOnlyCollection<string> features)
    {
        if (features.Contains("MedallionItemFeature")) return "徽章";
        if (features.Contains("UtilityWeaponFeature")) return "工具";
        if (features.Contains("WeaponFeature")) return "武器";
        if (features.Contains("SkillTreeItemFeature")) return "天赋";
        if (features.Contains("AwakeningItemFeature")) return "觉醒";
        if (features.Contains("SkinItemFeature")) return "皮肤";
        if (features.Contains("MedallionsPackTagFeature") || features.Contains("AddListItemsToReadyToUnlockFeature")) return "扩展包";
        if (features.Contains("AffixItemFeature")) return "词缀";
        if (features.Contains("BlueprintItemFeature")) return "蓝图";
        if (features.Contains("DebugItemFeature")) return "调试";
        if (features.Contains("GainGoldFeature") || features.Contains("GoldHolderItemFeature") ||
            features.Contains("GainSpiritShardFeature") || features.Contains("GainCorruptedBloodFeature") ||
            features.Contains("AdditionalSkillPointFeature") || features.Contains("HealFeature") ||
            (features.Contains("EffectItemFeature") && ResourceContainers.Contains(internalName))) return "资源补给";
        // Specific equipment, talent, skin and pack tags take priority over their
        // additional crafting/forge unlock tags.
        if (features.Contains("UnlockInCraftFeature") || features.Contains("UnlockInForgeFeature")) return "永久升级";
        return "其他";
    }
}
