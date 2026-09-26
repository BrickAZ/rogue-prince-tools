// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
// Additional game linking permission: see GAME-LINKING-EXCEPTION.txt.

using BepInEx;
using RoguePrince.CheatMenu.Core;
using MotherBase.ToolKit;

namespace RoguePrince.CheatMenu;

internal sealed class ResourceActions
{
    public static readonly string[] Names = { "金币", "本局灵魂灰烬", "已储存灵魂灰烬", "腐化之血", "重选硬币", "技能点" };
    private readonly GameSession game;
    public string LastBackup { get; private set; } = MenuText.Get("尚未执行需要备份的修改");
    private Var<int> reroll;
    public string RerollBindingStatus { get; private set; } = MenuText.Get("等待重选硬币数据");
    private const string RunVarsGuid = "0dc58ebdb5041f4498728cd1826778f1";
    private const string RerollVariableGuid = "332ac972-802e-44ce-9ed6-a988633d059b";
    public ResourceActions(GameSession game) => this.game = game;

    public void BindReroll()
    {
        if (!game.Ready) { SetRerollBinding(null, MenuText.Get("等待存活角色与存档数据")); return; }
        try
        {
            // FreeRerollCount is a HUD GameObject name, not a variable name.
            // The HUD references RunVars/Progression_Shop_RerollAvailable by this GUID.
            var saver = VariableListSaver.Get();
            var lists = saver != null ? saver.listToSave : null;
            if (lists == null) { SetRerollBinding(null, MenuText.Get("重选硬币：等待存档变量加载")); return; }
            var matches = new Dictionary<IntPtr, Var<int>>();
            var runLists = 0;
            for (var i = 0; i < lists.Count; i++)
            {
                var list = lists[i];
                if (list == null || !string.Equals(list.Guid.guid, RunVarsGuid, StringComparison.OrdinalIgnoreCase)) continue;
                runLists++;
                Var variable = null;
                if (!list.FindByGuid(RerollVariableGuid, out variable) || variable == null) continue;
                var number = variable.TryCast<Var<int>>();
                if (number == null) { SetRerollBinding(null, MenuText.Get("重选硬币变量类型不兼容")); return; }
                matches[number.Pointer] = number;
            }
            if (matches.Count == 1) SetRerollBinding(matches.Values.First(), MenuText.Get("重选硬币已绑定"));
            else SetRerollBinding(null, runLists == 0 ? MenuText.Get("重选硬币：等待本局存档变量加载") :
                matches.Count == 0 ? MenuText.Get("未找到本局存档的重选硬币变量") : MenuText.Get("重选硬币存在多个不同变量，暂未绑定"));
        }
        catch (Exception e) { SetRerollBinding(null, MenuText.Get("重选硬币绑定失败：") + e.Message); }
    }

    private void SetRerollBinding(Var<int> variable, string status)
    {
        var changed = reroll?.Pointer != variable?.Pointer || RerollBindingStatus != status;
        reroll = variable;
        RerollBindingStatus = status;
        if (changed) Plugin.LogEvent($"reroll_binding bound={reroll != null} source=VariableListSaver guid={RerollVariableGuid} status={status}" +
            (reroll != null ? $" name={reroll.name} value={reroll.value}" : ""));
    }
    public bool Available(int kind) => game.Ready && (kind == 0 ? game.Gold != null : kind == 4 ? reroll != null : game.Meta != null);
    public int Read(int kind) => kind switch
    {
        0 => game.Gold.currentGold,
        1 => game.Meta.runSpiritShards,
        2 => game.Meta.persistentSpiritShards,
        3 => game.Meta.corruptedBlood,
        4 => reroll.value,
        5 => game.Meta.skillPoints,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    public bool IsPersistent(int kind) => kind is 2 or 3 or 5;
    public int Maximum(int kind) => kind == 5 ? 100 : kind == 4 ? 9999 : 9999999;

    public string Change(int kind, string text, bool add)
    {
        game.RequireReady();
        if (kind == 4) BindReroll();
        if (!Available(kind)) throw new InvalidOperationException(MenuText.Get("此资源尚未绑定"));
        var previous = Read(kind);
        if (!NumericPolicy.TryTarget(text, previous, add, Maximum(kind), out var target, out var error)) throw new ArgumentException(error + $"（0–{Maximum(kind)}）");
        if (IsPersistent(kind)) Backup();
        switch (kind)
        {
            case 0: game.Gold.SetGold(game.Player, target, false); break;
            case 1: game.Meta.SetRunSpiritShards(target); break;
            case 2: game.Meta.SetPersistentSpiritShards(target); break;
            case 3: game.Meta.SetCorruptedBlood(target); break;
            case 4: reroll.value = target; break;
            case 5: game.Meta.SetSkillPoints(target); break;
        }
        var actual = Read(kind);
        if (actual != target) throw new InvalidOperationException(MenuText.Format("游戏返回的数量为 {0}，目标 {1} 未完全生效", actual, target));
        return $"{MenuText.Get(Names[kind])}: {previous} → {actual}";
    }

    public string TransferShards()
    {
        game.RequireReady();
        if (game.Meta == null) throw new InvalidOperationException(MenuText.Get("成长系统尚未就绪"));
        var beforeRun = game.Meta.runSpiritShards;
        var beforeStored = game.Meta.persistentSpiritShards;
        if (beforeRun <= 0) return MenuText.Get("没有需要转存的灵魂灰烬");
        if ((long)beforeRun + beforeStored > 9999999) throw new InvalidOperationException(MenuText.Get("转存后超过菜单支持的数量上限"));
        Backup();
        game.Meta.TransferRunToPersistentSpiritShards();
        return MenuText.Format("转存完成：携带 {0} → {1}；储存 {2} → {3}", beforeRun, game.Meta.runSpiritShards, beforeStored, game.Meta.persistentSpiritShards);
    }

    public void Backup()
    {
        LastBackup = SaveBackup.Create(Path.Combine(Paths.GameRootPath, "userdata", "saves"), Path.Combine(Paths.BepInExRootPath, "CheatMenuBackups"));
        Plugin.LogEvent("save_backup=" + LastBackup);
    }
}
