// Test-only native interface substitutes. This project is never referenced by the plugin.
using MotherBase.ApplicationFlowManager;
namespace HarmonyLib
{
 [AttributeUsage(AttributeTargets.Class)]public class HarmonyPatch:Attribute{public HarmonyPatch(Type t,string method){}}
 [AttributeUsage(AttributeTargets.Method)]public class HarmonyPriority:Attribute{public HarmonyPriority(int priority){}}
 public static class Priority{public const int First=800;}
}
namespace UnityEngine {public static class Application{public static bool isFocused=true;}}
namespace MotherBase.ApplicationFlowManager
{
 public class ApplicationState
 {public string guid,stateID;public NativeLogic stateLogic;public ApplicationState(string name,bool start=false){guid=stateID=name;stateLogic=new BluesteakApplicationStateLogic{isStartRunBiome=start};}}
 public class NativeLogic{public T TryCast<T>()where T:class=>this as T;}
 public class BluesteakApplicationStateLogic:NativeLogic{public bool isStartRunBiome,isHubBiome,isTutorialBiome;public bool IsBiomeGeneration()=>true;}
 public class ApplicationSignal{public IntPtr Pointer;public ApplicationSignal(int id)=>Pointer=(IntPtr)id;}
 public class ApplicationFlowManager
 {
  public IntPtr Pointer=(IntPtr)1;public bool isLoading;public ApplicationState state;public Dictionary<string,ApplicationState> states=new();public Dictionary<long,ApplicationState> exits=new();
  public ApplicationState FindState(string id)=>states.GetValueOrDefault(id);public ApplicationState GetCurrentState()=>state;
  public bool AcceptsSignal(ApplicationSignal signal)=>exits.ContainsKey(signal.Pointer.ToInt64());public ApplicationState GetNextState(ApplicationSignal signal)=>exits.GetValueOrDefault(signal.Pointer.ToInt64());
  public void LoadStateInternal(ApplicationState target,ApplicationSignal signal){}
 }
}
public class ApplicationFlowService:ApplicationFlowManager
{public bool IsRunActive=true;public int seed=123;public int GetSeed()=>seed;public void SetSeed(int value)=>seed=value;public void ResetLayerLevelInRun(){}}
public class LevelGeneratorParameters{public string stateId;public int seed;}
public class TaskGenerateAsyncSubSequence{public LevelGeneratorParameters _parameters;public void Start(){}}
public class ItemProgressionManagementService
{public IntPtr Pointer=(IntPtr)2;public ProgressionSettings settings=new();public int GetAwakeningLevel()=>1;}
public class ProgressionSettings{public AwakeningSettings awakeningSettings=new();}
public class AwakeningSettings{public List<AwakeningLevelStep> awakeningLevelSteps=new(){new(){level=4}};}
public class AwakeningLevelStep{public int level;}
public static class RuntimeServiceLocator
{public static ItemProgressionManagementService progression=new();public static T Get<T>()where T:class=>typeof(T)==typeof(ItemProgressionManagementService)?progression as T:null;}
namespace RoguePrince.CheatMenu.Core
{public static class MenuText{public static string Get(string x)=>x;public static string Format(string x,params object[] values)=>string.Format(x,values);}}
namespace RoguePrince.CheatMenu
{
 internal static class Plugin{public static FakeLog MenuLog=new();public static void LogEvent(string text){}}
 internal class FakeLog{public void LogError(object message){}}
 internal class SceneActions{public static ApplicationFlowService flow;internal static ApplicationFlowService Flow()=>flow;internal static LevelMap Map(ApplicationFlowService flow)=>new();internal static bool Biome(ApplicationState s)=>s?.stateLogic?.TryCast<BluesteakApplicationStateLogic>() is {} l&&l.IsBiomeGeneration()&&!l.isHubBiome&&!l.isTutorialBiome;}
 internal class LevelTravelActions{internal class Entry{public ApplicationState State;public ApplicationSignal Signal;public string Key=>State.guid;public string Name=>State.guid;}}
 internal static class ScenePatchGuard{public static T Read<T>(Func<T> action,T fallback){try{return action();}catch{return fallback;}}public static void Write(Action action)=>action();}
}
