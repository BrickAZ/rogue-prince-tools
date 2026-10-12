// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;
public static partial class MenuText
{
 private static void AddBossTargetTranslations()
 {
  var additions=new Dictionary<string,string>
  {
   ["Boss 目标行为过滤"]="Boss target filtering",
   ["未开启"]="Off",
   ["Boss 列表作用域已变化，请刷新"]="The Boss list scope changed; refresh it.",
   ["当前房间 Boss：{0}；支持 Nogai / Kaali"]="Bosses in this room: {0}. Nogai / Kaali supported.",
   ["Boss 目标行为过滤已关闭"]="Boss target filtering disabled.",
   ["请刷新并选择当前房间 Boss"]="Refresh and select a Boss in the current room.",
   ["该 Boss 尚不支持目标行为过滤；未执行"]="Target filtering is unsupported for this Boss; no action taken.",
   ["Boss 或原生组件未就绪，请刷新"]="The Boss or its native components are not ready; refresh.",
   ["已过滤 {0} 的目标行为；已启动动作继续"]="Filtering {0}'s target behaviour; running actions continue.",
   ["Boss 过滤已关闭：目标或作用域失效"]="Boss filtering disabled: target or scope became invalid.",
   ["Boss 过滤读取失败，已关闭；可重新开启"]="Boss filtering disabled after a read failure; it can be enabled again.",
   ["只过滤 Nogai / Kaali 的已接入朝向、追赶与新攻击选择；不清空目标，不取消已启动动作。"]="Filters Nogai / Kaali's connected facing, pursuit and new attack selection. Targets and running actions are retained.",
   ["刷新当前房间 Boss"]="Refresh room Bosses",
   ["过滤目标：{0}"]="Filtered Boss: {0}",
   ["支持过滤"]="Filtering supported",["尚未支持"]="Unsupported",
   ["开启所选 Boss 过滤"]="Filter selected Boss",["关闭 Boss 过滤"]="Disable Boss filter",
   ["阶段脑逻辑继续。动作自身追踪、场地攻击仍可能命中；其他 Boss 不支持。"]="Phase brain logic continues. Action tracking and arena attacks may still hit. Other Bosses are unsupported.",
   ["当前房间没有可列出的 Boss，请刷新"]="No Bosses listed in this room. Refresh the list.",
   ["当前房间 Boss：{0}；支持 Nogai / Kaali / Baatar / Corrupted Nogai"]="Room Bosses: {0}. Nogai / Kaali / Baatar / Corrupted Nogai supported.",
   ["仅新攻击选择"]="New attacks only",
   ["仅填充攻击"]="Filler attacks only",
   ["Nogai 过滤朝向、追赶与新攻击选择。阶段与已启动动作仍可能命中。"]="Nogai: facing, pursuit and new attack selection. Phase and running attacks may still hit.",
   ["Kaali 过滤朝向及近战、远程行为选择。阶段与已启动动作仍可能命中。"]="Kaali: facing and melee/ranged behaviour selection. Phase and running attacks may still hit.",
   ["过滤所选实例已接入的行为。各 Boss 范围不同，选择后查看右侧说明。"]="Filters connected behaviours of the selected instance. Select a Boss to see its scope on the right.",
   ["当前房间 Boss：{0}；支持 Nogai / Kaali / Baatar / Corrupted Nogai / Altan / Berude"]="Room Bosses: {0}. Nogai / Kaali / Baatar / Corrupted Nogai / Altan / Berude supported.",
   ["Baatar 按实例过滤新序列攻击。阶段与已启动动作继续，仍可能命中。"]="Baatar: new sequence attacks by instance. Phases and running actions continue and may still hit.",
   ["Corrupted Nogai 仅过滤面向玩家的新攻击。阶段与已启动动作仍可能命中。"]="Corrupted Nogai: new attacks targeting the player. Phase and running attacks may still hit.",
   ["Altan 仅过滤常规新攻击选择。换位、外层特殊攻击仍可能命中。"]="Altan: regular new attack selection only. Repositioning and outer special attacks may still hit.",
   ["Berude 仅过滤填充攻击。主攻击编排、追赶与朝向继续，仍可能命中。"]="Berude: filler attacks only. Main attacks, pursuit and facing continue and may still hit.",
   ["仅新序列攻击"]="Sequence attacks only",
   ["已过滤 {0}：{1}；已启动动作继续"]="Filtering {0}: {1}; running actions continue.",
   ["Nogai / Kaali 保留原过滤；Baatar / Corrupted Nogai 仅过滤新攻击选择。"]="Nogai / Kaali retain their existing filters. Baatar / Corrupted Nogai filter new attack selection only.",
   ["Nogai / Kaali 保留原过滤；Baatar 按实例过滤新序列攻击，Corrupted Nogai 按玩家目标过滤新攻击。"]="Nogai / Kaali retain their filters. Baatar filters new sequence attacks by instance; Corrupted Nogai filters new attacks targeting the player.",
  };
  foreach(var pair in additions)English[pair.Key]=pair.Value;
 }
}
