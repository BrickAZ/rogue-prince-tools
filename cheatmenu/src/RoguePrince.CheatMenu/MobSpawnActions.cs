// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
using MotherBase.EntityFramework;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;

namespace RoguePrince.CheatMenu;

internal sealed class MobSpawnActions : IDisposable
{
    private sealed class Owned
    {
        internal GameObject Object;
        internal Entity Entity;
        internal long Generation;
        internal int Scene;
        internal bool Removing;
        internal long Id;
        internal string MobId,Name;
        internal bool Boss,AiEnabled;
        internal MobSpawnHealthMode HealthMode;
        internal TrainingScope Scope;
        internal BossSpawnCombat Combat;
    }
    private readonly GameSession game;
    private readonly MobBrowser browser = new();
    private readonly MobSpawnRequestGate gate = new();
    private readonly List<Owned> owned = new();
    private readonly Stack<Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase> routines = new();
    private TriggerMobSpawner spawner;
    private TriggerMobSpawner._SpawnMob_d__23 spawn;
    private Owned pending;
    private MobBrowser.Entry requestedMob;
    private Vector3 requestedFrom, position;
    private LevelRoomData requestedRoom;
    private int scene, setHealthFrame;
    private long requestId;
    private long ownedSequence;
    internal long CurrentRequestId=>requestId;
    internal long LastSuccessfulRequest {get;private set;}
    internal long LastSuccessfulOwned {get;private set;}
    internal event Action<long,bool> Retired;
    private float deadline, nextRequest, expectedHealth, expectedMaximum;
    private bool healthWritten;
    internal bool Busy => gate.Busy;
    internal bool HasOwned => owned.Count != 0;
    internal int Count => owned.Count(o => o.Object != null && o.Entity != null && !o.Entity.destroyed && !o.Removing);
    internal string Status { get; private set; } = MenuText.Get("选择怪物、血量与 AI 后生成一只");
    internal bool Error { get; private set; }
    internal MobSpawnActions(GameSession game) => this.game = game;
    internal List<MobBrowser.Entry> Filter(bool boss,string query)=>browser.Filter(boss,query);

    internal bool Request(MobSpawnOptions options)
    {
        game.RequireReady();
        if (!Application.isFocused || Busy || Time.realtimeSinceStartup < nextRequest) return false;
        if (!MobAiControl.Available) throw new InvalidOperationException(MenuText.Get("召唤控制补丁未就绪，请保留日志"));
        var entry=browser.Find(options.MobId);
        if(entry?.Data.Supported!=true)throw new InvalidOperationException(MenuText.Get("请先选择可召唤的怪物"));
        // Take the scope and safe floor before starting any asynchronous work.
        var room = CurrentRoom();
        if (room?.template == null) throw new InvalidOperationException(MenuText.Get("当前房间未就绪"));
        position = FindPosition(room);
        requestedFrom = game.Player.transform.position;
        requestedRoom = room;
        scene = SceneManager.GetActiveScene().handle;
        if (!gate.TryBegin(game.Generation, options, out requestId)) return false;
        requestedMob=entry;
        deadline = Time.realtimeSinceStartup + 25;
        healthWritten = false;
        Error = false;
        Status = MenuText.Format("正在生成：{0}，完成后可重新打开菜单查看结果",entry.Name);
        Plugin.LogEvent($"mob_request id={requestId} mob={entry.InternalName} boss={entry.Data.Boss} generation={game.Generation} mode={options.HealthMode} ai={options.AiEnabled} position={position} room=({room.position.x},{room.position.y}) template={room.template.Pointer} scene={scene}");
        try { browser.Reload(entry); }
        catch (Exception e) { Fail(e); return false; }
        return true;
    }

    private LevelRoomData CurrentRoom() => game.Ready ? game.Player.GetComponent<RoomStateComponent>()?.currentRoom : null;

