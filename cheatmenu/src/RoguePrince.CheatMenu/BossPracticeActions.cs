// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using RoguePrince.CheatMenu.Core;
using UnityEngine;
namespace RoguePrince.CheatMenu;

// Observes sequential native transitions on the selected instance or a rebuilt arena.
// No direct phase or HP writes; backwards selection requires the existing rebuild path.
internal sealed class BossPracticeActions
{
 private readonly GameSession game;private readonly SceneActions scenes;private readonly BossPhaseActions phases;
 private long flowId,oldLevel,run,oldBoss;private int seed,target,stablePhase=-1;
 private string state,kind;private float deadline,nextProbe,stableSince=float.NaN;
 private BossPhaseActions.Entry pending;private BossPhaseBinding binding;private bool waitingAdvance,currentInstance;
 internal bool Busy{get;private set;}
 internal bool AwaitingReload=>Busy&&pending==null;
 internal string Status{get;private set;}="";internal bool Error{get;private set;}
 internal BossPracticeActions(GameSession game,SceneActions scenes,BossPhaseActions phases){this.game=game;this.scenes=scenes;this.phases=phases;}
 private string Report(string text,bool error=false){Status=text;Error=error;Plugin.LogEvent("boss_practice "+text);return text;}
 internal string Start(BossPhaseActions.Entry entry,int phase,Action release)
 {
  if(Busy||scenes.Busy||phases.Busy||phase is <1 or >3)throw new InvalidOperationException(MenuText.Get("阶段练习请求无效或已有请求，未执行"));
  var origin=phases.Inspect(entry);var flow=SceneActions.Flow();
  if(!Application.isFocused||!flow.IsCurrentStateLoaded()||!entry.Health.isAlive||entry.Health.onDieActionStarted||!entry.Component.brainInitDone||entry.Boss.context.IsPaused()||entry.Component.currentPhaseIndex is <1 or >3)
   throw new InvalidOperationException(MenuText.Get("Boss 尚未初始化、已死亡或 AI 暂停，未执行"));
  var arenas=scenes.Bosses().Where(a=>a.Key==origin.State).ToArray();
  if(arenas.Length!=1)throw new InvalidOperationException(MenuText.Get("请进入正式 Boss 场地，未执行"));
  flowId=origin.Flow;oldLevel=origin.Level;run=origin.Run;seed=origin.Seed;state=origin.State;oldBoss=origin.Boss;kind=entry.Kind;target=phase;
  pending=null;binding=default;waitingAdvance=false;currentInstance=false;stablePhase=-1;stableSince=float.NaN;nextProbe=0;deadline=Time.realtimeSinceStartup+120;Busy=true;
  try{scenes.LoadBoss(arenas[0],release);}
  catch(Exception e){Plugin.LogEvent("boss_practice_load_uncertain "+e);return Report(MenuText.Get("场地请求结果不确定；保留观察，不会重新加载"),true);}
  return Report(MenuText.Format("正在重建 {0} 场地，目标阶段 {1}",kind,target));
 }
 internal string StartCurrent(BossPhaseActions.Entry entry,int phase,Action release)
 {
  if(Busy||scenes.Busy||phases.Busy||phase is <1 or >3)throw new InvalidOperationException(MenuText.Get("阶段练习请求无效或已有请求，未执行"));
  var origin=phases.Inspect(entry);var originalPhase=entry.Component.currentPhaseIndex;
  if(phase<originalPhase)throw new InvalidOperationException(MenuText.Get("原实例不能倒退阶段；请使用重建到所选阶段"));
  void Validate()
  {
   var flow=SceneActions.Flow();
   if(phases.Inspect(entry)!=origin||flow==null||!flow.IsCurrentStateLoaded()||scenes.Busy||!entry.Health.isAlive||entry.Health.onDieActionStarted||!entry.Component.brainInitDone||entry.Boss.context.IsPaused()||entry.Component.currentPhaseIndex!=originalPhase||originalPhase is <1 or >3)
    throw new InvalidOperationException(MenuText.Get("Boss 尚未初始化、已死亡或 AI 暂停，未执行"));
  }
  Validate();release();Validate();
  if(!float.IsFinite(Time.timeScale)||Time.timeScale<=0||!float.IsFinite(Time.realtimeSinceStartup))throw new InvalidOperationException(MenuText.Get("游戏仍被其他界面暂停，请先关闭原生暂停菜单"));
  flowId=origin.Flow;oldLevel=origin.Level;run=origin.Run;seed=origin.Seed;state=origin.State;oldBoss=origin.Boss;kind=entry.Kind;target=phase;
  pending=entry;binding=origin;waitingAdvance=false;currentInstance=true;stablePhase=-1;stableSince=float.NaN;nextProbe=0;deadline=Time.realtimeSinceStartup+120;Busy=true;
  Plugin.LogEvent($"boss_practice_current_begin boss={origin.Boss} level={origin.Level} from={originalPhase} target={phase}");
  return Report(MenuText.Format("原实例 {0} 正在推进到阶段 {1}；场地不重建",kind,target));
 }
 private string End(string reason,bool success=false)
 {
  if(waitingAdvance&&phases.Busy)phases.Cancel();
  Plugin.LogEvent($"boss_practice_end mode={(currentInstance?"current":"rebuild")} bound={pending!=null} boss={binding.Boss} level={binding.Level} originBoss={oldBoss} originLevel={oldLevel} target={target} success={success}");
  Busy=false;pending=null;waitingAdvance=false;stableSince=float.NaN;
  return Report(reason,!success);
 }
 internal void Tick()
 {
  if(!Busy)return;
  try
  {
   var now=Time.realtimeSinceStartup;var flow=SceneActions.Flow();var level=SceneActions.Level(flow);
   if(!Application.isFocused||flow==null||flow.Pointer.ToInt64()!=flowId||RunTools.RunId!=run||flow.GetSeed()!=seed||(!flow.isLoading&&flow.GetCurrentState()?.guid!=state))
   {End(MenuText.Get("阶段练习作用域已变化；自动流程停止"));return;}
   if(!float.IsFinite(now)||now>=deadline){End(MenuText.Get("阶段练习超时；未重复加载或发送伤害"));return;}
   if(pending==null)
   {
    if(flow.isLoading||scenes.Busy)return;
    if(scenes.Error){End(MenuText.Get("场地重建未确认；阶段练习停止"));return;}
    if(level==null||level.Pointer.ToInt64()==oldLevel||!game.Ready||!flow.IsCurrentStateLoaded()||!TrainingActions.ReadScope(game).Valid)return;
    if(now<nextProbe)return;nextProbe=now+.5f;
    phases.Refresh();var matches=phases.Entries.Where(e=>e.Kind==kind&&e.Key!=oldBoss).ToArray();
    if(matches.Length>1){End(MenuText.Get("新场地 Boss 不唯一；未发送阶段伤害"));return;}
    if(matches.Length==0)return;
    pending=matches[0];binding=phases.Inspect(pending);
    if(binding.Flow!=flowId||binding.Level!=level.Pointer.ToInt64()||binding.State!=state||binding.Run!=run||binding.Seed!=seed)
    {End(MenuText.Get("阶段练习作用域已变化；自动流程停止"));return;}
    Report(MenuText.Format("已绑定新 {0}，等待原生转换结束",kind));
   }
   if(flow.isLoading||!game.Ready||phases.Inspect(pending)!=binding||!pending.Health.isAlive||pending.Health.onDieActionStarted)
   {End(MenuText.Get("阶段练习作用域已变化；自动流程停止"));return;}
   if(waitingAdvance)
   {
    if(phases.Busy)return;
    if(phases.Result!="confirmed"){End(MenuText.Get("原生阶段推进未确认；自动流程停止"));return;}
    waitingAdvance=false;stableSince=float.NaN;
   }
   var phase=pending.Component.currentPhaseIndex;
   if(phase<0||phase>target){End(MenuText.Get("阶段索引不符合目标；自动流程停止"));return;}
   if(phase==0||Time.timeScale<=0||!phases.PracticeReady(pending)){stableSince=float.NaN;return;}
   if(stablePhase!=phase||float.IsNaN(stableSince)){stablePhase=phase;stableSince=now;return;}
   if(now-stableSince<.25f)return;
   if(phase==target)
   {End(MenuText.Format("已观察到目标阶段 {0}，原生转换动作已退出",target),true);return;}
   waitingAdvance=true;stableSince=float.NaN;
   try{phases.Request(pending,()=>{});}
   catch(Exception e)
   {
    Plugin.LogEvent("boss_practice_phase_uncertain "+e);
    if(!phases.Busy){End(MenuText.Get("原生阶段推进未确认；自动流程停止"));return;}
    Report(MenuText.Get("阶段请求结果不确定；保留观察，不重复伤害"),true);return;
   }
   Report(MenuText.Format("正在等待原生阶段 {0} → {1}",phase,phase+1));
  }
  catch(Exception e){Plugin.LogEvent("boss_practice_read_failed "+e);End(MenuText.Get("阶段练习读取失败；自动流程停止"));}
 }
 internal string Cancel()=>End(MenuText.Get(currentInstance?"已停止原实例推进；已发生的伤害不会撤销":"已停止阶段练习；已发生的重建与伤害不会撤销"));
 internal void Restore(bool rebind){if(Busy&&!(rebind&&AwaitingReload))Cancel();}
}
