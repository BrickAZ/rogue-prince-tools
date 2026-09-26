// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
// Additional game linking permission: see GAME-LINKING-EXCEPTION.txt.

namespace RoguePrince.CheatMenu.Core;

public enum SlotChange { Add, Remove, UnlockAll }

public static class SlotCapacityPolicy
{
    public static void Validate(int baseSlots, int additionalSlots, int maximum, int occupied)
    {
        var total = (long)baseSlots + additionalSlots;
        if (baseSlots < 0 || additionalSlots < 0 || maximum <= 0 || total > maximum || occupied < 0 || occupied > total)
            throw new ArgumentOutOfRangeException(nameof(baseSlots), MenuText.Get("徽章栏位数据不一致，未执行修改"));
    }

    // Every plan changes at most one native variable, avoiding a partial two-variable update.
    public static (int BaseSlots, int AdditionalSlots) Plan(int baseSlots, int additionalSlots, int maximum, int occupied, SlotChange change)
    {
        Validate(baseSlots, additionalSlots, maximum, occupied);
        var current = baseSlots + additionalSlots;
        switch (change)
        {
            case SlotChange.Add:
                if (current < maximum) additionalSlots++;
                break;
            case SlotChange.Remove:
                if (current == 0) break;
                if (current - 1 < occupied)
                    throw new InvalidOperationException(MenuText.Format("已装备 {0} 枚徽章，请先卸下一枚再减少栏位", occupied));
                if (additionalSlots > 0) additionalSlots--;
                else baseSlots--;
                break;
            case SlotChange.UnlockAll:
                additionalSlots = maximum - baseSlots;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(change));
        }
        return (baseSlots, additionalSlots);
    }
}
