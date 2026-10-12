// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using MotherBase.ApplicationFlowManager;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace RoguePrince.CheatMenu;

internal sealed class LevelTravelActions
{
    internal sealed class Entry
    {
        public ApplicationState State;
        public ApplicationSignal Signal;
        public LocalizedItemName Label;
        public string Key => State.guid;
        public string Name => Label.Display;
    }
    private readonly GameSession game;
    private readonly LevelChangeGate gate = new();
    private ApplicationFlowManager flow;
    public readonly List<Entry> Levels = new();
    public readonly List<Entry> Next = new();
    public bool Busy => gate.Busy;
    public bool Loading => Busy || (flow != null && flow.isLoading);
    public string CurrentName { get; private set; } = "—";
    public string Status { get; private set; } = "";
    public bool Error { get; private set; }
    public LevelTravelActions(GameSession game) => this.game = game;

    private static Entry ReadEntry(ApplicationState state, bool includeHub)
    {
        var logic = state?.stateLogic?.TryCast<BluesteakApplicationStateLogic>();
        if (logic == null || logic.isTutorialBiome || (!includeHub && logic.isHubBiome) || !logic.IsBiomeGeneration()) return null;
        var descriptor = logic.biomeDescriptor;
        return new Entry { State = state, Label = ItemBrowser.ReadLabel(descriptor?.biomeTitle, state.name) };
    }

    public void Refresh()
    {
        if (Busy) return;
        var found = UObject.FindObjectsOfType<ApplicationFlowManager>();
        if (found.Length != 1 || found[0].profile == null) { flow = null; Levels.Clear(); Next.Clear(); return; }
        flow = found[0];
        if (flow.isLoading) return;
        var current = flow.GetCurrentState();
        CurrentName = ReadEntry(current, true)?.Name ?? current?.name ?? "—";
        var levels = new Dictionary<string, Entry>(StringComparer.Ordinal);
        var states = flow.profile.applicationStates;
        if (states != null)
            for (int i = 0; i < states.Count; i++)
            {
                var entry = ReadEntry(states[i], false);
                if (entry != null && entry.Key != current?.guid) levels[entry.Key] = entry;
            }
        var next = new Dictionary<string, Entry>(StringComparer.Ordinal);
        var transitions = flow.profile.transitions;
        if (transitions != null)
            for (int i = 0; i < transitions.Count; i++)
            {
                var signal = transitions[i]?.signal;
                if (signal == null || !(signal.name == "NextBiome" || signal.name.StartsWith("BiomeChoice", StringComparison.Ordinal)) || !flow.AcceptsSignal(signal)) continue;
                var entry = ReadEntry(flow.GetNextState(signal), true);
                if (entry == null || entry.Key == current?.guid) continue;
                entry.Signal = signal;
                next[entry.Key] = entry;
            }
        Levels.Clear(); Levels.AddRange(levels.Values.OrderBy(e => e.Name));
        Next.Clear(); Next.AddRange(next.Values.OrderBy(e => e.Signal.name));
    }

    public string Travel(Entry entry, Action releaseMenu)
    {
        game.RequireReady();
        if (entry?.State == null || flow == null || flow.isLoading || Busy) throw new InvalidOperationException(MenuText.Get("关卡尚未就绪或正在加载"));
        var current = flow.GetCurrentState();
        var target = flow.FindState(entry.State.stateID);
        if (ReadEntry(target, entry.Signal != null) == null || target.guid != entry.Key)
            throw new InvalidOperationException(MenuText.Get("目标关卡已失效，请刷新关卡列表"));
        if (entry.Signal != null && (!flow.AcceptsSignal(entry.Signal) || flow.GetNextState(entry.Signal)?.guid != target.guid))
            throw new InvalidOperationException(MenuText.Get("当前出口已变化，请重新选择下一关"));
        if (!gate.TryBegin(game.Ready, flow.isLoading, true, current?.guid, target.guid, Time.realtimeSinceStartup))
            throw new InvalidOperationException(MenuText.Get("无法重复切关或跳到当前关卡"));
        try
        {
            releaseMenu();
            if (Time.timeScale <= 0) throw new InvalidOperationException(MenuText.Get("游戏仍被其他界面暂停，请先关闭原生暂停菜单"));
            RunTools.Current?.MenuLoadRequested(flow,target);
            if (entry.Signal != null) flow.SendSignal(entry.Signal);
            else flow.LoadState(target);
            Status = MenuText.Format("已请求进入：{0}", entry.Name); Error = false;
            Plugin.LogEvent($"level_travel_requested from={current?.name} to={target.name} signal={entry.Signal?.name ?? "direct"}");
            return Status;
        }
        catch { gate.Cancel(); throw; }
    }

    public void Tick()
    {
        if (!Busy) return;
        var result = gate.Observe(flow != null && flow.isLoading, flow?.GetCurrentState()?.guid, Time.realtimeSinceStartup);
        if (result == "pending") return;
        Error = result == "failed";
        Status = MenuText.Get(Error ? "切关超时；未自动重试，请查看游戏状态和日志" : "目标关卡已加载");
        Plugin.LogEvent($"level_travel_{result} current={flow?.GetCurrentState()?.name}");
    }
}
