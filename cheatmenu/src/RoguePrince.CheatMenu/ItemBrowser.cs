// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
// Additional game linking permission: see GAME-LINKING-EXCEPTION.txt.

using System.Text.RegularExpressions;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UObject = UnityEngine.Object;

namespace RoguePrince.CheatMenu;

internal sealed class ItemBrowser
{
    public sealed class Entry
    {
        public ItemData Data;
        public LocalizedItemName Label;
        public string Name => Label.Display;
        public string InternalName;
        public string Category;
        public bool HasTier;
    }
    private readonly GameSession game;
    public readonly List<Entry> Items = new();
    public string State { get; private set; } = MenuText.Get("进入存档后点击刷新物品库");
    public ItemBrowser(GameSession game) => this.game = game;

    public void Reload()
    {
        game.RequireReady();
        if (game.Saver == null || game.Saver.allItems == null) throw new InvalidOperationException(MenuText.Get("物品库尚未加载"));
        var next = new List<Entry>();
        var seen = new HashSet<IntPtr>();
        var snapshot = NativeItemSnapshot.Read(game.Saver.allItems.GetItems());
        var chinese = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier("zh-CN"));
        var english = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier("en-US"));
        foreach (var sourceItem in snapshot)
            {
                var item = sourceItem;
                // The mega-collection can contain lazy-loading placeholders.
                if (item != null && item.HasComponent<LoadItemFeature>()) item = item.GetComponent<LoadItemFeature>().LoadItemData();
                if (item == null || !seen.Add(item.Pointer)) continue;
                if (item.HasComponent<UnspawnableItemFeature>()) continue;
                var raw = item.name;
                var label = new LocalizedItemName(ReadName(item, chinese), ReadName(item, english), raw);
                var category = Classify(item);
                next.Add(new Entry { Data = item, Label = label, InternalName = raw, Category = category, HasTier = item.scalesWithTier });
            }
        Items.Clear();
        Items.AddRange(next.OrderBy(e => e.Category).ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase));
        State = MenuText.Format("已加载 {0} 项；名称缺失时显示资源原名", Items.Count);
        Plugin.LogEvent("item_catalog count=" + Items.Count);
        Plugin.LogEvent("item_catalog_categories " + string.Join("; ", Items.GroupBy(e => e.Category).Select(g => g.Key + "=" + g.Count())));
    }

    private static string ReadName(ItemData item, UnityEngine.Localization.Locale locale)
    {
        if (locale == null || item.title == null) return null;
        try
        {
            // Clone the reference: never change the shared ItemData or the game's selected locale.
            var local = new LocalizedString(item.title.TableReference, item.title.TableEntryReference);
            local.LocaleOverride = locale;
            var title = local.GetLocalizedString();
            return string.IsNullOrWhiteSpace(title) ? null : Regex.Replace(title, "<[^>]+>", "");
        }
        catch { return null; } // Exact asset name remains available if a table is not ready.
    }

    private static string Classify(ItemData item)
    {
        var features = new HashSet<string>(StringComparer.Ordinal);
        void Read<T>() where T : ItemFeature
        {
            if (item.HasComponent<T>()) features.Add(typeof(T).Name);
        }
        Read<MedallionItemFeature>(); Read<UtilityWeaponFeature>(); Read<WeaponFeature>();
        Read<SkillTreeItemFeature>(); Read<AwakeningItemFeature>(); Read<SkinItemFeature>();
        Read<MedallionsPackTagFeature>(); Read<AddListItemsToReadyToUnlockFeature>();
        Read<AffixItemFeature>(); Read<BlueprintItemFeature>(); Read<UnityTemplateProjects.Debug.DebugItemFeature>();
        Read<GainGoldFeature>(); Read<GoldHolderItemFeature>(); Read<GainSpiritShardFeature>();
        Read<GainCorruptedBloodFeature>(); Read<AdditionalSkillPointFeature>(); Read<HealFeature>();
        Read<EffectItemFeature>(); Read<UnlockInCraftFeature>(); Read<UnlockInForgeFeature>();
        return ItemCategoryPolicy.Classify(item.name, features);
    }

    public List<Entry> Filter(string query, string category)
    {
        query = query?.Trim() ?? "";
        return Items.Where(e => (category == "全部" || e.Category == category) && e.Label.Matches(query)).ToList();
    }

    public string Spawn(Entry entry, int tier)
    {
        game.RequireReady();
        if (entry == null || entry.Data == null) throw new InvalidOperationException(MenuText.Get("请先选择有效物品"));
        if (tier < 1 || tier > 100) throw new ArgumentOutOfRangeException(nameof(tier), MenuText.Get("菜单生成等级范围为 1–100；0 级不存在，无法使用（原版仅 1–5 级）"));
        var actualTier = entry.HasTier ? tier : 1;
        var debug = UObject.FindObjectOfType<DebugTriggerItemMenu>();
        if (debug != null && debug.spawnPrefab != null && debug.player != null && debug.player.CurrentValue != null && debug.player.CurrentValue.Pointer == game.Player.Pointer)
            debug.SpawnItem(entry.Data, actualTier);
        else
        {
            var service = UObject.FindObjectOfType<DebugService>();
            var drop = entry.Data.GetComponent<LootOnDropFeature>();
            var prefab = drop != null ? drop.lootPrefab : null;
            if (prefab == null && service != null && service.itemPrefab != null) prefab = service.itemPrefab.gameObject;
            if (prefab == null)
            {
                // Normal gameplay loot generators remain usable without the debug service.
                var prefabs = new Dictionary<IntPtr, GameObject>();
                foreach (var generator in Resources.FindObjectsOfTypeAll<LootGenerator>())
                    if (generator != null && generator.baseItemPickupPrefab != null && generator.mainCharacterVariable != null && generator.mainCharacterVariable.CurrentValue != null && generator.mainCharacterVariable.CurrentValue.Pointer == game.Player.Pointer)
                        prefabs[generator.baseItemPickupPrefab.Pointer] = generator.baseItemPickupPrefab;
                if (prefabs.Count == 1) prefab = prefabs.Values.First();
            }
            if (prefab == null) throw new InvalidOperationException(MenuText.Get("没有找到唯一的物品掉落预制体；请进入可游玩关卡后重试"));
            var spawned = UObject.Instantiate(prefab, game.Player.transform.position + new Vector3(0.8f, 0.8f, 0), Quaternion.identity);
            if (spawned == null) throw new InvalidOperationException(MenuText.Get("游戏没有创建掉落物"));
            try
            {
                var pickup = spawned.GetComponent<PickupItemComponent>();
                if (pickup == null) throw new InvalidOperationException(MenuText.Get("该物品使用特殊掉落物，当前备用生成路径不支持"));
                pickup.SetItemData(entry.Data, actualTier);
            }
            catch { UObject.Destroy(spawned); throw; }
        }
        return MenuText.Format("已请求生成：{0}", entry.Name) + (entry.HasTier ? MenuText.Format("（等级 {0}）", actualTier) : "");
    }
}