    internal void Tick()
    {
        Prune();
        if (!Busy) return;
        try
        {
            RequireScope(game.Ready, "player_not_ready");
            var activePlayer = game.Saver?.mainCharacterVariable?.CurrentValue;
            RequireScope(activePlayer != null && activePlayer.Pointer == game.PlayerPointer, "player_changed");
            RequireScope(gate.CanComplete(requestId, game.Generation), "generation_changed");
            RequireScope(Application.isFocused, "focus_lost");
            RequireScope(SceneManager.GetActiveScene().handle == scene, "scene_changed");
            var room = CurrentRoom();
            if (!MobRoomIdentity.Matches(requestedRoom, room))
            {
                Plugin.LogEvent($"mob_room_mismatch id={requestId} expected=({requestedRoom?.position.x},{requestedRoom?.position.y}) template={requestedRoom?.template?.Pointer} actual=({room?.position.x},{room?.position.y}) template={room?.template?.Pointer}");
                RequireScope(false, "room_changed");
            }
            RequireScope(pending?.Combat?.AiReleased==true||Vector3.Distance(game.Player.transform.position, requestedFrom) <= 1.5f, "player_moved");
            if (Time.realtimeSinceStartup > deadline)
                throw new TimeoutException(MenuText.Get("召唤超时，请保留日志") + (spawn == null ? " [asset]" : pending?.Boss==true ? " [boss-combat]" : healthWritten ? " [health-readback]" : " [native-init]"));
            browser.Tick();
            if (!browser.Ready(gate.Options.MobId)) return;
            if (spawn == null) StartNative();
            PumpNative();
            if (routines.Count != 0) return;
            if (pending?.Entity == null || pending.Entity.destroyed)
                throw new InvalidOperationException(MenuText.Get("原生生成器未返回有效怪物"));
            var health = pending.Entity.GetComponent<Damageable>();
            if (health == null) throw new InvalidOperationException(MenuText.Get("生成实例缺少生命组件"));
            if(pending.Boss)
            {
                pending.Combat??=new BossSpawnCombat(pending.Entity,game.Player,requestedRoom,gate.Options.HealthMode);
                if(!pending.Combat.Tick(gate.Options.AiEnabled))return;
            }
            else
            {
            if (!health._initialized) return;
            if (!health.isAlive || health.onDieActionStarted) throw new InvalidOperationException(MenuText.Get("生成实例在初始化期间死亡"));
            if (!healthWritten)
            {
                expectedMaximum = health.maxHealth;
                if (!MobSpawnPolicy.TryResolveHealth(expectedMaximum, gate.Options.HealthMode, out expectedHealth))
                    throw new InvalidOperationException(MenuText.Get("原生生命上限无效"));
                health.currentHealth = expectedHealth; // No damage/heal event; never SetHpToOne.
                healthWritten = true;
                setHealthFrame = Time.frameCount;
                return;
            }
            if (Time.frameCount == setHealthFrame) return;
            if (Math.Abs(health.maxHealth - expectedMaximum) > .01f || Math.Abs(health.currentHealth - expectedHealth) > .01f)
                throw new InvalidOperationException(MenuText.Get("出生生命被后续初始化改变，已取消本次生成"));
            MobAiControl.Ready(pending.Entity);
            }
            LastSuccessfulRequest=requestId;
            LastSuccessfulOwned=pending.Id;
            Status = MenuText.Format("已生成 {0}：{1:0.#}/{2:0.#} HP · AI {3}",requestedMob.Name, health.currentHealth, health.maxHealth,
                MenuText.Get(gate.Options.AiEnabled ? "开启" : "关闭"));
            Plugin.LogEvent($"mob_spawn_complete id={requestId} mob={gate.Options.MobId} boss={requestedMob.Data.Boss} mode={gate.Options.HealthMode} ai={gate.Options.AiEnabled} hp={health.currentHealth}/{health.maxHealth} entity={pending.Entity.GetInstanceID()} position={pending.Entity.transform.position} nativeAttackObserved={pending.Combat?.AttackObserved==true}");
            pending = null; // Ownership remains in the exact instance list.
            FinishRequest();
        }
        catch (Exception e) { Fail(e); }
    }

