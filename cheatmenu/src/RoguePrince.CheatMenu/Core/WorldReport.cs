// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;
namespace RoguePrince.CheatMenu.Core;

internal static class WorldReportLimits
{internal const int Entities=1024,Components=256,Items=256,Affixes=128,JsonBytes=2*1024*1024;}

// These identities are diagnostic, process-local observations, never restore handles.
internal readonly record struct WorldReportStamp(long Generation,long Player,long Saver,long Context,long Flow,long Map,
 long Run,string State,int Seed,int Scene,int Frame,long EnabledSet,int EnabledCount,int EnabledVersion,
 long DisabledSet,int DisabledCount,int DisabledVersion,long DestroyedSet,int DestroyedCount,int DestroyedVersion,
 long Inventory,long ItemList,int ItemCount,int ItemVersion,long Main,long Tool)
{
 internal bool SameScope(WorldReportStamp other)=>Generation==other.Generation&&Player==other.Player&&
  Saver==other.Saver&&Context==other.Context&&Flow==other.Flow&&Map==other.Map&&Run==other.Run&&
  State==other.State&&Seed==other.Seed&&Scene==other.Scene;
}
internal readonly record struct WorldEntityObservation(int InstanceId,string Name,string Group,int Scene,bool Enabled,
 bool Active,bool Destroyed,float[] Position,float? Health,float? MaxHealth,string[] Components);
internal readonly record struct WorldItemObservation(long Instance,string ItemId,string Name,int Tier,string Slot,string[] Affixes);
internal static class WorldReportFormat
{
 internal static string Serialize(WorldReportStamp stamp,IReadOnlyList<WorldEntityObservation> entities,
  IReadOnlyList<WorldItemObservation> items,float time,bool contextPaused)
 {
  if(entities.Count>WorldReportLimits.Entities||items.Count>WorldReportLimits.Items||
   entities.Count!=stamp.EnabledCount+stamp.DisabledCount||items.Count!=stamp.ItemCount||
   entities.Select(e=>e.InstanceId).Distinct().Count()!=entities.Count)
   throw new InvalidOperationException(MenuText.Get("状态报告成员不一致；未发布"));
  foreach(var entity in entities)
   if(entity.Position.Length!=3||entity.Position.Any(v=>!float.IsFinite(v))||
    entity.Health.HasValue&&!float.IsFinite(entity.Health.Value)||entity.MaxHealth.HasValue&&!float.IsFinite(entity.MaxHealth.Value))
    throw new InvalidOperationException(MenuText.Get("状态报告含无效数值；未发布"));
  var json=JsonSerializer.Serialize(new{
   schemaVersion=1,kind="scene-state-report",canRestore=false,synchronizedWorld=false,
   sampledAtRealtime=time,stamp,contextPaused,
   coverage=new[]{"current-context enabled and disabled entity membership","cached component type names",
    "observed entity position and health when present","current player inventory, equipped roles, tiers and affix IDs"},
   uncovered=new[]{"component field values, flags and complete reference graph","destroyed entities and pooled inactive objects",
    "non-entity objects and other contexts/scenes","active actions, animation/physics clocks and pending tasks",
    "boss phase machines, projectiles and effect lifecycle state","room triggers, rewards, RNG streams and queued events",
    "save/progression isolation and a synchronized quiescent restore boundary"},entities,playerItems=items
  },new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase,WriteIndented=true});
  if(System.Text.Encoding.UTF8.GetByteCount(json)>WorldReportLimits.JsonBytes)
   throw new InvalidOperationException(MenuText.Get("状态报告超过容量；未发布"));
  return json;
 }
}
