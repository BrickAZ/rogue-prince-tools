// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
// Additional game linking permission: see GAME-LINKING-EXCEPTION.txt.

using RoguePrince.CheatMenu.Core;
using MotherBase.EntityFramework;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace RoguePrince.CheatMenu;

internal sealed class GameSession
{
    public PlayerSaver Saver { get; private set; }
    public Entity Player { get; private set; }
    public Damageable Health { get; private set; }
    public EnergyComponent Energy { get; private set; }
    public GoldHolderComponent Gold { get; private set; }
    public AffectableComponent Affects { get; private set; }
    public SpeedBoostComponent Wind { get; private set; }
    public MetaProgressionManager Meta { get; private set; }
    public long Generation { get; private set; }
    public bool Ready => Player != null && !Player.destroyed && Player.gameObject.activeInHierarchy && Health != null && Health.isAlive && !Health.onDieActionStarted;
    public string Status => Ready ? MenuText.Get("角色已就绪") : MenuText.Get("等待存活角色（请进入存档）");
    public IntPtr PlayerPointer => Player != null ? Player.Pointer : IntPtr.Zero;

    public bool Refresh(Action beforeRebind)
    {
        if (Saver == null) Saver = UObject.FindObjectOfType<PlayerSaver>();
        if (Meta == null) Meta = UObject.FindObjectOfType<MetaProgressionManager>();
        Entity next = null;
        if (Saver != null && Saver.mainCharacterVariable != null) next = Saver.mainCharacterVariable.CurrentValue;
        if (next != null && next.destroyed) next = null;
        var oldPtr = Player != null ? Player.Pointer : IntPtr.Zero;
        var newPtr = next != null ? next.Pointer : IntPtr.Zero;
        if (newPtr == oldPtr && (next == null || Health != null)) return false;
        // Retain the old binding if cleanup fails, allowing a later retry.
        beforeRebind();
        var health = next != null ? next.GetComponent<Damageable>() : null;
        var energy = next != null ? next.GetComponent<EnergyComponent>() : null;
        var gold = next != null ? next.GetComponent<GoldHolderComponent>() : null;
        var affects = next != null ? next.GetComponent<AffectableComponent>() : null;
        var wind = next != null ? next.GetComponent<SpeedBoostComponent>() : null;
        // Commit only after all native reads succeed; never mix two players' components.
        Player = next;
        Health = health;
        Energy = energy;
        Gold = gold;
        Affects = affects;
        Wind = wind;
        Generation++;
        Plugin.LogEvent($"player_binding generation={Generation} ready={Ready}");
        return true;
    }

    public void RequireReady()
    {
        if (!Ready) throw new InvalidOperationException(MenuText.Get("角色未就绪，操作未执行"));
    }
}
