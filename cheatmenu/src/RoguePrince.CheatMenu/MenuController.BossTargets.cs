// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using Il2CppInterop.Runtime.Attributes;
using RoguePrince.CheatMenu.Core;
namespace RoguePrince.CheatMenu;
public sealed partial class MenuController
{
 private BossTargetActions.Entry selectedBossTarget;private int bossTargetPage;
 [HideFromIl2Cpp] private void DrawBossTargets()
 {
  var control=scenes.BossTargets;
  var available=BossTargetActions.Available||BossTargetActions.AdditionalAttackAvailable||BossTargetActions.ExtendedAvailable;
  Text(28,230,866,48,MenuText.Get("过滤所选实例已接入的行为。各 Boss 范围不同，选择后查看右侧说明。"),16,false,true);
  if(Button("bossTargetRefresh",28,289,277,MenuText.Get("刷新当前房间 Boss"),SceneReady()&&available))
   Do(MenuText.Get("刷新当前房间 Boss"),()=>{var result=control.Refresh();selectedBossTarget=null;return result;});
  Text(320,289,574,36,MenuText.Format("过滤目标：{0}",control.ActiveName),16);
  var all=control.Entries;var pages=Math.Max(1,(all.Count+4)/5);bossTargetPage=Math.Clamp(bossTargetPage,0,pages-1);
  for(int i=0;i<5&&bossTargetPage*5+i<all.Count;i++)
  {
   var entry=all[bossTargetPage*5+i];var label=entry.Name+" · "+(entry.Kind>=0&&!BossTargetActions.PatchReady(entry.Kind)?MenuText.Get("原生补丁不可用"):entry.FilterLabel);
   if(Button("bossTargetRow"+i,28,342+i*39,605,label,true,selectedBossTarget==entry,16))selectedBossTarget=entry;
  }
  if(Button("bossTargetEnable",650,342,244,MenuText.Get("开启所选 Boss 过滤"),SceneReady()&&available&&!control.Enabled&&selectedBossTarget!=null&&all.Contains(selectedBossTarget)&&(selectedBossTarget.Kind<0||BossTargetActions.PatchReady(selectedBossTarget.Kind)),false,16))
   Do(MenuText.Get("开启所选 Boss 过滤"),()=>{if(!SceneReady())throw new InvalidOperationException(MenuText.Get("先完成当前请求或恢复操作"));return control.SetEnabled(true,selectedBossTarget);});
  if(Button("bossTargetDisable",650,389,244,MenuText.Get("关闭 Boss 过滤"),control.Enabled,false,16))
   Do(MenuText.Get("关闭 Boss 过滤"),()=>control.SetEnabled(false));
  Text(650,437,244,97,selectedBossTarget?.Explanation??MenuText.Get("阶段脑逻辑继续。动作自身追踪、场地攻击仍可能命中；其他 Boss 不支持。"),15,false,true);
  if(all.Count==0)Text(28,350,605,75,MenuText.Get("当前房间没有可列出的 Boss，请刷新"),18,false,true);
  Text(28,535,866,20,control.Status,13,false,true);
  if(Button("bossTargetPrev",28,556,122,MenuText.Get("上一页"),bossTargetPage>0))bossTargetPage--;
  Text(170,556,460,36,MenuText.Format("第 {0} / {1} 页 · {2} 项",bossTargetPage+1,pages,all.Count),16);
  if(Button("bossTargetNext",650,556,244,MenuText.Get("下一页"),bossTargetPage+1<pages))bossTargetPage++;
 }
}
