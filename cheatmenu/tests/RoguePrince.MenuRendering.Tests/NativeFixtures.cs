using UnityEngine;
namespace Il2CppInterop.Runtime.Attributes{public class HideFromIl2CppAttribute:Attribute{}}
namespace UnityEngine
{
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 zero=>new(0,0);}
 public struct Rect{public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public bool Contains(Vector2 p)=>p.x>=x&&p.x<x+width&&p.y>=y&&p.y<y+height;}
 public struct Color{public Color(float r,float g,float b,float a=1){}public static Color white=>new(1,1,1);}
 public enum FontStyle{Normal}public enum EventType{Repaint,MouseDown,MouseUp,MouseDrag,ScrollWheel,Used}
 public class Event{public static Event current=new();public EventType type;public int button;public Vector2 mousePosition,delta;public void Use()=>type=EventType.Used;}
 public class GUIContent{public string text;public GUIContent(string text)=>this.text=text;}
 public class GUIStyle{public int fontSize;public FontStyle fontStyle;public bool wordWrap;public float fixedWidth=16;public GUIStyleState normal=new();public float Height=300;public float CalcHeight(GUIContent c,float width)=>Height;}
 public class GUIStyleState{public Color textColor;}
 public class GUISkin{public GUIStyle verticalScrollbar=new();}
 public static class GUI
 {
  public static GUISkin skin=new();public static Color color;public static int Depth,Begins,Ends,Labels,Unsupported;public static bool FailLabel;public static Vector2 Offset;public static Rect ClipRect;
  public static Vector2 BeginScrollView(Rect r,Vector2 p,Rect v,bool h,bool w){Unsupported++;throw new NotSupportedException("Method unstripping failed");}
  public static void EndScrollView(){Unsupported++;throw new NotSupportedException("Method unstripping failed");}
  public static void BeginClip(Rect r,Vector2 offset,Vector2 renderOffset,bool reset){Depth++;Begins++;Offset=offset;ClipRect=r;}
  public static void EndClip(){Depth--;Ends++;}
  public static void Label(Rect r,string text,GUIStyle style){Labels++;if(FailLabel)throw new InvalidOperationException("injected label failure");}
  public static void Reset(){Depth=Begins=Ends=Labels=Unsupported=0;FailLabel=false;Event.current=new(){type=EventType.Repaint};}
 }
 public enum CursorLockMode{None,Locked}
 public static class Cursor{public static CursorLockMode lockState;public static bool visible;}
 public static class Time{public static float timeScale=1,realtimeSinceStartup=1;}
 public class GameObject{public bool activeInHierarchy=true;}
 public class Object{public static UnityEngine.EventSystems.EventSystem[] Systems=Array.Empty<UnityEngine.EventSystems.EventSystem>();public static T[] FindObjectsOfType<T>()=>Systems.Cast<T>().ToArray();}
}
namespace UnityEngine.EventSystems
{
 public class BaseInputModule{public bool enabled=true;}
 public class EventSystem{public bool enabled=true;public BaseInputModule[] Modules={new()};public T[] GetComponents<T>()=>Modules.OfType<T>().ToArray();}
}
namespace MotherBase.ActionFramework
{
 public class PlayerInput
 {
  public IntPtr Pointer=new(1);public bool _isListeningUnityInput=true;public int activeInputs,lastActiveInputs;public Vector2 moveInputValue,aimInputValue;public GameObject gameObject=new();public int Resets;
  public void StartListenUnityInput()=>_isListeningUnityInput=true;public void StopListenUnityInput()=>_isListeningUnityInput=false;public void ResetInputStates()=>Resets++;
 }
}
namespace RoguePrince.CheatMenu.Core{public static class MenuText{public static string Get(string s)=>s;}}
namespace RoguePrince.CheatMenu
{
 public class GameSession{public Player Player=new();}
 public class Player{public bool destroyed;public MotherBase.ActionFramework.PlayerInput Input=new();public T GetComponent<T>()where T:class=>Input as T;}
 public static class Plugin{public static Log MenuLog=new();public static void LogEvent(string s){}}public class Log{public void LogWarning(string s){}}
 public sealed partial class MenuController
 {
  private float scale=1,releaseInputAt;private GUIStyle textStyle=new();private string focus="",pressed="",actionKeyCapture;private bool visible,dragging,hotkeyCapture;private int tab;
  private readonly InputLease inputLease=new();private static Color Accent=new(0,1,1);public int Boxes;
  private Rect R(float a,float b,float w,float h)=>new(a*scale,b*scale,w*scale,h*scale);
  private void Box(float a,float b,float w,float h,Color c)=>Boxes++;
  public Vector2 Draw(Vector2 scroll)=>AffixText(0,0,100,60,"词缀效果",scroll);
  public void ContentHeight(float height)=>textStyle.Height=height;
  public void Scale(float value)=>scale=value;
  public void Arm(GameSession game){visible=true;tab=6;focus="affixSearch";pressed="affixRow";dragging=hotkeyCapture=true;actionKeyCapture="binding";releaseInputAt=10;inputLease.Acquire(game);}
  public void Recover()=>RecoverDrawing();
  public bool Clean=>!visible&&tab==0&&focus==""&&pressed==""&&!dragging&&!hotkeyCapture&&actionKeyCapture==null&&releaseInputAt==0;
  public bool Wrap=>textStyle.wordWrap;
 }
}
