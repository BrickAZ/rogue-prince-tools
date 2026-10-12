using RoguePrince.CheatMenu.Core;
namespace UnityEngine
{
 public static class Time {public static float timeScale=1;}
 public static class Application {public static bool isFocused=true;}
 public readonly record struct Vector3(float x,float y,float z)
 {public static float Distance(Vector3 a,Vector3 b)=>MathF.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z));}
 public class Transform {public Vector3 position=new(10,5,0);}
}
public class NativeObject {static long serial=10;public IntPtr Pointer=(IntPtr)(++serial);}
public class EntityContext:NativeObject {public bool Paused;public bool IsPaused()=>Paused;}
public class Entity:NativeObject
{
 public bool destroyed;public EntityContext context=new();public UnityEngine.Transform transform=new();
 public Damageable Health=new();public EnergyComponent Energy=new();public RoomStateComponent Room=new();public PlatformStateComponent Platform=new();
 public T GetComponent<T>()where T:class=>new object[]{Health,Energy,Room,Platform}.OfType<T>().SingleOrDefault();
}
public class Damageable:NativeObject
{
 public bool isAlive=true,onDieActionStarted;public float maxHealth=100;float hp=80;public Action<float> BeforeWrite;
 public float currentHealth {get=>hp;set{BeforeWrite?.Invoke(value);hp=value;}}
}
public class EnergyComponent:NativeObject
{
 public bool isInitialized=true;public int energyMaxValueStat=10,energyCurrentValueStat=7,Writes,Triggers;public Action<int> DuringSet;
 public void SetEnergy(int value,bool triggerChange){Writes++;if(triggerChange)Triggers++;energyCurrentValueStat=value;DuringSet?.Invoke(value);}
}
public class Platform {public bool Valid=true;public bool isValid()=>Valid;}
public class PlatformStateComponent {public Platform currentPlatform=new();}
public class LevelRoomData {}
public class RoomStateComponent {public LevelRoomData currentRoom=new();}
public static class TeleportExtensions
{
 public static int Calls;public static Action<Entity,UnityEngine.Vector3> During;
 public static void Teleport(Entity e,UnityEngine.Vector3 p,bool reset){Calls++;e.transform.position=p;During?.Invoke(e,p);}
}
namespace RoguePrince.CheatMenu.Core {public static class MenuText {public static bool IsEnglish=>false;public static string Get(string s)=>s;public static string Format(string s,params object[] a)=>string.Format(s,a);} }
namespace RoguePrince.CheatMenu
{
 internal class GameSession
 {
  internal Entity Player=new();internal bool Ready=>!Player.destroyed&&Player.Health.isAlive&&!Player.Health.onDieActionStarted;
  internal Damageable Health=>Player.Health;internal EnergyComponent Energy=>Player.Energy;internal IntPtr PlayerPointer=>Player.Pointer;
  internal void RequireReady(){if(!Ready)throw new InvalidOperationException("not ready");}
 }
 internal static class TrainingActions
 {
  internal static TrainingScope Scope=new(1,1,3,4,0,0);internal static Action<UnityEngine.Vector3> Validate;
  internal static TrainingScope ReadScope(GameSession g)=>g.Ready?Scope:default;
  internal static void ValidatePosition(GameSession g,LevelRoomData room,UnityEngine.Vector3 point)=>Validate?.Invoke(point);
 }
 internal static class Plugin {internal static List<string> Events=new();internal static void LogEvent(string s)=>Events.Add(s);}
}
