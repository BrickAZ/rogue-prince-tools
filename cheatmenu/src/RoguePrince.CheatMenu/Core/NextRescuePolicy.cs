// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;

public sealed class NextRescuePolicy
{
    private string context, origin;
    private int seed;
    private float started;
    public bool Pending { get; private set; }
    public bool Prepared { get; private set; }
    public bool Confirmed { get; private set; }
    public string Destination { get; private set; }
    public bool TryArm(bool ready, bool loading, string run, int runSeed, string current)
    {
        if (Pending || !ready || loading || string.IsNullOrEmpty(run) || string.IsNullOrEmpty(current)) return false;
        context=run; seed=runSeed; origin=current; Pending=true; return true;
    }
    public string Begin(string run, int runSeed, string target, bool supported, float now, Action force)
    {
        if (!Pending) return "idle";
        if (run!=context || runSeed!=seed || string.IsNullOrEmpty(target) || (Prepared && Destination!=target))
        { Cancel(); return "context_changed"; }
        if (target==origin) return "current";
        if (!supported) { Cancel(); return "unsupported"; }
        if (Prepared) return "prepared";
        try { force(); }
        catch { Cancel(); throw; }
        Destination=target; started=now; Prepared=true; return "prepared";
    }
    public bool Confirm(string target, bool valid, Action releaseSchedule)
    {
        if (!Prepared || target!=Destination) return false;
        if (Confirmed) return true;
        if (!valid) { Cancel(); return false; }
        try { releaseSchedule(); }
        catch { Cancel(); throw; }
        Confirmed=true; return true;
    }
    public string Observe(string run, int runSeed, bool active, bool loading, string current, float now)
    {
        if (!Pending) return "idle";
        if (!active || run!=context || runSeed!=seed) { Cancel(); return "context_changed"; }
        if (!Prepared && !loading && !string.IsNullOrEmpty(current) && current!=origin)
        { Cancel(); return "unsupported"; }
        if (Prepared && Confirmed && !loading && current==Destination) { Cancel(); return "completed"; }
        if (Prepared && now-started>=60f) { Cancel(); return "failed"; }
        return "pending";
    }
    public void Cancel()
    {
        Pending=Prepared=Confirmed=false; context=origin=Destination=null;
    }
}

public static class RescueWriteTransaction
{
    public static void Apply(Action write, Action verify, Action restore)
    {
        try { write(); verify(); }
        catch (Exception error)
        {
            try { restore(); }
            catch (Exception rollback) { throw new AggregateException(MenuText.Get("营救安排和恢复均失败，请查看日志"),error,rollback); }
            throw new InvalidOperationException(MenuText.Get("营救安排失败，已恢复原生安排"),error);
        }
    }
}
