// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MotherBase.EntityFramework;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
using NVector=System.Numerics.Vector3;
using NQuaternion=System.Numerics.Quaternion;
namespace RoguePrince.CheatMenu;
internal enum ReceiverRead { Ineligible, Unsupported, Shape }
internal sealed class ReceiverOverlaySource
{
 private readonly GameSession game;
 internal const int ScanCapacity=512,DisplayCapacity=200;
 private readonly Il2CppReferenceArray<Collider> buffer=new(ScanCapacity);
 private Collider[] items=Array.Empty<Collider>();
 private TrainingScope scope;private IntPtr context;
 internal IReadOnlyList<Collider> Items=>items;
 internal bool Limited {get;private set;}
 internal ReceiverOverlaySource(GameSession game)=>this.game=game;
 internal void Clear(){items=Array.Empty<Collider>();scope=default;context=IntPtr.Zero;Limited=false;}
 private bool Current()=>game.Ready&&Application.isFocused&&scope.Matches(TrainingActions.ReadScope(game))&&context!=IntPtr.Zero&&game.Player?.context?.Pointer==context;
 private Entity Owner(Collider c)
 {
  if(!Current()||c==null||!c.enabled||!c.gameObject.activeInHierarchy||c.gameObject.scene.handle!=scope.Scene)return null;
  // DamageSystem.ProcessHit resolves the first Entity in the collider's parent chain (includeInactive=false).
  var entity=c.GetComponentInParent<Entity>();
  if(entity==null||entity.destroyed||!entity.gameObject.activeInHierarchy||entity.gameObject.scene.handle!=scope.Scene||entity.context?.Pointer!=context||!entity.HasComponentData<HitReceiver>()||!entity.HasComponentData<Damageable>())return null;
  var health=entity.GetComponentData<Damageable>();return health!=null&&health.isAlive&&!health.onDieActionStarted?entity:null;
 }
 internal void Scan()
 {
  Clear();if(!game.Ready||!Application.isFocused)return;
  scope=TrainingActions.ReadScope(game);context=game.Player?.context?.Pointer??IntPtr.Zero;if(!Current()){Clear();return;}
  try
  {
   var count=Physics.OverlapSphereNonAlloc(game.Player.transform.position,30,buffer,~0,QueryTriggerInteraction.Collide);
   if(count<0||count>ScanCapacity)throw new InvalidOperationException("Invalid native overlap result");
   Limited=count==ScanCapacity;var found=new List<Collider>();var seen=new HashSet<IntPtr>();
   for(int i=0;i<count;i++)
   {
    var c=buffer[i];if(Owner(c)==null||!seen.Add(c.Pointer))continue;
    if(found.Count==DisplayCapacity){Limited=true;continue;}found.Add(c);
   }
   if(!Current()){Clear();return;}items=found.ToArray();
  }
  catch{Clear();throw;}
  finally{for(int i=0;i<buffer.Length;i++)buffer[i]=null;}
 }
 private static NVector V(Vector3 v)=>new(v.x,v.y,v.z);
 private static NQuaternion Q(Quaternion q)=>new(q.x,q.y,q.z,q.w);
 internal ReceiverRead Read(Collider collider,out ReceiverShape shape,out bool player)
 {
  shape=default;player=false;var owner=Owner(collider);if(owner==null)return ReceiverRead.Ineligible;
  player=owner.Pointer==game.PlayerPointer;var t=collider.transform;var scale=t.lossyScale;
  var absolute=new Vector3(Math.Abs(scale.x),Math.Abs(scale.y),Math.Abs(scale.z));
  var box=collider.TryCast<BoxCollider>();if(box!=null){shape=new(ReceiverShapeKind.Box,V(t.TransformPoint(box.center)),Q(t.rotation),V(Vector3.Scale(box.size*.5f,absolute)),0,0,0);return ReceiverRead.Shape;}
  var sphere=collider.TryCast<SphereCollider>();if(sphere!=null){shape=new(ReceiverShapeKind.Sphere,V(t.TransformPoint(sphere.center)),NQuaternion.Identity,default,sphere.radius*Math.Max(absolute.x,Math.Max(absolute.y,absolute.z)),0,0);return ReceiverRead.Shape;}
  var capsule=collider.TryCast<CapsuleCollider>();if(capsule!=null)
  {
   var axis=capsule.direction;if(axis<0||axis>2)return ReceiverRead.Unsupported;
   var sizes=new[]{absolute.x,absolute.y,absolute.z};
   shape=new(ReceiverShapeKind.Capsule,V(t.TransformPoint(capsule.center)),Q(t.rotation),default,capsule.radius*Math.Max(sizes[(axis+1)%3],sizes[(axis+2)%3]),capsule.height*sizes[axis],axis);return ReceiverRead.Shape;
  }
  // A mesh's AABB is not its receiving shape. Report it as omitted, rather than drawing an inaccurate hitbox.
  return ReceiverRead.Unsupported;
 }
}
