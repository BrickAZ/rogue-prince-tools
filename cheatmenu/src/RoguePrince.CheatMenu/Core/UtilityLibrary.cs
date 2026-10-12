// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;

namespace RoguePrince.CheatMenu.Core;

public sealed class UtilityData
{
    public int Version { get; set; } = 1;
    public List<string> Favorites { get; set; } = new();
    public List<WeaponPreset> Presets { get; set; } = new();
    public Dictionary<string,string> Bindings { get; set; } = new();
    public Dictionary<string,double> BestTimes { get; set; } = new();
}
public sealed class UtilityLibrary
{
    private readonly string path;
    private UtilityData data = new();
    public UtilityData Data => Clone(data);
    public bool ReadOnly { get; private set; }
    public string Error { get; private set; } = "";
    private static UtilityData Clone(UtilityData value) => JsonSerializer.Deserialize<UtilityData>(JsonSerializer.Serialize(value));
    public UtilityLibrary(string path)
    {
        this.path = path;
        try {
            if (!File.Exists(path)) return;
            if (new FileInfo(path).Length > 262144) throw new ArgumentException(MenuText.Get("配置文件过大"));
            var json=File.ReadAllText(path);
            using var doc=JsonDocument.Parse(json);
            if(doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("Version",out var version) || version.GetInt32() != 1)
                throw new ArgumentException(MenuText.Get("配置版本不支持"));
            var loaded=JsonSerializer.Deserialize<UtilityData>(json); Validate(loaded); data=loaded;
        } catch(Exception ex) { ReadOnly=true; Error=ex.Message; }
    }
    private static void Validate(UtilityData value)
    {
        if(value == null || value.Version != 1 || value.Favorites == null || value.Presets == null || value.Bindings == null || value.BestTimes == null || value.Favorites.Count > 2048 || value.Presets.Count > 128 || value.BestTimes.Count > 128)
            throw new ArgumentException(MenuText.Get("配置内容无效或超出容量"));
        foreach(var id in value.Favorites) if(id==null || !new[]{"item:","affix:","mob:"}.Any(prefix=>id.StartsWith(prefix,StringComparison.Ordinal)) || !WeaponPreset.ValidId(id.Substring(id.IndexOf(':')+1))) throw new ArgumentException(MenuText.Get("收藏标识无效"));
        if(value.Favorites.Distinct(StringComparer.Ordinal).Count()!=value.Favorites.Count) throw new ArgumentException(MenuText.Get("收藏重复"));
        foreach(var preset in value.Presets) { if(preset == null) throw new ArgumentException(MenuText.Get("预设文本无效")); preset.Validate(); }
        if(value.Presets.Select(p=>p.Name).Distinct(StringComparer.Ordinal).Count()!=value.Presets.Count) throw new ArgumentException(MenuText.Get("预设名称已存在"));
        foreach(var binding in value.Bindings) ActionKeyPolicy.Validate(binding.Key,binding.Value,"",value.Bindings);
        foreach(var best in value.BestTimes) { if(WeaponPreset.ValidName(best.Key)!=best.Key || !PracticeTimer.IsDuration(best.Value)) throw new ArgumentException(MenuText.Get("最佳计时无效")); }
    }
    public void Edit(Action<UtilityData> change)
    {
        if(ReadOnly) throw new InvalidOperationException(MenuText.Get("配置读取失败；保留原文件，当前禁止保存")+": "+Error);
        var next=Clone(data); change(next); Validate(next);
        var json=JsonSerializer.Serialize(next,new JsonSerializerOptions { WriteIndented=true });
        if(System.Text.Encoding.UTF8.GetByteCount(json)>262144) throw new ArgumentException(MenuText.Get("配置文件过大"));
        var committed=JsonSerializer.Deserialize<UtilityData>(json);
        var temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(temp,json);
            if(File.Exists(path)) File.Replace(temp,path,path+".bak"); else File.Move(temp,path);
            data=committed;
        } finally { if(File.Exists(temp)) File.Delete(temp); }
    }
    public bool IsFavorite(string kind,string id) => data.Favorites.Contains(kind+":"+id);
    public void ToggleFavorite(string kind,string id) => Edit(next => { var key=kind+":"+id; if(!next.Favorites.Remove(key)) next.Favorites.Add(key); });
    public void AddPreset(WeaponPreset preset) => Edit(next => {
        if(preset == null) throw new ArgumentException(MenuText.Get("预设文本无效"));
        preset.Validate(); if(next.Presets.Any(p=>p.Name==preset.Name)) throw new ArgumentException(MenuText.Get("预设名称已存在")); next.Presets.Add(WeaponPreset.Parse(preset.Export()));
    });
    public void RemovePreset(string name) => Edit(next => next.Presets.RemoveAll(p=>p.Name==name));
    public void SetBinding(string action,string key,string menuKey) => Edit(next => { ActionKeyPolicy.Validate(action,key,menuKey,next.Bindings); next.Bindings[action]=key; });
    public void RecordBest(string name,double seconds) => Edit(next => {
        name=WeaponPreset.ValidName(name); if(!PracticeTimer.IsDuration(seconds)) throw new ArgumentException(MenuText.Get("最佳计时无效"));
        if(!next.BestTimes.TryGetValue(name,out var previous)||seconds<previous) next.BestTimes[name]=seconds;
    });
}
