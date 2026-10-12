// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using BepInEx;
using Il2CppInterop.Runtime.Attributes;
using RoguePrince.CheatMenu.Core;
using UnityEngine;

namespace RoguePrince.CheatMenu;

public sealed partial class MenuController
{
    private UtilityLibrary utilities;
    private Dictionary<string,string> actionKeys = new();
    private readonly PracticeTimer practice = new();
    private bool itemFavoritesOnly, mobFavoritesOnly, affixFavoritesOnly;
    private string actionKeyCapture, selectedPresetName;
    private int presetPage, utilitySection, logPage,utilityPage;
    private bool practiceWasActive;
    private bool utilityLoading=true;
    private static readonly string[] ActionLabels = { "回满生命", "补满能量", "1 滴血生成", "原版满血生成", "清理本菜单生成的怪物", "开始 / 停止计时", "恢复正常速度", "退出截图模式" };

    [HideFromIl2Cpp]
    private void InitUtilities()
    {
        utilities = new UtilityLibrary(System.IO.Path.Combine(Paths.ConfigPath,"local.rogueprince.cheatmenu.utilities.json"));
        actionKeys = utilities.Data.Bindings;
        if(utilities.ReadOnly) Plugin.LogEvent("utility_config_read_only "+utilities.Error);
    }
    [HideFromIl2Cpp]
    private bool HasActionKeys() => actionKeys.Values.Any(k=>k!="None");
    [HideFromIl2Cpp]
    private bool UtilityPlayerReady() => game.Ready && game.Saver?.mainCharacterVariable?.CurrentValue?.Pointer==game.PlayerPointer;
    [HideFromIl2Cpp]
    private bool PracticeActive() => UtilityPlayerReady() && Application.isFocused && !visible && !utilityLoading && Time.timeScale>0 && releaseInputAt<=0;
    [HideFromIl2Cpp]
    private bool ReadUtilityLoading()
    {
        if(!UtilityPlayerReady() || levels.Loading || runRestart.Busy || bossPractice.Busy) return true;
        var service=RuntimeServiceLocator.Get<ApplicationFlowService>();
        var current=service?.GetCurrentStateLogic()?.flowManagerVariable?.CurrentValue;
        return service?.IsRunActive!=true || current==null || current.isLoading;
    }
    [HideFromIl2Cpp]
    private void UpdateUtilities()
    {
        if(!visible && !practice.Running && !HasActionKeys()) {practiceWasActive=false;return;}
        utilityLoading=ReadUtilityLoading();
        var active=PracticeActive();
        practice.Advance(Time.unscaledDeltaTime, active && practiceWasActive);
        practiceWasActive=active;
        var emergencyKeyboard=UnityEngine.InputSystem.Keyboard.current;
        if(!visible && focus.Length==0 && actionKeyCapture==null && !hotkeyCapture && Application.isFocused && emergencyKeyboard!=null)
            foreach(var emergency in new[]{"normalSpeed","exitPhoto"})
                if(actionKeys.TryGetValue(emergency,out var binding) && binding!="None" && binding!="F8" && binding!=Plugin.Hotkey.Value.ToString() && Enum.TryParse<UnityEngine.InputSystem.Key>(binding,out var emergencyKey) && emergencyKeyboard[emergencyKey].wasPressedThisFrame)
                {Do(MenuText.Get(emergency=="normalSpeed"?"恢复正常速度":"退出截图模式"),()=>RunUtilityAction(emergency));return;}
        if(!ActionKeyPolicy.CanDispatch(visible,focus.Length>0 || actionKeyCapture!=null || hotkeyCapture,Application.isFocused,UtilityPlayerReady(),utilityLoading,affixes.RecoveryPending||rescue.RecoveryPending||loadouts.RecoveryPending,mobs.Busy||training.Busy||waves.Active||CoreWorkBusy||runRestart.Busy,Time.timeScale<=0||releaseInputAt>0)) return;
        var keyboard=UnityEngine.InputSystem.Keyboard.current;
        if(keyboard==null) return;
        foreach(var binding in actionKeys)
        {
            if(binding.Key=="normalSpeed" || binding.Key=="exitPhoto" || binding.Value=="None" || binding.Value==Plugin.Hotkey.Value.ToString() || binding.Value=="F8" || !Enum.TryParse<UnityEngine.InputSystem.Key>(binding.Value,out var key) || !keyboard[key].wasPressedThisFrame) continue;
            Do(MenuText.Get(ActionLabels[Array.IndexOf(ActionKeyPolicy.Actions,binding.Key)]),()=>RunUtilityAction(binding.Key));
            break;
        }
    }
    [HideFromIl2Cpp]
    private string RunUtilityAction(string action)
    {
        switch(action)
        {
            case "heal": actions.Heal(); return MenuText.Get("生命已回满");
            case "energy": actions.RefillEnergy(); return MenuText.Get("能量已补满");
            case "clearMobs": return training.ClearAll();
            case "normalSpeed": return training.NormalSpeed();
            case "exitPhoto": return photo.Restore();
            case "timer": return TogglePractice();
            case "spawnOneHp": case "spawnFull":
                return AdmitTraining(()=>{
                if(training.Busy)throw new InvalidOperationException(MenuText.Get("已有训练请求正在执行"));
                if(selectedMob?.Data.Supported!=true || !FilteredMobs(fields.TryGetValue("mobSearch",out var query)?query:"").Contains(selectedMob))
                    throw new InvalidOperationException(MenuText.Get("请先在怪物页选择可召唤目标"));
                mobs.Request(new MobSpawnOptions(action=="spawnOneHp"?MobSpawnHealthMode.OneHp:MobSpawnHealthMode.NativeFull,mobAiEnabled,selectedMob.InternalName));
                return mobs.Status;});
            default: throw new InvalidOperationException(MenuText.Get("未知快捷操作"));
        }
    }
    [HideFromIl2Cpp]
    private void CaptureUtilityKey(Event e)
    {
        if(e.keyCode==KeyCode.Escape) { actionKeyCapture=null; e.Use(); return; }
        var action=actionKeyCapture;
        Do(MenuText.Get("设置快捷键"),()=>{
            utilities.SetBinding(action,e.keyCode.ToString(),Plugin.Hotkey.Value.ToString());
            actionKeys=utilities.Data.Bindings; actionKeyCapture=null;
            return MenuText.Get("快捷键已设为 ")+e.keyCode;
        });
        e.Use();
    }
    [HideFromIl2Cpp]
    private void SetMenuKey(KeyCode key)
    {
        if(actionKeys.Values.Contains(key.ToString())) { message=MenuText.Get("该按键已绑定独立操作，请先取消绑定"); messageError=true; return; }
        Plugin.Hotkey.Value=key; hotkeyCapture=false; message=MenuText.Get("快捷键已设为 ")+key;
    }
    [HideFromIl2Cpp]
    private void FavoriteButton(string id,string kind,string target,float a,float b,float w)
    {
        var saved=utilities.IsFavorite(kind,target);
        if(Button(id,a,b,w,saved?"★":"☆",!utilities.ReadOnly,saved,18))
            Do(MenuText.Get("收藏"),()=>{utilities.ToggleFavorite(kind,target); return MenuText.Get(saved?"已取消收藏":"已收藏");});
    }
    [HideFromIl2Cpp]
    private List<MobBrowser.Entry> FilteredMobs(string query)
        => mobs.Filter(mobBossSection,query).Where(e=>!mobFavoritesOnly || utilities.IsFavorite("mob",e.InternalName)).ToList();

