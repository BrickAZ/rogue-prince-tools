// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using HarmonyLib;
using MotherBase.EntityFramework;
namespace RoguePrince.CheatMenu;
internal static class ScenePatchGuard
{
 internal static T Read<T>(Func<T> action,T fallback)
 {try{return action();}catch(Exception e){SceneActions.Current?.ResetEnemies();RunTools.Current?.Disable();Plugin.MenuLog.LogError("[RPCheat] scene_patch_disabled "+e);return fallback;}}
 internal static void Write(Action action)=>Read(()=>{action();return true;},false);
}
[HarmonyPatch(typeof(MobBrain),nameof(MobBrain.TryGetVisibleTarget))]
internal static class SceneTargetPatch
{private static void Postfix(Entity __0,ref Entity __1,ref bool __result){var target=__1;if(__result&&ScenePatchGuard.Read(()=>SceneActions.Current?.RejectTarget(__0,target)==true,false)){__1=null;__result=false;}}}
[HarmonyPatch(typeof(BSAIInput),nameof(BSAIInput.PreUpdateAI))]
internal static class SceneTargetPrePatch
{private static void Prefix(Entity __0)=>ScenePatchGuard.Write(()=>SceneActions.Current?.FilterTarget(__0));}
[HarmonyPatch(typeof(BSAIInput),nameof(BSAIInput.PostUpdateAI))]
internal static class SceneTargetPostPatch
{private static void Postfix(Entity __0)=>ScenePatchGuard.Write(()=>SceneActions.Current?.FilterTarget(__0));}
[HarmonyPatch(typeof(MobArenaComponent),nameof(MobArenaComponent.UpdateArena))]
internal static class SceneArenaSpawnPatch
{private static bool Prefix(MobArenaComponent __instance)=>!ScenePatchGuard.Read(()=>SceneActions.Current?.BlockArena(__instance)==true,false);}
[HarmonyPatch(typeof(CorruptedNogaiInvokeMobFeature),nameof(CorruptedNogaiInvokeMobFeature.SpawnMob))]
internal static class SceneNogaiSpawnPatch
{private static bool Prefix(Entity __0)=>!ScenePatchGuard.Read(()=>SceneActions.Current?.NoArenaSpawn==true&&SceneActions.Current.Enemy(__0,true),false);}
[HarmonyPatch(typeof(MoleInvokeMobFeature),nameof(MoleInvokeMobFeature.SpawnMob))]
internal static class SceneMoleSpawnPatch
{private static bool Prefix(Entity __0)=>!ScenePatchGuard.Read(()=>SceneActions.Current?.NoArenaSpawn==true&&SceneActions.Current.Enemy(__0,true),false);}
[HarmonyPatch(typeof(InvokeSingleMobFeature),nameof(InvokeSingleMobFeature.Spawn))]
internal static class SceneSingleSummonPatch
{internal static bool Prefix(Entity __0,UnityEngine.Vector3 __3)=>!ScenePatchGuard.Read(()=>SceneActions.Current?.BlockSummon(__0,__3)==true,false);}
