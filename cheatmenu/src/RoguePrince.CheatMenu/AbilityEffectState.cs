// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using MotherBase.ActionFramework;
namespace RoguePrince.CheatMenu;

// Confirms the installed native effects, including callbacks that failed after changing state.
internal sealed class AbilityEffectState
{
    private sealed class Effect
    {
        internal readonly ItemFeature Feature; internal readonly ItemInstanceData Item;
        internal readonly bool Action; internal readonly IntPtr Identity;
        internal Effect(ItemFeature feature, ItemInstanceData item, bool action, IntPtr identity)
        { Feature=feature;Item=item;Action=action;Identity=identity; }
    }
    private readonly GameSession game;
    private readonly Dictionary<IntPtr, Effect> effects = new();
    private readonly List<Action> restoreOrders = new();
    private readonly List<Action> guardContainers = new();
    internal AbilityEffectState(GameSession game, IEnumerable<ItemInstanceData> items)
    {
        this.game = game;
        foreach (var item in items)
        {
            if (item.components == null || item.components.Count != 0)
                throw new InvalidOperationException(Core.MenuText.Get("技能实例包含未接入的附加组件"));
            var features = item.source.subAssets;
            for (var i = 0; i < features.Count; i++)
            {
                var feature = features[i]; var name = feature.GetIl2CppType().FullName;
                if (name == "ActionBindingsOverrideFeature") effects.TryAdd(feature.Pointer, new(feature, item, true, feature.Pointer));
                if (name == "StatModifierFeature")
                {
                    var modifier = feature.Cast<StatModifierFeature>().targetValue ?? throw new InvalidOperationException("Missing skill modifier");
                    effects.TryAdd(feature.Pointer, new(feature, item, false, modifier.Pointer));
                }
            }
        }
        var actionIds=effects.Values.Where(x=>x.Action).Select(x=>x.Identity).ToHashSet();
        if(actionIds.Count>0)
            CaptureOrder(()=>game.Player.GetComponentData<ActionBindingsReference>()?.overrideBindings?.overriders,
                row=>row.Pointer.ToString(),key=>actionIds.Any(p=>p.ToString()==key));
        var statEffects=effects.Values.Where(x=>!x.Action).ToArray();
        var statIds=statEffects.Select(x=>x.Identity).ToHashSet();
        foreach(var effect in statEffects.GroupBy(x=>x.Feature.Cast<StatModifierFeature>().targetValue.GetTargetStat().FullName).Select(x=>x.First()))
        {
            var target=effect.Feature.Cast<StatModifierFeature>().targetValue.GetTargetStat();
            CaptureOrder(()=>
            {
                var stats=game.Player.GetComponent<StatsStateComponent>()?.CurrentStats ?? throw new InvalidOperationException("Missing stats state");
                return stats.TryGetValue(target,out var state)?state.modifiers:null;
            },row=>row.Item1==null?throw new InvalidOperationException("Null native modifier"):
                row.Item1.Pointer+"|"+(statIds.Contains(row.Item1.Pointer)?"owned":row.Item2?.Pointer.ToString()??"null"),
                key=>key.EndsWith("|owned",StringComparison.Ordinal));
        }
    }
    private void CaptureOrder<T>(Func<Il2CppSystem.Collections.Generic.List<T>> read,Func<T,string> identity,Func<string,bool> owned)
    {
        var initial=read();var pointer=initial?.Pointer??IntPtr.Zero;
        string[] Keys(Il2CppSystem.Collections.Generic.List<T> list)
        {
            if(list==null)return Array.Empty<string>();
            var count=list.Count;var keys=new string[count];
            for(var i=0;i<count;i++)keys[i]=identity(list[i]);
            if(count!=list.Count)throw new InvalidOperationException("Native effect order changed while reading");
            return keys;
        }
        var before=Keys(initial);
        var foreign=before.Where(k=>!owned(k)).ToArray();
        if(foreign.Distinct(StringComparer.Ordinal).Count()!=foreign.Length)
            throw new InvalidOperationException(Core.MenuText.Get("原生效果顺序发生外部变化，保留恢复记录"));
        void Guard()
        {
            var current=read();
            if(pointer!=IntPtr.Zero && current?.Pointer!=pointer)
                throw new InvalidOperationException(Core.MenuText.Get("原生效果容器已更换，保留恢复记录"));
            if(pointer==IntPtr.Zero && current!=null)pointer=current.Pointer;
        }
        guardContainers.Add(Guard);
        restoreOrders.Add(()=>
        {
            Guard();
            var current=read();
            if(pointer!=IntPtr.Zero && current?.Pointer!=pointer)
                throw new InvalidOperationException(Core.MenuText.Get("原生效果容器已更换，保留恢复记录"));
            var actual=Keys(current);
            var oldForeign=before.Where(k=>!owned(k)).ToArray();
            var currentForeign=actual.Where(k=>!owned(k)).ToArray();
            var surviving=oldForeign.Where(k=>currentForeign.Contains(k)).ToArray();
            if(oldForeign.Distinct(StringComparer.Ordinal).Count()!=oldForeign.Length ||
                currentForeign.Distinct(StringComparer.Ordinal).Count()!=currentForeign.Length ||
                !surviving.SequenceEqual(currentForeign.Where(k=>oldForeign.Contains(k))) ||
                !before.Where(owned).OrderBy(k=>k,StringComparer.Ordinal).SequenceEqual(actual.Where(owned).OrderBy(k=>k,StringComparer.Ordinal)))
                throw new InvalidOperationException(Core.MenuText.Get("原生效果顺序发生外部变化，保留恢复记录"));
            // Preserve new/expired foreign effects. Place originals after their nearest surviving
            // original predecessor, before effects appended later by other native systems.
            var groups=new Dictionary<string,List<string>>(StringComparer.Ordinal);var anchor="";
            foreach(var key in before)
            {
                if(!owned(key)){if(surviving.Contains(key))anchor=key;continue;}
                if(!groups.TryGetValue(anchor,out var group))groups[anchor]=group=new();group.Add(key);
            }
            var desired=new List<string>();
            if(groups.TryGetValue("",out var first))desired.AddRange(first);
            foreach(var key in currentForeign){desired.Add(key);if(groups.TryGetValue(key,out var group))desired.AddRange(group);}
            // Move runtime entries only. Keep the fresh contexts created by native pickup callbacks.
            for(var i=0;i<desired.Count;i++)
            {
                if(identity(current[i])==desired[i])continue;
                var j=i+1;while(j<current.Count && identity(current[j])!=desired[i])j++;
                if(j==current.Count)throw new InvalidOperationException("Native effect order entry missing");
                var row=current[j];current.RemoveAt(j);current.Insert(i,row);
            }
            if(!desired.SequenceEqual(Keys(read())))throw new InvalidOperationException(Core.MenuText.Get("原生效果顺序恢复回读失败"));
        });
    }
    internal void GuardContainers(){foreach(var guard in guardContainers)guard();}
    internal void RestoreOrder(){foreach(var restore in restoreOrders)restore();}
    internal Dictionary<IntPtr, int> Expected(IEnumerable<ItemInstanceData> items)
    {
        var result = effects.Keys.ToDictionary(x => x, _ => 0);
        foreach (var item in items)
            for (var i = 0; i < item.source.subAssets.Count; i++)
                if (result.ContainsKey(item.source.subAssets[i].Pointer)) result[item.source.subAssets[i].Pointer]++;
        if (result.Values.Any(x => x > 1)) throw new InvalidOperationException(Core.MenuText.Get("技能共享效果实例，拒绝重叠试装"));
        return result;
    }
    private int Count(Effect effect)
    {
        if (effect.Action)
        {
            var list = game.Player.GetComponentData<ActionBindingsReference>()?.overrideBindings?.overriders ?? throw new InvalidOperationException("Missing action overriders");
            var count = list.Count; var found = 0;
            for (var i = 0; i < count; i++) if (list[i]?.Pointer == effect.Identity) found++;
            if (list.Count != count) throw new InvalidOperationException("Action effects changed while reading");
            return found;
        }
        var modifier = effect.Feature.Cast<StatModifierFeature>().targetValue;
        var stats = game.Player.GetComponent<StatsStateComponent>()?.CurrentStats ?? throw new InvalidOperationException("Missing stats state");
        var target = modifier.GetTargetStat() ?? throw new InvalidOperationException("Missing target stat");
        if (!stats.TryGetValue(target, out var state)) return 0;
        var modifiers = state?.modifiers ?? throw new InvalidOperationException("Missing modifiers");
        var n = modifiers.Count; var matches = 0;
        for (var i = 0; i < n; i++)
        {
            var current = modifiers[i].Item1;
            // Native RemoveModifier can compare IDs. Reject a different modifier sharing the ID.
            if (current != null && current.GetId() == modifier.GetId())
            {
                if (current.Pointer != effect.Identity) throw new InvalidOperationException(Core.MenuText.Get("技能效果与其他来源使用同一标识，拒绝修改"));
                matches++;
            }
        }
        if (modifiers.Count != n) throw new InvalidOperationException("Stat effects changed while reading");
        return matches;
    }
    internal void Verify(Dictionary<IntPtr, int> expected)
    {
        foreach (var pair in effects)
            if (Count(pair.Value) != expected[pair.Key]) throw new InvalidOperationException(Core.MenuText.Get("技能原生效果回读失败，保留恢复记录"));
    }
    internal void Clear()
    {
        foreach (var effect in effects.Values)
        {
            var count = Count(effect);
            if (count > 1) throw new InvalidOperationException(Core.MenuText.Get("技能共享效果实例，拒绝重叠试装"));
            if (count == 0) continue;
            if (effect.Action) effect.Feature.Cast<ActionBindingsOverrideFeature>().OnRemoved(game.Player, game.Player.GetComponent<InventoryComponent>(), effect.Item);
            else effect.Feature.Cast<StatModifierFeature>().OnRemoved(game.Player, game.Player.GetComponent<InventoryComponent>(), effect.Item);
            if (Count(effect) != 0) throw new InvalidOperationException(Core.MenuText.Get("技能原生效果清理尚未完成"));
        }
    }
}
