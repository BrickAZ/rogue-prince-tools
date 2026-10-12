// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;
public static partial class MenuText
{
 private static void AddToolLimitTranslations()
 {
  var additions=new Dictionary<string,string>
  {
   ["工具次数与投射物"]="Tool limits",
   ["当前没有已装备的原生工具"]="No native utility weapon is equipped.",
   ["工具作用域已变化，未执行"]="The tool scope changed; no action was taken.",
   ["当前工具没有原生次数限制"]="This tool has no native limited-use component.",
   ["当前工具次数已耗尽；未补充或锁定"]="This tool is exhausted; uses were neither refilled nor locked.",
   ["当前工具没有原生可回收投射物"]="This tool has no native recoverable projectiles.",
   ["投射物计数不一致或超出范围，未执行"]="Projectile counts are inconsistent or out of range; no action was taken.",
   ["没有需要回收的工具投射物"]="There are no tool projectiles to recall.",
   ["原生回收已请求但未确认；不会自动重试"]="Native recall was requested but not confirmed; it will not retry automatically.",
   ["已回收工具投射物：{0}；可用额度 {1}"]="Recalled {0} tool projectiles; available quota {1}.",
   ["无原生次数限制"]="No native use limit",["已耗尽"]="Exhausted",["无可回收投射物"]="No recoverable projectiles",
   ["可用 {0} / {1}，在场 {2}"]="Available {0} / {1}, in world {2}",
   ["工具：{0}\n可用次数：{1}\n投射物：{2}"]="Tool: {0}\nAvailable uses: {1}\nProjectiles: {2}",
   ["锁定当前工具次数"]="Lock current tool uses",
   ["回收当前工具投射物"]="Recall current tool projectiles",
   ["关闭工具次数锁定"]="Disable tool use lock",
   ["工具次数锁定已关闭"]="Tool use lock disabled.",
   ["只锁定当前装备工具的原生剩余次数；换工具、切房、失焦或死亡后自动关闭。已耗尽的次数不补回。"]="Locks only the equipped tool's native remaining uses. Changing tools or rooms, losing focus or dying disables the lock. Exhausted uses are not refilled.",
   ["回收会移除该工具仍在场上的可回收投射物并归还额度，不补能量或重置冷却。未声明的特殊限制不处理。"]="Recall removes this tool's recoverable projectiles from the world and returns their quota. Energy and cooldown are unchanged. Other special limits are not handled.",
  };
  foreach(var pair in additions)English[pair.Key]=pair.Value;
 }
}
