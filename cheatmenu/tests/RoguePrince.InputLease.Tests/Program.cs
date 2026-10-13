using System.Reflection;
using RoguePrince.CheatMenu;
using UnityEngine;
using UnityEngine.EventSystems;
int passed=0,failed=0;
void Assert(bool value,string why){if(!value)throw new Exception(why);}
void Test(string name,Action body)
{
 Time.timeScale=.75f;Time.realtimeSinceStartup=1;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
 UnityEngine.Object.Systems=Array.Empty<EventSystem>();MenuController.Instance=null;
 UnityEngine.InputSystem.Keyboard.current=null;UnityEngine.InputSystem.Gamepad.current=null;UnityEngine.InputSystem.Mouse.current=null;
 try{body();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+(e.InnerException??e).Message);}
}
(GameSession game,EventSystem ui,InputLease lease) Setup()
{var game=new GameSession();var ui=new EventSystem();UnityEngine.Object.Systems=new[]{ui};return(game,ui,new InputLease());}
bool Active(InputLease lease)=>(bool)(typeof(InputLease).GetProperty("Active",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(lease)??false);
bool NativeAllowed()
{
 var patch=typeof(InputLease).Assembly.GetType("RoguePrince.CheatMenu.MenuPauseInputPatch");
 if(patch==null)throw new Exception("native pause input scope guard missing");
 return (bool)patch.GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
}
Test("native pause view keeps a live EventSystem during menu capture",()=>{var(g,ui,l)=Setup();l.Acquire(g);NativePauseView.Hide();Assert(ui.enabled&&ui.Selections==1,"native selection dependency disabled");});
Test("capture disables UI input modules while retaining the EventSystem",()=>{var(g,ui,l)=Setup();l.Acquire(g);Assert(ui.enabled&&!ui.Modules[0].enabled&&!g.Player.Input._isListeningUnityInput&&Time.timeScale==0,"menu does not own only input modules/player/pause");});
Test("closed menu returns the acquired UI module and native selection",()=>{var(g,ui,l)=Setup();l.Acquire(g);l.Release();NativePauseView.Hide();Assert(ui.enabled&&ui.Modules[0].enabled&&g.Player.Input._isListeningUnityInput&&Time.timeScale==.75f&&!Active(l),"input release incomplete");});
Test("initially disabled modules and external pause remain owned externally",()=>{var(g,ui,l)=Setup();var disabled=new BaseInputModule{enabled=false};ui.Modules=new[]{ui.Modules[0],disabled};g.Player.Input._isListeningUnityInput=false;Time.timeScale=0;l.Acquire(g);l.Release();Assert(ui.enabled&&ui.Modules[0].enabled&&!disabled.enabled&&!g.Player.Input._isListeningUnityInput&&Time.timeScale==0,"external input or pause changed");});
Test("an inactive EventSystem is not enabled by cleanup",()=>{var(g,ui,l)=Setup();ui.enabled=false;l.Acquire(g);l.Release();Assert(!ui.enabled&&ui.Modules[0].enabled,"unowned UI was enabled");});
Test("new UI modules are captured on the next scan without disabling systems",()=>{var(g,ui,l)=Setup();l.Acquire(g);var second=new EventSystem();UnityEngine.Object.Systems=new[]{ui,second};Time.realtimeSinceStartup=2;l.Acquire(g);Assert(ui.enabled&&second.enabled&&!second.Modules[0].enabled,"new native system not safely captured");l.Release();Assert(ui.Modules[0].enabled&&second.Modules[0].enabled,"new module not returned");});
Test("repeated captures do not duplicate module release ownership",()=>{var(g,ui,l)=Setup();l.Acquire(g);Time.realtimeSinceStartup=2;l.Acquire(g);l.Release();Assert(ui.Modules[0].Changes==2,"module was disabled or restored more than once");});
Test("release restores cursor and is idempotent",()=>{var(g,ui,l)=Setup();l.Acquire(g);l.Release();l.Release();Assert(Cursor.lockState==CursorLockMode.Locked&&!Cursor.visible&&Time.timeScale==.75f,"repeat cleanup changed released state");});
Test("failed UI module restore is retained for retry",()=>{var(g,ui,l)=Setup();l.Acquire(g);ui.Modules[0].FailEnables=1;l.Release();Assert(Active(l)&&!ui.Modules[0].enabled,"failed module restore was forgotten");l.Release();Assert(!Active(l)&&ui.Modules[0].enabled,"module retry did not complete");});
Test("one failed module does not prevent other modules from returning",()=>{var(g,ui,l)=Setup();var second=new BaseInputModule();ui.Modules=new[]{ui.Modules[0],second};l.Acquire(g);ui.Modules[0].FailEnables=1;l.Release();Assert(second.enabled&&Active(l),"one failure stopped independent cleanup");l.Release();Assert(!Active(l)&&ui.Modules[0].enabled,"pending module not returned");});
Test("failed player resume is retained for retry",()=>{var(g,ui,l)=Setup();l.Acquire(g);g.Player.Input.FailStarts=1;l.Release();Assert(Active(l)&&!g.Player.Input._isListeningUnityInput,"failed player resume was forgotten");l.Release();Assert(!Active(l)&&g.Player.Input._isListeningUnityInput,"player retry did not complete");});
Test("native Escape remains available without a CheatMenu instance",()=>Assert(NativeAllowed(),"global pause blocked"));
Test("native Escape is blocked while CheatMenu is visible",()=>{var(g,ui,l)=Setup();new MenuController().Open(g);Assert(!NativeAllowed(),"native pause reentered while menu visible");});
Test("native Escape is blocked through delayed release and restored afterwards",()=>{var(g,ui,l)=Setup();var menu=new MenuController();menu.Open(g);menu.CloseWithoutRelease();Assert(!NativeAllowed(),"native pause entered closing frame");menu.Release();Assert(NativeAllowed(),"native pause remained blocked after actual release");NativePauseView.Hide();});
Test("native Escape remains blocked during failed release and returns on retry",()=>{var(g,ui,l)=Setup();var menu=new MenuController();menu.Open(g);menu.CloseWithoutRelease();ui.Modules[0].FailEnables=1;menu.Release();Assert(!NativeAllowed(),"native pause entered partially released UI");menu.Release();Assert(NativeAllowed(),"native pause stuck after successful retry");});
Test("quit cleanup does not reenable native UI or player objects",()=>{var(g,ui,l)=Setup();l.Acquire(g);l.Release(true);Assert(!Active(l)&&!ui.Modules[0].enabled&&!g.Player.Input._isListeningUnityInput,"quit touched native input");});
bool Held()=>(bool)typeof(MenuController).GetMethod("ConfirmStillHeld",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,null);
Test("held Escape delays closing handoff until key release",()=>{var keyboard=new UnityEngine.InputSystem.Keyboard();UnityEngine.InputSystem.Keyboard.current=keyboard;keyboard.escapeKey.isPressed=true;Assert(Held(),"Escape press leaked into native pause");keyboard.escapeKey.isPressed=false;Assert(!Held(),"Escape release kept handoff blocked");});
Test("held mouse click cannot leak to a reenabled native UI module",()=>{var mouse=new UnityEngine.InputSystem.Mouse();UnityEngine.InputSystem.Mouse.current=mouse;mouse.leftButton.isPressed=true;Assert(Held(),"close click reached native UI");mouse.leftButton.isPressed=false;Assert(!Held(),"mouse release kept handoff blocked");});
Test("controller cancel and R3 presses delay handoff through release",()=>{var pad=new UnityEngine.InputSystem.Gamepad();UnityEngine.InputSystem.Gamepad.current=pad;foreach(var button in new[]{pad.buttonEast,pad.rightStickButton}){button.isPressed=true;Assert(Held(),"controller close press leaked");button.isPressed=false;}Assert(!Held(),"released controller kept handoff blocked");});
Test("missing input devices do not retain a closing lease",()=>Assert(!Held(),"missing devices kept closing lease"));
Console.WriteLine($"RESULT {passed} passed, {failed} failed");Environment.ExitCode=failed==0?0:1;
