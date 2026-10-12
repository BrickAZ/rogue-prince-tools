// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;
public static partial class MenuText
{
 private static void AddWorldReportTranslations()
 {
  var additions=new Dictionary<string,string>{
   ["场景状态报告"]="Scene state report",
   ["采样场景状态"]="Sample scene state",
   ["复制状态 JSON"]="Copy state JSON",
   ["清除状态报告"]="Clear state report",
   ["尚未采样当前场景"]="No scene sample yet.",
   ["采样当前场景的实体、组件清单、生命、位置和玩家物品，复制 JSON 供后续快照调查。"]="Sample entities, component lists, health, positions and player items. Copy JSON for snapshot research.",
   ["报告包含未覆盖状态；完整快照、读档与死亡恢复尚未实现。"]="The report lists uncovered state. Full snapshots, loading and death restoration are not implemented.",
   ["保留采样时刻；关闭菜单、失焦或切换场景后清除。"]="Keeps the sampled moment; cleared on menu close, focus loss or scene changes.",
   ["状态报告需要菜单暂停、焦点和当前已加载战斗场景"]="A report requires menu pause, focus and a currently loaded combat scene.",
   ["状态报告原生集合尚未就绪"]="Native collections for the report are not ready.",
   ["状态报告成员不一致；未发布"]="Report members are inconsistent. Nothing published.",
   ["状态报告含无效数值；未发布"]="Report contains invalid numbers. Nothing published.",
   ["状态报告超过容量；未发布"]="Report exceeds capacity. Nothing published.",
   ["采样期间场景或集合变化；报告已丢弃"]="Scene or collection changed during sampling. Report discarded.",
   ["报告已失效，请重新采样"]="Report expired. Sample again.",
   ["场景状态 JSON 已复制"]="Scene state JSON copied.",
   ["场景状态报告已清除"]="Scene state report cleared.",
   ["场景状态已采样；完整恢复尚未实现"]="Scene state sampled. Full restoration is not implemented.",
   ["实体 {0}（启用 {1} / 停用 {2}）· 物品 {3} · 帧 {4}"]="Entities {0} (enabled {1} / disabled {2}) · Items {3} · Frame {4}"
  };
  foreach(var pair in additions)English[pair.Key]=pair.Value;
 }
}
