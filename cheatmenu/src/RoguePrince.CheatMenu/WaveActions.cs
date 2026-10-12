// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using MotherBase.EntityFramework;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
namespace RoguePrince.CheatMenu;
internal sealed class WaveActions
{
    private readonly GameSession game;
    private readonly MobSpawnActions mobs;
    private readonly TrainingActions training;
    private readonly CombatActions combat;
    internal readonly WavePlan Plan=new();
    private TrainingScope scope,dummyScope;
    private MobSpawnOptions options;
    private long request,dummyId;
    private bool awaiting,dummyWasAi,dummyRespawn;
    private Entity dummy;
    private Vector3 anchor;
    private string dummyMob;
    private float next;
    internal bool DummyActive=>dummyId!=0||dummyRespawn;
    internal bool Active=>Plan.Active||awaiting||DummyActive;
    internal string Status {get;private set;}="";
    internal WaveActions(GameSession game,MobSpawnActions mobs,TrainingActions training,CombatActions combat)
    {this.game=game;this.mobs=mobs;this.training=training;this.combat=combat;mobs.Retired+=Retired;}
    private void Retired(long id,bool killed)
    {
        var active=Plan.Active;Plan.Retire(id,killed);
        if(active&&!Plan.Active){var completed=Plan.Completed&&game.Ready&&scope.Matches(TrainingActions.ReadScope(game));Status=MenuText.Get(completed?"全部波次已完成":"波次已停止：目标被清理或失效");combat.Practice.Finish(completed?"complete":game.Ready?"cleared":"death",completed);combat.WaveRecording=false;Plugin.LogEvent("waves_ended completed="+completed);}
        if(id==dummyId){combat.Dummy=IntPtr.Zero;dummyId=0;dummy=null;dummyRespawn=killed;if(!killed)Status=MenuText.Get("木桩已清理，自动恢复已停止");}
    }
    internal string Start(string mob,int waves,int perWave)
    {
        combat.Require();if(Active||training.Busy||mobs.Busy)throw new InvalidOperationException(MenuText.Get("已有训练请求正在执行"));
        var entry=mobs.Filter(false,"").SingleOrDefault(e=>e.InternalName==mob);
        if(entry?.Data.Supported!=true||entry.Data.Boss)throw new InvalidOperationException(MenuText.Get("请先选择普通怪物"));
        scope=TrainingActions.ReadScope(game);options=new(MobSpawnHealthMode.NativeFull,true,mob);Plan.Start(waves,perWave);awaiting=false;next=Time.realtimeSinceStartup;
        combat.MarkPractice("wave start");combat.Practice.Start($"{mob} {waves}x{perWave}",0);combat.WaveRecording=true;return Status=MenuText.Get("怪物波次已开始");
    }
    internal string Stop()
    {
        Plan.Cancel();if(awaiting&&mobs.Busy&&mobs.CurrentRequestId==request)mobs.CancelPending();awaiting=false;
        combat.Practice.Finish("cancel",false);combat.WaveRecording=false;return Status=MenuText.Get("波次已停止，存活目标可在自有怪物页清理");
    }
    internal string SetDummy(long id)
    {
        combat.Require();if(!CombatActions.DamageAvailable||Active||training.Busy||mobs.Busy)throw new InvalidOperationException(MenuText.Get("木桩控制暂不可用"));
        var target=mobs.Targets().SingleOrDefault(t=>t.Id==id&&!t.Boss)??throw new InvalidOperationException(MenuText.Get("请先选择自有普通怪物"));
        dummy=mobs.TargetEntity(id);dummyId=id;dummyWasAi=target.AiEnabled;dummyMob=target.MobId;dummyScope=TrainingActions.ReadScope(game);anchor=dummy.transform.position;
        if(target.AiEnabled)mobs.ToggleAi(id);
        combat.Dummy=dummy.Pointer;mobs.Refill(id);combat.Ledger.Select(dummy.Pointer.ToInt64());combat.Ledger.Reset();combat.Recording=true;
        return Status=MenuText.Get("木桩已固定，受击后恢复生命并统计伤害");
    }
    internal string StopDummy()
    {
        combat.Dummy=IntPtr.Zero;dummyRespawn=false;
        if(dummy!=null&&!dummy.destroyed)mobs.RestoreTargetAi(dummyId,dummy.Pointer,dummyWasAi);
        dummyId=0;dummy=null;return Status=MenuText.Get("木桩模式已关闭");
    }
    internal void Tick(bool visible)
    {
        var current=TrainingActions.ReadScope(game);
        if(Plan.Active&&(!scope.Matches(current)||!Application.isFocused)){Stop();return;}
        if(DummyActive&&(!dummyScope.Matches(current)||!Application.isFocused)){StopDummy();return;}
        if(dummy!=null&&combat.Dummy==IntPtr.Zero){StopDummy();return;}
        if(dummy!=null&&!dummy.destroyed&&combat.Dummy!=IntPtr.Zero&&Vector3.Distance(dummy.transform.position,anchor)>.05f)TeleportExtensions.Teleport(dummy,anchor,true);
        if(awaiting)
        {
            if(mobs.Busy)return;awaiting=false;
            if(mobs.LastSuccessfulRequest!=request||mobs.Error){Stop();dummyRespawn=false;Status=MenuText.Get("训练生成失败，已停止")+" "+mobs.Status;return;}
            if(dummyRespawn){dummyRespawn=false;SetDummy(mobs.LastSuccessfulOwned);return;}
            if(Plan.Active)Plan.Submitted(mobs.LastSuccessfulOwned);
            next=Time.realtimeSinceStartup+.8f;
        }
        if(visible||Time.timeScale<=0||training.Busy||mobs.Busy||Time.realtimeSinceStartup<next||(!Plan.CanSpawn&&!dummyRespawn))return;
        if(mobs.Request(dummyRespawn?new(MobSpawnHealthMode.NativeFull,false,dummyMob):options)){request=mobs.CurrentRequestId;awaiting=true;}
        else if(mobs.Error){Stop();dummyRespawn=false;Status=mobs.Status;}
    }
    internal void Reset(){if(Plan.Active||awaiting)Stop();if(DummyActive)StopDummy();}
}
