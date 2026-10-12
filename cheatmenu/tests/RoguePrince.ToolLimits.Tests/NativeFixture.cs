using RoguePrince.CheatMenu.Core;
namespace UnityEngine
{
 public static class Application {public static bool isFocused=true;}
 public sealed class GameObject {public bool activeInHierarchy=true;public Scene scene=new();}
 public sealed class Scene {public int handle=3;}
}
namespace MotherBase.EntityFramework
{
 public sealed class Entity
 {
  public IntPtr Pointer;public bool destroyed;public UnityEngine.GameObject gameObject=new();public int Reads;public bool ThrowRead;public Action BeforeRead;
  public readonly Dictionary<Type,object> Components=new();
  public T GetComponent<T>() where T:class {Reads++;BeforeRead?.Invoke();if(ThrowRead)throw new InvalidOperationException("native read");return Components.GetValueOrDefault(typeof(T)) as T;}
 }
}
public class NativeObject {private static long serial=10;public IntPtr Pointer=(IntPtr)(++serial);}
public sealed class ItemData:NativeObject
{
 public string name="Tool",title="Tool";public bool Utility=true;public PickableAmmoFeature Ammo;
 public T GetComponent<T>() where T:class=>typeof(T)==typeof(UtilityWeaponFeature)?(Utility?new UtilityWeaponFeature():null) as T:Ammo as T;
}
public sealed class ItemInstanceData:NativeObject
{
 public ItemData source=new();public RemainingUses Uses;
 public T GetComponent<T>() where T:class=>Uses as T;
}
public sealed class RemainingUses:NativeObject {public int remainingUses=4;public void OnUse(MotherBase.EntityFramework.Entity owner,InventoryComponent inventory,ItemInstanceData item){remainingUses--;}}
public sealed class UtilityWeaponFeature {}
public sealed class PickableAmmoFeature {public int maxAmmoCount=5;}
public sealed class InventoryComponent:NativeObject
{
 public ItemInstanceData Tool=new(){Uses=new()};public bool HasTool=true,Contains=true;
 public bool HasItem(ItemInstanceData item)=>Contains&&item?.Pointer==Tool.Pointer;
 public bool TryGetSecondaryWeapon(out ItemInstanceData tool){tool=Tool;return HasTool;}
}
public sealed class ProjectilePickerComponent:NativeObject
{
 public int remainingProjectileCount=3,Calls;public readonly List<object> projectiles=new(){new(),new()};
 public bool ThrowReset,ThrowAfterPartialReset,FailReadback;public Action DuringReset;
 public void ResetProjectiles(){Calls++;if(ThrowReset)throw new InvalidOperationException("native reset");if(ThrowAfterPartialReset){remainingProjectileCount++;projectiles.RemoveAt(0);throw new InvalidOperationException("partial native reset");}if(!FailReadback){remainingProjectileCount+=projectiles.Count;projectiles.Clear();}DuringReset?.Invoke();}
}
namespace RoguePrince.CheatMenu
{
 internal sealed class GameSession
 {
  internal MotherBase.EntityFramework.Entity Player=new(){Pointer=(IntPtr)1};internal bool Ready=true;internal IntPtr PlayerPointer=>Player.Pointer;
 }
 internal sealed class CombatActions
 {
  internal static CombatActions Current;internal GameSession Game=new();internal bool ModesBound=true,OneHit=true,Undying=true;internal ToolLimitActions Tools;
  internal void Require(){if(!Game.Ready||!UnityEngine.Application.isFocused)throw new InvalidOperationException("not ready");}
  internal bool IsPlayer(MotherBase.EntityFramework.Entity owner)=>owner?.Pointer==Game.PlayerPointer;
 }
 internal static class TrainingActions
 {
  internal static TrainingScope Scope=new(1,1,3,4,0,0);
  internal static TrainingScope ReadScope(GameSession game)=>Scope;
 }
 internal static class ItemBrowser {internal sealed class Label {internal string Display;}internal static Label ReadLabel(string title,string name)=>new(){Display=name};}
 internal static class Plugin {internal static readonly TestLog MenuLog=new();internal static void LogEvent(string text){} }
 internal sealed class TestLog {internal void LogWarning(object text){} }
}
