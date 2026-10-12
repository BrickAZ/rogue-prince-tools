// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using MotherBase.EntityFramework;
using HarmonyLib;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
namespace RoguePrince.CheatMenu;
internal sealed class ToolLimitActions
{
 internal static bool Available;
 private readonly CombatActions combat;
 private TrainingScope scope;
 private IntPtr itemPointer,usesPointer,sourcePointer,inventoryPointer;
 internal bool UsesLocked {get;private set;}
 internal ToolLimitActions(CombatActions combat){this.combat=combat;}
 private (InventoryComponent Inventory,ItemInstanceData Item,TrainingScope Scope) ReadCurrent()
 {
  var game=combat.Game;var current=TrainingActions.ReadScope(game);
  if(!Application.isFocused||!game.Ready||game.Player==null||game.Player.destroyed||!game.Player.gameObject.activeInHierarchy||
     !current.Valid||current.Player!=game.PlayerPointer.ToInt64()||current.Scene!=game.Player.gameObject.scene.handle)
   throw new InvalidOperationException(MenuText.Get("训练操作需要当前角色、房间与焦点就绪"));
  var inventory=game.Player.GetComponent<InventoryComponent>();
  if(inventory==null||!inventory.TryGetSecondaryWeapon(out var item)||item?.source?.GetComponent<UtilityWeaponFeature>()==null||!inventory.HasItem(item))
   throw new InvalidOperationException(MenuText.Get("当前没有已装备的原生工具"));
  if(!current.Matches(TrainingActions.ReadScope(game)))throw new InvalidOperationException(MenuText.Get("工具作用域已变化，未执行"));
  return (inventory,item,current);
 }
 internal void SetUsesLock(bool enabled)
 {
  if(!enabled){Clear();return;}
  if(!Available)throw new InvalidOperationException(MenuText.Get("原生补丁不可用"));
  combat.Require();var state=ReadCurrent();var uses=state.Item.GetComponent<RemainingUses>();
  if(uses==null)throw new InvalidOperationException(MenuText.Get("当前工具没有原生次数限制"));
  if(uses.remainingUses<0)throw new InvalidOperationException(MenuText.Get("当前工具次数已耗尽；未补充或锁定"));
  scope=state.Scope;itemPointer=state.Item.Pointer;usesPointer=uses.Pointer;sourcePointer=state.Item.source.Pointer;inventoryPointer=state.Inventory.Pointer;
  UsesLocked=true;
 }
 private bool CurrentLock()
 {
  if(!UsesLocked)return false;
  if(!combat.ModesBound||!Application.isFocused||!combat.Game.Ready){Clear();return false;}
  var state=ReadCurrent();var uses=state.Item.GetComponent<RemainingUses>();
  if(!scope.Matches(state.Scope)||state.Item.Pointer!=itemPointer||state.Inventory.Pointer!=inventoryPointer||
     state.Item.source.Pointer!=sourcePointer||uses?.Pointer!=usesPointer||uses.remainingUses<0){Clear();return false;}
  return true;
 }
 internal bool SuppressUse(RemainingUses component,Entity owner,InventoryComponent inventory,ItemInstanceData item)
 {
  if(!UsesLocked)return false;
  if(!combat.IsPlayer(owner)||inventory?.Pointer!=inventoryPointer||item?.Pointer!=itemPointer)return false;
  return CurrentLock()&&component?.Pointer==usesPointer;
 }
 internal void Tick(){if(!UsesLocked)return;try{CurrentLock();}catch(Exception e){Disable(e);}}
 internal void Clear(){UsesLocked=false;scope=default;itemPointer=usesPointer=sourcePointer=inventoryPointer=IntPtr.Zero;}
 internal void Disable(Exception e){Clear();Plugin.MenuLog.LogWarning("[RPCheat] tool_limits_disabled "+e);}
 internal string RecallProjectiles()
 {
  combat.Require();var state=ReadCurrent();var source=state.Item.source.Pointer;var ammo=state.Item.source.GetComponent<PickableAmmoFeature>();
  var picker=combat.Game.Player.GetComponent<ProjectilePickerComponent>();
  if(ammo==null||picker?.projectiles==null)throw new InvalidOperationException(MenuText.Get("当前工具没有原生可回收投射物"));
  int maximum=ammo.maxAmmoCount,count=picker.projectiles.Count,remaining=picker.remainingProjectileCount;
  if(maximum<=0||maximum>256||remaining<0||count<0||(long)remaining+count!=maximum)
   throw new InvalidOperationException(MenuText.Get("投射物计数不一致或超出范围，未执行"));
  var before=ReadCurrent();
  if(!state.Scope.Matches(before.Scope)||state.Item.Pointer!=before.Item.Pointer||before.Item.source.Pointer!=source||state.Inventory.Pointer!=before.Inventory.Pointer||
     combat.Game.Player.GetComponent<ProjectilePickerComponent>()?.Pointer!=picker.Pointer)
   throw new InvalidOperationException(MenuText.Get("工具作用域已变化，未执行"));
  if(count==0)return MenuText.Get("没有需要回收的工具投射物");
  try
  {
   picker.ResetProjectiles();
   var after=ReadCurrent();
   if(!state.Scope.Matches(after.Scope)||state.Item.Pointer!=after.Item.Pointer||after.Item.source.Pointer!=source||state.Inventory.Pointer!=after.Inventory.Pointer||
      combat.Game.Player.GetComponent<ProjectilePickerComponent>()?.Pointer!=picker.Pointer||picker.projectiles.Count!=0||picker.remainingProjectileCount!=maximum)
    throw new InvalidOperationException(MenuText.Get("原生回收已请求但未确认；不会自动重试"));
   return MenuText.Format("已回收工具投射物：{0}；可用额度 {1}",count,maximum);
  }
  catch(Exception e){throw new InvalidOperationException(MenuText.Get("原生回收已请求但未确认；不会自动重试"),e);}
 }
 internal string Description()
 {
  try
  {
   var state=ReadCurrent();var uses=state.Item.GetComponent<RemainingUses>();var ammo=state.Item.source.GetComponent<PickableAmmoFeature>();
   var picker=ammo==null?null:combat.Game.Player.GetComponent<ProjectilePickerComponent>();
   var count=uses==null?MenuText.Get("无原生次数限制"):uses.remainingUses<0?MenuText.Get("已耗尽"):((long)uses.remainingUses+1).ToString();
   var projectiles=picker==null?MenuText.Get("无可回收投射物"):MenuText.Format("可用 {0} / {1}，在场 {2}",picker.remainingProjectileCount,ammo.maxAmmoCount,picker.projectiles?.Count??0);
   return MenuText.Format("工具：{0}\n可用次数：{1}\n投射物：{2}",ItemBrowser.ReadLabel(state.Item.source.title,state.Item.source.name).Display,count,projectiles);
  }
  catch(Exception e){if(UsesLocked)Disable(e);return e.Message;}
 }
}
[HarmonyPatch(typeof(RemainingUses),nameof(RemainingUses.OnUse))]
internal static class ToolUsesPatch
{
 internal static bool Prefix(RemainingUses __instance,Entity __0,InventoryComponent __1,ItemInstanceData __2)
 {
  var tools=CombatActions.Current?.Tools;
  try{return tools?.SuppressUse(__instance,__0,__1,__2)!=true;}
  catch(Exception e){tools?.Disable(e);return true;}
 }
}
