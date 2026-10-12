// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MotherBase.EntityFramework;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
using EntitySet=Il2CppSystem.Collections.Generic.HashSet<MotherBase.EntityFramework.Entity>;
namespace RoguePrince.CheatMenu;

internal sealed class WorldReportActions
{
 private readonly GameSession game;
 private WorldReportStamp? captured;
 internal string Json{get;private set;}="";
 internal string Status{get;private set;}="";
 internal string Summary{get;private set;}="";
 internal bool HasReport=>captured!=null;
 internal WorldReportActions(GameSession game)=>this.game=game;
 private WorldReportStamp ReadStamp()
 {
  var flow=SceneActions.Flow();var player=game.Player;var context=player?.context;
  var map=flow==null?null:SceneActions.Map(flow);
  if(!Application.isFocused||Time.timeScale!=0||!game.Ready||player==null||context==null||map==null||
   game.Saver?.mainCharacterVariable?.CurrentValue?.Pointer!=game.PlayerPointer||
   flow==null||flow.isLoading||!flow.IsRunActive||!flow.IsCurrentStateLoaded())
   throw new InvalidOperationException(MenuText.Get("状态报告需要菜单暂停、焦点和当前已加载战斗场景"));
  var enabled=context._enabledEntities;var disabled=context._disabledEntities;var destroyed=context._destroyedEntities;
  var inventory=player.GetComponent<InventoryComponent>();var items=inventory?.items;
  if(enabled==null||disabled==null||destroyed==null||items==null||flow.GetCurrentState()==null)
   throw new InvalidOperationException(MenuText.Get("状态报告原生集合尚未就绪"));
  inventory.TryGetMainWeapon(out var main);inventory.TryGetSecondaryWeapon(out var tool);
  return new(game.Generation,player.Pointer.ToInt64(),game.Saver.Pointer.ToInt64(),context.Pointer.ToInt64(),
   flow.Pointer.ToInt64(),map.Pointer.ToInt64(),RunTools.RunId,flow.GetCurrentState().guid,flow.GetSeed(),
   player.gameObject.scene.handle,Time.frameCount,enabled.Pointer.ToInt64(),enabled.Count,enabled._version,
   disabled.Pointer.ToInt64(),disabled.Count,disabled._version,destroyed.Pointer.ToInt64(),destroyed.Count,destroyed._version,
   inventory.Pointer.ToInt64(),items.Pointer.ToInt64(),items.Count,items._version,main?.Pointer.ToInt64()??0,tool?.Pointer.ToInt64()??0);
 }
 internal string Capture()
 {
  // Retire old managed data first. A failed read must not leave a stale export enabled.
  Clear();
  try
  {
   var before=ReadStamp();
   if(before.EnabledCount<0||before.DisabledCount<0||before.ItemCount<0||
    (long)before.EnabledCount+before.DisabledCount>WorldReportLimits.Entities||before.ItemCount>WorldReportLimits.Items)
    throw new InvalidOperationException(MenuText.Get("状态报告超过容量；未发布"));
   var entities=new List<WorldEntityObservation>();
   ReadGroup(game.Player.context._enabledEntities,"enabled",before,entities);
   ReadGroup(game.Player.context._disabledEntities,"disabled",before,entities);
   var items=ReadItems(before);
   var contextPaused=game.Player.context.IsPaused();var time=Time.realtimeSinceStartup;
   if(before!=ReadStamp())throw new InvalidOperationException(MenuText.Get("采样期间场景或集合变化；报告已丢弃"));
   var json=WorldReportFormat.Serialize(before,entities,items,time,contextPaused);
   // No native wrappers escape this call; stored report consists only of managed values.
   Json=json;captured=before;
   Summary=MenuText.Format("实体 {0}（启用 {1} / 停用 {2}）· 物品 {3} · 帧 {4}",entities.Count,before.EnabledCount,before.DisabledCount,items.Count,before.Frame);
   Status=MenuText.Get("场景状态已采样；完整恢复尚未实现");
   Plugin.LogEvent($"world_report_capture entities={entities.Count} items={items.Count} frame={before.Frame} can_restore=false");
   return Status;
  }
  catch{Clear();throw;}
 }
 private static void ReadGroup(EntitySet source,string group,WorldReportStamp stamp,List<WorldEntityObservation> result)
 {
  var count=source.Count;var version=source._version;
  if(count<0||count>WorldReportLimits.Entities||result.Count+count>WorldReportLimits.Entities)
   throw new InvalidOperationException(MenuText.Get("状态报告超过容量；未发布"));
  // Reference-type CopyTo avoids the generated boxed native struct enumerator.
  var copy=new Il2CppReferenceArray<Entity>(count);source.CopyTo(copy,0,count);
  if(source.Count!=count||source._version!=version)throw new InvalidOperationException(MenuText.Get("采样期间场景或集合变化；报告已丢弃"));
  for(int i=0;i<count;i++)
  {
   var entity=copy[i];
   if(entity==null||entity.destroyed||entity.context?.Pointer.ToInt64()!=stamp.Context||entity.gameObject.scene.handle!=stamp.Scene)
    throw new InvalidOperationException(MenuText.Get("状态报告成员不一致；未发布"));
   var cache=entity.componentCache;
   if(cache==null)throw new InvalidOperationException(MenuText.Get("状态报告原生集合尚未就绪"));
   var componentCount=cache.Count;var componentVersion=cache._version;
   if(componentCount<0||componentCount>WorldReportLimits.Components)throw new InvalidOperationException(MenuText.Get("状态报告超过容量；未发布"));
   var components=new Il2CppReferenceArray<Component>(componentCount);cache.Values.CopyTo(components,0);
   if(entity.componentCache?.Pointer!=cache.Pointer||cache.Count!=componentCount||cache._version!=componentVersion)
    throw new InvalidOperationException(MenuText.Get("采样期间场景或集合变化；报告已丢弃"));
   var names=new string[componentCount];
   for(int c=0;c<componentCount;c++)names[c]=components[c]?.GetIl2CppType().FullName??throw new InvalidOperationException(MenuText.Get("状态报告成员不一致；未发布"));
   Array.Sort(names,StringComparer.Ordinal);
   var point=entity.transform.position;var health=entity.GetComponent<Damageable>();
   result.Add(new(entity.GetInstanceID(),entity.gameObject.name,group,entity.gameObject.scene.handle,entity.enabled,
    entity.gameObject.activeInHierarchy,entity.destroyed,new[]{point.x,point.y,point.z},health?.currentHealth,health?.maxHealth,names));
  }
 }
 private List<WorldItemObservation> ReadItems(WorldReportStamp stamp)
 {
  var inventory=game.Player.GetComponent<InventoryComponent>();var source=inventory.items;
  var count=source.Count;var version=source._version;
  if(count<0||count>WorldReportLimits.Items)throw new InvalidOperationException(MenuText.Get("状态报告超过容量；未发布"));
  var copy=new Il2CppReferenceArray<ItemInstanceData>(count);source.CopyTo(copy,0);
  if(inventory.items?.Pointer!=source.Pointer||source.Count!=count||source._version!=version)
   throw new InvalidOperationException(MenuText.Get("采样期间场景或集合变化；报告已丢弃"));
  var result=new List<WorldItemObservation>();
  for(int i=0;i<count;i++)
  {
   var item=copy[i];
   if(item?.source==null)throw new InvalidOperationException(MenuText.Get("状态报告成员不一致；未发布"));
   var affixes=item.GetComponent<AffixHolderItemFeature.AffixHolderComponentData>()?.affixesGuids;
   if(affixes?.Length>WorldReportLimits.Affixes)throw new InvalidOperationException(MenuText.Get("状态报告超过容量；未发布"));
   var ids=new string[affixes?.Length??0];for(int j=0;j<ids.Length;j++)ids[j]=affixes[j].guid;
   var id=item.Pointer.ToInt64();
   result.Add(new(id,item.source.Guid.guid,item.source.name,item.tier,id==stamp.Main?"main":id==stamp.Tool?"tool":"inventory",ids));
  }
  return result;
 }
 internal string Copy()
 {
  try
  {
   if(captured==null||!captured.Value.SameScope(ReadStamp()))throw new InvalidOperationException(MenuText.Get("报告已失效，请重新采样"));
   GUIUtility.systemCopyBuffer=Json;
   return Status=MenuText.Get("场景状态 JSON 已复制");
  }
  catch{Clear();throw;}
 }
 internal void Observe(bool menuVisible)
 {
  if(captured==null)return;
  try{if(!menuVisible||!captured.Value.SameScope(ReadStamp()))Clear();}catch{Clear();}
 }
 internal string Clear(){captured=null;Json=Summary="";return Status=MenuText.Get("场景状态报告已清除");}
}
