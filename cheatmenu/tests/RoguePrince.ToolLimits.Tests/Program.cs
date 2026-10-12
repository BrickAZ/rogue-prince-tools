using RoguePrince.CheatMenu;
using RoguePrince.CheatMenu.Core;
using MotherBase.EntityFramework;
using UnityEngine;
internal static class Program
{
 static int passed,failed;static CombatActions c;static InventoryComponent inv;static ProjectilePickerComponent picker;static ToolLimitActions tools;
 static void Assert(bool v){if(!v)throw new Exception("Assertion failed");}
 static void Reject(Action f){try{f();}catch(InvalidOperationException){return;}throw new Exception("Expected rejection");}
 static InvalidOperationException Rejection(Action f){try{f();}catch(InvalidOperationException e){return e;}throw new Exception("Expected rejection");}
 static void Reset(){Application.isFocused=true;TrainingActions.Scope=new(1,1,3,4,0,0);ToolLimitActions.Available=true;c=new();CombatActions.Current=c;inv=new();picker=new();inv.Tool.source.Ammo=new();c.Game.Player.Components[typeof(InventoryComponent)]=inv;c.Game.Player.Components[typeof(ProjectilePickerComponent)]=picker;tools=new(c);c.Tools=tools;}
 static void Check(string name,Action f){Reset();try{f();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
 static bool Suppress()=>tools.SuppressUse(inv.Tool.Uses,c.Game.Player,inv,inv.Tool);
 static void Main()
 {
  Check("disabled use lock does not read native data",()=>{c.Game.Player.ThrowRead=true;Assert(!Suppress()&&!tools.UsesLocked&&c.Game.Player.Reads==0);});
  Check("enable preserves actual remaining count",()=>{tools.SetUsesLock(true);Assert(tools.UsesLocked&&Suppress()&&inv.Tool.Uses.remainingUses==4);});
  Check("native zero use is still valid and locked",()=>{inv.Tool.Uses.remainingUses=0;tools.SetUsesLock(true);Assert(Suppress()&&inv.Tool.Uses.remainingUses==0);});
  Check("negative exhausted uses never revived",()=>{inv.Tool.Uses.remainingUses=-1;Reject(()=>tools.SetUsesLock(true));Assert(!tools.UsesLocked&&inv.Tool.Uses.remainingUses==-1);});
  Check("missing remaining uses rejects without mutation",()=>{inv.Tool.Uses=null;Reject(()=>tools.SetUsesLock(true));Assert(!tools.UsesLocked);});
  Check("ordinary non utility items excluded",()=>{inv.Tool.source.Utility=false;Reject(()=>tools.SetUsesLock(true));});
  Check("no equipped tool rejects",()=>{inv.HasTool=false;Reject(()=>tools.SetUsesLock(true));});
  Check("missing patch rejects",()=>{ToolLimitActions.Available=false;Reject(()=>tools.SetUsesLock(true));});
  Check("not ready rejects",()=>{c.Game.Ready=false;Reject(()=>tools.SetUsesLock(true));});
  Check("unfocused rejects",()=>{Application.isFocused=false;Reject(()=>tools.SetUsesLock(true));});
  Check("foreign owner not suppressed and current lock kept",()=>{tools.SetUsesLock(true);Assert(!tools.SuppressUse(inv.Tool.Uses,new(){Pointer=(IntPtr)90},inv,inv.Tool)&&tools.UsesLocked);});
  Check("foreign inventory not suppressed",()=>{tools.SetUsesLock(true);Assert(!tools.SuppressUse(inv.Tool.Uses,c.Game.Player,new(),inv.Tool));});
  Check("foreign item not suppressed",()=>{tools.SetUsesLock(true);Assert(!tools.SuppressUse(inv.Tool.Uses,c.Game.Player,inv,new()));});
  Check("foreign component not suppressed",()=>{tools.SetUsesLock(true);Assert(!tools.SuppressUse(new(),c.Game.Player,inv,inv.Tool));});
  Check("weapon change retires lock",()=>{tools.SetUsesLock(true);inv.Tool=new(){Uses=new()};tools.Tick();Assert(!tools.UsesLocked&&!Suppress());});
  Check("component replacement retires lock",()=>{tools.SetUsesLock(true);inv.Tool.Uses=new();Assert(!Suppress()&&!tools.UsesLocked);});
  Check("asset replacement retires lock",()=>{tools.SetUsesLock(true);inv.Tool.source=new();tools.Tick();Assert(!tools.UsesLocked);});
  Check("generation change retires lock",()=>{tools.SetUsesLock(true);TrainingActions.Scope=TrainingActions.Scope with{Generation=2};Assert(!Suppress()&&!tools.UsesLocked);});
  Check("room change retires lock",()=>{tools.SetUsesLock(true);TrainingActions.Scope=TrainingActions.Scope with{Room=10};tools.Tick();Assert(!tools.UsesLocked);});
  Check("scene change retires lock",()=>{tools.SetUsesLock(true);TrainingActions.Scope=TrainingActions.Scope with{Scene=9};tools.Tick();Assert(!tools.UsesLocked);});
  Check("focus loss retires lock",()=>{tools.SetUsesLock(true);Application.isFocused=false;tools.Tick();Assert(!tools.UsesLocked);});
  Check("loading retires lock",()=>{tools.SetUsesLock(true);c.Game.Ready=false;tools.Tick();Assert(!tools.UsesLocked);});
  Check("external count exhaustion retires lock",()=>{tools.SetUsesLock(true);inv.Tool.Uses.remainingUses=-1;Assert(!Suppress()&&!tools.UsesLocked);});
  Check("manual off works while not ready",()=>{tools.SetUsesLock(true);c.Game.Ready=false;c.Game.Player.ThrowRead=true;tools.SetUsesLock(false);Assert(!tools.UsesLocked);});
  Check("recall restores native quota and clears current projectiles",()=>{var status=tools.RecallProjectiles();Assert(picker.Calls==1&&picker.remainingProjectileCount==5&&picker.projectiles.Count==0&&!string.IsNullOrEmpty(status));});
  Check("empty recall does not call native reset",()=>{picker.projectiles.Clear();picker.remainingProjectileCount=5;Assert(!string.IsNullOrEmpty(tools.RecallProjectiles())&&picker.Calls==0);});
  Check("missing ammo feature recall rejects",()=>{inv.Tool.source.Ammo=null;Reject(()=>tools.RecallProjectiles());Assert(picker.Calls==0);});
  Check("missing picker recall rejects",()=>{c.Game.Player.Components.Remove(typeof(ProjectilePickerComponent));Reject(()=>tools.RecallProjectiles());Assert(picker.Calls==0);});
  Check("inconsistent ammo quota rejects without write",()=>{picker.remainingProjectileCount=4;Reject(()=>tools.RecallProjectiles());Assert(picker.Calls==0&&picker.projectiles.Count==2);});
  Check("negative ammo quota rejects",()=>{picker.remainingProjectileCount=-1;Reject(()=>tools.RecallProjectiles());Assert(picker.Calls==0);});
  Check("oversized quota rejects",()=>{inv.Tool.source.Ammo.maxAmmoCount=10000;Reject(()=>tools.RecallProjectiles());Assert(picker.Calls==0);});
  Check("recall while loading rejects",()=>{c.Game.Ready=false;Reject(()=>tools.RecallProjectiles());Assert(picker.Calls==0);});
  Check("recall while unfocused rejects",()=>{Application.isFocused=false;Reject(()=>tools.RecallProjectiles());Assert(picker.Calls==0);});
  Check("native recall exception no automatic retry",()=>{picker.ThrowReset=true;Reject(()=>tools.RecallProjectiles());Assert(picker.Calls==1&&c.OneHit&&c.Undying);});
  Check("recall readback failure not claimed successful",()=>{picker.FailReadback=true;Reject(()=>tools.RecallProjectiles());Assert(picker.Calls==1);});
  Check("scope changed during recall not claimed successful",()=>{picker.DuringReset=()=>TrainingActions.Scope=TrainingActions.Scope with{Room=99};Reject(()=>tools.RecallProjectiles());Assert(picker.Calls==1);});
  Check("real prefix suppresses only current tool decrement",()=>{tools.SetUsesLock(true);Assert(!ToolUsesPatch.Prefix(inv.Tool.Uses,c.Game.Player,inv,inv.Tool)&&inv.Tool.Uses.remainingUses==4);});
  Check("real prefix off permits native decrement",()=>{Assert(ToolUsesPatch.Prefix(inv.Tool.Uses,c.Game.Player,inv,inv.Tool));inv.Tool.Uses.OnUse(c.Game.Player,inv,inv.Tool);Assert(inv.Tool.Uses.remainingUses==3);});
  Check("real prefix foreign item runs original",()=>{tools.SetUsesLock(true);Assert(ToolUsesPatch.Prefix(new(),c.Game.Player,inv,new())&&tools.UsesLocked);});
  Check("real prefix read error disables only use lock",()=>{tools.SetUsesLock(true);c.Game.Player.ThrowRead=true;Assert(ToolUsesPatch.Prefix(inv.Tool.Uses,c.Game.Player,inv,inv.Tool)&&!tools.UsesLocked&&c.OneHit&&c.Undying);});
  Check("clear stops real prefix without native mutation",()=>{tools.SetUsesLock(true);Assert(!ToolUsesPatch.Prefix(inv.Tool.Uses,c.Game.Player,inv,inv.Tool));tools.Clear();Assert(!tools.UsesLocked&&ToolUsesPatch.Prefix(inv.Tool.Uses,c.Game.Player,inv,inv.Tool)&&inv.Tool.Uses.remainingUses==4);});
  Check("stale modes retire lock",()=>{tools.SetUsesLock(true);c.ModesBound=false;tools.Tick();Assert(!tools.UsesLocked);});
  Check("destroyed player retires lock",()=>{tools.SetUsesLock(true);c.Game.Player.destroyed=true;tools.Tick();Assert(!tools.UsesLocked);});
  Check("inactive player retires lock",()=>{tools.SetUsesLock(true);c.Game.Player.gameObject.activeInHierarchy=false;tools.Tick();Assert(!tools.UsesLocked);});
  Check("readonly description shows current tool without mutation",()=>{Assert(tools.Description().Contains("Tool")&&picker.Calls==0&&inv.Tool.Uses.remainingUses==4);});
  Check("unowned equipped instance rejects",()=>{inv.Contains=false;Reject(()=>tools.SetUsesLock(true));Reject(()=>tools.RecallProjectiles());Assert(picker.Calls==0);});
  Check("source asset changes before recall rejects without write",()=>{c.Game.Player.BeforeRead=()=>{if(c.Game.Player.Reads==3)inv.Tool.source=new(){Ammo=new()};};Reject(()=>tools.RecallProjectiles());Assert(picker.Calls==0);});
  Check("source asset changes during recall rejects confirmation",()=>{picker.DuringReset=()=>inv.Tool.source=new(){Ammo=new()};Reject(()=>tools.RecallProjectiles());Assert(picker.Calls==1);});
  Check("native recall exception reports requested but unconfirmed with cause",()=>{picker.ThrowReset=true;var e=Rejection(()=>tools.RecallProjectiles());Assert(picker.Calls==1&&e.Message==MenuText.Get("原生回收已请求但未确认；不会自动重试")&&e.InnerException?.Message=="native reset"&&c.OneHit&&c.Undying);});
  Check("loading after recall cannot report a preflight rejection",()=>{picker.DuringReset=()=>c.Game.Ready=false;var e=Rejection(()=>tools.RecallProjectiles());Assert(picker.Calls==1&&e.Message==MenuText.Get("原生回收已请求但未确认；不会自动重试")&&e.InnerException!=null);});
  Check("tool missing after recall cannot report a preflight rejection",()=>{picker.DuringReset=()=>inv.HasTool=false;var e=Rejection(()=>tools.RecallProjectiles());Assert(picker.Calls==1&&e.Message==MenuText.Get("原生回收已请求但未确认；不会自动重试")&&e.InnerException!=null);});
  Check("description read error retires only tool lock",()=>{tools.SetUsesLock(true);c.Game.Player.ThrowRead=true;Assert(tools.Description()=="native read"&&!tools.UsesLocked&&c.OneHit&&c.Undying&&inv.Tool.Uses.remainingUses==4&&picker.Calls==0);});
  Check("partial native recall is neither retried nor rolled back",()=>{picker.ThrowAfterPartialReset=true;var e=Rejection(()=>tools.RecallProjectiles());Assert(e.Message==MenuText.Get("原生回收已请求但未确认；不会自动重试")&&e.InnerException?.Message=="partial native reset"&&picker.Calls==1&&picker.remainingProjectileCount==4&&picker.projectiles.Count==1);});
  Console.WriteLine($"RESULT {passed} passed, {failed} failed");Environment.ExitCode=failed==0?0:1;
 }
}
