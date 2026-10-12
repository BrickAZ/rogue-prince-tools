// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;

public static class AffixSelectionPolicy
{
    public static bool CanAdd(int allowedMask, int targetMask, bool fromPool, bool weaponExcluded, bool duplicate, bool conflict)
        => targetMask != 0 && (allowedMask & targetMask) == targetMask && fromPool && !weaponExcluded && !duplicate && !conflict;
}

public static class AffixEditTransaction
{
    public static void Apply(Action replace, Action verify, Action restore)
    {
        try { replace(); verify(); }
        catch (Exception error)
        {
            try { restore(); }
            catch (Exception rollback) { throw new AggregateException(MenuText.Get("词缀应用和恢复均失败；停止修改并查看日志"), error, rollback); }
            throw new InvalidOperationException(MenuText.Get("词缀应用失败，已恢复原来的词缀"), error);
        }
    }
}

public static class AffixOwnerScope
{
    public static bool IsCurrent(long generation, long currentGeneration, IntPtr player, IntPtr currentPlayer,
        IntPtr inventory, IntPtr currentInventory, IntPtr weapon, IntPtr currentWeapon, int tier, int currentTier)
        => player != IntPtr.Zero && inventory != IntPtr.Zero && weapon != IntPtr.Zero && generation == currentGeneration &&
            player == currentPlayer && inventory == currentInventory && weapon == currentWeapon && tier == currentTier;
}

public sealed class ScopedAffixRecovery
{
    private Action restore;
    private long generation;
    private string description;
    private readonly List<string> retired = new();
    public bool Pending => restore != null;
    public IReadOnlyList<string> Retired => retired;
    public void Set(long ownerGeneration, Action callback, string record)
    {
        if (Pending) throw new InvalidOperationException(MenuText.Get("旧词缀恢复未完成，请先重试恢复"));
        generation = ownerGeneration; restore = callback ?? throw new ArgumentNullException(nameof(callback)); description = record;
    }
    public bool Recover(long currentGeneration)
    {
        if (!Pending) return false;
        if (generation != currentGeneration) throw new InvalidOperationException(MenuText.Get("原角色已变化，不能向新角色恢复旧词缀"));
        restore(); restore = null; description = null; return true;
    }
    public string Retire()
    {
        if (!Pending) return null;
        var record = description;
        retired.Add(record);
        // Retain diagnostics, release old native objects and never replay across bindings.
        restore = null; description = null;
        return record;
    }
}

public sealed class LevelChangeGate
{
    public bool Busy { get; private set; }
    private string destination;
    private float started;
    public bool TryBegin(bool ready, bool loading, bool validTarget, string current, string target, float now)
    {
        if (Busy || !ready || loading || !validTarget || string.IsNullOrEmpty(target) || current == target) return false;
        Busy = true; destination = target; started = now; return true;
    }
    public string Observe(bool loading, string current, float now)
    {
        if (!Busy) return "idle";
        if (!loading && current == destination) { Cancel(); return "completed"; }
        if (now - started >= 30f) { Cancel(); return "failed"; }
        return "pending";
    }
    public void Cancel() { Busy = false; destination = null; }
}
