// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using Il2CppInterop.Runtime.Attributes;
using RoguePrince.CheatMenu.Core;
namespace RoguePrince.CheatMenu;
public sealed partial class MenuController
{
 private WorldReportActions worldReport;
 [HideFromIl2Cpp]
 private void DrawWorldReport()
 {
  Text(28,230,866,69,MenuText.Get("采样当前场景的实体、组件清单、生命、位置和玩家物品，复制 JSON 供后续快照调查。"),18,false,true);
  Text(28,313,866,64,MenuText.Get("报告包含未覆盖状态；完整快照、读档与死亡恢复尚未实现。"),18,true,true);
  if(Button("worldReportCapture",28,400,415,MenuText.Get("采样场景状态"),SceneReady()&&!sceneCamera.Active&&!photo.HasState))
   Do(MenuText.Get("场景状态报告"),()=>{if(!SceneReady())throw new InvalidOperationException(MenuText.Get("先完成当前请求或恢复操作"));return worldReport.Capture();});
  if(Button("worldReportCopy",465,400,429,MenuText.Get("复制状态 JSON"),worldReport.HasReport))Do(MenuText.Get("场景状态报告"),worldReport.Copy);
  Text(28,452,866,43,worldReport.HasReport?worldReport.Summary:MenuText.Get("尚未采样当前场景"),18,false,true);
  if(Button("worldReportClear",28,514,250,MenuText.Get("清除状态报告"),worldReport.HasReport))Do(MenuText.Get("场景状态报告"),worldReport.Clear);
  Text(300,508,594,47,MenuText.Get("保留采样时刻；关闭菜单、失焦或切换场景后清除。"),16,false,true);
  Text(28,566,866,35,worldReport.Status,15,false,true);
 }
}
