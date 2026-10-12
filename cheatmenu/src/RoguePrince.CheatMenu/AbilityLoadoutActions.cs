// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu;

// Runtime inventory effects only. Never calls the permanent skill-tree controller.
internal sealed class AbilityLoadoutActions
{
    private readonly GameSession game;
    private Action restore;
    private long generation;
    private IntPtr player;
    private Func<bool> sameScope;
    internal bool Active => restore != null;
    internal bool RecoveryPending { get; private set; }
    internal Func<bool> CanMutate = () => true;
    internal AbilityLoadoutActions(GameSession game) => this.game = game;

    // Exact native types, not inheritance: an unreviewed subclass could override callbacks.
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "SkillTreeItemFeature", "ActionBindingsOverrideFeature", "StatModifierFeature",
        "UnityTemplateProjects.Items.Features.EquipUnlockedItemFeature",
        "HideInEquipUIFeature", "UnspawnableItemFeature", "IsNotUpgradableFeature",
        "LoreDescriptionItemFeature", "ForbidRecycleItemFeature"
    };
    internal static bool Supported(ItemData data)
    {
        if (data == null || !data.HasComponent<SkillTreeItemFeature>()) return false;
        var features = data.subAssets;
        if (features == null || features.Count == 0) return false;
        var count = features.Count;
        for (var i = 0; i < count; i++)
            if (features[i] == null || !Allowed.Contains(features[i].GetIl2CppType().FullName)) return false;
        return features.Count == count;
    }
    private InventoryComponent Require()
    {
        game.RequireReady();
        if (!TrainingActions.ReadScope(game).Valid || game.Saver?.mainCharacterVariable?.CurrentValue?.Pointer != game.PlayerPointer)
            throw new InvalidOperationException(Core.MenuText.Get("技能试装作用域尚未就绪"));
        return game.Player.GetComponent<InventoryComponent>() ?? throw new InvalidOperationException(Core.MenuText.Get("角色背包尚未就绪"));
    }
    private static List<ItemInstanceData> Snapshot(InventoryComponent inventory)
    {
        var items = inventory.items ?? throw new InvalidOperationException("Inventory missing");
        var count = items.Count;
        var result = new List<ItemInstanceData>();
        for (var i = 0; i < count; i++) result.Add(items[i] ?? throw new InvalidOperationException("Null inventory item"));
        if (count != items.Count || result.Select(x => x.Pointer).Distinct().Count() != result.Count)
            throw new InvalidOperationException("Inventory changed or duplicated");
        return result;
    }
    internal List<ItemData> Catalog()
    {
        Require();
        var service = RuntimeServiceLocator.Get<ItemProgressionManagementService>();
        if (service?.playerVariable?.CurrentValue?.Pointer != game.PlayerPointer || game.Saver.allItems == null)
            throw new InvalidOperationException(Core.MenuText.Get("技能库尚未就绪"));
        var result = new List<ItemData>();
        foreach (var source in NativeItemSnapshot.Read(game.Saver.allItems.GetItems()))
        {
            var data = source;
            if (data?.HasComponent<LoadItemFeature>() == true) data = data.GetComponent<LoadItemFeature>().LoadItemData();
            if (Supported(data) && service.IsUnlocked(data, false)) result.Add(data);
        }
        return result.GroupBy(x => x.Guid.guid, StringComparer.Ordinal).Select(x => x.Single())
            .OrderBy(x => x.name, StringComparer.Ordinal).ToList();
    }
    internal string[] Capture()
    {
        var skills = Snapshot(Require()).Where(x => x.source.HasComponent<SkillTreeItemFeature>()).ToArray();
        if (skills.Any(x => !Supported(x.source)) || skills.Select(x => x.source.Guid.guid).Distinct().Count() != skills.Length)
            throw new InvalidOperationException(Core.MenuText.Get("当前技能包含未接入效果或重复实例，拒绝试装"));
        return skills.Select(x => x.source.Guid.guid).ToArray();
    }
    internal string Apply(string[] ids)
    {
        var inventory = Require();
        if (Active || game.InventoryRecoveryBlocked || !CanMutate())
            throw new InvalidOperationException(Core.MenuText.Get("先完成当前请求或恢复操作"));
        if (ids == null || ids.Length > 64 || ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
            throw new InvalidOperationException(Core.MenuText.Get("技能组合无效或包含重复项"));
        var catalog = Catalog();
        var replacements = ids.Select(id => new ItemInstanceData(catalog.SingleOrDefault(x => x.Guid.guid == id)
            ?? throw new InvalidOperationException(Core.MenuText.Get("组合含未解锁、未接入或失效技能")), 1)).ToArray();
        var originalIds = Capture();
        var before = Snapshot(inventory);
        var originals = before.Where(x => x.source.HasComponent<SkillTreeItemFeature>()).ToArray();
        var untouched = before.Except(originals).ToArray();
        var effectState = new AbilityEffectState(game, originals.Concat(replacements));
        var originalEffects = effectState.Expected(originals);
        var replacementEffects = effectState.Expected(replacements);
        effectState.Verify(originalEffects);
        var scope = TrainingActions.ReadScope(game);
        generation = game.Generation; player = game.PlayerPointer;
        sameScope = () => scope.Matches(TrainingActions.ReadScope(game));
        var owner = game.Player;
        void Same(bool applying)
        {
            if (game.Generation != generation || game.PlayerPointer != player || owner.destroyed || owner.GetComponent<InventoryComponent>()?.Pointer != inventory.Pointer
                || applying && (!scope.Matches(TrainingActions.ReadScope(game)) || !CanMutate()))
                throw new InvalidOperationException(Core.MenuText.Get("角色或技能试装作用域发生变化，保留恢复记录"));
            effectState.GuardContainers();
        }
        void Verify(IEnumerable<ItemInstanceData> expected)
        {
            var current = Snapshot(inventory);
            var expectedSkills = expected.Select(x => x.Pointer).OrderBy(x => x.ToInt64()).ToArray();
            var actualSkills = current.Where(x => x.source.HasComponent<SkillTreeItemFeature>()).Select(x => x.Pointer).OrderBy(x => x.ToInt64()).ToArray();
            if (!expectedSkills.SequenceEqual(actualSkills) || untouched.Any(x => !current.Any(y => y.Pointer == x.Pointer)))
                throw new InvalidOperationException(Core.MenuText.Get("技能实例回读失败，保留恢复记录"));
        }
        void Remove(IEnumerable<ItemInstanceData> set, bool applying)
        { foreach (var item in set.Reverse()) { Same(applying); if (inventory.HasItem(item)) inventory.RemoveItem(item); } }
        void Add(IEnumerable<ItemInstanceData> set, bool applying)
        { foreach (var item in set) { Same(applying); if (!inventory.HasItem(item) && !inventory.TryAddItem(item)) throw new InvalidOperationException(Core.MenuText.Get("原生技能装配失败")); } }
        void Rollback()
        {
            Same(false);
            var owned = originals.Concat(replacements).Select(x => x.Pointer).ToHashSet();
            if (Snapshot(inventory).Any(x => x.source.HasComponent<SkillTreeItemFeature>() && !owned.Contains(x.Pointer)))
                throw new InvalidOperationException(Core.MenuText.Get("技能已被其他操作替换，请先移开冲突技能再恢复"));
            Remove(replacements, false);
            // Re-run original callbacks after any partial native operation.
            Remove(originals, false); effectState.Clear(); Add(originals, false); Same(false); Verify(originals); effectState.Verify(originalEffects); effectState.RestoreOrder();
        }
        restore = Rollback;
        game.AbilityInventoryReserved = true;
        Plugin.LogEvent($"ability_begin generation={generation} player={player} original={string.Join(",", originalIds)} requested={string.Join(",", ids)}");
        try { Remove(originals, true); Add(replacements, true); Same(true); Verify(replacements); effectState.Verify(replacementEffects); }
        catch (Exception failed)
        {
            try { Restore(false); }
            catch (Exception recovery) { throw new AggregateException(Core.MenuText.Get("技能试装失败且恢复未完成，请重试恢复"), failed, recovery); }
            throw;
        }
        Plugin.LogEvent("ability_applied count=" + ids.Length);
        return Core.MenuText.Get("本局技能组合已试装；可恢复原实例");
    }
    internal string Restore(bool rebind = false)
    {
        if (restore == null) return Core.MenuText.Get("没有待恢复的技能组合");
        if (rebind && game.Generation == generation && game.PlayerPointer == player && game.Player.destroyed)
        {
            Plugin.LogEvent($"ability_retired_destroyed generation={generation} player={player}");
            restore = null; RecoveryPending = false; game.AbilityInventoryReserved = false;
            return Core.MenuText.Get("旧角色已销毁，技能记录已退役");
        }
        try { restore(); }
        catch { RecoveryPending = true; game.AbilityInventoryReserved = true; throw; }
        restore = null; RecoveryPending = false; game.AbilityInventoryReserved = false;
        Plugin.LogEvent("ability_restored original_instances=true");
        return Core.MenuText.Get("原技能实例已恢复");
    }
    internal void Tick(bool focused, bool loading)
    {
        if (Active && (!focused || loading || !game.Ready || sameScope?.Invoke() != true)) Restore(false);
    }
}
