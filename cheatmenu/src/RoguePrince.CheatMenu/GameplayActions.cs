// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
// Additional game linking permission: see GAME-LINKING-EXCEPTION.txt.

using HarmonyLib;
using MotherBase.EntityFramework;
using RoguePrince.CheatMenu.Core;
using UnityEngine;

namespace RoguePrince.CheatMenu;

internal sealed class GameplayActions
{
    private readonly GameSession game;
    private readonly OwnedAdjustment healthAdjustment = new();
    private readonly OwnedAdjustment energyAdjustment = new();
    private bool ownsGodFlag;
    public bool GodMode { get; private set; }
    public bool InfiniteEnergy { get; private set; }
    public bool HoldWind { get; private set; }
    public bool HasAdjustments => healthAdjustment.Active || energyAdjustment.Active;
    public static IntPtr InfiniteEnergyTarget;
    public static bool EnergyPatchAvailable;

    public GameplayActions(GameSession game) => this.game = game;

    public void Heal()
    {
        game.RequireReady();
        var missing = game.Health.maxHealth - game.Health.currentHealth;
        if (missing > 0) game.Health.ApplyHeal(game.Player, missing);
        // The explicit full-heal button guarantees the requested value after normal heal modifiers.
        if (game.Health.currentHealth < game.Health.maxHealth) game.Health.currentHealth = game.Health.maxHealth;
    }

    public void RefillEnergy()
    {
        game.RequireReady();
        if (game.Energy == null || !game.Energy.isInitialized) throw new InvalidOperationException(MenuText.Get("能量组件尚未就绪"));
        game.Energy.SetEnergy(game.Energy.energyMaxValueStat, true);
    }

    public void SetMaximum(bool health, int value, bool fill)
    {
        game.RequireReady();
        if (value < 1 || value > 10000) throw new ArgumentOutOfRangeException(nameof(value), MenuText.Get("上限范围为 1–10000"));
        if (health)
        {
            var old = StatsUtils.GetStatBaseValueFloat<Health>(game.Player);
            healthAdjustment.ApplyThrough(old, value, v => StatsUtils.SetStatFloat<Health>(game.Player, v));
            game.Health.currentHealth = Math.Min(game.Health.currentHealth, game.Health.maxHealth);
            if (fill) Heal();
        }
        else
        {
            if (game.Energy == null) throw new InvalidOperationException(MenuText.Get("能量组件不存在"));
            var old = StatsUtils.GetStatBaseValueInt<EnergyMaxValue>(game.Player);
            energyAdjustment.ApplyThrough(old, value, v => StatsUtils.SetStatInt<EnergyMaxValue>(game.Player, (int)v));
            game.Energy.SetEnergy(Math.Min(game.Energy.energyCurrentValueStat, game.Energy.energyMaxValueStat), true);
            if (fill) RefillEnergy();
        }
    }

    public void RestoreMaximums()
    {
        game.RequireReady();
        RestoreMaximumsCore();
    }

    private void RestoreMaximumsCore()
    {
        if (healthAdjustment.Active)
        {
            healthAdjustment.Restore(StatsUtils.GetStatBaseValueFloat<Health>(game.Player), 1, v => StatsUtils.SetStatFloat<Health>(game.Player, v));
            if (game.Health != null) game.Health.currentHealth = Math.Min(game.Health.currentHealth, game.Health.maxHealth);
        }
        if (energyAdjustment.Active && game.Energy != null)
        {
            energyAdjustment.Restore(StatsUtils.GetStatBaseValueInt<EnergyMaxValue>(game.Player), 1, v => StatsUtils.SetStatInt<EnergyMaxValue>(game.Player, (int)v));
            game.Energy.SetEnergy(Math.Min(game.Energy.energyCurrentValueStat, game.Energy.energyMaxValueStat), true);
        }
    }

    public void SetGodMode(bool enabled)
    {
        game.RequireReady();
        if (enabled && !game.Player.HasComponentData<GodModeFlag>())
        {
            // Same native component operation as ToggleGodModeCommand, scoped to this binding.
            game.Player.AddComponentData<GodModeFlag>();
            ownsGodFlag = game.Player.HasComponentData<GodModeFlag>();
            if (!ownsGodFlag) throw new InvalidOperationException(MenuText.Get("原生无敌开关没有生效"));
        }
        else if (!enabled)
        {
            ReleaseGodMode();
        }
        GodMode = enabled;
    }

