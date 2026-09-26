// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
// Additional game linking permission: see GAME-LINKING-EXCEPTION.txt.

using Il2CppInterop.Runtime.Attributes;
using RoguePrince.CheatMenu.Core;
using UnityEngine;

namespace RoguePrince.CheatMenu;

public sealed class MenuController : MonoBehaviour
{
    internal static MenuController Instance;
    private GameSession game;
    private GameplayActions actions;
    private ResourceActions resources;
    private MedallionSlotActions medallions;
    private ItemBrowser items;
    private readonly InputLease inputLease = new();
    private readonly Dictionary<string, string> fields = new();
    private Font font;
    private GUIStyle textStyle;
    private GUIStyle panelStyle;
    private bool visible;
    private bool collapsed;
    private bool stopping;
    private bool wasReady;
    private bool applicationQuitting;
    private bool firstPaint = true;
    private bool fillWithMaximum = true;
    private bool dragging;
    private bool replaceText;
    private bool hotkeyCapture;
    private float nextRefresh;
    private float nextTick;
    private float releaseInputAt;
    private float x;
    private float y;
    private float scale;
    private Vector2 dragOffset;
    private int tab;
    private int itemPage;
    private int category;
    private string focus = "";
    private string pressed = "";
    private string message = "";
    private bool messageError;
    private ItemBrowser.Entry selected;
    private static readonly string[] Tabs = { "角色", "资源", "物品", "风神之息", "设置/setting" };
    private static readonly string[] Categories = ItemCategoryPolicy.Categories;
    private static readonly Color Accent = new(0.27f, 0.94f, 0.77f);

    public MenuController(IntPtr pointer) : base(pointer) { }

    public void Awake()
    {
        Instance = this;
        game = new GameSession();
        actions = new GameplayActions(game);
        resources = new ResourceActions(game);
        medallions = new MedallionSlotActions(game, resources.Backup);
        items = new ItemBrowser(game);
        x = Plugin.PositionX.Value;
        y = Plugin.PositionY.Value;
        message = MenuText.Format("所有作弊开关默认关闭；{0} 打开 / 关闭", Plugin.Hotkey.Value);
    }

    public void Update()
    {
        if (stopping || game == null) return;
        try
        {
            var now = Time.realtimeSinceStartup;
            if (now >= nextRefresh && (visible || actions.GodMode || actions.InfiniteEnergy || actions.HoldWind || actions.HasAdjustments))
            {
                nextRefresh = now + 0.5f;
                if (game.Refresh(actions.Reset))
                {
                    selected = null;
                    items.Items.Clear();
                }
                resources.BindReroll();
                if (visible) medallions.Refresh();
            }
            var ready = game.Ready;
            if (wasReady && !ready)
            {
                visible = false;
                focus = "";
                hotkeyCapture = false;
                inputLease.Release();
                actions.Reset();
            }
            wasReady = ready;
            if (visible) inputLease.Acquire(game);
            else if (releaseInputAt > 0 && now >= releaseInputAt) { inputLease.Release(); releaseInputAt = 0; }
            if (now >= nextTick) { nextTick = now + 0.15f; actions.Tick(); }
        }
        catch (Exception e)
        {
            actions.StopContinuous();
            visible = false;
            try { actions.Reset(); } catch (Exception cleanup) { Plugin.MenuLog.LogWarning(MenuText.Get("角色清理将在下次绑定时重试：") + cleanup.Message); }
            try { inputLease.Release(); } catch (Exception cleanup) { Plugin.MenuLog.LogWarning(MenuText.Get("输入清理：") + cleanup.Message); }
            ReportError(MenuText.Get("状态更新停止"), e);
            nextRefresh = Time.realtimeSinceStartup + 3;
        }
    }

