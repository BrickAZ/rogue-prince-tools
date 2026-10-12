// Test-only substitutes; production report adapter is linked, no fixture reaches the plugin.
namespace Il2CppInterop.Runtime.InteropTypes.Arrays
{
 public class Il2CppReferenceArray<T> where T:class
 {private readonly T[] values;public Il2CppReferenceArray(int count)=>values=new T[count];public int Length=>values.Length;public T this[int i]{get=>values[i];set=>values[i]=value;}}
}
namespace Il2CppSystem.Collections.Generic
{
 using Il2CppInterop.Runtime.InteropTypes.Arrays;
 public class HashSet<T>:UnityEngine.NativeObject where T:class
 {public T[] values;public int _version,fakeCount=-1,copyCalls;public Action afterCopy;public HashSet(params T[] values)=>this.values=values;public int Count=>fakeCount<0?values.Length:fakeCount;public void CopyTo(Il2CppReferenceArray<T> target,int start,int count){copyCalls++;for(int i=0;i<count;i++)target[start+i]=values[i];afterCopy?.Invoke();}}
 public class List<T>:UnityEngine.NativeObject where T:class
 {public T[] values;public int _version;public Action afterCopy;public List(params T[] values)=>this.values=values;public int Count=>values.Length;public void CopyTo(Il2CppReferenceArray<T> target,int start){for(int i=0;i<Count;i++)target[start+i]=values[i];afterCopy?.Invoke();}}
 public class Dictionary<TKey,TValue>:UnityEngine.NativeObject where TValue:class
 {public int _version;public ValueCollection Values;public Dictionary(params TValue[] values)=>Values=new(values);public int Count=>Values.values.Length;
  public class ValueCollection{public TValue[] values;public Action afterCopy;public ValueCollection(TValue[] values)=>this.values=values;public void CopyTo(Il2CppReferenceArray<TValue> target,int start){for(int i=0;i<values.Length;i++)target[start+i]=values[i];afterCopy?.Invoke();}}
 }
}
namespace UnityEngine
{
 public class NativeObject{static long id=20;public IntPtr Pointer=(IntPtr)Interlocked.Increment(ref id);public Type GetIl2CppType()=>GetType();}
 public class Component:NativeObject{}
 public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
 public class Transform:Component{public Vector3 position=new(1,2,3);}
 public class GameObject{public string name="entity";public bool activeInHierarchy=true;public Scene scene=new(){handle=2};}
 public class Scene{public int handle;}
 public static class Time{public static int frameCount=10;public static float timeScale=0,realtimeSinceStartup=3;}
 public static class Application{public static bool isFocused=true;}
 public static class GUIUtility{public static string systemCopyBuffer="sentinel";}
}
namespace MotherBase.EntityFramework
{
 public class EntityContext:UnityEngine.NativeObject
 {public Il2CppSystem.Collections.Generic.HashSet<Entity> _enabledEntities=new(),_disabledEntities=new(),_destroyedEntities=new();public int destroyedEntityCount=>_destroyedEntities.Count;public bool IsPaused()=>false;}
 public class Entity:UnityEngine.Component
 {public bool destroyed,enabled=true;public EntityContext context;public UnityEngine.GameObject gameObject=new();public UnityEngine.Transform transform=new();public Il2CppSystem.Collections.Generic.Dictionary<int,UnityEngine.Component> componentCache=new();public Dictionary<Type,object> extra=new();public int GetInstanceID()=>Pointer.ToInt32();public T GetComponent<T>()where T:class=>extra.GetValueOrDefault(typeof(T)) as T;}
}
public class Damageable:UnityEngine.Component{public float currentHealth=5;public float maxHealth=10;}
public class ItemData:UnityEngine.NativeObject{public SerializationGuid Guid=new(){guid="item-1"};public string name="weapon";}
public class SerializationGuid{public string guid;}
public class ItemInstanceData:UnityEngine.NativeObject
{public ItemData source=new();public int tier=2;public AffixHolderItemFeature.AffixHolderComponentData affix=new();public T GetComponent<T>()where T:class=>affix as T;}
public class AffixHolderItemFeature{public class AffixHolderComponentData{public SerializationGuid[] affixesGuids={new(){guid="affix-1"}};}}
public class InventoryComponent:UnityEngine.Component{public Il2CppSystem.Collections.Generic.List<ItemInstanceData> items=new(new ItemInstanceData());public bool TryGetMainWeapon(out ItemInstanceData item){item=items.values.FirstOrDefault();return item!=null;}public bool TryGetSecondaryWeapon(out ItemInstanceData item){item=null;return false;}}
public class EntityVariable{public MotherBase.EntityFramework.Entity CurrentValue;}
public class PlayerSaver:UnityEngine.NativeObject{public EntityVariable mainCharacterVariable=new();}
public class ApplicationState{public string guid="state-1";}
public class LevelMap:UnityEngine.NativeObject{}
public class ApplicationFlowService:UnityEngine.NativeObject
{public bool isLoading,IsRunActive=true;public int seed=42;public ApplicationState state=new();public LevelMap map=new();public ApplicationState GetCurrentState()=>state;public int GetSeed()=>seed;public bool IsCurrentStateLoaded()=>true;}
namespace RoguePrince.CheatMenu.Core
{public static class MenuText{public static string Get(string text)=>text;public static string Format(string text,params object[] args)=>string.Format(text,args);}}
namespace RoguePrince.CheatMenu
{
 public class GameSession{public bool Ready=true;public int Generation=3;public PlayerSaver Saver=new();public MotherBase.EntityFramework.Entity Player;public IntPtr PlayerPointer=>Player?.Pointer??IntPtr.Zero;}
 public static class SceneActions{public static ApplicationFlowService flow;public static ApplicationFlowService Flow()=>flow;public static LevelMap Map(ApplicationFlowService source)=>source.map;}
 public static class RunTools{public static long RunId=5;}
 public static class Plugin{public static void LogEvent(string text){}}
}
public static class Fixture
{
 public static RoguePrince.CheatMenu.GameSession game;public static MotherBase.EntityFramework.Entity player,enemy,disabled;public static MotherBase.EntityFramework.EntityContext context;public static ApplicationFlowService flow;public static InventoryComponent inventory;
 public static void Reset(){UnityEngine.Application.isFocused=true;UnityEngine.Time.timeScale=0;UnityEngine.Time.frameCount=10;UnityEngine.GUIUtility.systemCopyBuffer="sentinel";RoguePrince.CheatMenu.RunTools.RunId=5;
 context=new();player=new(){context=context};enemy=new(){context=context};disabled=new(){context=context,enabled=false};inventory=new();player.extra[typeof(InventoryComponent)]=inventory;player.extra[typeof(Damageable)]=new Damageable();player.componentCache=new(inventory,new Damageable());enemy.componentCache=new(new Damageable());context._enabledEntities=new(player,enemy);context._disabledEntities=new(disabled);
 game=new(){Player=player};game.Saver.mainCharacterVariable.CurrentValue=player;flow=new();RoguePrince.CheatMenu.SceneActions.flow=flow;
 }
}
