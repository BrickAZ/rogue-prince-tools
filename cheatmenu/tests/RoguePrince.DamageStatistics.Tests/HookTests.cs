using RoguePrince.CheatMenu;
using RoguePrince.CheatMenu.Core;
internal static partial class Program
{
    private static CombatActions Combat(){var c=new CombatActions{Game=Game()};CombatActions.Current=c;DamageHooks.Stack=null;return c;}
    private static Damageable Health()=>new(){_entity=Enemy()};
    private static void HookTests()
    {
        Check("hook updates total and detailed actual damage",()=>{var c=Combat();var h=Health();DamageHooks.Begin(h,Connection(c.Game),null,out var op);h.currentHealth=75;DamageHooks.End(op,null);Assert(c.Ledger.Total==25&&c.Ledger.Details.Recent.Single().Damage==25);});
        Check("hook conflict changes statistics only",()=>{var c=Combat();var h=Health();var conn=Connection(c.Game);conn.source=Enemy();DamageHooks.Begin(h,conn,c.Game.Player,out var op);Assert(op.Outgoing);h.currentHealth=80;DamageHooks.End(op,null);Assert(c.Ledger.Total==0&&c.Ledger.Details.OtherHits==1);});
        Check("hook nested same target counted once",()=>{var c=Combat();var h=Health();DamageHooks.Begin(h,Connection(c.Game),null,out var outer);DamageHooks.Begin(h,Connection(c.Game),null,out var inner);h.currentHealth=80;DamageHooks.End(inner,null);h.currentHealth=70;DamageHooks.End(outer,null);Assert(c.Ledger.Total==30&&c.Ledger.Details.Groups.Sum(x=>x.Damage)==30&&c.Ledger.Details.Recent.Count==2);});
        Check("hook nested enemy damage not assigned to outer player",()=>{var c=Combat();var h=Health();DamageHooks.Begin(h,Connection(c.Game),null,out var outer);var conn=Connection(c.Game);conn.source=Enemy();DamageHooks.Begin(h,conn,null,out var inner);h.currentHealth=80;DamageHooks.End(inner,null);h.currentHealth=70;DamageHooks.End(outer,null);Assert(c.Ledger.Total==10&&c.Ledger.Details.OtherHits==1&&c.Ledger.Details.Recent.Single().Damage==10);});
        Check("hook room change discards pending statistics",()=>{var c=Combat();var h=Health();DamageHooks.Begin(h,Connection(c.Game),null,out var op);TrainingActions.scope=new(1,1,1,7,0,0);h.currentHealth=80;DamageHooks.End(op,null);Assert(c.Ledger.Total==0);});
        Check("hook ledger clear discards pending statistics",()=>{var c=Combat();var h=Health();DamageHooks.Begin(h,Connection(c.Game),null,out var op);c.Ledger.Reset();h.currentHealth=80;DamageHooks.End(op,null);Assert(c.Ledger.Total==0);});
        Check("hook generation change discards pending statistics",()=>{var c=Combat();var h=Health();DamageHooks.Begin(h,Connection(c.Game),null,out var op);c.Game.Generation++;h.currentHealth=80;DamageHooks.End(op,null);Assert(c.Ledger.Total==0);});
        Check("hook unknown source excluded but diagnosed",()=>{var c=Combat();var h=Health();DamageHooks.Begin(h,null,null,out var op);h.currentHealth=80;DamageHooks.End(op,null);Assert(c.Ledger.Total==0&&c.Ledger.Details.UnknownHits==1);});
        Check("hook original exception leaves stack usable",()=>{var c=Combat();var h=Health();DamageHooks.Begin(h,Connection(c.Game),null,out var op);h.currentHealth=80;DamageHooks.End(op,new Exception("original"));Assert(c.Ledger.Total==0&&DamageHooks.Stack.Count==0);});
        Check("hook disabled recording does not read origin metadata",()=>{var c=Combat();c.Recording=false;ItemBrowser.Throw=true;var h=Health();DamageHooks.Begin(h,Connection(c.Game),null,out var op);h.currentHealth=80;DamageHooks.End(op,null);Assert(c.Ledger.Total==0&&c.Recording==false);ItemBrowser.Throw=false;});
        Check("hook metadata failure disables statistics preserves modes",()=>{var c=Combat();c.OneHit=true;c.DamageMultiplier=2;ItemBrowser.Throw=true;var h=Health();DamageHooks.Begin(h,Connection(c.Game),null,out var op);h.currentHealth=80;DamageHooks.End(op,null);Assert(!c.Recording&&c.OneHit&&c.DamageMultiplier==2&&DamageHooks.Stack.Count==0);ItemBrowser.Throw=false;});
    }
}
