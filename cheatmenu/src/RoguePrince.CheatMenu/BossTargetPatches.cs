// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using HarmonyLib;
using MotherBase.EntityFramework;
using MotherBase.ActionFramework;
namespace RoguePrince.CheatMenu;
internal static class BossTargetGuard
{
 internal static bool Block(Func<BossTargetActions,bool> action)
 {
  var current=BossTargetActions.Current;if(current?.Enabled!=true)return false;
  try{return action(current);}catch(Exception e){current.Disable(e);return false;}
 }
}
[HarmonyPatch(typeof(NogaiBossBrain),nameof(NogaiBossBrain.OrientateTowardTarget))]
internal static class NogaiTargetOrientationPatch
{internal static bool Prefix(NogaiBossBrain __instance,Entity __0,Entity __1)=>__1==null||!BossTargetGuard.Block(a=>a.Block(__0,__instance.Pointer,0,explicitTarget:__1));}
[HarmonyPatch(typeof(NogaiBossBrain),nameof(NogaiBossBrain.WalkToTarget))]
internal static class NogaiTargetWalkPatch
{internal static bool Prefix(NogaiBossBrain __instance,Entity __0,TargetComponent __1,MoveIntent3D __2)=>__1==null||__2==null||!BossTargetGuard.Block(a=>a.Block(__0,__instance.Pointer,0,target:__1,move:__2,stopMovement:true));}
[HarmonyPatch(typeof(NogaiBossBrain),nameof(NogaiBossBrain.TryStartNextAttack))]
internal static class NogaiTargetAttackPatch
{
 internal static bool Prefix(NogaiBossBrain __instance,Entity __0,NogaiBossComponent __2,ref bool __result)
 {if(__2==null||!BossTargetGuard.Block(a=>a.Block(__0,__instance.Pointer,0,component:__2)))return true;__result=false;return false;}
}
[HarmonyPatch(typeof(KaaliBossBrain),nameof(KaaliBossBrain.OrientateTowardTarget))]
internal static class KaaliTargetOrientationPatch
{internal static bool Prefix(KaaliBossBrain __instance,Entity __0,Entity __1)=>__1==null||!BossTargetGuard.Block(a=>a.Block(__0,__instance.Pointer,1,explicitTarget:__1));}
[HarmonyPatch(typeof(KaaliBossBrain),nameof(KaaliBossBrain.MeleeBehaviour))]
internal static class KaaliTargetMeleePatch
{internal static bool Prefix(KaaliBossBrain __instance,Entity __0,KaaliBossComponent __2)=>__2==null||!BossTargetGuard.Block(a=>a.Block(__0,__instance.Pointer,1,component:__2,stopMovement:true));}
[HarmonyPatch(typeof(KaaliBossBrain),nameof(KaaliBossBrain.RangePhaseBehaviour))]
internal static class KaaliTargetRangePatch
{internal static bool Prefix(KaaliBossBrain __instance,Entity __0,KaaliBossComponent __2,Entity __3)=>__2==null||__3==null||!BossTargetGuard.Block(a=>a.Block(__0,__instance.Pointer,1,component:__2,explicitTarget:__3,stopMovement:true));}
[HarmonyPatch(typeof(BaatarBossBrain),nameof(BaatarBossBrain.TryStartNextAttack))]
internal static class BaatarTargetAttackPatch
{
 internal static bool Prefix(BaatarBossBrain __instance,Entity __0,MoveIntent3D __2,BaatarBossComponent __3,ref bool __result)
 {if(__2==null||__3==null||!BossTargetGuard.Block(a=>a.Block(__0,__instance.Pointer,2,component:__3,move:__2)))return true;__result=false;return false;}
}
[HarmonyPatch(typeof(CorruptedNogaiBrain),nameof(CorruptedNogaiBrain.TryStartNextAttack))]
internal static class CorruptedNogaiTargetAttackPatch
{
 internal static bool Prefix(CorruptedNogaiBrain __instance,Entity __0,CorruptedNogaiBossComponent __2,ref bool __result)
 {if(__2==null||!BossTargetGuard.Block(a=>a.Block(__0,__instance.Pointer,3,component:__2)))return true;__result=false;return false;}
}
[HarmonyPatch(typeof(AltanBossBrain),nameof(AltanBossBrain.FollowTimeline))]
internal static class AltanTargetAttackPatch
{
 internal static bool Prefix(AltanBossBrain __instance,Entity __0,AltanBossComponent __2,MoveIntent3D __3)
 {return __2==null||__3==null||!BossTargetGuard.Block(a=>a.Block(__0,__instance.Pointer,4,component:__2,move:__3));}
}
[HarmonyPatch(typeof(GoatBossBrain),nameof(GoatBossBrain.TriggerFillerAction))]
internal static class BerudeTargetFillerPatch
{
 internal static bool Prefix(GoatBossBrain __instance,Entity __0,AIStateComponent __2,TargetComponent __3,ref bool __result)
 {if(__2==null||__3==null||!BossTargetGuard.Block(a=>a.Block(__0,__instance.Pointer,5,target:__3,ai:__2)))return true;__result=false;return false;}
}
