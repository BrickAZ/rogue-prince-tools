// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using RoguePrince.CheatMenu.Core;
internal static class CombatTests
{
    internal static void Run(Action<string,Action> check)
    {
        void Equal(double a,double b){if(Math.Abs(a-b)>.00001)throw new Exception($"{a} != {b}");}
        check("damage scales actual loss only",()=>{Equal(CombatPolicy.Health(100,90,2,false,false,false),80);Equal(CombatPolicy.Health(100,110,2,true,true,false),110);});
        check("one hit respects native cannot kill",()=>{Equal(CombatPolicy.Health(100,90,1,true,false,false),0);Equal(CombatPolicy.Health(100,90,1,true,false,true),1);});
        check("undying retains at least one HP",()=>{Equal(CombatPolicy.Health(10,-500,1,false,true,false),1);Equal(CombatPolicy.Health(10,9,1,false,true,false),9);});
        check("nested loss is not counted twice",()=>{var s=new CombatLedger();s.Record(2,20,0);s.Record(2,80,20);Equal(s.Total,80);Equal(s.Last,60);Equal(s.Hits,2);});
        check("invalid and healed damage is ignored",()=>{var s=new CombatLedger();s.Record(1,double.NaN,0);s.Record(1,-2,0);Equal(s.Hits,0);});
        check("selected target has separate stats",()=>{var s=new CombatLedger{Selected=2};s.Record(1,20,0);s.Record(2,30,0);s.Advance(2,true);s.Advance(30,false);Equal(s.Total,50);Equal(s.SelectedTotal,30);Equal(s.Dps,25);});
        check("DPS clock does not run before first hit",()=>{var s=new CombatLedger();s.Advance(50,true);Equal(s.Seconds,0);s.Record(1,10,0);s.Advance(2,true);Equal(s.Seconds,2);});
        check("practice best accepts only completed segments",()=>{var s=new PracticeStatistics();s.Start("A",0);s.Advance(2,true);s.Finish("teleport",false);if(s.Best.ContainsKey("A"))throw new Exception("false best");s.Start("A",1);s.Hit();s.Advance(3,true);s.Finish("complete",true);Equal(s.Best["A"],3);Equal(s.Records.Last().Hits,1);});
        check("wave does not advance with living members",()=>{var w=new WavePlan();w.Start(2,2);w.Submitted(1);w.Submitted(2);if(w.CanSpawn||w.Completed)throw new Exception("overlap");w.Retire(1,true);if(w.CanSpawn)throw new Exception("overlap");w.Retire(2,true);if(!w.CanSpawn||w.Wave!=2)throw new Exception("no next wave");});
        check("wave clear cancels without counting as a kill",()=>{var w=new WavePlan();w.Start(2,1);w.Submitted(1);w.Retire(1,false);if(w.Active||w.Completed)throw new Exception("false completion");});
        check("wave finishes exact final member",()=>{var w=new WavePlan();w.Start(1,1);w.Submitted(1);w.Retire(1,true);if(!w.Completed||w.Active)throw new Exception("not finished");});
        check("full loadout schema rejects future version",()=>{var p=new LoadoutPreset{Name="A",Main=new(){Name="M",ItemId="m",Tier=1,AffixIds=new()},Tool=new(){Name="T",ItemId="t",Tier=1,AffixIds=new()}};p.Validate();p.Version=2;try{p.Validate();throw new Exception("accepted");}catch(ArgumentException){} });
        check("attack step leaves actor clock unchanged",()=>{var p=new ActionStepPlan(new[]{(4,2,true),(3,2,false)});if(p.Next(0)!=false||p.Next(2)!=true||p.Next(2)!=false||p.Next(2)!=false)throw new Exception("step scope");});
        check("attack step fails closed on sequence mismatch",()=>{var p=new ActionStepPlan(new[]{(4,2,true),(3,2,true)});p.Next(0);if(p.Next(9)||p.Next(2))throw new Exception("wrong association");});
        check("tool cost bypass requires every exact scope guard",()=>{for(int mask=0;mask<32;mask++){var actual=CombatPolicy.ToolCostBypass((mask&1)!=0,(mask&2)!=0,(mask&4)!=0,(mask&8)!=0,(mask&16)!=0);if(actual!=(mask==31))throw new Exception("tool scope escaped "+mask);}});
        check("practice zero duration cannot become a best time",()=>{var p=new PracticeStatistics();p.Start("wave",0);p.Finish("complete",true);p.Confirm(new("room",0,0,"exit", ""));if(p.Best.Count!=0||p.Records.Count!=2)throw new Exception("zero best");});
    }
}
