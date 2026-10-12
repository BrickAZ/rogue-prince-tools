// Test-only native boundary models. Never included in the plugin.
using RoguePrince.CheatMenu.Core;
namespace MotherBase.EntityFramework
{internal sealed class Entity {internal IntPtr Pointer;internal InventoryComponent Inventory;internal bool destroyed=false;internal string name="Enemy";internal EntityContext context=new();internal UnityEngine.GameObject gameObject=new();internal T GetComponent<T>() where T:class=>Inventory as T;}internal sealed class EntityContext{internal IntPtr Pointer=new(8);}}
internal sealed class Damageable
{
    internal IntPtr Pointer;
    internal MotherBase.EntityFramework.Entity _entity;
    internal float currentHealth,maxHealth;
    internal bool isAlive=>currentHealth>0;
    internal bool onDieActionStarted=false;
}
internal sealed class DamageData {internal bool canNotKillTarget=false;}
internal sealed class DamageSystem
{internal sealed class Connection {internal MotherBase.EntityFramework.Entity source;internal MotherBase.EntityFramework.Entity sourceSpawnedObject=null;internal BaseDamageCaster sourceCaster=null;internal ItemInstanceData sourceItem=null;internal DamageData damageData=new();}}
namespace RoguePrince.CheatMenu
{
    internal sealed class CombatActions
    {
        internal static CombatActions Current;
        internal bool Bound=true,Active=true,Recording=true;
        internal long Generation=1;
        internal readonly GameSession Game=new();
        internal IntPtr Player=new(1),Dummy;
        internal readonly CombatLedger Ledger=new();
        internal int PlayerHits;
        internal bool IsPlayer(MotherBase.EntityFramework.Entity e)=>e?.Pointer==Player;
        internal void HitPlayer()=>PlayerHits++;
    }
}
internal static class CombatBoundaryTests
{
    internal static void Run(Action<string,Action> check)
    {
        void Equal(double a,double b){if(Math.Abs(a-b)>.00001)throw new Exception($"{a} != {b}");}
        var player=new MotherBase.EntityFramework.Entity{Pointer=new(1)};
        Damageable Target(int id,float hp)=>new(){Pointer=new(id+100),_entity=new(){Pointer=new(id)},currentHealth=hp,maxHealth=hp};
        RoguePrince.CheatMenu.CombatActions Setup(){RoguePrince.CheatMenu.DamageHooks.Stack=null;return RoguePrince.CheatMenu.CombatActions.Current=new();}
        check("native damage boundary records actual loss not requested damage",()=>{var c=Setup();var h=Target(2,100);RoguePrince.CheatMenu.DamageHooks.Begin(h,new(){source=player},player,out var op);h.currentHealth=0;RoguePrince.CheatMenu.DamageHooks.End(op,null);Equal(c.Ledger.Total,100);});
        check("native damage overkill clamps observed HP at zero",()=>{var c=Setup();var h=Target(2,100);RoguePrince.CheatMenu.DamageHooks.Begin(h,new(){source=player},player,out var op);h.currentHealth=-500;RoguePrince.CheatMenu.DamageHooks.End(op,null);Equal(c.Ledger.Total,100);});
        check("native dummy nested hit refills only after outer measurement",()=>{var c=Setup();var h=Target(2,100);c.Dummy=h._entity.Pointer;RoguePrince.CheatMenu.DamageHooks.Begin(h,new(){source=player},player,out var outer);h.currentHealth-=10;RoguePrince.CheatMenu.DamageHooks.Begin(h,new(){source=player},player,out var inner);h.currentHealth-=5;RoguePrince.CheatMenu.DamageHooks.End(inner,null);Equal(h.currentHealth,85);RoguePrince.CheatMenu.DamageHooks.End(outer,null);Equal(c.Ledger.Total,15);Equal(c.Ledger.Hits,2);Equal(h.currentHealth,100);});
        check("enemy and environment damage is excluded from outgoing ledger",()=>{var c=Setup();var h=Target(2,100);RoguePrince.CheatMenu.DamageHooks.Begin(h,new(){source=new(){Pointer=new(3)}},null,out var op);h.currentHealth=80;RoguePrince.CheatMenu.DamageHooks.End(op,null);Equal(c.Ledger.Total,0);});
        check("native damage generation change cannot update new-player stats",()=>{var c=Setup();var h=Target(2,100);RoguePrince.CheatMenu.DamageHooks.Begin(h,new(){source=player},player,out var op);h.currentHealth=80;c.Generation++;RoguePrince.CheatMenu.DamageHooks.End(op,null);Equal(c.Ledger.Total,0);});
        check("native damage exception preserves exception path and clears own scope",()=>{var c=Setup();var h=Target(2,100);RoguePrince.CheatMenu.DamageHooks.Begin(h,new(){source=player},player,out var op);h.currentHealth=80;RoguePrince.CheatMenu.DamageHooks.End(op,new Exception("native"));Equal(c.Ledger.Total,0);Equal(RoguePrince.CheatMenu.DamageHooks.Stack.Count,0);});
        check("native player hit counter does not count absorbed or healing writes",()=>{var c=Setup();var h=Target(1,100);RoguePrince.CheatMenu.DamageHooks.Begin(h,new(){source=new(){Pointer=new(3)}},null,out var op);h.currentHealth=110;RoguePrince.CheatMenu.DamageHooks.End(op,null);Equal(c.PlayerHits,0);});
        check("native current player damage counts one received hit",()=>{var c=Setup();var h=Target(1,100);RoguePrince.CheatMenu.DamageHooks.Begin(h,new(){source=new(){Pointer=new(3)}},null,out var op);h.currentHealth=90;RoguePrince.CheatMenu.DamageHooks.End(op,null);Equal(c.PlayerHits,1);Equal(c.Ledger.Total,0);});
    }
}