    public void SetInfiniteEnergy(bool enabled)
    {
        game.RequireReady();
        if (enabled && !EnergyPatchAvailable) throw new InvalidOperationException(MenuText.Get("无限能量接口未能挂接，请查看日志"));
        if (enabled && game.Energy == null) throw new InvalidOperationException(MenuText.Get("能量组件不存在"));
        InfiniteEnergyTarget = enabled ? game.Energy.Pointer : IntPtr.Zero;
        InfiniteEnergy = enabled;
        if (enabled) RefillEnergy();
    }

    public void TriggerWind()
    {
        game.RequireReady();
        var wind = game.Wind;
        if (wind == null || wind.config == null || wind.config.affectData == null || game.Affects == null)
            throw new InvalidOperationException(MenuText.Get("风神之息组件尚未就绪"));
        var ctx = OperationContextUtils.CreateContextSourceAndTargetEntity(game.Player, game.Player);
        try
        {
            // Use the original scoring path so activation-related perks and UI receive native events.
            wind.AddScoreFromEffect(ctx, 1000f, default);
            if (!game.Affects.HasAffect(wind.config.affectData))
                game.Affects.SetAffectData(wind.config.affectData, game.Player, Math.Max(1, wind.config.affectDuration), ctx, false);
        }
        finally { ctx.Release(); }
        if (!game.Affects.HasAffect(wind.config.affectData)) throw new InvalidOperationException(MenuText.Get("当前状态无法激活风神之息"));
    }

    public void SetHoldWind(bool enabled)
    {
        if (enabled) TriggerWind();
        HoldWind = enabled;
        // On disable, do not remove a naturally earned/shared effect: its original duration resumes.
    }

    public void Tick()
    {
        if (!game.Ready) { InfiniteEnergyTarget = IntPtr.Zero; return; }
        if (InfiniteEnergy && game.Energy != null) InfiniteEnergyTarget = game.Energy.Pointer;
        if (HoldWind)
        {
            var effect = game.Wind?.config?.affectData;
            if (effect == null) { HoldWind = false; return; }
            if (!game.Affects.HasAffect(effect)) TriggerWind();
            else if (game.Affects.GetRemainingTime(effect) < Math.Max(1, game.Wind.config.affectDuration) / 2)
                game.Affects.SetDuration(effect, Math.Max(1, game.Wind.config.affectDuration), false, false);
        }
    }

    private void ForgetPlayer()
    {
        InfiniteEnergyTarget = IntPtr.Zero;
        InfiniteEnergy = GodMode = HoldWind = ownsGodFlag = false;
        healthAdjustment.Clear();
        energyAdjustment.Clear();
    }

    public void Reset()
    {
        StopContinuous();
        if (game.Player == null || game.Player.destroyed)
        {
            ForgetPlayer();
            return;
        }
        // Inactive and dying actors still own their native flags/stats.
        ReleaseGodMode();
        RestoreMaximumsCore();
        if (HasAdjustments) throw new InvalidOperationException(MenuText.Get("原角色的上限恢复尚未完成，将保留记录以便重试"));
        ForgetPlayer();
    }

    public void StopContinuous()
    {
        InfiniteEnergyTarget = IntPtr.Zero;
        InfiniteEnergy = HoldWind = false;
    }

    private void ReleaseGodMode()
    {
        if (ownsGodFlag)
        {
            game.Player.RemoveComponentDataIfAny<GodModeFlag>();
            ownsGodFlag = false;
        }
        GodMode = false;
    }
}

[HarmonyPatch(typeof(EnergyComponent), nameof(EnergyComponent.HasInfiniteEnergy))]
internal static class InfiniteEnergyPatch
{
    private static void Postfix(EnergyComponent __instance, ref bool __result)
    {
        if (GameplayActions.InfiniteEnergyTarget != IntPtr.Zero && __instance.Pointer == GameplayActions.InfiniteEnergyTarget) __result = true;
    }
}