    [HideFromIl2Cpp]
    private void DrawPresets()
    {
        Text(28,140,860,32,MenuText.Get("武器 / 工具预设"),24,true);
        Text(28,179,100,36,MenuText.Get("预设名称"),16);
        var name=Field("presetName",133,179,395,"");
        if(Button("presetSave",546,179,349,MenuText.Get("保存当前词缀草稿"),!utilities.ReadOnly && game.Ready && affixes.HasTarget && !affixes.RecoveryPending))
            Do(MenuText.Get("保存预设"),()=>{var preset=affixes.CapturePreset(name);utilities.AddPreset(preset);selectedPresetName=preset.Name;return MenuText.Get("预设已保存");});
        if(Button("presetImport",28,227,255,MenuText.Get("从剪贴板导入"),!utilities.ReadOnly))
            Do(MenuText.Get("导入预设"),()=>{var preset=WeaponPreset.Parse(GUIUtility.systemCopyBuffer);utilities.AddPreset(preset);selectedPresetName=preset.Name;fields["presetName"]=preset.Name;presetPage=(utilities.Data.Presets.Count-1)/6;return MenuText.Get("已导入；请载入或生成进行兼容校验");});
        Text(305,226,589,40,MenuText.Get("先在词缀页载入武器并选词缀，再保存为预设。"),15,false,true);
        var all=utilities.Data.Presets;
        var pages=Math.Max(1,(all.Count+5)/6); presetPage=Math.Clamp(presetPage,0,pages-1);
        var chosen=all.FirstOrDefault(p=>p.Name==selectedPresetName);
        for(int i=0;i<6 && presetPage*6+i<all.Count;i++)
        {
            var preset=all[presetPage*6+i];
            if(Button("presetRow"+i,28,287+i*42,500,preset.Name,true,chosen?.Name==preset.Name)) { selectedPresetName=preset.Name;fields["presetName"]=preset.Name; }
        }
        if(all.Count==0) Text(28,296,500,85,MenuText.Get("暂无预设；保存草稿或从剪贴板导入 JSON。"),18,false,true);
        Box(552,287,344,294,new Color(.055f,.09f,.14f));
        Text(566,291,316,52,chosen?.Name??MenuText.Get("请选择预设"),19,true,true);
        Text(566,346,316,49,chosen==null?"":MenuText.Format("等级 {0} · 词缀 {1} 个",chosen.Tier,chosen.AffixIds.Count),16,false,true);
        var ready=game.Ready&&!levels.Loading&&!affixes.RecoveryPending&&chosen!=null;
        if(Button("presetLoad",566,405,316,MenuText.Get("载入词缀草稿"),ready))
            Do(MenuText.Get("载入预设"),()=>{var result=affixes.LoadPreset(chosen);affixPage=0;fields["affixSearch"]="";tab=6;return result;});
        if(Button("presetGenerate",566,449,316,MenuText.Get("生成预设武器"),ready))
            Do(MenuText.Get("生成预设"),()=>{affixes.LoadPreset(chosen);return affixes.Spawn();});
        if(Button("presetExport",566,493,152,MenuText.Get("复制 JSON"),chosen!=null))
            Do(MenuText.Get("导出预设"),()=>{GUIUtility.systemCopyBuffer=chosen.Export();return MenuText.Get("预设 JSON 已复制");});
        if(Button("presetDelete",730,493,152,MenuText.Get("删除预设"),chosen!=null&&!utilities.ReadOnly))
            Do(MenuText.Get("删除预设"),()=>{utilities.RemovePreset(chosen.Name);selectedPresetName=null;return MenuText.Get("预设已删除");});
        if(Button("presetRename",566,537,316,MenuText.Get("按输入名称重命名"),chosen!=null&&!utilities.ReadOnly))
            Do(MenuText.Get("重命名预设"),()=>{utilities.Edit(data=>{var p=data.Presets.Single(p=>p.Name==chosen.Name);p.Name=WeaponPreset.ValidName(name);});selectedPresetName=name.Trim();return MenuText.Get("预设已重命名");});
        if(Button("presetPrev",28,548,110,MenuText.Get("上一页"),presetPage>0))presetPage--;
        Text(156,548,225,36,MenuText.Format("第 {0} / {1} 页 · {2} 项",presetPage+1,pages,all.Count),16);
        if(Button("presetNext",413,548,115,MenuText.Get("下一页"),presetPage+1<pages))presetPage++;
        Text(28,584,866,19,MenuText.Get("预设不会自动装备；失效物品和冲突词缀会拒绝生成。"),13);
    }
    [HideFromIl2Cpp]
    private void DrawUtilities()
    {
        var captions=new[]{"信息与日志","操作快捷键","练习计时","训练与书签","自有怪物管理","截图与效果","伤害与生存","效果管理","完整配装","移动与工具","波次与练习","奖励容器","Boss 场地练习","跨地图书签","新局种子与等级","场景显示","房间敌人控制","镜头与飞行","后续遭遇","Boss 阶段","场景状态报告","伤害来源","工具次数与投射物","Boss 目标行为过滤","本局技能试装","角色练习检查点"};
        if(Button("utilityBack",28,140,40,"<",utilityPage>0)){utilityPage--;utilitySection=utilityPage*6;focus="";actionKeyCapture=null;}
        if(Button("utilityForward",854,140,40,">",utilityPage<4)){utilityPage++;utilitySection=utilityPage*6;focus="";actionKeyCapture=null;}
        for(int i=0;i<6 && utilityPage*6+i<captions.Length;i++) {var section=utilityPage*6+i;if(Button("utilSection"+section,82+(i%3)*256,140+(i/3)*44,242,MenuText.Get(captions[section]),true,utilitySection==section)) {utilitySection=section; actionKeyCapture=null;focus="";}}
        
        switch(utilitySection){case 1:DrawActionKeys();break;case 2:DrawPractice();break;case 3:DrawTraining();break;case 4:DrawOwned();break;case 5:DrawObserve();break;case 6:DrawCombat();break;case 7:DrawEffectEditor();break;case 8:DrawLoadouts();break;case 9:DrawMotionTools();break;case 10:DrawWavesPractice();break;case 11:DrawRewards();break;case 12:DrawBossPractice();break;case 13:DrawCrossMap();break;case 14:DrawRunSettings();break;case 15:DrawSceneDisplay();break;case 16:DrawSceneEnemies();break;case 17:DrawCameraStep();break;case 18:DrawNextEncounter();break;case 19:DrawBossPhases();break;case 20:DrawWorldReport();break;case 21:DrawDamageStatistics();break;case 22:DrawToolLimits();break;case 23:DrawBossTargets();break;case 24:DrawAbilities();break;case 25:DrawPlayerCheckpoint();break;default:DrawUtilityInfo();break;}
    }
    [HideFromIl2Cpp]
    private void DrawActionKeys()
    {
        Text(28,228,862,30,MenuText.Get("仅在菜单关闭、游戏就绪且窗口获得焦点时触发。F8 和菜单键保留。"),16,false,true);
        for(int i=0;i<ActionKeyPolicy.Actions.Length;i++)
        {
            var id=ActionKeyPolicy.Actions[i]; var row=266+i*40;
            Text(28,row,453,36,MenuText.Get(ActionLabels[i]),17);
            var key=actionKeys.TryGetValue(id,out var value)?value:"None";
            if(Button("bindKey"+id,490,row,248,actionKeyCapture==id?MenuText.Get("请按 F1–F12 / Esc 取消"):key=="None"?MenuText.Get("未绑定"):key,!utilities.ReadOnly,actionKeyCapture==id,15)) {actionKeyCapture=id;hotkeyCapture=false;focus="";}
            if(Button("unbindKey"+id,754,row,141,MenuText.Get("取消绑定"),key!="None"&&!utilities.ReadOnly))
                Do(MenuText.Get("取消绑定"),()=>{utilities.SetBinding(id,"None",Plugin.Hotkey.Value.ToString());actionKeys=utilities.Data.Bindings;actionKeyCapture=null;return MenuText.Get("快捷键已禁用");});
        }
        Text(28,586,866,18,MenuText.Get("怪物快捷键使用清单中选中的目标和 AI 设置；长按不会重复召唤。"),12,false,true);
    }
    [HideFromIl2Cpp]
    private string TogglePractice()
    {
        if(practice.Running)
        {
            var run=practice.Stop();
            if(run==null) return MenuText.Get("计时已停止，没有有效练习时间");
            var result=MenuText.Format("计时停止：{0} · {1}",run.Name,FormatDuration(run.Seconds));
            try { utilities.RecordBest(run.Name,run.Seconds); }
            catch(Exception ex) { throw new InvalidOperationException(result+" · "+MenuText.Get("最佳记录未保存")+": "+ex.Message,ex); }
            return result;
        }
        practice.Start(fields.TryGetValue("practiceName",out var name)?name:MenuText.Get("默认练习")); practiceWasActive=false;
        return MenuText.Get("计时已开始；关闭菜单后累计有效时间");
    }
    [HideFromIl2Cpp]
    private static string FormatDuration(double seconds) => TimeSpan.FromSeconds(seconds).ToString(@"d\.hh\:mm\:ss\.fff");
    [HideFromIl2Cpp]
    private void DrawPractice()
    {
        Text(28,231,130,36,MenuText.Get("练习名称"),17);
        var name=Field("practiceName",174,231,720,MenuText.Get("默认练习"));
        Text(28,284,865,36,MenuText.Get(practice.Running?(PracticeActive()?"计时中":"计时暂停"):"计时未运行")+" · "+(practice.Running?practice.Name:name),20,true);
        Text(28,329,865,56,FormatDuration(practice.Elapsed),36,true);
        var best=utilities.Data.BestTimes;
        var lookup=practice.Running?practice.Name:name.Trim();
        Text(28,401,865,36,MenuText.Get("最佳有效时间")+"："+(best.TryGetValue(lookup,out var seconds)?FormatDuration(seconds):"—"),22);
        if(Button("practiceToggle",28,467,415,MenuText.Get(practice.Running?"停止并记录成绩":"开始计时"),practice.Running || game.Ready)) Do(MenuText.Get("练习计时"),TogglePractice);
        if(Button("practiceReset",465,467,429,MenuText.Get("重置（不记录成绩）"))) Do(MenuText.Get("重置计时"),()=>{practice.Reset();return MenuText.Get("计时已重置");});
        Text(28,520,865,77,MenuText.Get("菜单、失焦、暂停、加载或死亡时不计时。手动停止后，同名练习仅保存更快成绩；名称在开始时固定。"),17,false,true);
    }
    [HideFromIl2Cpp]
    private int? ReadCurrentSeed()
    {
        if(ReadUtilityLoading()) return null;
        var service=RuntimeServiceLocator.Get<ApplicationFlowService>();
        return service?.IsRunActive==true ? service.GetSeed() : null;
    }
    [HideFromIl2Cpp]
    private void DrawUtilityInfo()
    {
        if(utilities.ReadOnly){Text(28,229,866,36,MenuText.Get("配置读取失败；保留原文件，当前禁止保存")+": "+utilities.Error,14,false,true);}
        var seed=ReadCurrentSeed();
        if(!utilities.ReadOnly) Text(28,229,575,36,MenuText.Get("当前种子")+"："+(seed?.ToString()??MenuText.Get("未就绪")),20,true);
        if(Button("seedCopy",636,229,258,MenuText.Get("复制当前种子"),seed.HasValue&&!utilities.ReadOnly)) Do(MenuText.Get("复制种子"),()=>{var current=ReadCurrentSeed()??throw new InvalidOperationException(MenuText.Get("当前种子未就绪"));GUIUtility.systemCopyBuffer=current.ToString();return MenuText.Get("种子已复制");});
        var position=game.Ready?game.Player.transform.position.ToString():"—";
        Text(28,273,866,39,MenuText.Format("版本 {0} · 关卡 {1} · 位置 {2}",Plugin.Version,levels.CurrentName,position),16,false,true);
        Text(28,320,866,25,MenuText.Format("自有怪物 {0} · 词缀恢复 {1} · 营救预约 {2}",mobs.Count,MenuText.Get(affixes.RecoveryPending?"等待":"无"),MenuText.Get(rescue.Pending?"等待":"无")),15);
        var log=Plugin.RecentLog.Snapshot().Reverse().ToArray();
        var pages=Math.Max(1,(log.Length+4)/5); logPage=Math.Clamp(logPage,0,pages-1);
        Text(28,355,866,25,MenuText.Get("最近菜单日志（新到旧）"),17,true);
        for(int i=0;i<5&&logPage*5+i<log.Length;i++) Text(28,388+i*32,866,29,log[logPage*5+i],13,false,true);
        if(Button("logPrev",28,555,118,MenuText.Get("上一页"),logPage>0))logPage--;
        if(Button("logNext",158,555,118,MenuText.Get("下一页"),logPage+1<pages))logPage++;
        if(Button("logCopy",636,555,258,MenuText.Get("复制最近日志"),log.Length>0)) Do(MenuText.Get("复制日志"),()=>{GUIUtility.systemCopyBuffer=string.Join(Environment.NewLine,Plugin.RecentLog.Snapshot());return MenuText.Get("最近日志已复制");});
        Text(297,555,318,36,MenuText.Format("第 {0} / {1} 页 · {2} 项",logPage+1,pages,log.Length),15);
    }
}

