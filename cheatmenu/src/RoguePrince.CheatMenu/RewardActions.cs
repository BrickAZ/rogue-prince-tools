// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using HarmonyLib;
using MotherBase.EntityFramework;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
using UObject=UnityEngine.Object;
namespace RoguePrince.CheatMenu;
internal sealed class RewardActions
{
    internal sealed class Owned
    {internal GameObject Host,PickupPrefab;internal ItemInstanceData Item;internal LootItemComponent Loot;internal TrainingScope Scope;internal bool Bound,Delivered;}
    internal static RewardActions Current;
    internal static bool Available;
    [ThreadStatic] internal static Owned Creating;
    private readonly GameSession game;
    private readonly MobSpawnActions mobs;
    private readonly List<Owned> owned=new();
    internal bool Active=>owned.Count>0;
    internal string Status {get;private set;}="";
    internal RewardActions(GameSession game,MobSpawnActions mobs){this.game=game;this.mobs=mobs;Current=this;}
    private GameObject PickupPrefab(ItemInstanceData item)
    {
        var overridePrefab=item.GetComponent<OverridePrefabLootItemComponent>()?.lootPrefab;
        if(overridePrefab!=null)return overridePrefab;
        var specific=item.source.GetComponent<LootOnDropFeature>()?.lootPrefab;if(specific!=null)return specific;
        var candidates=new Dictionary<IntPtr,GameObject>();
        foreach(var generator in Resources.FindObjectsOfTypeAll<LootGenerator>())if(generator?.mainCharacterVariable?.CurrentValue?.Pointer==game.PlayerPointer&&generator.baseItemPickupPrefab!=null)candidates[generator.baseItemPickupPrefab.Pointer]=generator.baseItemPickupPrefab;
        if(candidates.Count!=1)throw new InvalidOperationException(MenuText.Get("无法唯一确定当前角色的拾取物预制体"));return candidates.Values.Single();
    }
    internal string Spawn(ItemInstanceData item)
    {
        game.RequireReady();var scope=TrainingActions.ReadScope(game);
        if(!Available||!scope.Valid||mobs.Busy||!Application.isFocused||owned.Count>=8||item?.source==null)throw new InvalidOperationException(MenuText.Get("奖励容器暂不可用，最多保留 8 个"));
        var prefabs=new Dictionary<IntPtr,GameObject>();
        foreach(var debug in Resources.FindObjectsOfTypeAll<DebugTriggerItemMenu>())if(debug?.player?.CurrentValue?.Pointer==game.PlayerPointer&&debug.freeChestPrefab!=null)prefabs[debug.freeChestPrefab.Pointer]=debug.freeChestPrefab;
        if(prefabs.Count!=1)throw new InvalidOperationException(MenuText.Get("当前免费宝箱预制体未唯一加载"));
        var prefab=prefabs.Values.Single();
        if(prefab.GetComponentsInChildren<ChestLockComponent>(true).Length>0||prefab.GetComponentsInChildren<PalaceLootItemComponent>(true).Length>0)throw new InvalidOperationException(MenuText.Get("该宝箱包含钥匙或宫殿进度限制，未生成"));
        var position=mobs.RewardPosition();var pickup=PickupPrefab(item);
        if(pickup.GetComponent<PickupItemComponent>()==null)throw new InvalidOperationException(MenuText.Get("奖励拾取物缺少原生物品组件"));
        var record=new Owned{Host=new GameObject("RPCheat-RewardContainer"),Item=item,PickupPrefab=pickup,Scope=scope};record.Host.SetActive(false);
        var previous=Creating;Creating=record;
        try
        {
            var clone=UObject.Instantiate(prefab,record.Host.transform);clone.transform.position=position;
            var components=clone.GetComponentsInChildren<LootItemComponent>(true);
            if(components.Length!=1)throw new InvalidOperationException(MenuText.Get("免费宝箱内容组件不唯一，已撤销"));
            record.Loot=components[0];Bind(record,record.Loot);
            owned.Add(record);record.Host.SetActive(true);
            if(!scope.Matches(TrainingActions.ReadScope(game))||record.Loot.GetCountContent()!=1||record.Loot._toLoot[0].Pointer!=item.Pointer)throw new InvalidOperationException(MenuText.Get("奖励容器内容回读失败，已撤销"));
            Plugin.LogEvent($"reward_container_ready item={item.source.name} instance={item.Pointer} loot={record.Loot.Pointer} position={position}");
            return Status=MenuText.Get("指定奖励宝箱已生成，请使用原版交互开箱");
        }
        catch{owned.Remove(record);UObject.Destroy(record.Host);throw;}
        finally{Creating=previous;}
    }
    private static void Bind(Owned record,LootItemComponent loot)
    {
        if(record.Loot!=null&&record.Loot.Pointer!=loot.Pointer)throw new InvalidOperationException("Unexpected reward component");
        record.Loot=loot;loot.unlockItemOnSpawn=false;loot.unlockOnlyBlueprint=false;
        loot._toLoot=new Il2CppSystem.Collections.Generic.List<ItemInstanceData>();loot._toLoot.Add(record.Item);
        loot._prefabToSpawn=new Il2CppSystem.Collections.Generic.List<GameObject>();loot._prefabToSpawn.Add(record.PickupPrefab);record.Bound=true;
    }
    internal static bool Define(LootItemComponent loot)
    {
        var current=Current;if(current==null)return true;
        var record=current.owned.FirstOrDefault(o=>o.Loot?.Pointer==loot.Pointer);
        if(record==null&&Creating?.Host!=null&&loot.transform.IsChildOf(Creating.Host.transform))record=Creating;
        if(record==null)return true;
        if(!record.Scope.Matches(TrainingActions.ReadScope(current.game))){UObject.Destroy(record.Host);return false;}
        // Skip native random reward tracking and permanent unlock side effects for this exact container.
        try{Bind(record,loot);}catch(Exception e){UObject.Destroy(record.Host);current.owned.Remove(record);Plugin.MenuLog.LogWarning("[RPCheat] owned_reward_bind_failed "+e);}return false;
    }
    internal static void Delivered(LootItemComponent loot,GameObject pickup,int index)
    {
        var current=Current;var record=current?.owned.FirstOrDefault(o=>o.Loot?.Pointer==loot.Pointer);
        if(record==null||index!=0||!record.Scope.Matches(TrainingActions.ReadScope(current.game)))return;
        var actual=pickup?.GetComponent<PickupItemComponent>()?.ItemToPickup;
        if(actual?.Pointer!=record.Item.Pointer)throw new InvalidOperationException("Reward pickup identity mismatch");
        record.Delivered=true;current.Status=MenuText.Get("宝箱已生成指定拾取物，等待玩家拾取");Plugin.LogEvent("reward_pickup_created exact_instance=true");
    }
    internal void Tick()
    {
        var scope=TrainingActions.ReadScope(game);
        for(int i=owned.Count-1;i>=0;i--){var r=owned[i];if(r.Host==null){owned.RemoveAt(i);continue;}if(!r.Scope.Matches(scope)){UObject.Destroy(r.Host);owned.RemoveAt(i);}}
    }
    internal string Clear(){foreach(var r in owned)if(r.Host!=null)UObject.Destroy(r.Host);owned.Clear();return Status=MenuText.Get("已清理本菜单的奖励容器");}
}
[HarmonyPatch(typeof(LootItemComponent),nameof(LootItemComponent.DefineContent))]
internal static class RewardDefinePatch
{private static bool Prefix(LootItemComponent __instance){try{return RewardActions.Define(__instance);}catch(Exception e){Plugin.MenuLog.LogWarning("[RPCheat] reward_define_failed "+e);return true;}}}
[HarmonyPatch(typeof(LootItemComponent),nameof(LootItemComponent.SetContent),new[]{typeof(GameObject),typeof(int),typeof(bool)})]
internal static class RewardDeliveryPatch
{private static void Postfix(LootItemComponent __instance,GameObject __0,int __1){try{RewardActions.Delivered(__instance,__0,__1);}catch(Exception e){Plugin.MenuLog.LogWarning("[RPCheat] reward_delivery_failed "+e);}}}
