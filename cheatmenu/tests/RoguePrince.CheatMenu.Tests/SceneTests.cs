using RoguePrince.CheatMenu.Core;
internal static class SceneTests
{
 internal static void Run(Action<string,Action> check)
 {
  void Eq<T>(T a,T b){if(!Equals(a,b))throw new Exception($"Expected {b}, got {a}");}
  check("same guid reload never accepts old level instance",()=>{var g=new SceneReloadGate();g.Begin("boss",11,0);Eq(g.Observe("boss",11,false,true,1),"pending");Eq(g.Observe("boss",12,false,true,2),"completed");});
  check("reload waits for destination readiness",()=>{var g=new SceneReloadGate();g.Begin("boss",11,0);Eq(g.Observe("boss",12,true,true,1),"pending");Eq(g.Observe("boss",12,false,false,2),"pending");Eq(g.Observe("other",13,false,true,3),"failed");});
  check("reload times out without automatic retry",()=>{var g=new SceneReloadGate();g.Begin("boss",11,0);Eq(g.Observe("boss",11,false,true,61),"failed");Eq(g.Active,false);});
  check("reload rejects invalid destination or repeated request",()=>{var g=new SceneReloadGate();try{g.Begin("",1,0);throw new Exception("accepted invalid");}catch(ArgumentException){}g.Begin("a",1,0);try{g.Begin("b",1,0);throw new Exception("accepted busy");}catch(InvalidOperationException){}Eq(g.Target,"a");});
  check("seed reservation is manager bound and ignores ordinary biome",()=>{var p=new RunStartReservation();p.Arm(5,-2147483648,0);Eq(p.ShouldApply(5,true),false);p.ObserveReset(6);Eq(p.ShouldApply(5,true),false);p.ObserveReset(5);Eq(p.ShouldApply(6,true),false);Eq(p.ShouldApply(5,false),false);Eq(p.ShouldApply(5,true),true);Eq(p.Seed,int.MinValue);p.Consume(5,true);Eq(p.Pending,false);});
  check("failed seed write remains cancellable",()=>{var p=new RunStartReservation();p.Arm(5,42,2);p.ObserveReset(5);Eq(p.ShouldApply(5,true),true);Eq(p.Pending,true);p.Cancel();Eq(p.ShouldApply(5,true),false);});
  check("disabled and photo overlays draw nothing",()=>{Eq(SceneOverlayPolicy.CanDraw(false,false,true,true,true),false);Eq(SceneOverlayPolicy.CanDraw(true,true,true,true,true),false);Eq(SceneOverlayPolicy.CanDraw(true,false,false,true,true),false);Eq(SceneOverlayPolicy.CanDraw(true,false,true,true,true),true);});
  check("seed parse accepts signed int32 only",()=>{Eq(SceneValuePolicy.Seed("-123"),-123);foreach(var x in new[]{"","1.2","2147483648","NaN"})try{SceneValuePolicy.Seed(x);throw new Exception("accepted invalid");}catch(ArgumentException){}});
  check("awakening range uses native maximum",()=>{Eq(SceneValuePolicy.Difficulty("3",4),3);foreach(var x in new[]{"-1","5","bad"})try{SceneValuePolicy.Difficulty(x,4);throw new Exception("accepted invalid");}catch(ArgumentException){}});
  check("cross map bookmark requires same run seed and complete topology",()=>{var p=new CrossMapPoint(7,42,"map","fingerprint",10,20,0);Eq(p.CanReturn(7,42,"map","fingerprint"),true);Eq(p.CanReturn(8,42,"map","fingerprint"),false);Eq(p.CanReturn(7,43,"map","fingerprint"),false);Eq(p.CanReturn(7,42,"other","fingerprint"),false);Eq(p.CanReturn(7,42,"map","different"),false);});
  check("cross map point rejects empty topology or non finite coordinate",()=>{foreach(var fp in new[]{"",null})try{new CrossMapPoint(1,2,"m",fp,1,2,3);throw new Exception("invalid accepted");}catch(ArgumentException){}try{new CrossMapPoint(1,2,"m","f",float.NaN,2,3);throw new Exception("nan accepted");}catch(ArgumentException){}});
  check("encounter applies only bound accepted exit and cancels other loads",()=>{var p=new EncounterReservation();p.Arm(5,"from","boss",new[]{6L,7L});Eq(p.Select(5,"from",6),"boss");Eq(p.Select(5,"from",8),null);Eq(p.Select(6,"from",6),null);Eq(p.Select(5,"other",6),null);Eq(p.Load(5,"other"),"cancelled");Eq(p.Pending,false);});
  check("encounter consumes once and refuses no valid exits",()=>{var p=new EncounterReservation();try{p.Arm(1,"a","b",Array.Empty<long>());throw new Exception("empty accepted");}catch(ArgumentException){}p.Arm(1,"a","b",new[]{2L});Eq(p.Load(1,"b"),"consumed");Eq(p.Select(1,"a",2),null);});
 }
}
