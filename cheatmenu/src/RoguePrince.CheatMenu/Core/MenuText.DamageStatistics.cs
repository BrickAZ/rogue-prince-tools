// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;
public static partial class MenuText
{
    private static void AddDamageStatisticsTranslations()
    {
        var additions=new Dictionary<string,string>{
            ["伤害来源"]="Damage sources",
            ["复制伤害 JSON"]="Copy damage JSON",
            ["伤害统计 JSON 已复制"]="Damage statistics JSON copied.",
            ["排除：其他来源 {0} 次 · 未知来源 {1} 次 · 自身受伤 {2} 次"]="Excluded: other {0} · unknown {1} · player hurt {2}",
            ["按物品与路径分组"]="Group by item / delivery",
            ["最近命中（最多64条）"]="Recent hits (up to 64)",
            ["暂无玩家伤害记录；开启统计后关闭菜单进行攻击。"]="No player damage yet. Enable recording, close the menu, then attack.",
            ["命中 #{0} · 损失 {1:0.##} · 目标 {2}"]="Hit #{0} · Loss {1:0.##} · Target {2}",
            ["其他分组（容量上限合并）"]="Other groups (capacity overflow)",
            ["累计 {0:0.##} · 命中 {1} 次"]="Damage {0:0.##} · Hits {1}",
            ["最多32组 / 64击；直接伤害可含有明确原生来源的持续效果。"]="Up to 32 groups / 64 hits; direct damage may include effects with an explicit native source.",
            ["无来源物品"]="No source item",
            ["碰撞伤害"]="Collision damage",
            ["生成物碰撞"]="Spawned collision",
            ["直接伤害"]="Direct damage",
            ["未知路径"]="Unknown delivery"
        };
        foreach(var pair in additions)English[pair.Key]=pair.Value;
    }
}
