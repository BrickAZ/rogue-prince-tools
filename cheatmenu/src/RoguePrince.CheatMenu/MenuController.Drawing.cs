// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using Il2CppInterop.Runtime.Attributes;
using RoguePrince.CheatMenu.Core;
using UnityEngine;
namespace RoguePrince.CheatMenu;

public sealed partial class MenuController
{
    [HideFromIl2Cpp]
    private Vector2 AffixText(float a,float b,float w,float h,string value,Vector2 scroll)
    {
        textStyle.fontSize=Math.Max(6,(int)(14*scale));
        textStyle.fontStyle=FontStyle.Normal;
        textStyle.normal.textColor=new Color(0.86f,0.90f,0.95f);
        var oldWrap=textStyle.wordWrap;
        textStyle.wordWrap=true;
        try
        {
            var viewport=R(a,b,w,h);
            float width=Math.Max(1,(w-24)*scale);
            float height=Math.Max(viewport.height,textStyle.CalcHeight(new GUIContent(value),width));
            float maximum=Math.Max(0,height-viewport.height);
            scroll.x=0;scroll.y=Math.Clamp(scroll.y,0,maximum);
            var e=Event.current;
            if(maximum>0&&e.type==EventType.ScrollWheel&&viewport.Contains(e.mousePosition))
            {scroll.y=Math.Clamp(scroll.y+e.delta.y*textStyle.fontSize*2,0,maximum);e.Use();}
            float thumbHeight=Math.Min(viewport.height,Math.Max(12*scale,viewport.height*viewport.height/height));
            var id="affixScroll"+b;
            if(maximum>0)
            {
                var track=R(a+w-16,b,16,h);
                if(e.type==EventType.MouseDown&&e.button==0&&track.Contains(e.mousePosition)){pressed=id;focus="";}
                if(pressed==id&&(e.type==EventType.MouseDown||e.type==EventType.MouseDrag))
                {scroll.y=Math.Clamp((e.mousePosition.y-viewport.y-thumbHeight/2)/Math.Max(1,viewport.height-thumbHeight),0,1)*maximum;e.Use();}
                if(pressed==id&&e.type==EventType.MouseUp){pressed="";e.Use();}
            }
            else if(pressed==id)pressed="";
            // Begin/EndScrollView are throwing unstripping stubs in this game.
            // These clip primitives have actual native implementations. Pair the
            // clip even if Label fails so another menu cannot inherit the viewport.
            GUI.BeginClip(viewport,new Vector2(0,-scroll.y),Vector2.zero,false);
            try {GUI.color=Color.white;GUI.Label(new Rect(0,0,width,height),value,textStyle);}
            finally {GUI.EndClip();}
            if(maximum>0)
            {
                Box(a+w-16,b,16,h,new Color(.055f,.09f,.14f));
                float thumbY=(viewport.height-thumbHeight)*scroll.y/maximum;
                Box(a+w-14,b+thumbY/scale,12,thumbHeight/scale,Accent);
            }
            return scroll;
        }
        finally {textStyle.wordWrap=oldWrap;}
    }
    [HideFromIl2Cpp]
    private void RecoverDrawing()
    {
        visible=false;
        tab=0;focus="";pressed="";dragging=false;hotkeyCapture=false;actionKeyCapture=null;releaseInputAt=0;
        try { inputLease.Release(); } catch(Exception cleanup) { Plugin.MenuLog.LogWarning(MenuText.Get("输入清理：")+cleanup.Message); }
    }
}
