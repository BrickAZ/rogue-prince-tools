using RoguePrince.CheatMenu.Core;
internal static class BossPhaseTests
{
 internal static void Run(Action<string,Action> check)
 {
  void Eq<T>(T a,T b){if(!Equals(a,b))throw new Exception($"Expected {b}, got {a}");}
  void Refuse(Action a){try{a();throw new Exception("Unsafe request accepted");}catch(ArgumentException){}}
  var bind=new BossPhaseBinding(new(1,2,3,4,5,6),7,8,9,10,11,12,"boss",42);
  check("boss phase native threshold needs living non-final initialized stage",()=>{Eq(BossPhasePolicy.TargetHealth(1,100,90,.66f,true,false),66f);foreach(var phase in new[]{-1,0,3,4})Refuse(()=>BossPhasePolicy.TargetHealth(phase,100,90,.5f,true,false));Refuse(()=>BossPhasePolicy.TargetHealth(1,100,90,.5f,false,false));Refuse(()=>BossPhasePolicy.TargetHealth(1,100,90,.5f,true,true));});
  check("boss phase rejects lethal or invalid thresholds and spent threshold",()=>{foreach(var r in new[]{0f,1f,-1f,float.NaN,float.PositiveInfinity})Refuse(()=>BossPhasePolicy.TargetHealth(1,100,90,r,true,false));Refuse(()=>BossPhasePolicy.TargetHealth(1,100,.4f,.001f,true,false));Refuse(()=>BossPhasePolicy.TargetHealth(1,100,50,.5f,true,false));Refuse(()=>BossPhasePolicy.TargetHealth(1,100,101,.5f,true,false));});
  check("boss phase request cannot be repeated while observing",()=>{var g=new BossPhaseGate();g.Begin(bind,1,0);Refuse(()=>g.Begin(bind,1,1));Eq(g.Pending,true);Eq(g.Observe(bind,1,true,1),"pending");});
  check("boss phase confirms only exact next native phase",()=>{var g=new BossPhaseGate();g.Begin(bind,1,0);Eq(g.Observe(bind,2,true,1),"confirmed");Eq(g.Pending,false);g.Begin(bind,1,2);Eq(g.Observe(bind,3,true,3),"unexpected");});
  check("boss phase never confirms a replaced native owner",()=>{foreach(var b in new[]{bind with{Boss=99},bind with{Brain=99},bind with{Level=99},bind with{Run=99},bind with{Seed=99},bind with{State="other"},bind with{Scope=new(2,2,3,4,5,6)}}){var g=new BossPhaseGate();g.Begin(bind,1,0);Eq(g.Observe(b,2,true,1),"cancelled");Eq(g.Pending,false);}});
  check("boss phase death or focus cancellation never sends another damage",()=>{var g=new BossPhaseGate();g.Begin(bind,1,0);Eq(g.Observe(bind,2,false,1),"cancelled");g.Begin(bind,1,2);g.Cancel();Eq(g.Pending,false);Eq(g.Observe(bind,2,true,3),"idle");});
  check("boss phase observation timeout retires without retry",()=>{var g=new BossPhaseGate();g.Begin(bind,2,0);Eq(g.Observe(bind,2,true,31),"timeout");Eq(g.Pending,false);Eq(g.Observe(bind,3,true,32),"idle");});
  check("boss phase refuses empty binding and nonfinite clock",()=>{var g=new BossPhaseGate();Refuse(()=>g.Begin(bind with{Boss=0},1,0));Refuse(()=>g.Begin(bind,1,double.NaN));Eq(g.Pending,false);});
  check("shared phase admission blocks conflicting work before side effects",()=>{var calls=0;foreach(var name in new[]{"batch","wave","bookmark","spawn","hotkeySpawn"}){try{BossPhaseAdmission.Execute(true,()=>{calls++;return name;});throw new Exception("Admission bypassed");}catch(InvalidOperationException){}Eq(calls,0);}Eq(BossPhaseAdmission.Execute(false,()=>{calls++;return "spawn";}),"spawn");Eq(calls,1);});
 }
}
