using System.Reflection;
using RoguePrince.CheatMenu;
var type=typeof(GameSession).Assembly.GetType("RoguePrince.CheatMenu.AbilityLoadoutActions");
if(type==null){Console.WriteLine("FAIL: native ability loadout adapter is missing; no guarded skill apply/restore exists");return 1;}
var checks=0;
void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
object Call(object adapter,string method,params object[] args){try{return type.GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(adapter,method=="Apply"?new object[]{args.Cast<string>().ToArray()}:args);}catch(TargetInvocationException e){throw e.InnerException;}}
bool Active(object adapter)=>(bool)type.GetProperty("Active",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(adapter);
object New(GameSession game)=>Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{game},null);
ItemData Skill(string id,params string[] features)=>new(id,new[]{"SkillTreeItemFeature"}.Concat(features).ToArray());
var old=Skill("old","StatModifierFeature");var next=Skill("next","ActionBindingsOverrideFeature");var unsafeSkill=Skill("unsafe","OnPickupEffectFeature");
GameSession Setup(){var g=new GameSession();g.Catalog.AddRange(new[]{old,next,unsafeSkill});g.Unlocked.AddRange(new[]{old,next,unsafeSkill});g.Inventory.TryAddItem(new(old,1));g.Inventory.items.Add(new(new("weapon","WeaponFeature"),1));g.Inventory.Mutations=0;return g;}
var game=Setup();var a=New(game);var original=game.Inventory.items[0];var weapon=game.Inventory.items[1];
Call(a,"Apply",new[]{"next"});Check(Active(a),"successful apply retains restoration lease");Check(game.Inventory.items.Any(x=>x.source==next),"new skill added");Check(!game.Inventory.items.Contains(original),"old skill removed");Check(game.Inventory.items.Contains(weapon),"weapon retained");Call(a,"Restore",false);Check(game.Inventory.items.Contains(original),"restore reuses original native instance");Check(!Active(a),"restoration releases lease");
game=Setup();a=New(game);original=game.Inventory.items[0];game.Inventory.FailAddId="next";
try{Call(a,"Apply",new[]{"next"});throw new Exception("missing apply rejection");}catch(InvalidOperationException){}
Check(game.Inventory.items.Contains(original),"failed apply rolls original instance back");Check(!Active(a),"successful rollback leaves no pending lease");
game=Setup();a=New(game);game.Inventory.FailAddId="next";game.Inventory.FailRestoreId="old";
try{Call(a,"Apply",new[]{"next"});throw new Exception("missing recovery failure");}catch(AggregateException){}
Check(Active(a),"failed rollback retains restoration lease");Check(game.InventoryRecoveryBlocked,"failed rollback blocks inventory work");game.Inventory.FailRestoreId=null;Call(a,"Restore",false);Check(!game.InventoryRecoveryBlocked,"recovery clears block");
game=Setup();a=New(game);Call(a,"Apply",new[]{"next"});game.Generation++;game.PlayerPointer=(IntPtr)99;
var calls=game.Inventory.Mutations;try{Call(a,"Restore",false);throw new Exception("missing scope rejection");}catch(InvalidOperationException){}
Check(calls==game.Inventory.Mutations,"old lease never mutates new player");Check(Active(a),"wrong-player failure retains lease");
foreach(var ids in new[]{new[]{"unsafe"},new[]{"missing"},new[]{"next","next"}}){game=Setup();a=New(game);calls=game.Inventory.Mutations;try{Call(a,"Apply",ids);throw new Exception("missing invalid set rejection");}catch(InvalidOperationException){}Check(game.Inventory.Mutations==calls,"invalid set rejected before mutation");}
game=Setup();a=New(game);game.Unlocked.Remove(next);calls=game.Inventory.Mutations;try{Call(a,"Apply",new[]{"next"});throw new Exception("missing locked rejection");}catch(InvalidOperationException){}Check(calls==game.Inventory.Mutations,"locked skill rejected without progression writes");
game=Setup();a=New(game);Call(a,"Apply",Array.Empty<string>());Check(game.Inventory.items.All(x=>!x.source.HasComponent<SkillTreeItemFeature>()),"empty combination temporarily removes skills");Call(a,"Restore",false);Check(game.Inventory.items.Any(x=>x.source==old),"empty combination restores originals");
game=Setup();a=New(game);game.Inventory.FailAfterEffectsId="next";
try{Call(a,"Apply",new[]{"next"});throw new Exception("missing partial add rejection");}catch(InvalidOperationException){}
Check(game.Player.EffectCount(next)==0,"partial native pickup effects removed even if item never entered inventory");
Check(game.Player.EffectCount(old)==1,"partial native pickup rollback restores one original effect");
game=Setup();a=New(game);game.Inventory.FailAfterRemovalId="old";
try{Call(a,"Apply",new[]{"next"});throw new Exception("missing partial removal rejection");}catch(InvalidOperationException){}
Check(game.Player.EffectCount(old)==1,"partial native removal cannot double the old effect during recovery");
game=Setup();a=New(game);Call(a,"Apply",new[]{"next"});Check(game.InventoryRecoveryBlocked,"active trial reserves inventory mutations");
Call(a,"Tick",false,false);Check(!Active(a)&&game.Player.EffectCount(old)==1,"focus loss restores original skills and effects");
game=Setup();a=New(game);Call(a,"Apply",new[]{"next"});Call(a,"Tick",true,true);Check(!Active(a),"loading restores before retiring current scope");
game=Setup();a=New(game);Call(a,"Apply",new[]{"next"});game.Room++;Call(a,"Tick",true,false);Check(!Active(a),"room change restores the original owner");
game=Setup();a=New(game);Call(a,"Apply",new[]{"next"});game.Ready=false;Call(a,"Tick",true,false);Check(!Active(a),"death restores without requiring live player admission");
game=Setup();a=New(game);Call(a,"Apply",new[]{"next"});game.Player.destroyed=true;calls=game.Inventory.Mutations;Call(a,"Restore",true);Check(!Active(a)&&calls==game.Inventory.Mutations,"destroyed old player lease retires without native writes");
game=Setup();a=New(game);game.Inventory.items[0].components.Add(new object());calls=game.Inventory.Mutations;try{Call(a,"Apply",new[]{"next"});throw new Exception("missing component rejection");}catch(InvalidOperationException){}Check(calls==game.Inventory.Mutations,"unknown instance components rejected before mutation");
game=Setup();a=New(game);var statFeature=(StatModifierFeature)old.subAssets.Single(f=>f is StatModifierFeature);game.Player.Effects[statFeature.Pointer.ToInt64()]=2;calls=game.Inventory.Mutations;
try{Call(a,"Apply",new[]{"next"});throw new Exception("missing duplicated effect rejection");}catch(InvalidOperationException){}Check(calls==game.Inventory.Mutations,"duplicated native effect rejected before mutation");
game=Setup();a=New(game);var foreign=Skill("foreign","StatModifierFeature");var foreignStat=(StatModifierFeature)foreign.subAssets.Single(f=>f is StatModifierFeature);foreignStat.targetValue.Id=statFeature.targetValue.Id;game.Player.Effects[foreignStat.Pointer.ToInt64()]=1;calls=game.Inventory.Mutations;
try{Call(a,"Apply",new[]{"next"});throw new Exception("missing effect alias rejection");}catch(InvalidOperationException){}Check(calls==game.Inventory.Mutations,"native modifier ID alias rejected before mutation");
game=Setup();a=New(game);Call(a,"Apply",new[]{"next"});foreignStat.targetValue.Id="unrelated-effect";game.Player.Effects[foreignStat.Pointer.ToInt64()]=1;Call(a,"Restore",false);Check(game.Player.Effects[foreignStat.Pointer.ToInt64()]==1,"unrelated native effects survive restoration");
game=Setup();a=New(game);Call(a,"Apply",new[]{"next"});var outsider=new ItemInstanceData(foreign,1);game.Inventory.items.Add(outsider);calls=game.Inventory.Mutations;
try{Call(a,"Restore",false);throw new Exception("missing foreign skill conflict rejection");}catch(InvalidOperationException){}Check(calls==game.Inventory.Mutations&&Active(a),"foreign skill conflict retains recovery and does not remove others' skills");
game=Setup();a=New(game);game.Player.Effects[foreignStat.Pointer.ToInt64()]=1;
var originalOrder=game.Player.Stats.CurrentStats[new("FixtureStat")].modifiers.Select(x=>x.Item1.Pointer).ToArray();
Call(a,"Apply",new[]{"next"});Call(a,"Restore",false);
Check(game.Player.Stats.CurrentStats[new("FixtureStat")].modifiers.Select(x=>x.Item1.Pointer).SequenceEqual(originalOrder),"restoration preserves stat modifier order relative to untouched effects");
game=Setup();a=New(game);var ownAction=new ItemInstanceData(next,1);game.Inventory.TryAddItem(ownAction);var foreignAction=new ActionBindingsOverrideFeature();game.Player.Effects[foreignAction.Pointer.ToInt64()]=1;
var originalActions=game.Player.Actions.overrideBindings.overriders.Select(x=>x.Pointer).ToArray();Call(a,"Apply",Array.Empty<string>());Call(a,"Restore",false);
Check(game.Player.Actions.overrideBindings.overriders.Select(x=>x.Pointer).SequenceEqual(originalActions),"restoration preserves action override priority relative to untouched effects");
game=Setup();a=New(game);var secondStat=Skill("second-stat","StatModifierFeature");game.Catalog.Add(secondStat);game.Unlocked.Add(secondStat);game.Player.Effects[foreignStat.Pointer.ToInt64()]=1;
var mods=game.Player.Stats.CurrentStats[new("FixtureStat")].modifiers;var oldContext=mods.Single(x=>x.Item1.Pointer==statFeature.Pointer).Item2.Pointer;var foreignContext=mods.Single(x=>x.Item1.Pointer==foreignStat.Pointer).Item2.Pointer;
Call(a,"Apply",new[]{"second-stat"});Call(a,"Restore",false);
Check(mods.Select(x=>x.Item1.Pointer).SequenceEqual(new[]{statFeature.Pointer,foreignStat.Pointer}),"old/new skills sharing a stat restore original order");
Check(mods.Single(x=>x.Item1.Pointer==statFeature.Pointer).Item2.Pointer!=oldContext,"restoration retains fresh native callback context");
Check(mods.Single(x=>x.Item1.Pointer==foreignStat.Pointer).Item2.Pointer==foreignContext,"foreign modifier context is not reconstructed");
game=Setup();a=New(game);Call(a,"Apply",new[]{"next"});game.Player.Stats.CurrentStats[new("FixtureStat")].modifiers=new();calls=game.Inventory.Mutations;
try{Call(a,"Restore",false);throw new Exception("missing container replacement rejection");}catch(InvalidOperationException){}Check(calls==game.Inventory.Mutations&&Active(a),"replaced native effect container is rejected before mutation");
game=Setup();a=New(game);Call(a,"Apply",new[]{"next"});game.InventoryRecoveryBlocked=false;Check(game.InventoryRecoveryBlocked,"other inventory cleanup cannot release the ability reservation");Call(a,"Restore",false);Check(!game.InventoryRecoveryBlocked,"ability restoration releases only its own reservation");
game=Setup();a=New(game);Call(a,"Apply",new[]{"next"});game.InventoryRecoveryBlocked=true;Call(a,"Restore",false);Check(game.InventoryRecoveryBlocked,"ability restoration preserves another inventory recovery block");
game=Setup();a=New(game);game.Catalog.Add(secondStat);game.Unlocked.Add(secondStat);game.Inventory.RemoveItem(game.Inventory.items[0]);game.Player.Effects[foreignStat.Pointer.ToInt64()]=1;
Call(a,"Apply",new[]{"second-stat"});Call(a,"Restore",false);Check(game.Inventory.items.All(x=>!x.source.HasComponent<SkillTreeItemFeature>())&&game.Player.Effects.GetValueOrDefault(foreignStat.Pointer.ToInt64())==1,"empty original skill set restores without removing foreign stat effects");
game=Setup();a=New(game);var foreign2=new StatModifierFeature();game.Player.Effects[foreignStat.Pointer.ToInt64()]=1;game.Player.Effects[foreign2.Pointer.ToInt64()]=1;Call(a,"Apply",new[]{"next"});mods=game.Player.Stats.CurrentStats[new("FixtureStat")].modifiers;mods.Reverse();
try{Call(a,"Restore",false);throw new Exception("missing order conflict rejection");}catch(InvalidOperationException){}Check(Active(a)&&game.InventoryRecoveryBlocked,"foreign modifier reorder conflict retains recovery lease");
var f1=mods.Single(x=>x.Item1.Pointer==foreignStat.Pointer);mods.Remove(f1);mods.Insert(0,f1);Call(a,"Restore",false);Check(!Active(a)&&mods[0].Item1.Pointer==statFeature.Pointer,"recovery retries after external order conflict is resolved");
Console.WriteLine($"RESULT {checks} passed, 0 failed");return 0;

