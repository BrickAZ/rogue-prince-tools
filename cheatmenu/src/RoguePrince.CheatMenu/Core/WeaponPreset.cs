// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;

namespace RoguePrince.CheatMenu.Core;

public sealed class WeaponPreset
{
    public string Name { get; set; }
    public string ItemId { get; set; }
    public int Tier { get; set; }
    public List<string> AffixIds { get; set; }
    public static string ValidName(string value)
    {
        value = value?.Trim();
        if (string.IsNullOrEmpty(value) || value.Length > 48 || value.Any(char.IsControl))
            throw new ArgumentException(MenuText.Get("名称请输入 1–48 个字符"));
        return value;
    }
    public static bool ValidId(string id) => !string.IsNullOrWhiteSpace(id) && id.Length <= 128 && !id.Any(c => char.IsControl(c) || char.IsWhiteSpace(c));
    public void Validate()
    {
        Name = ValidName(Name);
        if (!ValidId(ItemId) || Tier < 1 || Tier > 100 || AffixIds == null || AffixIds.Count > 64 || AffixIds.Any(id => !ValidId(id)) || AffixIds.Distinct(StringComparer.Ordinal).Count() != AffixIds.Count)
            throw new ArgumentException(MenuText.Get("预设物品、等级或词缀标识无效"));
    }
    public static WeaponPreset Parse(string json)
    {
        if (json == null || json.Length > 65536) throw new ArgumentException(MenuText.Get("预设文本为空或过大"));
        var result = JsonSerializer.Deserialize<WeaponPreset>(json) ?? throw new ArgumentException(MenuText.Get("预设文本无效"));
        result.Validate(); return result;
    }
    public string Export() { Validate(); return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }); }
}
