// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;
public static class CombatPolicy
{
    public static float Multiplier(float v,float min=.1f,float max=10f)
    {if(!float.IsFinite(v)||v<min||v>max)throw new ArgumentException(MenuText.Get("倍率超出范围"));return v;}
    public static bool ToolCostBypass(bool enabled,bool scoped,bool player,bool currentTool,bool energyReady)
        =>enabled&&scoped&&player&&currentTool&&energyReady;
    public static float Health(float before,float requested,float multiplier,bool oneHit,bool undying,bool cannotKill)
    {
        if(!float.IsFinite(before)||!float.IsFinite(requested)||before<=0||requested>=before)return requested;
        var result=oneHit?0:before-(before-requested)*multiplier;
        if(undying||cannotKill)result=Math.Max(1,result);
        return float.IsFinite(result)?result:requested;
    }
}
public sealed class CombatLedger
{
    public DamageStatistics Details {get;}=new();
    public long Selected {get;set;}
    public double Total {get;private set;}
    public double Last {get;private set;}
    public long Hits {get;private set;}
    public double SelectedTotal {get;private set;}
    public double Seconds {get;private set;}
    public double Dps=>Seconds>0?Total/Seconds:0;
    public void Record(long target,double loss,double nested)
    {var own=loss-nested;if(!double.IsFinite(own)||own<=0)return;Last=own;Total+=own;Hits++;if(target==Selected)SelectedTotal+=own;}
    public void RecordDetailed(long target,double loss,double nested,DamageCredit credit,long generation,int frame)
    {if(Details.Record(target,loss,nested,credit,generation,frame))Record(target,loss,nested);}
    public void Advance(double delta,bool active){if(Hits>0&&active&&double.IsFinite(delta)&&delta>0&&delta<1)Seconds+=delta;else if(Hits>0&&active&&delta>=1&&delta<=60)Seconds+=delta;}
    public void Reset(){Total=Last=SelectedTotal=Seconds=0;Hits=0;Details.Clear();}
    public void Select(long target){Selected=target;SelectedTotal=0;}
}
public sealed class ActionStepPlan
{
    private readonly (int id,int frame,bool attack)[] actions;
    private int index=-1;
    private bool invalid;
    public ActionStepPlan(IEnumerable<(int id,int frame,bool attack)> actions)=>this.actions=actions.ToArray();
    public bool Next(int frame)
    {
        if(index++<0)return false; // Actor clock is always first.
        var i=index-1;
        if(invalid||i>=actions.Length)return false;
        if(actions[i].frame!=frame){invalid=true;return false;}
        return actions[i].attack;
    }
}
public sealed class WavePlan
{
    public bool Active {get;private set;}
    public bool Completed {get;private set;}
    public int Wave {get;private set;}
    public int Waves {get;private set;}
    public int PerWave {get;private set;}
    public int Spawned {get;private set;}
    public readonly HashSet<long> Living=new();
    public bool CanSpawn=>Active&&Spawned<PerWave;
    public void Start(int waves,int perWave){if(waves<1||waves>20||perWave<1||perWave>10)throw new ArgumentException(MenuText.Get("波次 1–20，每波 1–10 只"));Living.Clear();Wave=1;Waves=waves;PerWave=perWave;Spawned=0;Completed=false;Active=true;}
    public void Submitted(long id){if(!CanSpawn||!Living.Add(id))throw new InvalidOperationException("Wave submission mismatch");Spawned++;}
    public void Retire(long id,bool killed){if(!Active||!Living.Remove(id))return;if(!killed){Cancel();return;}if(Living.Count==0&&Spawned==PerWave){if(Wave==Waves){Active=false;Completed=true;}else{Wave++;Spawned=0;}}}
    public void Cancel(){Active=false;Completed=false;Living.Clear();}
}
public sealed class PracticeRecord
{
    public string Segment {get;} public double Seconds {get;} public int Hits {get;} public string Result {get;} public string Location {get;}
    public PracticeRecord(string segment,double seconds,int hits,string result,string location){Segment=segment;Seconds=seconds;Hits=hits;Result=result;Location=location;}
}
public sealed class PracticeStatistics
{
    public readonly List<PracticeRecord> Records=new();
    public readonly Dictionary<string,double> Best=new();
    public string Segment {get;private set;}
    public double Seconds {get;private set;}
    public int Hits {get;private set;}
    public bool Running=>Segment!=null;
    public void Start(string segment,double unused){Segment=segment;Seconds=0;Hits=0;}
    public void Advance(double delta,bool active){if(Running&&active&&double.IsFinite(delta)&&delta>0&&delta<=60)Seconds+=delta;}
    public void Hit(){if(Running)Hits++;}
    public void Finish(string result,bool completed,string location="")
    {if(!Running)return;Records.Add(new(Segment,Seconds,Hits,result,location));if(Records.Count>64)Records.RemoveAt(0);if(completed&&double.IsFinite(Seconds)&&Seconds>0&&(!Best.TryGetValue(Segment,out var best)||Seconds<best))Best[Segment]=Seconds;Segment=null;}
    public void Confirm(PracticeRecord pending)
    {if(pending==null)return;Records.Add(new(pending.Segment,pending.Seconds,pending.Hits,"native exit",pending.Location));if(Records.Count>64)Records.RemoveAt(0);if(double.IsFinite(pending.Seconds)&&pending.Seconds>0&&(!Best.TryGetValue(pending.Segment,out var best)||pending.Seconds<best))Best[pending.Segment]=pending.Seconds;}
    public PracticeRecord Snapshot(string result,string location)=>Running?new(Segment,Seconds,Hits,result,location):null;
    public void Reset(){Segment=null;Seconds=0;Hits=0;Records.Clear();Best.Clear();}
}
