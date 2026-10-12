// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using MotherBase.ActionFramework;
using RoguePrince.CheatMenu.Core;
namespace RoguePrince.CheatMenu;
internal static class EffectSnapshot
{
    internal static List<string> Read(GameSession game)
    {
        var result=new List<string>();
        if(!TrainingActions.ReadScope(game).Valid)return result;
        var list=game.Player.GetComponent<AffectableComponent>()?.activeAffects;
        if(list==null)return result;
        var duration=FrameInfo.FrameDuration;
        for(int i=0;i<Math.Min(list.Count,512);i++)
        {
            var affect=list[i];if(affect==null || !affect.isActive)continue;
            var data=affect.data;
            var name=data==null?"?":ItemBrowser.ReadLabel(data.title,data.name).Display+" ["+data.name+"]";
            result.Add(MenuText.Format("{0} · ID {1} · 层数 {2} · {3}",name,affect.id,affect.stack,StatusEffectView.Remaining(affect.remainingDuration,duration)));
        }
        return result;
    }
}
