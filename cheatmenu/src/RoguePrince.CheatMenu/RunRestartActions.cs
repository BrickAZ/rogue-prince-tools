// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using HarmonyLib;
using MotherBase.ApplicationFlowManager;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
namespace RoguePrince.CheatMenu;
internal sealed class RunRestartActions
{
 internal static RunRestartActions Current;
 internal static bool Available;
 private readonly GameSession game;
 private readonly RunTools tools;
 private readonly RestartGate gate=new();
 private long reservation;
 internal bool Busy=>gate.Busy;
 internal string Status {get;private set;}="";
 internal bool Error {get;private set;}
 internal RunRestartActions(GameSession game,RunTools tools){this.game=game;this.tools=tools;Current=this;}
 private string Report(string text,bool error=false){Status=text;Error=error;Plugin.LogEvent("run_restart "+text);return text;}
 private readonly record struct Origin(long Flow,long Saver,long Level,long Player,long Generation,string State,long Run);
 private Origin ReadOrigin()
 {
  game.RequireReady();var flow=SceneActions.Flow();var level=SceneActions.Level(flow);var state=flow?.GetCurrentState();
  if(!Application.isFocused||flow==null||flow.isLoading||!flow.IsCurrentStateLoaded()||!flow.IsRunActive||!SceneActions.Biome(state)||level==null||game.Saver==null||game.Saver.mainCharacterVariable?.CurrentValue?.Pointer!=game.PlayerPointer)
   throw new InvalidOperationException(MenuText.Get("重开需要当前存活角色与已加载的战斗关卡"));
  return new(flow.Pointer.ToInt64(),game.Saver.Pointer.ToInt64(),level.Pointer.ToInt64(),game.PlayerPointer.ToInt64(),game.Generation,state.guid,RunTools.RunId);
 }
 private static bool StartBiome(ApplicationState state)=>SceneActions.Biome(state)&&state.stateLogic.TryCast<BluesteakApplicationStateLogic>()?.isStartRunBiome==true;
 private static bool HasStartRoute(ApplicationFlowService flow)
 {
  if(flow.profile?.transitions==null)return false;
  foreach(var t in flow.profile.transitions)if(t?.signal!=null&&t.currentState?.stateLogic?.TryCast<BluesteakApplicationStateLogic>()?.isHubBiome==true&&StartBiome(t.nextState))return true;
  return false;
 }
 private static PauseUIView PauseView()
 {
  var views=Resources.FindObjectsOfTypeAll<PauseUIView>().Where(v=>v!=null&&v.gameObject.scene.IsValid()&&v.gameObject.scene.isLoaded&&v.onGiveUpConfirmed!=null).GroupBy(v=>v.Pointer).Select(g=>g.First()).ToArray();
  if(views.Length!=1)throw new InvalidOperationException(MenuText.Get("原生放弃界面缺失或不唯一；未执行"));
  var view=views[0];var e=view.onGiveUpConfirmed;var count=e.GetPersistentEventCount();
  if(count<=0)throw new InvalidOperationException(MenuText.Get("原生放弃事件未绑定；未执行"));
  for(int i=0;i<count;i++)if(e.GetPersistentTarget(i)==null||string.IsNullOrWhiteSpace(e.GetPersistentMethodName(i)))throw new InvalidOperationException(MenuText.Get("原生放弃事件未绑定；未执行"));
  return view;
 }
 internal string Start(string seed,string level,bool overrideDifficulty,Action release)
 {
  if(!Available||!RunTools.Available)throw new InvalidOperationException(MenuText.Get("原生补丁不可用"));
  if(Busy||tools.Pending)throw new InvalidOperationException(MenuText.Get("先完成或取消当前重开及新局预约"));
  var origin=ReadOrigin();var flow=SceneActions.Flow();var view=PauseView();var eventPointer=view.onGiveUpConfirmed.Pointer;
  _=SceneValuePolicy.Seed(seed);if(overrideDifficulty)_=SceneValuePolicy.Difficulty(level,tools.MaxDifficulty());
  if(!HasStartRoute(flow))
   throw new InvalidOperationException(MenuText.Get("未找到原生据点开局路线；未执行"));
  release();
  if(Time.timeScale<=0||ReadOrigin()!=origin||PauseView().Pointer!=view.Pointer||view.onGiveUpConfirmed.Pointer!=eventPointer)throw new InvalidOperationException(MenuText.Get("角色、地图或暂停状态已变化；未放弃本轮"));
  tools.Arm(seed,level,overrideDifficulty);reservation=tools.StartRevision;
  try{gate.Begin(origin.Flow,origin.Saver,origin.Level,origin.State,origin.Run,SceneValuePolicy.Seed(seed),overrideDifficulty?SceneValuePolicy.Difficulty(level,tools.MaxDifficulty()):null,Time.realtimeSinceStartup);}
  catch{tools.CancelStartIfOwned(reservation);throw;}
  try{view.onGiveUpConfirmed.Invoke();}
  catch(Exception e){Report(MenuText.Get("原生放弃调用异常，结果不确定；继续观察且不会重发"),true);Plugin.MenuLog.LogError(e);throw new InvalidOperationException(Status,e);}
  return Report(MenuText.Get("已提交原生放弃；等待返回据点后自动开局"));
 }
 internal void Generated(LevelGeneratorParameters parameters)
 {
  if(!Busy||!gate.StartSubmitted||parameters==null)return;
  var flow=SceneActions.Flow();var state=flow?.FindState(parameters.stateId);
  gate.Generated(state?.guid,parameters.seed,RunTools.RunId,reservation==tools.StartRevision&&reservation==tools.ConsumedStartRevision&&tools.ConsumedStartRunId==RunTools.RunId&&!tools.SeedPending&&flow?.GetSeed()==parameters.seed);
  if(!Busy)End("failed");
 }
 private string End(string result)
 {
  tools.CancelStartIfOwned(reservation);
  return Report(MenuText.Get(result switch{"completed"=>"已观察到新局重置、起始种子与新地图加载","timeout"=>"重开观察超时；已停止自动流程，不会重试","cancelled"=>"重开观察已取消；已经发生的放弃或加载不会撤销",_=>"原生开局或生成回读不匹配；已停止自动流程"}),result!="completed");
 }
 internal void Tick()
 {
  if(!Busy)return;
  try
  {
   var flow=SceneActions.Flow();var state=flow?.GetCurrentState();var logic=state?.stateLogic?.TryCast<BluesteakApplicationStateLogic>();
   if(reservation!=tools.StartRevision){gate.Cancel();End("cancelled");return;}
   var result=gate.Observe(flow?.Pointer.ToInt64()??0,game.Saver?.Pointer.ToInt64()??0,state?.guid,flow?.isLoading==true,logic?.isHubBiome==true,game.Ready&&game.Saver?.mainCharacterVariable?.CurrentValue?.Pointer==game.PlayerPointer&&flow?.IsCurrentStateLoaded()==true&&(logic?.isHubBiome==true||flow.IsRunActive),Application.isFocused,RunTools.RunId,SceneActions.Level(flow)?.Pointer.ToInt64()??0,flow?.GetSeed()??0,Time.realtimeSinceStartup);
   if(result=="pending")return;
   if(result=="hub")
   {
    if(Time.timeScale<=0)return;
    if(flow.IsRunActive)throw new InvalidOperationException(MenuText.Get("据点本轮尚未结束；停止自动开局"));
    var choices=new List<(ApplicationSignal signal,ApplicationState state)>();
    foreach(var transition in flow.profile.transitions)
    {
     var signal=transition?.signal;if(signal==null||choices.Any(c=>c.signal.Pointer==signal.Pointer)||!flow.AcceptsSignal(signal))continue;
     var target=flow.GetNextState(signal);if(!StartBiome(target)||flow.FindState(target.stateID)?.guid!=target.guid)continue;choices.Add((signal,target));
    }
    if(choices.Count!=1)throw new InvalidOperationException(MenuText.Get("据点开局出口缺失或不唯一；停止自动开局"));
    var choice=choices[0];gate.SubmitStart(state.guid,choice.state.guid);
    // Mark before invocation: native load/generation may run synchronously or throw after a side effect.
    try{flow.SendSignal(choice.signal);if(Busy)Report(MenuText.Get("已提交一次原生开局；等待新局生成确认"));}
    catch(Exception e){Plugin.MenuLog.LogError(e);if(Busy)Report(MenuText.Get("原生开局调用异常，继续观察且不会重发"),true);}
    return;
   }
   if(result=="completed"&&gate.Difficulty.HasValue&&RuntimeServiceLocator.Get<ItemProgressionManagementService>()?.GetAwakeningLevel()!=gate.Difficulty.Value)result="failed";
   End(result);
  }
  catch(Exception e){gate.Cancel();End("failed");Plugin.MenuLog.LogError(e);}
 }
 internal string Cancel(){gate.Cancel();return End("cancelled");}
 internal void Dispose(){if(Busy)Cancel();if(Current==this)Current=null;}
}
[HarmonyPatch(typeof(TaskGenerateAsyncSubSequence),nameof(TaskGenerateAsyncSubSequence.Start))]
internal static class RunRestartGenerationPatch
{private static void Postfix(TaskGenerateAsyncSubSequence __instance)=>ScenePatchGuard.Write(()=>RunRestartActions.Current?.Generated(__instance._parameters));}