    private void RequireScope(bool valid, string reason)
    {
        if (valid) return;
        Plugin.LogEvent($"mob_scope_rejected id={requestId} reason={reason}");
        throw new InvalidOperationException(MenuText.Get("召唤已取消：角色、房间或焦点发生变化") + " [" + reason + "]");
    }

    private void StartNative()
    {
        // Revalidate physical clearance after loading; do not spawn into a moving actor.
        CheckClearance(position);
        var host = new GameObject("RPCheat-MobSpawner");
        host.SetActive(false); // Never run the native debug toolbar's Start().
        spawner = host.AddComponent<TriggerMobSpawner>();
        spawner.enabled = false;
        spawner.player = game.Saver.mainCharacterVariable;
        spawner.affectMobsDataList = new Il2CppSystem.Collections.Generic.List<TriggerMobSpawner.DebugAffectReference>();
        var playerTransform = game.Player.transform;
        if (!MobSpawnPlacement.TryOffset(playerTransform, position, out var offset))
        {
            Plugin.LogEvent($"mob_placement_rejected id={requestId} forwardX={playerTransform.forward.x:R} rightX={playerTransform.right.x:R} player={playerTransform.position} target={position}");
            throw new InvalidOperationException(MenuText.Get("角色朝向或位置暂不可用，请站稳后重试"));
        }
        spawner.SpawnOffset = offset;
        // Tick calls PumpNative immediately after this method, without a yield.
        // Native MoveNext caches _spawnPos before it can wait for loading.
        Plugin.LogEvent($"mob_placement_ready id={requestId} frame={Time.frameCount} forwardX={playerTransform.forward.x:R} rightX={playerTransform.right.x:R} player={playerTransform.position} target={position} offset={offset}");
        RequireScope(requestedMob?.InternalName==gate.Options.MobId&&requestedMob.Rule!=null,"selected_target_changed");
        var iterator = spawner.SpawnMob(requestedMob.Rule, false, false, "");
        spawn = iterator.Cast<TriggerMobSpawner._SpawnMob_d__23>();
        routines.Push(iterator);
        Plugin.LogEvent($"mob_native_begin id={requestId}");
    }

    private void PumpNative()
    {
        // Native TaskCoroutine owns its loading coroutine; poll its concrete API.
        // We never run the spawning iterator in a detached/global coroutine.
        for (int steps = 0; steps < 16 && routines.Count > 0; steps++)
        {
            var currentObject = routines.Peek();
            var task = currentObject.TryCast<TaskCoroutine>();
            if (task != null)
            {
                // TaskCoroutine drives a separate loading task. Poll it once;
                // do not recursively drive its Current or its child coroutine.
                if (task.MoveNext()) return;
                routines.Pop();
                continue;
            }
            var routine = currentObject.Cast<Il2CppSystem.Collections.IEnumerator>();
            bool more;
            try { more = routine.MoveNext(); }
            finally { CaptureInstance(); }
            if (!more) { routines.Pop(); continue; }
            var current = routine.Current;
            if (current == null) return;
            var childTask = current.TryCast<TaskCoroutine>();
            if (childTask != null) { routines.Push(childTask); continue; }
            var nested = current.TryCast<Il2CppSystem.Collections.IEnumerator>();
            if (nested == null) throw new InvalidOperationException("Unsupported spawn yield: " + current.GetIl2CppType().FullName);
            if (nested.Pointer == routine.Pointer) return;
            routines.Push(nested);
        }
    }

