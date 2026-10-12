// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
using UnityEngine;

namespace RoguePrince.CheatMenu;

internal static class MobSpawnPlacement
{
    internal static bool TryOffset(Transform player, Vector3 target, out Vector3 offset)
    {
        offset = default;
        if (player == null) return false;
        // Native _SpawnMob_d__23 uses player.position.x + forward.x * SpawnOffset.x,
        // while y/z are added directly. right.x is not its facing value.
        float facing = player.forward.x;
        if (!float.IsFinite(facing) || Math.Abs(facing) < .0001f) return false;
        var delta = target - player.position;
        var candidate = new Vector3(delta.x / facing, delta.y, delta.z);
        if (!float.IsFinite(candidate.x) || !float.IsFinite(candidate.y) || !float.IsFinite(candidate.z))
            return false;
        offset = candidate;
        return true;
    }
}
