using System.Reflection;
using RoguePrince.CheatMenu.Core;

internal static class MobCatalogTests
{
    private static Type Catalog=>typeof(MenuText).Assembly.GetType("RoguePrince.CheatMenu.Core.MobCatalogPolicy")??throw new Exception("Missing production feature: selectable enemy catalog");
    private static object Find(string id)=>Catalog.GetMethod("Find").Invoke(null,new object[]{id});
    private static string[] Filter(bool boss,string query)=>((System.Collections.IEnumerable)Catalog.GetMethod("Filter").Invoke(null,new object[]{boss,query})).Cast<object>().Select(e=>(string)e.GetType().GetProperty("Id").GetValue(e)).ToArray();
    private static void Assert(bool value){if(!value)throw new Exception("Unexpected catalog behavior");}
    public static void Run(Action<string,Action> check)
    {
        check("unknown or empty enemy selection never falls back to swordsman",()=>{foreach(var id in new[]{null,"","missing"})Assert(Find(id)==null);});
        check("regular section searches preserve the palace variant as a separate target",()=>{Assert(Filter(false,"  archer ").SequenceEqual(new[]{"Archer","Archer-Palace"}));});
        check("native bosses without Boss in their resource name stay in the boss section",()=>{Assert(Filter(true,"Nogai").SequenceEqual(new[]{"CorruptedNogai","Nogai"}));Assert(Filter(false,"Nogai").Length==0);});
        check("boss prefab-only entries are available as distinct selected targets",()=>{foreach(var id in new[]{"AltanBoss","GoatBoss","Mole","BaatarBoss"}){Assert(Find(id)!=null);Assert(Filter(true,id).Contains(id));Assert(!Filter(false,id).Contains(id));}});
        check("enemy search accepts Chinese names in either interface language",()=>{foreach(var lang in new[]{"zh-CN","en-US"}){MenuText.SetLanguage(lang);Assert(Filter(false,"弓箭手").SequenceEqual(new[]{"Archer","Archer-Palace"}));}MenuText.SetLanguage("zh-CN");});
        check("enemy request freezes its selected identity instead of reading later menu selection",()=>{
            var constructor=typeof(MobSpawnOptions).GetConstructor(new[]{typeof(MobSpawnHealthMode),typeof(bool),typeof(string)})??throw new Exception("Missing selected target in production request");
            var original=(MobSpawnOptions)constructor.Invoke(new object[]{MobSpawnHealthMode.NativeFull,true,"Archer"});
            var gate=new MobSpawnRequestGate();Assert(gate.TryBegin(4,original,out var request));
            var later=(MobSpawnOptions)constructor.Invoke(new object[]{MobSpawnHealthMode.OneHp,false,"Nogai"});
            Assert(!gate.TryBegin(4,later,out _));Assert((string)typeof(MobSpawnOptions).GetProperty("MobId").GetValue(gate.Options)=="Archer");Assert(gate.CanComplete(request,4));
        });
        check("missing or unknown request target is rejected before native generation",()=>{
            var constructor=typeof(MobSpawnOptions).GetConstructor(new[]{typeof(MobSpawnHealthMode),typeof(bool),typeof(string)})??throw new Exception("Missing selected target in production request");
            foreach(var id in new[]{null,"","missing"}){var gate=new MobSpawnRequestGate();var options=(MobSpawnOptions)constructor.Invoke(new object[]{MobSpawnHealthMode.NativeFull,true,id});Assert(!gate.TryBegin(1,options,out _));Assert(!gate.Busy);}
        });
        check("AI control accepts both regular and boss native brain families",()=>{
            var policy=typeof(MenuText).Assembly.GetType("RoguePrince.CheatMenu.Core.MobAiPolicy")??throw new Exception("Missing generalized native AI policy");
            foreach(var row in new[]{(true,true,false,true),(true,false,true,true),(true,false,false,false),(false,true,false,false),(false,false,true,false)})Assert((bool)policy.GetMethod("Supports").Invoke(null,new object[]{row.Item1,row.Item2,row.Item3})==row.Item4);
        });
        check("Nogai can be selected for a native request despite its empty stock spawn reference",()=>{
            var constructor=typeof(MobSpawnOptions).GetConstructor(new[]{typeof(MobSpawnHealthMode),typeof(bool),typeof(string)})??throw new Exception("Missing selected target in production request");
            var gate=new MobSpawnRequestGate();Assert(gate.TryBegin(1,(MobSpawnOptions)constructor.Invoke(new object[]{MobSpawnHealthMode.NativeFull,true,"Nogai"}),out _));
        });
    }
}
