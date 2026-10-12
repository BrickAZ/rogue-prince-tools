// Test-only Unity / native camera boundary. Executes the production SceneCamera adapter.
namespace UnityEngine
{
 public class Object
 {
  public static List<Object> Objects=new();private static long next=100;public IntPtr Pointer=(IntPtr)(next++);public bool Destroyed;
  public Object()=>Objects.Add(this);
  public static void Destroy(Object value){if(!ReferenceEquals(value,null))value.Destroyed=true;}
  public static T[] FindObjectsOfType<T>()where T:Object=>Objects.OfType<T>().Where(x=>!x.Destroyed).ToArray();
  public static bool operator==(Object a,Object b){var na=ReferenceEquals(a,null)||a.Destroyed;var nb=ReferenceEquals(b,null)||b.Destroyed;return na||nb?na==nb:ReferenceEquals(a,b);}
  public static bool operator!=(Object a,Object b)=>!(a==b);public override bool Equals(object obj)=>ReferenceEquals(this,obj);public override int GetHashCode()=>Pointer.GetHashCode();
 }
 public class GameObject:Object
 {public string name;public bool activeInHierarchy=true;public Scene scene=new();public GameObject(string name=""){this.name=name;}public void SetActive(bool value)=>activeInHierarchy=value;public T AddComponent<T>()where T:MonoBehaviour,new(){var c=new T();c.gameObject=this;return c;}}
 public class Scene{public int handle=1;}
 public class MonoBehaviour:Object{private bool state=true;public bool enabled{get{if(Destroyed)throw new InvalidOperationException("destroyed Unity component");return state;}set{if(Destroyed)throw new InvalidOperationException("destroyed Unity component");state=value;}}public GameObject gameObject=new();public Transform transform=new();}
 public class ScriptableObject:Object{public static T CreateInstance<T>()where T:ScriptableObject,new()=>new();}
 public class Transform{public Vector3 position;}
 public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>new();public static float Distance(Vector3 a,Vector3 b)=>MathF.Sqrt((a.x-b.x)*(a.x-b.x)+(a.y-b.y)*(a.y-b.y)+(a.z-b.z)*(a.z-b.z));}
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new();public Vector2 normalized=>this;}
 public static class Time{public static float timeScale=1;}
}
namespace UnityEngine.InputSystem
{
 public class InputActionMap:UnityEngine.Object{public bool enabled=true;public void Enable()=>enabled=true;public void Disable()=>enabled=false;}
 public class InputActionAsset{public Dictionary<string,InputActionMap> Maps=new();public InputActionMap FindActionMap(string name,bool throwIfMissing)=>Maps.GetValueOrDefault(name);}
 public class Button{public bool isPressed;}
 public class Keyboard{public static Keyboard current;public Button aKey=new(),dKey=new(),wKey=new(),sKey=new();}
}
namespace Cinemachine{public class CinemachineVirtualCamera:UnityEngine.MonoBehaviour{}public class CinemachineBrain:UnityEngine.MonoBehaviour{}}
namespace MotherBase.EntityFramework
{
 public class Entity:UnityEngine.MonoBehaviour
 {public bool destroyed;public bool God,Intangible,Flying;public RoomStateComponent room=new();public T GetComponent<T>()where T:class=>room as T;public bool HasComponentData<T>()=>typeof(T)==typeof(GodModeFlag)?God:typeof(T)==typeof(IntangibleFlag)&&Intangible;}
}
public class GodModeFlag{}public class IntangibleFlag{}
public class EntityVariable:UnityEngine.ScriptableObject{public MotherBase.EntityFramework.Entity CurrentValue;}
public class CinemachineBrainVariable:UnityEngine.ScriptableObject{public Cinemachine.CinemachineBrain CurrentValue;}
public class LevelRoomData{}
public class RoomStateComponent{public LevelRoomData currentRoom=new();}
public class LevelMap{public bool FindRoomAtCoord(UnityEngine.Vector3 position,out LevelRoomData room){room=new();return true;}}
public class FreeCamControls:UnityEngine.MonoBehaviour{public UnityEngine.Vector3 _moveInputValue,_aimInputValue;}
public class CamerasDebug:UnityEngine.MonoBehaviour
{
 public EntityVariable player;public Cinemachine.CinemachineVirtualCamera FreeCamComponent;public FreeCamControls controls;public UnityEngine.InputSystem.InputActionAsset inputRef;public string playerInputs="player",cameraInputs="camera";
 public void SetFreeCamState(bool value){FreeCamComponent.enabled=value;controls.enabled=value;var p=inputRef.FindActionMap(playerInputs,false);var c=inputRef.FindActionMap(cameraInputs,false);if(value){p.Disable();c.Enable();}else{p.Enable();c.Disable();}}
}
public class CameraTeleportDebug:UnityEngine.MonoBehaviour
{
 public static int Deactivated;public static bool FailDeactivate;public EntityVariable player;public CinemachineBrainVariable cinemachineCamera;public bool _teleportActive,_wasInGodMode,_wasIntangible;public UnityEngine.Vector2 _moveInput2D;
 public void ActivateTeleportMode(){cinemachineCamera.CurrentValue.enabled=false;_wasInGodMode=player.CurrentValue.God;_wasIntangible=player.CurrentValue.Intangible;player.CurrentValue.God=player.CurrentValue.Intangible=player.CurrentValue.Flying=true;_teleportActive=true;}
 public void DeactivateTeleportMode(){if(FailDeactivate)throw new IOException("native restoration failure");Deactivated++;cinemachineCamera.CurrentValue.enabled=true;player.CurrentValue.Flying=false;if(!_wasInGodMode)player.CurrentValue.God=false;if(!_wasIntangible)player.CurrentValue.Intangible=false;_teleportActive=false;}
}
public static class TeleportExtensions{public static void Teleport(MotherBase.EntityFramework.Entity entity,UnityEngine.Vector3 position,bool reset)=>entity.transform.position=position;}
namespace RoguePrince.CheatMenu.Core
{public struct TrainingScope{public bool Valid;public long Player;public int Scene;public bool Matches(TrainingScope other)=>Valid&&other.Valid&&Player==other.Player&&Scene==other.Scene;}}
namespace RoguePrince.CheatMenu
{
 internal class GameSession{public MotherBase.EntityFramework.Entity Player=new();public bool Ready=true;public IntPtr PlayerPointer=>Player.Pointer;public void RequireReady(){if(!Ready)throw new Exception("not ready");}}
 internal static class TrainingActions{public static Core.TrainingScope ReadScope(GameSession game)=>new(){Valid=game.Ready,Player=game.PlayerPointer.ToInt64(),Scene=game.Player.gameObject.scene.handle};public static void ValidatePosition(GameSession game,LevelRoomData room,UnityEngine.Vector3 point){}}
}
