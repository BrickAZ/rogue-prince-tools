// Offline substitutes only. The plugin never references this project.
namespace Il2CppInterop.Runtime.InteropTypes.Arrays
{
 public class Il2CppReferenceArray<T>{private readonly T[] data;public Il2CppReferenceArray(int n)=>data=new T[n];public int Length=>data.Length;public T this[int i]{get=>data[i];set=>data[i]=value;}}
}
namespace HarmonyLib{[AttributeUsage(AttributeTargets.Class)]public class HarmonyPatch:Attribute{public HarmonyPatch(Type type,string method){}}}
public class MobBrain{public bool TryGetVisibleTarget()=>false;}
public class BSAIInput{public void PreUpdateAI(){}public void PostUpdateAI(){}}
public class MobArenaComponent{public void UpdateArena(){}}
public class CorruptedNogaiInvokeMobFeature{public void SpawnMob(){}}
public class MoleInvokeMobFeature{public void SpawnMob(){}}
public class InvokeSingleMobFeature{public void Spawn(){}}
public class ApplicationFlowService{public void ResetLayerLevelInRun(){}}
namespace UnityEngine
{
 public static class Application{public static bool isFocused=true;}
 public struct Vector3
 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public static Vector3 operator *(Vector3 a,float b)=>new(a.x*b,a.y*b,a.z*b);
  public static Vector3 Scale(Vector3 a,Vector3 b)=>new(a.x*b.x,a.y*b.y,a.z*b.z);
 }
 public struct Quaternion{public float x,y,z,w;public Quaternion(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}public static Quaternion identity=>new(0,0,0,1);}
 public class Transform{public Vector3 position,lossyScale=new(1,1,1);public Quaternion rotation=Quaternion.identity;public Vector3 TransformPoint(Vector3 p)=>new(position.x+p.x*lossyScale.x,position.y+p.y*lossyScale.y,position.z+p.z*lossyScale.z);}
 public struct Scene{public int handle;}
 public class GameObject{public bool activeInHierarchy=true;public Scene scene=new(){handle=1};}
 public class Component
 {
  public IntPtr Pointer;public GameObject gameObject=new();public Transform transform=new();public MotherBase.EntityFramework.Entity parent;
  public T GetComponentInParent<T>()where T:class=>parent as T;public T TryCast<T>()where T:class=>this as T;
 }
 public class Collider:Component{public bool enabled=true,isTrigger;}
 public class BoxCollider:Collider{public Vector3 center,size=new(2,4,6);}
 public class SphereCollider:Collider{public Vector3 center;public float radius=1;}
 public class CapsuleCollider:Collider{public Vector3 center;public float radius=1,height=6;public int direction=1;}
 public class MeshCollider:Collider{}
 public enum QueryTriggerInteraction{Ignore,Collide}
 public static class Physics
 {
  public static Collider[] hits=Array.Empty<Collider>();public static QueryTriggerInteraction query;public static Action afterQuery;
  public static int OverlapSphereNonAlloc(Vector3 pos,float radius,Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Collider> buffer,int mask,QueryTriggerInteraction triggers){query=triggers;int n=Math.Min(hits.Length,buffer.Length);for(int i=0;i<n;i++)buffer[i]=hits[i];afterQuery?.Invoke();return n;}
 }
}
namespace MotherBase.EntityFramework
{
 public class EntityContext{public IntPtr Pointer=(IntPtr)3;}
 public class Entity:UnityEngine.Component
 {
  public bool destroyed;public EntityContext context=new();public Damageable health=new();public HitReceiver receiver=new();public RoomStateComponent roomState=new();
  public bool HasComponentData<T>()=>typeof(T)==typeof(HitReceiver)?receiver!=null:health!=null;public T GetComponentData<T>()where T:class=>typeof(T)==typeof(HitReceiver)?receiver as T:health as T;
  public T GetComponent<T>()where T:class=>typeof(T)==typeof(RoomStateComponent)?roomState as T:health as T;
 }
}
public class Damageable{public bool isAlive=true,onDieActionStarted;}
public class HitReceiver{}
public class RoomStateComponent{public Room currentRoom=new();}
public class Room{public bool inside=true;public bool IsInRoom(UnityEngine.Vector3 position)=>inside;}
namespace RoguePrince.CheatMenu
{
 internal static class Plugin{public static FakeLog MenuLog=new();public static void LogEvent(string text){}}
 internal class FakeLog{public void LogError(string text){}}
 internal class RunTools{public static RunTools Current=new();public void Disable(){}public void ResetRun(){}}
 internal class SceneActions
 {
  public static SceneActions Current;public GameSession game;public bool NoArenaSpawn;public int resets;public Func<MotherBase.EntityFramework.Entity,bool,bool> enemy=(_,_)=>true;
  public bool Enemy(MotherBase.EntityFramework.Entity entity,bool boss)=>enemy(entity,boss);
  public bool BlockSummon(MotherBase.EntityFramework.Entity entity,UnityEngine.Vector3 position)=>SceneSummonGate.Block(game,NoArenaSpawn,entity,position,Enemy);
  public void ResetEnemies(){NoArenaSpawn=false;resets++;}public bool BlockArena(MobArenaComponent arena)=>false;
  public bool RejectTarget(MotherBase.EntityFramework.Entity entity,MotherBase.EntityFramework.Entity target)=>false;public void FilterTarget(MotherBase.EntityFramework.Entity entity){}public void ClearPoint(){}
 }
 internal class GameSession{public bool Ready=true;public long Generation=1;public MotherBase.EntityFramework.Entity Player=new(){Pointer=(IntPtr)1};public IntPtr PlayerPointer=>Player.Pointer;}
 internal static class TrainingActions{public static Core.TrainingScope scope=new(1,1,1,4,0,0);public static Core.TrainingScope ReadScope(GameSession game)=>game.Ready?scope:default;}
}
