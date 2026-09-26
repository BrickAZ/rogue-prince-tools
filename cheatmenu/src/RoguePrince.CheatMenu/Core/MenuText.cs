// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
// Additional game linking permission: see GAME-LINKING-EXCEPTION.txt.

using System.Globalization;

namespace RoguePrince.CheatMenu.Core;

// Menu locale is independent of Unity's selected game locale and of category IDs.
public static partial class MenuText
{
    public static string LanguageCode { get; private set; } = "zh-CN";
    public static bool IsEnglish => LanguageCode == "en-US";
    public static void SetLanguage(string code) => LanguageCode = code == "en-US" ? "en-US" : "zh-CN";
    public static string Get(string key) => IsEnglish && English.TryGetValue(key, out var value) ? value : key;
    public static string Format(string key, params object[] values) => string.Format(CultureInfo.InvariantCulture, Get(key), values);
    public static IReadOnlyDictionary<string, string> Translations => English;
}

public sealed class LocalizedItemName
{
    private readonly string chinese;
    private readonly string english;
    private readonly string internalName;
    public LocalizedItemName(string chinese, string english, string internalName)
    {
        this.internalName = internalName ?? "";
        this.chinese = string.IsNullOrWhiteSpace(chinese) ? this.internalName : chinese;
        this.english = string.IsNullOrWhiteSpace(english) ? this.internalName : english;
    }
    public string Display => MenuText.IsEnglish ? english : chinese;
    public bool Matches(string query)
    {
        query = query?.Trim() ?? "";
        return query.Length == 0 || chinese.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            english.Contains(query, StringComparison.OrdinalIgnoreCase) || internalName.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
