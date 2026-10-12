// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
using HarmonyLib;
using MotherBase.EntityFramework;
using MotherBase.ActionFramework;
using RoguePrince.CheatMenu.Core;

namespace RoguePrince.CheatMenu;

// Only these two native methods call brain.Think/Act. Awake/Start, Actor,
// physics, animation and Damageable run normally; no shared brain mutation.
internal static class MobAiControl
{
    internal sealed class Owned
    {
        internal Entity Entity;
        internal AIStateComponent State;
        internal MoveIntent3D Move;
        internal bool Ready, AiEnabled, OriginalStayIdle, Boss;
    }
    internal static bool Available;
    private static readonly Dictionary<IntPtr, Owned> owned = new();
    internal static void Register(Entity entity, bool aiEnabled)
    {
        var state = entity.GetComponent<AIStateComponent>();
        var move = entity.GetComponent<MoveIntent3D>();
        var input = entity.GetComponent<BSAIInput>();
        var brain=input?.brain;
        bool boss=brain!=null&&(brain.TryCast<AltanBossBrain>()!=null||brain.TryCast<BaatarBossBrain>()!=null||
            brain.TryCast<CorruptedNogaiBrain>()!=null||brain.TryCast<GoatBossBrain>()!=null||
            brain.TryCast<KaaliBossBrain>()!=null||brain.TryCast<NogaiBossBrain>()!=null);
        if(!MobAiPolicy.Supports(state!=null&&move!=null&&brain!=null,brain?.TryCast<MobBrain>()!=null,boss))
            throw new InvalidOperationException(MenuText.Get("所选怪物缺少受支持的原生 AI 组件"));
        owned.Add(entity.Pointer, new Owned { Entity=entity, State=state, Move=move,
            AiEnabled=aiEnabled, OriginalStayIdle=state.stayIdle,Boss=boss });
    }
    internal static void Ready(Entity entity)
    {
        if (!owned.TryGetValue(entity.Pointer, out var record)) throw new InvalidOperationException("Unowned spawn");
        record.Ready = true;
        record.State.stayIdle = record.AiEnabled ? record.OriginalStayIdle : true;
    }
    internal static void Hold(Entity entity)
    {
        if (entity != null && owned.TryGetValue(entity.Pointer, out var record)) record.Ready = false;
    }
    internal static void SetEnabled(Entity entity,bool enabled)
    {
        if(entity==null || !owned.TryGetValue(entity.Pointer,out var record) || !record.Ready)
            throw new InvalidOperationException("Unowned or unfinished training target");
        var previous=record.State.stayIdle;
        try
        {
            record.State.stayIdle=enabled?record.OriginalStayIdle:true;
            if(record.State.stayIdle!=(enabled?record.OriginalStayIdle:true))throw new InvalidOperationException("AI state readback failed");
            record.AiEnabled=enabled;
        }
        catch {record.State.stayIdle=previous;throw;}
    }
    internal static void Forget(IntPtr pointer) => owned.Remove(pointer);
    internal static bool IsOwnedBoss(Entity entity)=>entity!=null&&owned.TryGetValue(entity.Pointer,out var record)&&record.Boss;
    internal static bool Allow(Entity entity)
    {
        if (entity == null || !owned.TryGetValue(entity.Pointer, out var record)) return true;
        if (record.Ready && record.AiEnabled) return true;
        // Ignore only autonomous input, never velocity/knockback or reaction actions.
        try
        {
            if (record.State != null) { record.State.stayIdle = true; record.State.intent = MobIntent.Idle; }
            if (record.Move != null) record.Move.magnitude = 0;
        }
        catch (Exception e) { Plugin.MenuLog.LogWarning("[RPCheat] mob_ai_hold " + e.Message); }
        return false;
    }
}
[HarmonyPatch(typeof(BSAIInput), nameof(BSAIInput.PreUpdateAI))]
internal static class MobPreAiPatch
{
    private static bool Prefix(Entity __0) => MobAiControl.Allow(__0);
}
[HarmonyPatch(typeof(BSAIInput), nameof(BSAIInput.PostUpdateAI))]
internal static class MobPostAiPatch
{
    private static bool Prefix(Entity __0) => MobAiControl.Allow(__0);
}
[HarmonyPatch(typeof(Entity), nameof(Entity.OnDestroy))]
internal static class MobDestroyedPatch
{
    private static void Prefix(Entity __instance) => MobAiControl.Forget(__instance.Pointer);
}
[HarmonyPatch(typeof(SituationTriggerEffectComponent), nameof(SituationTriggerEffectComponent.ApplyTriggerEffect))]
internal static class BossEncounterEffectPatch
{
    // Native prefab listeners include formal quest progress and arena globals.
    // Detached listeners are also guarded in case a callback was already queued.
    private static bool Prefix(SituationTriggerEffectComponent __instance)=>!MobAiControl.IsOwnedBoss(__instance.GetComponent<Entity>());
}
[HarmonyPatch(typeof(CutsceneManager), nameof(CutsceneManager.BindActorEntityAndSetAvailable))]
internal static class BossCutsceneActorPatch
{
    private static bool Prefix(Entity __0)=>!MobAiControl.IsOwnedBoss(__0);
}
