// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using Il2CppInterop.Runtime.Attributes;
using RoguePrince.CheatMenu.Core;
namespace RoguePrince.CheatMenu;
public sealed partial class MenuController
{
 [HideFromIl2Cpp] private void DrawToolLimits()
 {
  Text(28,230,866,105,combat.Tools.Description(),18,false,true);
  Mode("toolUsesLock",28,347,866,"锁定当前工具次数",combat.Tools.UsesLocked,combat.Tools.SetUsesLock,ToolLimitActions.Available);
  if(Button("toolRecall",28,395,866,MenuText.Get("回收当前工具投射物"),CombatReady()))Do(MenuText.Get("回收当前工具投射物"),combat.Tools.RecallProjectiles);
  Text(28,445,866,50,MenuText.Get("只锁定当前装备工具的原生剩余次数；换工具、切房、失焦或死亡后自动关闭。已耗尽的次数不补回。"),16,false,true);
  Text(28,500,866,50,MenuText.Get("回收会移除该工具仍在场上的可回收投射物并归还额度，不补能量或重置冷却。未声明的特殊限制不处理。"),16,false,true);
  if(Button("toolUsesOff",28,560,866,MenuText.Get("关闭工具次数锁定"),combat.Tools.UsesLocked))Do(MenuText.Get("关闭工具次数锁定"),()=>{combat.Tools.Clear();return MenuText.Get("工具次数锁定已关闭");});
 }
}
