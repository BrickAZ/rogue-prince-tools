using RoguePrince.CheatMenu.Core;
int passed=0;
void Check(bool value,string name){if(!value)throw new Exception(name);passed++;}
var n=new Native();var b=new BossSpawnBootstrap(n);
Check(!b.Tick(1,true,true),"wait for native intro exit");
Check(string.Join(",",n.Calls)=="prepare,init,stop-wait,enter","initialize before intro exit and before HP");
Check(n.HealthWrites==0,"no HP write while phase zero");
n.Phase=1;
Check(!b.Tick(2,true,true),"health readback deferred");
Check(n.HealthWrites==1&&n.Calls[^1]=="health","HP after native initialization and combat entry");
Check(!b.Tick(2,true,true)&&!n.Calls.Contains("release"),"same frame cannot release AI");
Check(!b.Tick(3,true,true)&&n.Calls[^1]=="release","release AI after readback");
Check(!b.Tick(4,true,true),"phase and alive HP do not prove attack");
n.Attack=true;
Check(b.Tick(5,true,true)&&b.AttackObserved,"native attack completes combat readiness");
Check(n.HealthWrites==1&&n.Calls.Count(x=>x=="init")==1,"no repeated initialization or HP reset");
n=new Native{Phase=1};b=new(n);
b.Tick(1,true,false);Check(!b.Tick(2,true,false),"AI off still requires readback");
Check(b.Tick(3,true,false)&&!b.AttackObserved,"AI off is initialized, not claimed as fighting");
n=new Native{Phase=1};b=new(n);b.Tick(1,true,true);b.Tick(2,true,true);n.Readback=false;
try{b.Tick(3,true,true);throw new Exception("missing health failure");}catch(InvalidOperationException){passed++;}
Check(!n.Calls.Contains("release"),"failed HP readback holds AI");
n=new Native();b=new(n);Check(!b.Tick(1,false,true)&&n.Calls.Count==0,"not-ready entity has no side effects");
GraphTests.Run(Check);
Check(BossSpawnLayout.Map(1.5f,1.5f,31.5f,-5,5)==-5,"native arena left edge maps inside summon host");
Check(BossSpawnLayout.Map(31.5f,1.5f,31.5f,-5,5)==5,"native arena right edge maps inside summon host");
Check(BossSpawnLayout.Map(16.5f,1.5f,31.5f,-5,5)==0,"native arena center becomes host-relative zero");
Check(BossSpawnLayout.Map(13.5f,8.5f,13.5f,1,7)==7,"original high wall stays below current room ceiling");
Check(BossSpawnLayout.Map(99,1.5f,31.5f,-5,5)==5,"outlying wall point cannot escape room bounds");
Check(BossSpawnLayout.Map(2,2,2,1,7)==4,"zero-width native axis maps to center");
try{BossSpawnLayout.Map(float.NaN,0,1,0,1);throw new Exception("invalid wall coordinate accepted");}catch(InvalidOperationException){passed++;}
Console.WriteLine($"RESULT {passed} passed, 0 failed (Boss spawn bootstrap and native asset graph)");
sealed class Native:IBossSpawnNative
{
 public List<string> Calls=new();public int Phase,HealthWrites;public bool Attack,Readback=true;
 public void Prepare()=>Calls.Add("prepare");public void Initialize()=>Calls.Add("init");
 public void StopWaiting()=>Calls.Add("stop-wait");public void EnterCombat()=>Calls.Add("enter");
 public bool CombatEntered=>Phase>0;public bool NativeAttackActive=>Attack;
 public void WriteHealth(){Calls.Add("health");HealthWrites++;}
 public bool HealthMatches=>Readback;public void ReleaseAi()=>Calls.Add("release");
}
