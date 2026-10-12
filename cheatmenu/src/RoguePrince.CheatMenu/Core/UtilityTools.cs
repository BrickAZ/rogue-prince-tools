// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;

public static class ActionKeyPolicy
{
    public static readonly string[] Actions = { "heal", "energy", "spawnOneHp", "spawnFull", "clearMobs", "timer", "normalSpeed", "exitPhoto" };
    public static bool CanDispatch(bool menu,bool editing,bool focused,bool ready,bool loading,bool recovery,bool busy,bool paused)
        => !menu && !editing && focused && ready && !loading && !recovery && !busy && !paused;
    public static bool CanCapture(bool menu,int tab,int section) => menu && tab==9 && section==1;
    public static void Validate(string action, string key, string menuKey, Dictionary<string,string> bindings)
    {
        if (!Actions.Contains(action)) throw new ArgumentException(MenuText.Get("未知快捷操作"));
        if (key == "None") return;
        if (!Enumerable.Range(1,12).Select(i => "F"+i).Contains(key) || key == "F8" || key == menuKey || bindings.Any(e => e.Key != action && e.Value == key))
            throw new ArgumentException(MenuText.Get("快捷键冲突；请使用未占用的 F1–F12（除 F8）"));
    }
}

public sealed class PracticeResult
{
    public string Name { get; set; }
    public double Seconds { get; set; }
}
public sealed class PracticeTimer
{
    public static bool IsDuration(double seconds) => double.IsFinite(seconds) && seconds>0 && seconds<=TimeSpan.MaxValue.TotalSeconds-1;
    public bool Running { get; private set; }
    public double Elapsed { get; private set; }
    public string Name { get; private set; } = "";
    public void Start(string name)
    {
        if (Running) throw new InvalidOperationException(MenuText.Get("计时已经开始"));
        Name = WeaponPreset.ValidName(name); Elapsed = 0; Running = true;
    }
    public void Advance(double seconds, bool active)
    {
        if (Running && active && IsDuration(seconds) && IsDuration(Elapsed+seconds)) Elapsed += seconds;
    }
    public PracticeResult Stop()
    {
        if (!Running) return null;
        Running = false;
        return Elapsed > 0 ? new PracticeResult { Name = Name, Seconds = Elapsed } : null;
    }
    public void Reset() { Running = false; Elapsed = 0; Name = ""; }
}
public sealed class RecentMenuLog
{
    private readonly int capacity;
    private readonly Queue<string> entries = new();
    private readonly object sync = new();
    public RecentMenuLog(int capacity) { if(capacity < 1 || capacity > 256) throw new ArgumentOutOfRangeException(nameof(capacity)); this.capacity = capacity; }
    public void Add(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        text = text.Replace('\n',' ').Replace('\r',' '); if(text.Length > 600) text = text.Substring(0,597)+"...";
        lock(sync) { entries.Enqueue(text); while(entries.Count > capacity) entries.Dequeue(); }
    }
    public string[] Snapshot() { lock(sync) return entries.ToArray(); }
}
