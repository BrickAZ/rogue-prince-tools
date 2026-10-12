// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using Il2CppInterop.Runtime.Attributes;
using RoguePrince.CheatMenu.Core;
namespace RoguePrince.CheatMenu;
public sealed partial class MenuController
{
 private BossPhaseActions bossPhases;private BossPracticeActions bossPractice;private long chosenPhaseBoss;private int phaseBossPage,practiceTargetPhase=1;private bool showBossPracticeStatus;
 private bool BossPhaseWorkBusy=>bossPhases.Busy||bossPractice?.Busy==true;
 [HideFromIl2Cpp]
 private string AdmitTraining(Func<string> work)=>BossPhaseAdmission.Execute(CoreWorkBusy||runRestart.Busy,work);
 [HideFromIl2Cpp]
 private void DrawBossPhases()
 {
  Text(28,230,866,60,MenuText.Get("原实例可逐段推进到较后阶段；重建可选择阶段 1 / 2 / 3。原生转换与伤害沿用原版，倒退需要重建场地。"),16,false,true);
  var all=bossPhases.Entries;var pages=Math.Max(1,(all.Count+1)/2);phaseBossPage=Math.Clamp(phaseBossPage,0,pages-1);
  for(int i=0;i<2&&phaseBossPage*2+i<all.Count;i++)
  {var entry=all[phaseBossPage*2+i];string label;try{label=entry.Label;}catch{label=MenuText.Get("目标关卡已失效，请刷新关卡列表");}if(Button("phaseBoss"+i,28,296+i*39,602,label,!BossPhaseWorkBusy,chosenPhaseBoss==entry.Key,16))chosenPhaseBoss=entry.Key;}
  var selected=all.FirstOrDefault(x=>x.Key==chosenPhaseBoss);
  if(Button("phaseRefresh",651,296,244,MenuText.Get("刷新当前 Boss"),SceneReady()))Do(MenuText.Get("刷新当前 Boss"),()=>{var result=bossPhases.Refresh();chosenPhaseBoss=bossPhases.Entries.FirstOrDefault()?.Key??0;return result;});
  if(Button("phaseAdvance",651,344,244,MenuText.Get("推进下一阶段"),SceneReady()&&!sceneCamera.Active&&!photo.HasState&&selected!=null))Do(MenuText.Get("推进下一阶段"),()=>{showBossPracticeStatus=false;return bossPhases.Request(selected,ReleaseForTravel);});
  if(Button("phaseCancel",651,392,244,MenuText.Get("取消阶段观察"),BossPhaseWorkBusy,false,16))Do(MenuText.Get("取消阶段观察"),()=>bossPractice.Busy?bossPractice.Cancel():bossPhases.Cancel());
  for(int p=1;p<=3;p++){var phase=p;if(Button("practicePhase"+p,28+(p-1)*142,397,132,MenuText.Format("阶段 {0}",p),!BossPhaseWorkBusy,practiceTargetPhase==p,16))practiceTargetPhase=phase;}
  if(Button("practiceCurrent",28,442,415,MenuText.Get("原实例推进到所选阶段"),SceneReady()&&!runTools.Pending&&!rescue.Pending&&!sceneCamera.Active&&!photo.HasState&&selected!=null,false,16))
   Do(MenuText.Get("原实例阶段推进"),()=>{if(!SceneReady()||runTools.Pending||rescue.Pending||sceneCamera.Active||photo.HasState)throw new InvalidOperationException(MenuText.Get("先完成当前请求或恢复操作"));RestoreTraining();showBossPracticeStatus=true;return bossPractice.StartCurrent(selected,practiceTargetPhase,ReleaseForTravel);});
  if(Button("practiceRebuild",466,442,429,MenuText.Get("重建到所选阶段"),SceneReady()&&!runTools.Pending&&!rescue.Pending&&!sceneCamera.Active&&!photo.HasState&&selected!=null,false,16))
   Do(MenuText.Get("重建到所选阶段"),()=>{if(!SceneReady()||runTools.Pending||rescue.Pending||sceneCamera.Active||photo.HasState)throw new InvalidOperationException(MenuText.Get("先完成当前请求或恢复操作"));RestoreTraining();showBossPracticeStatus=true;return bossPractice.Start(selected,practiceTargetPhase,ReleaseForTravel);});
  Text(28,486,866,42,MenuText.Get(BossPhaseWorkBusy?"观察中，请关闭菜单让原生 Boss 控制器运行。":"原实例推进保留当前实体与场地；停止不会撤销伤害。转换未确认时不会重发，同实例倒退尚未接入。"),14,false,true);
  if(all.Count==0)Text(28,305,602,75,MenuText.Get("当前房间没有支持的原生 Boss"),18,false,true);
  if(Button("phasePrev",28,535,110,MenuText.Get("上一页"),phaseBossPage>0))phaseBossPage--;
  Text(150,535,330,36,MenuText.Format("第 {0} / {1} 页 · {2} 项",phaseBossPage+1,pages,all.Count),16);
  if(Button("phaseNext",491,535,139,MenuText.Get("下一页"),phaseBossPage+1<pages))phaseBossPage++;
  if(Button("phaseArena",651,535,244,MenuText.Get("返回场地练习"),!BossPhaseWorkBusy)){utilityPage=2;utilitySection=12;}
  Text(28,579,866,28,showBossPracticeStatus?bossPractice.Status:bossPhases.Status,14,showBossPracticeStatus?bossPractice.Error:bossPhases.Error,true);
 }
}
