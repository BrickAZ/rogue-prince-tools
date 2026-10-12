using System.Reflection;
using RoguePrince.CheatMenu.Core;

internal static class UtilityTests
{
    private static Type T(string name) => typeof(MenuText).Assembly.GetType("RoguePrince.CheatMenu.Core." + name) ?? throw new Exception("Missing production feature: " + name);
    private static dynamic New(string name, params object[] args) => Activator.CreateInstance(T(name), args);
    private static object Call(string type, string method, params object[] args) => T(type).GetMethod(method).Invoke(null, args);
    private static void Assert(bool ok) { if (!ok) throw new Exception("Unexpected utility behavior"); }
    private static void Reject(Action action) { try { action(); } catch (Exception) { return; } throw new Exception("Invalid input accepted"); }
    private const string Good = "{\"Name\":\"练习 🔥\",\"ItemId\":\"weapon-guid\",\"Tier\":5,\"AffixIds\":[\"affix-guid\"]}";
    public static void Run(Action<string, Action> check)
    {
        check("preset Unicode name and IDs survive clipboard roundtrip", () => {
            dynamic p = Call("WeaponPreset", "Parse", Good);
            dynamic copy = Call("WeaponPreset", "Parse", (string)p.Export());
            Assert(copy.Name == "练习 🔥" && copy.ItemId == "weapon-guid" && copy.Tier == 5 && copy.AffixIds.Count == 1 && copy.AffixIds[0] == "affix-guid");
        });
        foreach (var json in new[] { "null", "{}", Good.Replace("weapon-guid", ""), Good.Replace("5,", "0,"), Good.Replace("5,", "101,"), Good.Replace("[\"affix-guid\"]", "[\"x\",\"x\"]"), Good.Replace("[\"affix-guid\"]", "null"), Good.Replace("练习 🔥", " ") })
            check("invalid preset rejects " + json, () => { T("WeaponPreset"); Reject(() => Call("WeaponPreset", "Parse", json)); });
        check("oversized import is rejected before deserialization", () => { T("WeaponPreset"); Reject(() => Call("WeaponPreset", "Parse", new string(' ', 70000) + Good)); });
        check("favorites and preset library survive restart", () => WithFile(path => {
            dynamic lib = New("UtilityLibrary", path);
            lib.ToggleFavorite("item", "weapon-guid"); lib.AddPreset((dynamic)Call("WeaponPreset", "Parse", Good));
            dynamic reread = New("UtilityLibrary", path);
            Assert(reread.IsFavorite("item", "weapon-guid") && !reread.IsFavorite("mob", "weapon-guid") && reread.Data.Presets.Count == 1);
            reread.ToggleFavorite("item", "weapon-guid"); Assert(!New("UtilityLibrary", path).IsFavorite("item", "weapon-guid"));
        }));
        foreach(var invalid in new[] {"{oops", "{\"Version\":99}", "null"})
            check("damaged or future library is preserved " + invalid, () => WithFile(path => {
                File.WriteAllText(path, invalid); dynamic lib = New("UtilityLibrary", path);
                Assert(lib.ReadOnly); Reject(() => lib.ToggleFavorite("item", "a")); Assert(File.ReadAllText(path) == invalid);
            }));
        check("failed persistence does not commit favorite to memory", () => WithFile(path => {
            dynamic lib = New("UtilityLibrary", path); Directory.CreateDirectory(path);
            Reject(() => lib.ToggleFavorite("item", "a")); Assert(!lib.IsFavorite("item", "a"));
        }));
        check("action keys reject reserved menu and duplicate bindings", () => {
            var bindings = new Dictionary<string,string>{{"heal","F2"}};
            Reject(() => Call("ActionKeyPolicy", "Validate", "energy", "F2", "F9", bindings));
            foreach(var key in new[]{"F8","F9","Space","F13"}) Reject(() => Call("ActionKeyPolicy", "Validate", "energy", key, "F9", bindings));
            Call("ActionKeyPolicy", "Validate", "heal", "F2", "F9", bindings); Call("ActionKeyPolicy", "Validate", "energy", "None", "F9", bindings);
        });
        check("timer excludes paused time and reset never saves PB", () => {
            dynamic timer = New("PracticeTimer"); timer.Start("A"); timer.Advance(2d,true); timer.Advance(20d,false);
            Assert(timer.Elapsed == 2d); timer.Reset(); Assert(timer.Stop() == null && !timer.Running && timer.Elapsed == 0d);
        });
        check("timer stop freezes name and duration without repeated PB", () => {
            dynamic timer = New("PracticeTimer"); timer.Start("A"); timer.Advance(3d,true); dynamic run = timer.Stop();
            Assert(run.Name == "A" && run.Seconds == 3d && timer.Stop() == null); timer.Advance(5d,true); Assert(timer.Elapsed == 3d);
        });
        check("timer rejects invalid deltas and duplicate start", () => {
            dynamic timer = New("PracticeTimer"); timer.Start("A"); Reject(() => timer.Start("B"));
            foreach(var delta in new[]{-1d,double.NaN,double.PositiveInfinity}) timer.Advance(delta,true);
            Assert(timer.Elapsed == 0d && timer.Stop() == null);
        });
        check("PB replaces only faster positive duration for same practice", () => WithFile(path => {
            dynamic lib = New("UtilityLibrary", path); lib.RecordBest("A",10d); lib.RecordBest("A",12d); lib.RecordBest("B",4d); lib.RecordBest("A",8d);
            Reject(() => lib.RecordBest("A",0d)); dynamic data=New("UtilityLibrary",path).Data;
            Assert(data.BestTimes["A"] == 8d && data.BestTimes["B"] == 4d);
        }));
        check("recent log evicts oldest and returns detached snapshots", () => {
            dynamic log = New("RecentMenuLog",3); log.Add("one"); log.Add("two"); log.Add("three"); var old = (string[])log.Snapshot();
            log.Add("four"); var current = (string[])log.Snapshot(); Assert(old[0] == "one" && current.SequenceEqual(new[]{"two","three","four"}));
        });
        check("caller cannot mutate a saved preset without persistence", () => WithFile(path=>{
            dynamic lib=New("UtilityLibrary",path); dynamic preset=Call("WeaponPreset","Parse",Good); lib.AddPreset(preset);
            preset.Name="changed";preset.AffixIds.Clear(); Assert(lib.Data.Presets[0].Name=="练习 🔥"&&lib.Data.Presets[0].AffixIds.Count==1);
        }));
        check("empty favorite identities are rejected without creating a file", () => WithFile(path=>{
            dynamic lib=New("UtilityLibrary",path); foreach(var id in new[]{null,""," "})Reject(()=>lib.ToggleFavorite("item",id));Assert(!File.Exists(path));
        }));
        check("invalid best time cannot later crash the timer display", () => WithFile(path=>{
            dynamic lib=New("UtilityLibrary",path); Reject(()=>lib.RecordBest("A",double.MaxValue));Assert(!File.Exists(path));
        }));
        check("shortcuts never dispatch during editing pause loading recovery or lost focus",()=>{
            var baseline=new object[]{false,false,true,true,false,false,false,false};
            Assert((bool)Call("ActionKeyPolicy","CanDispatch",baseline));
            for(int i=0;i<8;i++){var blocked=(object[])baseline.Clone();blocked[i]=!(bool)blocked[i];Assert(!(bool)Call("ActionKeyPolicy","CanDispatch",blocked));}
        });
        check("key capture cancels after an automatic close or leaving shortcut page",()=>{
            Assert((bool)Call("ActionKeyPolicy","CanCapture",true,9,1));
            foreach(var state in new[]{(false,9,1),(true,8,1),(true,9,0),(true,9,2)})
                Assert(!(bool)Call("ActionKeyPolicy","CanCapture",state.Item1,state.Item2,state.Item3));
        });
        check("edit callback cannot retain a live reference to committed configuration",()=>WithFile(path=>{
            var lib=new UtilityLibrary(path); UtilityData held=null;
            lib.Edit(d=>{held=d;d.Favorites.Add("item:a");}); held.Favorites.Add("item:b");
            Assert(!lib.IsFavorite("item","b")&&!new UtilityLibrary(path).IsFavorite("item","b"));
        }));
    }
    private static void WithFile(Action<string> action) {
        var root=Path.Combine(AppContext.BaseDirectory,"rpc-utilities-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try{ action(Path.Combine(root,"library.json")); } finally {Directory.Delete(root,true);}
    }
}
