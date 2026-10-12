// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using Cinemachine;
using MotherBase.EntityFramework;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UObject=UnityEngine.Object;
namespace RoguePrince.CheatMenu;
internal sealed class SceneCamera
{
 private readonly GameSession game;
 private CamerasDebug camera;
 private CinemachineVirtualCamera virtualCamera;
 private FreeCamControls controls;
 private InputActionMap playerMap,cameraMap;
 private bool oldCamera,oldControls,oldPlayerMap,oldCameraMap;
 private CameraTeleportDebug flight;
 private Entity flightPlayer;
 private CinemachineBrain flightBrain;
 private bool oldBrain;
 private bool oldGod,oldIntangible;
 private Vector3 flightOrigin;
 private LevelRoomData flightRoom;
 private TrainingScope scope;
 private bool cameraOwned,flightOwned;
 internal bool FreeCamera=>cameraOwned;
 internal bool Flight=>flightOwned;
 internal bool Active=>FreeCamera||Flight;
 internal SceneCamera(GameSession game)=>this.game=game;
 private TrainingScope Require(){game.RequireReady();var s=TrainingActions.ReadScope(game);if(!s.Valid||!Application.isFocused)throw new InvalidOperationException(MenuText.Get("训练操作需要当前角色、房间与焦点就绪"));return s;}
 internal string EnableCamera(Action release)
 {
  if(Active)throw new InvalidOperationException(MenuText.Get("请先恢复镜头与飞行模式"));var current=Require();
  var found=UObject.FindObjectsOfType<CamerasDebug>().Where(x=>x.enabled&&x.player?.CurrentValue?.Pointer==game.PlayerPointer&&x.FreeCamComponent!=null&&x.controls!=null&&x.inputRef!=null).ToArray();
  if(found.Length!=1)throw new InvalidOperationException(MenuText.Get("当前场景缺少唯一的原生调试组件"));
  var debug=found[0];var inputPlayer=debug.inputRef.FindActionMap(debug.playerInputs,false);var inputCamera=debug.inputRef.FindActionMap(debug.cameraInputs,false);
  if(inputPlayer==null||inputCamera==null||inputPlayer.Pointer==inputCamera.Pointer||debug.FreeCamComponent.enabled)throw new InvalidOperationException(MenuText.Get("镜头或输入状态不支持接管"));
  release();if(Time.timeScale<=0)throw new InvalidOperationException(MenuText.Get("游戏仍被其他界面暂停，请先关闭原生暂停菜单"));
  scope=current;camera=debug;virtualCamera=debug.FreeCamComponent;controls=debug.controls;playerMap=inputPlayer;cameraMap=inputCamera;
  oldCamera=virtualCamera.enabled;oldControls=controls.enabled;oldPlayerMap=playerMap.enabled;oldCameraMap=cameraMap.enabled;
  cameraOwned=true;
  try{debug.SetFreeCamState(true);if(!virtualCamera.enabled||!controls.enabled)throw new InvalidOperationException(MenuText.Get("镜头状态回读失败"));}
  catch{Restore();throw;}
  return MenuText.Get("自由镜头已开启；使用原生镜头输入绑定");
 }
 internal string EnableFlight(Action release)
 {
  if(Active)throw new InvalidOperationException(MenuText.Get("请先恢复镜头与飞行模式"));var current=Require();
  var found=UObject.FindObjectsOfType<CameraTeleportDebug>().Where(x=>x.enabled&&x.player?.CurrentValue?.Pointer==game.PlayerPointer&&x.cinemachineCamera?.CurrentValue!=null&&!x._teleportActive).ToArray();
  if(found.Length!=1)throw new InvalidOperationException(MenuText.Get("当前场景缺少唯一的原生调试组件"));
  var room=game.Player.GetComponent<RoomStateComponent>()?.currentRoom;var origin=game.Player.transform.position;TrainingActions.ValidatePosition(game,room,origin);
  release();if(Time.timeScale<=0)throw new InvalidOperationException(MenuText.Get("游戏仍被其他界面暂停，请先关闭原生暂停菜单"));
  var capturedPlayer=game.Player;var capturedBrain=found[0].cinemachineCamera.CurrentValue;var capturedBrainEnabled=capturedBrain.enabled;
  var capturedGod=capturedPlayer.HasComponentData<GodModeFlag>();var capturedIntangible=capturedPlayer.HasComponentData<IntangibleFlag>();
  scope=current;flight=found[0];flightPlayer=capturedPlayer;flightBrain=capturedBrain;oldBrain=capturedBrainEnabled;flightRoom=room;flightOrigin=origin;
  oldGod=capturedGod;oldIntangible=capturedIntangible;flightOwned=true;
  try{flight.ActivateTeleportMode();if(!flight._teleportActive)throw new InvalidOperationException(MenuText.Get("飞行状态回读失败"));}
  catch{Restore();throw;}
  return MenuText.Get("飞行穿墙已开启；WASD 移动，重新打开菜单可退出");
 }
 internal void Tick(bool menuVisible)
 {
  if(!Active)return;var current=TrainingActions.ReadScope(game);
  if(!scope.Matches(current)||!Application.isFocused){Restore();return;}
  if(FreeCamera&&virtualCamera==null){Restore();return;}
  if(FreeCamera&&!menuVisible){if(playerMap!=null&&playerMap.enabled)playerMap.Disable();if(cameraMap!=null&&!cameraMap.enabled)cameraMap.Enable();}
  if(Flight)
  {
   if(flight==null){Restore();return;}
   var keyboard=Keyboard.current;var input=Vector2.zero;
   if(!menuVisible&&Time.timeScale>0&&keyboard!=null)input=new((keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0),(keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0));
   flight._moveInput2D=input.normalized;
  }
 }
 internal string Restore()
 {
  var errors=new List<Exception>();
  if(cameraOwned)
  {
   try
   {
    // Restore captured concrete objects, never a replacement referenced by a reloaded debug component.
    if(virtualCamera!=null)virtualCamera.enabled=oldCamera;if(controls!=null){controls._moveInputValue=Vector3.zero;controls._aimInputValue=Vector3.zero;controls.enabled=oldControls;}
    if(playerMap!=null){if(oldPlayerMap)playerMap.Enable();else playerMap.Disable();}
    if(cameraMap!=null){if(oldCameraMap)cameraMap.Enable();else cameraMap.Disable();}
    if((virtualCamera!=null&&virtualCamera.enabled!=oldCamera)||(controls!=null&&controls.enabled!=oldControls)||(playerMap!=null&&playerMap.enabled!=oldPlayerMap)||(cameraMap!=null&&cameraMap.enabled!=oldCameraMap))throw new InvalidOperationException(MenuText.Get("镜头状态回读失败"));
    camera=null;virtualCamera=null;controls=null;playerMap=cameraMap=null;cameraOwned=false;
   }catch(Exception e){errors.Add(e);}
  }
  if(flightOwned)
  {
   try
   {
    if(flight!=null)flight._moveInput2D=Vector2.zero;
    if(flightPlayer!=null&&!flightPlayer.destroyed)
    {
     if(flightPlayer.gameObject.activeInHierarchy&&flightPlayer.Pointer==game.PlayerPointer && flightPlayer.gameObject.scene.handle==scope.Scene)
     {
      var map=SceneActions.Map(SceneActions.Flow());var current=flightPlayer.transform.position;bool safe=false;
      try{if(map!=null&&map.FindRoomAtCoord(current,out var room)){TrainingActions.ValidatePosition(game,room,current);safe=true;}}catch(InvalidOperationException){}
      if(!safe){TrainingActions.ValidatePosition(game,flightRoom,flightOrigin);TeleportExtensions.Teleport(flightPlayer,flightOrigin,true);if(Vector3.Distance(flightPlayer.transform.position,flightOrigin)>.35f)throw new InvalidOperationException(MenuText.Get("书签原位置恢复未通过回读"));}
     }
     // The native helper adds/removes movement flags. Isolate its bindings if shared variables now name another player/brain.
     // A pre-existing flag removed by another mode stays removed; this lease only removes flags it added.
     var expectedGod=oldGod&&flightPlayer.HasComponentData<GodModeFlag>();var expectedIntangible=oldIntangible&&flightPlayer.HasComponentData<IntangibleFlag>();
     var helper=flight;GameObject recoveryObject=null;EntityVariable ownPlayer=null;CinemachineBrainVariable ownBrain=null;EntityVariable originalPlayer=null;CinemachineBrainVariable originalBrain=null;
     try
     {
      if(helper==null||flightBrain==null){recoveryObject=new GameObject("RPCheat FlightRecovery");recoveryObject.SetActive(false);}
      if(helper==null){helper=recoveryObject.AddComponent<CameraTeleportDebug>();helper.enabled=false;}
      originalPlayer=helper.player;originalBrain=helper.cinemachineCamera;
      ownPlayer=ScriptableObject.CreateInstance<EntityVariable>();ownPlayer.CurrentValue=flightPlayer;
      ownBrain=ScriptableObject.CreateInstance<CinemachineBrainVariable>();ownBrain.CurrentValue=flightBrain!=null?flightBrain:recoveryObject.AddComponent<CinemachineBrain>();
      helper._wasInGodMode=oldGod;helper._wasIntangible=oldIntangible;
      helper.player=ownPlayer;helper.cinemachineCamera=ownBrain;helper.DeactivateTeleportMode();
      if(flightPlayer.HasComponentData<GodModeFlag>()!=expectedGod||flightPlayer.HasComponentData<IntangibleFlag>()!=expectedIntangible)throw new InvalidOperationException(MenuText.Get("飞行状态回读失败"));
     }
     finally{if(helper!=null){helper.player=originalPlayer;helper.cinemachineCamera=originalBrain;}if(ownPlayer!=null)UObject.Destroy(ownPlayer);if(ownBrain!=null)UObject.Destroy(ownBrain);if(recoveryObject!=null)UObject.Destroy(recoveryObject);}
    }
    else if(flight!=null)flight._teleportActive=false;
    if(flightBrain!=null){flightBrain.enabled=oldBrain;if(flightBrain.enabled!=oldBrain)throw new InvalidOperationException(MenuText.Get("镜头状态回读失败"));}
    if(flight!=null&&flight._teleportActive)throw new InvalidOperationException(MenuText.Get("飞行状态回读失败"));
    flight=null;flightPlayer=null;flightBrain=null;flightRoom=null;flightOwned=false;
   }catch(Exception e){errors.Add(e);}
  }
  if(errors.Count>0)throw new AggregateException(MenuText.Get("镜头或飞行恢复未完成，请重试"),errors);
  return MenuText.Get("镜头与飞行已恢复");
 }
}
