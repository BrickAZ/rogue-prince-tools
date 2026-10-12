// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu;

internal static class WeaponAffixOwnership
{
    // GUID identity is shared between weapons; the native owner pointer is not.
    public static bool IsOwnedBy(ItemInstanceData owner, ItemInstanceData child)
        => owner != null && child?.GetComponent<AffixItemFeature.AffixComponentData>()?.targetItem?.Pointer == owner.Pointer;
}
