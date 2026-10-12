// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using RoguePrince.CheatMenu.Core;
using AffixData = AffixHolderItemFeature.AffixHolderComponentData;
using OwnerData = AffixItemFeature.AffixComponentData;

namespace RoguePrince.CheatMenu;

internal sealed class WeaponAffixActions
{
    internal sealed class Option
    {
        public ItemData Data;
        public LocalizedItemName Label;
        public string Id => Data.Guid.guid;
        public int Mask;
        public bool InPool, Excluded;
        public readonly HashSet<string> ExcludedAffixes = new(StringComparer.Ordinal);
    }
    private readonly GameSession game;
    private readonly ItemBrowser browser;
    private readonly Action backup;
    private ItemInstanceData target;
    private InventoryComponent inventory;
    private long generation;
    private IntPtr playerPointer;
    private int slot, targetMask, tier;
    private string[] expectedIds = Array.Empty<string>();
    private LocalizedItemName label;
    private readonly ScopedAffixRecovery recovery = new();
    public bool RecoveryPending => recovery.Pending;
    public bool HasTarget => target != null;
    public bool IsSpawn => slot == 2;
    public string Name => label?.Display ?? MenuText.Get("请选择武器");
    public string Current => string.Join("\n", expectedIds.Select(id => "• " + (Options.FirstOrDefault(e => e.Id == id)?.Label.Display ?? MenuText.Get("词缀文本未加载"))));
    public readonly List<Option> Options = new();
    public readonly List<Option> Draft = new();
    public WeaponAffixActions(GameSession game, ItemBrowser browser, Action backup) { this.game = game; this.browser = browser; this.backup = backup; }

