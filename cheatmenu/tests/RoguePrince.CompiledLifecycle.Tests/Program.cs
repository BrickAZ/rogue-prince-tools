// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using System.Reflection;
using System.Runtime.CompilerServices;
if(args.Length!=2)throw new ArgumentException("Usage: compiled-plugin.dll initialized-game-directory");
var game=Path.GetFullPath(args[1]);
AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{
 var file=new AssemblyName(e.Name).Name+".dll";
 foreach(var dir in new[]{"core","interop"}){var path=Path.Combine(game,"BepInEx",dir,file);if(File.Exists(path))return Assembly.LoadFrom(path);}
 return null;
};
var assembly=Assembly.LoadFrom(Path.GetFullPath(args[0]));
Type Type(string name)=>assembly.GetType("RoguePrince.CheatMenu."+name,true);
const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
void Field(object o,string field,object value)=>o.GetType().GetField(field,flags).SetValue(o,value);
void Auto(object o,string property,object value)=>Field(o,"<"+property+">k__BackingField",value);
bool Enabled(object o)=>(bool)o.GetType().GetProperty("Enabled",flags).GetValue(o);
int passed=0,failed=0;
void Test(string name,Action body){try{body();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e);}}
var scene=RuntimeHelpers.GetUninitializedObject(Type("SceneActions"));
var boss=RuntimeHelpers.GetUninitializedObject(Type("BossTargetActions"));
Field(scene,"BossTargets",boss);
Type("SceneActions").GetField("Current",flags).SetValue(null,scene);
var core=Assembly.LoadFrom(Path.Combine(game,"BepInEx/core/BepInEx.Core.dll"));
var logger=Activator.CreateInstance(core.GetType("BepInEx.Logging.ManualLogSource"),"lifecycle-offline");
Type("Plugin").GetField("MenuLog",flags).SetValue(null,logger);
void Arm(){Auto(boss,"Enabled",true);Auto(scene,"NoTarget",true);Auto(scene,"NoArenaSpawn",true);}
Test("actual compiled ScenePatchGuard exception preserves independent Boss mode",()=>{
 Arm();var read=Type("ScenePatchGuard").GetMethod("Read",flags).MakeGenericMethod(typeof(bool));
 var result=(bool)read.Invoke(null,new object[]{(Func<bool>)(()=>throw new InvalidOperationException("old group read error")),false});
 if(result||(bool)Type("SceneActions").GetProperty("NoTarget",flags).GetValue(scene)||(bool)Type("SceneActions").GetProperty("NoArenaSpawn",flags).GetValue(scene)||!Enabled(boss))throw new Exception("Old group did not fail open independently");
});
Test("actual compiled shared Restore clears both groups without native objects",()=>{
 Arm();Type("SceneActions").GetMethod("Restore",flags).Invoke(scene,new object[]{true});
 if(Enabled(boss)||(bool)Type("SceneActions").GetProperty("NoTarget",flags).GetValue(scene)||(bool)Type("SceneActions").GetProperty("NoArenaSpawn",flags).GetValue(scene))throw new Exception("Shared cleanup left a group active");
});
var phasePractice=RuntimeHelpers.GetUninitializedObject(Type("BossPracticeActions"));
bool PracticeBusy()=>(bool)phasePractice.GetType().GetProperty("Busy",flags).GetValue(phasePractice);
void RestorePractice(bool rebind)=>phasePractice.GetType().GetMethod("Restore",flags).Invoke(phasePractice,new object[]{rebind});
Test("actual compiled expected loading rebind preserves practice",()=>{
 Auto(phasePractice,"Busy",true);Field(phasePractice,"pending",null);RestorePractice(true);
 if(!PracticeBusy())throw new Exception("Expected reload observation was cancelled");
});
Test("actual compiled manual recovery cancels pending practice",()=>{
 RestorePractice(false);if(PracticeBusy())throw new Exception("Manual cleanup retained automatic work");
});
Test("actual compiled post-bind rebind cancels practice",()=>{
 Auto(phasePractice,"Busy",true);Field(phasePractice,"pending",RuntimeHelpers.GetUninitializedObject(Type("BossPhaseActions+Entry")));RestorePractice(true);
 if(PracticeBusy())throw new Exception("New player could inherit bound phase automation");
});
var frame=RuntimeHelpers.GetUninitializedObject(Type("FrameObservationActions"));
Test("actual compiled inactive frame restore needs no native context",()=>{
 Type("FrameObservationActions").GetMethod("Restore",flags).Invoke(frame,null);
 if(Enabled(frame)||(bool)Type("FrameObservationActions").GetProperty("RecoveryPending",flags).GetValue(frame))throw new Exception("Inactive frame observer acquired a lease");
});
Test("actual compiled inactive frame Tick needs no native context",()=>{
 Type("FrameObservationActions").GetMethod("Tick",flags).Invoke(frame,new object[]{false});
 if(Enabled(frame))throw new Exception("Inactive Tick enabled frame observer");
});
Test("actual compiled disabled frame step rejects before native access",()=>{
 bool rejected=false;try{Type("FrameObservationActions").GetMethod("Step",flags).Invoke(frame,new object[]{(Action)(()=>throw new Exception("release must not run"))});}catch(TargetInvocationException e)when(e.InnerException is InvalidOperationException){rejected=true;}
 if(!rejected)throw new Exception("Disabled frame step was admitted");
});
var abilityGame=RuntimeHelpers.GetUninitializedObject(Type("GameSession"));
var ability=Activator.CreateInstance(Type("AbilityLoadoutActions"),flags,null,new[]{abilityGame},null);
bool AbilityActive()=>(bool)Type("AbilityLoadoutActions").GetProperty("Active",flags).GetValue(ability);
Test("actual compiled inactive ability restore needs no native objects",()=>{Type("AbilityLoadoutActions").GetMethod("Restore",flags).Invoke(ability,new object[]{false});if(AbilityActive())throw new Exception("inactive restore acquired lease");});
Test("actual compiled inactive ability tick needs no native objects",()=>{Type("AbilityLoadoutActions").GetMethod("Tick",flags).Invoke(ability,new object[]{false,true});if(AbilityActive())throw new Exception("inactive tick acquired lease");});
Test("actual compiled successful ability recovery releases its lease",()=>{var calls=0;Field(ability,"restore",(Action)(()=>calls++));Field(abilityGame,"AbilityInventoryReserved",true);Type("AbilityLoadoutActions").GetMethod("Restore",flags).Invoke(ability,new object[]{false});if(calls!=1||AbilityActive()||(bool)Type("GameSession").GetProperty("InventoryRecoveryBlocked",flags).GetValue(abilityGame))throw new Exception("successful restore retained lease/block");});
Test("actual compiled failed ability recovery keeps lease and inventory block",()=>{Field(ability,"restore",(Action)(()=>throw new InvalidOperationException("partial cleanup")));try{Type("AbilityLoadoutActions").GetMethod("Restore",flags).Invoke(ability,new object[]{false});throw new Exception("restore error swallowed");}catch(TargetInvocationException e)when(e.InnerException is InvalidOperationException){}if(!AbilityActive()||!(bool)Type("GameSession").GetProperty("InventoryRecoveryBlocked",flags).GetValue(abilityGame)||!(bool)Type("AbilityLoadoutActions").GetProperty("RecoveryPending",flags).GetValue(ability))throw new Exception("failed restore lost lease/block");});
Test("actual compiled loadout retirement cannot release live ability reservation",()=>{Field(abilityGame,"AbilityInventoryReserved",true);Type("GameSession").GetProperty("InventoryRecoveryBlocked",flags).SetValue(abilityGame,true);var loadout=RuntimeHelpers.GetUninitializedObject(Type("LoadoutActions"));Field(loadout,"game",abilityGame);Type("LoadoutActions").GetMethod("Retire",flags).Invoke(loadout,null);if(!(bool)Type("GameSession").GetProperty("InventoryRecoveryBlocked",flags).GetValue(abilityGame))throw new Exception("loadout retirement cleared ability reservation");});
Test("actual compiled ability restoration preserves independent loadout block",()=>{Type("GameSession").GetProperty("InventoryRecoveryBlocked",flags).SetValue(abilityGame,true);Field(ability,"restore",(Action)(()=>{}));Type("AbilityLoadoutActions").GetMethod("Restore",flags).Invoke(ability,new object[]{false});if(!(bool)Type("GameSession").GetProperty("InventoryRecoveryBlocked",flags).GetValue(abilityGame)||(bool)Type("GameSession").GetField("AbilityInventoryReserved",flags).GetValue(abilityGame))throw new Exception("ability restoration affected another block");});
var checkpoint=RuntimeHelpers.GetUninitializedObject(Type("PlayerCheckpointActions"));
Test("actual compiled inactive checkpoint observe needs no native objects",()=>{Type("PlayerCheckpointActions").GetMethod("Observe",flags).Invoke(checkpoint,new object[]{false});});
Test("actual compiled inactive checkpoint reset needs no native objects",()=>{Type("PlayerCheckpointActions").GetMethod("Reset",flags).Invoke(checkpoint,new object[]{true});});
Test("actual compiled missing checkpoint rejects before menu release or native access",()=>{bool rejected=false;try{Type("PlayerCheckpointActions").GetMethod("Restore",flags).Invoke(checkpoint,new object[]{(Action)(()=>throw new Exception("release must not run"))});}catch(TargetInvocationException e)when(e.InnerException is InvalidOperationException){rejected=true;}if(!rejected)throw new Exception("Missing checkpoint admitted");});
Console.WriteLine($"RESULT {passed} passed, {failed} failed");Environment.ExitCode=failed==0?0:1;



