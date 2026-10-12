// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using MotherBase.ActionFramework;
using MotherBase.EntityFramework;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
using UObject=UnityEngine.Object;
namespace RoguePrince.CheatMenu;

internal sealed class BossSpawnCombat : IBossSpawnNative,IDisposable
{
 private readonly Entity entity,player;
 private readonly LevelRoomData levelRoom;
 private readonly MobSpawnHealthMode mode;
 private readonly BossSpawnBootstrap bootstrap;
 private BossSpawnRoom room;
 private BossSpawnAssetGraph graph;
 private float expectedHealth,expectedMaximum;
 private BossComponentBase component;
 private Actor actor;
 private BSAIInput input;
 private string kind;
 private readonly List<(EntityVariable Source,EntityVariable Private)> entityVariables=new();
 private EntityVariable bossEntityVariable;
 internal bool AiReleased=>bootstrap.AiReleased;
 internal bool AttackObserved=>bootstrap.AttackObserved;
 internal BossSpawnCombat(Entity entity,Entity player,LevelRoomData levelRoom,MobSpawnHealthMode mode)
 {
  this.entity=entity;this.player=player;this.levelRoom=levelRoom;this.mode=mode;bootstrap=new(this);
 }
 // CaptureInstance calls this in the same native MoveNext that returns the new
 // instance, before Unity can dispatch Start on its EntityVariableSetter.
 internal void IsolatePublishers()
 {
  bossEntityVariable=ScriptableObject.CreateInstance<EntityVariable>();bossEntityVariable.CurrentValue=entity;
  foreach(var effect in entity.GetComponentsInChildren<SituationTriggerEffectComponent>(true))
  {
   effect.enabled=false;
   // OnDisable only detaches load delegates; OnBeginLoad removes the actual
   // situation reaction. Before Start, entity is null and no reaction exists.
   if(effect.entity!=null)effect.OnBeginLoad();
   if(effect.enabled)throw new InvalidOperationException("Boss encounter listener did not detach");
  }
  foreach(var setter in entity.GetComponentsInChildren<EntityVariableSetter>(true))
  {
   var source=setter.variable;if(source==null)continue;
   var copy=bossEntityVariable;entityVariables.Add((source,copy));
   copy.CurrentValue=entity;setter.variable=copy;
   if(setter.variable.Pointer!=copy.Pointer||copy.CurrentValue?.Pointer!=entity.Pointer)
    throw new InvalidOperationException("Boss entity publisher isolation failed");
  }
 }
 internal bool Tick(bool aiEnabled)=>bootstrap.Tick(Time.frameCount,entity.context!=null&&entity.GetComponent<Actor>()!=null,aiEnabled);
 public void Prepare()
 {
  actor=entity.GetComponent<Actor>();input=entity.GetComponent<BSAIInput>();
  component=entity.GetComponent<BossComponentBase>();
  if(actor==null||input?.brain==null||component==null)throw new InvalidOperationException("Boss native components are incomplete");
  room=new BossSpawnRoom(player,levelRoom,entity.transform.position);
  graph=new BossSpawnAssetGraph(room.Variable,bossEntityVariable);
  foreach(var v in entityVariables)graph.Bind(v.Source,v.Private);
  input.brain=graph.Clone(input.brain);
  if(actor.actorConfig!=null)actor.actorConfig=graph.Clone(actor.actorConfig);
  var bindings=entity.GetComponent<ActionBindingsReference>();
  if(bindings?.bindings!=null)bindings.bindings=graph.Clone(bindings.bindings);
  var walls=entity.GetComponent<BackWallConfigurationComponent>();if(walls!=null)room.AdaptWalls(walls,graph);
  var health=entity.GetComponent<Damageable>();
  if(health?.onDieAction==null)throw new InvalidOperationException("Boss native death action is missing");
  // Formal Boss death actions resolve cutscenes, achievements and run endings.
  // This instance uses the same death layer/animation, with those encounter
  // callbacks removed. MobSpawnActions retires it when native HP reaches zero.
  var death=graph.Clone(health.onDieAction.GetActionData(entity));
  death.actionFeatures=new Il2CppSystem.Collections.Generic.List<ActionFeature>();
  death.actionFeatureMap=new Il2CppSystem.Collections.Generic.List<long>();death.endFrame=1;
  health.onDieAction=death;health.overrideDieAction=false;health.overridenDieActionData=null;
  health.onDieEffect=null;health.onDieEvent=null;health.skipDeathTriggers=true;
  health.showWorldSpaceLifeBar=true;
  room.ActivateSupports();
  Plugin.LogEvent($"boss_spawn_services entity={entity.GetInstanceID()} assets={graph.Count} room=({levelRoom.position.x},{levelRoom.position.y})");
 }
 public void Initialize()
 {
  if(component.brainInitDone)throw new InvalidOperationException("Boss brain ran before summon initialization ownership");
  var brain=input.brain;
  if(brain.TryCast<AltanBossBrain>() is {} a){kind="Altan";a.Init(entity);}
  else if(brain.TryCast<BaatarBossBrain>() is {} b){kind="Baatar";b.Init(entity);}
  else if(brain.TryCast<GoatBossBrain>() is {} g){kind="Berude";g.Init(entity);}
  else if(brain.TryCast<CorruptedNogaiBrain>() is {} c){kind="CorruptedNogai";c.Init(entity);}
  else if(brain.TryCast<KaaliBossBrain>() is {} k){kind="Kaali";k.Init(entity);}
  else if(brain.TryCast<NogaiBossBrain>() is {} n){kind="Nogai";n.Init(entity);entity.GetComponent<NogaiBossComponent>().room=room.Nogai;}
  else throw new InvalidOperationException("Unknown Boss brain");
  if(!component.brainInitDone)throw new InvalidOperationException("Native Boss initialization did not complete");
  Plugin.LogEvent($"boss_spawn_initialized entity={entity.GetInstanceID()} kind={kind} phase={component.currentPhaseIndex}");
 }
 public void StopWaiting()
 {
  // Stop through Actor so lock-velocity/rotation/other native feature cleanup runs.
  var waits=new List<ActionInstance>();
  for(int i=0;i<actor.activeActions.Count;i++)
  {
   var action=actor.activeActions[i];var name=action.data?.name??"";
   if(name.Contains("WaitForStart",StringComparison.Ordinal))waits.Add(action);
  }
  foreach(var wait in waits)actor.StopAction(entity,wait,null);
  // Mole/Nogai intentionally retain DodgeFlag when their waiting action stops;
  // their original post-intro replaces it. The summon skips that cinematic.
  if(entity.HasComponentData<DodgeFlag>())entity.RemoveComponentData<DodgeFlag>();
  for(int i=0;i<actor.activeActions.Count;i++)if(actor.activeActions[i].data?.name?.Contains("WaitForStart",StringComparison.Ordinal)==true)
   throw new InvalidOperationException("Boss native waiting action remained active");
 }
 private void IntroEnd<T>()where T:ActionFeature
 {
  var feature=ScriptableObject.CreateInstance<T>();
  try{feature.OnStop(entity,default,null);}finally{UObject.Destroy(feature);}
 }
 public void EnterCombat()
 {
  // These native callbacks were checked in the current GameAssembly. They finish
  // phase-zero/intro-only entity state, without cinematics, UI streams or progress
  // variable writes from the full intro action.
  switch(kind)
  {
   case "Altan":IntroEnd<AltanIntroFeature>();break;
   case "Baatar":IntroEnd<BaatarIntroFeature>();break;
   case "CorruptedNogai":IntroEnd<CorruptedNogaiIntroFeature>();break;
   case "Berude":IntroEnd<GoatmanderIntroFeature>();IntroEnd<GoatmanderPostIntroFeature>();break;
   case "Kaali":IntroEnd<MoleIntroFeature>();break;
   case "Nogai":IntroEnd<NogaiIntroFeature>();break;
  }
  if(!CombatEntered)throw new InvalidOperationException("Native Boss intro exit did not enter combat");
  if(entity.HasComponentData<DodgeFlag>())throw new InvalidOperationException("Boss intro dodge state remained active");
  var target=entity.GetComponent<TargetComponent>();
  if(target==null||target.target?.Pointer!=player.Pointer)throw new InvalidOperationException("Native Boss target is not the current player");
  Plugin.LogEvent($"boss_spawn_entered entity={entity.GetInstanceID()} kind={kind} phase={component.currentPhaseIndex} target={target.target.GetInstanceID()}");
 }
 public bool CombatEntered=>component?.brainInitDone==true&&component.currentPhaseIndex>0;
 public void WriteHealth()
 {
  var health=entity.GetComponent<Damageable>();
  if(health==null||!health._initialized||!health.isAlive||health.onDieActionStarted)throw new InvalidOperationException("Boss native health is not ready");
  expectedMaximum=health.maxHealth;
  if(!MobSpawnPolicy.TryResolveHealth(expectedMaximum,mode,out expectedHealth))throw new InvalidOperationException("Boss native maximum health is invalid");
  health.currentHealth=expectedHealth;
 }
 public bool HealthMatches
 {get{var h=entity.GetComponent<Damageable>();return h!=null&&h.isAlive&&!h.onDieActionStarted&&Math.Abs(h.maxHealth-expectedMaximum)<=.01f&&Math.Abs(h.currentHealth-expectedHealth)<=.01f;}}
 public void ReleaseAi(){MobAiControl.Ready(entity);Plugin.LogEvent($"boss_spawn_ai_released entity={entity.GetInstanceID()} kind={kind}");}
 public bool NativeAttackActive
 {
  get
  {
   if(!CombatEntered)return false;
   var h=entity.GetComponent<Damageable>();if(h==null||!h.isAlive||h.onDieActionStarted)throw new InvalidOperationException("Boss died before an autonomous attack was observed");
   var b=input.brain;
   return b.TryCast<AltanBossBrain>()?.HasActiveAttackAction(actor)==true||b.TryCast<BaatarBossBrain>()?.HasActiveAttackAction(actor)==true||
    b.TryCast<GoatBossBrain>()?.HasActiveAttackAction(actor)==true||b.TryCast<CorruptedNogaiBrain>()?.HasActiveAttackAction(actor)==true||
    b.TryCast<KaaliBossBrain>()?.HasActiveAttackAction(actor)==true||b.TryCast<NogaiBossBrain>()?.HasActiveAttackAction(actor)==true;
  }
 }
 internal void ClearSpawned()
 {
  // Native summoning features register adds/projectiles here. Destroy only this
  // summon tree, without Kill, rewards, or touching an arena's actors.
  var seen=new HashSet<IntPtr>();var queue=new Queue<Entity>();queue.Enqueue(entity);
  while(queue.Count>0)
  {
   var owner=queue.Dequeue();if(owner==null||owner.destroyed||!seen.Add(owner.Pointer))continue;
   var list=owner.GetComponent<SpawnedGameObjectsComponent>()?.spawnedGameObjects;
   if(list==null)continue;
   for(int i=0;i<list.Count;i++)
   {
    var item=list[i];var child=item.entity;
    if(child!=null&&child.Pointer!=player.Pointer&&child.Pointer!=entity.Pointer)queue.Enqueue(child);
    var obj=item.gameObject;
    if(obj!=null&&obj!=player.gameObject&&obj!=entity.gameObject)UObject.Destroy(obj);
   }
   list.Clear();
  }
 }
 public void Dispose(){ClearSpawned();graph?.Dispose();room?.Dispose();if(bossEntityVariable!=null)UObject.Destroy(bossEntityVariable);bossEntityVariable=null;entityVariables.Clear();graph=null;room=null;}
}
