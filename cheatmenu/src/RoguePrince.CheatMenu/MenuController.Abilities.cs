// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using Il2CppInterop.Runtime.Attributes;
using RoguePrince.CheatMenu.Core;
namespace RoguePrince.CheatMenu;
public sealed partial class MenuController
{
    private AbilityLoadoutActions abilities;
    private List<ItemData> abilityCatalog = new();
    private readonly HashSet<string> abilityDraft = new(StringComparer.Ordinal);
    private string[] savedAbilityCombination;
    private int abilityPage;
    [HideFromIl2Cpp]
    private bool AbilityReady() => playerCheckpoint?.RecoveryPending!=true && TrainingActions.ReadScope(game).Valid && !levels.Loading && !BossPhaseWorkBusy && !FrameObservationActive
        && !runRestart.Busy && !scenes.Busy && !mobs.Busy && !training.Busy && !waves.Active
        && !loadouts.RecoveryPending && !affixes.RecoveryPending && !rescue.RecoveryPending;
    [HideFromIl2Cpp]
    private void DrawAbilities()
    {
        var ready = AbilityReady() && !abilities.Active && !game.InventoryRecoveryBlocked;
        if (Button("abilityRefresh",28,228,277,MenuText.Get("刷新可试装技能"),ready,false,16)) Do(MenuText.Get("本局技能试装"),()=>
        { abilityCatalog=abilities.Catalog();abilityPage=0;return MenuText.Format("可试装技能 {0} 项；只列已解锁且已接入装卸的技能",abilityCatalog.Count); });
        if (Button("abilityCapture",320,228,277,MenuText.Get("读取当前技能组合"),ready,false,16)) Do(MenuText.Get("本局技能试装"),()=>
        { abilityDraft.Clear();foreach(var id in abilities.Capture())abilityDraft.Add(id);return MenuText.Format("已读取 {0} 项到组合草稿",abilityDraft.Count); });
        if (Button("abilitySession",613,228,281,MenuText.Get("保存组合到会话"),!abilities.Active,false,16)) Do(MenuText.Get("本局技能试装"),()=>
        { savedAbilityCombination=abilityDraft.OrderBy(x=>x,StringComparer.Ordinal).ToArray();return MenuText.Get("组合已暂存；新角色应用时重新校验解锁和资源"); });
        var pages=Math.Max(1,(abilityCatalog.Count+4)/5);abilityPage=Math.Clamp(abilityPage,0,pages-1);
        for(var i=0;i<5 && abilityPage*5+i<abilityCatalog.Count;i++)
        {
            var data=abilityCatalog[abilityPage*5+i];var id=data.Guid.guid;
            var label=ItemBrowser.ReadLabel(data.title,data.name).Display;
            if(Button("abilityRow"+i,28,277+i*41,866,(abilityDraft.Contains(id)?"✓ ":"+ ")+label,ready,abilityDraft.Contains(id),16))
            {if(!abilityDraft.Add(id))abilityDraft.Remove(id);}
        }
        if(abilityCatalog.Count==0)Text(28,280,866,110,MenuText.Get("先刷新技能库；未解锁、单向效果和未知组件不会列出。没有支持项时拒绝试装。"),17,false,true);
        if(Button("abilityPrev",28,487,114,MenuText.Get("上一页"),abilityPage>0))abilityPage--;
        Text(157,487,426,36,MenuText.Format("第 {0} / {1} 页 · 已选 {2} 项",abilityPage+1,pages,abilityDraft.Count),16);
        if(Button("abilityNext",594,487,114,MenuText.Get("下一页"),abilityPage+1<pages))abilityPage++;
        if(Button("abilityLoadSession",724,487,170,MenuText.Get("载入会话组合"),savedAbilityCombination!=null&&!abilities.Active,false,15))Do(MenuText.Get("本局技能试装"),()=>
        {abilityDraft.Clear();foreach(var id in savedAbilityCombination)abilityDraft.Add(id);return MenuText.Get("会话组合已载入草稿，尚未应用");});
        if(Button("abilityApply",28,535,415,MenuText.Get("试装所选技能组合"),ready,false,17))Do(MenuText.Get("本局技能试装"),()=>abilities.Apply(abilityDraft.ToArray()));
        if(Button("abilityRestore",465,535,429,MenuText.Get(abilities.RecoveryPending?"重试恢复原技能":"恢复原技能实例"),abilities.Active,false,17))Do(MenuText.Get("本局技能试装"),()=>abilities.Restore());
        Text(28,579,866,39,MenuText.Get("空组合会临时移除技能。换房、加载、死亡或失焦时恢复；不会永久解锁，也不隔离原版自动保存。"),14,false,true);
    }
}
