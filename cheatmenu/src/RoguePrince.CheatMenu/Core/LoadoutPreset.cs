// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;
namespace RoguePrince.CheatMenu.Core;
public sealed class MedallionPreset
{public string ItemId {get;set;} public int Tier {get;set;}}
public sealed class LoadoutPreset
{
    public int Version {get;set;}=1;
    public string Name {get;set;}
    public WeaponPreset Main {get;set;}
    public WeaponPreset Tool {get;set;}
    public List<MedallionPreset> Medallions {get;set;}=new();
    public void Validate()
    {
        Name=WeaponPreset.ValidName(Name);
        if(Version!=1||Main==null||Tool==null||Medallions==null||Medallions.Count>32||Medallions.Any(m=>m==null||!WeaponPreset.ValidId(m.ItemId)||m.Tier<1||m.Tier>100))throw new ArgumentException(MenuText.Get("配装版本或内容无效"));
        Main.Validate();Tool.Validate();
    }
    public string Export(){Validate();return JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true});}
    public static LoadoutPreset Parse(string value){if(value==null||value.Length>65536)throw new ArgumentException(MenuText.Get("配装文本为空或过大"));var p=JsonSerializer.Deserialize<LoadoutPreset>(value)??throw new ArgumentException(MenuText.Get("配装文本无效"));p.Validate();return p;}
}
public sealed class LoadoutLibrary
{
    private readonly string path;
    public List<LoadoutPreset> Presets {get;private set;}=new();
    public bool ReadOnly {get;private set;}
    public string Error {get;private set;}="";
    public LoadoutLibrary(string path)
    {this.path=path;try{if(File.Exists(path)){var json=File.ReadAllText(path);if(json.Length>2*1024*1024)throw new ArgumentException("Loadout file too large");Presets=JsonSerializer.Deserialize<List<LoadoutPreset>>(json)??throw new ArgumentException("Null preset list");Validate(Presets);}}catch(Exception e){ReadOnly=true;Error=e.Message;Presets=new();}}
    private static void Validate(List<LoadoutPreset> data){if(data.Count>64)throw new ArgumentException("Maximum 64 loadouts");foreach(var p in data)p.Validate();if(data.Select(p=>p.Name).Distinct(StringComparer.Ordinal).Count()!=data.Count)throw new ArgumentException("Duplicate loadout name");}
    public void Save(LoadoutPreset preset)
    {preset.Validate();Edit(list=>{list.RemoveAll(p=>p.Name==preset.Name);list.Add(preset);});}
    public void Delete(string name)=>Edit(list=>list.RemoveAll(p=>p.Name==name));
    private void Edit(Action<List<LoadoutPreset>> edit)
    {
        if(ReadOnly)throw new InvalidOperationException(Error);
        var next=Presets.Select(p=>LoadoutPreset.Parse(p.Export())).ToList();edit(next);Validate(next);
        var tmp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(tmp,JsonSerializer.Serialize(next,new JsonSerializerOptions{WriteIndented=true}));if(File.Exists(path))File.Replace(tmp,path,path+".bak");else File.Move(tmp,path);Presets=next;}finally{if(File.Exists(tmp))File.Delete(tmp);}
    }
}
