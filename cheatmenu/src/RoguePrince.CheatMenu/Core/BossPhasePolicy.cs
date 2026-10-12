// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;
public readonly record struct BossPhaseBinding(TrainingScope Scope,long Run,long Boss,long Component,long Brain,long Flow,long Level,string State,int Seed)
{
 public bool Valid=>Scope.Valid&&Run>0&&Boss!=0&&Component!=0&&Brain!=0&&Flow!=0&&Level!=0&&!string.IsNullOrEmpty(State);
}
public static class BossPhasePolicy
{
 public static float TargetHealth(int phase,float max,float current,float ratio,bool initialized,bool reached)
 {
  if(!initialized||reached||phase is not (1 or 2)||!float.IsFinite(max)||!float.IsFinite(current)||!float.IsFinite(ratio)||max<=0||current<=0||current>max||ratio<=0||ratio>=1)
   throw new ArgumentException(MenuText.Get("当前阶段不支持推进，未执行"));
  var target=max*ratio;
  if(!float.IsFinite(target)||target<1||current<=target+.01f)throw new ArgumentException(MenuText.Get("阶段阈值已到达或无效，未执行"));
  return target;
 }
}
public sealed class BossPhaseGate
{
 private BossPhaseBinding owner;private int from;private double deadline;
 public bool Pending{get;private set;}
 public void Begin(BossPhaseBinding binding,int phase,double now)
 {
  if(Pending||!binding.Valid||phase is not (1 or 2)||!double.IsFinite(now))throw new ArgumentException(MenuText.Get("阶段请求尚在观察或目标已失效"));
  owner=binding;from=phase;deadline=now+30;Pending=true;
 }
 public string Observe(BossPhaseBinding binding,int phase,bool alive,double now)
 {
  if(!Pending)return "idle";
  string result= !alive||!binding.Valid||binding!=owner ? "cancelled" : phase==from+1 ? "confirmed" : phase!=from ? "unexpected" : !double.IsFinite(now)||now>=deadline ? "timeout" : "pending";
  if(result!="pending")Pending=false;return result;
 }
 public void Cancel()=>Pending=false;
}
public static class BossPhaseAdmission
{
 public static T Execute<T>(bool observing,Func<T> work)
 {
  if(observing)throw new InvalidOperationException(MenuText.Get("先完成当前请求或恢复操作"));
  return work();
 }
}
