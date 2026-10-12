// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using Il2CppInterop.Runtime.Attributes;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
namespace RoguePrince.CheatMenu;
public sealed partial class MenuController
{
 private SceneActions scenes;
 private SceneCamera sceneCamera;
 private SceneOverlay sceneOverlay;
 private RunTools runTools;
 private RunRestartActions runRestart;
 private FrameObservationActions frameObservation;
 private bool FrameObservationActive=>frameObservation?.Enabled==true||frameObservation?.RecoveryPending==true;
 private bool CoreWorkBusy=>BossPhaseWorkBusy||FrameObservationActive||abilities?.Active==true||playerCheckpoint?.RecoveryPending==true;
 private bool restartConfirmation;
 private string restartDraft;
 private bool showRestartStatus;
 private List<LevelTravelActions.Entry> bossArenas=new();
 private string chosenBoss;
 private int bossPage;
 private bool overrideAwakening;
 private bool SceneReady()=>game.Ready&&!levels.Loading&&!scenes.Busy&&!CoreWorkBusy&&!runRestart.Busy&&!mobs.Busy&&!training.Busy&&!waves.Active&&!loadouts.RecoveryPending&&!affixes.RecoveryPending&&!rescue.RecoveryPending;
 [HideFromIl2Cpp]
 private void SceneLoad(Func<string> load)
 {Do(MenuText.Get("场景工具"),()=>{if(!SceneReady())throw new InvalidOperationException(MenuText.Get("先完成当前请求或恢复操作"));RestoreTraining();return load();});}
 [HideFromIl2Cpp]
 private void SceneStatus()
 {Text(28,557,866,48,scenes.Status,15,scenes.Error,true);}
 [HideFromIl2Cpp]
 private void DrawBossPractice()
 {
  Text(28,230,866,56,MenuText.Get("进入原生 Boss 场地并保持当前配装；重复挑战会重新加载地图。死亡与奖励仍遵循原版。"),17,false,true);
  var pages=Math.Max(1,(bossArenas.Count+4)/5);bossPage=Math.Clamp(bossPage,0,pages-1);
  for(int i=0;i<5&&bossPage*5+i<bossArenas.Count;i++)
  {var entry=bossArenas[bossPage*5+i];if(Button("arena"+i,28,294+i*41,605,entry.Name,true,chosenBoss==entry.Key))chosenBoss=entry.Key;}
  var selectedArena=bossArenas.FirstOrDefault(x=>x.Key==chosenBoss);
  if(Button("enterArena",651,294,244,MenuText.Get("进入 / 重建场地"),SceneReady()&&selectedArena!=null,false,16))SceneLoad(()=>scenes.LoadBoss(selectedArena,ReleaseForTravel));
  var flow=SceneActions.Flow();var current=bossArenas.FirstOrDefault(x=>x.Key==flow?.GetCurrentState()?.guid);
  if(Button("repeatArena",651,342,244,MenuText.Get("重打当前 Boss"),SceneReady()&&current!=null,false,16))SceneLoad(()=>scenes.LoadBoss(current,ReleaseForTravel));
  if(Button("arenaLoadout",651,390,244,MenuText.Get("选择完整配装"))){utilityPage=1;utilitySection=8;}
  Text(651,438,244,65,MenuText.Get("先在配装页应用预设，再进入场地。"),16,false,true);
  if(Button("arenaPrev",28,514,122,MenuText.Get("上一页"),bossPage>0))bossPage--;
  Text(170,514,460,36,MenuText.Format("第 {0} / {1} 页 · {2} 项",bossPage+1,pages,bossArenas.Count),16);
  if(Button("arenaNext",650,514,244,MenuText.Get("下一页"),bossPage+1<pages))bossPage++;
  SceneStatus();
 }
 [HideFromIl2Cpp]
 private void DrawCrossMap()
 {
  Text(28,230,866,72,MenuText.Get("记录本轮的关卡、种子、地图指纹与安全位置。返回时先加载目标关卡，再核对拓扑；不匹配则只提示。"),17,false,true);
  Text(28,323,866,65,scenes.HasPoint?scenes.PointLabel:MenuText.Get("未记录 / 已失效"),20,true,true);
  if(Button("crossCapture",28,414,277,MenuText.Get("记录地图书签"),SceneReady()))Do(MenuText.Get("记录地图书签"),scenes.CapturePoint);
  if(Button("crossReturn",320,414,277,MenuText.Get("返回地图书签"),SceneReady()&&scenes.HasPoint))SceneLoad(()=>scenes.ReturnPoint(ReleaseForTravel));
  if(Button("crossClear",613,414,281,MenuText.Get("删除地图书签"),!scenes.Busy&&scenes.HasPoint))Do(MenuText.Get("删除地图书签"),()=>{scenes.ClearPoint();return MenuText.Get("书签已删除");});
  Text(28,477,866,62,MenuText.Get("仅本轮保留；新局清除。随机地图重建可能变化，因此不能保证任意旧坐标都能返回。"),17,false,true);SceneStatus();
 }
 [HideFromIl2Cpp]
 private void DrawRunSettings()
 {
  Text(28,230,85,36,MenuText.Get("种子"),17);var seed=Field("nextRunSeed",126,230,264,"42");
  if(Button("seedUseCurrent",412,230,226,MenuText.Get("填入当前种子"),game.Ready))Do(MenuText.Get("填入当前种子"),()=>{fields["nextRunSeed"]=SceneActions.Flow().GetSeed().ToString();return MenuText.Get("种子已填入");});
  if(Button("seedArm",655,230,240,MenuText.Get("预约原生新局"),RunTools.Available&&!levels.Loading&&!scenes.Busy&&!runRestart.Busy))Do(MenuText.Get("预约原生新局"),()=>{if(runRestart.Busy)throw new InvalidOperationException(MenuText.Get("先完成或取消当前重开及新局预约"));var result=runTools.Arm(fields["nextRunSeed"],fields.TryGetValue("nextRunLevel",out var level)?level:"0",overrideAwakening);showRestartStatus=false;return result;});
  if(Button("overrideLevel",28,278,415,MenuText.Get("覆盖觉醒等级（不改觉醒卡）"),!runTools.SeedPending,overrideAwakening,16))overrideAwakening=!overrideAwakening;
  var levelText=Field("nextRunLevel",466,278,170,"0");
  if(Button("seedCancel",655,278,240,MenuText.Get("取消新局预约"),runTools.SeedPending&&!runRestart.Busy))Do(MenuText.Get("取消新局预约"),()=>{var result=runTools.CancelStart();showRestartStatus=false;return result;});
  Text(28,329,866,62,MenuText.Get("按原版方式开始下一轮后生效。可反复填入同一种子；其他随机源、解锁、觉醒卡和路线仍影响结果。"),16,false,true);
  if(Button("restoreLevel",28,400,305,MenuText.Get("恢复原生觉醒等级"),runTools.ActiveDifficulty.HasValue&&!runRestart.Busy))Do(MenuText.Get("恢复原生觉醒等级"),()=>{if(runRestart.Busy)throw new InvalidOperationException(MenuText.Get("先完成或取消当前重开及新局预约"));var result=runTools.RestoreDifficulty();showRestartStatus=false;return result;});
  Text(357,400,537,36,runTools.ActiveDifficulty.HasValue?MenuText.Format("本轮覆盖等级：{0}",runTools.ActiveDifficulty.Value):MenuText.Get("觉醒等级：原生"),17);
  var draft=$"{seed}|{levelText}|{overrideAwakening}|{game.Generation}|{RunTools.RunId}|{SceneActions.Flow()?.GetCurrentState()?.guid}";
  if(draft!=restartDraft){restartDraft=draft;restartConfirmation=false;}
  if(Button("runRestart",28,454,420,MenuText.Get(restartConfirmation?"确认放弃本轮并重开":"放弃本轮并重开"),SceneReady()&&!runTools.Pending&&!rescue.Pending&&!sceneCamera.Active&&!photo.HasState&&RunRestartActions.Available,false,16))
  {
   if(!restartConfirmation)restartConfirmation=true;
   else Do(MenuText.Get("放弃本轮并重开"),()=>{if(!SceneReady()||runTools.Pending||rescue.Pending)throw new InvalidOperationException(MenuText.Get("先完成当前请求或恢复操作"));restartConfirmation=false;showRestartStatus=true;return runRestart.Start(seed,levelText,overrideAwakening,()=>{RestorePlayer();ReleaseForTravel();});});
  }
  if(Button("runRestartCancel",466,454,428,MenuText.Get(runRestart.Busy?"停止自动重开观察":"取消重开确认"),runRestart.Busy||restartConfirmation,false,16))Do(MenuText.Get("停止自动重开观察"),()=>{restartConfirmation=false;return runRestart.Busy?runRestart.Cancel():MenuText.Get("重开确认已取消；未放弃本轮");});
  Text(28,501,866,49,MenuText.Get("确认后按原版放弃本轮、返回据点并自动开局。取消观察不撤销已发生的放弃；本轮配装与资源按原版重置。"),15,false,true);
  var restartStatus=(runRestart.Busy||showRestartStatus)&&!string.IsNullOrEmpty(runRestart.Status);
  Text(28,559,866,46,restartStatus?runRestart.Status:runTools.Status,15,restartStatus?runRestart.Error:runTools.Error,true);
 }
 [HideFromIl2Cpp]
 private void DrawSceneDisplay()
 {
  Text(28,231,866,56,MenuText.Get("读取当前已生成的碰撞体、攻击与受击形状；关闭菜单后显示。地图揭示仅作用于当前地图。"),17,false,true);
  if(Button("revealMap",28,310,415,MenuText.Get("揭示当前地图"),SceneReady()))Do(MenuText.Get("揭示当前地图"),scenes.RevealMap);
  if(Button("sceneMarkers",465,310,429,MenuText.Get("出口 / 传送 / 特殊标记"),SceneReady(),scenes.Markers,16))Do(MenuText.Get("场景显示"),()=>scenes.ToggleOverlay(0));
  if(Button("sceneColliders",28,365,415,MenuText.Get("实体与地形碰撞框"),SceneReady(),scenes.Colliders))Do(MenuText.Get("场景显示"),()=>scenes.ToggleOverlay(1));
  if(Button("sceneAttacks",465,365,429,MenuText.Get("当前原生攻击判定框"),SceneReady(),scenes.Attacks))Do(MenuText.Get("场景显示"),()=>scenes.ToggleOverlay(2));
  if(Button("sceneReceivers",28,420,866,MenuText.Get("原生受击碰撞体（盒 / 球 / 胶囊）"),SceneReady(),scenes.Receivers,17))Do(MenuText.Get("场景显示"),()=>scenes.ToggleOverlay(3));
  Text(28,465,866,35,MenuText.Get("受击：蓝角色 / 红其他 · 形状不代表当前一定能受伤"),16,false,true);
  Text(28,505,866,46,MenuText.Get("受击扫描半径30米，最多200个形状；网格/未知形状跳过并计数。无敌、队伍、条件与直接伤害无几何预测。"),15,false,true);SceneStatus();
 }
 [HideFromIl2Cpp]
 private void DrawSceneEnemies()
 {
  if(Button("enemyNoTarget",28,240,866,MenuText.Get("普通怪物不锁定玩家"),SceneReady()&&SceneActions.TargetAvailable,scenes.NoTarget))Do(MenuText.Get("普通怪物不锁定玩家"),()=>scenes.SetTargets(!scenes.NoTarget));
  Text(28,290,866,58,MenuText.Get("只过滤本房间普通怪物对玩家的目标；不关闭 AI，不取消已经发出的攻击，Boss 专用目标逻辑不在范围内。"),17,false,true);
  if(Button("enemyNoSpawn",28,365,866,MenuText.Get("冻结房间竞技场 / 已接入召唤"),SceneReady()&&SceneActions.SpawnAvailable,scenes.NoArenaSpawn))Do(MenuText.Get("冻结刷怪"),()=>scenes.SetSpawn(!scenes.NoArenaSpawn));
  Text(28,415,866,70,MenuText.Get("竞技场队列与结算一起暂停，关闭后继续。另拦截鼹鼠及腐化诺盖的两个召唤入口；取消的单次召唤不补发。其他剧情刷怪器未接入。"),15,false,true);
  if(Button("clearNativeRoom",28,497,866,MenuText.Get("全房间清除普通怪物（原生死亡）"),SceneReady(),false,17))Do(MenuText.Get("清除普通怪物"),()=>{waves.Reset();return scenes.ClearRoom();});SceneStatus();
 }
 [HideFromIl2Cpp]
 private void DrawCameraStep()
 {
  if(Button("enableFreeCam",28,240,415,MenuText.Get("开启原生自由镜头"),SceneReady()&&!sceneCamera.Active&&!photo.HasState))Do(MenuText.Get("自由镜头"),()=>sceneCamera.EnableCamera(ReleaseForTravel));
  if(Button("enableFlight",465,240,429,MenuText.Get("开启原生飞行穿墙"),SceneReady()&&!sceneCamera.Active&&!photo.HasState))Do(MenuText.Get("飞行穿墙"),()=>sceneCamera.EnableFlight(ReleaseForTravel));
  Text(28,286,866,64,MenuText.Get("需要当前场景已加载对应调试组件。飞行使用 WASD；结束时若位置不安全，会先返回开启时的平地。失焦、换房、切关或死亡时恢复。"),16,false,true);
  if(Button("restoreSceneCamera",28,358,866,MenuText.Get("恢复镜头与飞行"),sceneCamera.Active))Do(MenuText.Get("恢复镜头与飞行"),sceneCamera.Restore);
  if(Button("frameObserveEnable",28,411,277,MenuText.Get("开启单帧观察（实验）"),FrameObservationActions.Available&&SceneReady()&&!sceneCamera.Active&&!photo.HasState&&!runTools.Pending&&!rescue.Pending,false,16))Do(MenuText.Get("单帧观察"),()=>{if(!SceneReady())throw new InvalidOperationException(MenuText.Get("先完成当前请求或恢复操作"));RestoreTraining();return frameObservation.Enable(ReleaseForTravel);});
  if(Button("frameObserveStep",320,411,277,MenuText.Get("推进一个渲染帧"),frameObservation.Enabled&&!frameObservation.Pending,false,16))Do(MenuText.Get("单帧观察"),()=>frameObservation.Step(ReleaseForTravel));
  if(Button("frameObserveRestore",613,411,281,MenuText.Get("恢复连续运行"),FrameObservationActive,false,16))Do(MenuText.Get("单帧观察"),frameObservation.Restore);
  Text(28,463,866,46,MenuText.Get("每次放行一个渲染帧并重新暂停；U / F / L 显示实际更新次数。失焦、换房或死亡时退出。"),16,false,true);
  Text(28,514,866,45,frameObservation.Status,16,frameObservation.Error,true);
  Text(28,570,866,34,MenuText.Get("严格同步逐帧仍未接入；每次推进时间随实际帧耗时变化。"),15,false,true);
 }
 [HideFromIl2Cpp]
 private void DrawNextEncounter()
 {
  Text(28,230,866,60,MenuText.Get("只预约当前原生出口支持的关卡；之后使用正常出口进入。若进入其他关卡或开始新局，预约取消。"),17,false,true);
  var next=levels.Next;
  for(int i=0;i<Math.Min(next.Count,4);i++)
  {var target=next[i];if(Button("encounter"+i,28,308+i*49,866,MenuText.Format("下一出口选择：{0}",target.Name),SceneReady()&&RunTools.Available&&!runTools.EncounterPending))Do(MenuText.Get("预约遭遇"),()=>runTools.ArmEncounter(target,next));}
  if(next.Count==0)Text(28,317,866,62,MenuText.Get("当前没有可预约的原生出口候选"),18,false,true);
  if(Button("encounterCancel",28,516,415,MenuText.Get("取消遭遇预约"),runTools.EncounterPending))Do(MenuText.Get("取消遭遇预约"),runTools.CancelEncounter);
  if(Button("rescueJump",465,516,429,MenuText.Get("营救预约设置"))){tab=5;}
  Text(28,557,866,48,runTools.Status,15,runTools.Error,true);
 }
}
