// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using System.Globalization;
namespace RoguePrince.CheatMenu.Core;
public sealed class SceneReloadGate
{
 public bool Active {get;private set;}
 public string Target {get;private set;}
 private long oldInstance;
 private double deadline;
 public void Begin(string target,long instance,double now)
 {if(Active)throw new InvalidOperationException("Scene request already pending");if(string.IsNullOrWhiteSpace(target)||instance==0||!double.IsFinite(now))throw new ArgumentException("Invalid scene identity");Target=target;oldInstance=instance;deadline=now+60;Active=true;}
 public string Observe(string state,long instance,bool loading,bool ready,double now)
 {
  if(!Active)return "idle";
  if(now>=deadline || (!loading&&ready&&state!=Target)){Cancel();return "failed";}
  if(!loading&&ready&&state==Target&&instance!=0&&instance!=oldInstance){Cancel();return "completed";}
  return "pending";
 }
 public void Cancel()=>Active=false;
}
public sealed class RunStartReservation
{
 public bool Pending {get;private set;}
 public int Seed {get;private set;}
 public int? Difficulty {get;private set;}
 private long manager;
 private bool resetSeen;
 public void Arm(long owner,int seed,int? difficulty)
 {if(owner==0||difficulty<0)throw new ArgumentException("Invalid run reservation");manager=owner;Seed=seed;Difficulty=difficulty;Pending=true;resetSeen=false;}
 public void ObserveReset(long owner){if(Pending&&owner==manager)resetSeen=true;}
 public bool ShouldApply(long owner,bool startBiome)=>Pending&&owner==manager&&startBiome&&resetSeen;
 public void Consume(long owner,bool startBiome){if(ShouldApply(owner,startBiome))Pending=false;}
 public void Cancel()=>Pending=false;
}
public static class SceneOverlayPolicy
{
 public static bool CanDraw(bool enabled,bool photo,bool focused,bool ready,bool repaint)=>enabled&&!photo&&focused&&ready&&repaint;
}
public static class SceneValuePolicy
{
 public static int Seed(string value)=>int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out var n)?n:throw new ArgumentException(MenuText.Get("种子必须是 32 位有符号整数"));
 public static int Difficulty(string value,int nativeMax)=>int.TryParse(value,out var n)&&n>=0&&n<=nativeMax?n:throw new ArgumentException(MenuText.Get("觉醒等级超出当前游戏范围"));
}
public sealed class CrossMapPoint
{
 public long Run {get;}
 public int Seed {get;}
 public string State {get;}
 public string Topology {get;}
 public float X {get;}public float Y {get;}public float Z {get;}
 public CrossMapPoint(long run,int seed,string state,string topology,float x,float y,float z)
 {if(run<=0||string.IsNullOrWhiteSpace(state)||string.IsNullOrWhiteSpace(topology)||!float.IsFinite(x)||!float.IsFinite(y)||!float.IsFinite(z))throw new ArgumentException("Invalid cross map bookmark");Run=run;Seed=seed;State=state;Topology=topology;X=x;Y=y;Z=z;}
 public bool CanReturn(long run,int seed,string state,string topology)=>Run==run&&Seed==seed&&State==state&&Topology==topology;
}
public sealed class EncounterReservation
{
 public bool Pending {get;private set;}
 private long manager;private string source,target;private HashSet<long> signals=new();
 public void Arm(long owner,string from,string to,IEnumerable<long> exits)
 {var keys=exits.Where(x=>x!=0).ToHashSet();if(owner==0||string.IsNullOrWhiteSpace(from)||string.IsNullOrWhiteSpace(to)||from==to||keys.Count==0)throw new ArgumentException("Invalid encounter reservation");manager=owner;source=from;target=to;signals=keys;Pending=true;}
 public string Select(long owner,string from,long signal)=>Pending&&owner==manager&&from==source&&signals.Contains(signal)?target:null;
 public string Load(long owner,string destination){if(!Pending||owner!=manager)return "idle";Pending=false;return destination==target?"consumed":"cancelled";}
 public void Cancel()=>Pending=false;
}
