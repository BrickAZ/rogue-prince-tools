// Offline native-interface substitutes. Never referenced by the plugin.
namespace UnityEngine
{
 public static class Application{public static bool isFocused=true;}
 public static class Time{public static int frameCount=17;}
 public struct Scene{public int handle;}
 public class GameObject{public bool activeInHierarchy=true;public Scene scene=new(){handle=1};}
}
namespace MotherBase.EntityFramework
{
 public class EntityContext{public IntPtr Pointer=(IntPtr)8;}
 public class Entity{public IntPtr Pointer;public bool destroyed;public string name="Enemy";public EntityContext context=new();public UnityEngine.GameObject gameObject=new();}
}
public class BaseDamageCaster{public T TryCast<T>()where T:class=>this as T;}
public class DamageCaster:BaseDamageCaster{}
public class DirectDamageCaster:BaseDamageCaster{}
public class FakeGuid{public string guid="sword-id";}
public class ItemData{public FakeGuid Guid=new();public string name="Sword";public object title=new();}
public class ItemInstanceData{public ItemData source=new();}
public class DamageSystem
{
 public class Connection{public MotherBase.EntityFramework.Entity source,sourceSpawnedObject;public BaseDamageCaster sourceCaster;public ItemInstanceData sourceItem;public DamageData damageData=new();}
}
public class DamageData{public bool canNotKillTarget;}
public class Damageable{public MotherBase.EntityFramework.Entity _entity;public float currentHealth=100,maxHealth=100;public bool isAlive=true,onDieActionStarted;}
namespace RoguePrince.CheatMenu
{
 internal class GameSession{public bool Ready=true;public long Generation=1;public MotherBase.EntityFramework.Entity Player=new(){Pointer=(IntPtr)1,name="Player"};public IntPtr PlayerPointer=>Player.Pointer;}
 internal static class TrainingActions{public static Core.TrainingScope scope=new(1,1,1,3,0,0);public static Core.TrainingScope ReadScope(GameSession game)=>game.Ready?scope:default;}
 internal static class ItemBrowser{internal record Label(string Display);public static bool Throw;public static Label ReadLabel(object title,string name)=>Throw?throw new Exception("native item read"):new(name);}
 internal static class Plugin{public static FakeLog MenuLog=new();}
 internal class FakeLog{public int warnings;public void LogWarning(string text){warnings++;}}
 internal class CombatActions
 {
  public static CombatActions Current;public GameSession Game=new();public bool Bound=true,Recording=true,OneHit;public float DamageMultiplier=1;public bool Active=true;public IntPtr Dummy=IntPtr.Zero;public Core.CombatLedger Ledger=new();public int playerHits;
  public long Generation=>Game.Generation;public bool IsPlayer(MotherBase.EntityFramework.Entity e)=>e?.Pointer==Game.PlayerPointer;public void HitPlayer(){playerHits++;}
 }
}
