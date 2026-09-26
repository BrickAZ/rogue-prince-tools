// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
// Additional game linking permission: see GAME-LINKING-EXCEPTION.txt.

using RoguePrince.CheatMenu.Core;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace RoguePrince.CheatMenu;

internal sealed class MedallionSlotActions
{
    private readonly GameSession game;
    private readonly Action backup;
    private string lastError = "";
    public bool Available { get; private set; }
    public int Current { get; private set; }
    public int Maximum { get; private set; }
    public int Occupied { get; private set; }
    public string Status { get; private set; } = MenuText.Get("徽章栏位：等待角色");

    public MedallionSlotActions(GameSession game, Action backup) { this.game = game; this.backup = backup; }

    private sealed class Binding
    {
        public ItemProgressionManagementService Service;
        public IntRef BaseRef, AdditionalRef;
        public int BaseSlots, AdditionalSlots, Maximum, Occupied;
        public IntPtr Player;
    }

    private Binding Read()
    {
        game.RequireReady();
        // Use the same service as the native inventory screen and medallion pickup check.
        var service = RuntimeServiceLocator.Get<ItemProgressionManagementService>();
        if (service == null || service.playerVariable?.CurrentValue?.Pointer != game.PlayerPointer)
            throw new InvalidOperationException(MenuText.Get("徽章成长系统尚未绑定当前角色"));
        var baseRef = service.maxSlotsMedallions;
        var additionalRef = service.maxAdditionalSlotsMedallions;
        var capRef = service.maxSlotsFullCapacityMedallions;
        var baseVar = baseRef?.FindVar();
        var additionalVar = additionalRef?.FindVar();
        var capVar = capRef?.FindVar();
        if (baseVar == null || additionalVar == null || capVar == null ||
            baseVar.Pointer == additionalVar.Pointer || baseVar.Pointer == capVar.Pointer || additionalVar.Pointer == capVar.Pointer)
            throw new InvalidOperationException(MenuText.Get("徽章栏位变量缺失或重叠"));

        var inventory = game.Player.GetComponent<InventoryComponent>();
        var items = inventory?.items;
        if (items == null) throw new InvalidOperationException(MenuText.Get("角色库存尚未就绪"));
        var occupied = 0;
        var count = items.Count;
        // Native InventoryTabMenuUIView filters item.source.HasComponent<MedallionItemFeature>().
        // Count each matching instance once, including half-medallions. No interface enumerator.
        for (var i = 0; i < count; i++)
        {
            var item = items[i];
            if (item?.source == null) throw new InvalidOperationException(MenuText.Get("库存中存在未就绪物品"));
            if (item.source.HasComponent<MedallionItemFeature>()) occupied++;
        }
        if (items.Count != count) throw new InvalidOperationException(MenuText.Get("库存正在变化，请稍后重试"));
        var basis = baseRef.value;
        var additional = additionalRef.value;
        var maximum = capRef.value;
        SlotCapacityPolicy.Validate(basis, additional, maximum, occupied);
        if (service.GetMaxSlotsMedallions() != basis + additional)
            throw new InvalidOperationException(MenuText.Get("原生徽章容量与栏位变量不一致"));
        return new Binding { Service = service, BaseRef = baseRef, AdditionalRef = additionalRef,
            BaseSlots = basis, AdditionalSlots = additional, Maximum = maximum, Occupied = occupied, Player = game.PlayerPointer };
    }

    public void Refresh()
    {
        Available = false;
        if (!game.Ready) { Status = MenuText.Get("徽章栏位：等待角色"); return; }
        try
        {
            var state = Read();
            Current = state.BaseSlots + state.AdditionalSlots;
            Maximum = state.Maximum;
            Occupied = state.Occupied;
            Available = true;
            Status = MenuText.Format("徽章栏位  {0} / {1}  ·  已装备 {2} 枚", Current, Maximum, Occupied);
            lastError = "";
        }
        catch (Exception e)
        {
            Status = MenuText.Get("徽章栏位：") + e.Message;
            if (lastError != e.Message) Plugin.MenuLog.LogWarning("[RPCheat] medallion_slots_unavailable " + e);
            lastError = e.Message;
        }
    }

    public string Change(SlotChange change)
    {
        var before = Read();
        var plan = SlotCapacityPolicy.Plan(before.BaseSlots, before.AdditionalSlots, before.Maximum, before.Occupied, change);
        if (plan.BaseSlots == before.BaseSlots && plan.AdditionalSlots == before.AdditionalSlots)
            return change == SlotChange.Remove ? MenuText.Get("徽章栏位已为 0") : MenuText.Get("徽章栏位已达到游戏上限");
        backup();
        var latest = Read();
        if (latest.Player != before.Player || latest.Service.Pointer != before.Service.Pointer ||
            latest.BaseRef.Pointer != before.BaseRef.Pointer || latest.AdditionalRef.Pointer != before.AdditionalRef.Pointer ||
            latest.BaseSlots != before.BaseSlots || latest.AdditionalSlots != before.AdditionalSlots ||
            latest.Maximum != before.Maximum || latest.Occupied != before.Occupied)
            throw new InvalidOperationException(MenuText.Get("备份期间角色或徽章状态发生变化，请重试"));
        try
        {
            // VarRef.value invokes native OnValueChanged. Never write Var._value or UI lock flags.
            var changedBase = plan.BaseSlots != before.BaseSlots;
            if (changedBase) before.BaseRef.value = plan.BaseSlots;
            else before.AdditionalRef.value = plan.AdditionalSlots;
            var actual = Read();
            if (actual.BaseSlots != plan.BaseSlots || actual.AdditionalSlots != plan.AdditionalSlots)
                throw new InvalidOperationException(MenuText.Get("游戏未保留目标栏位数，请检查当前显示后重试"));
            var total = actual.BaseSlots + actual.AdditionalSlots;
            Plugin.LogEvent($"medallion_slots operation={change} before={before.BaseSlots + before.AdditionalSlots} after={total} cap={actual.Maximum} occupied={actual.Occupied} base={actual.BaseSlots} additional={actual.AdditionalSlots}");
            var uiUpdated = RefreshNativeViews(changedBase);
            return MenuText.Format("徽章栏位：{0} → {1} / {2}；已装备徽章保留", before.BaseSlots + before.AdditionalSlots, total, actual.Maximum) +
                (uiUpdated ? "" : MenuText.Get("；请重新打开库存刷新显示"));
        }
        finally { Refresh(); }
    }

    private bool RefreshNativeViews(bool changedBase)
    {
        try
        {
            // HUD subscribes only to additional slots. Base-slot reductions need its native rebuild.
            if (changedBase)
                foreach (var hud in UObject.FindObjectsOfType<MedallionHUDController>())
                    if (hud != null && hud.entityVariable?.CurrentValue?.Pointer == game.PlayerPointer) hud.ResetState();
            // The inventory grid normally rebuilds on opening, so also refresh one already on screen.
            foreach (var view in UObject.FindObjectsOfType<InventoryTabMenuUIView>())
                if (view != null && view.entityVariable?.CurrentValue?.Pointer == game.PlayerPointer) view.SetupInventoryTabMenu();
            return true;
        }
        catch (Exception e)
        {
            Plugin.MenuLog.LogWarning("[RPCheat] medallion_slots_ui_refresh " + e);
            return false;
        }
    }
}
