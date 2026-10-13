// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
// Additional game linking permission: see GAME-LINKING-EXCEPTION.txt.

using Il2CppInterop.Runtime.Attributes;
using RoguePrince.CheatMenu.Core;
using UnityEngine;

namespace RoguePrince.CheatMenu;

public sealed partial class MenuController : MonoBehaviour
{
    internal static MenuController Instance;
    private GameSession game;
    private GameplayActions actions;
    private ResourceActions resources;
    private MedallionSlotActions medallions;
    private ItemBrowser items;
    private MobSpawnActions mobs;
    private LevelTravelActions levels;
    private WeaponAffixActions affixes;
    private NextRescueActions rescue;
    private int rescueRevision;
    private float rescueNoticeUntil;
    private int levelPage, affixPage;
    private Vector2 affixCurrentScroll, affixDraftScroll;
    private bool nextLevelMode;
    private LevelTravelActions.Entry selectedLevel;
    private bool mobAiEnabled = true;
    private int mobSelection;
    private bool mobBossSection, mobListFocus=true;
    private int mobPage;
    private MobBrowser.Entry selectedMob;
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
    private static readonly string[] Tabs = { "角色", "资源", "物品", "怪物", "风神之息", "关卡", "武器词缀", "设置", "预设", "工具" };
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
        mobs = new MobSpawnActions(game);
        levels = new LevelTravelActions(game);
        affixes = new WeaponAffixActions(game, items, resources.Backup);
        rescue = new NextRescueActions(game);
        training = new TrainingActions(game,mobs);
        photo = new PhotoActions(game);
        combat=new CombatActions(game);
        effects=new EffectActions(game);
        loadouts=new LoadoutActions(game,items,affixes,resources.Backup);
        waves=new WaveActions(game,mobs,training,combat);
        rewards=new RewardActions(game,mobs);
        scenes=new SceneActions(game);sceneCamera=new SceneCamera(game);sceneOverlay=new SceneOverlay(game,scenes);runTools=new RunTools();
        bossPhases=new BossPhaseActions(game,scenes);
        bossPractice=new BossPracticeActions(game,scenes,bossPhases);
        runRestart=new RunRestartActions(game,runTools);
        worldReport=new WorldReportActions(game);
        frameObservation=new FrameObservationActions(game){IsMenuVisible=()=>visible};
        abilities=new AbilityLoadoutActions(game){CanMutate=AbilityReady};
        playerCheckpoint=new PlayerCheckpointActions(game){CanMutate=CheckpointReady};
        InitUtilities();
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
            if (now >= nextRefresh && (visible || runRestart.Busy || CoreWorkBusy || playerCheckpoint.HasCheckpoint || scenes.Active || sceneCamera.Active || runTools.Active || HasActionKeys() || practice.Running || actions.GodMode || actions.InfiniteEnergy || actions.HoldWind || actions.HasAdjustments || training.HasSpeed || training.Busy || photo.HasState || mobs.Busy || mobs.HasOwned || rescue.Pending || combat.Active || waves.Active || rewards.Active || loadouts.RecoveryPending))
            {
                nextRefresh = now + 0.5f;
                var recoveryWasPending=affixes.RecoveryPending;
                if (game.Refresh(() => RestorePlayer(true)))
                {
                    selected = null;
                    items.Items.Clear();
                    if(recoveryWasPending){message=MenuText.Get("角色已更换；旧词缀恢复记录已留档，不会应用到新角色。");messageError=true;}
                }
                resources.BindReroll();
                if (visible) medallions.Refresh();
                if (visible) {levels.Refresh();bossArenas=scenes.Bosses();}
            }
            var travelWasBusy = levels.Busy;
            frameObservation.Tick(visible);
            abilities.Tick(Application.isFocused,ReadUtilityLoading());
            playerCheckpoint.Observe(ReadUtilityLoading());
            levels.Tick();
            if (travelWasBusy && !levels.Busy) { message = levels.Status; messageError = levels.Error; }
            if (visible && levels.Loading) ReleaseForTravel();
            rescue.Tick();
            if(rescueRevision!=rescue.Revision){rescueRevision=rescue.Revision;message=rescue.Status;messageError=rescue.Error;rescueNoticeUntil=now+12f;}
            mobs.Tick();
            training.Tick(visible);
            photo.Tick(visible);
            combat.Tick(visible,ReadUtilityLoading(),training.Busy||mobs.Busy);
            waves.Tick(visible);
            rewards.Tick();
            scenes.Tick();sceneCamera.Tick(visible);runTools.Tick();sceneOverlay.Tick();
            bossPhases.Tick();
            var practiceWasBusy=bossPractice.Busy;bossPractice.Tick();
            if(practiceWasBusy&&!bossPractice.Busy)chosenPhaseBoss=bossPhases.Entries.FirstOrDefault()?.Key??0;
            runRestart.Tick();
            worldReport.Observe(visible);
            HandleMobNavigation();
            var ready = game.Ready;
            if (wasReady && !ready)
            {
                visible = false;
                focus = "";
                hotkeyCapture = false;
                inputLease.Release();
                RestorePlayer(bossPractice.AwaitingReload);
            }
            wasReady = ready;
            if (visible) inputLease.Acquire(game);
            else if ((releaseInputAt>0||inputLease.Active) && now>=releaseInputAt && (!Application.isFocused||!ConfirmStillHeld()))
                releaseInputAt=inputLease.Release()?0:now+0.15f;
            if (now >= nextTick) { nextTick = now + 0.15f; actions.Tick(); }
            UpdateUtilities();
        }
        catch (Exception e)
        {
            actions.StopContinuous();
            visible = false;
            try { RestorePlayer(); } catch (Exception cleanup) { Plugin.MenuLog.LogWarning(MenuText.Get("角色清理将在下次绑定时重试：") + cleanup.Message); }
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
                if(!ActionKeyPolicy.CanCapture(visible,tab,utilitySection)) actionKeyCapture=null;
                if (actionKeyCapture!=null) CaptureUtilityKey(e);
                else if (hotkeyCapture)
                {
                    if (e.keyCode == KeyCode.Escape) hotkeyCapture = false;
                    else if (e.keyCode >= KeyCode.F1 && e.keyCode <= KeyCode.F12 && e.keyCode != KeyCode.F8)
                    { SetMenuKey(e.keyCode); }
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
                sceneOverlay.Draw(textStyle,photo.Active);
                if (e.type == EventType.Repaint && !photo.Active)
                {
                    textStyle.fontSize = 16;
                    textStyle.normal.textColor = Accent;
                    GUI.Label(new Rect(Math.Max(5, Screen.width - 310), 12, 300, 28), MenuText.Format("{0}  调试菜单  {1}", Plugin.Hotkey.Value, Plugin.Version), textStyle);
                    if(FrameObservationActive)
                    {
                        textStyle.fontSize=14;textStyle.wordWrap=true;
                        GUI.Label(new Rect(Math.Max(5,Screen.width-490),44,480,92),MenuText.Format("单帧观察暂停中 · {0} 打开菜单\n{1}",Plugin.Hotkey.Value,frameObservation.Status),textStyle);
                        textStyle.wordWrap=false;
                    }
                    if(Time.realtimeSinceStartup<rescueNoticeUntil)
                    {
                        var noticeWidth=Math.Min(480,Screen.width-20);
                        textStyle.fontSize=14;textStyle.wordWrap=true;
                        textStyle.normal.textColor=rescue.Error?new Color(1f,.65f,.35f):Accent;
                        GUI.Label(new Rect(Screen.width-noticeWidth-10,44,noticeWidth,94),rescue.Status,textStyle);
                        textStyle.wordWrap=false;
                    }
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
            RecoverDrawing();
            ReportError(MenuText.Get("菜单绘制失败"), ex);
        }
        finally { GUI.color = oldColor; GUI.depth = oldDepth; }
    }

    [HideFromIl2Cpp]
    private void Toggle()
    {
        if (!visible && (levels.Loading||scenes.Busy)) return;
        if(!visible&&!MenuPauseInputPatch.Available)
        {ReportError(MenuText.Get("输入保护尚未就绪"),new InvalidOperationException(MenuText.Get("原生暂停输入保护未安装，请查看日志")));return;}
        visible = !visible;
        focus = "";
        pressed = "";
        hotkeyCapture = false;
        actionKeyCapture = null;
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
    private bool Button(string id, float a, float b, float w, string caption, bool enabled=true, bool selectedButton=false, int textSize=17, bool wrap=false)
    {
        var rect=R(a,b,w,36);
        var hover=rect.Contains(Event.current.mousePosition);
        var color=!enabled?new Color(0.11f,0.14f,0.19f):selectedButton?new Color(0.12f,0.38f,0.33f):hover?new Color(0.23f,0.30f,0.38f):new Color(0.15f,0.20f,0.28f);
        Box(a,b,w,36,color);
        Text(a+10,b+1,w-18,34,caption,textSize,selectedButton,wrap);
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
        var max=focus.EndsWith("Name",StringComparison.Ordinal)?48:focus=="search"||focus.EndsWith("Search",StringComparison.Ordinal)?100:12;
        fields[focus]=value.Length>max?value[..max]:value;
        if(focus=="search") itemPage=0;
        if(focus=="levelSearch") levelPage=0;
        if(focus=="affixSearch") affixPage=0;
        if(focus=="mobSearch") mobPage=0;
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
        var tabX=24f;
        for(int i=0;i<Tabs.Length;i++)
        {
            var width=i==0?82:i==1?84:i==4?110:i==6?94:76;
            if(Button("tab"+i,tabX,88,width,MenuText.Get(Tabs[i]),true,tab==i,13)){tab=i;focus="";actionKeyCapture=null;}
            tabX+=width+6;
        }
        switch(tab) {case 0:DrawPlayer();break;case 1:DrawResources();break;case 2:DrawItems();break;case 3:DrawMobs();break;case 4:DrawWind();break;case 5:DrawLevels();break;case 6:DrawAffixes();break;case 7:DrawSettings();break;case 8:DrawPresets();break;case 9:DrawUtilities();break;}
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
        var query=Field("search",94,139,307,"");
        if(Button("reloadItems",546,139,172,MenuText.Get("刷新物品库"),game.Ready))Do(MenuText.Get("刷新物品库"),()=>{items.Reload();itemPage=0;selected=null;return items.State;});
        if(Button("clearSearch",414,139,110,MenuText.Get("清空搜索"),true,false,14)){fields["search"]="";query="";itemPage=0;}
        if(Button("itemFavorites",737,139,159,MenuText.Get("仅收藏"),true,itemFavoritesOnly)){itemFavoritesOnly=!itemFavoritesOnly;itemPage=0;selected=null;}
        for(int i=0;i<Categories.Length;i++)
            if(Button("cat"+i,28+(i%7)*125,186+(i/7)*40,116,MenuText.Get(Categories[i]),true,category==i)){category=i;itemPage=0;selected=null;}
        var result=items.Filter(query,Categories[category]).Where(e=>!itemFavoritesOnly||utilities.IsFavorite("item",e.Data.Guid.guid)).ToList();
        if(selected!=null&&!result.Contains(selected))selected=null;
        const int pageSize=6;
        var pages=Math.Max(1,(result.Count+pageSize-1)/pageSize);
        itemPage=Math.Clamp(itemPage,0,pages-1);
        for(int i=0;i<pageSize && itemPage*pageSize+i<result.Count;i++)
        {
            var entry=result[itemPage*pageSize+i];var row=275+i*43;
            if(Button("item"+i,28,row,502,entry.Name+"  ·  "+MenuText.Get(entry.Category),true,selected==entry))selected=entry;
            FavoriteButton("itemFav"+i,"item",entry.Data.Guid.guid,539,row,52);
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
        if(Button("customWeapon",630,521,243,MenuText.Get("自定义词缀后生成"),game.Ready&&selected?.Data?.HasComponent<AffixHolderItemFeature>()==true&&!affixes.RecoveryPending))
            Do(MenuText.Get("载入新武器"),()=>{var v=1;if(selected.HasTier&&(!NumericPolicy.TryTarget(tier,0,false,100,out v,out _)||v<1))throw new ArgumentException(MenuText.Get("请先选择武器和有效等级"));var result=affixes.LoadSpawn(selected,v);affixPage=0;fields["affixSearch"]="";tab=6;return result;});
        if(Button("prev",28,548,100,MenuText.Get("上一页"),itemPage>0))itemPage--;
        Text(145,548,320,36,MenuText.Format("第 {0} / {1} 页 · {2} 项", itemPage+1, pages, result.Count),17);
        if(Button("next",490,548,101,MenuText.Get("下一页"),itemPage+1<pages))itemPage++;
        Text(615,565,290,30,MenuText.Get("生成不会自动永久解锁。"),14,false,true);
    }

    [HideFromIl2Cpp]
    private void ReleaseForTravel()
    {
        combat.MarkPractice("teleport / menu travel");waves.Reset();combat.StopModes();
        visible=false; focus=""; pressed=""; hotkeyCapture=false; actionKeyCapture=null; releaseInputAt=0;
        mobs.CancelPending(); inputLease.Release(); SavePosition();
    }
    [HideFromIl2Cpp]
    private void Travel(LevelTravelActions.Entry entry)
    {
        if(loadouts.RecoveryPending){message=MenuText.Get("配装恢复尚未完成");messageError=true;return;}
        if(affixes.RecoveryPending){message=MenuText.Get("旧词缀恢复未完成，请先重试恢复");messageError=true;return;}
        if(scenes.Busy||CoreWorkBusy||runRestart.Busy){message=MenuText.Get("先完成当前请求或恢复操作");messageError=true;return;}
        Do(MenuText.Get("跳关"),()=>{RestoreTraining();return levels.Travel(entry,ReleaseForTravel);});
    }
    [HideFromIl2Cpp]
    private void DrawLevels()
    {
        Text(28,140,865,36,MenuText.Format("当前关卡：{0}",levels.CurrentName),24,true);
        Text(28,186,70,36,MenuText.Get("搜索"));
        var query=Field("levelSearch",100,186,470,"");
        if(Button("refreshLevels",595,186,300,MenuText.Get("刷新关卡列表"),game.Ready&&!levels.Loading))
            Do(MenuText.Get("刷新关卡列表"),()=>{levels.Refresh();selectedLevel=null;return MenuText.Format("可选关卡：{0}",levels.Levels.Count);});
        if(Button("nextLevel",28,233,250,MenuText.Get("进入下一关"),game.Ready&&!levels.Loading&&levels.Next.Count>0&&!affixes.RecoveryPending,nextLevelMode))
        {
            nextLevelMode=true;levelPage=0;selectedLevel=null;fields["levelSearch"]="";query="";
            if(levels.Next.Count==1)Travel(levels.Next[0]);
        }
        if(Button("chooseLevel",295,233,250,MenuText.Get("选择指定关卡"),!levels.Loading,!nextLevelMode)){nextLevelMode=false;levelPage=0;selectedLevel=null;}
        var rescueLabel=MenuText.Get(rescue.RecoveryPending?"重试恢复营救安排":rescue.Pending?"取消营救预约":"下一关必出营救任务");
        if(Button("nextRescue",562,233,333,rescueLabel,game.Ready&&!levels.Loading&&NextRescueActions.Available,rescue.Pending))
            Do(MenuText.Get("营救任务"),()=>rescue.Pending||rescue.RecoveryPending?rescue.Cancel():rescue.Arm());
        var result=(nextLevelMode?levels.Next:levels.Levels).Where(e=>e.Label.Matches(query)).ToList();
        var pages=Math.Max(1,(result.Count+5)/6);levelPage=Math.Clamp(levelPage,0,pages-1);
        selectedLevel=selectedLevel==null?null:result.FirstOrDefault(e=>e.Key==selectedLevel.Key);
        for(int i=0;i<6&&levelPage*6+i<result.Count;i++)
        {
            var entry=result[levelPage*6+i];
            if(Button("levelRow"+i,28,287+i*42,554,entry.Name,true,selectedLevel?.Key==entry.Key))selectedLevel=entry;
        }
        Text(608,286,280,90,nextLevelMode?MenuText.Get("当前有分支出口，请选择目的地。"):MenuText.Get("选择游戏关卡后进入；当前关卡不重复加载。"),18,false,true);
        Text(608,386,280,60,selectedLevel?.Name??MenuText.Get("请选择关卡"),22,true,true);
        if(Button("travelLevel",608,463,280,MenuText.Get("进入选中关卡"),game.Ready&&!levels.Loading&&selectedLevel!=null&&!affixes.RecoveryPending))Travel(selectedLevel);
        if(Button("levelPrev",28,548,105,MenuText.Get("上一页"),levelPage>0))levelPage--;
        Text(148,548,295,36,MenuText.Format("第 {0} / {1} 页 · {2} 项",levelPage+1,pages,result.Count),16);
        if(Button("levelNext",477,548,105,MenuText.Get("下一页"),levelPage+1<pages))levelPage++;
        Text(608,507,280,50,rescue.Status.Length==0?MenuText.Get("营救未预约；不支持的下一关只提示，不顺延。"):rescue.Status,13,rescue.Pending,true);
        Text(608,560,280,37,MenuText.Get("切关会关闭菜单并恢复输入；装备与资源沿用原生切关规则。"),12,false,true);
        if(result.Count==0)Text(28,300,554,80,MenuText.Get("当前没有可用关卡；进入存档后刷新。"),18,false,true);
    }
    [HideFromIl2Cpp]
    private void DrawAffixes()
    {
        Text(28,140,865,38,MenuText.Format("武器词缀：{0}",affixes.Name),24,true);
        var ready=game.Ready&&!levels.Loading&&!affixes.RecoveryPending;
        if(Button("loadMain",28,187,265,MenuText.Get("载入主武器"),ready))Do(MenuText.Get("载入主武器"),()=>{affixPage=0;return affixes.LoadEquipped(false);});
        if(Button("loadSecondary",310,187,265,MenuText.Get("载入工具"),ready))Do(MenuText.Get("载入工具"),()=>{affixPage=0;return affixes.LoadEquipped(true);});
        if(Button("recoverAffixes",593,187,302,MenuText.Get("重试恢复原词缀"),game.Ready&&affixes.RecoveryPending))Do(MenuText.Get("恢复词缀"),affixes.Recover);
        Text(28,236,65,36,MenuText.Get("搜索"));
        var query=Field("affixSearch",100,236,326,"");
        if(Button("affixFavorites",439,236,137,MenuText.Get("仅收藏"),true,affixFavoritesOnly)){affixFavoritesOnly=!affixFavoritesOnly;affixPage=0;}
        Text(601,236,294,40,MenuText.Get(affixes.IsSpawn?"生成新武器":"修改当前装备"),19,true);
        var result=affixes.Options.Where(e=>e.Label.Matches(query)&&(!affixFavoritesOnly||utilities.IsFavorite("affix",e.Id))).ToList();
        var pages=Math.Max(1,(result.Count+5)/6);affixPage=Math.Clamp(affixPage,0,pages-1);
        for(int i=0;i<6&&affixPage*6+i<result.Count;i++)
        {
            var option=result[affixPage*6+i];var chosen=affixes.Draft.Any(e=>e.Id==option.Id);
            if(Button("affixRow"+i,28,288+i*42,489,(chosen?"☑ ":"□ ")+option.Label.Display,ready&&(chosen||affixes.CanAdd(option)),chosen,14,true))affixes.Toggle(option);
            FavoriteButton("affixFav"+i,"affix",option.Id,526,288+i*42,50);
        }
        Text(602,284,287,25,MenuText.Get("当前词缀"),17,true);
        affixCurrentScroll=AffixText(602,312,287,72,affixes.Current.Length==0?MenuText.Get("无"):affixes.Current,affixCurrentScroll);
        Text(602,391,287,25,MenuText.Format("草稿词缀：{0} 个",affixes.Draft.Count),17,true);
        affixDraftScroll=AffixText(602,419,287,78,affixes.Draft.Count==0?MenuText.Get("无"):string.Join("\n",affixes.Draft.Select(e=>"• "+e.Label.Display)),affixDraftScroll);
        if(Button("applyAffixes",602,505,287,MenuText.Get(affixes.IsSpawn?"生成自定义武器":"应用词缀"),ready&&affixes.HasTarget))
            Do(MenuText.Get("应用武器词缀"),()=>affixes.IsSpawn?affixes.Spawn():affixes.ApplyEquipped());
        if(Button("clearAffixes",602,549,287,MenuText.Get("清空词缀草稿"),ready&&affixes.HasTarget))affixes.ClearDraft();
        if(Button("affixPrev",28,545,105,MenuText.Get("上一页"),affixPage>0))affixPage--;
        Text(148,545,295,36,MenuText.Format("第 {0} / {1} 页 · {2} 项",affixPage+1,pages,result.Count),16);
        if(Button("affixNext",471,545,105,MenuText.Get("下一页"),affixPage+1<pages))affixPage++;
        Text(28,579,548,20,MenuText.Get("勾选和清空只修改草稿；点击应用或生成后生效。"),13);
        if(!affixes.HasTarget)Text(28,300,548,110,MenuText.Get("载入当前武器，或在物品页选择武器后点击自定义词缀后生成。"),19,false,true);
    }

    [HideFromIl2Cpp]
    private void DrawMobs()
    {
        Text(28,143,680,28,MenuText.Get("怪物召唤清单"),25,true);
        Text(28,175,855,19,MenuText.Get("上下选目标 · 左右切换清单/操作 · Q/E 切分区 · Enter / A 确认"),13);
        if(Button("mobRegular",28,205,258,MenuText.Get("普通怪物"),!mobs.Busy,!mobBossSection))SetMobSection(false);
        if(Button("mobBosses",305,205,258,MenuText.Get("Boss"),!mobs.Busy,mobBossSection))SetMobSection(true);
        Text(28,252,65,36,MenuText.Get("搜索"));
        var query=Field("mobSearch",94,252,214,"");
        if(Button("mobFavorites",320,252,111,MenuText.Get("仅收藏"),!mobs.Busy,mobFavoritesOnly,14)){mobFavoritesOnly=!mobFavoritesOnly;mobPage=0;selectedMob=null;}
        if(Button("mobSearchClear",443,252,120,MenuText.Get("清空搜索"))){query=fields["mobSearch"]="";mobPage=0;}
        var result=FilteredMobs(query);
        if(selectedMob!=null&&!result.Contains(selectedMob))selectedMob=null;
        const int pageSize=6;
        var pages=Math.Max(1,(result.Count+pageSize-1)/pageSize);
        mobPage=Math.Clamp(mobPage,0,pages-1);
        for(int i=0;i<pageSize&&mobPage*pageSize+i<result.Count;i++)
        {
            var entry=result[mobPage*pageSize+i];
            var label=entry.Name==entry.InternalName?entry.Name:entry.Name+" · "+entry.InternalName;
            if(!entry.Data.Supported)label+=" · "+MenuText.Get("不可召唤");
            if(Button("mobEntry"+i,28,295+i*38,477,label,!mobs.Busy,selectedMob==entry)){selectedMob=entry;mobListFocus=true;}
            FavoriteButton("mobFav"+i,"mob",entry.InternalName,514,295+i*38,49);
        }
        if(result.Count==0)Text(28,305,535,70,MenuText.Get("没有符合条件的怪物"),18,false,true);
        if(Button("mobPrev",28,532,100,MenuText.Get("上一页"),!mobs.Busy&&mobPage>0))mobPage--;
        Text(140,532,310,36,MenuText.Format("第 {0} / {1} 页 · {2} 项",mobPage+1,pages,result.Count),16);
        if(Button("mobNext",463,532,100,MenuText.Get("下一页"),!mobs.Busy&&mobPage+1<pages))mobPage++;
        Box(587,205,309,362,new Color(.055f,.09f,.14f));
        Text(603,211,277,44,selectedMob?.Name??MenuText.Get("请选择怪物"),20,true,true);
        Text(603,258,277,23,selectedMob?.InternalName??MenuText.Get("支持名称与资源名搜索"),13);
        if(Button("mobAI",603,298,277,MenuText.Get("启用 AI")+"  "+MenuText.Get(mobAiEnabled?"开启":"关闭"),!mobs.Busy,!mobListFocus&&mobSelection==0))
            ExecuteMobAction(0);
        var bossSelected=selectedMob?.Data.Boss==true;
        var canSpawn=game.Ready&&!training.Busy&&!mobs.Busy&&selectedMob?.Data.Supported==true;
        if(Button("mobOne",603,340,277,MenuText.Get("1 滴血生成"),canSpawn,!mobListFocus&&mobSelection==1))ExecuteMobAction(1);
        if(Button("mobFull",603,382,277,MenuText.Get("原版满血生成"),canSpawn,!mobListFocus&&mobSelection==2))ExecuteMobAction(2);
        Text(603,427,277,63,bossSelected?MenuText.Get("在当前房间开战。请站在宽阔平地上；AI 开启后等待原生攻击就绪。"):selectedMob?.Data.Supported==false?MenuText.Get("此资源缺少通用召唤组件，不能在此生成。"):MenuText.Get("生成后菜单自动关闭。请站在宽阔平地上，远离门、NPC 和陷阱。"),14,false,true);
        if(Button("mobClear",603,497,277,MenuText.Get("清理本菜单生成的怪物"),mobs.HasOwned||mobs.Busy,!mobListFocus&&mobSelection==3))ExecuteMobAction(3);
        Text(603,540,277,22,MenuText.Format("当前自有怪物：{0}",mobs.Count),15);
        Text(28,575,855,30,mobs.Status,14,!mobs.Error,true);
    }

    [HideFromIl2Cpp]
    private void SetMobSection(bool boss){mobBossSection=boss;selectedMob=null;mobPage=0;mobListFocus=true;}

    [HideFromIl2Cpp]
    private void ExecuteMobAction(int index)
    {
        mobSelection=index;
        mobListFocus=false;
        if(index==0){mobAiEnabled=!mobAiEnabled;return;}
        if(index==3)
        {
            if(mobs.HasOwned||mobs.Busy||training.BatchActive)Do(MenuText.Get("清理怪物"),training.ClearAll);
            return;
        }
        if(!game.Ready||training.Busy||mobs.Busy||CoreWorkBusy||runRestart.Busy||selectedMob?.Data.Supported!=true)return;
        try
        {
            AdmitTraining(()=>{if(mobs.Request(new MobSpawnOptions(index==1?MobSpawnHealthMode.OneHp:MobSpawnHealthMode.NativeFull,mobAiEnabled,selectedMob.InternalName)))Toggle();return mobs.Status;});
            message=mobs.Status;messageError=mobs.Error;
        }
        catch(Exception e){ReportError(MenuText.Get("生成怪物"),e);}
    }

    [HideFromIl2Cpp]
    private void OpenBossPractice()
    {
        bossArenas=scenes.Bosses();chosenBoss=null;bossPage=0;
        tab=9;utilityPage=2;utilitySection=12;focus="";
    }

    [HideFromIl2Cpp]
    private void HandleMobNavigation()
    {
        if(!visible||collapsed||hotkeyCapture||actionKeyCapture!=null||!Application.isFocused)return;
        var pad=UnityEngine.InputSystem.Gamepad.current;
        if(pad!=null)
        {
            if(pad.leftShoulder.wasPressedThisFrame){tab=(tab+Tabs.Length-1)%Tabs.Length;focus="";actionKeyCapture=null;return;}
            if(pad.rightShoulder.wasPressedThisFrame){tab=(tab+1)%Tabs.Length;focus="";actionKeyCapture=null;return;}
            if(tab==3&&pad.buttonEast.wasPressedThisFrame){Toggle();return;}
        }
        if(tab!=3||focus.Length!=0)return;
        var keyboard=UnityEngine.InputSystem.Keyboard.current;
        if(mobs.Busy)return;
        if((keyboard!=null&&keyboard.qKey.wasPressedThisFrame)||(pad!=null&&pad.leftTrigger.wasPressedThisFrame)){SetMobSection(false);return;}
        if((keyboard!=null&&keyboard.eKey.wasPressedThisFrame)||(pad!=null&&pad.rightTrigger.wasPressedThisFrame)){SetMobSection(true);return;}
        if((keyboard!=null&&keyboard.leftArrowKey.wasPressedThisFrame)||(pad!=null&&pad.dpad.left.wasPressedThisFrame)){mobListFocus=true;return;}
        if((keyboard!=null&&keyboard.rightArrowKey.wasPressedThisFrame)||(pad!=null&&pad.dpad.right.wasPressedThisFrame)){mobListFocus=false;return;}
        bool up=(keyboard!=null&&keyboard.upArrowKey.wasPressedThisFrame)||(pad!=null&&pad.dpad.up.wasPressedThisFrame);
        bool down=(keyboard!=null&&keyboard.downArrowKey.wasPressedThisFrame)||(pad!=null&&pad.dpad.down.wasPressedThisFrame);
        bool confirm=(keyboard!=null&&keyboard.enterKey.wasPressedThisFrame)||(pad!=null&&pad.buttonSouth.wasPressedThisFrame);
        if(mobListFocus)
        {
            var list=FilteredMobs(fields.TryGetValue("mobSearch",out var query)?query:"");
            if(list.Count==0)return;
            int index=selectedMob==null?-1:list.IndexOf(selectedMob);
            if(up||down){index=index<0?0:(index+(up?-1:1)+list.Count)%list.Count;selectedMob=list[index];mobPage=index/6;}
            if(confirm){if(selectedMob==null){selectedMob=list[Math.Min(mobPage*6,list.Count-1)];}mobListFocus=false;mobSelection=2;}
            return;
        }
        if(up)
            mobSelection=(mobSelection+3)%4;
        if(down)
            mobSelection=(mobSelection+1)%4;
        if(confirm)
            ExecuteMobAction(mobSelection);
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
        if(Button("resetCheats",30,329,345,MenuText.Get("关闭作弊并撤销生命/能量调整"),game.Ready))Do(MenuText.Get("关闭作弊"),()=>{if(runRestart.Busy)runRestart.Cancel();RecoverySteps.Run(()=>RestoreTraining(),()=>actions.Reset());return MenuText.Get("已关闭作弊并撤销生命/能量调整；资源、徽章栏位与物品保留");});
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
                actionKeyCapture = null;
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
        Plugin.RecentLog.Add(message);
        Plugin.MenuLog.LogError("[RPCheat] "+operation+" "+e);
    }
    public void OnApplicationFocus(bool hasFocus)
    {
        if(!hasFocus){restartConfirmation=false;if(runRestart?.Busy==true)runRestart.Cancel();practiceWasActive=false;try{RestoreTraining();}catch(Exception e){ReportError(MenuText.Get("训练状态恢复"),e);}}
        if(!hasFocus&&visible){mobs.CancelPending();visible=false;focus="";hotkeyCapture=false;actionKeyCapture=null;inputLease.Release();}
    }
    public void OnApplicationQuit(){applicationQuitting=true;Shutdown(true);}
    public void OnDestroy()=>Shutdown(applicationQuitting);
    [HideFromIl2Cpp]
    internal void Shutdown(bool quitting)
    {
        if(stopping)return;
        visible=false;
        var failures=new List<Exception>();
        try{RestoreTraining();}catch(Exception e){failures.Add(e);}
        if(!quitting&&loadouts?.RecoveryPending==true)failures.Add(new InvalidOperationException(MenuText.Get("配装恢复尚未完成")));
        try{rescue?.Dispose(quitting);}catch(Exception e){failures.Add(e);}
        try{runRestart?.Dispose();}catch(Exception e){failures.Add(e);}
        try{runTools?.Dispose();}catch(Exception e){failures.Add(e);}
        if(!quitting)
        {
            try{mobs?.Dispose();}catch(Exception e){failures.Add(e);}
            try{actions?.Reset();}catch(Exception e){failures.Add(e);}
        }
        try{if(!inputLease.Release(quitting))failures.Add(new InvalidOperationException(MenuText.Get("输入恢复尚未完成")));}catch(Exception e){failures.Add(e);}
        if(!quitting && photo?.HasState==true)failures.Add(new InvalidOperationException(MenuText.Get("镜头撤销仍待原生确认；请关闭菜单后重试卸载")));
        if(!quitting && frameObservation?.RecoveryPending==true)failures.Add(new InvalidOperationException(MenuText.Get("时间控制仍待归还；请退出原生暂停后重试卸载")));
        if(failures.Count>0 && !quitting)throw new AggregateException(MenuText.Get("清理尚未完成，保留控制器以便重试"),failures);
        foreach(var failure in failures)Plugin.MenuLog.LogWarning("[RPCheat] shutdown_cleanup "+failure);
        stopping=true;
        GameplayActions.InfiniteEnergyTarget=IntPtr.Zero;
        if(font!=null)UnityEngine.Object.Destroy(font);
        Instance=null;
        Plugin.LogEvent("menu_shutdown");
    }
}
