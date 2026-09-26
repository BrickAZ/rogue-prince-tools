// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
// Additional game linking permission: see GAME-LINKING-EXCEPTION.txt.

using RoguePrince.CheatMenu.Core;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using NativeItemSet = Il2CppSystem.Collections.Generic.HashSet<ItemData>;

namespace RoguePrince.CheatMenu;

internal static class NativeItemSnapshot
{
    public static ItemData[] Read(Il2CppSystem.Collections.Generic.IEnumerable<ItemData> source)
    {
        if (source == null) throw new InvalidOperationException(MenuText.Get("原生物品库没有返回集合"));
        var sourceType = IL2CPP.il2cpp_class_get_namespace_(source.ObjectClass) + "." + IL2CPP.il2cpp_class_get_name_(source.ObjectClass);
        var items = source.TryCast<NativeItemSet>();
        if (items == null) throw new InvalidOperationException(MenuText.Get("不支持的原生物品集合类型：") + sourceType);

        // GetItems builds a fresh native HashSet. Its boxed struct enumerator cannot safely
        // use this installation's generated IEnumerator.MoveNext (missing receiver unbox).
        // CopyTo runs on the reference-type set, then array indexing crosses the boundary.
        var count = items.Count;
        var version = items._version;
        Plugin.LogEvent($"item_snapshot_begin type={sourceType} reader=HashSet.CopyTo count={count} version={version}");
        var native = new Il2CppReferenceArray<ItemData>(count);
        items.CopyTo(native, 0, count);
        if (items.Count != count || items._version != version)
            throw new InvalidOperationException(MenuText.Get("复制期间原生物品集合确实发生变化；本次列表未替换"));

        var result = new ItemData[count];
        for (var i = 0; i < count; i++) result[i] = native[i];
        Plugin.LogEvent($"item_snapshot_complete copied={result.Length} version_unchanged=true");
        return result;
    }
}
