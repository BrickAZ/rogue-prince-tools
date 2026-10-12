using RoguePrince.CheatMenu;
using UnityEngine;
internal static class CameraTests
{
 internal static void Run(Action<string,Action> check)
 {
  void Eq<T>(T a,T b){if(!Equals(a,b))throw new Exception($"Expected {b}, got {a}");}
  (GameSession game,SceneCamera controller,CameraTeleportDebug helper) Flight(bool existingFlags=false)
  {UnityEngine.Object.Objects.Clear();CameraTeleportDebug.FailDeactivate=false;CameraTeleportDebug.Deactivated=0;var game=new GameSession();game.Player.God=game.Player.Intangible=existingFlags;var helper=new CameraTeleportDebug{player=new(){CurrentValue=game.Player},cinemachineCamera=new(){CurrentValue=new()}};var controller=new SceneCamera(game);controller.EnableFlight(()=>{});return(game,controller,helper);}
  check("inactive living player still receives native flight restoration",()=>{var(g,c,h)=Flight();g.Player.gameObject.activeInHierarchy=false;c.Restore();Eq(g.Player.Flying,false);Eq(g.Player.God,false);Eq(g.Player.Intangible,false);Eq(c.Active,false);Eq(CameraTeleportDebug.Deactivated,1);});
  check("destroyed flight helper does not retire a living player's flags",()=>{var(g,c,h)=Flight();UnityEngine.Object.Destroy(h);c.Restore();Eq(g.Player.Flying,false);Eq(g.Player.God,false);Eq(c.Active,false);Eq(CameraTeleportDebug.Deactivated,1);});
  check("destroyed brain does not prevent old player flag recovery",()=>{var(g,c,h)=Flight();UnityEngine.Object.Destroy(h.cinemachineCamera.CurrentValue);c.Restore();Eq(g.Player.Flying,false);Eq(g.Player.God,false);Eq(c.Active,false);});
  check("changed shared player variable cannot restore flags to replacement",()=>{var(g,c,h)=Flight();var next=new MotherBase.EntityFramework.Entity{God=true,Intangible=true,Flying=true};h.player.CurrentValue=next;c.Restore();Eq(g.Player.Flying,false);Eq(next.Flying,true);Eq(next.God,true);Eq(h.player.CurrentValue,next);});
  check("failed native restoration keeps lease until successful retry",()=>{var(g,c,h)=Flight();CameraTeleportDebug.FailDeactivate=true;try{c.Restore();throw new Exception("failure swallowed");}catch(AggregateException){}Eq(c.Active,true);CameraTeleportDebug.FailDeactivate=false;c.Restore();Eq(g.Player.Flying,false);Eq(c.Active,false);});
  check("flight restoration preserves flags that existed before takeover",()=>{var(g,c,h)=Flight(true);c.Restore();Eq(g.Player.God,true);Eq(g.Player.Intangible,true);Eq(g.Player.Flying,false);Eq(c.Active,false);});
  check("external removal of pre-existing flags does not strand flight recovery",()=>{var(g,c,h)=Flight(true);g.Player.God=g.Player.Intangible=false;c.Restore();Eq(g.Player.God,false);Eq(g.Player.Intangible,false);Eq(g.Player.Flying,false);Eq(c.Active,false);});
 }
}
