using RoguePrince.CheatMenu.Core;
public class NativeObject
{
 static long serial=10;public IntPtr Pointer=(IntPtr)(++serial);
 public T TryCast<T>() where T:class=>this as T;
 public Type GetIl2CppType()=>GetType();
}
namespace UnityEngine
{
 public static class Application {public static bool isFocused=true;}
 public sealed class Scene {public int handle=3;}
 public readonly struct Vector3 {}
 public sealed class Transform {public Vector3 position;}
 public sealed class GameObject {public bool activeInHierarchy=true;public Scene scene=new();}
 public sealed class Object {public static BossComponentBase[] Bosses=Array.Empty<BossComponentBase>();public static T[] FindObjectsOfType<T>()=>Bosses.Cast<T>().ToArray();}
}
namespace MotherBase.EntityFramework
{
 public sealed class Entity:NativeObject
 {
  public bool destroyed,ThrowRead;public Type ThrowComponent;public int Reads;public Action BeforeRead;public NativeObject context=new();public UnityEngine.GameObject gameObject=new();public UnityEngine.Transform transform=new();
  public readonly Dictionary<Type,object> Components=new();
  public T GetComponent<T>() where T:class {Reads++;BeforeRead?.Invoke();if(ThrowRead||ThrowComponent==typeof(T))throw new InvalidOperationException("native read");return Components.GetValueOrDefault(typeof(T)) as T;}
 }
}
namespace MotherBase.ActionFramework {public sealed class MoveIntent3D:NativeObject {public float magnitude=1;}}
public sealed class BSAIInput:NativeObject {public NativeObject brain;}
public sealed class NogaiBossBrain:NativeObject {public void OrientateTowardTarget(){}public void WalkToTarget(){}public bool TryStartNextAttack()=>true;}
public sealed class KaaliBossBrain:NativeObject {public void OrientateTowardTarget(){}public void MeleeBehaviour(){}public void RangePhaseBehaviour(){}}
public sealed class GoatBossBrain:NativeObject {public bool TriggerFillerAction()=>true;}
public sealed class AltanBossBrain:NativeObject {public void FollowTimeline(){}}
public sealed class UnknownBossBrain:NativeObject {}
public sealed class BaatarBossBrain:NativeObject {public bool TryStartNextAttack()=>true;}
public sealed class CorruptedNogaiBrain:NativeObject {public bool TryStartNextAttack()=>true;}
public class BossComponentBase:NativeObject
{
 public MotherBase.EntityFramework.Entity Owner;public bool brainInitDone=true;public int currentPhaseIndex=1;
 public T GetComponent<T>() where T:class=>Owner as T;
}
public sealed class NogaiBossComponent:BossComponentBase {}
public sealed class KaaliBossComponent:BossComponentBase {}
public sealed class GoatBossComponent:BossComponentBase {}
public sealed class AltanBossComponent:BossComponentBase {}
public sealed class UnknownBossComponent:BossComponentBase {}
public sealed class BaatarBossComponent:BossComponentBase {}
public sealed class CorruptedNogaiBossComponent:BossComponentBase {}
public sealed class MobComponent:NativeObject {public bool isBoss=true;}
public sealed class TargetComponent:NativeObject {public MotherBase.EntityFramework.Entity target,lastTarget;public bool hasLastTargetPosition=true;}
public sealed class Damageable:NativeObject {public bool isAlive=true,onDieActionStarted;}
public sealed class AIStateComponent:NativeObject {public bool stayIdle;}
public sealed class LevelRoomData:NativeObject {public NativeObject template=new();}
public sealed class RoomStateComponent:NativeObject {public LevelRoomData currentRoom;}
namespace RoguePrince.CheatMenu
{
 internal sealed class GameSession
 {
  internal MotherBase.EntityFramework.Entity Player=new();internal bool Ready=true;internal IntPtr PlayerPointer=>Player.Pointer;
  internal void RequireReady(){if(!Ready)throw new InvalidOperationException("not ready");}
 }
 internal static class TrainingActions {internal static TrainingScope Scope;internal static TrainingScope ReadScope(GameSession game)=>Scope;}
 internal static class MobRoomIdentity {internal static bool Matches(LevelRoomData a,LevelRoomData b)=>a!=null&&b?.Pointer==a.Pointer;}
 internal static class SceneActions {internal static LevelRoomData FallbackRoom;internal static SceneActionsMap Map(object flow)=>FallbackRoom==null?null:new();internal static object Flow()=>null;}
 internal sealed class SceneActionsMap {internal bool FindRoomAtCoord(UnityEngine.Vector3 p,out LevelRoomData room){room=SceneActions.FallbackRoom;return room!=null;}}
 internal static class RunTools {internal static long RunId=1;}
 internal static class Plugin {internal static readonly TestLog MenuLog=new();internal static void LogEvent(string text){} }
 internal sealed class TestLog {internal void LogWarning(object text){} }
}
