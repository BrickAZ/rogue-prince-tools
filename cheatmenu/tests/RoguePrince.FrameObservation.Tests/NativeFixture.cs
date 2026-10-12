using RoguePrince.CheatMenu.Core;
using MotherBase.EntityFramework;
namespace UnityEngine
{
 public static class Time {public static int frameCount=100;public static float timeScale=1,deltaTime=.017f,realtimeSinceStartup=20;}
 public static class Application {public static bool isFocused=true;}
 public class Object {public static GameStateStack[] Stacks;public static T[] FindObjectsOfType<T>()=>Stacks.Cast<T>().ToArray();}
 public class GameObject {public bool activeInHierarchy=true;}
}
public class NativeObject {static long serial=10;public IntPtr Pointer=(IntPtr)(++serial);public T TryCast<T>() where T:class=>this as T;}
namespace MotherBase.EntityFramework {public class EntityContext:NativeObject {public bool Paused;public bool IsPaused()=>Paused;} }
public class ManagedSystemManager:NativeObject {public bool enabled=true;public UnityEngine.GameObject gameObject=new();public EntityContext _context=new();}
public class GameplayUpdate:NativeObject {public ManagedSystemManager _systemManager=new();public bool isActive=true,isPushed=true;public int Updates,Fixed,Late;public Action DuringUpdate,DuringLate;public void OnUpdate(){Updates++;DuringUpdate?.Invoke();}public void OnFixedUpdate(){Fixed++;}public void OnLateUpdate(){Late++;DuringLate?.Invoke();} }
public class GameStateStack:UnityEngine.Object {public bool enabled=true;public UnityEngine.GameObject gameObject=new();public List<NativeObject> stack=new();}
public class Entity:NativeObject {public EntityContext context;}
namespace HarmonyLib { [AttributeUsage(AttributeTargets.Class)]public class HarmonyPatch:Attribute{public HarmonyPatch(Type type,string method){}} }
namespace RoguePrince.CheatMenu.Core {public static class MenuText{public static bool IsEnglish=>false;public static string Get(string text)=>text;public static string Format(string text,params object[] args)=>string.Format(text,args);} }
namespace RoguePrince.CheatMenu
{
 internal class GameSession {internal Entity Player=new();internal bool Ready=true;internal IntPtr PlayerPointer=>Player.Pointer;internal void RequireReady(){if(!Ready)throw new InvalidOperationException("not ready");}}
 internal static class TrainingActions {internal static TrainingScope Scope=new(1,1,3,4,0,0);internal static TrainingScope ReadScope(GameSession game)=>game.Ready?Scope:default;}
 internal static class Plugin {internal static List<string> Events=new();internal static void LogEvent(string text)=>Events.Add(text);}
}
