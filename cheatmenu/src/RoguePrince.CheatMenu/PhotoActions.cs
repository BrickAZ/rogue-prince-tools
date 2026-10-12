// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using Doozy.Runtime.UIManager.Containers;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
using HarmonyLib;
namespace RoguePrince.CheatMenu;
internal sealed class PhotoActions
{
    private readonly GameSession game;
    private readonly List<Canvas> hidden=new();
    private CameraSystem camera;
    private MotherBase.EntityFramework.EntityContext context;
    private TrainingScope scope;
    private int zoomId;
    private readonly DeferredNativeLease zoomLease=new();
    private long cameraRevision;
    private float pendingSince;
    private bool cameraDisabled;
    private static PhotoActions instance;
    internal static bool Available;
    private float nextHud;
    internal bool Active {get;private set;}
    internal bool HasState=>Active || zoomLease.Owned || hidden.Count>0 || cameraDisabled;
    internal bool Ready=>Active && !zoomLease.Pending && !zoomLease.Releasing;
    internal float Zoom {get;private set;}
    internal string Status {get;private set;}="";
    internal PhotoActions(GameSession game){this.game=game;instance=this;}
    internal static void CameraUpdated(CameraSystem system)
    {if(instance?.camera!=null && system!=null && instance.camera.Pointer==system.Pointer)instance.cameraRevision++;}
    internal static void CameraDisabled(CameraSystem system,MotherBase.EntityFramework.EntityContext context)
    {
        if(instance?.camera==null || system==null || instance.camera.Pointer!=system.Pointer || instance.context?.Pointer!=context?.Pointer)return;
        // A disabled native system no longer owns camera requests; retire its old-context handle.
        instance.zoomLease.Retire();instance.cameraDisabled=true;
    }
    private static readonly HashSet<string> hudNames=new(StringComparer.Ordinal){"HUDPlayer","CurrencyHUD","AwakeningLevelHUD","XPBar","SKillPointCurrencyHUD","BossHealthBar","BossShieldBar"};
    private int CaptureHud()
    {
        foreach(var view in Resources.FindObjectsOfTypeAll<UIView>())
        {
            if(view==null || view.Id?.Category!="GameplayHUD" || !hudNames.Contains(view.Id.Name) || !view.gameObject.activeInHierarchy)continue;
            var canvas=view.canvas;
            if(canvas==null || !canvas.transform.IsChildOf(view.transform))continue;
            if(!hidden.Any(c=>c!=null && c.Pointer==canvas.Pointer) && canvas.enabled){hidden.Add(canvas);canvas.enabled=false;}
        }
        return hidden.Count;
    }
    internal string Enter(float zoom)
    {
        TrainingValuePolicy.Zoom(zoom);
        if(HasState)throw new InvalidOperationException(MenuText.Get("请先退出并恢复截图模式"));
        if(!Available)throw new InvalidOperationException(MenuText.Get("截图镜头确认补丁未就绪"));
        scope=TrainingActions.ReadScope(game);
        if(!scope.Valid || !Application.isFocused)throw new InvalidOperationException(MenuText.Get("截图模式需要游戏就绪"));
        var candidates=Resources.FindObjectsOfTypeAll<CameraSystem>().Where(c=>c!=null && c._currentZoomValues!=null).ToArray();
        if(candidates.Length!=1)throw new InvalidOperationException(MenuText.Get("无法唯一定位活动摄像机，未启用截图模式"));
        camera=candidates[0];context=game.Player.context;
        if(context==null)throw new InvalidOperationException(MenuText.Get("角色摄像机上下文未就绪"));
        try
        {
            zoomId=CameraSystem.StartZoomInstance(context,zoom);zoomLease.Issue(cameraRevision);Zoom=zoom;pendingSince=Time.realtimeSinceStartup;
            if(CaptureHud()==0)throw new InvalidOperationException(MenuText.Get("未找到独立游戏 HUD，未启用截图模式"));
            Active=true;nextHud=Time.realtimeSinceStartup+.5f;
            Status=MenuText.Get("截图请求已发送；关闭菜单后等待原生镜头确认");return Status;
        }
        catch {Restore();throw;}
    }
    internal string SetZoom(float value)
    {
        TrainingValuePolicy.Zoom(value);
        if(!Ready || !scope.Matches(TrainingActions.ReadScope(game)))throw new InvalidOperationException(MenuText.Get("截图模式未启用或作用域已改变"));
        var old=Zoom;
        try{CameraSystem.UpdateZoomInstance(context,zoomId,value);zoomLease.Issue(cameraRevision);Zoom=value;pendingSince=Time.realtimeSinceStartup;}
        catch{CameraSystem.UpdateZoomInstance(context,zoomId,old);zoomLease.Issue(cameraRevision);throw;}
        return MenuText.Format("镜头缩放偏移：{0:0.##}",value);
    }
    internal void Tick(bool menuVisible)
    {
        if(!HasState)return;
        if(cameraDisabled){Restore();cameraDisabled=false;camera=null;context=null;return;}
        if(zoomLease.Owned && camera==null){zoomLease.Retire();context=null;}
        if(!Application.isFocused || !scope.Matches(TrainingActions.ReadScope(game)))
        {if(!zoomLease.Releasing || hidden.Count>0)Restore();}
        if(zoomLease.CanObserve(cameraRevision))
        {
            var values=camera?._currentZoomValues;
            var present=values!=null && values.ContainsKey(zoomId);
            try
            {
                if(zoomLease.Observe(cameraRevision,present,present&&float.IsFinite(values[zoomId])&&Math.Abs(values[zoomId]-Zoom)<.0001f))
                {
                    Status=MenuText.Get(zoomLease.Owned?"截图模式已启用；关闭菜单后隐藏 HUD 与角标":"截图模式已退出，HUD 与镜头请求已恢复");
                    if(!zoomLease.Owned){camera=null;context=null;Zoom=0;}
                }
            }
            catch {Restore();throw;}
        }
        if(zoomLease.Pending && !zoomLease.Releasing)
        {
            if(menuVisible)pendingSince=Time.realtimeSinceStartup;
            else if(Time.realtimeSinceStartup-pendingSince>3){Restore();throw new TimeoutException(MenuText.Get("镜头缩放请求回读失败"));}
        }
        if(Active && Time.realtimeSinceStartup>=nextHud){nextHud=Time.realtimeSinceStartup+.5f;CaptureHud();}
    }
    internal string Restore()
    {
        // Restore canvases independently so a failed camera release cannot leave HUD hidden.
        var failures=new List<Exception>();
        for(int i=hidden.Count-1;i>=0;i--)
        {
            try{var canvas=hidden[i];if(canvas!=null){canvas.enabled=true;if(!canvas.enabled)throw new InvalidOperationException("HUD restore readback failed");}hidden.RemoveAt(i);}
            catch(Exception e){failures.Add(e);}
        }
        if(zoomLease.Owned && !zoomLease.Releasing)
        {
            try
            {
                if(camera!=null && context!=null){CameraSystem.StopZoomInstance(context,zoomId);zoomLease.Release(cameraRevision);}
                else{zoomLease.Retire();camera=null;context=null;Zoom=0;}
            }
            catch(Exception e){failures.Add(e);}
        }
        Active=hidden.Count>0;
        if(failures.Count>0)throw new AggregateException(MenuText.Get("截图恢复未完成，请重试退出截图模式"),failures);
        Status=MenuText.Get(zoomLease.Owned?"HUD 已恢复，等待原生镜头撤销确认":"截图模式已退出，HUD 与镜头请求已恢复");Plugin.LogEvent("photo_release_requested");return Status;
    }
}

[HarmonyPatch(typeof(CameraSystem),nameof(CameraSystem.UpdateSystem))]
internal static class PhotoCameraUpdatedPatch
{
    private static void Postfix(CameraSystem __instance)=>PhotoActions.CameraUpdated(__instance);
}
[HarmonyPatch(typeof(CameraSystem),nameof(CameraSystem.OnSystemDisabled))]
internal static class PhotoCameraDisabledPatch
{
    private static void Postfix(CameraSystem __instance,MotherBase.EntityFramework.EntityContext __0)=>PhotoActions.CameraDisabled(__instance,__0);
}
