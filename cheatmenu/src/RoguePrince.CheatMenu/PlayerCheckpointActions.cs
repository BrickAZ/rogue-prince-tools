// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using RoguePrince.CheatMenu.Core;
using UnityEngine;

namespace RoguePrince.CheatMenu;

// A living player's local position/vitals checkpoint, never a world or save snapshot.
internal sealed class PlayerCheckpointActions
{
    private readonly GameSession game;
    private sealed class State
    {
        internal readonly TrainingScope Scope;
        internal readonly long Health,Energy,Context;
        internal readonly Vector3 Position;
        internal readonly float Hp,MaxHp;
        internal readonly int EnergyValue,MaxEnergy;
        internal State(TrainingScope scope,long health,long energy,long context,Vector3 position,float hp,float maxHp,int energyValue,int maxEnergy)
        {Scope=scope;Health=health;Energy=energy;Context=context;Position=position;Hp=hp;MaxHp=maxHp;EnergyValue=energyValue;MaxEnergy=maxEnergy;}
        internal bool Same(State other)=>Scope==other.Scope&&Health==other.Health&&Energy==other.Energy&&Context==other.Context
            && Position.Equals(other.Position)&&Hp==other.Hp&&MaxHp==other.MaxHp&&EnergyValue==other.EnergyValue&&MaxEnergy==other.MaxEnergy;
    }
    private State checkpoint,recovery;
    internal Func<bool> CanMutate = () => true;
    internal bool HasCheckpoint => checkpoint != null;
    internal bool RecoveryPending => recovery != null;
    internal string Status {get;private set;} = "";
    internal bool Error {get;private set;}
    internal string Summary => checkpoint == null ? MenuText.Get("尚未记录角色检查点") :
        MenuText.Format("生命 {0:0.##}/{1:0.##} · 能量 {2}/{3} · 位置 ({4:0.##}, {5:0.##})",
            checkpoint.Hp,checkpoint.MaxHp,checkpoint.EnergyValue,checkpoint.MaxEnergy,checkpoint.Position.x,checkpoint.Position.y);
    internal PlayerCheckpointActions(GameSession game) => this.game=game;
    private string Report(string value,bool error=false)
    {Status=value;Error=error;Plugin.LogEvent("player_checkpoint "+value);return value;}
    private void Event(string kind,State state) => Plugin.LogEvent($"player_checkpoint_{kind} player={state.Scope.Player} generation={state.Scope.Generation} scene={state.Scope.Scene} room={state.Scope.Room} roomX={state.Scope.RoomX} roomY={state.Scope.RoomY} health={state.Hp} energy={state.EnergyValue}");
    private bool Bound(State state) => game.Ready && game.Health!=null && float.IsFinite(game.Health.currentHealth) && game.Health.currentHealth>0
        && state.Scope.Matches(TrainingActions.ReadScope(game))
        && game.PlayerPointer.ToInt64()==state.Scope.Player
        && game.Health?.Pointer.ToInt64()==state.Health && game.Energy?.Pointer.ToInt64()==state.Energy
        && game.Player.GetComponent<Damageable>()?.Pointer==game.Health.Pointer
        && game.Player.GetComponent<EnergyComponent>()?.Pointer==game.Energy.Pointer
        && game.Player.context?.Pointer.ToInt64()==state.Context;
    private void Guard(State state,bool requireFocus=false)
    {
        if(requireFocus&&!Application.isFocused || !Bound(state) || game.Player.context.IsPaused() || !game.Energy.isInitialized
            || game.Health.maxHealth!=state.MaxHp || game.Energy.energyMaxValueStat!=state.MaxEnergy)
            throw new InvalidOperationException(MenuText.Get("检查点角色、组件、房间或生命能量上限已变化"));
    }
    private void SafePosition(Vector3 position)
        => TrainingActions.ValidatePosition(game,game.Player.GetComponent<RoomStateComponent>()?.currentRoom,position);
    private State Read()
    {
        game.RequireReady();
        if(!Application.isFocused || !CanMutate() || !TrainingActions.ReadScope(game).Valid || game.Player.context==null || game.Player.context.IsPaused())
            throw new InvalidOperationException(MenuText.Get("检查点需要存活角色、安全平地、焦点及空闲操作"));
        var health=game.Health;var energy=game.Energy;
        if(health==null||energy==null||!energy.isInitialized||!float.IsFinite(health.currentHealth)||!float.IsFinite(health.maxHealth)
            || health.currentHealth<=0||health.maxHealth<=0||health.currentHealth>health.maxHealth
            || energy.energyMaxValueStat<=0||energy.energyCurrentValueStat<0||energy.energyCurrentValueStat>energy.energyMaxValueStat)
            throw new InvalidOperationException(MenuText.Get("检查点生命或能量数值无效"));
        var platform=game.Player.GetComponent<PlatformStateComponent>();
        if(platform==null||!platform.currentPlatform.isValid())throw new InvalidOperationException(MenuText.Get("检查点需要存活角色、安全平地、焦点及空闲操作"));
        var state=new State(TrainingActions.ReadScope(game),health.Pointer.ToInt64(),energy.Pointer.ToInt64(),game.Player.context.Pointer.ToInt64(),
            game.Player.transform.position,health.currentHealth,health.maxHealth,energy.energyCurrentValueStat,energy.energyMaxValueStat);
        Guard(state);SafePosition(state.Position);return state;
    }
    internal string Capture()
    {
        if(RecoveryPending)throw new InvalidOperationException(MenuText.Get("先重试请求前角色状态恢复"));
        checkpoint=Read();Event("captured",checkpoint);
        return Report(MenuText.Get("已记录同房间角色位置、生命和能量"));
    }
    internal string Restore(Action release)
    {
        if(checkpoint==null||RecoveryPending)throw new InvalidOperationException(MenuText.Get("尚无可恢复检查点或存在待恢复请求"));
        var target=checkpoint;var before=Read();Guard(target);SafePosition(target.Position);
        release();
        if(!Read().Same(before) || !float.IsFinite(Time.timeScale) || Time.timeScale<=0)
            throw new InvalidOperationException(MenuText.Get("释放菜单后角色状态变化；未恢复检查点"));
        Guard(target);SafePosition(target.Position);
        recovery=before; // Retain the source before the first native mutation.
        try
        {
            Apply(target,true);recovery=null;Event("restored",target);
            return Report(MenuText.Get("角色检查点恢复回读通过；世界与战斗状态保持当前进度"));
        }
        catch(Exception failure)
        {
            if(!Application.isFocused)checkpoint=null;
            try
            {
                var restored=Recover();
                if(restored)Report(MenuText.Get("检查点恢复失败；请求前角色状态已恢复"),true);
            }
            catch(Exception rollback)
            {
                Report(MenuText.Get("角色状态部分恢复；保留请求前恢复记录，请重试"),true);
                throw new AggregateException(Status,failure,rollback);
            }
            throw new InvalidOperationException(Status,failure);
        }
    }
    private void Apply(State state,bool requireFocus=false)
    {
        Guard(state,requireFocus);SafePosition(state.Position);
        TeleportExtensions.Teleport(game.Player,state.Position,true);
        Guard(state,requireFocus);
        if(!PositionMatches(state.Position))
            throw new InvalidOperationException(MenuText.Get("检查点位置回读失败"));
        // false suppresses the native energy-change situation triggers.
        game.Energy.SetEnergy(state.EnergyValue,false);Guard(state,requireFocus);
        game.Health.currentHealth=state.Hp;Guard(state,requireFocus);
        if(!float.IsFinite(game.Health.currentHealth)||Math.Abs(game.Health.currentHealth-state.Hp)>.001f
            || game.Energy.energyCurrentValueStat!=state.EnergyValue||!PositionMatches(state.Position))
            throw new InvalidOperationException(MenuText.Get("检查点位置或生命能量回读失败"));
    }
    private bool PositionMatches(Vector3 target)
    {
        var distance=Vector3.Distance(game.Player.transform.position,target);
        return float.IsFinite(distance)&&distance<=.01f;
    }
    private bool Recover()
    {
        if(recovery==null)return true;
        if(!Bound(recovery))
        {
            Event("recovery_retired",recovery);recovery=null;checkpoint=null;
            Report(MenuText.Get("角色或房间已离开；已停止旧状态恢复，已发生的变化不会撤销"),true);return false;
        }
        Apply(recovery);Event("recovered",recovery);recovery=null;return true;
    }
    internal string RetryRecovery()
    {
        if(Recover())return Report(MenuText.Get("请求前角色状态恢复回读通过"));
        return Status;
    }
    internal void Observe(bool loading)
    {
        if(checkpoint==null&&recovery==null)return;
        if(recovery!=null&&!Bound(recovery)){Recover();return;}
        if(checkpoint==null)return;
        if(loading||!Application.isFocused||!Bound(checkpoint)||game.Health.maxHealth!=checkpoint.MaxHp||game.Energy.energyMaxValueStat!=checkpoint.MaxEnergy)
        {Event("invalidated",checkpoint);checkpoint=null;Report(MenuText.Get("角色检查点已失效：作用域、上限、加载或焦点变化"),true);}
    }
    internal string Clear()
    {
        if(RecoveryPending)throw new InvalidOperationException(MenuText.Get("先重试请求前角色状态恢复"));
        checkpoint=null;return Report(MenuText.Get("已清除角色检查点"));
    }
    internal void Reset(bool rebind=false)
    {
        if(checkpoint==null&&recovery==null)return;
        try{Recover();}finally{checkpoint=null;}
    }
}
