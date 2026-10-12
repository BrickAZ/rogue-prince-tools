// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using HarmonyLib;
using MotherBase.EntityFramework;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
using UObject=UnityEngine.Object;
namespace RoguePrince.CheatMenu;

// One observed Unity frame, not a fixed simulation tick. Native physics/animation
// scheduling is left intact; no manual invocation or synthetic deltaTime is used.
internal sealed class FrameObservationActions
{
 internal static bool Available=false;
 internal static FrameObservationActions Current;
 internal Func<bool> IsMenuVisible=()=>false;
 private readonly GameSession game;
 private GameplayUpdate owner;
 private ManagedSystemManager manager;
 private EntityContext context;
 private TrainingScope scope;
 private IntPtr player;
 private float resumeScale,deadline;
 private int targetFrame,updates,fixedUpdates,lateUpdates;
 internal bool Enabled {get;private set;}
 internal bool Pending {get;private set;}
 internal bool RecoveryPending {get;private set;}
 internal int Completed {get;private set;}
 internal string Status {get;private set;}="";
 internal bool Error {get;private set;}
 internal FrameObservationActions(GameSession game){this.game=game;Current=this;}
 private TrainingScope RequireScope()
 {
  game.RequireReady();var current=TrainingActions.ReadScope(game);
  if(!current.Valid||!Application.isFocused)throw new InvalidOperationException(MenuText.Get("训练操作需要当前角色、房间与焦点就绪"));
  return current;
 }
 private GameplayUpdate SelectOwner()
 {
  var stacks=UObject.FindObjectsOfType<GameStateStack>().Where(x=>x.enabled&&x.gameObject.activeInHierarchy).ToArray();
  if(stacks.Length!=1)throw new InvalidOperationException(MenuText.Get("缺少唯一的原生游戏更新入口"));
  var found=new List<GameplayUpdate>();var elements=stacks[0].stack;
  if(elements!=null)for(int i=0;i<elements.Count;i++){var entry=elements[i]?.TryCast<GameplayUpdate>();if(entry!=null&&entry.isActive&&entry.isPushed)found.Add(entry);}
  if(found.Count!=1)throw new InvalidOperationException(MenuText.Get("缺少唯一的原生游戏更新入口"));
  return found[0];
 }
 private bool Bound()
  =>Enabled&&game.Ready&&Application.isFocused&&scope.Matches(TrainingActions.ReadScope(game))&&game.PlayerPointer==player&&owner!=null&&owner.isActive&&owner.isPushed&&manager!=null&&manager.enabled&&manager.gameObject.activeInHierarchy&&owner._systemManager?.Pointer==manager.Pointer&&manager._context?.Pointer==context?.Pointer&&game.Player.context?.Pointer==context?.Pointer;
 private bool NativePaused()=>context!=null&&context.IsPaused();
 internal string Enable(Action release)
 {
  if(Enabled||RecoveryPending||!Available)throw new InvalidOperationException(MenuText.Get("单帧观察不可用或已经开启"));
  var before=RequireScope();var selected=SelectOwner();var selectedManager=selected._systemManager;
  if(selectedManager==null||!selectedManager.enabled||!selectedManager.gameObject.activeInHierarchy||selectedManager._context==null||game.Player.context?.Pointer!=selectedManager._context.Pointer||selectedManager._context.IsPaused())throw new InvalidOperationException(MenuText.Get("原生游戏更新已暂停或绑定不匹配"));
  release();
  if(!before.Matches(RequireScope())||SelectOwner().Pointer!=selected.Pointer||selected._systemManager?.Pointer!=selectedManager.Pointer||game.Player.context?.Pointer!=selectedManager._context?.Pointer||selectedManager._context.IsPaused()||!float.IsFinite(Time.timeScale)||Time.timeScale<=0)throw new InvalidOperationException(MenuText.Get("原生游戏更新已暂停或绑定不匹配"));
  scope=before;owner=selected;manager=selectedManager;context=manager._context;player=game.PlayerPointer;resumeScale=Time.timeScale;
  Enabled=true;Pending=false;Completed=0;
  try{Pause();}catch{Restore();throw;}
  return Report(MenuText.Get("单帧观察已暂停；打开菜单推进一次或恢复运行"));
 }
 internal string Step(Action release)
 {
  if(!Enabled||Pending)throw new InvalidOperationException(MenuText.Get("请先开启单帧观察并等待当前请求结束"));
  if(!Bound()||NativePaused()){Restore();throw new InvalidOperationException(MenuText.Get("原生游戏更新已暂停或绑定不匹配"));}
  release();
  if(!Bound()||NativePaused()){Restore();throw new InvalidOperationException(MenuText.Get("原生游戏更新已暂停或绑定不匹配"));}
  if(Time.timeScale!=0){Restore();throw new InvalidOperationException(MenuText.Get("时间控制已被其他模式接管"));}
  targetFrame=Time.frameCount+1;updates=fixedUpdates=lateUpdates=0;deadline=Time.realtimeSinceStartup+1;Pending=true;
  Time.timeScale=resumeScale;
  Plugin.LogEvent($"frame_observation_request frame={targetFrame} scale={resumeScale}");
  return Report(MenuText.Get("已请求下一渲染帧；等待原生更新确认"));
 }
 private void Pause()
 {
  Time.timeScale=0;
  if(Time.timeScale!=0)throw new InvalidOperationException(MenuText.Get("暂停回读失败，请恢复运行"));
 }
 private string Report(string text,bool error=false){Status=text;Error=error;return text;}
 private void Abort()
 {
  Pending=false;Pause();
  Report(MenuText.Get("原生更新未完整确认，已重新暂停；可恢复运行"),true);
  Plugin.LogEvent($"frame_observation_aborted frame={targetFrame} update={updates} fixed={fixedUpdates} late={lateUpdates}");
 }
 internal void Tick(bool menuVisible)
 {
  if(RecoveryPending){ReleaseDeferred(menuVisible);return;}
  if(!Enabled)return;
  if(!Bound()||NativePaused()){Restore();return;}
  if(Pending&&(menuVisible||Time.frameCount>targetFrame||Time.realtimeSinceStartup>deadline)){Abort();return;}
  if(!Pending&&Time.timeScale!=0){Restore();Report(MenuText.Get("时间控制已被其他模式接管"),true);}
 }
 internal bool Before(GameplayUpdate source,int phase,out bool admitted)
 {
  admitted=false;
  if(!Enabled||source?.Pointer!=owner?.Pointer)return true;
  try
  {
   if(!Bound()||NativePaused()){Restore();return true;}
   if(!Pending)return false;
   if(Time.frameCount>targetFrame||Time.realtimeSinceStartup>deadline){Abort();return false;}
   if(Time.frameCount!=targetFrame||(phase==1&&updates>0)||(phase==2&&updates!=1))return false;
   admitted=true;return true;
  }
  catch(Exception e){Restore();Report(e.Message,true);return true;}
 }
 internal void After(GameplayUpdate source,int phase,bool admitted)
 {
  if(!admitted||!Enabled||!Pending||source?.Pointer!=owner?.Pointer||Time.frameCount!=targetFrame)return;
  try
  {
   if(!Bound()||NativePaused()){Restore();return;}
   if(phase==0)fixedUpdates++;
   else if(phase==1)updates++;
   else
   {
    lateUpdates++;var delta=Time.deltaTime;
    if(!float.IsFinite(delta)||delta<=0){Abort();return;}
    if(float.IsFinite(Time.timeScale)&&Time.timeScale>0)resumeScale=Time.timeScale;
    Pause();Pending=false;Completed++;
    Report(MenuText.Format("已观察 {0} 帧 · U / F / L：{1} / {2} / {3} · Δt {4:0.##} ms",Completed,updates,fixedUpdates,lateUpdates,delta*1000));
    Plugin.LogEvent(FormattableString.Invariant($"frame_observation_complete frame={targetFrame} update={updates} fixed={fixedUpdates} late={lateUpdates} delta={delta:R}"));
   }
  }
  catch(Exception e){Abort();Report(e.Message,true);}
 }
 internal string Restore()
 {
  if(!Enabled&&!RecoveryPending)return Status;
  if(Enabled&&Pending&&LivePauseContext()?.IsPaused()==true)Pause();
  Enabled=Pending=false;RecoveryPending=Time.timeScale==0;
  ReleaseDeferred(IsMenuVisible());
  return Status;
 }
 private EntityContext LivePauseContext()
 {
  // Never dereference an old context after its Unity manager is destroyed or
  // rebound. During travel, wait for the new canonical player to become ready.
  var capturedLive=manager!=null&&manager._context?.Pointer==context?.Pointer;
  if(game.PlayerPointer==player&&capturedLive&&game.Player.context?.Pointer==context?.Pointer&&owner?._systemManager?.Pointer==manager.Pointer)return context;
  if(game.Ready&&TrainingActions.ReadScope(game).Valid)return game.Player.context;
  return capturedLive?context:null;
 }
 private void ReleaseDeferred(bool menuVisible)
 {
  if(RecoveryPending&&Time.timeScale==0)
  {
   var live=LivePauseContext();
   if(menuVisible||live==null||live.IsPaused())
   {
    Report(MenuText.Get("单帧观察已退出；等待原生暂停结束并关闭菜单后归还时间控制"));return;
   }
   Time.timeScale=resumeScale;
   if(Time.timeScale!=resumeScale)throw new InvalidOperationException(MenuText.Get("时间恢复回读失败，请重试恢复"));
  }
  RecoveryPending=false;owner=null;manager=null;context=null;
  Plugin.LogEvent("frame_observation_released");
  Report(MenuText.Get("单帧观察已退出；已释放本模式的暂停"));
 }
}
[HarmonyPatch(typeof(GameplayUpdate),nameof(GameplayUpdate.OnFixedUpdate))]
internal static class FrameObservationFixedPatch
{
 internal static bool Prefix(GameplayUpdate __instance,out bool __state){__state=false;return FrameObservationActions.Current?.Before(__instance,0,out __state)??true;}
 internal static void Postfix(GameplayUpdate __instance,bool __state)=>FrameObservationActions.Current?.After(__instance,0,__state);
}
[HarmonyPatch(typeof(GameplayUpdate),nameof(GameplayUpdate.OnUpdate))]
internal static class FrameObservationUpdatePatch
{
 internal static bool Prefix(GameplayUpdate __instance,out bool __state){__state=false;return FrameObservationActions.Current?.Before(__instance,1,out __state)??true;}
 internal static void Postfix(GameplayUpdate __instance,bool __state)=>FrameObservationActions.Current?.After(__instance,1,__state);
}
[HarmonyPatch(typeof(GameplayUpdate),nameof(GameplayUpdate.OnLateUpdate))]
internal static class FrameObservationLatePatch
{
 internal static bool Prefix(GameplayUpdate __instance,out bool __state){__state=false;return FrameObservationActions.Current?.Before(__instance,2,out __state)??true;}
 internal static void Postfix(GameplayUpdate __instance,bool __state)=>FrameObservationActions.Current?.After(__instance,2,__state);
}
