using RoguePrince.CheatMenu;
using RoguePrince.CheatMenu.Core;
using MotherBase.EntityFramework;
internal static partial class Program
{
    private static GameSession Game(){UnityEngine.Application.isFocused=true;TrainingActions.scope=new(1,1,1,3,0,0);ItemBrowser.Throw=false;return new();}
    private static Entity Enemy()=>new(){Pointer=(IntPtr)2};
    private static DamageSystem.Connection Connection(GameSession g)=>new(){source=g.Player,sourceCaster=new DamageCaster(),sourceItem=new()};
    private static void AdapterTests()
    {
        Check("native adapter player item and collision",()=>{var g=Game();var s=DamageStatisticsSource.Capture(g,Enemy(),Connection(g),null,4);Assert(s!=null&&s.Credit.Kind==DamageCreditKind.Player&&s.Credit.ItemId=="sword-id"&&s.Frame==17&&s.Epoch==4);});
        Check("native adapter spawned collision",()=>{var g=Game();var c=Connection(g);c.sourceSpawnedObject=new(){Pointer=(IntPtr)9};Assert(DamageStatisticsSource.Capture(g,Enemy(),c,null,0)?.Credit.Delivery==DamageDelivery.SpawnedCollision);});
        Check("native adapter direct cast classification",()=>{var g=Game();var c=Connection(g);c.sourceCaster=new DirectDamageCaster();c.sourceSpawnedObject=new(){Pointer=(IntPtr)9};Assert(DamageStatisticsSource.Capture(g,Enemy(),c,null,0)?.Credit.Delivery==DamageDelivery.DirectCast);});
        Check("native adapter source conflict excludes player",()=>{var g=Game();var c=Connection(g);c.source=Enemy();Assert(DamageStatisticsSource.Capture(g,Enemy(),c,g.Player,0)?.Credit.Kind==DamageCreditKind.OtherSource);});
        Check("native adapter null connection fallback",()=>{var g=Game();Assert(DamageStatisticsSource.Capture(g,Enemy(),null,g.Player,0)?.Credit.Kind==DamageCreditKind.Player);});
        Check("native adapter absent source kept unknown",()=>{var g=Game();Assert(DamageStatisticsSource.Capture(g,Enemy(),null,null,0)?.Credit.Kind==DamageCreditKind.UnknownSource);});
        Check("native adapter source item missing safe",()=>{var g=Game();var c=Connection(g);c.sourceItem=null;Assert(DamageStatisticsSource.Capture(g,Enemy(),c,null,0)?.Credit.ItemId=="");});
        Check("native adapter focus and loading reject",()=>{var g=Game();UnityEngine.Application.isFocused=false;Assert(DamageStatisticsSource.Capture(g,Enemy(),Connection(g),null,0)==null);UnityEngine.Application.isFocused=true;g.Ready=false;Assert(DamageStatisticsSource.Capture(g,Enemy(),Connection(g),null,0)==null);});
        Check("native adapter other scene and context reject",()=>{var g=Game();var e=Enemy();e.gameObject.scene=new(){handle=2};Assert(DamageStatisticsSource.Capture(g,e,Connection(g),null,0)==null);e=Enemy();e.context.Pointer=(IntPtr)9;Assert(DamageStatisticsSource.Capture(g,e,Connection(g),null,0)==null);});
        Check("native adapter destroyed and disabled reject",()=>{var g=Game();var e=Enemy();e.destroyed=true;Assert(DamageStatisticsSource.Capture(g,e,Connection(g),null,0)==null);e=Enemy();e.gameObject.activeInHierarchy=false;Assert(DamageStatisticsSource.Capture(g,e,Connection(g),null,0)==null);});
        Check("native adapter captured credit retains values",()=>{var g=Game();var c=Connection(g);var s=DamageStatisticsSource.Capture(g,Enemy(),c,null,0);c.sourceItem.source.name="changed";c.source=Enemy();Assert(s?.Credit.ItemName=="Sword"&&s.Credit.Source==1);});
        Check("native adapter current scope validates",()=>{var g=Game();var e=Enemy();var s=DamageStatisticsSource.Capture(g,e,Connection(g),null,0);Assert(s!=null&&DamageStatisticsSource.IsCurrent(g,s,e));TrainingActions.scope=new(1,1,1,7,0,0);Assert(!DamageStatisticsSource.IsCurrent(g,s,e));});
        Check("native adapter stale target rejected",()=>{var g=Game();var e=Enemy();var s=DamageStatisticsSource.Capture(g,e,Connection(g),null,0);Assert(s!=null);e.Pointer=(IntPtr)3;Assert(!DamageStatisticsSource.IsCurrent(g,s,e));});
        Check("native adapter stale context rejected",()=>{var g=Game();var e=Enemy();var s=DamageStatisticsSource.Capture(g,e,Connection(g),null,0);Assert(s!=null);g.Player.context.Pointer=(IntPtr)10;Assert(!DamageStatisticsSource.IsCurrent(g,s,e));});
    }
}
