// Test-only native interfaces; production BossPhaseActions is linked verbatim.
using MotherBase.EntityFramework;
namespace UnityEngine
{
 public class Object
 {static long next=100;public IntPtr Pointer=(IntPtr)(next++);public static List<Object> All=new();public bool destroyedObject;public Object()=>All.Add(this);public static T[] FindObjectsOfType<T>()where T:Object=>All.OfType<T>().Where(x=>!x.destroyedObject).ToArray();}
 public class GameObject{public string name="Boss";public bool activeInHierarchy=true;public Scene scene=new();public Entity owner;}
 public class Scene{public int handle=3;}
 public struct Vector3{public float x,y,z;}
 public class Transform{public Vector3 position=new(){x=5,y=6};}
 public class MonoBehaviour:Object{public GameObject gameObject=new();public Transform transform=new();public bool enabled=true;public T GetComponent<T>()where T:class=>gameObject.owner?.GetComponent<T>();public T TryCast<T>()where T:class=>this as T;}
 public static class Application{public static bool isFocused=true;}
 public static class Time{public static float timeScale=1,realtimeSinceStartup;}
}
namespace MotherBase.EntityFramework
{
 public class Entity:UnityEngine.MonoBehaviour
 {
  public bool destroyed;public EntityContext context=new();public Dictionary<Type,object> components=new();
  public Entity(){gameObject.owner=this;}
  public new T GetComponent<T>()where T:class=>this as T??components.Values.OfType<T>().FirstOrDefault();
  public T Add<T>(T c)where T:class{components[typeof(T)]=c;if(c is UnityEngine.MonoBehaviour m){m.gameObject=gameObject;m.transform=transform;}return c;}
 }
 public class EntityContext:UnityEngine.Object{public bool Paused;public bool IsPaused()=>Paused;}
}
namespace MotherBase.ActionFramework
{
 public class Actor{public bool Transition;}
 public class ActionData{}
}
public static class ActorUtils{public static bool HasActiveAction(MotherBase.ActionFramework.Actor actor,MotherBase.ActionFramework.ActionData action)=>actor.Transition;}
public class AffectData{public string name="transition-invincibility";}
public class AffectableComponent{public bool Invincible;public bool HasAffect(string name)=>Invincible;}
public class BossComponentBase:UnityEngine.MonoBehaviour{public int currentPhaseIndex=1;public bool brainInitDone=true,reachedPhaseThreshold;}
public class AltanBossComponent:BossComponentBase{}
public class BaatarBossComponent:BossComponentBase{}
public class GoatBossComponent:BossComponentBase{}
public class CorruptedNogaiBossComponent:BossComponentBase{}
public class KaaliBossComponent:BossComponentBase{}
public class NogaiBossComponent:BossComponentBase{}
public class TestBrain:UnityEngine.Object{public float lifeRatioPhase1=.66f,lifeRatioPhase2=.33f;public MotherBase.ActionFramework.ActionData startPhaseAction=new(),phaseTransitionAction=new(),goatBossPhase2Action=new(),transitionAction=new();public AffectData bossInvincibleAffect=new();public T TryCast<T>()where T:class=>this as T;}
public class AltanBossBrain:TestBrain{public float GetLifeRatioToNextPhase(AltanBossComponent c)=>c.currentPhaseIndex==1?lifeRatioPhase1:lifeRatioPhase2;}
public class BaatarBossBrain:TestBrain{}
public class GoatBossBrain:TestBrain{public float lifeRatioPhaseTuto=.66f;}
public class CorruptedNogaiBrain:TestBrain{}
public class KaaliBossBrain:TestBrain{}
public class NogaiBossBrain:TestBrain{}
public class BSAIInput{public TestBrain brain;}
public class AIStateComponent{public bool stayIdle;}
public class MobComponent{public bool isBoss=true;}
public class Damageable:UnityEngine.MonoBehaviour{public bool isAlive=true,onDieActionStarted;public float maxHealth=100,currentHealth=90;}
public class LevelRoomData:UnityEngine.Object{}
public class RoomStateComponent{public LevelRoomData currentRoom;}
public class LevelInstance:UnityEngine.Object{}
public class TestState{public string guid="arena";}
public class TestFlow:UnityEngine.Object{public bool isLoading;public int seed=42;public TestState state=new();public int GetSeed()=>seed;public TestState GetCurrentState()=>state;public bool IsCurrentStateLoaded()=>!isLoading;}
public class TestMap{public LevelRoomData room;public bool FindRoomAtCoord(UnityEngine.Vector3 p,out LevelRoomData r){r=room;return r!=null;}}
public static class MobRoomIdentity{public static bool Matches(LevelRoomData a,LevelRoomData b)=>a!=null&&b!=null&&a.Pointer==b.Pointer;}
public class EntityVariable{public Entity CurrentValue;}
public class TestDebugWindow{public EntityVariable bossEntityVariable=new();}
public static class DebugService{public static bool Ready=true;public static bool TryGetDebugItemStatic<T>(out T item)where T:new(){item=new();if(item is TestDebugWindow window)window.bossEntityVariable.CurrentValue=NativePhase.OriginalBoss;return Ready;}}
public static class NativePhase
{public static Entity OriginalBoss;public static int Calls;public static bool Advance=true,ThrowAfterDamage;public static string Kind;public static void Fire(string kind,Entity e,BossComponentBase c,TestBrain b){Calls++;Kind=kind;var h=e.GetComponent<Damageable>();var ratio=c.currentPhaseIndex==1?b.lifeRatioPhase1:b.lifeRatioPhase2;h.currentHealth=h.maxHealth*ratio;if(ThrowAfterDamage)throw new IOException("native callback failed after HP mutation");if(Advance)c.currentPhaseIndex++;}}
public class AltanDebugWindow:TestDebugWindow{public static void SkipToNextFightPhase(Entity e,AltanBossComponent c,AltanBossBrain b)=>NativePhase.Fire("Altan",e,c,b);}
public class BaatarDebugWindow:TestDebugWindow{public static void SkipToNextFightPhase(Entity e,BaatarBossComponent c,BaatarBossBrain b)=>NativePhase.Fire("Baatar",e,c,b);}
public class BerudeDebugWindow:TestDebugWindow{public static void SkipToNextFightPhase(Entity e,GoatBossComponent c,GoatBossBrain b)=>NativePhase.Fire("Berude",e,c,b);}
public class CorruptedNogaiDebugWindow:TestDebugWindow{public static void SkipToNextFightPhase(Entity e,CorruptedNogaiBossComponent c,CorruptedNogaiBrain b)=>NativePhase.Fire("CorruptedNogai",e,c,b);}
public class KaaliDebugWindow:TestDebugWindow{public static void SkipToNextFightPhase(Entity e,KaaliBossComponent c,KaaliBossBrain b)=>NativePhase.Fire("Kaali",e,c,b);}
public class NogaiDebugWindow:TestDebugWindow{public static void SkipToNextFightPhase(Entity e,NogaiBossComponent c,NogaiBossBrain b)=>NativePhase.Fire("Nogai",e,c,b);}
namespace RoguePrince.CheatMenu.Core
{public static class MenuText{public static bool IsEnglish=>false;public static string Get(string s)=>s;public static string Format(string s,params object[] a)=>string.Format(s,a);}}
namespace RoguePrince.CheatMenu
{
 internal class GameSession{public Entity Player=new();public bool Ready=true;public long Generation=1;public IntPtr PlayerPointer=>Player.Pointer;public void RequireReady(){if(!Ready)throw new InvalidOperationException("not ready");}}
 internal static class TrainingActions{public static Core.TrainingScope ReadScope(GameSession g){var r=g.Player.GetComponent<RoomStateComponent>()?.currentRoom;return new(g.Generation,g.PlayerPointer.ToInt64(),3,r?.Pointer.ToInt64()??0,5,6);}}
 internal static class RunTools{public static long RunId=1;}
 internal class LevelTravelActions{public class Entry{public string Key="arena";}}
 internal class SceneActions
 {public static TestFlow flow=new();public static LevelInstance level=new();public static TestMap map=new();public bool Arena=true,Busy,Error=false,ThrowAfterLoad;public int LoadCalls;public static TestFlow Flow()=>flow;public static LevelInstance Level(TestFlow f)=>level;public static TestMap Map(TestFlow f)=>map;public List<LevelTravelActions.Entry> Bosses()=>Arena?new(){new()}:new();public string LoadBoss(LevelTravelActions.Entry entry,Action release){release();LoadCalls++;Busy=true;flow.isLoading=true;if(ThrowAfterLoad)throw new IOException("load failed after dispatch");return "loading";}}
 internal static class Plugin{public static List<string> Logs=new();public static void LogEvent(string s)=>Logs.Add(s);}
}
