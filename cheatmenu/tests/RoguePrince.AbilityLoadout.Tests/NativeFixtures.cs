namespace RoguePrince.CheatMenu;
internal class GameSession
{
 internal long Generation=1;internal int Room=1;internal IntPtr PlayerPointer=(IntPtr)10;private bool legacyBlock;internal bool AbilityInventoryReserved=false;internal bool InventoryRecoveryBlocked{get=>legacyBlock||AbilityInventoryReserved;set=>legacyBlock=value;}internal bool Ready=true;internal Player Player=new();internal InventoryComponent Inventory=>Player.Inv;
 internal List<ItemData> Catalog=new();internal List<ItemData> Unlocked=new();internal Saver Saver;
 internal GameSession(){Player.Inv.Owner=Player;Saver=new(this);RuntimeServiceLocator.Service=new(){playerVariable=new(this),unlockedItems=Unlocked};}
 internal void RequireReady(){if(!Ready)throw new InvalidOperationException("not ready");}
}
internal class Player
{
 internal bool destroyed=false;internal InventoryComponent Inv=new();internal IntPtr Pointer=10;
 internal readonly EffectDictionary Effects;internal readonly StatsStateComponent Stats;internal readonly MotherBase.ActionFramework.ActionBindingsReference Actions;
 internal Player(){Effects=new(this);Stats=new(this);Actions=new(this);}
 internal T GetComponent<T>() where T:class=>typeof(T)==typeof(StatsStateComponent)?Stats as T:Inv as T;
 internal T GetComponentData<T>() where T:class=>Actions as T;
 internal int EffectCount(ItemData d)=>d.subAssets.Where(f=>f.Name is "ActionBindingsOverrideFeature" or "StatModifierFeature").Sum(f=>Effects.GetValueOrDefault(f.Pointer.ToInt64()));
}
internal class Saver{internal EntityVariable mainCharacterVariable;internal ItemCollection allItems;internal Saver(GameSession g){mainCharacterVariable=new(g);allItems=new(g.Catalog);}}
internal class EntityVariable{readonly GameSession game;internal EntityVariable(GameSession g)=>game=g;internal Player CurrentValue=>game.Player;}
internal class ItemCollection{readonly List<ItemData> items;internal ItemCollection(List<ItemData> i)=>items=i;internal List<ItemData> GetItems()=>items;}
internal static class NativeItemSnapshot{internal static List<ItemData> Read(List<ItemData> i)=>i.ToList();}
internal class InventoryComponent
{
 internal Player Owner;internal IntPtr Pointer=20;internal List<ItemInstanceData> items=new();internal string FailAddId,FailRestoreId,FailAfterEffectsId,FailAfterRemovalId;internal int Mutations;
 internal bool HasItem(ItemInstanceData i)=>items.Contains(i);
 internal bool TryAddItem(ItemInstanceData i){Mutations++;if(i.source.Guid.guid==FailAddId||i.source.Guid.guid==FailRestoreId)throw new InvalidOperationException("native add failure");foreach(var f in i.source.subAssets.Where(f=>f.Name is "ActionBindingsOverrideFeature" or "StatModifierFeature"))Owner.Effects[f.Pointer.ToInt64()]=Owner.Effects.GetValueOrDefault(f.Pointer.ToInt64())+1;if(i.source.Guid.guid==FailAfterEffectsId){FailAfterEffectsId=null;throw new InvalidOperationException("partial native pickup");}items.Add(i);return true;}
 internal void RemoveItem(ItemInstanceData i){Mutations++;items.Remove(i);if(i.source.Guid.guid==FailAfterRemovalId){FailAfterRemovalId=null;throw new InvalidOperationException("partial native remove");}foreach(var f in i.source.subAssets)Owner.Effects.Remove(f.Pointer.ToInt64());}
}
internal class ItemData
{
 internal IntPtr Pointer;internal string name;internal SerializationGuid Guid;internal List<ItemFeature> subAssets;
 internal ItemData(string id,params string[] features){name=id;Guid=new(id);Pointer=(IntPtr)id.GetHashCode();subAssets=features.Select(s=>s=="StatModifierFeature"?new StatModifierFeature():s=="ActionBindingsOverrideFeature"?new ActionBindingsOverrideFeature():new ItemFeature(s)).ToList();}
 internal bool HasComponent<T>()=>subAssets.Any(x=>x.Name==typeof(T).Name);internal T GetComponent<T>() where T:class=>null;
}
internal class ItemFeature{internal static readonly Dictionary<long,ItemFeature> Registry=new();static int sequence=100;internal IntPtr Pointer=(IntPtr)(++sequence);internal string Name;internal ItemFeature(string n){Name=n;Registry[Pointer.ToInt64()]=this;}internal NativeType GetIl2CppType()=>new(Name);internal T Cast<T>() where T:class=>this as T;}
internal record NativeType(string FullName);
internal record SerializationGuid(string guid);
internal class ItemInstanceData{static int sequence=40;internal IntPtr Pointer=(IntPtr)(++sequence);internal List<object> components=new();internal ItemData source;internal int tier;internal ItemInstanceData(ItemData data,int value){source=data;tier=value;}}
internal class SkillTreeItemFeature{}internal class LoadItemFeature{internal ItemData LoadItemData()=>null;}
internal class ItemProgressionManagementService{internal EntityVariable playerVariable;internal List<ItemData> unlockedItems;internal bool IsUnlocked(ItemData i,bool unused)=>unlockedItems.Contains(i);}
internal static class RuntimeServiceLocator{internal static ItemProgressionManagementService Service;internal static T Get<T>() where T:class=>Service as T;}
internal readonly record struct FixtureScope(long Generation,IntPtr Player,int Room){internal bool Valid=>Player!=IntPtr.Zero;internal bool Matches(FixtureScope other)=>this==other;}
internal static class TrainingActions{internal static FixtureScope ReadScope(GameSession g)=>g.Ready?new(g.Generation,g.PlayerPointer,g.Room):default;}
internal static class Plugin{internal static void LogEvent(string text){}}
internal class ActionBindingsOverrideFeature:ItemFeature{internal ActionBindingsOverrideFeature():base("ActionBindingsOverrideFeature"){}internal void OnRemoved(Player p,InventoryComponent inv,ItemInstanceData i)=>p.Effects.Remove(Pointer.ToInt64());}
internal class StatModifierFeature:ItemFeature{internal NativeModifier targetValue;internal StatModifierFeature():base("StatModifierFeature"){targetValue=new(Pointer);}internal void OnRemoved(Player p,InventoryComponent inv,ItemInstanceData i)=>p.Effects.Remove(Pointer.ToInt64());}
internal class NativeModifier{internal IntPtr Pointer;internal string Id;internal NativeModifier(IntPtr p){Pointer=p;Id=p.ToString();}internal string GetId()=>Id;internal NativeType GetTargetStat()=>new("FixtureStat");}
internal class StatsStateComponent
{
 internal StatsStateComponent(Player p){}
 internal Dictionary<NativeType,StatObject> CurrentStats=new(){{new("FixtureStat"),new()}};
 internal class StatObject{internal Il2CppSystem.Collections.Generic.List<ModifierTuple> modifiers=new();}
}
internal class ModifierTuple{internal NativeModifier Item1;internal NativeContext Item2=new();internal ModifierTuple(NativeModifier m)=>Item1=m;}
internal class NativeContext{static int n=500;internal IntPtr Pointer=(IntPtr)(++n);}
internal class EffectDictionary:Dictionary<long,int>
{
 readonly Player player;internal EffectDictionary(Player p)=>player=p;
 public new int this[long key]{get=>base[key];set{Remove(key);base[key]=value;var f=ItemFeature.Registry[key];for(int i=0;i<value;i++){if(f is StatModifierFeature s)player.Stats.CurrentStats[new("FixtureStat")].modifiers.Add(new(s.targetValue));if(f is ActionBindingsOverrideFeature)player.Actions.overrideBindings.overriders.Add(f);}}}
 public new bool Remove(long key){var removed=base.Remove(key);player.Stats.CurrentStats[new("FixtureStat")].modifiers.RemoveAll(x=>x.Item1.Pointer.ToInt64()==key);player.Actions.overrideBindings.overriders.RemoveAll(x=>x.Pointer.ToInt64()==key);return removed;}
}
