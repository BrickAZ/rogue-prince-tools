// Test-only native inventory models for the linked production loadout transaction.
using RoguePrince.CheatMenu.Core;
using MotherBase.EntityFramework;
public sealed class SerializationGuid {public string guid;}
public sealed class ItemData
{
    public string name;
    public object title=new();
    public SerializationGuid Guid=new();
    public readonly HashSet<Type> Features=new();
    public bool HasComponent<T>()=>Features.Contains(typeof(T));
}
public sealed class WeaponFeature{}
public sealed class UtilityWeaponFeature{}
public sealed class MedallionItemFeature{}
public sealed class AffixHolderItemFeature
{public void OnPickup(){}public void OnRemoved(){}public sealed class AffixHolderComponentData {public SerializationGuid[] affixesGuids=Array.Empty<SerializationGuid>();}}
internal sealed class EntityVariable {internal Entity CurrentValue;}
internal sealed class PlayerSaver {internal EntityVariable mainCharacterVariable;}
internal sealed class ItemProgressionManagementService {internal EntityVariable playerVariable;internal int Capacity=4;internal int GetMaxSlotsMedallions()=>Capacity;}
internal static class RuntimeServiceLocator {internal static object Service;internal static T Get<T>()=> (T)Service;}
internal sealed class InventoryComponent
{
    internal IntPtr Pointer=new(99);
    internal readonly List<ItemInstanceData> items=new();
    internal ItemInstanceData Main,Tool;
    internal Func<ItemInstanceData,bool> FailAdd;
    internal int BroadCleanup;
    internal bool HasItem(ItemInstanceData i)=>items.Contains(i);
    internal bool TryGetMainWeapon(out ItemInstanceData i){i=Main;return i!=null;}
    internal bool TryGetSecondaryWeapon(out ItemInstanceData i){i=Tool;return i!=null;}
    internal bool TryAddItem(ItemInstanceData i)
    {
        if(FailAdd?.Invoke(i)==true)return false;
        items.Add(i);if(i.source.HasComponent<UtilityWeaponFeature>())Tool=i;else if(i.source.HasComponent<WeaponFeature>())Main=i;
        return true;
    }
    internal void RemoveItem(ItemInstanceData i)
    {
        if(i.source.HasComponent<WeaponFeature>()&&RoguePrince.CheatMenu.LoadoutActions.ManagedOwners?.Contains(i.Pointer)!=true)BroadCleanup++;
        items.Remove(i);if(Main==i)Main=null;if(Tool==i)Tool=null;
    }
}
namespace HarmonyLib { [AttributeUsage(AttributeTargets.Class,AllowMultiple=true)]internal sealed class HarmonyPatch:Attribute {internal HarmonyPatch(Type type,string method){}} }
namespace BepInEx {internal static class Paths {internal static string ConfigPath=>Path.GetTempPath();}}
namespace RoguePrince.CheatMenu
{
    internal sealed class WeaponAffixActions
    {
        internal bool RecoveryPending=false;
        internal readonly ItemBrowser Browser;
        internal WeaponAffixActions(ItemBrowser browser)=>Browser=browser;
        internal (ItemInstanceData Owner,ItemInstanceData[] Children) PrepareInstance(WeaponPreset preset)
        {var source=Browser.Items.Single(e=>e.Data.Guid.guid==preset.ItemId).Data;var owner=new ItemInstanceData(source,preset.Tier);owner.Holder.affixesGuids=preset.AffixIds.Select(id=>new SerializationGuid{guid=id}).ToArray();var children=preset.AffixIds.Select(id=>new ItemInstanceData(new(){Guid=new(){guid=id},name=id},1){Affix=new(){targetItem=owner}}).ToArray();return(owner,children);}
    }
}
internal static class LoadoutBoundaryTests
{
    internal static void Run(Action<string,Action> check)
    {
        (RoguePrince.CheatMenu.GameSession,RoguePrince.CheatMenu.LoadoutActions,InventoryComponent,LoadoutPreset,ItemInstanceData[]) Setup()
        {
            var game=new RoguePrince.CheatMenu.GameSession();var inv=new InventoryComponent();game.Player.Inventory=inv;
            RuntimeServiceLocator.Service=new ItemProgressionManagementService{playerVariable=new(){CurrentValue=game.Player}};
            var browser=new RoguePrince.CheatMenu.ItemBrowser();
            ItemData Data(string id,params Type[] features){var d=new ItemData{name=id,Guid=new(){guid=id}};d.Features.UnionWith(features);browser.Items.Add(new(){Data=d});return d;}
            var main=new ItemInstanceData(Data("oldMain",typeof(WeaponFeature)),2);var tool=new ItemInstanceData(Data("oldTool",typeof(WeaponFeature),typeof(UtilityWeaponFeature)),2);var medal=new ItemInstanceData(Data("oldMedal",typeof(MedallionItemFeature)),2);
            var child=new ItemInstanceData(Data("sharedAffix"),1){Affix=new(){targetItem=main}};main.Holder.affixesGuids=new[]{child.source.Guid};
            var unrelated=new ItemInstanceData(Data("unrelated"),1);
            foreach(var i in new[]{main,tool,medal,child,unrelated})inv.TryAddItem(i);
            Data("newMain",typeof(WeaponFeature));Data("newTool",typeof(WeaponFeature),typeof(UtilityWeaponFeature));Data("newMedal",typeof(MedallionItemFeature));
            var p=new LoadoutPreset{Name="test",Main=new(){Name="main",ItemId="newMain",Tier=3,AffixIds=new(){"sharedAffix"}},Tool=new(){Name="tool",ItemId="newTool",Tier=4,AffixIds=new()},Medallions=new(){new(){ItemId="newMedal",Tier=2}}};
            RoguePrince.CheatMenu.LoadoutActions.Available=true;RoguePrince.CheatMenu.TrainingActions.Room=1;
            return(game,new(game,browser,new(browser),()=>{}),inv,p,new[]{main,tool,medal,child,unrelated});
        }
        check("native full loadout replaces only captured instances and exact affix owner",()=>{var(g,a,i,p,old)=Setup();a.Apply(p);if(i.items.Contains(old[0])||!i.items.Contains(old[4])||i.Main.tier!=3||i.Tool.tier!=4||i.BroadCleanup!=0)throw new Exception("ownership failure");var child=i.items.Single(x=>x.Affix!=null);if(child.Affix.targetItem!=i.Main)throw new Exception("wrong owner");});
        check("native loadout failure restores original parent and child instances",()=>{var(g,a,i,p,old)=Setup();var fail=true;i.FailAdd=item=>{if(item.source.Guid.guid=="newTool"&&fail){fail=false;return true;}return false;};try{a.Apply(p);throw new Exception("accepted");}catch(InvalidOperationException){}if(!old.All(i.HasItem)||i.items.Count!=old.Length||a.RecoveryPending||i.BroadCleanup!=0)throw new Exception("failed rollback");});
        check("native loadout recovery retries same inventory after room change",()=>{var(g,a,i,p,old)=Setup();i.FailAdd=item=>item.source.Guid.guid is "newTool" or "oldTool";try{a.Apply(p);throw new Exception("accepted");}catch(AggregateException){}if(!a.RecoveryPending||!g.InventoryRecoveryBlocked)throw new Exception("no recovery lock");RoguePrince.CheatMenu.TrainingActions.Room=2;i.FailAdd=null;a.Recover();if(!old.All(i.HasItem)||a.RecoveryPending||g.InventoryRecoveryBlocked)throw new Exception("failed retry");});
        check("native loadout rollback refuses replacing unrelated intervening equipment",()=>{var(g,a,i,p,old)=Setup();i.FailAdd=item=>item.source.Guid.guid is "newTool" or "oldTool";try{a.Apply(p);}catch(AggregateException){}var alien=new ItemInstanceData(new(){Guid=new(){guid="alien"},name="alien"},1);alien.source.Features.Add(typeof(WeaponFeature));i.FailAdd=null;i.TryAddItem(alien);try{a.Recover();throw new Exception("overwrote alien");}catch(InvalidOperationException){}if(i.Main!=alien||!a.RecoveryPending)throw new Exception("changed alien");i.RemoveItem(alien);a.Recover();if(!old.All(i.HasItem))throw new Exception("not restored");});
        check("native loadout recovery never writes onto a new player generation",()=>{var(g,a,i,p,old)=Setup();i.FailAdd=item=>item.source.Guid.guid is "newTool" or "oldTool";try{a.Apply(p);}catch(AggregateException){}g.Generation++;i.FailAdd=null;try{a.Recover();throw new Exception("accepted");}catch(InvalidOperationException){}a.Retire();if(g.InventoryRecoveryBlocked||a.RecoveryPending)throw new Exception("stale lock");});
        check("native loadout over-capacity preflight leaves original inventory",()=>{var(g,a,i,p,old)=Setup();((ItemProgressionManagementService)RuntimeServiceLocator.Service).Capacity=0;try{a.Apply(p);throw new Exception("accepted");}catch(InvalidOperationException){}if(!old.SequenceEqual(i.items))throw new Exception("changed before preflight");});
    }
}
