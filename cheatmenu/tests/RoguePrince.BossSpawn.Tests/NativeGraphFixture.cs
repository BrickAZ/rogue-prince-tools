using System.Reflection;
#pragma warning disable CS0169 // These markers model interop native-field metadata read by reflection.
namespace Il2CppInterop.Runtime.InteropTypes
{
 public class Il2CppObjectBase
 {
  static long next;internal static Dictionary<IntPtr,Dictionary<string,object?>> Values=new();internal static Dictionary<IntPtr,Type> Types=new();
  public IntPtr Pointer;public Il2CppObjectBase(){Pointer=new IntPtr(++next);Values[Pointer]=new();Types[Pointer]=GetType();}public Il2CppObjectBase(IntPtr p)=>Pointer=p;
  public T Cast<T>()where T:Il2CppObjectBase=>typeof(T)==GetType()?(T)this:(T)Activator.CreateInstance(typeof(T),Pointer)!;
  protected T? Read<T>(string key)=>Values[Pointer].TryGetValue(key,out var v)?(T?)v:default;protected void Write(string key,object? value)=>Values[Pointer][key]=value;
 }
}
namespace Il2CppSystem
{
 public class Object:Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase{public Object(){}public Object(IntPtr p):base(p){}public NativeType GetIl2CppType()=>new(){FullName=Types[Pointer].FullName!};}
 public class NativeType{public string FullName="";}
}
namespace UnityEngine
{
 public class Object:Il2CppSystem.Object
 {
  public Object(){}public Object(IntPtr p):base(p){}public bool Dead;public string name{get=>Read<string>("name")??"";set=>Write("name",value);}
  public static Object Instantiate(Object o){var c=(Object)Activator.CreateInstance(Types[o.Pointer])!;Values[c.Pointer]=new(Values[o.Pointer]);return c;}
  public static List<IntPtr> Destroyed=new();public static void Destroy(Object o){Destroyed.Add(o.Pointer);o.Dead=true;}
 }
 public class ScriptableObject:Object{public ScriptableObject(){}public ScriptableObject(IntPtr p):base(p){}}
}
namespace Il2CppSystem.Collections.Generic
{
 public class List<T>:Il2CppSystem.Object
 {
  System.Collections.Generic.List<T> Items=>Read<System.Collections.Generic.List<T>>("items")!;
  public List()=>Write("items",new System.Collections.Generic.List<T>());public List(IntPtr p):base(p){}
  public int Count=>Items.Count;public T this[int index]{get=>Items[index];set=>Items[index]=value;}public void Add(T item)=>Items.Add(item);
 }
}
namespace Il2CppInterop.Runtime.InteropTypes.Arrays
{
 public class Il2CppReferenceArray<T>:Il2CppSystem.Object
 {
  T[] Items=>Read<T[]>("items")!;public Il2CppReferenceArray(long size)=>Write("items",new T[size]);public Il2CppReferenceArray(IntPtr p):base(p){}
  public long Length=>Items.LongLength;public T this[int index]{get=>Items[index];set=>Items[index]=value;}
 }
}
namespace MotherBase.ActionFramework
{
 public class ActionTrigger:UnityEngine.ScriptableObject{public ActionTrigger(){}public ActionTrigger(IntPtr p):base(p){}}
 public class ActionFeature:UnityEngine.ScriptableObject{public ActionFeature(){}public ActionFeature(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr__active;public bool _active{get=>Read<bool>("active");set=>Write("active",value);}}
 public class ActionData:ActionTrigger
 {public ActionData(){}public ActionData(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_actionFeatures;public Il2CppSystem.Collections.Generic.List<ActionFeature>? actionFeatures{get=>Read<Il2CppSystem.Collections.Generic.List<ActionFeature>>("features");set=>Write("features",value);}}
 public class ActorConfig:UnityEngine.ScriptableObject
 {public ActorConfig(){}public ActorConfig(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_startActions;public Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<ActionTrigger>? startActions{get=>Read<Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<ActionTrigger>>("start");set=>Write("start",value);}}
 public class ActionBinding:Il2CppSystem.Object
 {public ActionBinding(){}public ActionBinding(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_action;public ActionTrigger? action{get=>Read<ActionTrigger>("action");set=>Write("action",value);}}
 public class ActionBindings:UnityEngine.ScriptableObject
 {public ActionBindings(){}public ActionBindings(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_actionBindings;public Il2CppSystem.Collections.Generic.List<ActionBinding>? actionBindings{get=>Read<Il2CppSystem.Collections.Generic.List<ActionBinding>>("bindings");set=>Write("bindings",value);}}
}
public class BossAiAction:Il2CppSystem.Object{public BossAiAction(){}public BossAiAction(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_attackAction;public MotherBase.ActionFramework.ActionTrigger? attackAction{get=>Read<MotherBase.ActionFramework.ActionTrigger>("attack");set=>Write("attack",value);}}
public class UtilityActionTrigger:Il2CppSystem.Object{public UtilityActionTrigger(){}public UtilityActionTrigger(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_attackAction;public MotherBase.ActionFramework.ActionTrigger? attackAction{get=>Read<MotherBase.ActionFramework.ActionTrigger>("attack");set=>Write("attack",value);}}
public class AltanBossBrain:UnityEngine.ScriptableObject{public AltanBossBrain(){}public AltanBossBrain(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_cornerActions;public Il2CppSystem.Collections.Generic.List<BossAiAction>? cornerActions{get=>Read<Il2CppSystem.Collections.Generic.List<BossAiAction>>("attacks");set=>Write("attacks",value);}}
public class BaatarBossBrain:UnityEngine.ScriptableObject{public BaatarBossBrain(){}public BaatarBossBrain(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_cornerActions;public Il2CppSystem.Collections.Generic.List<BossAiAction>? cornerActions{get=>Read<Il2CppSystem.Collections.Generic.List<BossAiAction>>("attacks");set=>Write("attacks",value);}}
public class GoatBossBrain:UnityEngine.ScriptableObject{public GoatBossBrain(){}public GoatBossBrain(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_aiActions;public Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<GoatAiAction>? aiActions{get=>Read<Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<GoatAiAction>>("attacks");set=>Write("attacks",value);}}
public class CorruptedNogaiBrain:UnityEngine.ScriptableObject{public CorruptedNogaiBrain(){}public CorruptedNogaiBrain(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_cornerActions;public Il2CppSystem.Collections.Generic.List<BossAiAction>? cornerActions{get=>Read<Il2CppSystem.Collections.Generic.List<BossAiAction>>("attacks");set=>Write("attacks",value);}}
public class KaaliBossBrain:UnityEngine.ScriptableObject{public KaaliBossBrain(){}public KaaliBossBrain(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_cornerActions;public Il2CppSystem.Collections.Generic.List<BossAiAction>? cornerActions{get=>Read<Il2CppSystem.Collections.Generic.List<BossAiAction>>("attacks");set=>Write("attacks",value);}}
public class NogaiBossBrain:UnityEngine.ScriptableObject{public NogaiBossBrain(){}public NogaiBossBrain(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_cornerActions;public Il2CppSystem.Collections.Generic.List<BossAiAction>? cornerActions{get=>Read<Il2CppSystem.Collections.Generic.List<BossAiAction>>("attacks");set=>Write("attacks",value);}}
public class SpawnBackWallConfiguration:MotherBase.ActionFramework.ActionFeature{public SpawnBackWallConfiguration(){}public SpawnBackWallConfiguration(IntPtr p):base(p){}}
public class BossSetupCameraFeature:MotherBase.ActionFramework.ActionFeature{public BossSetupCameraFeature(){}public BossSetupCameraFeature(IntPtr p):base(p){}}
public class VerticalScrollingFeature:MotherBase.ActionFramework.ActionFeature{public VerticalScrollingFeature(){}public VerticalScrollingFeature(IntPtr p):base(p){}}
public class NogaiToggleCamera:MotherBase.ActionFramework.ActionFeature{public NogaiToggleCamera(){}public NogaiToggleCamera(IntPtr p):base(p){}}
public class RoomVariable:UnityEngine.ScriptableObject{public RoomVariable(){}public RoomVariable(IntPtr p):base(p){}}
public class TestRoomFeature:MotherBase.ActionFramework.ActionFeature
{public TestRoomFeature(){}public TestRoomFeature(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_roomVariable;private static IntPtr NativeFieldInfoPtr_next;
public RoomVariable? roomVariable{get=>Read<RoomVariable>("room");set=>Write("room",value);}public MotherBase.ActionFramework.ActionTrigger? next{get=>Read<MotherBase.ActionFramework.ActionTrigger>("next");set=>Write("next",value);}}
public class GoatAiAction:BossAiAction{public GoatAiAction(){}public GoatAiAction(IntPtr p):base(p){}}
public class EffectData:Il2CppSystem.Object{public EffectData(){}public EffectData(IntPtr p):base(p){}}
public class LaunchActionEffectData:EffectData{public LaunchActionEffectData(){}public LaunchActionEffectData(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_toLaunch;public MotherBase.ActionFramework.ActionTrigger? toLaunch{get=>Read<MotherBase.ActionFramework.ActionTrigger>("launch");set=>Write("launch",value);}}
public class EffectDataRef:Il2CppSystem.Object{public EffectDataRef(){}public EffectDataRef(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_data;public EffectData? data{get=>Read<EffectData>("data");set=>Write("data",value);}}
public class AtFrameEffect:MotherBase.ActionFramework.ActionFeature{public AtFrameEffect(){}public AtFrameEffect(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_effect;public EffectDataRef? effect{get=>Read<EffectDataRef>("effect");set=>Write("effect",value);}}
namespace UnityEngine.AddressableAssets{public class AssetReference:Il2CppSystem.Object{public AssetReference(){}public AssetReference(IntPtr p):base(p){}}}
public class TestAssetReference:UnityEngine.AddressableAssets.AssetReference{public TestAssetReference(){}public TestAssetReference(IntPtr p):base(p){}}
public class EffectScriptableObject:UnityEngine.ScriptableObject{public EffectScriptableObject(){}public EffectScriptableObject(IntPtr p):base(p){}}
public class EntityVariable:UnityEngine.ScriptableObject{public EntityVariable(){}public EntityVariable(IntPtr p):base(p){}}
public class BackWallElement:Il2CppSystem.Object{public BackWallElement(){}public BackWallElement(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_x;public float x{get=>Read<float>("x");set=>Write("x",value);}}
public class BackWallConfiguration:UnityEngine.ScriptableObject{public BackWallConfiguration(){}public BackWallConfiguration(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_elements;public Il2CppSystem.Collections.Generic.List<BackWallElement>? elements{get=>Read<Il2CppSystem.Collections.Generic.List<BackWallElement>>("elements");set=>Write("elements",value);}}
public class VariableEffect:EffectData{public VariableEffect(){}public VariableEffect(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_variable;public EntityVariable? variable{get=>Read<EntityVariable>("variable");set=>Write("variable",value);}}
public struct UIEventID{public string streamCategory,streamName;}
public class SendUIEventEffectData:EffectData{public SendUIEventEffectData(){}public SendUIEventEffectData(IntPtr p):base(p){}private static IntPtr NativeFieldInfoPtr_uiEvent;public UIEventID uiEvent{get=>Read<UIEventID>("ui");set=>Write("ui",value);}}
