// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using HarmonyLib;
using MotherBase.ApplicationFlowManager;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
namespace RoguePrince.CheatMenu;
internal sealed class RunTools
{
 internal static RunTools Current;
 internal static bool Available;
 internal static long RunId=1;
 private readonly RunStartReservation start=new();
 private readonly EncounterReservation encounter=new();
 private ApplicationFlowService manager;
 private ItemProgressionManagementService progression;
 private int? difficulty;
 private long difficultyRun;
 private int difficultySeed;
 private ApplicationState destination;
 private long menuLoadOwner;
 private string menuLoadTarget;
 private bool generationFromMenu;
 internal string Status {get;private set;}="";
 internal bool Error {get;private set;}
 internal bool Pending=>start.Pending||encounter.Pending;
 internal bool Active=>Pending||difficulty.HasValue;
 internal bool EncounterPending=>encounter.Pending;
 internal bool SeedPending=>start.Pending;
 internal long StartRevision {get;private set;}
 internal long ConsumedStartRevision {get;private set;}
 internal long ConsumedStartRunId {get;private set;}
 internal int? ActiveDifficulty=>difficulty;
 internal RunTools(){Current=this;}
 private string Report(string text,bool error=false){Status=text;Error=error;Plugin.LogEvent("run_tools "+text);return text;}
 internal int MaxDifficulty()
 {
  var service=RuntimeServiceLocator.Get<ItemProgressionManagementService>();var levels=service?.settings?.awakeningSettings?.awakeningLevelSteps;
  if(levels==null||levels.Count==0)throw new InvalidOperationException(MenuText.Get("觉醒设置尚未就绪"));
  int max=0;foreach(var level in levels)max=Math.Max(max,level.level);return max;
 }
 internal string Arm(string seed,string level,bool overrideDifficulty)
 {
  if(!Available)throw new InvalidOperationException(MenuText.Get("原生补丁不可用"));
  var flow=SceneActions.Flow();if(flow==null||flow.isLoading)throw new InvalidOperationException(MenuText.Get("关卡尚未就绪或正在加载"));
  var value=SceneValuePolicy.Seed(seed);int? next=overrideDifficulty?SceneValuePolicy.Difficulty(level,MaxDifficulty()):null;
  manager=flow;start.Arm(flow.Pointer.ToInt64(),value,next);StartRevision++;ConsumedStartRevision=0;return Report(MenuText.Get("已预约下一次原生新局的种子与等级"));
 }
 internal string CancelStart(){start.Cancel();StartRevision++;ConsumedStartRevision=0;return Report(MenuText.Get("新局预约已取消"));}
 internal void CancelStartIfOwned(long revision){if(revision==StartRevision&&start.Pending)CancelStart();}
 internal string RestoreDifficulty(){difficulty=null;return Report(MenuText.Get("已恢复原生觉醒等级"));}
 internal void Prepare(LevelGeneratorParameters parameters)
 {
  if(!start.Pending||parameters==null)return;
  try
  {
   var flow=SceneActions.Flow();if(flow==null||flow.Pointer!=manager?.Pointer)return;
   var state=flow.FindState(parameters.stateId);var logic=state?.stateLogic?.TryCast<BluesteakApplicationStateLogic>();
   if(generationFromMenu||!start.ShouldApply(flow.Pointer.ToInt64(),logic?.isStartRunBiome==true)||!SceneActions.Biome(state)||flow.GetCurrentState()?.guid!=state.guid||!flow.IsRunActive)return;
   var oldSeed=flow.GetSeed();var oldParameter=parameters.seed;
   try
   {flow.SetSeed(start.Seed);parameters.seed=start.Seed;if(flow.GetSeed()!=start.Seed||parameters.seed!=start.Seed)throw new InvalidOperationException("Native seed readback failed");}
   catch{parameters.seed=oldParameter;flow.SetSeed(oldSeed);throw;}
   progression=RuntimeServiceLocator.Get<ItemProgressionManagementService>();difficulty=start.Difficulty;difficultyRun=RunId;difficultySeed=start.Seed;
   start.Consume(flow.Pointer.ToInt64(),true);ConsumedStartRevision=StartRevision;ConsumedStartRunId=RunId;Report(MenuText.Format("起始关卡生成种子已设为 {0}",parameters.seed));
  }
  catch(Exception e){start.Cancel();ConsumedStartRevision=0;Report(MenuText.Get("新局预约失败，已取消")+": "+e.Message,true);Plugin.MenuLog.LogError(e);}
 }
 internal int Difficulty(ItemProgressionManagementService owner,int native)
 {
  var flow=SceneActions.Flow();return difficulty.HasValue&&RunId==difficultyRun&&flow?.Pointer==manager?.Pointer&&flow.IsRunActive&&flow.GetSeed()==difficultySeed&&owner?.Pointer==progression?.Pointer?difficulty.Value:native;
 }
 internal string ArmEncounter(LevelTravelActions.Entry target,IReadOnlyList<LevelTravelActions.Entry> nativeNext)
 {
  if(!Available)throw new InvalidOperationException(MenuText.Get("原生补丁不可用"));
  var flow=SceneActions.Flow();if(flow==null||flow.isLoading||!flow.IsRunActive||target==null||!nativeNext.Any(x=>x.Key==target.Key))throw new InvalidOperationException(MenuText.Get("当前出口不支持该遭遇；未执行"));
  var signals=nativeNext.Where(x=>x.Signal!=null&&flow.AcceptsSignal(x.Signal)&&flow.GetNextState(x.Signal)?.guid==x.Key).Select(x=>x.Signal.Pointer.ToInt64()).Distinct().ToArray();
  var actual=flow.FindState(target.State.stateID);if(actual?.guid!=target.Key||!SceneActions.Biome(actual))throw new InvalidOperationException(MenuText.Get("当前出口不支持该遭遇；未执行"));
  manager=flow;destination=actual;encounter.Arm(flow.Pointer.ToInt64(),flow.GetCurrentState().guid,target.Key,signals);
  return Report(MenuText.Format("已预约原生出口候选：{0}",target.Name));
 }
 internal string CancelEncounter(){encounter.Cancel();destination=null;return Report(MenuText.Get("遭遇预约已取消"));}
 internal ApplicationState Select(ApplicationFlowManager flow,ApplicationSignal signal,ApplicationState original)
 {
  if(signal==null||flow==null||flow.isLoading||!Application.isFocused||!encounter.Pending)return original;
  if(encounter.Select(flow.Pointer.ToInt64(),flow.GetCurrentState()?.guid,signal.Pointer.ToInt64())!=destination?.guid)return original;
  return destination;
 }
 internal void Loading(ApplicationFlowManager flow,ApplicationState target)
 {if(flow==null)return;generationFromMenu=menuLoadOwner==flow.Pointer.ToInt64()&&menuLoadTarget==target?.guid;menuLoadOwner=0;menuLoadTarget=null;var result=encounter.Load(flow.Pointer.ToInt64(),target?.guid);if(result=="idle")return;destination=null;Report(MenuText.Get(result=="consumed"?"遭遇预约已交给原生加载流程":"进入其他关卡，遭遇预约已取消"));}
 internal void MenuLoadRequested(ApplicationFlowManager flow,ApplicationState target)
 {menuLoadOwner=flow?.Pointer.ToInt64()??0;menuLoadTarget=target?.guid;}
 internal ApplicationState Redirect(ApplicationFlowManager flow,ApplicationSignal signal,ApplicationState original)
 {if(menuLoadOwner==flow?.Pointer.ToInt64()&&menuLoadTarget==original?.guid)return original;return Select(flow,signal,original);}
 internal void ResetRun(ApplicationFlowService flow)
 {if(flow==null||flow.Pointer!=SceneActions.Flow()?.Pointer)return;RunId++;start.ObserveReset(flow.Pointer.ToInt64());difficulty=null;encounter.Cancel();destination=null;Plugin.LogEvent("scene_run_reset id="+RunId);}
 internal void Disable(){start.Cancel();StartRevision++;ConsumedStartRevision=0;encounter.Cancel();difficulty=null;destination=null;}
 internal void Tick()
 {
  if(!Active)return;var flow=SceneActions.Flow();
  if(flow==null||flow.Pointer!=manager?.Pointer){start.Cancel();encounter.Cancel();difficulty=null;destination=null;Report(MenuText.Get("本轮已变化，场景预约已取消"));}
  else if(difficulty.HasValue&&(!flow.IsRunActive||RunId!=difficultyRun||flow.GetSeed()!=difficultySeed))difficulty=null;
  else if(encounter.Pending&&!flow.isLoading&&destination!=null&&flow.GetCurrentState()?.guid==destination.guid){encounter.Cancel();destination=null;}
 }
 internal void Dispose(){start.Cancel();StartRevision++;ConsumedStartRevision=0;encounter.Cancel();difficulty=null;destination=null;if(Current==this)Current=null;}
}
[HarmonyPatch(typeof(TaskGenerateAsyncSubSequence),nameof(TaskGenerateAsyncSubSequence.Start))]
internal static class RunSeedGeneratePatch
{[HarmonyPriority(Priority.First)]private static void Prefix(TaskGenerateAsyncSubSequence __instance)=>RunTools.Current?.Prepare(__instance._parameters);}
[HarmonyPatch(typeof(ItemProgressionManagementService),nameof(ItemProgressionManagementService.GetAwakeningLevel))]
internal static class RunDifficultyPatch
{private static void Postfix(ItemProgressionManagementService __instance,ref int __result){var original=__result;__result=ScenePatchGuard.Read(()=>RunTools.Current?.Difficulty(__instance,original)??original,original);}}
[HarmonyPatch(typeof(ApplicationFlowService),nameof(ApplicationFlowService.ResetLayerLevelInRun))]
internal static class SceneRunResetPatch
{private static void Postfix(ApplicationFlowService __instance)=>ScenePatchGuard.Write(()=>RunTools.Current?.ResetRun(__instance));}
[HarmonyPatch(typeof(ApplicationFlowManager),nameof(ApplicationFlowManager.LoadStateInternal))]
internal static class SceneEncounterLoadPatch
{private static void Prefix(ApplicationFlowManager __instance,ref ApplicationState __0,ApplicationSignal __1){var original=__0;__0=ScenePatchGuard.Read(()=>RunTools.Current?.Redirect(__instance,__1,original)??original,original);var target=__0;ScenePatchGuard.Write(()=>RunTools.Current?.Loading(__instance,target));}}
