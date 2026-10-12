using RoguePrince.CheatMenu.Core;
internal static partial class Program
{
    private static int passed,failed;
    internal static void Check(string name,Action test){try{test();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
    internal static void Assert(bool value){if(!value)throw new Exception("Assertion failed");}
    internal static DamageCredit Player(string id="sword",DamageDelivery delivery=DamageDelivery.Collision)=>DamageCreditPolicy.Resolve(1,1,0,false,delivery,id,"Sword","Enemy");
    private static void Main()
    {
        Check("native player source credited",()=>Assert(Player().Kind==DamageCreditKind.Player));
        Check("explicit source fallback credited",()=>Assert(DamageCreditPolicy.Resolve(1,0,1,false,DamageDelivery.DirectCast,"","","E").Kind==DamageCreditKind.Player));
        Check("native enemy wins conflicting fallback",()=>Assert(DamageCreditPolicy.Resolve(1,2,1,false,DamageDelivery.Collision,"","","E").Kind==DamageCreditKind.OtherSource));
        Check("missing source remains unknown",()=>Assert(DamageCreditPolicy.Resolve(1,0,0,false,DamageDelivery.Unknown,"","","E").Kind==DamageCreditKind.UnknownSource));
        Check("self damage excluded",()=>Assert(DamageCreditPolicy.Resolve(1,1,0,true,DamageDelivery.Collision,"","","P").Kind==DamageCreditKind.SelfDamage));
        Check("source free player damage tracked as self",()=>{var s=new DamageStatistics();var c=DamageCreditPolicy.Resolve(1,0,0,true,DamageDelivery.Unknown,"","","P");Assert(c.Kind==DamageCreditKind.SelfDamage&&!s.Record(1,5,0,c,1,1));Assert(s.SelfHits==1&&s.UnknownHits==0&&s.Groups.Count==0&&s.Recent.Count==0);});
        Check("no bound player cannot credit zero",()=>{foreach(var isPlayer in new[]{false,true})Assert(DamageCreditPolicy.Resolve(0,0,0,isPlayer,DamageDelivery.Unknown,null,null,null).Kind==DamageCreditKind.UnknownSource);});
        Check("text normalized and bounded",()=>{var c=DamageCreditPolicy.Resolve(1,1,0,false,DamageDelivery.Collision,new string('x',1000),null,"A\nB");Assert(c.ItemId.Length<=128&&c.ItemName!=null&&!c.TargetName.Contains('\n'));});
        Check("records actual net loss",()=>{var s=new DamageStatistics();Assert(s.Record(2,30,10,Player(),1,12));Assert(s.Recent.Single().Damage==20&&s.Groups.Single().Damage==20);});
        Check("healing zero and fully nested ignored",()=>{var s=new DamageStatistics();Assert(!s.Record(2,-1,0,Player(),1,1)&&!s.Record(2,0,0,Player(),1,1)&&!s.Record(2,3,3,Player(),1,1));Assert(s.Recent.Count==0);});
        Check("nonfinite and negative nested rejected",()=>{var s=new DamageStatistics();foreach(var n in new[]{double.NaN,double.PositiveInfinity,-1})Assert(!s.Record(2,5,n,Player(),1,1));Assert(!s.Record(2,double.NaN,0,Player(),1,1));});
        Check("unknown and enemy tracked outside totals",()=>{var s=new DamageStatistics();s.Record(2,5,0,DamageCreditPolicy.Resolve(1,0,0,false,DamageDelivery.Unknown,"","",""),1,1);s.Record(2,5,0,DamageCreditPolicy.Resolve(1,3,0,false,DamageDelivery.Collision,"","",""),1,1);Assert(s.UnknownHits==1&&s.OtherHits==1&&s.Groups.Count==0&&s.Recent.Count==0);});
        Check("self tracked separately",()=>{var s=new DamageStatistics();Assert(!s.Record(1,5,0,DamageCreditPolicy.Resolve(1,1,0,true,DamageDelivery.Collision,"","",""),1,1));Assert(s.SelfHits==1);});
        Check("stable item id ignores localized labels",()=>{var s=new DamageStatistics();s.Record(2,5,0,Player(),1,1);s.Record(2,7,0,Player() with{ItemName="剑"},1,2);Assert(s.Groups.Count==1&&s.Groups[0].Damage==12&&s.Groups[0].Hits==2);});
        Check("native delivery paths separated",()=>{var s=new DamageStatistics();foreach(var d in Enum.GetValues<DamageDelivery>())s.Record(2,5,0,Player(delivery:d),1,1);Assert(s.Groups.Count==4);});
        Check("same label different assets separated",()=>{var s=new DamageStatistics();s.Record(2,5,0,Player("a"),1,1);s.Record(2,5,0,Player("b"),1,1);Assert(s.Groups.Count==2);});
        Check("group overflow retains damage bounded",()=>{var s=new DamageStatistics();for(int i=0;i<50;i++)s.Record(2,1,0,Player(i.ToString()),1,i);Assert(s.Groups.Count==DamageStatistics.GroupLimit&&s.Groups.Sum(x=>x.Damage)==50&&s.Groups.Single(x=>x.Overflow).Hits==19);});
        Check("recent ring keeps latest hits and sequence",()=>{var s=new DamageStatistics();for(int i=0;i<100;i++)s.Record(2,1,0,Player(),3,i);Assert(s.Recent.Count==64&&s.Recent[0].Sequence==37&&s.Recent[^1].Frame==99&&s.Recent[^1].Generation==3);});
        Check("clear retires in flight epoch and all counters",()=>{var s=new DamageStatistics();s.Record(2,1,0,Player(),1,1);var epoch=s.Epoch;s.Clear();Assert(s.Epoch!=epoch&&s.Groups.Count==0&&s.Recent.Count==0&&s.OtherHits==0&&s.UnknownHits==0);});
        Check("export only managed data round trips",()=>{var s=new DamageStatistics();s.Record(2,1,0,Player(),1,1);var json=System.Text.Json.JsonSerializer.Serialize(new{s.Groups,s.Recent});Assert(json.Contains("Sword")&&json.Contains("Generation"));});
        AdapterTests();
        HookTests();
        Console.WriteLine($"RESULT {passed} passed, {failed} failed");Environment.ExitCode=failed==0?0:1;
    }
}
