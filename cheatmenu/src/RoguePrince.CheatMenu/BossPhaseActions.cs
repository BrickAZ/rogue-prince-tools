// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using MotherBase.EntityFramework;
using MotherBase.ActionFramework;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
using UObject=UnityEngine.Object;
namespace RoguePrince.CheatMenu;
internal sealed class BossPhaseActions
{
 internal sealed class Entry
 {
  internal string Kind;internal Entity Boss;internal BossComponentBase Component;internal Damageable Health;
  internal long Brain;internal Func<bool> SameComponent,DebugReady;internal Func<float> Ratio;internal Action Execute;
  internal BossPhaseBinding Binding;internal long Key=>Boss.Pointer.ToInt64();
  internal string Label=>MenuText.Format("{0} · 原生阶段 {1} · 生命 {2}/{3}",Kind,Component.currentPhaseIndex,Health.currentHealth.ToString("0.0"),Health.maxHealth.ToString("0.0"));
 }
 private readonly GameSession game;private readonly SceneActions scenes;private readonly BossPhaseGate gate=new();
 private Entry pending;private int fromPhase;
 internal List<Entry> Entries{get;private set;}=new();
 internal bool Busy=>gate.Pending;
 internal string Status{get;private set;}="";internal bool Error{get;private set;}
 internal string Result{get;private set;}="idle";
 internal BossPhaseActions(GameSession game,SceneActions scenes){this.game=game;this.scenes=scenes;}
 private string Report(string value,bool error=false){Status=value;Error=error;Plugin.LogEvent("boss_phase "+value);return value;}
 private BossPhaseBinding ReadBinding(Entry entry)
 {
  game.RequireReady();var scope=TrainingActions.ReadScope(game);var flow=SceneActions.Flow();var level=SceneActions.Level(flow);
  if(!Application.isFocused||!scope.Valid||flow==null||flow.isLoading||level==null)throw new InvalidOperationException(MenuText.Get("目标 Boss、脑组件或房间已变化，未执行"));
  if(!scenes.Bosses().Any(x=>x.Key==flow.GetCurrentState()?.guid))throw new InvalidOperationException(MenuText.Get("请进入正式 Boss 场地，未执行"));
  var e=entry.Boss;var c=entry.Component;
  if(e==null||e.destroyed||!e.gameObject.activeInHierarchy||e.gameObject.scene.handle!=scope.Scene||c==null||!entry.SameComponent()||e.context==null||game.Player.context==null||e.context.Pointer!=game.Player.context.Pointer||e.GetComponent<MobComponent>()?.isBoss!=true||e.GetComponent<BSAIInput>()?.brain?.Pointer.ToInt64()!=entry.Brain||e.GetComponent<Damageable>()?.Pointer!=entry.Health?.Pointer)
   throw new InvalidOperationException(MenuText.Get("目标 Boss、脑组件或房间已变化，未执行"));
  var room=e.GetComponent<RoomStateComponent>()?.currentRoom;
  if(room==null){var map=SceneActions.Map(flow);if(map==null||!map.FindRoomAtCoord(e.transform.position,out room))throw new InvalidOperationException(MenuText.Get("目标 Boss、脑组件或房间已变化，未执行"));}
  if(!MobRoomIdentity.Matches(game.Player.GetComponent<RoomStateComponent>()?.currentRoom,room))throw new InvalidOperationException(MenuText.Get("目标 Boss、脑组件或房间已变化，未执行"));
  return new(scope,RunTools.RunId,e.Pointer.ToInt64(),c.Pointer.ToInt64(),entry.Brain,flow.Pointer.ToInt64(),level.Pointer.ToInt64(),flow.GetCurrentState().guid,flow.GetSeed());
 }
 private Entry Make(string kind,Entity e,BossComponentBase c,long brain,Func<bool> same,Func<float> ratio,Func<bool> debug,Action execute)
 {var entry=new Entry{Kind=kind,Boss=e,Component=c,Brain=brain,Health=e.GetComponent<Damageable>(),SameComponent=same,Ratio=ratio,DebugReady=debug,Execute=execute};entry.Binding=ReadBinding(entry);return entry;}
 private Entry ReadEntry(BossComponentBase component)
 {
  var e=component.GetComponent<Entity>();if(e==null)return null;var brain=e.GetComponent<BSAIInput>()?.brain;if(brain==null)return null;
  if(component.TryCast<AltanBossComponent>() is {} a&&brain.TryCast<AltanBossBrain>() is {} ab)
   return Make("Altan",e,a,ab.Pointer.ToInt64(),()=>e.GetComponent<AltanBossComponent>()?.Pointer==a.Pointer,()=>ab.GetLifeRatioToNextPhase(a),()=>DebugService.TryGetDebugItemStatic<AltanDebugWindow>(out var debug)&&debug?.bossEntityVariable?.CurrentValue?.Pointer==e.Pointer,()=>AltanDebugWindow.SkipToNextFightPhase(e,a,ab));
  if(component.TryCast<BaatarBossComponent>() is {} b&&brain.TryCast<BaatarBossBrain>() is {} bb)
   return Make("Baatar",e,b,bb.Pointer.ToInt64(),()=>e.GetComponent<BaatarBossComponent>()?.Pointer==b.Pointer,()=>b.currentPhaseIndex==1?bb.lifeRatioPhase1:bb.lifeRatioPhase2,()=>DebugService.TryGetDebugItemStatic<BaatarDebugWindow>(out var debug)&&debug?.bossEntityVariable?.CurrentValue?.Pointer==e.Pointer,()=>BaatarDebugWindow.SkipToNextFightPhase(e,b,bb));
  if(component.TryCast<GoatBossComponent>() is {} g&&brain.TryCast<GoatBossBrain>() is {} gb)
   return Make("Berude",e,g,gb.Pointer.ToInt64(),()=>e.GetComponent<GoatBossComponent>()?.Pointer==g.Pointer,()=>g.currentPhaseIndex==1?gb.lifeRatioPhaseTuto:gb.lifeRatioPhase2,()=>DebugService.TryGetDebugItemStatic<BerudeDebugWindow>(out var debug)&&debug?.bossEntityVariable?.CurrentValue?.Pointer==e.Pointer,()=>BerudeDebugWindow.SkipToNextFightPhase(e,g,gb));
  if(component.TryCast<CorruptedNogaiBossComponent>() is {} n&&brain.TryCast<CorruptedNogaiBrain>() is {} nb)
   return Make("CorruptedNogai",e,n,nb.Pointer.ToInt64(),()=>e.GetComponent<CorruptedNogaiBossComponent>()?.Pointer==n.Pointer,()=>n.currentPhaseIndex==1?nb.lifeRatioPhase1:nb.lifeRatioPhase2,()=>DebugService.TryGetDebugItemStatic<CorruptedNogaiDebugWindow>(out var debug)&&debug?.bossEntityVariable?.CurrentValue?.Pointer==e.Pointer,()=>CorruptedNogaiDebugWindow.SkipToNextFightPhase(e,n,nb));
  if(component.TryCast<KaaliBossComponent>() is {} k&&brain.TryCast<KaaliBossBrain>() is {} kb)
   return Make("Kaali",e,k,kb.Pointer.ToInt64(),()=>e.GetComponent<KaaliBossComponent>()?.Pointer==k.Pointer,()=>k.currentPhaseIndex==1?kb.lifeRatioPhase1:kb.lifeRatioPhase2,()=>DebugService.TryGetDebugItemStatic<KaaliDebugWindow>(out var debug)&&debug?.bossEntityVariable?.CurrentValue?.Pointer==e.Pointer,()=>KaaliDebugWindow.SkipToNextFightPhase(e,k,kb));
  if(component.TryCast<NogaiBossComponent>() is {} o&&brain.TryCast<NogaiBossBrain>() is {} ob)
   return Make("Nogai",e,o,ob.Pointer.ToInt64(),()=>e.GetComponent<NogaiBossComponent>()?.Pointer==o.Pointer,()=>o.currentPhaseIndex==1?ob.lifeRatioPhase1:ob.lifeRatioPhase2,()=>DebugService.TryGetDebugItemStatic<NogaiDebugWindow>(out var debug)&&debug?.bossEntityVariable?.CurrentValue?.Pointer==e.Pointer,()=>NogaiDebugWindow.SkipToNextFightPhase(e,o,ob));
  return null;
 }
 internal string Refresh()
 {
  if(Busy)throw new InvalidOperationException(MenuText.Get("阶段请求尚在观察或目标已失效"));
  var entries=new List<Entry>();
  foreach(var component in UObject.FindObjectsOfType<BossComponentBase>())
   try{var entry=ReadEntry(component);if(entry!=null)entries.Add(entry);}catch(InvalidOperationException){}
  Entries=entries.GroupBy(x=>x.Key).Select(x=>x.First()).ToList();return Report(MenuText.Get(Entries.Count==0?"当前房间没有支持的原生 Boss":"刷新当前 Boss"));
 }
 internal string Request(Entry entry,Action release)
 {
  if(Busy||entry==null||!Entries.Contains(entry))throw new InvalidOperationException(MenuText.Get("阶段请求尚在观察或目标已失效"));
  void Validate()
  {
   if(ReadBinding(entry)!=entry.Binding)throw new InvalidOperationException(MenuText.Get("目标 Boss、脑组件或房间已变化，未执行"));
   if(!entry.Health.isAlive||entry.Health.onDieActionStarted||!entry.Component.brainInitDone||entry.Boss.context.IsPaused()||entry.Boss.GetComponent<AIStateComponent>()?.stayIdle!=false)throw new InvalidOperationException(MenuText.Get("Boss 尚未初始化、已死亡或 AI 暂停，未执行"));
   BossPhasePolicy.TargetHealth(entry.Component.currentPhaseIndex,entry.Health.maxHealth,entry.Health.currentHealth,entry.Ratio(),entry.Component.brainInitDone,entry.Component.reachedPhaseThreshold);
   if(!entry.DebugReady())throw new InvalidOperationException(MenuText.Get("原生 Boss 调试入口未就绪，未执行"));
  }
  Validate();release();Validate();if(Time.timeScale<=0)throw new InvalidOperationException(MenuText.Get("游戏仍被其他界面暂停，请先关闭原生暂停菜单"));
  fromPhase=entry.Component.currentPhaseIndex;gate.Begin(entry.Binding,fromPhase,Time.realtimeSinceStartup);pending=entry;Result="pending";
  try{entry.Execute();}catch(Exception e){Report(MenuText.Get("原生阶段请求结果不确定，保留观察；未重复发送伤害"),true);Plugin.LogEvent("boss_phase_dispatch_error "+e);throw;}
  return Report(MenuText.Get("已提交原生阶段伤害，等待 Boss 控制器回读"));
 }
 internal void Tick()
 {
  if(!Busy)return;string result;
  try{result=gate.Observe(ReadBinding(pending),pending.Component.currentPhaseIndex,pending.Health.isAlive&&!pending.Health.onDieActionStarted,Time.realtimeSinceStartup);}
  catch(Exception e){Plugin.LogEvent("boss_phase_observe_error "+e);result="cancelled";gate.Cancel();}
  if(result=="pending")return;
  pending=null;Result=result;
  if(result=="confirmed")Report(MenuText.Format("已观察到原生阶段推进：{0} → {1}",fromPhase,fromPhase+1));
  else Report(MenuText.Get(result=="timeout"?"阶段观察超时；未重复发送伤害":result=="unexpected"?"原生阶段发生非预期变化；未重复发送伤害":"阶段观察已取消；未向新对象写入"),true);
 }
 internal string Cancel(){gate.Cancel();pending=null;Result="cancelled";return Report(MenuText.Get("阶段观察已取消；未向新对象写入"));}
 internal BossPhaseBinding Inspect(Entry entry)
 {
  if(entry==null||!Entries.Contains(entry))throw new InvalidOperationException(MenuText.Get("阶段请求尚在观察或目标已失效"));
  return ReadBinding(entry);
 }
 private static bool Settled(Entity entity,ActionData transition,AffectData invincibility)
 {
  var actor=entity.GetComponent<Actor>();var affects=entity.GetComponent<AffectableComponent>();
  return actor!=null&&affects!=null&&transition!=null&&invincibility!=null&&!string.IsNullOrEmpty(invincibility.name)&&!ActorUtils.HasActiveAction(actor,transition)&&!affects.HasAffect(invincibility.name);
 }
 internal bool PracticeReady(Entry entry)
 {
  if(Inspect(entry)!=entry.Binding||!entry.Health.isAlive||entry.Health.onDieActionStarted||!entry.Component.brainInitDone||entry.Component.reachedPhaseThreshold||entry.Boss.context.IsPaused()||entry.Boss.GetComponent<AIStateComponent>()?.stayIdle!=false)return false;
  var e=entry.Boss;var brain=e.GetComponent<BSAIInput>()?.brain;if(brain==null)return false;
  if(brain.TryCast<AltanBossBrain>() is {} a)return Settled(e,a.startPhaseAction,a.bossInvincibleAffect);
  if(brain.TryCast<BaatarBossBrain>() is {} b)return Settled(e,b.startPhaseAction,b.bossInvincibleAffect);
  if(brain.TryCast<GoatBossBrain>() is {} g)return Settled(e,g.goatBossPhase2Action,g.bossInvincibleAffect);
  if(brain.TryCast<CorruptedNogaiBrain>() is {} n)return Settled(e,n.phaseTransitionAction,n.bossInvincibleAffect);
  if(brain.TryCast<KaaliBossBrain>() is {} k)return Settled(e,k.transitionAction,k.bossInvincibleAffect);
  if(brain.TryCast<NogaiBossBrain>() is {} o)return Settled(e,o.phaseTransitionAction,o.bossInvincibleAffect);
  return false;
 }
}
