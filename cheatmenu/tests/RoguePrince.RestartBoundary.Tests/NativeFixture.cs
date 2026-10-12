// Test-only interface substitutes. None are referenced or distributed by the plugin.
using MotherBase.ApplicationFlowManager;
namespace HarmonyLib
{
 [AttributeUsage(AttributeTargets.Class)]public class HarmonyPatch:Attribute{public HarmonyPatch(Type t,string method){}}
 [AttributeUsage(AttributeTargets.Method)]public class HarmonyPriority:Attribute{public HarmonyPriority(int priority){}}
 public static class Priority{public const int First=800;}
}
namespace UnityEngine
{
 public class Object{public IntPtr Pointer;}
 public class GameObject{public UnityEngine.SceneManagement.Scene scene=new();}
 public class MonoBehaviour:Object{public GameObject gameObject=new();}
 public static class Application{public static bool isFocused=true;}
 public static class Time{public static float timeScale=1,realtimeSinceStartup;}
 public static class Resources{public static List<PauseUIView> views=new();public static T[] FindObjectsOfTypeAll<T>()=>views.OfType<T>().ToArray();}
}
namespace UnityEngine.SceneManagement{public class Scene{public bool isLoaded=true,valid=true;public bool IsValid()=>valid;}}
namespace UnityEngine.Events
{
 public class UnityEvent:UnityEngine.Object
 {
  public UnityEvent(){Pointer=(IntPtr)70;}
  public int listeners=1,calls;public bool targetValid=true;public Action action;
  public int GetPersistentEventCount()=>listeners;
  public UnityEngine.Object GetPersistentTarget(int i)=>targetValid?new():null;
  public string GetPersistentMethodName(int i)=>targetValid?"Execute":"";
  public void Invoke(){calls++;action?.Invoke();}
 }
}
public class PauseUIView:UnityEngine.MonoBehaviour{public UnityEngine.Events.UnityEvent onGiveUpConfirmed=new();}
public class Entity:UnityEngine.Object{}
public class EntityVariable{public Entity CurrentValue;}
public class PlayerSaver:UnityEngine.Object{public EntityVariable mainCharacterVariable=new();}
namespace MotherBase.ApplicationFlowManager
{
 public class NativeLogic{public T TryCast<T>()where T:class=>this as T;}
 public class BluesteakApplicationStateLogic:NativeLogic{public bool isStartRunBiome,isHubBiome,isTutorialBiome;public bool IsBiomeGeneration()=>true;}
 public class ApplicationState:UnityEngine.Object
 {public string guid,stateID;public NativeLogic stateLogic;public ApplicationState(string id,bool start=false,bool hub=false){guid=stateID=id;stateLogic=new BluesteakApplicationStateLogic{isStartRunBiome=start,isHubBiome=hub};}}
 public class ApplicationSignal:UnityEngine.Object{public string name="NextBiome";public ApplicationSignal(int id)=>Pointer=(IntPtr)id;}
 public class ApplicationTransition{public ApplicationState currentState,nextState;public ApplicationSignal signal;}
 public class ApplicationProfile{public List<ApplicationTransition> transitions=new();}
 public class ApplicationFlowManager:UnityEngine.Object
 {
  public bool isLoading,loaded=true;public ApplicationState state;public Dictionary<string,ApplicationState> states=new();public Dictionary<long,ApplicationState> exits=new();public ApplicationProfile profile=new();public int sends;public Action onSend;
  public ApplicationState FindState(string id)=>states.GetValueOrDefault(id);public ApplicationState GetCurrentState()=>state;
  public bool AcceptsSignal(ApplicationSignal signal)=>exits.ContainsKey(signal.Pointer.ToInt64());public ApplicationState GetNextState(ApplicationSignal signal)=>exits.GetValueOrDefault(signal.Pointer.ToInt64());
  public void SendSignal(ApplicationSignal signal){sends++;onSend?.Invoke();}
  public void LoadStateInternal(ApplicationState target,ApplicationSignal signal){}
 }
}
public class ApplicationFlowService:ApplicationFlowManager
{public bool IsRunActive=true,failSeedWrites;public int seed=123;public LevelInstance level=new(){Pointer=(IntPtr)30};public int GetSeed()=>seed;public void SetSeed(int value){if(failSeedWrites)throw new InvalidOperationException("native seed write failed");seed=value;}public bool IsCurrentStateLoaded()=>loaded;public void ResetLayerLevelInRun(){}}
public class LevelInstance:UnityEngine.Object{}
public class LevelGeneratorParameters{public string stateId;public int seed;}
public class TaskGenerateAsyncSubSequence{public LevelGeneratorParameters _parameters;public void Start(){}}
public class ItemProgressionManagementService:UnityEngine.Object
{public ProgressionSettings settings=new();public int GetAwakeningLevel()=>RoguePrince.CheatMenu.RunTools.Current?.Difficulty(this,1)??1;}
public class ProgressionSettings{public AwakeningSettings awakeningSettings=new();}
public class AwakeningSettings{public List<AwakeningLevelStep> awakeningLevelSteps=new(){new(){level=4}};}
public class AwakeningLevelStep{public int level;}
public static class RuntimeServiceLocator{public static ItemProgressionManagementService progression=new(){Pointer=(IntPtr)2};public static T Get<T>()where T:class=>typeof(T)==typeof(ItemProgressionManagementService)?progression as T:null;}
namespace RoguePrince.CheatMenu.Core{public static class MenuText{public static string Get(string x)=>x;public static string Format(string x,params object[] values)=>string.Format(x,values);}}
namespace RoguePrince.CheatMenu
{
 internal static class Plugin{public static FakeLog MenuLog=new();public static void LogEvent(string text){}}
 internal class FakeLog{public void LogError(object message){}}
 internal class GameSession{public Entity Player=new(){Pointer=(IntPtr)21};public PlayerSaver Saver=new(){Pointer=(IntPtr)20};public bool Ready=true;public IntPtr PlayerPointer=>Player.Pointer;public long Generation=1;public GameSession(){Saver.mainCharacterVariable.CurrentValue=Player;}public void RequireReady(){if(!Ready)throw new Exception("not ready");}}
 internal class SceneActions{public static ApplicationFlowService flow;internal static ApplicationFlowService Flow()=>flow;internal static LevelInstance Level(ApplicationFlowService f)=>f?.level;internal static bool Biome(ApplicationState s)=>s?.stateLogic?.TryCast<BluesteakApplicationStateLogic>() is {} l&&l.IsBiomeGeneration()&&!l.isHubBiome&&!l.isTutorialBiome;}
 internal class LevelTravelActions{internal class Entry{public ApplicationState State=null;public ApplicationSignal Signal=null;public string Key=>State.guid;public string Name=>State.guid;}}
 internal static class ScenePatchGuard{public static T Read<T>(Func<T> action,T fallback){try{return action();}catch{return fallback;}}public static void Write(Action action)=>action();}
}