    private void CaptureInstance()
    {
        if (pending != null || spawn == null || spawn._mobInstance_5__4 == null) return;
        var obj = spawn._mobInstance_5__4;
        var entity = obj.GetComponent<Entity>();
        pending = new Owned { Object=obj, Entity=entity, Generation=game.Generation, Scene=scene,
            Id=++ownedSequence,MobId=gate.Options.MobId,Name=requestedMob.Name,Boss=requestedMob.Data.Boss,
            AiEnabled=gate.Options.AiEnabled,HealthMode=gate.Options.HealthMode,Scope=TrainingActions.ReadScope(game) };
        owned.Add(pending); // Capture even incomplete objects for failure cleanup.
        if (entity == null) throw new InvalidOperationException("Spawned prefab has no Entity");
        MobAiControl.Register(entity, gate.Options.AiEnabled);
        if(pending.Boss)
        {
            pending.Combat=new BossSpawnCombat(entity,game.Player,requestedRoom,gate.Options.HealthMode);
            pending.Combat.IsolatePublishers();
        }
        if (Vector3.Distance(obj.transform.position, position) > .5f)
            throw new InvalidOperationException($"Native spawn position mismatch: target={position} actual={obj.transform.position} nativeTarget={spawn._spawnPos_5__2}");
        Plugin.LogEvent($"mob_instance_captured id={requestId} entity={entity.GetInstanceID()} target={position} actual={obj.transform.position} nativeTarget={spawn._spawnPos_5__2}");
    }

    private void FinishRequest()
    {
        gate.Complete(requestId);
        nextRequest = Time.realtimeSinceStartup + .6f;
        StopRoutines();
        if (spawner != null) UObject.Destroy(spawner.gameObject);
        spawner = null;
        spawn = null;
        requestedRoom = null;
        requestedMob = null;
        healthWritten = false;
    }

    private void StopRoutines()
    {
        foreach (var routine in routines)
        {
            try
            {
                var task = routine.TryCast<TaskCoroutine>();
                if (task != null) task.Cleanup();
                else routine.TryCast<Il2CppSystem.IDisposable>()?.Dispose();
            }
            catch (Exception e) { Plugin.MenuLog.LogWarning("[RPCheat] mob_coroutine_cleanup " + e.Message); }
        }
        routines.Clear();
    }

    private void Fail(Exception e)
    {
        Error = true;
        Status = MenuText.Get("生成失败：") + e.Message;
        Plugin.MenuLog.LogError($"[RPCheat] mob_spawn_failed id={requestId} {e}");
        try { if (pending != null) Remove(pending); }
        finally { pending = null; FinishRequest(); }
        browser.Dispose(); // Allow retry after failed/timeout asset load.
    }

