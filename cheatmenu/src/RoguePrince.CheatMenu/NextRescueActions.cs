// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using HarmonyLib;
using MotherBase.ApplicationFlowManager;
using RoguePrince.CheatMenu.Core;
using UnityEngine;

namespace RoguePrince.CheatMenu;

internal sealed class NextRescueActions
{
    internal static NextRescueActions Instance;
    internal static bool Available;
    private readonly GameSession game;
    private readonly NextRescuePolicy policy=new();
    private ApplicationFlowService service;
    private ApplicationFlowManager flow;
    private IntRef savedRef;
    private string savedContext, targetName="";
    private int savedSeed, previousLayer, forcedLayer;
    private IntPtr expectedStructure;
    public bool Pending=>policy.Pending;
    public bool RecoveryPending=>savedRef!=null&&!Pending;
    public string Status { get; private set; }="";
    public bool Error { get; private set; }
    public int Revision { get; private set; }
    public NextRescueActions(GameSession game) { this.game=game; Instance=this; }
    private static string StateKey(ApplicationStateID state)=>state==null?"":state.Pointer.ToInt64().ToString("X");
    private static string Context(ApplicationFlowService s,ApplicationFlowManager f)=>s==null||f==null?"":$"{s.Pointer}:{f.Pointer}";
    private static ApplicationFlowManager ReadFlow(ApplicationFlowService s)=>s?.GetCurrentStateLogic()?.flowManagerVariable?.CurrentValue;
    private void Report(string text,bool error=false) { if(Status==text&&Error==error)return;Status=text; Error=error;Revision++; }
    public string Arm()
    {
        game.RequireReady();
        if(!Available)throw new InvalidOperationException(MenuText.Get("营救生成补丁未就绪"));
        if(RecoveryPending)throw new InvalidOperationException(MenuText.Get("请先重试恢复营救安排"));
        service=RuntimeServiceLocator.Get<ApplicationFlowService>(); flow=ReadFlow(service);
        if(service==null||flow==null||!service.IsRunActive||service.huntLayerRef==null||
           !policy.TryArm(game.Ready,flow.isLoading,Context(service,flow),service.GetSeed(),StateKey(flow.GetCurrentState()?.stateID)))
            throw new InvalidOperationException(MenuText.Get("无法预约营救；请等待当前关卡就绪"));
        Report(MenuText.Get("已预约下一关营救；不支持时只提示并取消，不顺延"));
        Plugin.LogEvent($"rescue_armed origin={flow.GetCurrentState()?.name} seed={service.GetSeed()}"); return Status;
    }
    public string Cancel()
    {
        policy.Cancel(); RestoreUncommitted();
        Report(MenuText.Get("营救预约已取消")); Plugin.LogEvent("rescue_cancelled"); return Status;
    }
    private void ClearLease() { savedRef=null; savedContext=null; expectedStructure=IntPtr.Zero; }
    private bool LeaseIsCurrent()
    {
        var current=RuntimeServiceLocator.Get<ApplicationFlowService>();
        // Keep the original manager during unload/load, when the service's current logic can be absent.
        return current!=null&&Context(current,flow)==savedContext&&current.GetSeed()==savedSeed&&current.huntLayerRef?.Pointer==savedRef?.Pointer;
    }
    private void RestoreUncommitted()
    {
        if(savedRef==null)return;
        if(!LeaseIsCurrent()) { Plugin.LogEvent($"rescue_restore_retired context={savedContext} previous={previousLayer}");ClearLease();return; }
        if(savedRef.value!=forcedLayer) { Plugin.LogEvent("rescue_restore_skipped schedule_changed");ClearLease();return; }
        DebugSeedCommand.ForceHuntAtLayer(previousLayer);
        if(savedRef.value!=previousLayer)throw new InvalidOperationException(MenuText.Get("营救安排恢复未通过回读，请重试恢复"));
        Plugin.LogEvent($"rescue_restored layer={previousLayer}");ClearLease();
    }
    internal void BeforeGeneration(BluesteakApplicationStateLogic logic)
    {
        if(!Pending)return;
        try
        {
            var current=RuntimeServiceLocator.Get<ApplicationFlowService>();
            var manager=logic?.flowManagerVariable?.CurrentValue;
            var info=logic?.GetBiomeGenerationInfo();
            var target=StateKey(info?.stateId);
            var supported=logic!=null&&logic.IsBiomeGeneration()&&!logic.isHubBiome&&!logic.isTutorialBiome&&logic.layerInGame>0&&info?.huntLevelStructure!=null;
            var result=policy.Begin(Context(current,manager),current?.GetSeed()??0,target,supported,Time.realtimeSinceStartup,()=>
            {
                savedRef=current.huntLayerRef??throw new InvalidOperationException(MenuText.Get("原生营救安排不可用"));
                savedContext=Context(current,manager);savedSeed=current.GetSeed();previousLayer=savedRef.value;forcedLayer=logic.layerInGame;
                expectedStructure=info.huntLevelStructure.Pointer;
                targetName=ItemBrowser.ReadLabel(info.biomeDescriptor?.biomeTitle,info.name).Display;
                RescueWriteTransaction.Apply(()=>DebugSeedCommand.ForceHuntAtLayer(forcedLayer),
                    ()=>{if(savedRef.value!=forcedLayer)throw new InvalidOperationException("Native rescue layer readback failed");},RestoreUncommitted);
                Plugin.LogEvent($"rescue_prepared state={target} layer={forcedLayer} previous={previousLayer}");
            });
            if(result=="unsupported") { RestoreUncommitted();Report(MenuText.Get("下一关不支持营救任务；未修改，预约已取消"),true);Plugin.LogEvent($"rescue_not_supported state={target}"); }
            else if(result=="context_changed") { RestoreUncommitted();Report(MenuText.Get("本轮已变化，营救预约已取消")); }
            else if(result=="prepared")Report(MenuText.Format("正在生成营救任务：{0}",targetName));
        }
        catch(Exception error) { Fail(error); }
    }
    internal void CheckParameters(LevelGeneratorParameters parameters)
    {
        if(!policy.Prepared||policy.Confirmed||StateKey(parameters?.stateId)!=policy.Destination)return;
        try
        {
            if(!LeaseIsCurrent())throw new InvalidOperationException("Rescue run binding changed before generation");
            bool valid=parameters.isRescue&&parameters.structure!=null&&parameters.structure.Pointer==expectedStructure;
            // The native parameters now contain the rescue structure and flag. Release only our
            // temporary schedule before Start so a later load of this layer does not inherit it.
            if(!policy.Confirm(StateKey(parameters.stateId),valid,RestoreUncommitted))throw new InvalidOperationException("Native rescue structure or isRescue flag was not selected");
            Plugin.LogEvent($"rescue_parameters_confirmed state={policy.Destination} structure={parameters.structure.name}");
        }
        catch(Exception error) { Fail(error); }
    }
    private void Fail(Exception error)
    {
        policy.Cancel();
        try { RestoreUncommitted(); }
        catch(Exception rollback){Plugin.MenuLog.LogError("[RPCheat] rescue_restore_failed "+rollback);}
        Report(MenuText.Get(RecoveryPending?"营救安排恢复失败，请重试恢复并查看日志":"营救预约失败并取消；未自动重试，请查看日志"),true);
        Plugin.MenuLog.LogError("[RPCheat] rescue_failed "+error);
    }
    public void Tick()
    {
        if(!Pending)return;
        try
        {
            var current=RuntimeServiceLocator.Get<ApplicationFlowService>();
            var manager=flow;
            var loading=manager!=null&&manager.isLoading;
            var result=policy.Observe(Context(current,manager),current?.GetSeed()??0,current!=null&&current.IsRunActive,
                loading,StateKey(manager?.GetCurrentState()?.stateID),Time.realtimeSinceStartup);
            if(result=="completed") { ClearLease();Report(MenuText.Format("营救关卡已加载：{0}；预约已用完",targetName));Plugin.LogEvent($"rescue_completed state={manager?.GetCurrentState()?.name}"); }
            else if(result=="failed") { RestoreUncommitted();Report(MenuText.Get("营救加载超时；预约已取消，未自动重试"),true);Plugin.LogEvent("rescue_timeout"); }
            else if(result=="context_changed") { RestoreUncommitted();Report(MenuText.Get("本轮已变化，营救预约已取消")); }
            else if(result=="unsupported") { Report(MenuText.Get("下一关不支持营救任务；未修改，预约已取消"),true);Plugin.LogEvent("rescue_not_supported no_biome_generation"); }
            else if(result=="pending"&&!loading&&!game.Ready)Cancel();
        }
        catch(Exception error){Fail(error);}
    }
    internal void RunReset(ApplicationFlowService changed=null)
    {
        if(changed!=null&&service?.Pointer!=changed.Pointer)return;
        if(!Pending&&savedRef==null)return;
        policy.Cancel(); // Retire this reservation at a run reset; never write the previous run's value into a new run.
        if(savedRef!=null)Plugin.LogEvent($"rescue_restore_retired run_reset previous={previousLayer}");
        ClearLease();Report(MenuText.Get("本轮已变化，营救预约已取消"));
    }
    public void Dispose(bool quitting)
    {
        try { if(quitting)RunReset();else Cancel(); }
        finally { if(Instance==this)Instance=null; }
    }
}

[HarmonyPatch(typeof(BluesteakApplicationStateLogic),nameof(BluesteakApplicationStateLogic.GenerateBiome))]
internal static class NextRescueGeneratePatch
{
    private static void Prefix(BluesteakApplicationStateLogic __instance)=>NextRescueActions.Instance?.BeforeGeneration(__instance);
}
[HarmonyPatch(typeof(TaskGenerateAsyncSubSequence),nameof(TaskGenerateAsyncSubSequence.Start))]
internal static class NextRescueParametersPatch
{
    private static void Prefix(TaskGenerateAsyncSubSequence __instance)=>NextRescueActions.Instance?.CheckParameters(__instance._parameters);
}
[HarmonyPatch(typeof(ApplicationFlowService),nameof(ApplicationFlowService.ResetLayerLevelInRun))]
internal static class NextRescueRunResetPatch
{
    private static void Postfix(ApplicationFlowService __instance)=>NextRescueActions.Instance?.RunReset(__instance);
}