    public void OnGUI()
    {
        if (stopping || game == null) return;
        var e = Event.current;
        if (e == null) return;
        var oldColor = GUI.color;
        var oldDepth = GUI.depth;
        try
        {
            if (e.type == EventType.KeyDown)
            {
                if (hotkeyCapture)
                {
                    if (e.keyCode == KeyCode.Escape) hotkeyCapture = false;
                    else if (e.keyCode >= KeyCode.F1 && e.keyCode <= KeyCode.F12 && e.keyCode != KeyCode.F8)
                    { Plugin.Hotkey.Value = e.keyCode; hotkeyCapture = false; message = MenuText.Get("快捷键已设为 ") + e.keyCode; }
                    e.Use();
                }
                else if (e.keyCode == Plugin.Hotkey.Value) { Toggle(); e.Use(); }
                else if (visible && e.keyCode == KeyCode.Escape) { if (focus.Length > 0) focus = ""; else Toggle(); e.Use(); }
                else if (visible) EditText(e);
            }
            EnsureStyle();
            GUI.depth = -11000;
            scale = Math.Max(0.45f, Math.Min(Plugin.Scale.Value * Screen.height / 1080f, Math.Min((Screen.width - 20) / 930f, (Screen.height - 20) / 670f)));
            if (!visible)
            {
                if (e.type == EventType.Repaint)
                {
                    textStyle.fontSize = 16;
                    textStyle.normal.textColor = Accent;
                    GUI.Label(new Rect(Math.Max(5, Screen.width - 310), 12, 300, 28), MenuText.Format("{0}  调试菜单  {1}", Plugin.Hotkey.Value, Plugin.Version), textStyle);
                }
                return;
            }
            x = Math.Clamp(x, 5, Math.Max(5, Screen.width - 930 * scale - 5));
            y = Math.Clamp(y, 5, Math.Max(5, Screen.height - (collapsed ? 65 : 670) * scale - 5));
            DrawMenu();
            HandleDrag(e);
            if (e.type == EventType.MouseUp) pressed = "";
            if (firstPaint && e.type == EventType.Repaint) { firstPaint = false; Plugin.LogEvent("menu_first_repaint"); }
        }
        catch (Exception ex)
        {
            visible = false;
            try { inputLease.Release(); } catch (Exception cleanup) { Plugin.MenuLog.LogWarning(MenuText.Get("输入清理：") + cleanup.Message); }
            ReportError(MenuText.Get("菜单绘制失败"), ex);
        }
        finally { GUI.color = oldColor; GUI.depth = oldDepth; }
    }

    [HideFromIl2Cpp]
    private void Toggle()
    {
        visible = !visible;
        focus = "";
        pressed = "";
        hotkeyCapture = false;
        if (visible) { nextRefresh = 0; inputLease.Acquire(game); }
        else { releaseInputAt = Time.realtimeSinceStartup + 0.15f; SavePosition(); }
        Plugin.LogEvent("menu_visible=" + visible);
    }

