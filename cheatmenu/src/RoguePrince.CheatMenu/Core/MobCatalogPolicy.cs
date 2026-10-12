// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;

// Resource identities were read from the installed catalog and native prefabs.
// Chinese labels are menu descriptions; the native identity remains visible/searchable.
public sealed class MobCatalogEntry
{
    public string Id {get;} public string RuleName {get;} public string RuleGuid {get;} public string PrefabGuid {get;}
    public bool Boss {get;} public bool PrefabOnly {get;} public bool Supported {get;} public string Chinese {get;}
    public MobCatalogEntry(string id,string ruleName,string ruleGuid,string prefabGuid,bool boss,bool prefabOnly,bool supported,string chinese)
    {Id=id;RuleName=ruleName;RuleGuid=ruleGuid;PrefabGuid=prefabGuid;Boss=boss;PrefabOnly=prefabOnly;Supported=supported;Chinese=chinese;}
    public string Name=>MenuText.IsEnglish?Id:Chinese;
    public bool Matches(string query)=>Id.Contains(query,StringComparison.OrdinalIgnoreCase)||
        Chinese.Contains(query,StringComparison.OrdinalIgnoreCase);
}
public static class MobCatalogPolicy
{
    public static IReadOnlyList<MobCatalogEntry> Entries { get; }=Array.AsReadOnly(new MobCatalogEntry[]
    {
        new("AirMob","AirMobSpawnRule","7a8f1c969541240488c0fa2995e8479c","",false,false,false,"空中怪物（测试）"),
        new("Alarm","AlarmSpawnRule","f9a962d095dc10742a6c8792f8ca1240","a49d4c08919ab204ea579501193b7f61",false,false,true,"警报兵"),
        new("AltanBoss","RPCheat-AltanBossSpawnRule","","e2306c0ebbc1a8c4a8b2f8370ce215ef",true,true,true,"Altan"),
        new("Archer","ArcherSpawnRule","ac0910a514c470244925c6251185e34c","d9fb384b2d487b44aa0165867da554bd",false,false,true,"弓箭手"),
        new("Archer-Palace","Archer-PalaceSpawnRule","65736d604048aa74489e7d237a4a7961","6ff0880f043b2f04fb37d374231e71ff",false,false,true,"宫殿弓箭手"),
        new("Assassin","AssassinSpawnRule","3396ec0a3e2a77c4f939d83498ef1db3","4dfc5e7f61d61aa49b13ebf2bb81885f",false,false,true,"刺客"),
        new("BaatarBoss","RPCheat-BaatarBossSpawnRule","","ab22f3be802cb8c4680c78b432186999",true,true,true,"Baatar"),
        new("Bomber","BomberSpawnRule","f2605b7dfa69743448761bfbe230481d","283eab1a8192a6144a58fd48ddc52297",false,false,true,"爆破兵"),
        new("Bomber-Palace","Bomber-PalaceSpawnRule","97692e3c2aaabfa4f9f6fe1906a6b196","21866a2ec8b5c894f969f30dc0f03ce1",false,false,true,"宫殿爆破兵"),
        new("CorruptedNogai","CorruptedNogaiSpawnRule","b44eb8a29ff71e2428c731f4840b46f5","0a99a46e7c34b5c46bdd035cc09e4955",true,false,true,"腐化 Nogai"),
        new("CrossBow","CrossBowSpawnRule","0305478b67b5a8444a5bcabde038cb0b","810e5c7ad92a06841a4ca915041df8e6",false,false,true,"弩兵"),
        new("Digger","DiggerSpawnRule","a56f821a1f75eae489672dcb52bde34e","a9f6c82662129ba4a998dde607f5c440",false,false,true,"挖掘者"),
        new("GoatBoss","RPCheat-GoatBossSpawnRule","","711d7aeacfc19b84c8d986e577d445e7",true,true,true,"Goat"),
        new("GroundMob","GroundMobSpawnRule","4e0292d6fda6b214a89832b40cbfe0dc","",false,false,false,"地面怪物（测试）"),
        new("Hornet","HornetSpawnRule","05b87e417b7d25d4e9496309de437ee4","7940ff8c08271a6429af8a2730fb0a3f",false,false,false,"黄蜂"),
        new("Inquisitor","InquisitorSpawnRule","ec4d59ba5c378974580710ee96451d6f","2cf3b05e543999048b208ff6f183c5fa",false,false,true,"审判者"),
        new("Invocator","InvocatorSpawnRule","61c816cf401ae5d4fbf0d4be1c7b1f9a","5443fc6f71bd8b1479adec3a3d8dbfd1",false,false,true,"召唤师"),
        new("Kamikaze","KamikazeSpawnRule","a54c6cd682d53c14e80dca5bc5862ffb","6f7896b5f5b498d4ca1aae7564dd7ba0",false,false,true,"自爆怪"),
        new("KnifeThrower","KnifeThrowerSpawnRule","fb868f19ad10f2441b03c26ad6a8380b","0902685f1ff336a4fa32899f91a2544e",false,false,true,"飞刀兵"),
        new("Lancer","LancerSpawnRule","3d528c3f7bb45c84e9db8cf6b47347af","4f1e4fa024d73154a861bb482e192b77",false,false,true,"长枪兵"),
        new("Mole","RPCheat-MoleSpawnRule","","ede8de5c1b7c313459464a92a6cbe2cf",true,true,true,"Kaali"),
        new("NarrowStomper","NarrowStomperSpawnRule","4b22fb15a9f39d94b85f2b708642a874","bbcff6fc05dcb314194410f6867df067",false,false,true,"窄型踩踏者"),
        new("Nogai","RPCheat-NogaiSpawnRule","","5e230890fcf588849bb52a748fa0ef21",true,true,true,"Nogai"),
        new("Pangolin","PangolinSpawnRule","6e737cf7cfa95ac4189ec038a2daff18","2db20081fa20dda479331c656bd74955",false,false,true,"穿山甲"),
        new("ScarabSwarm","ScarabSwarmSpawnRule","f2571278a4f02d44bb583982f75cba6b","75da2d1b919e9254d9724a2e6d90d9ee",false,false,false,"圣甲虫群"),
        new("ShieldedWarrior","ShieldedWarriorSpawnRule","7d5e3371553211b46b9ef62c85fda884","c686a83cec650df43981af79654b512a",false,false,true,"持盾战士"),
        new("SkyStalker","SkyStalkerSpawnRule","248aa8461f874c943bce76010537ce68","de7aa074b086695478f0064d6bf18398",false,false,true,"天空猎手"),
        new("Smoker","SmokerSpawnRule","cee12b964549d3c4c8d985aedd6b99df","2b5b7808cd7298a438677b1514d43745",false,false,true,"烟雾怪"),
        new("Spinner","SpinnerSpawnRule","6c196c9abcc63db4c83462bab03bad52","af0abf96c0404a947959becf2218891e",false,false,true,"旋转怪"),
        new("Stomper","StomperSpawnRule","1694c8a4dcf91ff42a40340be1606666","5614612a6f252024f95f662542d738bc",false,false,true,"踩踏者"),
        new("SwordMan","SwordManSpawnRule","531f66ddf35f06e48936aa5f5ea65035","68922940c0864c04699ede21ff28cb93",false,false,true,"普通剑兵"),
        new("SwordMan-Palace","SwordMan-PalaceSpawnRule","3f0be965ac313084e9052cc40c0ea5f2","66e7eb27abb7c1944967cde1b8224e19",false,false,true,"宫殿剑兵"),
        new("SwordWarrior","SwordWarriorSpawnRule","60355f1b6cd0c32458ecd4e59d0ec3ef","033c8300a8906cb4fbf532ed35fa392e",false,false,true,"剑战士"),
        new("Trapper","TrapperSpawnRule","ece802d54ee16b946980856ed7ea2773","3b61daefbd122d043bffb52d5fb17c43",false,false,true,"陷阱兵"),
        new("TrashMan","TrashManSpawnRule","435f79e18821eb549a8793a48417630b","dce9c92b870329148ad1657a6c84f47e",false,false,true,"腐化兵"),
        new("TrashMan-Palace","TrashMan-PalaceSpawnRule","1a1bf49fee3e46645b00bcfacf677059","04171bd885ba7f146ba080f62878be86",false,false,true,"宫殿腐化兵"),
        new("Trashword","TrashwordSpawnRule","5503b28c04c09914285db927e7a68e7f","1badb0711c3057f49b2a2c6a38c929d6",false,false,true,"腐化剑兵"),
        new("Warrior","WarriorSpawnRule","07e13fdc716a0f8488d38c70521cd4c9","562e433ae0d281b428cb4f6c3f5e72b8",false,false,true,"战士"),
        new("Warrior-Palace","Warrior-PalaceSpawnRule","dfb253aa0b95ecc49818420229f79a0a","c0664ef52e456234281344f6990fe084",false,false,true,"宫殿战士"),
    });
    public static MobCatalogEntry Find(string id)=>Entries.FirstOrDefault(e=>e.Id==id);
    public static List<MobCatalogEntry> Filter(bool boss,string query)
    {
        query=query?.Trim()??"";
        return Entries.Where(e=>e.Boss==boss&&e.Matches(query)).ToList();
    }
}
public static class MobAiPolicy
{
    public static bool Supports(bool components,bool regularBrain,bool bossBrain)=>components&&(regularBrain||bossBrain);
}
