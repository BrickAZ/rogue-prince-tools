// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using MotherBase.ActionFramework;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
namespace RoguePrince.CheatMenu;
internal sealed class EffectActions
{
    internal sealed class Entry {internal AffectData Data;internal LocalizedItemName Label;internal string Name=>Label.Display+" ["+Data.name+"]";}
    private readonly GameSession game;
    internal readonly List<Entry> Catalog=new();
    internal EffectActions(GameSession game)=>this.game=game;
    internal void Reload()
    {
        Require();var next=new Dictionary<IntPtr,Entry>();
        foreach(var data in Resources.FindObjectsOfTypeAll<AffectData>())if(data!=null)next[data.Pointer]=new(){Data=data,Label=ItemBrowser.ReadLabel(data.title,data.name)};
        var active=game.Affects.activeAffects;for(int i=0;i<active.Count;i++){var data=active[i]?.data;if(data!=null)next[data.Pointer]=new(){Data=data,Label=ItemBrowser.ReadLabel(data.title,data.name)};}
        Catalog.Clear();Catalog.AddRange(next.Values.OrderBy(e=>e.Name));
    }
    private void Require(){game.RequireReady();if(!TrainingActions.ReadScope(game).Valid||game.Affects==null)throw new InvalidOperationException(MenuText.Get("效果组件尚未就绪"));}
    private static int Frames(float seconds)
    {var frame=FrameInfo.FrameDuration;if(!float.IsFinite(seconds)||seconds<.1f||seconds>3600||!float.IsFinite(frame)||frame<=0)throw new ArgumentException(MenuText.Get("时长请输入 0.1–3600 秒"));return checked((int)Math.Ceiling(seconds/frame));}
    internal string Apply(Entry entry,int stacks,float seconds)
    {
        Require();var data=entry?.Data;if(data==null||!Catalog.Any(e=>e.Data.Pointer==data.Pointer))throw new InvalidOperationException(MenuText.Get("请选择已加载的效果"));
        if(stacks<1||stacks>1000||data.maxStacks>0&&stacks>data.maxStacks)throw new ArgumentException(MenuText.Get("层数超出效果原生上限"));
        if(!game.Affects.CanBeAffectedBy(data,false))throw new InvalidOperationException(MenuText.Get("角色当前不能获得该效果"));
        var duration=Frames(seconds);var ctx=OperationContextUtils.CreateContextSourceAndTargetEntity(game.Player,game.Player);
        try
        {
            var old=game.Affects.GetNbStacks(data);
            if(old>stacks)game.Affects.RemoveStack(game.Player,data,old-stacks);
            if(old<stacks)game.Affects.SetAffectData(data,game.Player,duration,data.overridePreviousInstanceOnApply?stacks:stacks-old,ctx,false);
            Require();
            game.Affects.SetDuration(data,duration,false,true);
            if(game.Affects.GetNbStacks(data)!=stacks||!game.Affects.HasAffect(data)||game.Affects.GetRemainingTime(data)!=duration||game.Affects.GetTotalDuration(data)!=duration)throw new InvalidOperationException(MenuText.Get("效果回读失败，原生规则未保留目标层数或时长"));
            Plugin.LogEvent($"effect_set id={data.name} stacks={stacks} duration={duration}");return MenuText.Get("效果层数与时长已设置");
        }
        finally{ctx?.Release();}
    }
    internal string Remove(Entry entry)
    {Require();if(entry?.Data==null)throw new InvalidOperationException(MenuText.Get("请选择已加载的效果"));game.Affects.RemoveAffect(game.Player,entry.Data);if(game.Affects.HasAffect(entry.Data))throw new InvalidOperationException(MenuText.Get("原生效果移除未完成"));return MenuText.Get("选中效果已移除");}
    internal string Clear()
    {
        Require();var list=game.Affects.activeAffects;var captured=new Dictionary<IntPtr,AffectData>();for(int i=0;i<list.Count;i++){var data=list[i]?.data;if(data!=null)captured[data.Pointer]=data;}
        foreach(var data in captured.Values){Require();game.Affects.RemoveAffect(game.Player,data);}
        return MenuText.Format("已请求移除 {0} 种当前效果",captured.Count);
    }
}
