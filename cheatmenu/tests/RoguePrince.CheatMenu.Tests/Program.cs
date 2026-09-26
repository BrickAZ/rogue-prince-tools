// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
// Additional game linking permission: see GAME-LINKING-EXCEPTION.txt.

using RoguePrince.CheatMenu.Core;
using System.Text.Json;

var failed = 0;
var passed = 0;
void Check(string name, Action test)
{
    try { test(); passed++; Console.WriteLine("PASS " + name); }
    catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
}
void Eq<T>(T actual, T expected) { if (!Equals(actual, expected)) throw new Exception($"expected {expected}, got {actual}"); }

// A wrong add/set branch or unchecked integer overflow must fail these cases.
foreach (var row in new[] {
    ("100",350,true,1000000,true,450), ("100",350,false,1000000,true,100),
    ("0",350,false,1000000,true,0), ("-1",350,false,1000000,false,0),
    ("2147483647",100,true,int.MaxValue,false,0), ("999999999999",0,false,1000000,false,0),
    ("",0,false,1000000,false,0), ("NaN",0,false,1000000,false,0),
    ("1.5",0,false,1000000,false,0), (" 30 ",20,true,1000,true,50),
    ("1001",0,false,1000,false,0) })
    Check("numeric " + row, () => { var ok=NumericPolicy.TryTarget(row.Item1,row.Item2,row.Item3,row.Item4,out var v,out _); Eq(ok,row.Item5); if(ok) Eq(v,row.Item6); });

Check("restore retains intervening game bonus",()=>{var a=new OwnedAdjustment();Eq(a.Apply(200,300),300f);Eq(a.Restore(320,1,_=>{}),220f);Eq(a.Active,false);});
Check("repeated target changes restore only own net delta",()=>{var a=new OwnedAdjustment();a.Apply(200,300);a.Apply(320,250);Eq(a.Restore(260,1,_=>{}),230f);});
Check("restore clamps positive maximum",()=>{var a=new OwnedAdjustment();a.Apply(100,500);Eq(a.Restore(300,1,_=>{}),1f);});
Check("clear prevents stale player restoration",()=>{var a=new OwnedAdjustment();a.Apply(200,300);a.Clear();Eq(a.Restore(180,1,_=>{}),180f);});
Check("failed restore retains contribution and retries original target",()=>{var a=new OwnedAdjustment();a.Apply(200,300);try{a.Restore(320,1,_=>throw new IOException("native write failed"));}catch(IOException){}Eq(a.Active,true);float written=0;a.Restore(320,1,v=>written=v);Eq(written,220f);Eq(a.Active,false);});
Check("failed apply preserves earlier owned adjustment",()=>{var a=new OwnedAdjustment();a.Apply(200,300);try{a.ApplyThrough(300,500,_=>throw new IOException("native write failed"));}catch(IOException){}Eq(a.Restore(320,1,_=>{}),220f);});

// Production slot planning: never strand an equipped medallion or exceed the game's cap.
void SlotPlan(int basis, int extra, int cap, int occupied, SlotChange change, int expectedBase, int expectedExtra)
{
    var plan = SlotCapacityPolicy.Plan(basis, extra, cap, occupied, change);
    Eq(plan.BaseSlots, expectedBase); Eq(plan.AdditionalSlots, expectedExtra);
}
Check("slots add exactly one",()=>SlotPlan(4,0,9,4,SlotChange.Add,4,1));
Check("slots subtract extra first",()=>SlotPlan(4,2,9,5,SlotChange.Remove,4,1));
Check("slots can reduce base when extras are empty",()=>SlotPlan(4,0,9,2,SlotChange.Remove,3,0));
Check("slots allow removing the last unoccupied slot",()=>SlotPlan(1,0,9,0,SlotChange.Remove,0,0));
Check("slots zero is a no-op",()=>SlotPlan(0,0,9,0,SlotChange.Remove,0,0));
Check("slots use runtime cap instead of hardcoded nine",()=>SlotPlan(4,1,12,3,SlotChange.UnlockAll,4,8));
Check("slots adding at cap is a no-op",()=>SlotPlan(4,5,9,9,SlotChange.Add,4,5));
Check("slots unlocking at cap is a no-op",()=>SlotPlan(4,5,9,9,SlotChange.UnlockAll,4,5));
Check("slots refuse stranding an equipped medallion",()=>{
    try { SlotCapacityPolicy.Plan(4,1,9,5,SlotChange.Remove); throw new Exception("removed occupied slot"); }
    catch (InvalidOperationException e) { Eq(e.Message.Contains("卸下"),true); }
});
Check("slots reject corrupt state and overflowing totals",()=>{
    foreach(var state in new[]{(-1,0,9,0),(4,-1,9,0),(4,0,0,0),(4,6,9,0),(int.MaxValue,1,int.MaxValue,0),(4,0,9,5),(4,0,9,-1)})
    {
        try { SlotCapacityPolicy.Plan(state.Item1,state.Item2,state.Item3,state.Item4,SlotChange.Add); throw new Exception("accepted invalid slot state"); }
        catch(ArgumentOutOfRangeException) { }
    }
});
Check("slots addition at int maximum does not overflow",()=>SlotPlan(int.MaxValue,0,int.MaxValue,0,SlotChange.Add,int.MaxValue,0));

// Captured native ItemData feature lists, including packs, effect-wrapped resources,
// and progression items whose names alone would suggest the wrong category.
using (var fixtures = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "item-category-cases.json"))))
    foreach (var row in fixtures.RootElement.EnumerateArray())
        Check("item category " + row.GetProperty("name").GetString(), () => {
            var features = row.GetProperty("features").EnumerateArray().Select(f => f.GetString()).ToArray();
            Eq(ItemCategoryPolicy.Classify(row.GetProperty("name").GetString(), features), row.GetProperty("expected").GetString());
        });
