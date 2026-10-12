// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using RoguePrince.CheatMenu.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UObject=UnityEngine.Object;

namespace RoguePrince.CheatMenu;
internal sealed class TrainingActions
{
    private readonly GameSession game;
    private readonly MobSpawnActions mobs;
    private readonly ScopedScalarLease speed=new();
    private readonly BatchSpawnQueue queue=new();
    private readonly RoomBookmark bookmark=new();
    private TrainingScope speedScope,batchScope,returnScope;
    private MobSpawnOptions batchOptions;
    private long batchRequest;
    private Vector3 returnSource,returnTarget;
    private float returnDeadline;
    private bool returning;
    internal string Status {get;private set;}="";
    internal bool Error {get;private set;}
    internal bool Busy=>queue.Active || returning;
    internal bool BatchActive=>queue.Active;
    internal int Completed=>queue.Completed;
    internal int Total=>queue.Total;
    internal bool HasBookmark=>bookmark.HasPoint;
    internal bool HasSpeed=>speed.Active;
    internal float Speed=>GlobalSlowMoFeedbackSystem.globalDebugTimeScale;
    internal string BookmarkText=>bookmark.HasPoint?$"({bookmark.X:0.##}, {bookmark.Y:0.##}, {bookmark.Z:0.##})":MenuText.Get("未记录 / 已失效");
    internal TrainingActions(GameSession game,MobSpawnActions mobs){this.game=game;this.mobs=mobs;}
    internal static TrainingScope ReadScope(GameSession game)
    {
        if(!game.Ready || game.Saver?.mainCharacterVariable?.CurrentValue?.Pointer!=game.PlayerPointer) return default;
        var service=RuntimeServiceLocator.Get<ApplicationFlowService>();
        var flow=service?.GetCurrentStateLogic()?.flowManagerVariable?.CurrentValue;
        if(service?.IsRunActive!=true || flow==null || flow.isLoading) return default;
        var room=game.Player.GetComponent<RoomStateComponent>()?.currentRoom;
        if(room?.template==null) return default;
        return new(game.Generation,game.PlayerPointer.ToInt64(),SceneManager.GetActiveScene().handle,room.template.Pointer.ToInt64(),room.position.x,room.position.y);
    }
    private TrainingScope RequireScope()
    {
        var current=ReadScope(game);
        if(!current.Valid || !Application.isFocused) throw new InvalidOperationException(MenuText.Get("训练操作需要当前角色、房间与焦点就绪"));
        return current;
    }
    private static bool SameLife(TrainingScope first,TrainingScope second)=>first.Valid && second.Valid && first.Generation==second.Generation && first.Player==second.Player && first.Scene==second.Scene;
    private string Report(string value,bool error=false){Status=value;Error=error;Plugin.LogEvent("training "+value);return value;}
    internal string SetSpeed(float value)
    {
        TrainingValuePolicy.Speed(value);
        if(value==1) return NormalSpeed();
        var current=RequireScope();
        speedScope=current;
        speed.Apply(value,()=>Speed,TimeDebugMenu.SetTimeScale);
        return Report(MenuText.Format("游戏速度：{0:0.##} 倍",value));
    }
    internal string NormalSpeed()
    {
        TimeDebugMenu.SetTimeScale(1);
        if(!float.IsFinite(Speed)||Math.Abs(Speed-1)>.0001f) throw new InvalidOperationException(MenuText.Get("速度倍率回读失败"));
        speed.Clear();return Report(MenuText.Get("已恢复正常速度"));
    }
    private void RestoreSpeed()
    {
        if(!speed.Active) return;
        var current=Speed;if(!float.IsFinite(current))throw new InvalidOperationException(MenuText.Get("原生数值无效"));
        var restore=speed.RestoreValue(current);
        if(current!=restore) {TimeDebugMenu.SetTimeScale(restore);if(!float.IsFinite(Speed)||Math.Abs(Speed-restore)>.0001f) throw new InvalidOperationException(MenuText.Get("速度恢复失败，请使用恢复正常速度"));}
        speed.Clear();Plugin.LogEvent("training_speed_released value="+Speed);
    }
    internal string StartBatch(string mob,int count,double interval,bool ai,MobSpawnHealthMode mode)
    {
        if(Busy || mobs.Busy) throw new InvalidOperationException(MenuText.Get("已有训练请求正在执行"));
        var current=RequireScope();
        queue.Start(mob,count,interval,Time.realtimeSinceStartup);batchScope=current;
        batchOptions=new(mode,ai,mob);
        return Report(MenuText.Format("批量生成已开始：0 / {0}",count));
    }
    internal string CancelBatch()
    {
        var ownPending=queue.Awaiting && mobs.Busy && mobs.CurrentRequestId==batchRequest;
        queue.Cancel();if(ownPending)mobs.CancelPending();
        return Report(MenuText.Format("批量已停止：已完成 {0} / {1}",queue.Completed,queue.Total));
    }
    internal string ClearAll(){if(queue.Active)CancelBatch();return mobs.ClearOwned();}
    internal void Tick(bool menuVisible)
    {
        var current=ReadScope(game);bookmark.Observe(current);
        if(speed.Active && (!SameLife(speedScope,current)||!Application.isFocused))RestoreSpeed();
        if(returning) TickReturn(current);
        if(!queue.Active)return;
        if(!batchScope.Matches(current)||!Application.isFocused){CancelBatch();Report(MenuText.Get("批量已取消：角色、房间、场景或焦点变化"),true);return;}
        if(queue.Awaiting)
        {
            if(mobs.Busy) return;
            var success=mobs.LastSuccessfulRequest==batchRequest && !mobs.Error;
            queue.Complete(Time.realtimeSinceStartup,success);
            Report(success?MenuText.Format("批量生成：{0} / {1}",queue.Completed,queue.Total):MenuText.Get("批量在首次失败后停止：")+mobs.Status,!success);
            return;
        }
        if(menuVisible || Time.timeScale<=0 || !queue.Ready(Time.realtimeSinceStartup)) return;
        try
        {
            var previous=mobs.CurrentRequestId;
            var accepted=mobs.Request(batchOptions);
            if(queue.Submitted(previous,mobs.CurrentRequestId,accepted,mobs.Error,Time.realtimeSinceStartup))batchRequest=mobs.CurrentRequestId;
            else if(!queue.Active)Report(MenuText.Get("批量在首次失败后停止：")+mobs.Status,true);
        }
        catch(Exception ex){queue.Cancel();Report(MenuText.Get("批量在首次失败后停止：")+ex.Message,true);}
    }
    internal string SaveBookmark()
    {
        if(Busy||mobs.Busy) throw new InvalidOperationException(MenuText.Get("已有训练请求正在执行"));
        var current=RequireScope();
        var platform=game.Player.GetComponent<PlatformStateComponent>();
        if(platform==null||!platform.currentPlatform.isValid()) throw new InvalidOperationException(MenuText.Get("书签需在安全平地记录"));
        var point=game.Player.transform.position;CheckBookmarkPosition(point);
        bookmark.Capture(current,point.x,point.y,point.z);
        return Report(MenuText.Get("已记录当前房间安全位置")+" "+BookmarkText);
    }
    internal string ReturnBookmark(Action releaseMenu)
    {
        if(Busy||mobs.Busy)throw new InvalidOperationException(MenuText.Get("已有训练请求正在执行"));
        var scope=RequireScope();
        if(!bookmark.Matches(scope)) throw new InvalidOperationException(MenuText.Get("书签不属于当前房间"));
        var target=new Vector3(bookmark.X,bookmark.Y,bookmark.Z);CheckBookmarkPosition(target);
        returnSource=game.Player.transform.position;CheckBookmarkPosition(returnSource);
        returnTarget=target;returnScope=scope;
        releaseMenu();
        returning=true;returnDeadline=Time.realtimeSinceStartup+3;
        try {TeleportExtensions.Teleport(game.Player,target,true);}
        catch {RollbackReturn(scope);throw;}
        return Report(MenuText.Get("已请求原生传送，等待位置回读"));
    }
    private void TickReturn(TrainingScope current)
    {
        if(!returnScope.Matches(current)){returning=false;Report(MenuText.Get("书签返回已中止：作用域变化，未向新场景写入旧坐标"),true);return;}
        if(Vector3.Distance(game.Player.transform.position,returnTarget)<.35f){returning=false;Report(MenuText.Get("已返回同房间书签"));return;}
        if(Time.realtimeSinceStartup>returnDeadline){RollbackReturn(current);Report(MenuText.Get("书签位置回读失败，已请求恢复原位置"),true);}
    }
    private void RollbackReturn(TrainingScope current)
    {
        if(!returnScope.Matches(current)){returning=false;return;}
        CheckBookmarkPosition(returnSource);
        TeleportExtensions.Teleport(game.Player,returnSource,true);
        if(Vector3.Distance(game.Player.transform.position,returnSource)>.35f) throw new InvalidOperationException(MenuText.Get("书签原位置恢复未通过回读"));
        returning=false;
    }
    private void CheckBookmarkPosition(Vector3 point)
    {
        var room=game.Player.GetComponent<RoomStateComponent>()?.currentRoom;
        ValidatePosition(game,room,point);
    }
    internal static void ValidatePosition(GameSession game,LevelRoomData room,Vector3 point)
    {
        var t=room?.template;
        if(t==null || !float.IsFinite(point.x)||!float.IsFinite(point.y)||!float.IsFinite(point.z))throw new InvalidOperationException(MenuText.Get("书签位置无效"));
        var x=Mathf.FloorToInt(point.x-room.position.x);var y=Mathf.RoundToInt(point.y-room.position.y);
        if(x<2 || x>=t.width-2 || y<1 || y+3>=t.height)throw new InvalidOperationException(MenuText.Get("书签位置靠近房间边界"));
        for(int dx=-1;dx<=1;dx++)
        {
            if((int)t.gpeData[t.GetIndex(x+dx,y-1)] is not (27 or 28)) throw new InvalidOperationException(MenuText.Get("书签需在安全平地记录"));
            for(int dy=0;dy<=2;dy++) if((int)t.gpeData[t.GetIndex(x+dx,y+dy)]!=0 || (int)t.colData[t.GetIndex(x+dx,y+dy)]==6)throw new InvalidOperationException(MenuText.Get("书签位置被地形阻挡"));
        }
        for(int dx=-2;dx<=2;dx++)for(int dy=-1;dy<=3;dy++)
            if((int)t.gpeData[t.GetIndex(x+dx,y+dy)] is 3 or 4 or 5 or 6 or 8 or 10 or 19 or 20)throw new InvalidOperationException(MenuText.Get("书签位置靠近陷阱"));
        if(!Physics.Raycast(point+Vector3.up*.4f,Vector3.down,out var hit,.75f,~0,QueryTriggerInteraction.Ignore)||hit.normal.y<.8f)throw new InvalidOperationException(MenuText.Get("书签需在安全平地记录"));
        foreach(var collider in Physics.OverlapCapsule(point+Vector3.up*.45f,point+Vector3.up*1.4f,.35f,~0,QueryTriggerInteraction.Ignore))
            if(collider!=null && !collider.transform.IsChildOf(game.Player.transform))throw new InvalidOperationException(MenuText.Get("书签位置被对象阻挡"));
        foreach(var interaction in UObject.FindObjectsOfType<InteractableComponent>())
            if(Vector3.Distance(point,interaction.transform.position)<2.3f)throw new InvalidOperationException(MenuText.Get("书签位置靠近门或交互对象"));
    }
    internal void Reset()
    {
        if(queue.Active)CancelBatch();
        if(returning){RollbackReturn(ReadScope(game));if(returning) throw new InvalidOperationException(MenuText.Get("书签原位置恢复未通过回读"));}
        bookmark.Clear();RestoreSpeed();
    }
}
