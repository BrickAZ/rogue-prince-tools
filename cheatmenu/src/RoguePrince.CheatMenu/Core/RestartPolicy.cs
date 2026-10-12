// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;
public sealed class RestartGate
{
 public bool Busy {get;private set;}
 public string Result {get;private set;}="idle";
 public int Seed {get;private set;}
 public int? Difficulty {get;private set;}
 public bool StartSubmitted {get;private set;}
 private long owner,saver,oldLevel,oldRun;
 private string origin,hub,target;
 private double deadline;
 private bool generated;
 private long generatedRun;
 public void Begin(long manager,long playerSaver,long level,string state,long run,int seed,int? difficulty,double now)
 {
  if(Busy)throw new InvalidOperationException("Restart already pending");
  if(manager==0||playerSaver==0||level==0||string.IsNullOrWhiteSpace(state)||run<=0||difficulty<0||!double.IsFinite(now))throw new ArgumentException("Invalid restart origin");
  owner=manager;saver=playerSaver;oldLevel=level;origin=state;oldRun=run;Seed=seed;Difficulty=difficulty;deadline=now+120;
  hub=target=null;generated=StartSubmitted=false;Busy=true;Result="pending";
 }
 public void SubmitStart(string hubState,string startState)
 {
  if(!Busy||StartSubmitted||string.IsNullOrWhiteSpace(hubState)||string.IsNullOrWhiteSpace(startState)||hubState==startState)throw new InvalidOperationException("Invalid or repeated native start");
  hub=hubState;target=startState;StartSubmitted=true;
 }
 public void Generated(string state,int seed,long run,bool consumed)
 {
  if(!Busy||!StartSubmitted)return;
  if(state!=target||seed!=Seed||!consumed){Finish("failed");return;}
  generated=run>oldRun;generatedRun=run;
 }
 public string Observe(long manager,long playerSaver,string state,bool loading,bool isHub,bool ready,bool focused,long run,long level,int seed,double now)
 {
  if(!Busy)return Result;
  if(!focused||manager!=owner||playerSaver!=saver||run<oldRun)return Finish("cancelled");
  if(!double.IsFinite(now)||now>=deadline)return Finish("timeout");
  if(loading||!ready)return "pending";
  if(!StartSubmitted)
  {
   if(isHub)return "hub";
   return state==origin?"pending":Finish("cancelled");
  }
  if(state==hub&&isHub)return "pending";
  if(state!=target)return Finish("cancelled");
  if(!generated||run<=oldRun||level==0||level==oldLevel)return "pending";
  if(run!=generatedRun)return Finish("cancelled");
  return Finish(seed==Seed?"completed":"failed");
 }
 public string Cancel()=>Finish("cancelled");
 private string Finish(string result){Busy=false;Result=result;return result;}
}
