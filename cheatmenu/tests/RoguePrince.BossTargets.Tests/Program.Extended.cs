using RoguePrince.CheatMenu;
using MotherBase.EntityFramework;
using MotherBase.ActionFramework;
using UnityEngine;
internal static partial class Program
{
 static void ExtendedAvailable(bool value)=>BossTargetActions.ExtendedAvailable=value;
 static NativeObject ExtendedBoss(int kind)
 {
  ExtendedAvailable(true);
  NativeObject next=kind==4?new AltanBossBrain():new GoatBossBrain();
  BossComponentBase part=kind==4?new AltanBossComponent{Owner=boss}:new GoatBossComponent{Owner=boss};
  boss.Components.Remove(typeof(NogaiBossComponent));boss.Components[part.GetType()]=part;boss.GetComponent<BSAIInput>().brain=next;
  UnityEngine.Object.Bosses=new[]{part};actions.Refresh();entry=actions.Entries.Single();return next;
 }
 static bool ExtendedBlock(int kind,NativeObject next)=>actions.Block(boss,next.Pointer,kind,entry.Component,target:target,move:kind==4?move:null);
 static void ExtendedBossTests()
 {
  foreach(var kind in new[]{4,5})
  {
   var name=kind==4?"Altan":"Berude";
   Check(name+" matches canonical native component and brain",()=>{ExtendedBoss(kind);Assert(entry.Kind==kind&&entry.Name==name&&BossTargetActions.PatchReady(kind));});
   Check(name+" selected filtering preserves target phase AI and movement",()=>{var next=ExtendedBoss(kind);Arm();Assert(ExtendedBlock(kind,next)&&target.target==game.Player&&target.lastTarget==game.Player&&entry.Component.currentPhaseIndex==1&&!entry.Ai.stayIdle&&move.magnitude==1);});
   Check(name+" off does not inspect native data",()=>{var next=ExtendedBoss(kind);boss.ThrowRead=true;var reads=boss.Reads;Assert(!ExtendedBlock(kind,next)&&reads==boss.Reads);});
   Check(name+" foreign owner permits native",()=>{var next=ExtendedBoss(kind);Arm();Assert(!actions.Block(new(),next.Pointer,kind,entry.Component)&&actions.Enabled);});
   Check(name+" foreign brain permits native",()=>{ExtendedBoss(kind);Arm();Assert(!actions.Block(boss,(IntPtr)999999,kind,entry.Component)&&actions.Enabled);});
   Check(name+" foreign component permits native",()=>{var next=ExtendedBoss(kind);Arm();Assert(!actions.Block(boss,next.Pointer,kind,new BossComponentBase())&&actions.Enabled);});
   Check(name+" nonplayer target permits native",()=>{var next=ExtendedBoss(kind);Arm();target.target=new();Assert(!ExtendedBlock(kind,next)&&actions.Enabled);});
   Check(name+" null target permits native",()=>{var next=ExtendedBoss(kind);Arm();target.target=null;Assert(!ExtendedBlock(kind,next)&&actions.Enabled);});
   Check(name+" replaced target retires binding",()=>{var next=ExtendedBoss(kind);Arm();boss.Components[typeof(TargetComponent)]=new TargetComponent();Assert(!ExtendedBlock(kind,next)&&!actions.Enabled);});
   Check(name+" replaced move retires binding",()=>{var next=ExtendedBoss(kind);Arm();boss.Components[typeof(MoveIntent3D)]=new MoveIntent3D();Assert(!ExtendedBlock(kind,next)&&!actions.Enabled);});
   Check(name+" replaced typed boss component retires binding",()=>{var next=ExtendedBoss(kind);Arm();boss.Components[entry.Component.GetType()]=kind==4?new AltanBossComponent():new GoatBossComponent();Assert(!ExtendedBlock(kind,next)&&!actions.Enabled);});
   Check(name+" room change retires binding",()=>{var next=ExtendedBoss(kind);Arm();TrainingActions.Scope=TrainingActions.Scope with{Room=12};Assert(!ExtendedBlock(kind,next)&&!actions.Enabled);});
   Check(name+" run change retires binding",()=>{var next=ExtendedBoss(kind);Arm();RunTools.RunId++;Assert(!ExtendedBlock(kind,next)&&!actions.Enabled);});
   Check(name+" focus loss retires binding",()=>{var next=ExtendedBoss(kind);Arm();Application.isFocused=false;Assert(!ExtendedBlock(kind,next)&&!actions.Enabled);});
   Check(name+" loading retires binding",()=>{var next=ExtendedBoss(kind);Arm();game.Ready=false;Assert(!ExtendedBlock(kind,next)&&!actions.Enabled);});
   Check(name+" death retires binding",()=>{var next=ExtendedBoss(kind);Arm();entry.Health.isAlive=false;Assert(!ExtendedBlock(kind,next)&&!actions.Enabled);});
   Check(name+" native read failure retires and permits original",()=>{var next=ExtendedBoss(kind);Arm();boss.ThrowRead=true;Assert(!ExtendedBlock(kind,next)&&!actions.Enabled);});
   Check(name+" manual off needs no native reads",()=>{var next=ExtendedBoss(kind);Arm();boss.ThrowRead=true;actions.SetEnabled(false);Assert(!ExtendedBlock(kind,next)&&!actions.Enabled);});
   Check(name+" mismatched brain rejects activation",()=>{ExtendedBoss(kind);entry.Input.brain=new UnknownBossBrain();actions.Refresh();Assert(actions.Entries.Single().Kind<0);Reject(()=>actions.SetEnabled(true,actions.Entries.Single()));});
   Check(name+" missing target rejects activation",()=>{ExtendedBoss(kind);boss.Components.Remove(typeof(TargetComponent));actions.Refresh();Reject(()=>actions.SetEnabled(true,actions.Entries.Single()));});
   Check(name+" missing move rejects activation",()=>{ExtendedBoss(kind);boss.Components.Remove(typeof(MoveIntent3D));actions.Refresh();Reject(()=>actions.SetEnabled(true,actions.Entries.Single()));});
   Check(name+" phase zero rejects activation",()=>{ExtendedBoss(kind);entry.Component.currentPhaseIndex=0;Reject(()=>actions.SetEnabled(true,entry));});
   Check(name+" idle AI rejects activation",()=>{ExtendedBoss(kind);entry.Ai.stayIdle=true;Reject(()=>actions.SetEnabled(true,entry));});
   Check(name+" phase progress stays native",()=>{var next=ExtendedBoss(kind);Arm();entry.Component.currentPhaseIndex=2;Assert(ExtendedBlock(kind,next)&&entry.Component.currentPhaseIndex==2);});
   Check(name+" old patch groups can fail independently",()=>{var next=ExtendedBoss(kind);BossTargetActions.Available=BossTargetActions.AdditionalAttackAvailable=false;Arm();Assert(ExtendedBlock(kind,next)&&actions.Enabled);});
   Check(name+" extended group loss retires only current binding",()=>{var next=ExtendedBoss(kind);Arm();ExtendedAvailable(false);Assert(!ExtendedBlock(kind,next)&&!actions.Enabled);});
  }
  Check("extended group unavailable preserves Nogai",()=>{ExtendedAvailable(false);Arm();Assert(Block());});
  Check("extended group unavailable preserves Baatar",()=>{ExtendedAvailable(false);var next=ExtraBoss(2);Arm();Assert(ExtraBlock(2,next));});
 }
 static bool ExtendedPrefix(int kind,NativeObject next,Entity owner,BossComponentBase part,MoveIntent3D nativeMove,AIStateComponent ai,TargetComponent nativeTarget,ref bool result)
 {
  var type=typeof(BossTargetActions).Assembly.GetType("RoguePrince.CheatMenu."+(kind==4?"AltanTargetAttackPatch":"BerudeTargetFillerPatch"))??throw new Exception("Missing extended native Prefix");
  var method=type.GetMethod("Prefix",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)??throw new Exception("Missing Prefix method");
  object[] args=kind==4?new object[]{next,owner,part,nativeMove}:new object[]{next,owner,ai,nativeTarget,result};
  var runOriginal=(bool)method.Invoke(null,args);if(kind==5)result=(bool)args[4];return runOriginal;
 }
 static bool CurrentExtendedPrefix(int kind,NativeObject next,ref bool result)=>ExtendedPrefix(kind,next,boss,entry.Component,move,entry.Ai,target,ref result);
 static void ExtendedPrefixTests()
 {
  foreach(var kind in new[]{4,5})
  {
   var name=kind==4?"Altan":"Berude";
   Check(name+" actual Prefix blocks only connected entry",()=>{var next=ExtendedBoss(kind);Arm();bool result=true;Assert(!CurrentExtendedPrefix(kind,next,ref result)&&result==(kind==4)&&move.magnitude==1&&target.target==game.Player&&entry.Component.currentPhaseIndex==1&&!entry.Ai.stayIdle);});
   Check(name+" disabled actual Prefix reads nothing",()=>{var next=ExtendedBoss(kind);boss.ThrowRead=true;var reads=boss.Reads;bool result=true;Assert(CurrentExtendedPrefix(kind,next,ref result)&&result&&reads==boss.Reads);});
   Check(name+" foreign owner actual Prefix permits original",()=>{var next=ExtendedBoss(kind);Arm();bool result=true;Assert(ExtendedPrefix(kind,next,new(),entry.Component,move,entry.Ai,target,ref result)&&result&&actions.Enabled);});
   Check(name+" foreign brain actual Prefix permits original",()=>{ExtendedBoss(kind);Arm();NativeObject other=kind==4?new AltanBossBrain():new GoatBossBrain();bool result=true;Assert(CurrentExtendedPrefix(kind,other,ref result)&&result&&actions.Enabled);});
   Check(name+" nonplayer target actual Prefix permits original",()=>{var next=ExtendedBoss(kind);Arm();target.target=new();bool result=true;Assert(CurrentExtendedPrefix(kind,next,ref result)&&result&&actions.Enabled);});
   Check(name+" null current target actual Prefix permits original",()=>{var next=ExtendedBoss(kind);Arm();target.target=null;bool result=true;Assert(CurrentExtendedPrefix(kind,next,ref result)&&result&&actions.Enabled);});
   Check(name+" canonical component replacement actual Prefix retires",()=>{var next=ExtendedBoss(kind);Arm();boss.Components[entry.Component.GetType()]=kind==4?new AltanBossComponent():new GoatBossComponent();bool result=true;Assert(CurrentExtendedPrefix(kind,next,ref result)&&result&&!actions.Enabled);});
   Check(name+" scope replacement actual Prefix retires",()=>{var next=ExtendedBoss(kind);Arm();TrainingActions.Scope=TrainingActions.Scope with{Generation=7};bool result=true;Assert(CurrentExtendedPrefix(kind,next,ref result)&&result&&!actions.Enabled);});
   Check(name+" read failure actual Prefix permits original",()=>{var next=ExtendedBoss(kind);Arm();boss.ThrowRead=true;bool result=true;Assert(CurrentExtendedPrefix(kind,next,ref result)&&result&&!actions.Enabled);});
   Check(name+" third group loss actual Prefix retires",()=>{var next=ExtendedBoss(kind);Arm();ExtendedAvailable(false);bool result=true;Assert(CurrentExtendedPrefix(kind,next,ref result)&&result&&!actions.Enabled);});
   Check(name+" old group failures preserve third Prefix",()=>{var next=ExtendedBoss(kind);BossTargetActions.Available=BossTargetActions.AdditionalAttackAvailable=false;Arm();bool result=true;Assert(!CurrentExtendedPrefix(kind,next,ref result)&&result==(kind==4)&&actions.Enabled);});
   Check(name+" manual disable releases actual Prefix",()=>{var next=ExtendedBoss(kind);Arm();actions.SetEnabled(false);bool result=true;Assert(CurrentExtendedPrefix(kind,next,ref result)&&result&&move.magnitude==1);});
  }
  Check("Altan Prefix rejects foreign Boss argument",()=>{var next=ExtendedBoss(4);Arm();bool result=true;Assert(ExtendedPrefix(4,next,boss,new AltanBossComponent(),move,entry.Ai,target,ref result)&&result&&actions.Enabled);});
  Check("Altan Prefix rejects foreign movement argument",()=>{var next=ExtendedBoss(4);Arm();bool result=true;Assert(ExtendedPrefix(4,next,boss,entry.Component,new(),entry.Ai,target,ref result)&&result&&move.magnitude==1);});
  Check("Altan Prefix rejects null movement argument",()=>{var next=ExtendedBoss(4);Arm();bool result=true;Assert(ExtendedPrefix(4,next,boss,entry.Component,null,entry.Ai,target,ref result)&&result&&actions.Enabled);});
  Check("Altan Prefix rejects null Boss argument",()=>{var next=ExtendedBoss(4);Arm();bool result=true;Assert(ExtendedPrefix(4,next,boss,null,move,entry.Ai,target,ref result)&&result&&actions.Enabled);});
  Check("Berude Prefix rejects foreign AI argument",()=>{var next=ExtendedBoss(5);Arm();bool result=true;Assert(ExtendedPrefix(5,next,boss,entry.Component,move,new(),target,ref result)&&result&&actions.Enabled);});
  Check("Berude Prefix rejects foreign target component argument",()=>{var next=ExtendedBoss(5);Arm();bool result=true;Assert(ExtendedPrefix(5,next,boss,entry.Component,move,entry.Ai,new(){target=game.Player},ref result)&&result&&actions.Enabled);});
  Check("Berude Prefix rejects null AI argument",()=>{var next=ExtendedBoss(5);Arm();bool result=true;Assert(ExtendedPrefix(5,next,boss,entry.Component,move,null,target,ref result)&&result&&actions.Enabled);});
  Check("Berude Prefix rejects null target component argument",()=>{var next=ExtendedBoss(5);Arm();bool result=true;Assert(ExtendedPrefix(5,next,boss,entry.Component,move,entry.Ai,null,ref result)&&result&&actions.Enabled);});
 }
}
