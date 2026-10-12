using RoguePrince.CheatMenu.Core;
internal static class RestartTests
{
 internal static void Run(Action<string,Action> check)
 {
  void Eq<T>(T a,T b){if(!Equals(a,b))throw new Exception($"expected {b}, got {a}");}
  RestartGate New(){var g=new RestartGate();g.Begin(10,20,30,"old",4,42,2,0);return g;}
  check("restart cannot complete from HP loss or loaded old state",()=>{var g=New();Eq(g.Observe(10,20,"old",false,false,true,true,4,30,42,1),"pending");Eq(g.Busy,true);});
  check("restart waits for a living loaded native hub",()=>{var g=New();Eq(g.Observe(10,20,"hub",true,true,false,true,4,40,42,1),"pending");Eq(g.Observe(10,20,"hub",false,true,false,true,4,40,42,2),"pending");Eq(g.Observe(10,20,"hub",false,true,true,true,4,40,42,3),"hub");});
  check("restart submit start exactly once before reentrant generation",()=>{var g=New();g.SubmitStart("hub","first");g.Generated("first",42,5,true);Eq(g.Observe(10,20,"first",false,false,true,true,5,50,42,2),"completed");Eq(g.Busy,false);});
  check("restart rejects a second start submission",()=>{var g=New();g.SubmitStart("hub","first");var threw=false;try{g.SubmitStart("hub","first");}catch(InvalidOperationException){threw=true;}Eq(threw,true);});
  check("restart generation receipt alone is not loaded playability",()=>{var g=New();g.SubmitStart("hub","first");g.Generated("first",42,5,true);Eq(g.Observe(10,20,"first",true,false,true,true,5,50,42,1),"pending");Eq(g.Observe(10,20,"first",false,false,false,true,5,50,42,2),"pending");});
  check("restart needs reset and a new map instance",()=>{var g=New();g.SubmitStart("hub","first");g.Generated("first",42,4,true);Eq(g.Observe(10,20,"first",false,false,true,true,4,50,42,1),"pending");g.Generated("first",42,5,true);Eq(g.Observe(10,20,"first",false,false,true,true,5,30,42,2),"pending");});
  check("restart mismatched generator seed cancels",()=>{var g=New();g.SubmitStart("hub","first");g.Generated("first",43,5,true);Eq(g.Busy,false);Eq(g.Result,"failed");});
  check("restart cannot accept an unconsumed reservation",()=>{var g=New();g.SubmitStart("hub","first");g.Generated("first",42,5,false);Eq(g.Busy,false);});
  check("restart foreign owner or lost focus cancels observation",()=>{foreach(var row in new[]{(11L,20L,true),(10L,21L,true),(10L,20L,false)}){var g=New();Eq(g.Observe(row.Item1,row.Item2,"hub",false,true,true,row.Item3,4,40,42,1),"cancelled");Eq(g.Busy,false);}});
  check("restart seed drift after generation cannot confirm",()=>{var g=New();g.SubmitStart("hub","first");g.Generated("first",42,5,true);Eq(g.Observe(10,20,"first",false,false,true,true,5,50,43,2),"failed");});
  check("restart timeout or nonfinite clock never retries",()=>{foreach(var now in new[]{120.0,double.NaN,double.PositiveInfinity}){var g=New();Eq(g.Observe(10,20,"old",false,false,true,true,4,30,42,now),"timeout");Eq(g.Busy,false);}});
  check("restart unrelated ready destination cancels",()=>{var g=New();Eq(g.Observe(10,20,"other",false,false,true,true,4,50,42,1),"cancelled");});
  check("restart rejects overlapping requests",()=>{var g=New();var threw=false;try{g.Begin(10,20,30,"old",4,42,null,1);}catch(InvalidOperationException){threw=true;}Eq(threw,true);});
  check("restart generation receipt belongs to the exact reset epoch",()=>{var g=New();g.SubmitStart("hub","first");g.Generated("first",42,5,true);Eq(g.Observe(10,20,"first",false,false,true,true,6,50,42,2),"cancelled");});
 }
}
