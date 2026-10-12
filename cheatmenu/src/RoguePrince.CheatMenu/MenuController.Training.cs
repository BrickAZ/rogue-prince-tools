// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using Il2CppInterop.Runtime.Attributes;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
namespace RoguePrince.CheatMenu;
public sealed partial class MenuController
{
    private TrainingActions training;
    private PhotoActions photo;
    private bool batchFull=true;
    private long selectedTarget;
    private int ownedPage,effectPage;
    private float nextEffects;
    private List<string> effectRows=new();
    private string effectError="";
    [HideFromIl2Cpp]
    private void RestorePlayer(bool rebind=false)
        =>RecoverySteps.Run(()=>RestoreTraining(rebind),()=>{if(rebind){affixes.Reset();loadouts?.Retire();}},()=>mobs.Reset(),()=>actions.Reset());
    [HideFromIl2Cpp]
    private void RestoreTraining(bool rebind=false)
    {
        // Both restorations must get a chance, including when one native call fails.
        var errors=new List<Exception>();
        try{playerCheckpoint?.Reset(rebind);}catch(Exception e){errors.Add(e);}
        try{frameObservation?.Restore();}catch(Exception e){errors.Add(e);}
        try{abilities?.Restore(rebind);}catch(Exception e){errors.Add(e);}
        try{bossPractice?.Restore(rebind);}catch(Exception e){errors.Add(e);}
        try{if(bossPhases?.Busy==true)bossPhases.Cancel();}catch(Exception e){errors.Add(e);}
        try{sceneCamera?.Restore();}catch(Exception e){errors.Add(e);}
        try{scenes?.Restore(rebind);}catch(Exception e){errors.Add(e);}
        try{photo?.Restore();}catch(Exception e){errors.Add(e);}
        try{training?.Reset();}catch(Exception e){errors.Add(e);}
        try{waves?.Reset();}catch(Exception e){errors.Add(e);}
        try{combat?.Reset(rebind);}catch(Exception e){errors.Add(e);}
        try{rewards?.Clear();}catch(Exception e){errors.Add(e);}
        if(errors.Count>0)throw new AggregateException(MenuText.Get("训练状态恢复尚未完成"),errors);
    }
    [HideFromIl2Cpp]
    private void DrawTraining()
    {
        var ready=TrainingActions.ReadScope(game).Valid && !levels.Loading&&!CoreWorkBusy&&!runRestart.Busy;
        Text(28,228,860,28,MenuText.Get("速度倍率（切关、死亡或失焦时恢复）"),17,true);
        var values=new[]{.25f,.5f,1f,1.5f,2f};
        for(int i=0;i<values.Length;i++)
        {
            var value=values[i];
            if(Button("speed"+i,28+i*175,262,163,value+" ×",ready || value==1,false,16))Do(MenuText.Get("游戏速度"),()=>training.SetSpeed(value));
        }
        Text(28,303,860,26,MenuText.Format("当前速度 {0:0.##} 倍 · 选中普通怪物：{1}",training.Speed,selectedMob?.Data.Boss==false?selectedMob.Name:MenuText.Get("请在怪物页选择")),16);
        Text(28,340,85,36,MenuText.Get("数量"),16);var count=Field("batchCount",116,340,72,"3");
        Text(201,340,123,36,MenuText.Get("间隔（秒）"),16);var interval=Field("batchInterval",335,340,84,"1");
        if(Button("batchHealth",435,340,218,MenuText.Get(batchFull?"原版满血生成":"1 滴血生成"),!training.Busy,true,15))batchFull=!batchFull;
        if(Button("batchAi",667,340,228,MenuText.Get(mobAiEnabled?"AI：开启":"AI：关闭"),!training.Busy,true,15))mobAiEnabled=!mobAiEnabled;
        if(Button("batchStart",28,386,277,MenuText.Get("开始普通怪物批量生成"),ready&&!training.Busy&&!mobs.Busy&&selectedMob?.Data.Boss==false&&selectedMob.Data.Supported, false,15))
            Do(MenuText.Get("批量生成"),()=>AdmitTraining(()=>{
                if(!int.TryParse(count,out var n)||!double.TryParse(interval,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var seconds))throw new InvalidOperationException(MenuText.Get("请输入有效数量与间隔"));
                var result=training.StartBatch(selectedMob.InternalName,n,seconds,mobAiEnabled,batchFull?MobSpawnHealthMode.NativeFull:MobSpawnHealthMode.OneHp);Toggle();return result;
            }));
        if(Button("batchCancel",319,386,277,MenuText.Get("停止批量生成"),training.BatchActive))Do(MenuText.Get("停止批量生成"),training.CancelBatch);
        if(Button("batchClear",610,386,285,MenuText.Get("清理本菜单生成的怪物"),mobs.HasOwned||mobs.Busy||training.BatchActive,false,15))Do(MenuText.Get("清理怪物"),training.ClearAll);
        Text(28,429,865,36,MenuText.Get("数量 1–10，间隔 0.6–10 秒；逐只确认，首次失败停止。"),15,false,true);
        Text(28,472,865,32,MenuText.Get("同房间位置书签")+"："+training.BookmarkText,18,true);
        if(Button("bookSave",28,514,415,MenuText.Get("记录安全位置"),ready&&!training.Busy&&!mobs.Busy))Do(MenuText.Get("位置书签"),training.SaveBookmark);
        if(Button("bookReturn",465,514,429,MenuText.Get("返回书签"),ready&&training.HasBookmark&&!training.Busy&&!mobs.Busy))Do(MenuText.Get("返回书签"),()=>AdmitTraining(()=>training.ReturnBookmark(ReleaseForTravel)));
        Text(28,556,865,44,training.Status,15,!training.Error,true);
    }
    [HideFromIl2Cpp]
    private void DrawOwned()
    {
        var targets=mobs.Targets();var pages=Math.Max(1,(targets.Count+4)/5);ownedPage=Math.Clamp(ownedPage,0,pages-1);
        var chosen=targets.FirstOrDefault(t=>t.Id==selectedTarget);
        if(chosen==null)selectedTarget=0;
        Text(28,228,865,34,MenuText.Get("仅管理本菜单生成、当前房间仍存活的目标。"),16,false,true);
        for(int i=0;i<5 && ownedPage*5+i<targets.Count;i++)
        {
            var target=targets[ownedPage*5+i];
            var caption=MenuText.Format("#{0} {1} · {2:0.#}/{3:0.#} HP · {4}",target.Id,target.Name,target.Health,target.Maximum,MenuText.Get(target.Boss?"Boss":target.AiEnabled?"AI 开启":"AI 关闭"));
            if(Button("owned"+target.Id,28,273+i*44,866,caption,true,selectedTarget==target.Id,16))selectedTarget=target.Id;
        }
        if(targets.Count==0)Text(28,275,866,100,MenuText.Get("暂无存活的自有目标；先在怪物页或训练页生成。"),18,false,true);
        if(Button("ownedPrev",28,496,130,MenuText.Get("上一页"),ownedPage>0))ownedPage--;
        Text(180,496,540,36,MenuText.Format("第 {0} / {1} 页 · {2} 项",ownedPage+1,pages,targets.Count),16);
        if(Button("ownedNext",754,496,140,MenuText.Get("下一页"),ownedPage+1<pages))ownedPage++;
        var ready=chosen!=null && !training.Busy && !mobs.Busy && TrainingActions.ReadScope(game).Valid;
        if(Button("targetAi",28,546,205,MenuText.Get("切换目标 AI"),ready&&!chosen.Boss))Do(MenuText.Get("目标 AI"),()=>mobs.ToggleAi(chosen.Id));
        if(Button("targetHeal",249,546,205,MenuText.Get("目标回满生命"),ready&&!chosen.Boss))Do(MenuText.Get("目标回血"),()=>mobs.Refill(chosen.Id));
        if(Button("targetSpawn",470,546,205,MenuText.Get("再生成同类"),ready&&!CoreWorkBusy&&!chosen.Boss))Do(MenuText.Get("生成同类"),()=>AdmitTraining(()=>{var result=mobs.Respawn(chosen.Id);Toggle();return result;}));
        if(Button("targetRemove",691,546,203,MenuText.Get("单独清理目标"),ready))Do(MenuText.Get("清理目标"),()=>mobs.RemoveTarget(chosen.Id));
    }
    [HideFromIl2Cpp]
    private void DrawObserve()
    {
        var ready=TrainingActions.ReadScope(game).Valid;
        var zoom=Field("photoZoom",28,231,115,"0");
        Text(154,231,304,36,MenuText.Get("镜头缩放偏移（-5 至 5）"),16);
        if(Button("photoZoomApply",476,231,205,MenuText.Get("应用镜头偏移"),photo.Ready&&!sceneCamera.Active&&!CoreWorkBusy&&!runRestart.Busy))Do(MenuText.Get("镜头缩放"),()=>photo.SetZoom(ParsePhotoZoom(zoom)));
        if(Button("photoToggle",697,231,198,MenuText.Get(photo.HasState?"退出截图模式":"启用截图模式"),photo.HasState||(ready&&!sceneCamera.Active&&!CoreWorkBusy&&!runRestart.Busy),false,16))Do(MenuText.Get("截图模式"),()=>photo.HasState?photo.Restore():photo.Enter(ParsePhotoZoom(zoom)));
        Text(28,276,867,36,photo.Status,14,false,true);
        Text(28,317,865,30,MenuText.Get("当前角色状态效果（只读）"),19,true);
        if(Time.realtimeSinceStartup>=nextEffects)
        {
            nextEffects=Time.realtimeSinceStartup+.5f;
            try{effectRows=EffectSnapshot.Read(game);effectError="";}catch(Exception e){effectRows.Clear();effectError=e.Message;}
        }
        var pages=Math.Max(1,(effectRows.Count+4)/5);effectPage=Math.Clamp(effectPage,0,pages-1);
        for(int i=0;i<5 && effectPage*5+i<effectRows.Count;i++)Text(28,355+i*38,866,36,effectRows[effectPage*5+i],15,false,true);
        if(effectRows.Count==0)Text(28,358,866,85,effectError.Length>0?effectError:MenuText.Get("暂无活动效果或角色未就绪"),17,false,true);
        if(Button("effectsPrev",28,555,130,MenuText.Get("上一页"),effectPage>0))effectPage--;
        Text(184,555,540,36,MenuText.Format("第 {0} / {1} 页 · {2} 项",effectPage+1,pages,effectRows.Count),16);
        if(Button("effectsNext",754,555,140,MenuText.Get("下一页"),effectPage+1<pages))effectPage++;
    }
    [HideFromIl2Cpp]
    private static float ParsePhotoZoom(string value)
    {
        if(!float.TryParse(value,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var result))throw new InvalidOperationException(MenuText.Get("请输入有效镜头偏移"));
        TrainingValuePolicy.Zoom(result);return result;
    }
}
