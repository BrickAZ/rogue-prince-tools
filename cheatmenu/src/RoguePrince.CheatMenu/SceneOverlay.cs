// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using MotherBase.ActionFramework;
using MotherBase.EntityFramework;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
namespace RoguePrince.CheatMenu;
internal sealed class SceneOverlay
{
 private readonly GameSession game;private readonly SceneActions scenes;
 private float scanAt;private Collider[] colliders=Array.Empty<Collider>();private DamageCaster[] casters=Array.Empty<DamageCaster>();private MinimapMarker[] markers=Array.Empty<MinimapMarker>();
 private int lines;private Camera camera;
 private readonly ReceiverOverlaySource receivers;
 internal SceneOverlay(GameSession game,SceneActions scenes){this.game=game;this.scenes=scenes;receivers=new(game);}
 internal void Tick()
 {
  if(!scenes.Receivers||!game.Ready||!Application.isFocused)receivers.Clear();
  if(!game.Ready||(!scenes.Colliders&&!scenes.Attacks&&!scenes.Markers&&!scenes.Receivers)){colliders=Array.Empty<Collider>();casters=Array.Empty<DamageCaster>();markers=Array.Empty<MinimapMarker>();return;}
  if(Time.realtimeSinceStartup<scanAt)return;scanAt=Time.realtimeSinceStartup+.25f;
  var scene=game.Player.gameObject.scene.handle;
  if(scenes.Receivers)try{receivers.Scan();}catch(Exception e){scenes.DisableReceivers(e);}
  colliders=scenes.Colliders?Physics.OverlapSphere(game.Player.transform.position,30,~0,QueryTriggerInteraction.Ignore).Where(c=>c!=null&&c.gameObject.scene.handle==scene).Take(200).ToArray():Array.Empty<Collider>();
  casters=scenes.Attacks?UnityEngine.Object.FindObjectsOfType<DamageCaster>().Where(c=>c.gameObject.scene.handle==scene).Take(150).ToArray():Array.Empty<DamageCaster>();
  markers=scenes.Markers?UnityEngine.Object.FindObjectsOfType<MinimapMarker>().Where(m=>m.gameObject.scene.handle==scene&&m.gameObject.activeInHierarchy).Take(150).ToArray():Array.Empty<MinimapMarker>();
 }
 internal void Draw(GUIStyle style,bool photoMode)
 {
  if(!SceneOverlayPolicy.CanDraw(scenes.Colliders||scenes.Attacks||scenes.Markers||scenes.Receivers,photoMode,Application.isFocused,game.Ready,Event.current?.type==EventType.Repaint)||!TrainingActions.ReadScope(game).Valid)return;
  camera=Camera.main;if(camera==null)return;lines=0;var color=GUI.color;var matrix=GUI.matrix;
  try
  {
   var receiverCount=0;var unsupported=0;var receiverLineLimit=false;
   if(scenes.Receivers)try
   {
    foreach(var collider in receivers.Items)
    {
     var state=receivers.Read(collider,out var shape,out var player);if(state==ReceiverRead.Ineligible)continue;
     if(state==ReceiverRead.Unsupported){unsupported++;continue;}var segments=ReceiverWireframe.Build(shape);
     if(segments.Count==0){unsupported++;continue;}if(lines+segments.Count>2500){receiverLineLimit=true;continue;}
     var tint=player?new Color(.4f,.65f,1):new Color(1,.1f,.1f);
     foreach(var segment in segments)Line(V(segment.First),V(segment.Second),tint);receiverCount++;
    }
   }
   catch(Exception e){receivers.Clear();scenes.DisableReceivers(e);}
   foreach(var collider in colliders)
   {if(collider==null||!collider.enabled||!collider.gameObject.activeInHierarchy)continue;var entity=collider.GetComponentInParent<Entity>();var tint=entity?.Pointer==game.PlayerPointer?Color.cyan:entity?.GetComponent<MobComponent>()!=null?new Color(1,.4f,.2f):Color.yellow;ColliderShape(collider,tint);}
   foreach(var caster in casters)
   {
    if(caster==null||!caster.gameObject.activeInHierarchy||caster.colliderStates==null)continue;
    var entity=caster.GetComponentInParent<Entity>();if(entity==null||entity.context?.Pointer!=game.Player.context?.Pointer)continue;
    var tint=entity.Pointer==game.PlayerPointer?Color.green:new Color(1,.2f,.5f);
    foreach(var state in caster.colliderStates){var s=state.settings;Shape(s.position,s.rotation,s.shape,tint);}
   }
   style.fontSize=13;style.wordWrap=false;style.normal.textColor=Color.white;
   foreach(var marker in markers)
   {
    if(marker==null||!marker.gameObject.activeInHierarchy)continue;var point=marker.useCustomTransform&&marker.customTransform!=null?marker.customTransform.position:marker.transform.position;
    var pixel=camera.WorldToScreenPoint(point);if(pixel.z<=0||pixel.x<-150||pixel.x>Screen.width+150||pixel.y<-50||pixel.y>Screen.height+50)continue;
    string label=marker.searchTransitionName?MenuText.Get("出口 / 路线"):marker.GetComponentInParent<LevelTeleportComponent>()!=null?MenuText.Get("传送点"):MenuText.Get("地图标记");
    GUI.color=Color.white;GUI.Label(new Rect(pixel.x-80,Screen.height-pixel.y-24,220,40),label+" · "+marker.gameObject.name,style);
   }
   GUI.color=Color.white;GUI.Label(new Rect(12,Screen.height-38,Math.Min(1100,Screen.width-24),30),MenuText.Get("碰撞：青角色 / 橙怪物 / 黄地形 · 攻击：绿角色 / 粉其他 · 网格仅包围盒"),style);
   if(scenes.Receivers){style.wordWrap=true;GUI.Label(new Rect(12,Screen.height-100,Math.Min(1100,Screen.width-24),60),MenuText.Get("受击：蓝角色 / 红其他 · 形状不代表当前一定能受伤")+"\n"+MenuText.Format("受击形状 {0} · 跳过网格/未知 {1} · 扫描上限 {2} · 线条上限 {3}",receiverCount,unsupported,receivers.Limited?MenuText.Get("已达"):MenuText.Get("未达"),receiverLineLimit?MenuText.Get("已达"):MenuText.Get("未达")),style);}
  }
  finally{GUI.color=color;GUI.matrix=matrix;}
 }
 private static Vector3 V(System.Numerics.Vector3 v)=>new(v.X,v.Y,v.Z);
 private void Line(Vector3 first,Vector3 second,Color tint)
 {
  if(lines>=2500)return;var a=camera.WorldToScreenPoint(first);var b=camera.WorldToScreenPoint(second);if(a.z<=0||b.z<=0||!float.IsFinite(a.x)||!float.IsFinite(a.y)||!float.IsFinite(b.x)||!float.IsFinite(b.y))return;
  var from=new Vector2(a.x,Screen.height-a.y);var to=new Vector2(b.x,Screen.height-b.y);var delta=to-from;var length=delta.magnitude;
  if(length<.1f||length>Screen.width*4)return;lines++;var saved=GUI.matrix;GUI.color=tint;
  try{GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,from);GUI.DrawTexture(new Rect(from.x,from.y,length,1.5f),Texture2D.whiteTexture);}
  finally{GUI.matrix=saved;}
 }
 private void Circle(Vector3 center,float radius,Quaternion rotation,Color color)
 {if(!float.IsFinite(radius)||radius<=0||radius>100)return;var previous=center+rotation*Vector3.right*radius;for(int i=1;i<=24;i++){var angle=i*Mathf.PI*2/24;var next=center+rotation*new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0);Line(previous,next,color);previous=next;}}
 private void Box(Vector3 center,Vector3 extents,Quaternion rotation,Color tint)
 {var v=new Vector3[8];for(int i=0;i<8;i++)v[i]=center+rotation*new Vector3((i&1)==0?-extents.x:extents.x,(i&2)==0?-extents.y:extents.y,(i&4)==0?-extents.z:extents.z);for(int i=0;i<8;i++)for(int bit=1;bit<=4;bit*=2)if((i&bit)==0)Line(v[i],v[i|bit],tint);}
 private void Capsule(Vector3 center,float radius,float distance,Quaternion rotation,Color tint)
 {var up=rotation*Vector3.up*Math.Max(0,distance)*.5f;Circle(center+up,radius,rotation,tint);Circle(center-up,radius,rotation,tint);var right=rotation*Vector3.right*radius;Line(center+up+right,center-up+right,tint);Line(center+up-right,center-up-right,tint);}
 private void Shape(Vector3 position,Quaternion rotation,ShapeData shape,Color tint)
 {
  var center=position+rotation*shape.positionOffset;var rot=rotation*Quaternion.Euler(shape.rotationOffset);
  switch(shape.shapeType.ToString()) {case "Sphere":Circle(center,shape.sphereRadius,rot,tint);break;case "Capsule":Capsule(center,shape.capsuleRadius,shape.capsuleSpheresDistance,rot,tint);break;case "Box":Box(center,shape.boxExtents,rot,tint);break;}
 }
 private void ColliderShape(Collider collider,Color tint)
 {
  var transform=collider.transform;var scale=transform.lossyScale;
  var box=collider.TryCast<BoxCollider>();if(box!=null){Box(transform.TransformPoint(box.center),Vector3.Scale(box.size*.5f,new(Math.Abs(scale.x),Math.Abs(scale.y),Math.Abs(scale.z))),transform.rotation,tint);return;}
  var sphere=collider.TryCast<SphereCollider>();if(sphere!=null){Circle(transform.TransformPoint(sphere.center),sphere.radius*Math.Max(Math.Abs(scale.x),Math.Max(Math.Abs(scale.y),Math.Abs(scale.z))),Quaternion.identity,tint);return;}
  var capsule=collider.TryCast<CapsuleCollider>();if(capsule!=null)
  {
   var sizes=new[]{Math.Abs(scale.x),Math.Abs(scale.y),Math.Abs(scale.z)};var axis=capsule.direction;var height=capsule.height*sizes[axis];var radius=capsule.radius*Math.Max(sizes[(axis+1)%3],sizes[(axis+2)%3]);
   var rotation=transform.rotation*(axis==0?Quaternion.Euler(0,0,90):axis==2?Quaternion.Euler(90,0,0):Quaternion.identity);Capsule(transform.TransformPoint(capsule.center),radius,Math.Max(0,height-2*radius),rotation,tint);return;
  }
  // Native MeshCollider data may be non-readable. Its actual world AABB is explicitly labelled in the UI.
  Box(collider.bounds.center,collider.bounds.extents,Quaternion.identity,new Color(.65f,.65f,.65f));
 }
}