    [HideFromIl2Cpp]
    private void EnsureStyle()
    {
        if (textStyle != null) return;
        font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 22);
        UnityEngine.Object.DontDestroyOnLoad(font);
        textStyle = new GUIStyle { font = font, richText = false, clipping = TextClipping.Clip, alignment = TextAnchor.MiddleLeft };
        panelStyle = new GUIStyle();
        panelStyle.normal.background = Texture2D.whiteTexture;
    }

    [HideFromIl2Cpp] private Rect R(float a, float b, float w, float h) => new(x+a*scale,y+b*scale,w*scale,h*scale);
    [HideFromIl2Cpp]
    private void Box(float a, float b, float w, float h, Color color)
    {
        if (Event.current.type != EventType.Repaint) return;
        GUI.color = color;
        GUI.Label(R(a,b,w,h), "", panelStyle);
        GUI.color = Color.white;
    }
    [HideFromIl2Cpp]
    private void Text(float a, float b, float w, float h, string text, int size=18, bool accent=false, bool wrap=false)
    {
        if (Event.current.type != EventType.Repaint) return;
        // Scale the font with its rectangle, including compact windows.
        textStyle.fontSize = Math.Max(6,(int)(size*scale));
        textStyle.normal.textColor = accent ? Accent : new Color(0.86f,0.90f,0.95f);
        textStyle.fontStyle = size >= 25 ? FontStyle.Bold : FontStyle.Normal;
        textStyle.wordWrap = wrap;
        GUI.color = Color.white;
        GUI.Label(R(a,b,w,h), text ?? "", textStyle);
    }
    [HideFromIl2Cpp]
    private bool Button(string id, float a, float b, float w, string caption, bool enabled=true, bool selectedButton=false)
    {
        var rect=R(a,b,w,36);
        var hover=rect.Contains(Event.current.mousePosition);
        var color=!enabled?new Color(0.11f,0.14f,0.19f):selectedButton?new Color(0.12f,0.38f,0.33f):hover?new Color(0.23f,0.30f,0.38f):new Color(0.15f,0.20f,0.28f);
        Box(a,b,w,36,color);
        Text(a+10,b+1,w-18,34,caption,17,selectedButton);
        var e=Event.current;
        if (enabled && hover && e.type==EventType.MouseDown && e.button==0) { pressed=id;focus="";e.Use(); }
        if (enabled && hover && e.type==EventType.MouseUp && e.button==0 && pressed==id) { pressed="";e.Use();return true; }
        return false;
    }
    [HideFromIl2Cpp]
    private string Field(string id, float a, float b, float w, string initial)
    {
        if (!fields.ContainsKey(id)) fields[id]=initial;
        Box(a,b,w,36,focus==id?new Color(0.08f,0.26f,0.26f):new Color(0.06f,0.10f,0.16f));
        Text(a+9,b+1,w-17,34,fields[id]+(focus==id?" |":""),17,focus==id);
        var e=Event.current;
        if (e.type==EventType.MouseDown && R(a,b,w,36).Contains(e.mousePosition)) { focus=id;replaceText=true;e.Use(); }
        return fields[id];
    }
    [HideFromIl2Cpp]
    private void EditText(Event e)
    {
        if (focus.Length==0 || !fields.ContainsKey(focus)) return;
        var value=fields[focus];
        if (e.control && e.keyCode==KeyCode.A) { replaceText=true; e.Use(); return; }
        if (e.control && e.keyCode==KeyCode.V)
        {
            var paste=GUIUtility.systemCopyBuffer ?? "";
            paste=new string(paste.Where(c=>!char.IsControl(c)).ToArray());
            value=(replaceText?"":value)+paste;
            replaceText=false;
        }
        else if(e.keyCode==KeyCode.Backspace) { value=replaceText?"":value.Length>0?value[..^1]:"";replaceText=false; }
        else if(e.keyCode==KeyCode.Return || e.keyCode==KeyCode.KeypadEnter) { focus="";e.Use();return; }
        else if (!char.IsControl(e.character) && e.character!='\0') {value=(replaceText?"":value)+e.character;replaceText=false;}
        else return;
        var max=focus=="search"?100:12;
        fields[focus]=value.Length>max?value[..max]:value;
        if(focus=="search") itemPage=0;
        e.Use();
    }
    [HideFromIl2Cpp]
    private void DrawMenu()
    {
        Box(0,0,930,collapsed?65:670,new Color(0.025f,0.04f,0.075f,0.98f));
        Box(0,0,5,collapsed?65:670,Accent);
        Text(22,10,680,38,MenuText.Get("波斯王子 · 调试菜单"),26,true);
        if(Button("collapse",710,13,102,collapsed?MenuText.Get("展开"):MenuText.Get("折叠"))) collapsed=!collapsed;
        if(Button("close",822,13,88,MenuText.Get("关闭菜单"))) Toggle();
        if(collapsed) return;
        Text(24,51,820,25,game.Status+MenuText.Get("    ·    菜单打开时暂停并阻止角色输入"),15);
        for(int i=0;i<Tabs.Length;i++) if(Button("tab"+i,24+i*177,88,169,MenuText.Get(Tabs[i]),true,tab==i)){tab=i;focus="";}
        switch(tab) {case 0:DrawPlayer();break;case 1:DrawResources();break;case 2:DrawItems();break;case 3:DrawWind();break;case 4:DrawSettings();break;}
        Box(22,606,886,48,messageError?new Color(0.29f,0.09f,0.10f):new Color(0.07f,0.16f,0.19f));
        Text(34,610,862,39,message,16,!messageError,true);
    }
    [HideFromIl2Cpp]
    private void DrawPlayer()
    {
        var ready=game.Ready;
        Text(28,143,690,33,ready?MenuText.Format("生命  {0:0.#} / {1:0.#}", game.Health.currentHealth, game.Health.maxHealth):MenuText.Get("生命  —"),24,true);
        if(Button("heal",710,143,186,MenuText.Get("回满生命"),ready)) Do(MenuText.Get("回满生命"),()=>{actions.Heal();return MenuText.Format("生命：{0:0.#} / {1:0.#}", game.Health.currentHealth, game.Health.maxHealth);});
        Text(28,191,300,36,MenuText.Get("基础生命上限  (1–10000)"));
        var hp=Field("hp",345,191,160,"300");
        if(Button("hpSet",530,191,150,MenuText.Get("应用生命上限"),ready)) SetMax(true,hp);
        Text(28,252,680,33,ready&&game.Energy!=null?MenuText.Format("能量  {0} / {1}", game.Energy.energyCurrentValueStat, game.Energy.energyMaxValueStat):MenuText.Get("能量  —"),24,true);
        if(Button("energyFill",710,252,186,MenuText.Get("补满能量"),ready&&game.Energy!=null)) Do(MenuText.Get("补满能量"),()=>{actions.RefillEnergy();return MenuText.Format("能量：{0} / {1}", game.Energy.energyCurrentValueStat, game.Energy.energyMaxValueStat);});
        Text(28,300,300,36,MenuText.Get("基础能量上限  (1–10000)"));
        var energy=Field("energy",345,300,160,"200");
        if(Button("energySet",530,300,150,MenuText.Get("应用能量上限"),ready&&game.Energy!=null)) SetMax(false,energy);
        if(Button("fill",28,360,270,(fillWithMaximum?"☑":"□")+MenuText.Get(" 修改上限时同时补满"),true,fillWithMaximum))fillWithMaximum=!fillWithMaximum;
        if(Button("restore",322,360,240,MenuText.Get("撤销生命/能量上限调整"),ready&&actions.HasAdjustments))Do(MenuText.Get("恢复上限"),()=>{actions.RestoreMaximums();return MenuText.Get("已撤销本菜单的生命/能量基础上限增减");});
        if(Button("god",28,430,260,MenuText.Get("无敌  ")+(actions.GodMode?MenuText.Get("开启"):MenuText.Get("关闭")),ready,actions.GodMode))Do(MenuText.Get("切换无敌"),()=>{actions.SetGodMode(!actions.GodMode);return MenuText.Get("无敌：")+(actions.GodMode?MenuText.Get("开启"):MenuText.Get("关闭"));});
        if(Button("infinite",310,430,260,MenuText.Get("无限能量  ")+(actions.InfiniteEnergy?MenuText.Get("开启"):MenuText.Get("关闭")),ready&&GameplayActions.EnergyPatchAvailable,actions.InfiniteEnergy))Do(MenuText.Get("切换无限能量"),()=>{actions.SetInfiniteEnergy(!actions.InfiniteEnergy);return MenuText.Get("无限能量：")+(actions.InfiniteEnergy?MenuText.Get("开启"):MenuText.Get("关闭"));});
        var slotsReady=ready&&medallions.Available;
        Text(28,479,860,35,medallions.Status,20,slotsReady);
        if(Button("slotAdd",28,521,210,MenuText.Get("加一个"),slotsReady&&medallions.Current<medallions.Maximum))Do(MenuText.Get("增加徽章栏位"),()=>medallions.Change(SlotChange.Add));
        if(Button("slotRemove",258,521,210,MenuText.Get("减一个"),slotsReady&&medallions.Current>0))Do(MenuText.Get("减少徽章栏位"),()=>medallions.Change(SlotChange.Remove));
        if(Button("slotUnlockAll",488,521,285,MenuText.Get("解锁到上限"),slotsReady&&medallions.Current<medallions.Maximum))Do(MenuText.Get("解锁徽章栏位"),()=>medallions.Change(SlotChange.UnlockAll));
        Text(28,567,870,29,MenuText.Get("栏位修改前自动备份；减少后须容纳已装备徽章，修改可能随游戏保存。"),16);
    }
    [HideFromIl2Cpp]
    private void SetMax(bool hp,string text)
    {
        Do(MenuText.Get("修改上限"),()=>{if(!NumericPolicy.TryTarget(text,0,false,10000,out var value,out _)||value<1)throw new ArgumentException(MenuText.Get("上限请输入 1–10000 的整数"));actions.SetMaximum(hp,value,fillWithMaximum);return hp?MenuText.Format("实际生命上限：{0:0.#}", game.Health.maxHealth):MenuText.Format("实际能量上限：{0}", game.Energy.energyMaxValueStat);});
    }
    [HideFromIl2Cpp]
    private void DrawResources()
    {
        Text(28,137,850,32,MenuText.Get("永久资源修改前自动备份存档；增加与设为是两个独立操作。"),17);
        for(int i=0;i<ResourceActions.Names.Length;i++)
        {
            var kind=i;var row=184+i*55;var available=resources.Available(kind);
            Text(28,row,278,36,MenuText.Get(ResourceActions.Names[i])+(resources.IsPersistent(i)?MenuText.Get(" [永久]"):MenuText.Get(" [本局]")),16);
            Text(315,row,130,36,available?resources.Read(i).ToString():MenuText.Get("未绑定"),19,true);
            var value=Field("resource"+i,445,row,132,i==5?"1":"100");
            if(Button("add"+i,596,row,130,MenuText.Get("增加"),available))Do(MenuText.Get("增加资源"),()=>resources.Change(kind,value,true));
            if(Button("set"+i,743,row,150,MenuText.Get("设为"),available))Do(MenuText.Get("设置资源"),()=>resources.Change(kind,value,false));
        }
        if(Button("transfer",28,533,300,MenuText.Get("将本局灵魂灰烬存入绿洲"),game.Ready&&game.Meta!=null))Do(MenuText.Get("转存灰烬"),resources.TransferShards);
        if(Button("resourcesReload",351,533,210,MenuText.Get("重新绑定重选硬币"),game.Ready))Do(MenuText.Get("绑定重选硬币"),()=>{resources.BindReroll();return resources.RerollBindingStatus+(resources.Available(4)?MenuText.Format("，当前 {0} 枚", resources.Read(4)):MenuText.Get("；菜单会自动重试"));});
        Text(585,532,310,42,MenuText.Get("备份：BepInEx/CheatMenuBackups"),14);
    }
    [HideFromIl2Cpp]
    private void DrawItems()
    {
        Text(28,139,65,36,MenuText.Get("搜索"));
        var query=Field("search",94,139,430,"");
        if(Button("reloadItems",546,139,172,MenuText.Get("刷新物品库"),game.Ready))Do(MenuText.Get("刷新物品库"),()=>{items.Reload();itemPage=0;selected=null;return items.State;});
        if(Button("clearSearch",737,139,159,MenuText.Get("清空搜索"))){fields["search"]="";query="";itemPage=0;}
        for(int i=0;i<Categories.Length;i++)
            if(Button("cat"+i,28+(i%7)*125,186+(i/7)*40,116,MenuText.Get(Categories[i]),true,category==i)){category=i;itemPage=0;selected=null;}
        var result=items.Filter(query,Categories[category]);
        if(selected!=null&&!result.Contains(selected))selected=null;
        const int pageSize=6;
        var pages=Math.Max(1,(result.Count+pageSize-1)/pageSize);
        itemPage=Math.Clamp(itemPage,0,pages-1);
        for(int i=0;i<pageSize && itemPage*pageSize+i<result.Count;i++)
        {
            var entry=result[itemPage*pageSize+i];var row=275+i*43;
            if(Button("item"+i,28,row,563,entry.Name+"  ·  "+MenuText.Get(entry.Category),true,selected==entry))selected=entry;
        }
        if(result.Count==0)Text(28,285,563,36,MenuText.Get("没有符合当前分类和搜索条件的物品"),17);
        Box(612,275,284,268,new Color(0.055f,0.09f,0.14f));
        Text(625,276,255,46,selected?.Name??MenuText.Get("请选择物品"),17,true,true);
        Text(625,322,255,20,selected?.InternalName??MenuText.Get("支持名称与资源名搜索"),13);
        Text(625,343,255,26,selected==null?"":selected.HasTier?MenuText.Get("原版系统等级：1–5 级"):MenuText.Get("此物品不使用等级"),16);
        if(selected?.HasTier==true)
        {
            Text(625,368,255,43,MenuText.Get("高于 5 级虽可生成，\n但不保证任何正常游玩体验。"),14,false,true);
            Text(625,414,255,20,MenuText.Get("0 级不存在，无法使用。"),14);
        }
        Text(625,437,70,32,MenuText.Get("等级"));
        var tier=Field("tier",697,435,178,"1");
        if(Button("spawn",630,479,243,MenuText.Get("在角色附近生成"),game.Ready&&selected!=null))Do(MenuText.Get("生成物品"),()=>{var v=1;if(selected.HasTier&&(!NumericPolicy.TryTarget(tier,0,false,100,out v,out _)||v<1))throw new ArgumentException(MenuText.Get("等级请输入 1–100；0 级不存在，无法使用（原版仅 1–5 级）"));return items.Spawn(selected,v);});
        Text(630,519,249,22,MenuText.Get("生成不会自动永久解锁。"),14);
        if(Button("prev",28,548,100,MenuText.Get("上一页"),itemPage>0))itemPage--;
        Text(145,548,320,36,MenuText.Format("第 {0} / {1} 页 · {2} 项", itemPage+1, pages, result.Count),17);
        if(Button("next",490,548,101,MenuText.Get("下一页"),itemPage+1<pages))itemPage++;
        Text(615,548,290,36,MenuText.Get("按物品类型分类；其他保留特殊条目。"),14,false,true);
    }
    [HideFromIl2Cpp]
    private void DrawWind()
    {
        var ready=game.Ready&&game.Wind!=null&&game.Affects!=null&&game.Wind.config!=null;
        var active=ready&&game.Wind.config.affectData!=null&&game.Affects.HasAffect(game.Wind.config.affectData);
        Text(30,154,850,42,MenuText.Get("风神之息  ·  ")+(active?MenuText.Get("已激活"):ready?MenuText.Get("未激活"):MenuText.Get("等待角色")),27,true);
        var ratio=ready?Math.Clamp(game.Wind.GetGaugeRatio(),0,1):0;
        Text(30,219,850,36,MenuText.Format("蓄积进度  {0:0}%", ratio*100),22);
        Box(30,275,865,20,new Color(0.12f,0.17f,0.22f));
        if(ratio>0)Box(30,275,865*ratio,20,Accent);
        if(Button("windOnce",30,335,300,MenuText.Get("立即触发一次"),ready))Do(MenuText.Get("触发风神之息"),()=>{actions.TriggerWind();return MenuText.Get("风神之息已激活");});
        if(Button("windHold",354,335,340,MenuText.Get("持续保持  ")+(actions.HoldWind?MenuText.Get("开启"):MenuText.Get("关闭")),ready,actions.HoldWind))Do(MenuText.Get("保持风神之息"),()=>{actions.SetHoldWind(!actions.HoldWind);return actions.HoldWind?MenuText.Get("风神之息保持开启"):MenuText.Get("已停止保持；现有状态按原生规则结束");});
        Text(30,416,865,34,MenuText.Get("一次触发遵循原来的持续时间；保持开启时维持原生增益。"),19);
        Text(30,460,865,34,MenuText.Get("关闭保持后恢复正常衰减，不强行删除自然获得的风神之息。"),19);
        Text(30,522,865,34,MenuText.Get("菜单打开时游戏暂停；关闭菜单后观察移动与技能联动效果。"),17);
    }
    [HideFromIl2Cpp]
    private void DrawSettings()
    {
        Text(30,145,590,36,"语言/language",23,true);
        if(Button("languageZh",650,145,108,"中文",true,!MenuText.IsEnglish)) ChangeLanguage("zh-CN");
        if(Button("languageEn",774,145,116,"English",true,MenuText.IsEnglish)) ChangeLanguage("en-US");
        Text(30,205,600,38,MenuText.Get("快捷键：")+Plugin.Hotkey.Value,23,true);
        if(Button("capture",650,205,240,hotkeyCapture?MenuText.Get("请按 F1–F12（除 F8）"):MenuText.Get("修改快捷键")))hotkeyCapture=true;
        Text(30,265,600,36,MenuText.Format("界面缩放：{0:0.00}x", Plugin.Scale.Value),22);
        if(Button("scaleDown",650,265,108,MenuText.Get("缩小")))Plugin.Scale.Value=Math.Max(0.65f,Plugin.Scale.Value-0.1f);
        if(Button("scaleUp",774,265,116,MenuText.Get("放大")))Plugin.Scale.Value=Math.Min(1.5f,Plugin.Scale.Value+0.1f);
        if(Button("resetCheats",30,329,345,MenuText.Get("关闭作弊并撤销生命/能量调整"),game.Ready))Do(MenuText.Get("关闭作弊"),()=>{actions.Reset();return MenuText.Get("已关闭作弊并撤销生命/能量调整；资源、徽章栏位与物品保留");});
        if(Button("resetUi",407,329,220,MenuText.Get("重置界面配置"))){Plugin.Hotkey.Value=KeyCode.F9;Plugin.Scale.Value=1;x=380;y=90;SavePosition();}
        Text(30,388,858,32,MenuText.Format("状态：独立插件 {0} / 本机 Build 24299434",Plugin.Version),18);
        Text(30,422,858,30,MenuText.Get("语言、快捷键、缩放和窗口位置会保存；F8 保留给原有 HUD。"),17);
        Text(30,455,858,30,MenuText.Get("永久资源与徽章栏位备份：BepInEx/CheatMenuBackups。"),17);
        Text(30,488,858,40,MenuText.Get("作弊开关不跨启动保存。禁用插件：关闭游戏后移走本插件 DLL。"),17,false,true);
        Text(30,531,858,25,"© 2026 BrickZhou/青春啊砖在he边看月亮 · GPLv3 · No warranty",17,true);
        if(Button("authorBilibili",30,562,250,"Bilibili / B站")) OpenAuthorLink("https://space.bilibili.com/517390275");
        if(Button("authorYouTube",300,562,250,"YouTube / Brickzhou")) OpenAuthorLink("https://www.youtube.com/@Brickzhou");
        if(Button("licenses",570,562,320,"GPLv3 / BepInEx licenses")) OpenAuthorLink("https://github.com/BrickAZ/rogue-prince-tools/blob/main/THIRD-PARTY-NOTICES.md");
    }
    [HideFromIl2Cpp]
    private void OpenAuthorLink(string url)
    {
        try { Application.OpenURL(url); }
        catch(Exception e) { ReportError("Open author page",e); }
    }
    [HideFromIl2Cpp]
    private void ChangeLanguage(string code)
    {
        MenuText.SetLanguage(code);
        Plugin.Language.Value = MenuText.LanguageCode;
        focus = "";
        hotkeyCapture = false;
        resources.BindReroll();
        medallions.Refresh();
        message = MenuText.Get("语言已切换为中文并保存；不影响游戏本身的语言。");
        messageError = false;
        Plugin.LogEvent("menu_language=" + MenuText.LanguageCode);
    }
    [HideFromIl2Cpp]
    private void HandleDrag(Event e)
    {
        if(e.type==EventType.MouseDown&&e.button==0&&R(8,0,690,53).Contains(e.mousePosition)){dragging=true;dragOffset=e.mousePosition-new Vector2(x,y);focus="";e.Use();}
        else if(dragging&&e.type==EventType.MouseDrag){x=e.mousePosition.x-dragOffset.x;y=e.mousePosition.y-dragOffset.y;e.Use();}
        else if(dragging&&e.type==EventType.MouseUp){dragging=false;SavePosition();e.Use();}
    }
    [HideFromIl2Cpp] private void SavePosition(){Plugin.PositionX.Value=x;Plugin.PositionY.Value=y;}
    [HideFromIl2Cpp]
    private void Do(string operation,Func<string> action)
    {
        try {message=action();messageError=false;Plugin.LogEvent("action="+operation+" result="+message);}
        catch(Exception e){ReportError(operation,e);}
    }
    [HideFromIl2Cpp]
    private void ReportError(string operation,Exception e)
    {
        message=operation+"："+e.Message;
        messageError=true;
        Plugin.MenuLog.LogError("[RPCheat] "+operation+" "+e);
    }
    public void OnApplicationFocus(bool hasFocus)
    {
        if(!hasFocus&&visible){visible=false;focus="";hotkeyCapture=false;inputLease.Release();}
    }
    public void OnApplicationQuit(){applicationQuitting=true;Shutdown(true);}
    public void OnDestroy()=>Shutdown(applicationQuitting);
    [HideFromIl2Cpp]
    internal void Shutdown(bool quitting)
    {
        if(stopping)return;
        stopping=true;
        visible=false;
        GameplayActions.InfiniteEnergyTarget=IntPtr.Zero;
        try { if(!quitting)actions?.Reset(); }
        catch(Exception e){Plugin.MenuLog.LogWarning(MenuText.Get("清理角色状态：")+e.Message);}
        finally {inputLease.Release(quitting);}
        if(font!=null)UnityEngine.Object.Destroy(font);
        Instance=null;
        Plugin.LogEvent("menu_shutdown");
    }
}
