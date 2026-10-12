// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using MotherBase.EntityFramework;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
using UObject=UnityEngine.Object;
namespace RoguePrince.CheatMenu;

// Native room services backed by this summon and coordinates in the current
// room. Room components stay disabled: native Room.Start must not publish a
// shared variable or take over the camera. The host becomes active after setup
// so native spawned children (Corrupted backwall) can render and collide.
internal sealed class BossSpawnRoom : IDisposable
{
 private readonly GameObject host;
 private readonly Entity player;
 private readonly List<Entity> supportEntities=new();
 private readonly Dictionary<Type,ScriptableObject> variables=new();
 private readonly float left,right,floor,top,z;
 internal BossSpawnRoom(Entity player,LevelRoomData room,Vector3 spawn)
 {
  this.player=player;
  var platform=player.GetComponent<PlatformStateComponent>().currentPlatform;
  left=Math.Max(room.position.x+1,Math.Min(platform.leftEdge.x,platform.rightEdge.x)+1);
  right=Math.Min(room.position.x+room.template.width-1,Math.Max(platform.leftEdge.x,platform.rightEdge.x)-1);
  floor=spawn.y;z=spawn.z;
  top=Math.Min(room.position.y+room.template.height-1,floor+10);
  if(right-left<5||top-floor<3)throw new InvalidOperationException("Current room has insufficient Boss combat space");
  host=new GameObject("RPCheat-BossRoom");host.SetActive(false);host.transform.position=P((left+right)/2,floor);
 }
 private Vector3 P(float x,float y)=>new(x,y,z);
 private Transform Point(string name,float x,float y)
 {var obj=new GameObject("RPCheat-"+name);obj.transform.SetParent(host.transform,false);obj.transform.position=P(x,y);return obj.transform;}
 private GameObject Empty(string name)=>Point(name,(left+right)/2,floor).gameObject;
 private Il2CppSystem.Collections.Generic.List<Transform> GroundPoints()
 {var list=new Il2CppSystem.Collections.Generic.List<Transform>();list.Add(Point("left",left,floor));list.Add(Point("center",(left+right)/2,floor));list.Add(Point("right",right,floor));return list;}
 private T Component<T>()where T:MonoBehaviour
 {var component=host.AddComponent<T>();component.enabled=false;return component;}
 internal ScriptableObject Variable(Type type)
 {
  if(variables.TryGetValue(type,out var old))return old;
  ScriptableObject value=null;
  if(type==typeof(AltanRoomVariable))
  {
   var v=ScriptableObject.CreateInstance<AltanRoomVariable>();value=v;variables.Add(type,v);
   var r=Component<AltanRoom>();r.variable=v;r.walls=Empty("AltanWalls");
   // Native position index 0 is a corner; index 1 is the middle.
   r.bossPositions=new[]{Point("AltanLeft",left,floor),Point("AltanCenter",(left+right)/2,floor),Point("AltanRight",right,floor)};
   r.topLeft=Point("AltanRocksLeft",left,top);r.topRight=Point("AltanRocksRight",right,top);v.CurrentValue=r;
  }
  else if(type==typeof(GoatBossRoomVariable))
  {
   var v=ScriptableObject.CreateInstance<GoatBossRoomVariable>();value=v;variables.Add(type,v);
   var r=Component<GoatBossRoom>();r.variable=v;r.leftCorner=Point("BerudeLeft",left,floor);r.rightCorner=Point("BerudeRight",right,floor);
   r.wallPhase1=Empty("BerudePhase1");r.wallPhase2=Empty("BerudePhase2");r.breakingWall=Empty("BerudeBreakWall");r.localPositionToSet=Vector3.zero;v.CurrentValue=r;
  }
  else if(type==typeof(MoleRoomVariable))
  {
   var v=ScriptableObject.CreateInstance<MoleRoomVariable>();value=v;variables.Add(type,v);
   var r=Component<MoleBossRoom>();r.variable=v;r.invokePositions=GroundPoints();r.tpPositions=GroundPoints();r.deathTransforms=GroundPoints();
   r.leftCorner=Point("KaaliLeft",left,floor);r.rightCorner=Point("KaaliRight",right,floor);r.ceiling=Point("KaaliCeiling",(left+right)/2,top);
   r.xAtkSpawnHeight=Point("KaaliCross",(left+right)/2,Math.Min(top,floor+4));r.leftTraps=Empty("KaaliLeftTraps");r.rightTraps=Empty("KaaliRightTraps");v.CurrentValue=r;
  }
  else if(type==typeof(NogaiRoomVariable))
  {
   var v=ScriptableObject.CreateInstance<NogaiRoomVariable>();value=v;variables.Add(type,v);
   var r=Component<NogaiRoom>();r.roomVariable=v;r.cornerLeft=Point("NogaiLeft",left,floor);r.cornerRight=Point("NogaiRight",right,floor);
   r.topLeft=Point("NogaiTopLeft",left,top);r.topRight=Point("NogaiTopRight",right,top);r.introPosition=Point("NogaiIntro",(left+right)/2,floor);
   r.exitDoor=Empty("NogaiExit");r.phase1Column=Empty("NogaiPhase1");r.phase2Column=Empty("NogaiPhase2");r.doors=Empty("NogaiDoors");r.fightCamera=Empty("NogaiCamera");v.CurrentValue=r;
  }
  else if(type==typeof(CorruptedNogaiRoomVariable))
  {
   var v=ScriptableObject.CreateInstance<CorruptedNogaiRoomVariable>();value=v;variables.Add(type,v);
   var r=Component<CorruptedNogaiRoom>();r.variable=v;r.invokePositions=GroundPoints();
   var positions=new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<CorruptedNogaiRoom.SlamPosition>(3L);
   for(int i=0;i<3;i++)positions[i]=new CorruptedNogaiRoom.SlamPosition{transform=Point("CorruptedSlam"+i,left+(right-left)*i/2,floor),animPhase=(CorruptedNogaiFistSlamState.AnimPhase)i};
   r.slamPositions=positions;r.startLeftFist=Point("CorruptedLeftFist",left,floor+2);r.startRightFist=Point("CorruptedRightFist",right,floor+2);
   r.backgroundLeftFist=Point("CorruptedBackLeft",left,top);r.backgroundRightFist=Point("CorruptedBackRight",right,top);
   r.meteorTopLeft=Point("CorruptedMeteorsLeft",left,top);r.meteorTopRight=Point("CorruptedMeteorsRight",right,top);
   r.spawnLeftCollision=Point("CorruptedLeftCollision",left,floor);r.spawnRightCollision=Point("CorruptedRightCollision",right,floor);r.trailingFire=Empty("CorruptedFire");v.CurrentValue=r;
  }
  else if(type==typeof(BaatarRoomVariable))
  {
   var v=ScriptableObject.CreateInstance<BaatarRoomVariable>();value=v;variables.Add(type,v);
   var r=Component<BaatarRoom>();r.variable=v;r.spawnTransform=Point("BaatarSpawn",(left+right)/2,floor);
   r.roomLeftBorderTransform=Point("BaatarLeft",left,floor);r.roomRightBorderTransform=Point("BaatarRight",right,floor);
   r.cameraTarget=Empty("BaatarCamera");r.shadowShaft=Empty("BaatarShaft");
   r.phase1KeyPoints=BaatarPoints();r.phase2KeyPoints=BaatarPoints();r.phase3KeyPoints=BaatarPoints();
   // The trap prefab is included in the already loaded Baatar bundle. Instantiate
   // it through EntityContext, just as native entity spawning does; never borrow
   // a live arena's trap or create a bare Entity with missing native components.
   var prefab=Resources.FindObjectsOfTypeAll<MotherBase.ActionFramework.Actor>().FirstOrDefault(a=>
    a.actorConfig?.name=="RisingTrapActorConfig"&&!a.gameObject.scene.IsValid())?.GetComponent<Entity>();
   if(prefab==null)throw new InvalidOperationException("Baatar native rising-trap prefab was not loaded");
   var traps=player.context.InstantiateEntity(prefab,true,false);supportEntities.Add(traps);
   traps.transform.position=P((left+right)/2,floor);
   if(traps.context?.Pointer!=player.context.Pointer)throw new InvalidOperationException("Baatar trap context mismatch");
   r.risingTraps=traps;v.CurrentValue=r;
  }
  return value;
 }
 private BaatarRoom.ShahinPhaseKeyPoints BaatarPoints()
 {
  var p=new BaatarRoom.ShahinPhaseKeyPoints();
  p.cameraPhaseTargetTransform=Point("BaatarView",(left+right)/2,floor+2);
  p.phaseCeiling=Empty("BaatarCeiling");p.slamTransform=Point("BaatarGround",(left+right)/2,floor);
  p.airTransform=Point("BaatarAir",(left+right)/2,Math.Min(top-1,floor+5));p.slamCastLeftTransform=Point("BaatarSlamLeft",left,floor);
  p.backgroundTransform=Point("BaatarBackground",(left+right)/2,floor);
  p.prepareSDGRenderer=HiddenRenderer("BaatarPrepare");p.castSDGRenderer=HiddenRenderer("BaatarCast");
  p.homingKeyPoints=new Il2CppSystem.Collections.Generic.List<BaatarRoom.ShahinPhaseKeyPoints.HomingKeyPoint>();
  for(int i=0;i<3;i++)p.homingKeyPoints.Add(new BaatarRoom.ShahinPhaseKeyPoints.HomingKeyPoint{
   shootingTransform=Point("BaatarHoming"+i,left+(right-left)*i/2,Math.Min(top-1,floor+4)),projectileTargetPositions=GroundPoints()});
  return p;
 }
 private MeshRenderer HiddenRenderer(string name)
 {
  var obj=GameObject.CreatePrimitive(PrimitiveType.Quad);obj.SetActive(false);obj.name="RPCheat-"+name;obj.transform.SetParent(host.transform,false);
  // Native sweep preparation reads a renderer/material; the private inactive
  // quad supplies that service without rendering an arena over the current room.
  return obj.GetComponent<MeshRenderer>();
 }
 internal NogaiRoom Nogai=>((NogaiRoomVariable)Variable(typeof(NogaiRoomVariable))).CurrentValue;
 internal void AdaptWalls(BackWallConfigurationComponent component,BossSpawnAssetGraph graph)
 {
  var all=new List<BackWallConfiguration>();
  foreach(var group in new[]{component.groundConfigurations,component.configurations})
   if(group!=null)for(int i=0;i<group.Length;i++)if(group[i]!=null)all.Add(group[i]);
  var elements=new List<BackWallElement>();
  foreach(var config in all)if(config.elements!=null)for(int i=0;i<config.elements.Count;i++)elements.Add(config.elements[i]);
  if(elements.Count==0)throw new InvalidOperationException("Boss native backwall configuration has no elements");
  var minX=elements.Min(e=>e.position.x);var maxX=elements.Max(e=>e.position.x);
  var minY=elements.Min(e=>e.position.y);var maxY=elements.Max(e=>e.position.y);
  var mapped=new HashSet<IntPtr>();
  Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<BackWallConfiguration> Copy(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<BackWallConfiguration> source)
  {
   if(source==null)return null;
   var result=new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<BackWallConfiguration>(source.Length);
   for(int i=0;i<source.Length;i++)
   {
    var copy=source[i]==null?null:graph.Clone(source[i]);result[i]=copy;
    if(copy==null||!mapped.Add(copy.Pointer))continue;
    for(int j=0;j<copy.elements.Count;j++)
    {
     var element=copy.elements[j];var p=element.position;
     element.position=new Vector2(BossSpawnLayout.Map(p.x,minX,maxX,-(right-left)/2+1,(right-left)/2-1),
      BossSpawnLayout.Map(p.y,minY,maxY,1,top-floor-1));copy.elements[j]=element;
    }
   }
   return result;
  }
  component.groundConfigurations=Copy(component.groundConfigurations);component.configurations=Copy(component.configurations);
 }
 internal void ActivateSupports()
 {
  foreach(var component in host.GetComponents<MonoBehaviour>())if(component.enabled)
   throw new InvalidOperationException("Boss room publisher unexpectedly enabled");
  host.SetActive(true);
 }
 public void Dispose(){foreach(var e in supportEntities)if(e!=null&&!e.destroyed)UObject.Destroy(e.gameObject);supportEntities.Clear();if(host!=null)UObject.Destroy(host);foreach(var value in variables.Values)if(value!=null)UObject.Destroy(value);variables.Clear();}
}
