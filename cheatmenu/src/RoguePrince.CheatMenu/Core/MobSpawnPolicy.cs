// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;

public enum MobSpawnHealthMode { OneHp, NativeFull }
public readonly record struct MobSpawnOptions(MobSpawnHealthMode HealthMode, bool AiEnabled, string MobId="SwordMan");
public static class MobSpawnPolicy
{
    public static bool TryResolveHealth(float nativeMaximum, MobSpawnHealthMode mode, out float currentHealth)
    {
        currentHealth = 0;
        if (!float.IsFinite(nativeMaximum) || nativeMaximum < 1 ||
            mode is not (MobSpawnHealthMode.OneHp or MobSpawnHealthMode.NativeFull)) return false;
        currentHealth = mode == MobSpawnHealthMode.OneHp ? 1 : nativeMaximum;
        return true;
    }
}
public sealed class MobSpawnRequestGate
{
    private long serial, current, generation;
    public bool Busy => current != 0;
    public MobSpawnOptions Options { get; private set; }
    public bool TryBegin(long generation, MobSpawnOptions options, out long requestId)
    {
        requestId = 0;
        if (Busy || MobCatalogPolicy.Find(options.MobId)?.Supported!=true || !MobSpawnPolicy.TryResolveHealth(1, options.HealthMode, out _)) return false;
        current = requestId = checked(++serial);
        this.generation = generation;
        Options = options;
        return true;
    }
    public bool CanComplete(long requestId, long generation) =>
        Busy && current == requestId && this.generation == generation;
    public void Complete(long requestId) { if (current == requestId) current = 0; }
    public void Invalidate() => current = 0;
}
