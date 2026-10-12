// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
using Il2CppInterop.Runtime;
using RoguePrince.CheatMenu.Core;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine;
namespace RoguePrince.CheatMenu;

internal sealed class MobBrowser : IDisposable
{
    internal sealed class Entry
    {
        internal MobCatalogEntry Data;
        internal string InternalName => Data.Id;
        internal string Name => Data.Name;
        internal MobSpawnRule Rule;
    }
    private AsyncOperationHandle<MobSpawnRule> handle;
    private bool requested, temporaryRule;
    private Entry loaded;
    internal List<Entry> Entries { get; }=MobCatalogPolicy.Entries.Select(d=>new Entry{Data=d}).ToList();
    internal Entry Find(string id)=>Entries.FirstOrDefault(e=>e.InternalName==id);
    internal List<Entry> Filter(bool boss,string query)
    {
        query=query?.Trim()??"";
        return Entries.Where(e=>e.Data.Boss==boss&&e.Data.Matches(query)).ToList();
    }
    internal bool Ready(string id)=>loaded?.InternalName==id&&loaded.Rule!=null;
    internal void Reload(Entry entry)
    {
        if(entry==null||!Entries.Contains(entry)||!entry.Data.Supported)throw new InvalidOperationException(MenuText.Get("请先选择可召唤的怪物"));
        if(loaded==entry&&(requested||temporaryRule))return;
        Dispose();loaded=entry;
        if(entry.Data.PrefabOnly)
        {
            // A private transient rule points at a verified native boss prefab. No asset is edited.
            FindAsset<GameObject>(entry.Data.PrefabGuid);
            var rule=ScriptableObject.CreateInstance<MobSpawnRule>();
            entry.Rule=rule;temporaryRule=true;
            rule.name=entry.Data.RuleName;
            rule.mobPrefabReference=new BSAssetReference<GameObject>(entry.Data.PrefabGuid);
        }
        else
        {
            handle=Addressables.LoadAssetAsync<MobSpawnRule>(FindAsset<MobSpawnRule>(entry.Data.RuleGuid));
            requested=true;
        }
    }
    internal void Tick()
    {
        if (!requested || !handle.IsDone) return;
        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            throw new InvalidOperationException(MenuText.Get("所选怪物资源加载失败"));
        var rule = handle.Result;
        if (rule.name != loaded.Data.RuleName || rule.mobPrefabReference == null || rule.mobPrefabReference.AssetGUID!=loaded.Data.PrefabGuid)
            throw new InvalidOperationException(MenuText.Get("所选怪物资源身份不匹配"));
        loaded.Rule = rule;
    }
    private static IResourceLocation FindAsset<T>(string key) where T:UnityEngine.Object
    {
        var iterator = Addressables.ResourceLocators.GetEnumerator();
        var moves = iterator.Cast<Il2CppSystem.Collections.IEnumerator>();
        while (moves.MoveNext())
        {
            var locator = iterator.Current;
            if (!locator.Locate((Il2CppSystem.String)key, Il2CppType.Of<T>(), out var locations) || locations == null) continue;
            var count = locations.Cast<Il2CppSystem.Collections.Generic.ICollection<IResourceLocation>>().Count;
            for (int i = 0; i < count; i++)
                if(locations[i]!=null)return locations[i];
        }
        throw new InvalidOperationException(MenuText.Get("未找到所选怪物原版资源，请保留日志"));
    }
    public void Dispose()
    {
        if(loaded!=null){if(temporaryRule&&loaded.Rule!=null)UnityEngine.Object.Destroy(loaded.Rule);loaded.Rule=null;}
        if (requested && handle.IsValid()) Addressables.Release(handle);
        requested=temporaryRule=false;loaded=null;
    }
}
