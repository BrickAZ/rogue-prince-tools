// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using BepInEx;
using HarmonyLib;
using RoguePrince.CheatMenu.Core;
namespace RoguePrince.CheatMenu;
internal sealed class LoadoutActions
{
    internal static bool Available;
    [ThreadStatic] internal static HashSet<IntPtr> ManagedOwners;
    private readonly GameSession game;
    private readonly ItemBrowser items;
    private readonly WeaponAffixActions affixes;
    private readonly Action backup;
    private Action restore;
    private long generation;
    private IntPtr player,inventory;
    internal bool RecoveryPending=>restore!=null;
    internal readonly LoadoutLibrary Library=new(System.IO.Path.Combine(Paths.ConfigPath,"local.rogueprince.cheatmenu.loadouts.json"));
    internal LoadoutActions(GameSession game,ItemBrowser items,WeaponAffixActions affixes,Action backup){this.game=game;this.items=items;this.affixes=affixes;this.backup=backup;}
    private InventoryComponent Require()
    {game.RequireReady();if(!TrainingActions.ReadScope(game).Valid||affixes.RecoveryPending)throw new InvalidOperationException(MenuText.Get("配装作用域或词缀恢复未就绪"));return game.Player.GetComponent<InventoryComponent>()??throw new InvalidOperationException(MenuText.Get("角色背包尚未就绪"));}
    private static List<ItemInstanceData> Snapshot(InventoryComponent inv)
    {var list=inv.items??throw new InvalidOperationException("Inventory missing");var n=list.Count;var result=new List<ItemInstanceData>();for(int i=0;i<n;i++)result.Add(list[i]??throw new InvalidOperationException("Inventory contains null"));if(list.Count!=n)throw new InvalidOperationException("Inventory changed");return result;}
    private static WeaponPreset Weapon(ItemInstanceData item,string name)
    {var array=item.GetComponent<AffixHolderItemFeature.AffixHolderComponentData>()?.affixesGuids;return new(){Name=name,ItemId=item.source.Guid.guid,Tier=item.tier,AffixIds=array==null?new():Enumerable.Range(0,array.Length).Select(i=>array[i].guid).ToList()};}
    internal LoadoutPreset Capture(string name)
    {
        var inv=Require();if(RecoveryPending)throw new InvalidOperationException(MenuText.Get("配装恢复尚未完成"));
        if(!inv.TryGetMainWeapon(out var main)||!inv.TryGetSecondaryWeapon(out var tool)||main==null||tool==null)throw new InvalidOperationException(MenuText.Get("主武器和工具必须同时装备"));
        var p=new LoadoutPreset{Name=name,Main=Weapon(main,"main"),Tool=Weapon(tool,"tool"),Medallions=Snapshot(inv).Where(i=>i.source.HasComponent<MedallionItemFeature>()).Select(i=>new MedallionPreset{ItemId=i.source.Guid.guid,Tier=i.tier}).ToList()};p.Validate();return p;
    }
    internal string Apply(LoadoutPreset preset)
    {
        var inv=Require();if(!Available||RecoveryPending)throw new InvalidOperationException(MenuText.Get("配装补丁或恢复尚未就绪"));preset.Validate();
        if(items.Items.Count==0)items.Reload();
        var main=affixes.PrepareInstance(preset.Main);var tool=affixes.PrepareInstance(preset.Tool);
        if(main.Owner.source.HasComponent<UtilityWeaponFeature>()||!tool.Owner.source.HasComponent<UtilityWeaponFeature>())throw new InvalidOperationException(MenuText.Get("主武器与工具类别不匹配"));
        var medals=preset.Medallions.Select(m=>{var data=items.Items.SingleOrDefault(i=>i.Data.Guid.guid==m.ItemId)?.Data;if(data?.HasComponent<MedallionItemFeature>()!=true)throw new InvalidOperationException(MenuText.Get("预设徽章已失效"));return new ItemInstanceData(data,m.Tier);}).ToArray();
        var service=RuntimeServiceLocator.Get<ItemProgressionManagementService>();if(service?.playerVariable?.CurrentValue?.Pointer!=game.PlayerPointer||medals.Length>service.GetMaxSlotsMedallions())throw new InvalidOperationException(MenuText.Get("预设徽章超过当前栏位数"));
        if(!inv.TryGetMainWeapon(out var oldMain)||!inv.TryGetSecondaryWeapon(out var oldTool))throw new InvalidOperationException(MenuText.Get("装备状态变化，请重试"));
        var before=Snapshot(inv);var oldParents=before.Where(i=>i.Pointer==oldMain.Pointer||i.Pointer==oldTool.Pointer||i.source.HasComponent<MedallionItemFeature>()).ToArray();
        var oldItems=before.Where(i=>oldParents.Any(p=>p.Pointer==i.Pointer||WeaponAffixOwnership.IsOwnedBy(p,i))).ToArray();
        var untouched=before.Where(i=>!oldItems.Any(o=>o.Pointer==i.Pointer)).ToArray();
        var parents=new[]{main.Owner,tool.Owner}.Concat(medals).ToArray();var replacements=parents.Concat(main.Children).Concat(tool.Children).ToArray();
        var scope=TrainingActions.ReadScope(game);generation=game.Generation;player=game.PlayerPointer;inventory=inv.Pointer;
        var restoring=false;
        void Same(){var current=Require();if(current.Pointer!=inv.Pointer||game.Generation!=generation||game.PlayerPointer!=player||game.Saver?.mainCharacterVariable?.CurrentValue?.Pointer!=player||!restoring&&!scope.Matches(TrainingActions.ReadScope(game)))throw new InvalidOperationException(MenuText.Get("配装操作中角色或房间发生变化"));}
        void Guarded(Action work)
        {var previous=ManagedOwners;ManagedOwners=new(oldParents.Concat(parents).Select(i=>i.Pointer));try{work();}finally{ManagedOwners=previous;}}
        void Remove(IEnumerable<ItemInstanceData> set){foreach(var i in set.Reverse()){Same();if(inv.HasItem(i))inv.RemoveItem(i);}}
        void Add(IEnumerable<ItemInstanceData> set){foreach(var i in set){Same();if(!inv.HasItem(i)&&!inv.TryAddItem(i))throw new InvalidOperationException(MenuText.Get("原生装备添加失败"));}}
        void Verify(ItemInstanceData expectedMain,ItemInstanceData expectedTool,IEnumerable<ItemInstanceData> expected)
        {Same();if(!inv.TryGetMainWeapon(out var a)||a.Pointer!=expectedMain.Pointer||!inv.TryGetSecondaryWeapon(out var b)||b.Pointer!=expectedTool.Pointer||!expected.All(i=>inv.HasItem(i))||!untouched.All(i=>inv.HasItem(i)))throw new InvalidOperationException(MenuText.Get("配装实例回读失败"));}
        void Rollback()
        {
            restoring=true;
            try
            {
                Same();
                if(inv.TryGetMainWeapon(out var currentMain)&&currentMain!=null&&currentMain.Pointer!=oldMain.Pointer&&currentMain.Pointer!=main.Owner.Pointer||inv.TryGetSecondaryWeapon(out var currentTool)&&currentTool!=null&&currentTool.Pointer!=oldTool.Pointer&&currentTool.Pointer!=tool.Owner.Pointer)
                    throw new InvalidOperationException(MenuText.Get("装备已被其他操作替换，请先移开冲突装备再恢复"));
                // Reattach all original parents: an item can remain in inventory while its equipment slot changed.
                Guarded(()=>{Remove(replacements);Remove(oldItems);Add(oldParents);Add(oldItems.Where(i=>!oldParents.Any(p=>p.Pointer==i.Pointer)));Verify(oldMain,oldTool,oldItems);});
            }
            finally{restoring=false;}
        }
        backup();Same();Plugin.LogEvent("loadout_begin name="+preset.Name);
        try{Guarded(()=>{Remove(oldItems);Add(parents);Add(main.Children);Add(tool.Children);Verify(main.Owner,tool.Owner,replacements);});}
        catch(Exception failed)
        {try{Rollback();Plugin.LogEvent("loadout_rolled_back");}catch(Exception recovery){restore=Rollback;game.InventoryRecoveryBlocked=true;Plugin.LogEvent($"loadout_restore_pending player={player} inventory={inventory} original={string.Join(",",oldParents.Select(i=>i.source.name))}");throw new AggregateException(MenuText.Get("配装失败且恢复未完成，请重试恢复"),failed,recovery);}throw;}
        Plugin.LogEvent("loadout_applied name="+preset.Name+" medals="+medals.Length);return MenuText.Get("完整配装已应用");
    }
    internal string Recover()
    {if(restore==null)return MenuText.Get("没有待恢复配装");if(game.Generation!=generation||game.PlayerPointer!=player||Require().Pointer!=inventory)throw new InvalidOperationException(MenuText.Get("不能向新角色恢复旧配装"));restore();restore=null;game.InventoryRecoveryBlocked=false;return MenuText.Get("原配装已恢复");}
    internal void Retire(){if(restore!=null)Plugin.LogEvent("loadout_recovery_retired player="+player+" inventory="+inventory);restore=null;game.InventoryRecoveryBlocked=false;}
}
[HarmonyPatch(typeof(AffixHolderItemFeature),nameof(AffixHolderItemFeature.OnPickup))]
internal static class LoadoutAffixPickupPatch
{private static bool Prefix(ItemInstanceData __2)=>LoadoutActions.ManagedOwners?.Contains(__2.Pointer)!=true;}
[HarmonyPatch(typeof(AffixHolderItemFeature),nameof(AffixHolderItemFeature.OnRemoved))]
internal static class LoadoutAffixRemovePatch
{private static bool Prefix(ItemInstanceData __2)=>LoadoutActions.ManagedOwners?.Contains(__2.Pointer)!=true;}
