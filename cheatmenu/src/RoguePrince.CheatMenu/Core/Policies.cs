// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
// Additional game linking permission: see GAME-LINKING-EXCEPTION.txt.

using System.Globalization;
namespace RoguePrince.CheatMenu.Core;

public static class NumericPolicy
{
    public static bool TryTarget(string text, int current, bool add, int maximum, out int target, out string error)
    {
        target = 0;
        error = MenuText.Get("请输入范围内的非负整数");
        if (!long.TryParse(text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < 0 || value > maximum) return false;
        var result = add ? (long)current + value : value;
        if (result < 0 || result > maximum) return false;
        target = (int)result;
        error = "";
        return true;
    }
}

public sealed class OwnedAdjustment
{
    private float contribution;
    public float Apply(float currentBase, float requestedBase)
    {
        if (!float.IsFinite(currentBase) || !float.IsFinite(requestedBase)) throw new ArgumentOutOfRangeException(nameof(requestedBase));
        contribution += requestedBase - currentBase;
        return requestedBase;
    }
    public float ApplyThrough(float currentBase, float requestedBase, Action<float> write)
    {
        if (!float.IsFinite(currentBase) || !float.IsFinite(requestedBase)) throw new ArgumentOutOfRangeException(nameof(requestedBase));
        write(requestedBase);
        return Apply(currentBase, requestedBase);
    }
    public float Restore(float currentBase, float minimum, Action<float> write)
    {
        if (!float.IsFinite(currentBase) || !float.IsFinite(minimum)) throw new ArgumentOutOfRangeException(nameof(currentBase));
        var restored = Math.Max(minimum, currentBase - contribution);
        write(restored);
        Clear();
        return restored;
    }
    public bool Active => contribution != 0;
    public void Clear() => contribution = 0;
}
