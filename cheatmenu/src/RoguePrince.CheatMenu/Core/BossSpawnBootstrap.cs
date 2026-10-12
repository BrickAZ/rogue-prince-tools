// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;

internal static class BossSpawnLayout
{
 internal static float Map(float value,float sourceMin,float sourceMax,float targetMin,float targetMax)
 {
  if(!float.IsFinite(value)||!float.IsFinite(sourceMin)||!float.IsFinite(sourceMax)||!float.IsFinite(targetMin)||!float.IsFinite(targetMax)||sourceMax<sourceMin||targetMax<targetMin)
   throw new InvalidOperationException("Boss arena coordinates are invalid");
  var fraction=sourceMax-sourceMin<.001f?.5f:Math.Clamp((value-sourceMin)/(sourceMax-sourceMin),0,1);
  return targetMin+fraction*(targetMax-targetMin);
 }
}

internal interface IBossSpawnNative
{
 void Prepare(); void Initialize(); void StopWaiting(); void EnterCombat();
 bool CombatEntered {get;}
 void WriteHealth(); bool HealthMatches {get;}
 void ReleaseAi(); bool NativeAttackActive {get;}
}

// This sequence is also used by the live native adapter. A living prefab with HP
// is insufficient: native brain initialization and intro exit precede HP, and an
// autonomous native attack must be seen before reporting an AI-enabled Boss ready.
internal sealed class BossSpawnBootstrap
{
 private readonly IBossSpawnNative native;
 private int stage,healthFrame;
 internal bool AiReleased=>stage>=3;
 internal bool AttackObserved {get;private set;}
 internal BossSpawnBootstrap(IBossSpawnNative native)=>this.native=native;
 internal bool Tick(int frame,bool initialized,bool aiEnabled)
 {
  if(!initialized)return false;
  if(stage==0)
  {
   native.Prepare();native.Initialize();native.StopWaiting();native.EnterCombat();
   stage=1;return false;
  }
  if(stage==1)
  {
   if(!native.CombatEntered)return false;
   native.WriteHealth();healthFrame=frame;stage=2;return false;
  }
  if(stage==2)
  {
   if(frame==healthFrame)return false;
   if(!native.HealthMatches)throw new InvalidOperationException("Boss birth health changed after native initialization");
   native.ReleaseAi();stage=3;return !aiEnabled;
  }
  if(!aiEnabled)return true;
  AttackObserved|=native.NativeAttackActive;
  return AttackObserved;
 }
}