    private void Remove(Owned item)
    {
        if (item.Removing || item.Object == null) return;
        // Entity.OnDestroy unregisters from native EntityContext (verified native
        // calls in evidence). This is not Kill.
        MobAiControl.Hold(item.Entity);
        UObject.Destroy(item.Object);
        item.Combat?.Dispose();item.Combat=null;
        item.Removing = true;
        Retired?.Invoke(item.Id,false);
    }
    private void Prune()
    {
        for (int i=owned.Count-1; i>=0; i--)
        {
            var item = owned[i];
            if (item.Object == null || item.Entity == null || item.Entity.destroyed)
            {
                if(!item.Removing)Retired?.Invoke(item.Id,false);
                if (item.Entity != null) MobAiControl.Forget(item.Entity.Pointer);
                item.Combat?.Dispose();item.Combat=null;
                owned.RemoveAt(i);
                continue;
            }
            if (item.Generation != game.Generation || item.Scene != SceneManager.GetActiveScene().handle||
                item.Boss&&!item.Scope.Matches(TrainingActions.ReadScope(game))) Remove(item);
            else if(item!=pending && !item.Removing)
            {
                var health=item.Entity.GetComponent<Damageable>();
                if(health!=null && health._initialized && (!health.isAlive || health.onDieActionStarted))
                {
                    if(item.Boss){Remove(item);}
                    else {MobAiControl.Forget(item.Entity.Pointer);owned.RemoveAt(i);Retired?.Invoke(item.Id,true);}
                }
            }
        }
    }
    internal sealed class Target
    {
        internal readonly long Id;
        internal readonly string MobId,Name;
        internal readonly bool Boss,AiEnabled;
        internal readonly float Health,Maximum;
        internal Target(long id,string mobId,string name,bool boss,bool aiEnabled,float health,float maximum)
        {Id=id;MobId=mobId;Name=name;Boss=boss;AiEnabled=aiEnabled;Health=health;Maximum=maximum;}
    }
    internal List<Target> Targets()
    {
        Prune();var scope=TrainingActions.ReadScope(game);var result=new List<Target>();
        foreach(var item in owned)
        {
            if(item==pending || item.Removing || !item.Scope.Matches(scope) || item.Object==null || item.Entity==null || item.Entity.destroyed)continue;
            var health=item.Entity.GetComponent<Damageable>();
            if(health==null || !health._initialized || !health.isAlive || health.onDieActionStarted)continue;
            result.Add(new(item.Id,item.MobId,item.Name,item.Boss,item.AiEnabled,health.currentHealth,health.maxHealth));
        }
        return result;
    }
    private Owned RequireTarget(long id,bool ordinary)
    {
        game.RequireReady();
        if(Busy || !Application.isFocused)throw new InvalidOperationException(MenuText.Get("目标操作暂不可用"));
        Prune();var scope=TrainingActions.ReadScope(game);
        var item=owned.FirstOrDefault(o=>o.Id==id && o!=pending && !o.Removing && o.Scope.Matches(scope) && o.Object!=null && o.Entity!=null && !o.Entity.destroyed);
        if(item==null || ordinary&&item.Boss)throw new InvalidOperationException(MenuText.Get("目标已失效或不是普通训练怪物"));
        var health=item.Entity.GetComponent<Damageable>();
        if(health==null || !health._initialized || !health.isAlive || health.onDieActionStarted)throw new InvalidOperationException(MenuText.Get("目标已死亡或未初始化"));
        return item;
    }
    internal Entity TargetEntity(long id)=>RequireTarget(id,true).Entity;
    internal void RestoreTargetAi(long id,IntPtr expected,bool enabled)
    {
        // Cleanup owns this exact native instance; it must also work after focus/room changes.
        var item=owned.FirstOrDefault(o=>o.Id==id&&o.Entity?.Pointer==expected&&!o.Removing);
        if(item==null||item.Object==null||item.Entity==null||item.Entity.destroyed)return;
        var health=item.Entity.GetComponent<Damageable>();if(health==null||!health._initialized||!health.isAlive||health.onDieActionStarted)return;
        if(item.AiEnabled!=enabled){MobAiControl.SetEnabled(item.Entity,enabled);item.AiEnabled=enabled;}
    }
    internal string Refill(long id)
    {
        var item=RequireTarget(id,true);var health=item.Entity.GetComponent<Damageable>();
        var maximum=health.maxHealth;if(!float.IsFinite(maximum)||maximum<=0)throw new InvalidOperationException(MenuText.Get("原生生命上限无效"));
        health.currentHealth=maximum;
        if(Math.Abs(health.currentHealth-maximum)>.01f)throw new InvalidOperationException(MenuText.Get("训练目标生命回读失败"));
        return MenuText.Get("训练目标已回满生命");
    }
    internal string ToggleAi(long id)
    {
        var item=RequireTarget(id,true);MobAiControl.SetEnabled(item.Entity,!item.AiEnabled);item.AiEnabled=!item.AiEnabled;
        return MenuText.Get(item.AiEnabled?"训练目标 AI 已开启":"训练目标 AI 已关闭");
    }
    internal string Respawn(long id)
    {
        var item=RequireTarget(id,true);
        if(!Request(new(item.HealthMode,item.AiEnabled,item.MobId)))throw new InvalidOperationException(MenuText.Get("生成尚在冷却，请稍后重试"));
        return Status; // Keep the original target until the user explicitly clears it.
    }
    internal string RemoveTarget(long id)
    {
        var item=RequireTarget(id,false);Remove(item);return MenuText.Get("已请求清理选中目标");
    }
    internal string ClearOwned()
    {
        var count = Count;
        gate.Invalidate();
        StopRoutines();
        foreach (var item in owned) Remove(item);
        pending = null;
        FinishRequest();
        Status = MenuText.Format("已请求清理本菜单生成的 {0} 只怪物", count);
        Error = false;
        Plugin.LogEvent("mob_clear_owned count=" + count);
        return Status;
    }
    internal void CancelPending()
    {
        if (Busy) Fail(new OperationCanceledException(MenuText.Get("召唤已取消：角色、房间或焦点发生变化")));
    }
    internal void Reset() { ClearOwned(); browser.Dispose(); }
    public void Dispose() => Reset();

