// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using System.Security.Cryptography;
using System.Text;
using MotherBase.ApplicationFlowManager;
using MotherBase.EntityFramework;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
using UObject=UnityEngine.Object;
namespace RoguePrince.CheatMenu;
internal sealed class SceneActions
{
 internal static SceneActions Current;
 internal static bool TargetAvailable,SpawnAvailable;
 private readonly GameSession game;
 internal readonly BossTargetActions BossTargets;
 private readonly SceneReloadGate reload=new();
 private ApplicationFlowService loadingFlow;
 private TrainingScope enemyScope;
 private CrossMapPoint point;
 private bool returnAfterLoad;
 private bool returning;
 private Vector3 returnOrigin,returnTarget;
 private TrainingScope returnScope;
 private float returnDeadline;
 internal bool NoTarget {get;private set;}
 internal bool NoArenaSpawn {get;private set;}
 internal bool Markers {get;private set;}
 internal bool Colliders {get;private set;}
 internal bool Attacks {get;private set;}
 internal bool Receivers {get;private set;}
 internal bool Active=>Busy||NoTarget||NoArenaSpawn||Markers||Colliders||Attacks||Receivers||BossTargets.Enabled;
 internal bool Busy=>reload.Active||returning;
 internal bool HasPoint=>point!=null;
 internal string PointLabel {get;private set;}="";
 internal string Status {get;private set;}="";
 internal bool Error {get;private set;}
 internal SceneActions(GameSession game){this.game=game;BossTargets=new(game);Current=this;}
 internal static ApplicationFlowService Flow()=>RuntimeServiceLocator.Get<ApplicationFlowService>();
 internal static LevelInstance Level(ApplicationFlowService flow)=>flow?.GetCurrentStateLogic()?.parameters?.levelInstance;
 internal static LevelMap Map(ApplicationFlowService flow)=>Level(flow)?.levelMap;
 private string Report(string text,bool error=false){Status=text;Error=error;Plugin.LogEvent("scene "+text);return text;}
 internal static bool Biome(ApplicationState state)=>state?.stateLogic?.TryCast<BluesteakApplicationStateLogic>() is {} logic && logic.IsBiomeGeneration()&&!logic.isTutorialBiome&&!logic.isHubBiome;
 internal List<LevelTravelActions.Entry> Bosses()
 {
  var flow=Flow();var result=new List<LevelTravelActions.Entry>();
  if(flow?.profile?.applicationStates==null||flow.bossLayers==null)return result;
  foreach(var state in flow.profile.applicationStates)
  {var logic=state?.stateLogic?.TryCast<BluesteakApplicationStateLogic>();if(!Biome(state)||!flow.bossLayers.Contains(logic.layerInGame))continue;result.Add(new(){State=state,Label=ItemBrowser.ReadLabel(logic.biomeDescriptor?.biomeTitle,state.name)});}
  return result.GroupBy(x=>x.Key).Select(x=>x.First()).OrderBy(x=>x.Name).ToList();
 }
 internal string LoadBoss(LevelTravelActions.Entry entry,Action release)
 {
  if(entry==null||!Bosses().Any(x=>x.Key==entry.Key))throw new InvalidOperationException(MenuText.Get("Boss 场地已失效，请刷新"));
  returnAfterLoad=false;return Load(entry.State,release);
 }
 private string Load(ApplicationState state,Action release)
 {
  game.RequireReady();var flow=Flow();var level=Level(flow);
  if(Busy||flow==null||flow.isLoading||level==null||!Biome(state)||flow.FindState(state.stateID)?.guid!=state.guid)throw new InvalidOperationException(MenuText.Get("关卡尚未就绪或正在加载"));
  reload.Begin(state.guid,level.Pointer.ToInt64(),Time.realtimeSinceStartup);loadingFlow=flow;
  try{release();if(Time.timeScale<=0)throw new InvalidOperationException(MenuText.Get("游戏仍被其他界面暂停，请先关闭原生暂停菜单"));RunTools.Current?.MenuLoadRequested(flow,state);flow.LoadState(state);return Report(MenuText.Format("请求重建原生场地：{0}",state.name));}
  catch{reload.Cancel();returnAfterLoad=false;throw;}
 }
 internal string CapturePoint()
 {
  game.RequireReady();var flow=Flow();if(Busy||!TrainingActions.ReadScope(game).Valid)throw new InvalidOperationException(MenuText.Get("训练操作需要当前角色、房间与焦点就绪"));
  var position=game.Player.transform.position;var map=Map(flow);if(map==null||!map.FindRoomAtCoord(position,out var room))throw new InvalidOperationException(MenuText.Get("书签位置无效"));
  TrainingActions.ValidatePosition(game,room,position);
  point=new(RunTools.RunId,flow.GetSeed(),flow.GetCurrentState().guid,Fingerprint(map),position.x,position.y,position.z);
  PointLabel=ItemBrowser.ReadLabel(flow.GetCurrentStateLogic().biomeDescriptor?.biomeTitle,flow.GetCurrentState().name).Display+" "+position;
  return Report(MenuText.Get("已记录本轮地图与安全位置"));
 }
 internal string ReturnPoint(Action release)
 {
  game.RequireReady();var flow=Flow();if(Busy||point==null||flow==null||flow.isLoading||point.Run!=RunTools.RunId||point.Seed!=flow.GetSeed())throw new InvalidOperationException(MenuText.Get("书签本轮或种子已变化，未执行"));
  if(flow.GetCurrentState()?.guid==point.State){BeginReturn(release);return Status;}
  var states=flow.profile.applicationStates;ApplicationState state=null;
  if(states!=null)foreach(var candidate in states)if(candidate?.guid==point.State){state=candidate;break;}
  if(!Biome(state))throw new InvalidOperationException(MenuText.Get("目标关卡已失效，请刷新关卡列表"));
  returnAfterLoad=true;return Load(state,release);
 }
 private static string Fingerprint(LevelMap map)
 {
  if(map?.roomList==null||map.roomList.Count==0)throw new InvalidOperationException(MenuText.Get("地图拓扑不可读，未执行"));
  using var data=new MemoryStream();using var writer=new BinaryWriter(data,Encoding.UTF8,true);
  writer.Write(map.bounds.xMin);writer.Write(map.bounds.xMax);writer.Write(map.bounds.yMin);writer.Write(map.bounds.yMax);
  var rooms=new List<LevelRoomData>();foreach(var room in map.roomList)rooms.Add(room);
  foreach(var room in rooms.OrderBy(r=>r.position.x).ThenBy(r=>r.position.y).ThenBy(r=>r.depth))
  {
   var t=room.template;if(t==null||t.width<=0||t.height<=0||t.colData==null||t.gpeData==null||t.colData.Length!=(long)t.width*t.height||t.gpeData.Length!=t.colData.Length)throw new InvalidOperationException(MenuText.Get("地图拓扑不可读，未执行"));
   writer.Write(room.position.x);writer.Write(room.position.y);writer.Write(t.width);writer.Write(t.height);writer.Write((int)t.type);writer.Write(t.hasBeenFlipped);
   for(int i=0;i<t.colData.Length;i++){writer.Write((int)t.colData[i]);writer.Write((int)t.gpeData[i]);}
  }
  writer.Flush();return Convert.ToHexString(SHA256.HashData(data.ToArray()));
 }
 private void BeginReturn(Action release)
 {
  var flow=Flow();var map=Map(flow);
  if(point==null||!point.CanReturn(RunTools.RunId,flow.GetSeed(),flow.GetCurrentState()?.guid,Fingerprint(map)))throw new InvalidOperationException(MenuText.Get("重建地图与书签拓扑不一致，未写入旧坐标"));
  var target=new Vector3(point.X,point.Y,point.Z);
  if(!map.FindRoomAtCoord(target,out var room))throw new InvalidOperationException(MenuText.Get("书签位置无效"));TrainingActions.ValidatePosition(game,room,target);
  returnOrigin=game.Player.transform.position;returnTarget=target;returnScope=TrainingActions.ReadScope(game);
  if(!map.FindRoomAtCoord(returnOrigin,out var sourceRoom))throw new InvalidOperationException(MenuText.Get("书签位置无效"));TrainingActions.ValidatePosition(game,sourceRoom,returnOrigin);
  release();returning=true;returnDeadline=Time.realtimeSinceStartup+3;
  try{TeleportExtensions.Teleport(game.Player,target,true);Report(MenuText.Get("已请求原生传送，等待位置回读"));}catch{RollbackReturn();throw;}
 }
 private void RollbackReturn()
 {
  var scope=TrainingActions.ReadScope(game);
  if(!SameLife(returnScope,scope)){returning=false;return;}
  var map=Map(Flow());if(map==null||!map.FindRoomAtCoord(returnOrigin,out var room))throw new InvalidOperationException(MenuText.Get("书签原位置恢复未通过回读"));
  TrainingActions.ValidatePosition(game,room,returnOrigin);TeleportExtensions.Teleport(game.Player,returnOrigin,true);
  if(Vector3.Distance(game.Player.transform.position,returnOrigin)>.35f)throw new InvalidOperationException(MenuText.Get("书签原位置恢复未通过回读"));returning=false;
 }
 private static bool SameLife(TrainingScope a,TrainingScope b)=>a.Valid&&b.Valid&&a.Generation==b.Generation&&a.Player==b.Player&&a.Scene==b.Scene;
 internal void Tick()
 {
  if(point!=null&&point.Run!=RunTools.RunId){point=null;PointLabel="";}
  var scope=TrainingActions.ReadScope(game);
  if((NoTarget||NoArenaSpawn)&&(!enemyScope.Matches(scope)||!Application.isFocused))ResetEnemies();
  BossTargets.Tick();
  if((Markers||Colliders||Attacks||Receivers)&&(!scope.Valid||!Application.isFocused)){Markers=Colliders=Attacks=Receivers=false;}
  if(reload.Active)
  {
   var current=Flow();if(current==null||current.Pointer!=loadingFlow.Pointer){reload.Cancel();returnAfterLoad=false;Report(MenuText.Get("场景请求失效，未自动重试"),true);return;}
   var result=reload.Observe(current.GetCurrentState()?.guid,Level(current)?.Pointer.ToInt64()??0,current.isLoading,game.Ready&&scope.Valid,Time.realtimeSinceStartup);
   if(result=="completed") {Report(MenuText.Get("原生场地已重建，保持当前配装"));if(returnAfterLoad){returnAfterLoad=false;try{BeginReturn(()=>{});}catch(Exception e){Report(e.Message,true);}}}
   else if(result=="failed"){returnAfterLoad=false;Report(MenuText.Get("场景请求失效，未自动重试"),true);}
  }
  if(returning)
  {
   if(!SameLife(returnScope,scope)){returning=false;Report(MenuText.Get("书签返回已中止：作用域变化，未向新场景写入旧坐标"),true);}
   else if(Vector3.Distance(game.Player.transform.position,returnTarget)<.35f&&game.Player.GetComponent<RoomStateComponent>()?.currentRoom?.IsInRoom(returnTarget)==true){returning=false;Report(MenuText.Get("已返回跨房间书签"));}
   else if(Time.realtimeSinceStartup>=returnDeadline){RollbackReturn();Report(MenuText.Get("书签位置回读失败，已请求恢复原位置"),true);}
  }
 }
 internal string SetTargets(bool enabled)
 {if(enabled&&!TargetAvailable)throw new InvalidOperationException(MenuText.Get("原生补丁不可用"));RequireEnemyScope();NoTarget=enabled;return Report(MenuText.Get(enabled?"普通怪物目标过滤已开启":"普通怪物目标过滤已关闭"));}
 internal string SetSpawn(bool enabled)
 {if(enabled&&!SpawnAvailable)throw new InvalidOperationException(MenuText.Get("原生补丁不可用"));RequireEnemyScope();NoArenaSpawn=enabled;return Report(MenuText.Get(enabled?"当前房间竞技场与已接入召唤暂停":"刷怪队列已恢复原生运行"));}
 private void RequireEnemyScope(){var scope=TrainingActions.ReadScope(game);if(!scope.Valid||!Application.isFocused)throw new InvalidOperationException(MenuText.Get("训练操作需要当前角色、房间与焦点就绪"));enemyScope=scope;}
 internal bool Enemy(Entity entity,bool allowBoss=false)
 {
  if(!Application.isFocused||entity==null||entity.destroyed||!entity.gameObject.activeInHierarchy||entity.gameObject.scene.handle!=enemyScope.Scene||entity.context==null||game.Player?.context==null||entity.context.Pointer!=game.Player.context.Pointer||!enemyScope.Matches(TrainingActions.ReadScope(game)))return false;
  var mob=entity.GetComponent<MobComponent>();if(mob==null||(!allowBoss&&mob.isBoss))return false;
  var room=entity.GetComponent<RoomStateComponent>()?.currentRoom;
  if(room?.template==null){var map=Map(Flow());if(map==null||!map.FindRoomAtCoord(entity.transform.position,out room))return false;}
  return MobRoomIdentity.Matches(game.Player.GetComponent<RoomStateComponent>()?.currentRoom,room);
 }
 internal bool BlockArena(MobArenaComponent arena)=>NoArenaSpawn&&Application.isFocused&&arena!=null&&arena.gameObject.scene.handle==enemyScope.Scene&&enemyScope.Matches(TrainingActions.ReadScope(game))&&game.Player.GetComponent<RoomStateComponent>().currentRoom.IsInRoom(arena.transform.position);
 internal bool BlockSummon(Entity owner,Vector3 position)=>SceneSummonGate.Block(game,NoArenaSpawn,owner,position,Enemy);
 internal void FilterTarget(Entity enemy)
 {
  if(!NoTarget||!Application.isFocused||!Enemy(enemy))return;
  var target=enemy.GetComponent<TargetComponent>();if(target==null)return;
  if(target.target?.Pointer==game.PlayerPointer)target.target=null;
  if(target.lastTarget?.Pointer==game.PlayerPointer){target.lastTarget=null;target.hasLastTargetPosition=false;}
 }
 internal bool RejectTarget(Entity enemy,Entity target)=>NoTarget&&Application.isFocused&&target?.Pointer==game.PlayerPointer&&Enemy(enemy);
 internal string ClearRoom()
 {
  RequireEnemyScope();int count=0;
  foreach(var health in UObject.FindObjectsOfType<Damageable>())
  {var enemy=health.GetComponent<Entity>();if(!health.isAlive||health.onDieActionStarted||!Enemy(enemy))continue;health.Kill(game.Player,null);count++;}
  return Report(MenuText.Format("已请求原生死亡：{0} 个普通怪物",count));
 }
 internal string RevealMap()
 {
  game.RequireReady();var scope=TrainingActions.ReadScope(game);if(!scope.Valid)throw new InvalidOperationException(MenuText.Get("地图尚未就绪"));
  var maps=UObject.FindObjectsOfType<MiniMapDataComponent>().Where(x=>x.gameObject.scene.handle==scope.Scene&&x.minimapDataVariable?.CurrentValue?.Pointer==x.Pointer).ToArray();
  if(maps.Length!=1||maps[0].fogOfWarTexture==null)throw new InvalidOperationException(MenuText.Get("地图尚未就绪"));
  maps[0].FullRevealMap();maps[0].UpdateFogOfWar();return Report(MenuText.Get("已揭示当前地图"));
 }
 internal string ToggleOverlay(int kind)
 {game.RequireReady();if(!Application.isFocused||!TrainingActions.ReadScope(game).Valid)throw new InvalidOperationException(MenuText.Get("地图尚未就绪"));switch(kind){case 0:Markers=!Markers;break;case 1:Colliders=!Colliders;break;case 2:Attacks=!Attacks;break;case 3:Receivers=!Receivers;break;}return Report(MenuText.Get("场景显示已切换"));}
 internal void DisableReceivers(Exception error){Receivers=false;Report(MenuText.Get("受击显示读取失败，已关闭；可重新开启"),true);Plugin.MenuLog.LogError("[RPCheat] receiver_overlay_disabled "+error);}
 internal void ResetEnemies(){NoTarget=NoArenaSpawn=false;}
 internal void Restore(bool rebind=false)
 {BossTargets.Clear();ResetEnemies();Markers=Colliders=Attacks=Receivers=false;if(returning)RollbackReturn();if(!rebind){reload.Cancel();returnAfterLoad=false;}}
 internal void ClearPoint(){point=null;PointLabel="";}
}

