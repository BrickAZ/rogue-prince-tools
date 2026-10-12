using RoguePrince.CheatMenu;
using RoguePrince.CheatMenu.Core;
using MotherBase.EntityFramework;
using MotherBase.ActionFramework;
using UnityEngine;
internal static partial class Program
{
 static int passed,failed;static GameSession game;static Entity boss;static NogaiBossBrain brain;static NogaiBossComponent component;static TargetComponent target;static MoveIntent3D move;static BossTargetActions actions;static BossTargetActions.Entry entry;
 static void Assert(bool value){if(!value)throw new Exception("Assertion failed");}
 static void Reject(Action action){try{action();}catch(InvalidOperationException){return;}throw new Exception("Expected rejection");}
 static void Reset()
 {
  ExtendedAvailable(false);
  Application.isFocused=true;RunTools.RunId=1;BossTargetActions.Available=BossTargetActions.AdditionalAttackAvailable=true;game=new();TrainingActions.Scope=new(1,game.PlayerPointer.ToInt64(),3,4,0,0);
  SceneActions.FallbackRoom=null;var room=new LevelRoomData();game.Player.Components[typeof(RoomStateComponent)]=new RoomStateComponent{currentRoom=room};
  boss=new(){context=game.Player.context};brain=new();component=new(){Owner=boss};target=new(){target=game.Player,lastTarget=game.Player};move=new();
  boss.Components[typeof(NogaiBossComponent)]=component;boss.Components[typeof(BSAIInput)]=new BSAIInput{brain=brain};boss.Components[typeof(TargetComponent)]=target;boss.Components[typeof(MoveIntent3D)]=move;boss.Components[typeof(MobComponent)]=new MobComponent();boss.Components[typeof(Damageable)]=new Damageable();boss.Components[typeof(AIStateComponent)]=new AIStateComponent();boss.Components[typeof(RoomStateComponent)]=new RoomStateComponent{currentRoom=room};
  UnityEngine.Object.Bosses=new BossComponentBase[]{component};actions=new(game);actions.Refresh();entry=actions.Entries.Single();
 }
 static void Check(string name,Action test){Reset();try{test();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
 static void Arm(){actions.SetEnabled(true,entry);Assert(actions.Enabled);}
 static bool Block(bool stop=false)=>actions.Block(boss,brain.Pointer,0,component,stopMovement:stop);
 static KaaliBossBrain Kaali()
 {
  var kb=new KaaliBossBrain();var kc=new KaaliBossComponent{Owner=boss};
  boss.Components.Remove(typeof(NogaiBossComponent));boss.Components[typeof(KaaliBossComponent)]=kc;boss.GetComponent<BSAIInput>().brain=kb;
  UnityEngine.Object.Bosses=new BossComponentBase[]{kc};actions.Refresh();entry=actions.Entries.Single();return kb;
 }
 static void Main()
 {
  Check("off does not read native owner",()=>{boss.ThrowRead=true;var reads=boss.Reads;Assert(!Block()&&!actions.Enabled&&boss.Reads==reads);});
  Check("bound current boss filters without clearing native target",()=>{Arm();Assert(Block()&&actions.Enabled&&target.target==game.Player&&target.lastTarget==game.Player);});
  Check("movement filter zeros intent only",()=>{Arm();Assert(Block(true)&&move.magnitude==0&&!entry.Ai.stayIdle&&component.currentPhaseIndex==1);});
  Check("orientation filter does not write move intent",()=>{Arm();Assert(Block()&&move.magnitude==1);});
  Check("manual off needs no native reads",()=>{Arm();boss.ThrowRead=true;game.Ready=false;actions.SetEnabled(false);Assert(!actions.Enabled&&!Block());});
  Check("missing patch rejects",()=>{BossTargetActions.Available=false;Reject(()=>actions.SetEnabled(true,entry));Assert(!actions.Enabled);});
  Check("loading refuses activation",()=>{game.Ready=false;Reject(()=>actions.SetEnabled(true,entry));});
  Check("focus loss refuses activation",()=>{Application.isFocused=false;Reject(()=>actions.SetEnabled(true,entry));});
  Check("stale list entry refuses activation",()=>{actions.Refresh();Reject(()=>actions.SetEnabled(true,entry));});
  Check("foreign owner passes without retiring current binding",()=>{Arm();Assert(!actions.Block(new(),brain.Pointer,0)&&actions.Enabled);});
  Check("foreign brain passes without retiring current binding",()=>{Arm();Assert(!actions.Block(boss,(IntPtr)999,0)&&actions.Enabled);});
  Check("wrong kind passes",()=>{Arm();Assert(!actions.Block(boss,brain.Pointer,1)&&actions.Enabled);});
  Check("foreign component passes",()=>{Arm();Assert(!actions.Block(boss,brain.Pointer,0,new NogaiBossComponent())&&actions.Enabled);});
  Check("foreign explicit target passes",()=>{Arm();Assert(!actions.Block(boss,brain.Pointer,0,explicitTarget:new())&&actions.Enabled);});
  Check("foreign target component passes",()=>{Arm();Assert(!actions.Block(boss,brain.Pointer,0,target:new())&&actions.Enabled);});
  Check("foreign move argument passes without writes",()=>{Arm();var other=new MoveIntent3D();Assert(!actions.Block(boss,brain.Pointer,0,move:other,stopMovement:true)&&other.magnitude==1&&move.magnitude==1);});
  Check("another current native target permits original",()=>{Arm();target.target=new();Assert(!Block()&&actions.Enabled);});
  Check("null native target permits original",()=>{Arm();target.target=null;Assert(!Block()&&actions.Enabled);});
  Check("room scope change retires",()=>{Arm();TrainingActions.Scope=TrainingActions.Scope with{Room=90};Assert(!Block()&&!actions.Enabled);});
  Check("generation change retires",()=>{Arm();TrainingActions.Scope=TrainingActions.Scope with{Generation=2};actions.Tick();Assert(!actions.Enabled);});
  Check("run change retires",()=>{Arm();RunTools.RunId=2;Assert(!Block()&&!actions.Enabled);});
  Check("loading retires",()=>{Arm();game.Ready=false;actions.Tick();Assert(!actions.Enabled);});
  Check("focus loss retires",()=>{Arm();Application.isFocused=false;Assert(!Block()&&!actions.Enabled);});
  Check("boss death retires",()=>{Arm();entry.Health.isAlive=false;actions.Tick();Assert(!actions.Enabled);});
  Check("boss death action retires",()=>{Arm();entry.Health.onDieActionStarted=true;Assert(!Block()&&!actions.Enabled);});
  Check("destroyed boss retires",()=>{Arm();boss.destroyed=true;Assert(!Block()&&!actions.Enabled);});
  Check("inactive boss retires",()=>{Arm();boss.gameObject.activeInHierarchy=false;Assert(!Block()&&!actions.Enabled);});
  Check("other scene retires",()=>{Arm();boss.gameObject.scene.handle=9;Assert(!Block()&&!actions.Enabled);});
  Check("other context retires",()=>{Arm();boss.context=new();Assert(!Block()&&!actions.Enabled);});
  Check("other boss room retires",()=>{Arm();boss.GetComponent<RoomStateComponent>().currentRoom=new();Assert(!Block()&&!actions.Enabled);});
  Check("replacement target component retires",()=>{Arm();boss.Components[typeof(TargetComponent)]=new TargetComponent();Assert(!Block()&&!actions.Enabled);});
  Check("replacement move component retires",()=>{Arm();boss.Components[typeof(MoveIntent3D)]=new MoveIntent3D();Assert(!Block()&&!actions.Enabled);});
  Check("replacement input component retires",()=>{Arm();boss.Components[typeof(BSAIInput)]=new BSAIInput{brain=brain};Assert(!Block()&&!actions.Enabled);});
  Check("replacement brain retires",()=>{Arm();entry.Input.brain=new NogaiBossBrain();actions.Tick();Assert(!actions.Enabled);});
  Check("replacement boss component retires",()=>{Arm();boss.Components[typeof(NogaiBossComponent)]=new NogaiBossComponent();Assert(!Block()&&!actions.Enabled);});
  Check("replacement health retires",()=>{Arm();boss.Components[typeof(Damageable)]=new Damageable();Assert(!Block()&&!actions.Enabled);});
  Check("replacement AI state retires",()=>{Arm();boss.Components[typeof(AIStateComponent)]=new AIStateComponent();Assert(!Block()&&!actions.Enabled);});
  Check("phase zero refuses activation",()=>{component.currentPhaseIndex=0;Reject(()=>actions.SetEnabled(true,entry));});
  Check("uninitialized boss refuses activation",()=>{component.brainInitDone=false;Reject(()=>actions.SetEnabled(true,entry));});
  Check("idle AI refuses activation",()=>{entry.Ai.stayIdle=true;Reject(()=>actions.SetEnabled(true,entry));});
  Check("native read failure disables filter and permits original",()=>{Arm();boss.ThrowRead=true;Assert(!Block()&&!actions.Enabled);});
  Check("clear preserves target and native phase",()=>{Arm();actions.Clear();Assert(!actions.Enabled&&target.target==game.Player&&target.lastTarget==game.Player&&component.currentPhaseIndex==1&&move.magnitude==1);});
  Check("native phase advancement keeps valid binding",()=>{Arm();component.currentPhaseIndex=2;Assert(Block()&&actions.Enabled);});
  Check("patch availability lost retires filter",()=>{Arm();BossTargetActions.Available=false;Assert(!Block()&&!actions.Enabled);});
  Check("native Nogai orientation prefix filters current player",()=>{Arm();Assert(!NogaiTargetOrientationPatch.Prefix(brain,boss,game.Player)&&target.target==game.Player);});
  Check("native orientation prefix permits foreign and null targets",()=>{Arm();Assert(NogaiTargetOrientationPatch.Prefix(brain,boss,new())&&NogaiTargetOrientationPatch.Prefix(brain,boss,null)&&actions.Enabled);});
  Check("native walk prefix filters and stops prior movement intent",()=>{Arm();Assert(!NogaiTargetWalkPatch.Prefix(brain,boss,target,move)&&move.magnitude==0);});
  Check("native walk prefix ignores foreign move and null target component",()=>{Arm();Assert(NogaiTargetWalkPatch.Prefix(brain,boss,target,new())&&NogaiTargetWalkPatch.Prefix(brain,boss,null,move)&&move.magnitude==1);});
  Check("native next attack prefix explicitly returns false",()=>{Arm();bool result=true;Assert(!NogaiTargetAttackPatch.Prefix(brain,boss,component,ref result)&&!result&&component.currentPhaseIndex==1);});
  Check("native next attack prefix off preserves result and reads nothing",()=>{boss.ThrowRead=true;var reads=boss.Reads;bool result=true;Assert(NogaiTargetAttackPatch.Prefix(brain,boss,component,ref result)&&result&&boss.Reads==reads);});
  Check("native prefix exception retires filter and permits original",()=>{Arm();boss.ThrowRead=true;bool result=true;Assert(NogaiTargetAttackPatch.Prefix(brain,boss,component,ref result)&&result&&!actions.Enabled);});
  Check("native Kaali orientation filters only current player",()=>{var kb=Kaali();Arm();Assert(!KaaliTargetOrientationPatch.Prefix(kb,boss,game.Player)&&KaaliTargetOrientationPatch.Prefix(kb,boss,new()));});
  Check("native Kaali melee prefix stops input without writing phase",()=>{var kb=Kaali();Arm();Assert(!KaaliTargetMeleePatch.Prefix(kb,boss,(KaaliBossComponent)entry.Component)&&move.magnitude==0&&entry.Component.currentPhaseIndex==1&&!entry.Ai.stayIdle);});
  Check("native Kaali ranged prefix preserves target reference",()=>{var kb=Kaali();Arm();Assert(!KaaliTargetRangePatch.Prefix(kb,boss,(KaaliBossComponent)entry.Component,game.Player)&&target.target==game.Player&&move.magnitude==0);});
  Check("native Kaali ranged prefix allows foreign target",()=>{var kb=Kaali();Arm();Assert(KaaliTargetRangePatch.Prefix(kb,boss,(KaaliBossComponent)entry.Component,new())&&move.magnitude==1);});
  Check("unsupported Boss remains listed and rejects activation",()=>{var unknown=new UnknownBossComponent{Owner=boss};boss.GetComponent<BSAIInput>().brain=new UnknownBossBrain();UnityEngine.Object.Bosses=new BossComponentBase[]{unknown};actions.Refresh();Assert(actions.Entries.Single().Kind<0);Reject(()=>actions.SetEnabled(true,actions.Entries.Single()));Assert(!actions.Enabled&&move.magnitude==1);});
  Check("mismatched component and brain is unsupported",()=>{boss.GetComponent<BSAIInput>().brain=new KaaliBossBrain();actions.Refresh();Reject(()=>actions.SetEnabled(true,actions.Entries.Single()));});
  Check("scope changed since scan refuses activation",()=>{TrainingActions.Scope=TrainingActions.Scope with{Generation=3};Reject(()=>actions.SetEnabled(true,entry));Assert(!actions.Enabled);});
  Check("missing target component refuses activation",()=>{boss.Components.Remove(typeof(TargetComponent));actions.Refresh();Reject(()=>actions.SetEnabled(true,actions.Entries.Single()));});
  Check("missing movement component refuses activation",()=>{boss.Components.Remove(typeof(MoveIntent3D));actions.Refresh();Reject(()=>actions.SetEnabled(true,actions.Entries.Single()));});
  Check("native map fallback resolves Boss without room state",()=>{boss.Components.Remove(typeof(RoomStateComponent));SceneActions.FallbackRoom=game.Player.GetComponent<RoomStateComponent>().currentRoom;actions.Refresh();entry=actions.Entries.Single();Arm();Assert(Block());});
  Check("unresolved Boss room excluded from list",()=>{boss.Components.Remove(typeof(RoomStateComponent));actions.Refresh();Assert(actions.Entries.Count==0);});
  Check("foreign room Boss excluded from list",()=>{boss.GetComponent<RoomStateComponent>().currentRoom=new();actions.Refresh();Assert(actions.Entries.Count==0);});
  Check("scope drift during scan rejects entire list",()=>{boss.BeforeRead=()=>TrainingActions.Scope=TrainingActions.Scope with{Generation=8};Reject(()=>actions.Refresh());Assert(actions.Entries.Count==0&&!actions.Enabled);});
  ExtraBossTests();
  ExtraPrefixTests();
  ExtendedBossTests();
  ExtendedPrefixTests();
  Console.WriteLine($"RESULT {passed} passed, {failed} failed");Environment.ExitCode=failed==0?0:1;
 }
 static NativeObject ExtraBoss(int kind)
 {
  NativeObject nextBrain=kind==2?new BaatarBossBrain():new CorruptedNogaiBrain();
  BossComponentBase nextComponent=kind==2?new BaatarBossComponent{Owner=boss}:new CorruptedNogaiBossComponent{Owner=boss};
  boss.Components.Remove(typeof(NogaiBossComponent));boss.Components[nextComponent.GetType()]=nextComponent;boss.GetComponent<BSAIInput>().brain=nextBrain;
  UnityEngine.Object.Bosses=new[]{nextComponent};actions.Refresh();entry=actions.Entries.Single();return nextBrain;
 }
 static bool ExtraBlock(int kind,NativeObject next)=>actions.Block(boss,next.Pointer,kind,entry.Component,move:kind==2?move:null);
 static void ExtraBossTests()
 {
  foreach(var kind in new[]{2,3})
  {
   string name=kind==2?"Baatar":"Corrupted Nogai";
   Check(name+" discovery matches component and brain",()=>{ExtraBoss(kind);Assert(entry.Kind==kind&&entry.Name==name);});
   Check(name+" new attack filter retains target phase and movement",()=>{var next=ExtraBoss(kind);Arm();Assert(ExtraBlock(kind,next)&&target.target==game.Player&&target.lastTarget==game.Player&&entry.Component.currentPhaseIndex==1&&move.magnitude==1&&!entry.Ai.stayIdle);});
   Check(name+" off performs no native reads",()=>{var next=ExtraBoss(kind);boss.ThrowRead=true;var reads=boss.Reads;Assert(!ExtraBlock(kind,next)&&boss.Reads==reads);});
   Check(name+" another owner passes without retiring binding",()=>{var next=ExtraBoss(kind);Arm();Assert(!actions.Block(new(),next.Pointer,kind,entry.Component)&&actions.Enabled);});
   Check(name+" another brain passes",()=>{ExtraBoss(kind);Arm();Assert(!actions.Block(boss,(IntPtr)99999,kind,entry.Component)&&actions.Enabled);});
   Check(name+" foreign native component passes",()=>{var next=ExtraBoss(kind);Arm();Assert(!actions.Block(boss,next.Pointer,kind,new BossComponentBase())&&actions.Enabled);});
   Check(name+" target rule follows native selection dependency",()=>{var next=ExtraBoss(kind);Arm();target.target=new();Assert(ExtraBlock(kind,next)==(kind==2)&&actions.Enabled&&move.magnitude==1);});
   Check(name+" null target rule follows native selection dependency",()=>{var next=ExtraBoss(kind);Arm();target.target=null;Assert(ExtraBlock(kind,next)==(kind==2)&&actions.Enabled);});
   Check(name+" scope change retires",()=>{var next=ExtraBoss(kind);Arm();TrainingActions.Scope=TrainingActions.Scope with{Room=7};Assert(!ExtraBlock(kind,next)&&!actions.Enabled);});
   Check(name+" generation change retires",()=>{ExtraBoss(kind);Arm();TrainingActions.Scope=TrainingActions.Scope with{Generation=7};actions.Tick();Assert(!actions.Enabled);});
   Check(name+" run change retires",()=>{var next=ExtraBoss(kind);Arm();RunTools.RunId=8;Assert(!ExtraBlock(kind,next)&&!actions.Enabled);});
   Check(name+" focus loss retires",()=>{ExtraBoss(kind);Arm();Application.isFocused=false;actions.Tick();Assert(!actions.Enabled);});
   Check(name+" loading retires",()=>{ExtraBoss(kind);Arm();game.Ready=false;actions.Tick();Assert(!actions.Enabled);});
   Check(name+" death retires",()=>{var next=ExtraBoss(kind);Arm();entry.Health.isAlive=false;Assert(!ExtraBlock(kind,next)&&!actions.Enabled);});
   Check(name+" replacement target only retires consuming controller",()=>{var next=ExtraBoss(kind);Arm();boss.Components[typeof(TargetComponent)]=new TargetComponent();Assert(ExtraBlock(kind,next)==(kind==2)&&actions.Enabled==(kind==2));});
   Check(name+" replacement brain retires",()=>{var next=ExtraBoss(kind);Arm();entry.Input.brain=new NativeObject();Assert(!ExtraBlock(kind,next)&&!actions.Enabled);});
   Check(name+" phase advancement is not suppressed by adapter",()=>{var next=ExtraBoss(kind);Arm();entry.Component.currentPhaseIndex=2;Assert(ExtraBlock(kind,next)&&entry.Component.currentPhaseIndex==2);});
   Check(name+" native read failure disables only this mode",()=>{var next=ExtraBoss(kind);Arm();boss.ThrowRead=true;Assert(!ExtraBlock(kind,next)&&!actions.Enabled);});
   Check(name+" manual disable requires no native read",()=>{var next=ExtraBoss(kind);Arm();boss.ThrowRead=true;actions.SetEnabled(false);Assert(!ExtraBlock(kind,next)&&!actions.Enabled&&move.magnitude==1);});
   Check(name+" mismatched brain is unsupported",()=>{ExtraBoss(kind);entry.Input.brain=kind==2?new CorruptedNogaiBrain():new BaatarBossBrain();actions.Refresh();Assert(actions.Entries.Single().Kind<0);Reject(()=>actions.SetEnabled(true,actions.Entries.Single()));});
  }
  Check("Baatar foreign movement input passes without mutation",()=>{var next=ExtraBoss(2);Arm();var other=new MoveIntent3D();Assert(!actions.Block(boss,next.Pointer,2,entry.Component,move:other)&&actions.Enabled&&other.magnitude==1&&move.magnitude==1);});
  Check("Baatar missing movement component rejects activation",()=>{ExtraBoss(2);Assert(entry.Kind==2);boss.Components.Remove(typeof(MoveIntent3D));actions.Refresh();Reject(()=>actions.SetEnabled(true,actions.Entries.Single()));});
  Check("Corrupted Nogai needs no movement component for new attack filtering",()=>{var next=ExtraBoss(3);boss.Components.Remove(typeof(MoveIntent3D));actions.Refresh();entry=actions.Entries.Single();Arm();Assert(ExtraBlock(3,next));});
  Check("Corrupted Nogai unrelated movement replacement does not retire filter",()=>{var next=ExtraBoss(3);Arm();boss.Components[typeof(MoveIntent3D)]=new MoveIntent3D();Assert(ExtraBlock(3,next)&&actions.Enabled&&move.magnitude==1);});
  Check("Baatar scan and filtering never query unused TargetComponent",()=>{var next=ExtraBoss(2);boss.ThrowComponent=typeof(TargetComponent);actions.Refresh();entry=actions.Entries.Single();Arm();Assert(ExtraBlock(2,next));});
 }
 static bool ExtraPrefix(int kind,NativeObject next,Entity owner,BossComponentBase nativeComponent,MoveIntent3D nativeMove,ref bool result)=>kind==2?
  BaatarTargetAttackPatch.Prefix((BaatarBossBrain)next,owner,nativeMove,(BaatarBossComponent)nativeComponent,ref result):
  CorruptedNogaiTargetAttackPatch.Prefix((CorruptedNogaiBrain)next,owner,(CorruptedNogaiBossComponent)nativeComponent,ref result);
 static void ExtraPrefixTests()
 {
  foreach(var kind in new[]{2,3})
  {
   string name=kind==2?"Baatar":"Corrupted Nogai";
   Check(name+" actual prefix skips new selection and returns false",()=>{var next=ExtraBoss(kind);Arm();bool result=true;Assert(!ExtraPrefix(kind,next,boss,entry.Component,move,ref result)&&!result&&move.magnitude==1&&target.target==game.Player&&entry.Component.currentPhaseIndex==1);});
   Check(name+" actual prefix off preserves result and reads nothing",()=>{var next=ExtraBoss(kind);boss.ThrowRead=true;var reads=boss.Reads;bool result=true;Assert(ExtraPrefix(kind,next,boss,entry.Component,move,ref result)&&result&&reads==boss.Reads);});
   Check(name+" actual prefix foreign owner permits original",()=>{var next=ExtraBoss(kind);Arm();bool result=true;Assert(ExtraPrefix(kind,next,new(),entry.Component,move,ref result)&&result&&actions.Enabled);});
   Check(name+" actual prefix null component permits original",()=>{var next=ExtraBoss(kind);Arm();bool result=true;Assert(ExtraPrefix(kind,next,boss,null,move,ref result)&&result&&actions.Enabled);});
   Check(name+" actual prefix foreign component permits original",()=>{var next=ExtraBoss(kind);Arm();bool result=true;BossComponentBase foreign=kind==2?new BaatarBossComponent():new CorruptedNogaiBossComponent();Assert(ExtraPrefix(kind,next,boss,foreign,move,ref result)&&result&&actions.Enabled);});
   Check(name+" actual prefix uses controller-specific target rule",()=>{var next=ExtraBoss(kind);Arm();target.target=new();bool result=true;Assert(ExtraPrefix(kind,next,boss,entry.Component,move,ref result)==(kind!=2)&&result==(kind!=2)&&actions.Enabled);});
   Check(name+" actual prefix read error permits original and retires",()=>{var next=ExtraBoss(kind);Arm();boss.ThrowRead=true;bool result=true;Assert(ExtraPrefix(kind,next,boss,entry.Component,move,ref result)&&result&&!actions.Enabled);});
   Check(name+" actual prefix scope change permits original and retires",()=>{var next=ExtraBoss(kind);Arm();TrainingActions.Scope=TrainingActions.Scope with{Room=99};bool result=true;Assert(ExtraPrefix(kind,next,boss,entry.Component,move,ref result)&&result&&!actions.Enabled);});
   Check(name+" additional patch group loss retires",()=>{var next=ExtraBoss(kind);Arm();BossTargetActions.AdditionalAttackAvailable=false;bool result=true;Assert(ExtraPrefix(kind,next,boss,entry.Component,move,ref result)&&result&&!actions.Enabled);});
   Check(name+" old patch group failure does not block added group",()=>{var next=ExtraBoss(kind);BossTargetActions.Available=false;Arm();bool result=true;Assert(!ExtraPrefix(kind,next,boss,entry.Component,move,ref result)&&!result&&actions.Enabled);});
   Check(name+" phase zero is refused",()=>{ExtraBoss(kind);entry.Component.currentPhaseIndex=0;Reject(()=>actions.SetEnabled(true,entry));});
   Check(name+" idle AI is refused",()=>{ExtraBoss(kind);entry.Ai.stayIdle=true;Reject(()=>actions.SetEnabled(true,entry));});
   Check(name+" initialization incomplete is refused",()=>{ExtraBoss(kind);entry.Component.brainInitDone=false;Reject(()=>actions.SetEnabled(true,entry));});
   Check(name+" missing target follows native dependency",()=>{var next=ExtraBoss(kind);boss.Components.Remove(typeof(TargetComponent));actions.Refresh();entry=actions.Entries.Single();if(kind==2){Arm();Assert(ExtraBlock(kind,next));}else Reject(()=>actions.SetEnabled(true,entry));});
  }
  Check("Baatar actual prefix foreign movement permits original",()=>{var next=ExtraBoss(2);Arm();bool result=true;Assert(ExtraPrefix(2,next,boss,entry.Component,new(),ref result)&&result&&actions.Enabled&&move.magnitude==1);});
  Check("Baatar actual prefix null movement permits original",()=>{var next=ExtraBoss(2);Arm();bool result=true;Assert(ExtraPrefix(2,next,boss,entry.Component,null,ref result)&&result&&actions.Enabled);});
  Check("added patch group failure preserves original Nogai filter",()=>{BossTargetActions.AdditionalAttackAvailable=false;Arm();Assert(Block()&&actions.Enabled);});
  Check("added patch group failure preserves original Kaali filter",()=>{var next=Kaali();BossTargetActions.AdditionalAttackAvailable=false;Arm();Assert(!KaaliTargetOrientationPatch.Prefix(next,boss,game.Player)&&actions.Enabled);});
  Check("new Boss activation rejects missing added patch group",()=>{ExtraBoss(2);BossTargetActions.AdditionalAttackAvailable=false;Reject(()=>actions.SetEnabled(true,entry));Assert(!actions.Enabled);});
 }
}