    internal Vector3 RewardPosition(){game.RequireReady();var room=CurrentRoom();if(room?.template==null)throw new InvalidOperationException(MenuText.Get("当前房间未就绪"));return FindPosition(room);}
    private Vector3 FindPosition(LevelRoomData room)
    {
        var platformState = game.Player.GetComponent<PlatformStateComponent>();
        if (platformState == null || !platformState.currentPlatform.isValid())
            throw new InvalidOperationException(MenuText.Get("请站在足够宽的平地上再召唤"));
        var platform = platformState.currentPlatform;
        var player = game.Player.transform.position;
        foreach (float distance in new[] {3f,-3f,4f,-4f,5f,-5f,6f,-6f})
        {
            float px = player.x + distance;
            if (px < Math.Min(platform.leftEdge.x,platform.rightEdge.x)+1.2f ||
                px > Math.Max(platform.leftEdge.x,platform.rightEdge.x)-1.2f) continue;
            int x = Mathf.FloorToInt(px-room.position.x);
            int y = Mathf.RoundToInt(platform.leftEdge.y-room.position.y);
            for (int dy=-1; dy<=1; dy++)
            {
                int cy=y+dy;
                if (!SafeCells(room,x,cy)) continue;
                var point=new Vector3(room.position.x+x+.5f,room.position.y+cy+.08f,player.z);
                if (Math.Abs(point.y-player.y)>1.2f || Math.Abs(point.x-player.x)<2.5f) continue;
                try { CheckClearance(point); return point; } catch (InvalidOperationException) { }
            }
        }
        throw new InvalidOperationException(MenuText.Get("附近没有安全召唤位置，请远离门、NPC、怪物和陷阱"));
    }

    private static bool SafeCells(LevelRoomData room,int x,int y)
    {
        var t=room.template;
        if (x<2 || x>=t.width-2 || y<1 || y+3>=t.height) return false;
        for (int dx=-1;dx<=1;dx++)
        {
            if ((int)t.gpeData[t.GetIndex(x+dx,y-1)] is not (27 or 28)) return false;
            for (int dy=0;dy<=2;dy++)
                if ((int)t.gpeData[t.GetIndex(x+dx,y+dy)]!=0 || (int)t.colData[t.GetIndex(x+dx,y+dy)]==6) return false;
        }
        for(int dx=-2;dx<=2;dx++)for(int dy=-1;dy<=3;dy++)
            if ((int)t.gpeData[t.GetIndex(x+dx,y+dy)] is 3 or 4 or 5 or 6 or 8 or 10 or 19 or 20) return false;
        return true;
    }

    private void CheckClearance(Vector3 point)
    {
        if (!Physics.Raycast(point+Vector3.up*.4f, Vector3.down, out var hit, .75f, ~0, QueryTriggerInteraction.Ignore) || hit.normal.y<.8f)
            throw new InvalidOperationException("No flat physical floor");
        foreach(var obstacle in Physics.OverlapCapsule(point+Vector3.up*.45f,point+Vector3.up*1.4f,.35f,~0,QueryTriggerInteraction.Ignore))
            if(obstacle!=null) throw new InvalidOperationException("Spawn body space occupied");
        foreach(var interaction in UObject.FindObjectsOfType<InteractableComponent>())
            if (Vector3.Distance(point,interaction.transform.position)<2.3f)
                throw new InvalidOperationException("Spawn too near interaction");
        foreach(var entity in UObject.FindObjectsOfType<Damageable>())
            if (Vector3.Distance(point,entity.transform.position)<2f)
                throw new InvalidOperationException("Spawn too near actor");
    }
}