Check("unknown resources are not classified by name alone", () =>
    Eq(ItemCategoryPolicy.Classify("Container-Shards-Unverified", new[] { "EffectItemFeature" }), "其他"));

// These exercise real file copies: skipping a file, overwriting the source, or accepting an empty backup must fail.
Check("language config defaults safely and round-trips both languages", () => {
    foreach (var code in new[] { "zh-CN", "en-US" }) {
        MenuText.SetLanguage(code); Eq(MenuText.LanguageCode, code);
    }
    foreach (var bad in new[] { "", "ja-JP", "invalid", null }) {
        MenuText.SetLanguage(bad); Eq(MenuText.LanguageCode, "zh-CN");
    }
});
Check("localized item names switch without replacing the item or losing search", () => {
    var label = new LocalizedItemName("双匕", "Double Daggers", "DoubleDaggerWeapon");
    try {
        MenuText.SetLanguage("zh-CN"); Eq(label.Display, "双匕");
        MenuText.SetLanguage("en-US"); Eq(label.Display, "Double Daggers");
        foreach (var query in new[] { "双匕", "daggers", "DoubleDaggerWeapon", "  DAGGERS  " }) Eq(label.Matches(query), true);
        Eq(label.Matches("missing"), false);
        MenuText.SetLanguage("zh-CN"); Eq(label.Display, "双匕");
    } finally { MenuText.SetLanguage("zh-CN"); }
});
Check("missing item translation retains the exact asset identity", () => {
    var label = new LocalizedItemName("", null, "UntranslatedAsset");
    try {
        MenuText.SetLanguage("en-US"); Eq(label.Display, "UntranslatedAsset");
        MenuText.SetLanguage("zh-CN"); Eq(label.Display, "UntranslatedAsset");
        Eq(label.Matches("asset"), true);
    } finally { MenuText.SetLanguage("zh-CN"); }
});
Check("language switch preserves category identity and slot validation", () => {
    try {
        MenuText.SetLanguage("en-US");
        var id = ItemCategoryPolicy.Classify("example", new[] { "MedallionItemFeature" });
        Eq(id, "徽章"); Eq(MenuText.Get(id), "Medallions");
        try { SlotCapacityPolicy.Plan(4,1,9,5,SlotChange.Remove); throw new Exception("accepted occupied slot"); }
        catch (InvalidOperationException e) { Eq(e.Message.Contains("Unequip"), true); }
        NumericPolicy.TryTarget("-1", 0, false, 100, out _, out var error);
        Eq(error.Contains("non-negative"), true);
    } finally { MenuText.SetLanguage("zh-CN"); }
});
Check("both translation formats retain all values and escaped braces", () => {
    foreach (var pair in MenuText.Translations) {
        var zh = System.Text.RegularExpressions.Regex.Matches(pair.Key, @"\{(\d+)(?:[^}]*)\}").Select(m => m.Groups[1].Value).OrderBy(x => x).ToArray();
        var en = System.Text.RegularExpressions.Regex.Matches(pair.Value, @"\{(\d+)(?:[^}]*)\}").Select(m => m.Groups[1].Value).OrderBy(x => x).ToArray();
        Eq(string.Join(",", zh), string.Join(",", en));
        var args = new object[zh.Length == 0 ? 0 : zh.Select(int.Parse).Max() + 1];
        Array.Fill(args, 3);
        string.Format(pair.Key, args); string.Format(pair.Value, args);
    }
});

var temp=Path.Combine(Path.GetTempPath(), "rp-cheat-test-"+Guid.NewGuid().ToString("N"));
try
{
    var source=Path.Combine(temp,"source");var backups=Path.Combine(temp,"backups");
    Directory.CreateDirectory(Path.Combine(source,"account"));
    File.WriteAllText(Path.Combine(source,"account","global.sav"),"original-save-1");
    File.WriteAllText(Path.Combine(source,"account","run.sav"),"run-2");
    Check("backup copies all saves and retains original",()=>{var path=SaveBackup.Create(source,backups);Eq(File.ReadAllText(Path.Combine(path,"account","global.sav")),"original-save-1");Eq(File.ReadAllText(Path.Combine(path,"account","run.sav")),"run-2");Eq(File.ReadAllText(Path.Combine(source,"account","global.sav")),"original-save-1");Eq(File.Exists(Path.Combine(path,"manifest.json")),true);});
    Check("backup uses unique directories",()=>{var a=SaveBackup.Create(source,backups);var b=SaveBackup.Create(source,backups);Eq(a==b,false);});
    Check("backup rejects empty source",()=>{var empty=Path.Combine(temp,"empty");Directory.CreateDirectory(empty);try{SaveBackup.Create(empty,backups);throw new Exception("accepted empty source");}catch(InvalidDataException){}});
    Check("backup refuses destination inside source",()=>{try{SaveBackup.Create(source,Path.Combine(source,"backups"));throw new Exception("accepted nested backup root");}catch(ArgumentException){}});
    Check("backup refuses a save locked by another writer",()=>{using var locked=new FileStream(Path.Combine(source,"account","global.sav"),FileMode.Open,FileAccess.ReadWrite,FileShare.None);try{SaveBackup.Create(source,backups);throw new Exception("accepted locked save");}catch(IOException){}});
}
finally { if(Directory.Exists(temp)) Directory.Delete(temp,true); }
Console.WriteLine($"RESULT {passed} passed, {failed} failed");
return failed == 0 ? 0 : 1;
