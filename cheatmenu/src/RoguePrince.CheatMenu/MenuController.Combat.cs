// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using Il2CppInterop.Runtime.Attributes;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
namespace RoguePrince.CheatMenu;
public sealed partial class MenuController
{
    private CombatActions combat;
    private EffectActions effects;
    private LoadoutActions loadouts;
    private WaveActions waves;
    private RewardActions rewards;
    private EffectActions.Entry chosenEffect;
    private string chosenLoadout;
    private int editEffectPage,loadoutPage;
    private bool showPractice;
    [HideFromIl2Cpp] private bool CombatReady()=>TrainingActions.ReadScope(game).Valid&&!levels.Loading&&!CoreWorkBusy&&!runRestart.Busy&&!loadouts.RecoveryPending;
    [HideFromIl2Cpp] private static float Number(string text,float min,float max)
    {if(!float.TryParse(text,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var value)||!float.IsFinite(value)||value<min||value>max)throw new ArgumentException(MenuText.Get("输入值超出范围"));return value;}
    [HideFromIl2Cpp] private static int Whole(string text,int min,int max)
    {if(!int.TryParse(text,out var value)||value<min||value>max)throw new ArgumentException(MenuText.Get("请输入范围内的整数"));return value;}
    [HideFromIl2Cpp] private void Mode(string id,float a,float b,float w,string name,bool value,Action<bool> set,bool available=true)
    {if(Button(id,a,b,w,MenuText.Get(name)+" · "+MenuText.Get(value?"开启":"关闭"),value||CombatReady()&&available,value,16))Do(MenuText.Get(name),()=>{if(!value)combat.Require();set(!value);return MenuText.Get(value?"已关闭":"已开启");});}
    [HideFromIl2Cpp] private void DrawCombat()
    {
        Mode("damageRecord",28,230,280,"伤害统计",combat.Recording,v=>combat.Recording=v,CombatActions.DamageAvailable);
        if(Button("damageReset",326,230,268,MenuText.Get("清空伤害统计")))Do(MenuText.Get("伤害统计"),()=>{combat.Ledger.Reset();return MenuText.Get("伤害统计已清空");});
        if(Button("damageTarget",611,230,283,MenuText.Get("使用选中的自有怪物"),CombatReady()&&selectedTarget!=0))Do(MenuText.Get("统计目标"),()=>{var entity=mobs.TargetEntity(selectedTarget);combat.Ledger.Select(entity.Pointer.ToInt64());return MenuText.Get("统计目标已更新");});
        Text(28,277,862,34,MenuText.Format("累计 {0:0.##} · 最后 {1:0.##} · 命中 {2}",combat.Ledger.Total,combat.Ledger.Last,combat.Ledger.Hits),22,true);
        Text(28,318,862,34,MenuText.Format("有效战斗 {0:0.##} 秒 · DPS {1:0.##} · 目标 {2:0.##}",combat.Ledger.Seconds,combat.Ledger.Dps,combat.Ledger.SelectedTotal),20);
        Text(28,363,155,36,MenuText.Get("玩家伤害倍率"),17);var value=Field("damageMultiplier",193,363,126,"1");
        if(Button("damageApply",337,363,257,MenuText.Get("应用（0.1–10 倍）"),CombatReady()&&CombatActions.DamageAvailable))Do(MenuText.Get("玩家伤害倍率"),()=>{combat.SetDamage(Number(value,.1f,10));return MenuText.Format("伤害倍率 {0:0.##} 倍",combat.DamageMultiplier);});
        if(Button("damageNormal",611,363,283,MenuText.Get("恢复全部战斗参数")))Do(MenuText.Get("战斗参数"),()=>{waves.StopDummy();combat.StopModes();return MenuText.Get("战斗参数已恢复");});
        Mode("oneHit",28,410,416,"一击必杀",combat.OneHit,v=>combat.OneHit=v,CombatActions.DamageAvailable);
        Mode("undying",462,410,432,"不死（仍受击）",combat.Undying,v=>combat.Undying=v,CombatActions.DamageAvailable);
        Text(28,459,862,55,MenuText.Get("只修改当前玩家造成的扣血；原生不可致死规则保留。不死保留普通受击和击退。"),17,false,true);
        Text(28,522,862,69,MenuText.Get("伤害按实际生命减少统计；护盾吸收、免疫和治疗不计入。菜单、暂停和加载期间停止计时，特殊剧情死亡仍需实测。"),16,false,true);
    }
    [HideFromIl2Cpp] private void DrawEffectEditor()
    {
        var query=Field("effectEditSearch",28,230,594,"");
        if(Button("effectEditReload",639,230,255,MenuText.Get("刷新已加载效果"),CombatReady()))Do(MenuText.Get("效果库"),()=>{effects.Reload();chosenEffect=null;editEffectPage=0;return MenuText.Format("已加载 {0} 种效果",effects.Catalog.Count);});
        var rows=effects.Catalog.Where(e=>e.Label.Matches(query)||e.Data.name.Contains(query,StringComparison.OrdinalIgnoreCase)).ToList();var pages=Math.Max(1,(rows.Count+5)/6);editEffectPage=Math.Clamp(editEffectPage,0,pages-1);
        for(int i=0;i<6&&editEffectPage*6+i<rows.Count;i++){var entry=rows[editEffectPage*6+i];if(Button("effectEditRow"+i,28,278+i*42,500,entry.Name,true,entry==chosenEffect,16))chosenEffect=entry;}
        Text(552,276,342,68,chosenEffect?.Name??MenuText.Get("请选择已加载的效果"),17,true,true);
        Text(552,352,150,32,MenuText.Get("目标层数"),16);var stacks=Field("effectStacks",712,352,182,"1");
        Text(552,394,150,32,MenuText.Get("时长（秒）"),16);var seconds=Field("effectSeconds",712,394,182,"10");
        if(Button("effectEditApply",552,440,342,MenuText.Get("设置效果 / 层数 / 时长"),CombatReady()&&chosenEffect!=null,false,16))Do(MenuText.Get("效果管理"),()=>effects.Apply(chosenEffect,Whole(stacks,1,1000),Number(seconds,.1f,3600)));
        if(Button("effectEditRemove",552,484,342,MenuText.Get("移除选中效果"),CombatReady()&&chosenEffect!=null))Do(MenuText.Get("效果管理"),()=>effects.Remove(chosenEffect));
        if(Button("effectEditClear",552,528,342,MenuText.Get("移除当前所有效果"),CombatReady()))Do(MenuText.Get("效果管理"),effects.Clear);
        if(Button("effectEditPrev",28,537,110,MenuText.Get("上一页"),editEffectPage>0))editEffectPage--;
        Text(152,537,231,36,MenuText.Format("第 {0} / {1} 页 · {2} 项",editEffectPage+1,pages,rows.Count),15);
        if(Button("effectEditNext",412,537,116,MenuText.Get("下一页"),editEffectPage+1<pages))editEffectPage++;
        Text(28,579,866,23,MenuText.Get("清空也会移除游戏增益；使用原生效果生命周期与免疫规则。"),14);
    }
    [HideFromIl2Cpp] private void DrawLoadouts()
    {
        var name=Field("loadoutName",28,230,549,"");
        if(Button("loadoutSave",595,230,299,MenuText.Get("保存当前完整配装"),CombatReady()&&!loadouts.Library.ReadOnly))Do(MenuText.Get("完整配装"),()=>{var p=loadouts.Capture(name);loadouts.Library.Save(p);chosenLoadout=p.Name;return MenuText.Get("完整配装已保存");});
        var all=loadouts.Library.Presets;var pages=Math.Max(1,(all.Count+5)/6);loadoutPage=Math.Clamp(loadoutPage,0,pages-1);var selectedLoadout=all.FirstOrDefault(p=>p.Name==chosenLoadout);
        for(int i=0;i<6&&loadoutPage*6+i<all.Count;i++){var preset=all[loadoutPage*6+i];if(Button("loadoutRow"+i,28,278+i*42,500,preset.Name,true,preset==selectedLoadout)){chosenLoadout=preset.Name;fields["loadoutName"]=preset.Name;}}
        Text(552,278,342,61,selectedLoadout?.Name??MenuText.Get("请选择完整配装"),19,true,true);
        Text(552,343,342,52,selectedLoadout==null?"":MenuText.Format("武器 {0} 级 · 工具 {1} 级 · 徽章 {2} 枚",selectedLoadout.Main.Tier,selectedLoadout.Tool.Tier,selectedLoadout.Medallions.Count),17,false,true);
        if(Button("loadoutApply",552,402,342,MenuText.Get("应用完整配装"),CombatReady()&&LoadoutActions.Available&&selectedLoadout!=null))Do(MenuText.Get("完整配装"),()=>loadouts.Apply(selectedLoadout));
        if(Button("loadoutRecover",552,446,342,MenuText.Get("重试恢复原配装"),loadouts.RecoveryPending))Do(MenuText.Get("完整配装"),loadouts.Recover);
        if(Button("loadoutImport",552,490,164,MenuText.Get("导入 JSON"),!loadouts.Library.ReadOnly,false,15))Do(MenuText.Get("完整配装"),()=>{var p=LoadoutPreset.Parse(GUIUtility.systemCopyBuffer);loadouts.Library.Save(p);chosenLoadout=p.Name;return MenuText.Get("配装已导入，应用时检查兼容性");});
        if(Button("loadoutExport",730,490,164,MenuText.Get("复制 JSON"),selectedLoadout!=null,false,15))Do(MenuText.Get("完整配装"),()=>{GUIUtility.systemCopyBuffer=selectedLoadout.Export();return MenuText.Get("配装 JSON 已复制");});
        if(Button("loadoutDelete",552,534,342,MenuText.Get("删除选中配装"),selectedLoadout!=null&&!loadouts.Library.ReadOnly))Do(MenuText.Get("完整配装"),()=>{loadouts.Library.Delete(selectedLoadout.Name);chosenLoadout=null;return MenuText.Get("配装已删除");});
        if(Button("loadoutPrev",28,537,110,MenuText.Get("上一页"),loadoutPage>0))loadoutPage--;
        Text(152,537,231,36,MenuText.Format("第 {0} / {1} 页 · {2} 项",loadoutPage+1,pages,all.Count),15);
        if(Button("loadoutNext",412,537,116,MenuText.Get("下一页"),loadoutPage+1<pages))loadoutPage++;
        Text(28,579,866,23,loadouts.Library.ReadOnly?MenuText.Get("配装文件只读")+": "+loadouts.Library.Error:MenuText.Get("不增加徽章容量；不兼容配装拒绝执行，替换失败恢复原实例。"),14);
    }
    [HideFromIl2Cpp] private void DrawMotionTools()
    {
        Text(28,230,145,36,MenuText.Get("移动速度倍率"),17);var move=Field("movementMultiplier",184,230,125,"1");
        if(Button("moveSet",327,230,271,MenuText.Get("应用（0.25–3 倍）"),CombatReady()&&CombatActions.MotionAvailable))Do(MenuText.Get("移动速度"),()=>{combat.SetMovement(Number(move,.25f,3));return MenuText.Get("移动倍率已设置");});
        Text(619,230,275,36,MenuText.Format("速度读数 {0:0.##}",combat.LastMovementSpeed),17);
        Text(28,278,145,36,MenuText.Get("攻击速度倍率"),17);var attack=Field("attackMultiplier",184,278,125,"1");
        if(Button("attackSet",327,278,271,MenuText.Get("应用（0.25–3 倍）"),CombatReady()&&CombatActions.AttackAvailable))Do(MenuText.Get("攻击速度"),()=>{combat.SetAttack(Number(attack,.25f,3));return MenuText.Get("绑定武器动作倍率已设置");});
        Text(619,278,275,36,MenuText.Format("动作倍率 {0:0.##}",combat.AttackMultiplier),17);
        Mode("infiniteJump",28,326,416,"无限空中跳跃",combat.InfiniteJump,v=>combat.InfiniteJump=v,CombatActions.MotionAvailable);
        Mode("noKnockback",462,326,432,"玩家无击退",combat.NoKnockback,v=>combat.NoKnockback=v,CombatActions.MotionAvailable);
        Mode("toolCooldown",28,374,416,"工具无冷却",combat.ToolCooldown,v=>{if(v&&combat.ToolIds().Count==0)throw new InvalidOperationException(MenuText.Get("当前工具没有可识别的原生冷却"));combat.ToolCooldown=v;},CombatActions.ToolAvailable);
        if(Button("toolReset",462,374,432,MenuText.Get("重置当前工具冷却"),CombatReady()))Do(MenuText.Get("工具冷却"),combat.ResetTool);
        Mode("toolNoCost",28,422,866,"工具无能量消耗",combat.ToolNoCost,combat.SetToolNoCost,CombatActions.ToolAvailable);
        Text(28,468,866,38,MenuText.Get("攻速只推进当前装备绑定的攻击动作；工具只绕过自身声明的冷却与能量槽消耗。"),16,false,true);
        if(Button("motionRestore",28,516,866,MenuText.Get("恢复全部战斗参数")))Do(MenuText.Get("战斗参数"),()=>{waves.StopDummy();combat.StopModes();return MenuText.Get("战斗参数已恢复");});
        Text(28,560,866,40,MenuText.Get("无消耗不修改共享资源。未识别的弹药、蓄力与特殊动作条件继续遵守原版规则。"),16,false,true);
    }
    [HideFromIl2Cpp] private void DrawWavesPractice()
    {
        if(Button("wavesView",28,230,416,MenuText.Get("波次与木桩"),true,!showPractice))showPractice=false;
        if(Button("statsView",462,230,432,MenuText.Get("练习统计"),true,showPractice))showPractice=true;
        if(showPractice){DrawPracticeStatistics();return;}
        Text(28,276,866,33,MenuText.Get("选中普通怪物")+"："+(selectedMob?.Data.Boss==false?selectedMob.Name:MenuText.Get("请在怪物页选择")),18,true);
        Text(28,319,85,36,MenuText.Get("波数"),17);var count=Field("waveCount",119,319,127,"3");
        Text(269,319,133,36,MenuText.Get("每波数量"),17);var per=Field("wavePer",411,319,127,"3");
        if(Button("waveStart",556,319,338,MenuText.Get("开始怪物波次"),CombatReady()&&!training.Busy&&!mobs.Busy&&!waves.Active&&selectedMob?.Data.Boss==false))Do(MenuText.Get("怪物波次"),()=>AdmitTraining(()=>{var r=waves.Start(selectedMob.InternalName,Whole(count,1,20),Whole(per,1,10));Toggle();return r;}));
        Text(28,362,866,34,MenuText.Format("第 {0} / {1} 波 · 已生成 {2} · 存活 {3}",waves.Plan.Wave,waves.Plan.Waves,waves.Plan.Spawned,waves.Plan.Living.Count),18);
        if(Button("waveStop",28,405,416,MenuText.Get("停止波次"),waves.Plan.Active))Do(MenuText.Get("怪物波次"),waves.Stop);
        if(Button("waveClear",462,405,432,MenuText.Get("停止并清理自有怪物"),waves.Active||mobs.HasOwned))Do(MenuText.Get("训练清理"),()=>{waves.Reset();return training.ClearAll();});
        if(Button("dummyStart",28,453,416,MenuText.Get("选中目标设为固定木桩"),CombatReady()&&!waves.Active&&selectedTarget!=0))Do(MenuText.Get("训练木桩"),()=>waves.SetDummy(selectedTarget));
        if(Button("dummyStop",462,453,432,MenuText.Get("关闭木桩模式"),waves.DummyActive))Do(MenuText.Get("训练木桩"),waves.StopDummy);
        Text(28,503,866,49,waves.Status,16,true,true);
        Text(28,556,866,43,MenuText.Get("木桩目标在自有怪物页选择。波次仅支持普通怪物；清理、切房、死亡和失焦会停止。"),16,false,true);
    }
    [HideFromIl2Cpp] private void DrawPracticeStatistics()
    {
        Mode("practiceAuto",28,278,416,"自动房间分段",combat.AutoPractice,v=>{if(!combat.WaveRecording)combat.MarkPractice("toggle");combat.AutoPractice=v;},CombatActions.DamageAvailable&&CombatActions.PracticeAvailable);
        if(Button("practiceStatsExport",462,278,209,MenuText.Get("复制统计 JSON"),true,false,15))Do(MenuText.Get("练习统计"),()=>{GUIUtility.systemCopyBuffer=System.Text.Json.JsonSerializer.Serialize(new{records=combat.Practice.Records,best=combat.Practice.Best},new System.Text.Json.JsonSerializerOptions{WriteIndented=true});return MenuText.Get("练习统计已复制");});
        if(Button("practiceStatsClear",685,278,209,MenuText.Get("清空练习统计"),true,false,15))Do(MenuText.Get("练习统计"),()=>{combat.Practice.Reset();return MenuText.Get("练习统计已清空");});
        var stats=combat.Practice;
        Text(28,324,866,33,MenuText.Format("当前 {0} · {1:0.##} 秒 · 受击 {2}",stats.Segment??"—",stats.Seconds,stats.Hits),18,true);
        var bestKey=stats.Segment??stats.Records.LastOrDefault()?.Segment;Text(28,360,866,30,MenuText.Get("最佳有效时间")+": "+(bestKey!=null&&stats.Best.TryGetValue(bestKey,out var best)?FormatDuration(best):"—"),17);
        for(int i=0;i<3&&i<stats.Records.Count;i++){var r=stats.Records[stats.Records.Count-1-i];Text(28,400+i*46,866,43,$"{r.Segment} · {r.Seconds:0.##} s · {MenuText.Get("受击")} {r.Hits} · {PracticeResult(r.Result)} {r.Location}",15,false,true);}
        Text(28,552,866,47,MenuText.Get("记录分段、受击与死亡位置；波次全部完成或原版出口目标加载确认后记最佳，跳关和手动停止不算成功。"),16,false,true);
    }
    [HideFromIl2Cpp] private static string PracticeResult(string result)=>MenuText.Get(result switch
    {
        "death"=>"死亡", "reset"=>"重置", "loading"=>"加载", "room change"=>"切换房间", "teleport"=>"传送",
        "toggle"=>"切换记录", "native exit pending"=>"出口加载待确认", "native exit"=>"原版出口已完成",
        "complete"=>"完成", "stop" or "cancel"=>"手动停止", "clear" or "cleared"=>"清理",
        "wave start"=>"开始波次", "teleport / menu travel"=>"传送或跳关", _=>result
    });
    [HideFromIl2Cpp] private void DrawRewards()
    {
        Text(28,231,866,54,MenuText.Get("物品页选中")+"："+(selected?.Name??MenuText.Get("请先选择有效物品")),20,true,true);
        Text(28,292,120,36,MenuText.Get("奖励等级"),17);var tier=Field("rewardTier",164,292,167,"1");
        if(Button("rewardItemPage",353,292,541,MenuText.Get("前往物品清单选择奖励"))){tab=2;focus="";}
        if(Button("rewardCreate",28,343,866,MenuText.Get("生成包含所选物品的免费宝箱"),CombatReady()&&RewardActions.Available&&selected!=null))Do(MenuText.Get("奖励容器"),()=>rewards.Spawn(new ItemInstanceData(selected.Data,selected.HasTier?Whole(tier,1,100):1)));
        if(Button("rewardAffix",28,391,866,MenuText.Get("生成包含当前词缀草稿的宝箱"),CombatReady()&&RewardActions.Available&&affixes.HasTarget&&!affixes.RecoveryPending))Do(MenuText.Get("奖励容器"),()=>rewards.Spawn(affixes.PrepareInstance(affixes.CapturePreset("reward")).Owner));
        if(Button("rewardClear",28,439,866,MenuText.Get("清理自有奖励容器"),rewards.Active))Do(MenuText.Get("奖励容器"),rewards.Clear);
        Text(28,491,866,49,rewards.Status,17,true,true);
        Text(28,547,866,53,MenuText.Get("走近后用原版交互开箱，再拾取奖励。免费宝箱资源不支持时提示退出；切房后清理自有容器。"),17,false,true);
    }
}
