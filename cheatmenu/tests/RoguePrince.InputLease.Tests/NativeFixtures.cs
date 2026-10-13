using UnityEngine;
namespace Il2CppInterop.Runtime.Attributes { public class HideFromIl2CppAttribute:Attribute{} }
namespace HarmonyLib
{
 public class HarmonyPatch:Attribute{public HarmonyPatch(Type type,string name){}}
 public class HarmonyPrefix:Attribute{}
}
namespace UnityEngine
{
 public struct Vector2{public float x,y;public static Vector2 zero=>new();}
 public enum CursorLockMode{None,Locked}
 public static class Cursor{public static CursorLockMode lockState;public static bool visible;}
 public static class Time{public static float timeScale=1,realtimeSinceStartup=1;}
 public class GameObject{public bool activeInHierarchy=true;}
 public class Object
 {
  public static UnityEngine.EventSystems.EventSystem[] Systems=Array.Empty<UnityEngine.EventSystems.EventSystem>();
  public static T[] FindObjectsOfType<T>()=>Systems.OfType<T>().ToArray();
 }
}
namespace UnityEngine.EventSystems
{
 public class BaseInputModule
 {
  private bool value=true;public int FailEnables,Changes;
  public bool enabled{get=>value;set{if(value&&FailEnables>0){FailEnables--;throw new InvalidOperationException("injected input module enable failure");}this.value=value;Changes++;}}
 }
 public class EventSystem
 {
  public bool enabled=true;public GameObject gameObject=new();public BaseInputModule[] Modules={new()};
  public static EventSystem current=>UnityEngine.Object.Systems.FirstOrDefault(s=>s.enabled&&s.gameObject.activeInHierarchy);
  public T[] GetComponents<T>()=>Modules.OfType<T>().ToArray();
  public int Selections;public void SetSelectedGameObject(GameObject target)=>Selections++;
 }
}
namespace MotherBase.ActionFramework
{
 public class PlayerInput
 {
  public IntPtr Pointer=new(1);public bool _isListeningUnityInput=true;public int activeInputs,lastActiveInputs,FailStarts;
  public Vector2 moveInputValue,aimInputValue;public GameObject gameObject=new();
  public void ResetInputStates(){activeInputs=lastActiveInputs=0;}
  public void StopListenUnityInput()=>_isListeningUnityInput=false;
  public void StartListenUnityInput(){if(FailStarts>0){FailStarts--;throw new InvalidOperationException("injected player resume failure");}_isListeningUnityInput=true;}
 }
}
namespace RoguePrince.CheatMenu.Core{public static class MenuText{public static string Get(string s)=>s;}}
namespace RoguePrince.CheatMenu
{
 public class GameSession{public Player Player=new();}
 public class Player{public bool destroyed;public MotherBase.ActionFramework.PlayerInput Input=new();public T GetComponent<T>()where T:class=>Input as T;}
 public static class Plugin
 {
  public static Log MenuLog=new();
  // This host invokes the production prefix directly; native registration is
  // supplied by the fixture instead of the real Plugin.Load bootstrap.
  static Plugin(){MenuPauseInputPatch.Available=true;}
  public static void LogEvent(string s){}
 }
 public class Log{public void LogWarning(string s){}}
 public sealed partial class MenuController
 {
  internal static MenuController Instance;private bool visible;private readonly InputLease inputLease=new();
  public void Open(GameSession game){Instance=this;visible=true;inputLease.Acquire(game);}
  public void CloseWithoutRelease()=>visible=false;
  public void Release()=>inputLease.Release();
 }
}
public class PauseInputController{public void OnInputActionPerformed(){}}
public static class NativePauseView
{
 // Matches the observed native fallback: UIContainer.SetSelected dereferences
 // EventSystem.current and throws if the menu removed the last active system.
 public static void Hide()=>UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
}
namespace UnityEngine.InputSystem
{
 public class Button{public bool isPressed;}
 public class Keyboard{public static Keyboard current;public Button enterKey=new(),spaceKey=new(),escapeKey=new();}
 public class Gamepad{public static Gamepad current;public Button buttonSouth=new(),buttonEast=new(),rightStickButton=new();}
 public class Mouse{public static Mouse current;public Button leftButton=new();}
}
