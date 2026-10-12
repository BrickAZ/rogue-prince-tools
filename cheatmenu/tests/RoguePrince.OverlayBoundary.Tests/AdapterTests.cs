using RoguePrince.CheatMenu;
using RoguePrince.CheatMenu.Core;
using MotherBase.EntityFramework;
using UnityEngine;
internal static class AdapterTests
{
 internal static void Run(Action<string,Action> test)
 {
  void Check(bool c){if(!c)throw new Exception("assertion failed");}
  (GameSession,ReceiverOverlaySource,BoxCollider) Setup()
  {
   Application.isFocused=true;TrainingActions.scope=new(1,1,1,4,0,0);Physics.afterQuery=null;
   var game=new GameSession();var c=new BoxCollider{Pointer=(IntPtr)10,parent=new Entity{Pointer=(IntPtr)2}};Physics.hits=new Collider[]{c};return(game,new(game),c);
  }
  test("native receiver scan reads parent damageable and includes triggers",()=>{var(g,s,c)=Setup();c.isTrigger=true;s.Scan();Check(s.Items.Count==1&&Physics.query==QueryTriggerInteraction.Collide);Check(s.Read(c,out var shape,out var player)==ReceiverRead.Shape&&!player&&shape.Kind==ReceiverShapeKind.Box);});
  test("player receiver classified separately",()=>{var(g,s,c)=Setup();c.parent=g.Player;s.Scan();Check(s.Read(c,out _,out var player)==ReceiverRead.Shape&&player);});
  test("mesh receiver explicitly unsupported",()=>{var(g,s,c)=Setup();var mesh=new MeshCollider{parent=c.parent};Physics.hits=new[]{mesh};s.Scan();Check(s.Items.Count==1&&s.Read(mesh,out _,out _)==ReceiverRead.Unsupported);});
  foreach(var change in new[]{"collider off","inactive","destroyed","dead","dying","no health","no receiver","other scene","other context"}){var key=change;test("exclude "+key,()=>{var(g,s,c)=Setup();switch(key){case "collider off":c.enabled=false;break;case "inactive":c.gameObject.activeInHierarchy=false;break;case "destroyed":c.parent.destroyed=true;break;case "dead":c.parent.health.isAlive=false;break;case "dying":c.parent.health.onDieActionStarted=true;break;case "no health":c.parent.health=null;break;case "no receiver":c.parent.receiver=null;break;case "other scene":c.gameObject.scene=new(){handle=2};break;case "other context":c.parent.context.Pointer=(IntPtr)7;break;}s.Scan();Check(s.Items.Count==0);});}
  foreach(var change in new[]{"focus","not ready","room","generation","context","disabled after scan","receiver removed"}){var key=change;test("reject stale cached receiver "+key,()=>{var(g,s,c)=Setup();s.Scan();Check(s.Items.Count==1);switch(key){case "focus":Application.isFocused=false;break;case "not ready":g.Ready=false;break;case "room":TrainingActions.scope=TrainingActions.scope with {RoomX=2};break;case "generation":TrainingActions.scope=TrainingActions.scope with {Generation=2};break;case "context":g.Player.context.Pointer=(IntPtr)7;break;case "disabled after scan":c.enabled=false;break;case "receiver removed":c.parent.receiver=null;break;}Check(s.Read(c,out _,out _)==ReceiverRead.Ineligible);});}
  test("scope change during native scan clears entire result",()=>{var(g,s,c)=Setup();Physics.afterQuery=()=>TrainingActions.scope=TrainingActions.scope with {Room=8};s.Scan();Check(s.Items.Count==0);});
  test("native scan capacity reports truncation",()=>{var(g,s,c)=Setup();Physics.hits=Enumerable.Repeat<Collider>(c,600).ToArray();s.Scan();Check(s.Limited&&s.Items.Count==1);});
  test("display capacity reports truncation",()=>{var(g,s,c)=Setup();Physics.hits=Enumerable.Range(0,201).Select(i=>(Collider)new BoxCollider{Pointer=(IntPtr)(100+i),parent=c.parent}).ToArray();s.Scan();Check(s.Limited&&s.Items.Count==ReceiverOverlaySource.DisplayCapacity);});
  test("clear removes candidates and prior bound scope",()=>{var(g,s,c)=Setup();s.Scan();Check(s.Items.Count==1);s.Clear();Check(s.Items.Count==0&&s.Read(c,out _,out _)==ReceiverRead.Ineligible);});
  test("negative nonuniform sphere scale uses largest absolute axis",()=>{var(g,s,c)=Setup();var sphere=new SphereCollider{Pointer=(IntPtr)9,parent=c.parent};sphere.transform.lossyScale=new(-2,3,.5f);Physics.hits=new[]{sphere};s.Scan();Check(s.Read(sphere,out var shape,out _)==ReceiverRead.Shape&&shape.Radius==3);});
  test("capsule scale follows its configured axis",()=>{var(g,s,c)=Setup();var capsule=new CapsuleCollider{Pointer=(IntPtr)9,parent=c.parent,direction=0};capsule.transform.lossyScale=new(-2,3,.5f);Physics.hits=new[]{capsule};s.Scan();Check(s.Read(capsule,out var shape,out _)==ReceiverRead.Shape&&shape.Radius==3&&shape.Height==12&&shape.Axis==0);});
  test("summon gate blocks scoped owner and in-room position",()=>{var(g,s,c)=Setup();bool bossAllowed=false;Check(SceneSummonGate.Block(g,true,c.parent,new(1,2,0),(e,boss)=>{bossAllowed=boss;return true;}));Check(bossAllowed);});
  test("summon gate off does not inspect owner",()=>{var(g,s,c)=Setup();Check(!SceneSummonGate.Block(g,false,c.parent,new(),(_,_)=>throw new Exception("must not inspect")));});
  test("summon gate rejects focus loss",()=>{var(g,s,c)=Setup();Application.isFocused=false;Check(!SceneSummonGate.Block(g,true,c.parent,new(),(_,_)=>true));});
  test("summon gate rejects outside-room destination",()=>{var(g,s,c)=Setup();g.Player.roomState.currentRoom.inside=false;Check(!SceneSummonGate.Block(g,true,c.parent,new(),(_,_)=>true));});
  test("summon gate rejects non-enemy owner",()=>{var(g,s,c)=Setup();Check(!SceneSummonGate.Block(g,true,c.parent,new(),(_,_)=>false));});
  test("summon gate rejects invalid position",()=>{var(g,s,c)=Setup();Check(!SceneSummonGate.Block(g,true,c.parent,new(float.NaN,0,0),(_,_)=>true));});
  test("real Harmony prefix skips supported native summon entry",()=>{var(g,s,c)=Setup();SceneActions.Current=new(){game=g,NoArenaSpawn=true};Check(!SceneSingleSummonPatch.Prefix(c.parent,new()));});
  test("real Harmony prefix allows unarmed native summon",()=>{var(g,s,c)=Setup();SceneActions.Current=new(){game=g};Check(SceneSingleSummonPatch.Prefix(c.parent,new()));});
  test("real Harmony prefix never suppresses another room",()=>{var(g,s,c)=Setup();SceneActions.Current=new(){game=g,NoArenaSpawn=true};g.Player.roomState.currentRoom.inside=false;Check(SceneSingleSummonPatch.Prefix(c.parent,new()));});
  test("native read exception disables control and allows original summon",()=>{var(g,s,c)=Setup();SceneActions.Current=new(){game=g,NoArenaSpawn=true,enemy=(_,_)=>throw new Exception("native read failed")};Check(SceneSingleSummonPatch.Prefix(c.parent,new()));Check(!SceneActions.Current.NoArenaSpawn&&SceneActions.Current.resets==1);});
  test("native query exception clears prior receiver result",()=>{var(g,s,c)=Setup();s.Scan();Check(s.Items.Count==1);Physics.afterQuery=()=>throw new Exception("native overlap failed");try{s.Scan();throw new Exception("expected failure");}catch(Exception e){Check(e.Message=="native overlap failed");}Check(s.Items.Count==0&&s.Read(c,out _,out _)==ReceiverRead.Ineligible);});
 }
}
