// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using Il2CppInterop.Runtime.Attributes;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
namespace RoguePrince.CheatMenu;
public sealed partial class MenuController
{
    private bool damageRecent;
    private int damageDetailsPage;
    [HideFromIl2Cpp] private void DrawDamageStatistics()
    {
        var ledger=combat.Ledger;var details=ledger.Details;
        Mode("damageDetailRecord",28,230,280,"伤害统计",combat.Recording,v=>combat.Recording=v,CombatActions.DamageAvailable);
        if(Button("damageDetailReset",326,230,268,MenuText.Get("清空伤害统计")))Do(MenuText.Get("伤害统计"),()=>{ledger.Reset();damageDetailsPage=0;return MenuText.Get("伤害统计已清空");});
        if(Button("damageDetailExport",611,230,283,MenuText.Get("复制伤害 JSON")))Do(MenuText.Get("伤害来源"),()=>
        {
            var options=new System.Text.Json.JsonSerializerOptions{WriteIndented=true};options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            GUIUtility.systemCopyBuffer=System.Text.Json.JsonSerializer.Serialize(new{schema=1,kind="damage-statistics",version=Plugin.Version,ledger.Total,ledger.Last,ledger.Hits,ledger.Seconds,ledger.Dps,ledger.Selected,ledger.SelectedTotal,details.Epoch,details.OtherHits,details.UnknownHits,details.SelfHits,details.Groups,details.Recent,groupLimit=DamageStatistics.GroupLimit,recentLimit=DamageStatistics.RecentLimit,sourceRule="native explicit source; fallback only when native source is absent; no inferred ownership"},options);
            return MenuText.Get("伤害统计 JSON 已复制");
        });
        Text(28,277,866,32,MenuText.Format("累计 {0:0.##} · 最后 {1:0.##} · 命中 {2}",ledger.Total,ledger.Last,ledger.Hits),21,true);
        Text(28,314,866,30,MenuText.Format("有效战斗 {0:0.##} 秒 · DPS {1:0.##} · 目标 {2:0.##}",ledger.Seconds,ledger.Dps,ledger.SelectedTotal),18);
        Text(28,348,866,29,MenuText.Format("排除：其他来源 {0} 次 · 未知来源 {1} 次 · 自身受伤 {2} 次",details.OtherHits,details.UnknownHits,details.SelfHits),15);
        if(Button("damageGroups",28,383,416,MenuText.Get("按物品与路径分组"),true,!damageRecent)){damageRecent=false;damageDetailsPage=0;}
        if(Button("damageRecent",462,383,432,MenuText.Get("最近命中（最多64条）"),true,damageRecent)){damageRecent=true;damageDetailsPage=0;}
        var groups=details.Groups.OrderByDescending(g=>g.Damage).ToArray();var hits=details.Recent.Reverse().ToArray();
        var count=damageRecent?hits.Length:groups.Length;var pages=Math.Max(1,(count+2)/3);damageDetailsPage=Math.Clamp(damageDetailsPage,0,pages-1);
        if(count==0)Text(28,431,866,72,MenuText.Get("暂无玩家伤害记录；开启统计后关闭菜单进行攻击。"),18,false,true);
        for(int i=0;i<3&&damageDetailsPage*3+i<count;i++)
        {
            var index=damageDetailsPage*3+i;var y=427+i*40;
            if(damageRecent)
            {
                var hit=hits[index];Text(28,y,866,19,DamageItem(hit.Credit.ItemName)+" · "+DamagePath(hit.Credit.Delivery),15,true);
                Text(28,y+19,866,19,MenuText.Format("命中 #{0} · 损失 {1:0.##} · 目标 {2}",hit.Sequence,hit.Damage,DamageName(hit.Credit.TargetName)),14);
            }
            else
            {
                var group=groups[index];Text(28,y,866,19,group.Overflow?MenuText.Get("其他分组（容量上限合并）"):DamageItem(group.ItemName)+" · "+DamagePath(group.Delivery),15,true);
                Text(28,y+19,866,19,MenuText.Format("累计 {0:0.##} · 命中 {1} 次",group.Damage,group.Hits),14);
            }
        }
        if(Button("damageDetailPrev",28,553,130,MenuText.Get("上一页"),damageDetailsPage>0))damageDetailsPage--;
        Text(180,553,540,36,MenuText.Format("第 {0} / {1} 页 · {2} 项",damageDetailsPage+1,pages,count),16);
        if(Button("damageDetailNext",754,553,140,MenuText.Get("下一页"),damageDetailsPage+1<pages))damageDetailsPage++;
        Text(28,593,866,20,MenuText.Get("最多32组 / 64击；直接伤害可含有明确原生来源的持续效果。"),12);
    }
    [HideFromIl2Cpp] private static string DamageName(string name)=>string.IsNullOrEmpty(name)?"—":name.Length<=32?name:name[..32]+"…";
    [HideFromIl2Cpp] private static string DamageItem(string name)=>string.IsNullOrEmpty(name)?MenuText.Get("无来源物品"):DamageName(name);
    [HideFromIl2Cpp] private static string DamagePath(DamageDelivery path)=>MenuText.Get(path switch
    {DamageDelivery.Collision=>"碰撞伤害",DamageDelivery.SpawnedCollision=>"生成物碰撞",DamageDelivery.DirectCast=>"直接伤害",_=>"未知路径"});
}
