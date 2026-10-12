// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using System.Text.RegularExpressions;
using UnityEngine.Localization;

namespace RoguePrince.CheatMenu;

internal static class LocalizedTextSource
{
    internal static string Read(LocalizedString reference, UnityEngine.Localization.Locale locale)
    {
        if (reference == null || locale == null) return null;
        try
        {
            // A new reference keeps the game's selected locale and shared ItemData untouched.
            // Affix descriptions serialize their numeric values as persistent local variables.
            var local = new LocalizedString(reference.TableReference, reference.TableEntryReference);
            local.Arguments = reference.Arguments;
            var variables = reference.m_LocalVariables;
            if (variables != null)
                for (int i = 0; i < variables.Count; i++)
                {
                    var pair = variables[i];
                    if (pair != null && pair.variable != null) local.Add(pair.name, pair.variable);
                }
            local.LocaleOverride = locale;
            var text = local.GetLocalizedString();
            if (string.IsNullOrWhiteSpace(text)) return null;
            text = Regex.Replace(text, "<[^>]+>", "").Trim();
            if (text.Length == 0 || text.StartsWith("Items/Affix_", StringComparison.Ordinal) ||
                text.StartsWith("Affix_", StringComparison.Ordinal) || text.Contains('{') || text.Contains('}') ||
                text.StartsWith("No translation found", StringComparison.OrdinalIgnoreCase)) return null;
            return text;
        }
        catch { return null; }
    }
}