    private static ItemData Resolve(ItemData data) => data?.HasComponent<LoadItemFeature>() == true ? data.GetComponent<LoadItemFeature>().LoadItemData() : data;
    private static HashSet<string> Expand(Il2CppReferenceArray<DynamicLoot> sources)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        if (sources == null) return ids;
        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] == null) continue;
            var items = sources[i].GetAllItems();
            if (items == null) throw new InvalidOperationException(MenuText.Get("词缀排斥表尚未加载"));
            for (int j = 0; j < items.Count; j++)
            {
                var data = Resolve(items[j]);
                if (data != null) ids.Add(data.Guid.guid);
            }
        }
        return ids;
    }
    private static string[] ReadIds(ItemInstanceData item)
    {
        var data = item.GetComponent<AffixData>() ?? throw new InvalidOperationException(MenuText.Get("该武器没有原生词缀实例"));
        var array = data.affixesGuids;
        return array == null ? Array.Empty<string>() : Enumerable.Range(0, array.Length).Select(i => array[i].guid).ToArray();
    }
    private static Il2CppReferenceArray<SerializationGuid> NativeIds(IEnumerable<Option> selected)
        => new(selected.Select(e => e.Data.Guid).ToArray());

    public string LoadEquipped(bool secondary)
    {
        game.RequireReady(); GuardRecovery();
        var inv = game.Player.GetComponent<InventoryComponent>();
        if (inv == null) throw new InvalidOperationException(MenuText.Get("角色背包尚未就绪"));
        ItemInstanceData item;
        var found = secondary ? inv.TryGetSecondaryWeapon(out item) : inv.TryGetMainWeapon(out item);
        if (!found || item == null) throw new InvalidOperationException(MenuText.Get("当前栏位没有武器"));
        SetTarget(item, secondary ? 1 : 0, inv);
        return MenuText.Format("已载入词缀：{0}", Name);
    }
    public string LoadSpawn(ItemBrowser.Entry entry, int itemTier)
    {
        game.RequireReady(); GuardRecovery();
        if (entry?.Data == null || itemTier < 1 || itemTier > 100) throw new InvalidOperationException(MenuText.Get("请先选择武器和有效等级"));
        SetTarget(new ItemInstanceData(entry.Data, itemTier), 2, null);
        return MenuText.Format("已载入词缀：{0}", Name);
    }
    public WeaponPreset CapturePreset(string name)
    {
        GuardRecovery(); ValidateTarget(); ValidateDraft();
        var preset = new WeaponPreset { Name=name, ItemId=target.source.Guid.guid, Tier=tier, AffixIds=Draft.Select(e=>e.Id).ToList() };
        preset.Validate(); return preset;
    }
    public string LoadPreset(WeaponPreset preset)
    {
        game.RequireReady(); GuardRecovery(); preset.Validate();
        if(browser.Items.Count==0) browser.Reload();
        var entry=browser.Items.SingleOrDefault(e=>e.Data?.Guid.guid==preset.ItemId)
            ?? throw new InvalidOperationException(MenuText.Get("预设物品在当前版本中不存在"));
        // Preflight on a private draft: no inventory, spawned loot or current draft is changed on failure.
        var candidate=new WeaponAffixActions(game,browser,backup);
        candidate.LoadSpawn(entry,preset.Tier); candidate.Draft.Clear();
        foreach(var id in preset.AffixIds)
        {
            var option=candidate.Options.SingleOrDefault(e=>e.Id==id);
            if(option==null || !candidate.CanAdd(option)) throw new InvalidOperationException(MenuText.Get("预设词缀失效或不兼容"));
            candidate.Draft.Add(option);
        }
        candidate.ValidateDraft();
        target=candidate.target; inventory=null; slot=2; generation=candidate.generation; playerPointer=candidate.playerPointer;
        tier=candidate.tier; targetMask=candidate.targetMask; expectedIds=candidate.expectedIds; label=candidate.label;
        Options.Clear(); Options.AddRange(candidate.Options); Draft.Clear(); Draft.AddRange(candidate.Draft);
        return MenuText.Format("预设已载入草稿：{0}",preset.Name);
    }
    private void SetTarget(ItemInstanceData item, int whichSlot, InventoryComponent inv)
    {
        var source = item.source;
        var holder = source?.GetComponent<AffixHolderItemFeature>();
        var mask = source?.HasComponent<UtilityWeaponFeature>() == true ? 2 : source?.HasComponent<WeaponFeature>() == true ? 1 : 0;
        if (mask == 0 || holder?.allowedAffixes == null || !item.HasComponent<AffixData>())
            throw new InvalidOperationException(MenuText.Get("该物品不支持武器词缀"));
        var ids = ReadIds(item);
        var options = new Dictionary<string, Option>(StringComparer.Ordinal);
        Option Make(ItemData data, bool inPool)
        {
            data = Resolve(data);
            var feature = data?.GetComponent<AffixItemFeature>();
            if (feature == null || !data.Guid.IsValid()) throw new InvalidOperationException(MenuText.Get("词缀资源或标识无效"));
            var option = new Option { Data = data, Label = ItemBrowser.ReadAffixLabel(data), Mask = (int)feature.allowedTargetTypes, InPool = inPool,
                Excluded = Expand(feature.excludedItems).Contains(source.Guid.guid) };
            option.ExcludedAffixes.UnionWith(Expand(feature.excludedAffixes));
            return option;
        }
        var pool = holder.allowedAffixes.GetAllItems() ?? throw new InvalidOperationException(MenuText.Get("词缀库尚未加载"));
        for (int i = 0; i < pool.Count; i++)
        {
            var data = Resolve(pool[i]);
            if (data?.HasComponent<AffixItemFeature>() != true) continue;
            var option = Make(data, true); options[option.Id] = option;
        }
        foreach (var id in ids)
            if (!options.ContainsKey(id))
            {
                var array = item.GetComponent<AffixData>().affixesGuids;
                var nativeId = array[Array.IndexOf(ids, id)];
                var data = game.Saver.allItems.GetItemByID(nativeId);
                options[id] = Make(data, false);
            }
        target = item; inventory = inv; slot = whichSlot; generation = game.Generation; playerPointer = game.PlayerPointer; tier = item.tier; targetMask = mask;
        expectedIds = ids; label = ItemBrowser.ReadLabel(source.title, source.name);
        Options.Clear(); Options.AddRange(options.Values.OrderBy(e => e.Label.Display));
        Draft.Clear(); Draft.AddRange(ids.Select(id => options[id]));
        Plugin.LogEvent($"affix_target source={source.name} tier={tier} slot={slot} current={string.Join(",",ids)} options={Options.Count}");
    }

    public bool CanAdd(Option option)
        => AffixSelectionPolicy.CanAdd(option.Mask, targetMask, option.InPool, option.Excluded, Draft.Any(e => e.Id == option.Id),
            Draft.Any(e => e.ExcludedAffixes.Contains(option.Id) || option.ExcludedAffixes.Contains(e.Id)));
    public void Toggle(Option option)
    {
        GuardRecovery();
        var old = Draft.FirstOrDefault(e => e.Id == option.Id);
        if (old != null) Draft.Remove(old);
        else if (CanAdd(option)) Draft.Add(option);
    }
    public void ClearDraft() { GuardRecovery(); Draft.Clear(); }
    public void Reset()
    {
        var record = recovery.Retire();
        if (record != null) Plugin.LogEvent("affix_recovery_quarantined " + record);
        target = null; inventory = null; Options.Clear(); Draft.Clear(); expectedIds = Array.Empty<string>(); label = null;
    }
    private void GuardRecovery()
    {
        if(game.InventoryRecoveryBlocked)throw new InvalidOperationException(MenuText.Get("配装恢复尚未完成"));
        if (RecoveryPending) throw new InvalidOperationException(MenuText.Get("旧词缀恢复未完成，请先重试恢复"));
    }
    private void ValidateTarget()
    {
        game.RequireReady();
        if (target == null || generation != game.Generation || game.Saver?.mainCharacterVariable?.CurrentValue?.Pointer != playerPointer || target.tier != tier || !ReadIds(target).SequenceEqual(expectedIds))
            throw new InvalidOperationException(MenuText.Get("武器或词缀已变化，请重新载入"));
        if (IsSpawn) return;
        RequireEquippedOwner(inventory, target);
    }
    private void RequireEquippedOwner(InventoryComponent expectedInventory, ItemInstanceData expectedOwner)
    {
        var inv = game.Player.GetComponent<InventoryComponent>();
        ItemInstanceData equipped = null;
        var found = slot == 1 ? inv != null && inv.TryGetSecondaryWeapon(out equipped) : inv != null && inv.TryGetMainWeapon(out equipped);
        if (!found || !AffixOwnerScope.IsCurrent(generation, game.Generation, playerPointer,
            game.Saver?.mainCharacterVariable?.CurrentValue?.Pointer ?? IntPtr.Zero, expectedInventory.Pointer, inv?.Pointer ?? IntPtr.Zero,
            expectedOwner.Pointer, equipped?.Pointer ?? IntPtr.Zero, tier, expectedOwner.tier))
            throw new InvalidOperationException(MenuText.Get("装备栏位已变化，请重新载入"));
    }
    private void ValidateDraft()
    {
        var ids = new HashSet<string>();
        foreach (var option in Draft)
            if (!AffixSelectionPolicy.CanAdd(option.Mask, targetMask, option.InPool, option.Excluded, !ids.Add(option.Id),
                Draft.Any(other => other.Id != option.Id && (other.ExcludedAffixes.Contains(option.Id) || option.ExcludedAffixes.Contains(other.Id)))))
                throw new InvalidOperationException(MenuText.Get("词缀组合不兼容，请移除冲突词缀"));
    }
    private static List<ItemInstanceData> OwnedChildren(InventoryComponent inv, ItemInstanceData owner)
    {
        var result = new List<ItemInstanceData>();
        var items = inv.items ?? throw new InvalidOperationException(MenuText.Get("角色背包尚未就绪"));
        for (int i = 0; i < items.Count; i++)
        {
            var child = items[i];
            if (WeaponAffixOwnership.IsOwnedBy(owner, child)) result.Add(child);
        }
        return result;
    }
    private static void VerifyChildren(InventoryComponent inv, ItemInstanceData owner, string[] ids)
    {
        if (!ReadIds(owner).SequenceEqual(ids) || !OwnedChildren(inv, owner).Select(i => i.source.Guid.guid).OrderBy(i => i).SequenceEqual(ids.OrderBy(i => i)))
            throw new InvalidOperationException(MenuText.Get("词缀数据或生效实例回读失败"));
    }
    public string ApplyEquipped()
    {
        GuardRecovery(); ValidateTarget(); ValidateDraft();
        if (IsSpawn) throw new InvalidOperationException(MenuText.Get("新武器请使用生成按钮"));
        var owner = target; var inv = inventory; var data = owner.GetComponent<AffixData>();
        var oldArray = data.affixesGuids;
        var oldIds = expectedIds.ToArray();
        var newIds = Draft.Select(o => o.Id).ToArray();
        if (oldIds.SequenceEqual(newIds)) return MenuText.Get("词缀没有变化");
        VerifyChildren(inv, owner, oldIds);
        var oldChildren = OwnedChildren(inv, owner);
        var newChildren = Draft.Select(o => {
            var child = new ItemInstanceData(o.Data, 1);
            child.AddComponent<OwnerData>().targetItem = owner;
            return child;
        }).ToArray();
        void Detach() { foreach (var child in OwnedChildren(inv, owner)) inv.RemoveItem(child); }
        void Restore()
        {
            RequireEquippedOwner(inv, owner);
            Detach(); data.affixesGuids = oldArray;
            foreach (var child in oldChildren)
            {
                child.GetComponent<OwnerData>().targetItem = owner;
                if (!inv.HasItem(child) && !inv.TryAddItem(child)) throw new InvalidOperationException(MenuText.Get("原词缀实例恢复失败"));
            }
            VerifyChildren(inv, owner, oldIds);
            if (!oldChildren.All(child => inv.HasItem(child))) throw new InvalidOperationException(MenuText.Get("原词缀实例恢复失败"));
        }
        backup();
        Plugin.LogEvent($"affix_edit_begin source={owner.source.name} tier={owner.tier} old={string.Join(",",oldIds)} new={string.Join(",",newIds)}");
        try
        {
            AffixEditTransaction.Apply(() => {
                Detach(); data.affixesGuids = NativeIds(Draft);
                foreach (var child in newChildren)
                    if (!inv.TryAddItem(child)) throw new InvalidOperationException(MenuText.Get("词缀原生应用失败"));
            }, () => { RequireEquippedOwner(inv, owner); VerifyChildren(inv, owner, newIds); if (!newChildren.All(child => inv.HasItem(child))) throw new InvalidOperationException(MenuText.Get("词缀原生应用失败")); }, Restore);
        }
        catch (AggregateException)
        {
            recovery.Set(generation, Restore, $"player={playerPointer} inventory={inv.Pointer} weapon={owner.Pointer} source={owner.source.name} tier={tier} original={string.Join(",",oldIds)} requested={string.Join(",",newIds)}");
            Plugin.LogEvent("affix_restore_blocked"); throw;
        }
        catch { Plugin.LogEvent("affix_edit_rolled_back"); throw; }
        expectedIds = newIds;
        Plugin.LogEvent("affix_edit_complete exact_owner=true");
        return MenuText.Format("已应用 {0} 个词缀：{1}", Draft.Count, Name);
    }
    public string Recover()
    {
        game.RequireReady();
        if (!RecoveryPending) return MenuText.Get("没有待恢复词缀");
        if (generation != game.Generation || game.Player.GetComponent<InventoryComponent>()?.Pointer != inventory.Pointer)
            throw new InvalidOperationException(MenuText.Get("原角色已变化，不能向新角色恢复旧词缀"));
        RequireEquippedOwner(inventory, target);
        recovery.Recover(game.Generation);
        Draft.Clear(); Draft.AddRange(expectedIds.Select(id => Options.First(o => o.Id == id)));
        Plugin.LogEvent("affix_restore_complete"); return MenuText.Get("已恢复原来的词缀");
    }
    public string Spawn()
    {
        GuardRecovery(); ValidateTarget(); ValidateDraft();
        if (!IsSpawn) throw new InvalidOperationException(MenuText.Get("请从物品页选择要生成的武器"));
        // Each pickup owns a fresh instance; repeated generation never shares component state.
        var instance = new ItemInstanceData(target.source, tier);
        instance.GetComponent<AffixData>().affixesGuids = NativeIds(Draft);
        var result = browser.SpawnInstance(instance);
        Plugin.LogEvent($"affix_spawn source={instance.source.name} tier={tier} affixes={string.Join(",",Draft.Select(e=>e.Id))}");
        return result;
    }
    internal (ItemInstanceData Owner,ItemInstanceData[] Children) PrepareInstance(WeaponPreset preset)
    {
        game.RequireReady();GuardRecovery();preset.Validate();if(browser.Items.Count==0)browser.Reload();
        var source=browser.Items.SingleOrDefault(e=>e.Data.Guid.guid==preset.ItemId)?.Data;
        if(source==null)throw new InvalidOperationException(MenuText.Get("预设物品在当前版本中不存在"));
        if(!source.HasComponent<AffixHolderItemFeature>()&&preset.AffixIds.Count==0&&(source.HasComponent<WeaponFeature>()||source.HasComponent<UtilityWeaponFeature>()))return(new ItemInstanceData(source,preset.Tier),Array.Empty<ItemInstanceData>());
        var candidate=new WeaponAffixActions(game,browser,backup);candidate.LoadPreset(preset);
        var owner=new ItemInstanceData(candidate.target.source,candidate.tier);
        owner.GetComponent<AffixData>().affixesGuids=NativeIds(candidate.Draft);
        var children=candidate.Draft.Select(o=>{var child=new ItemInstanceData(o.Data,1);child.AddComponent<OwnerData>().targetItem=owner;return child;}).ToArray();
        return(owner,children);
    }
}
