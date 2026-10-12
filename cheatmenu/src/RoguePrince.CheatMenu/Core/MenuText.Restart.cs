// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;
public static partial class MenuText
{
 private static void AddRestartTranslations()
 {
  var additions=new Dictionary<string,string>
  {
   ["重开需要当前存活角色与已加载的战斗关卡"]="Restart requires a living current player in a loaded combat biome.",
   ["原生放弃界面缺失或不唯一；未执行"]="Native give-up view is missing or ambiguous. Nothing submitted.",
   ["原生放弃事件未绑定；未执行"]="Native give-up event is not bound. Nothing submitted.",
   ["先完成或取消当前重开及新局预约"]="Complete or cancel the current restart or new-run reservation first.",
   ["未找到原生据点开局路线；未执行"]="No native hub-to-new-run route was found. Nothing submitted.",
   ["角色、地图或暂停状态已变化；未放弃本轮"]="Player, map or pause state changed. This run was not abandoned.",
   ["原生放弃调用异常，结果不确定；继续观察且不会重发"]="Native give-up threw; result uncertain. Observing without resubmission.",
   ["已提交原生放弃；等待返回据点后自动开局"]="Native give-up submitted. Waiting for the hub before starting a run.",
   ["已观察到新局重置、起始种子与新地图加载"]="Observed new-run reset, starting seed and a newly loaded map.",
   ["重开观察超时；已停止自动流程，不会重试"]="Restart observation timed out. Automatic flow stopped; no retry.",
   ["重开观察已取消；已经发生的放弃或加载不会撤销"]="Restart observation cancelled. Submitted give-up or loading is not undone.",
   ["原生开局或生成回读不匹配；已停止自动流程"]="Native start or generation readback mismatched. Automatic flow stopped.",
   ["据点本轮尚未结束；停止自动开局"]="The previous run is still active in the hub. Automatic start stopped.",
   ["据点开局出口缺失或不唯一；停止自动开局"]="Hub start exit is missing or ambiguous. Automatic start stopped.",
   ["已提交一次原生开局；等待新局生成确认"]="One native start submitted. Waiting for new-run generation confirmation.",
   ["原生开局调用异常，继续观察且不会重发"]="Native start threw. Observing without resubmission.",
   ["确认放弃本轮并重开"]="Confirm abandon and restart",
   ["放弃本轮并重开"]="Abandon run and restart",
   ["停止自动重开观察"]="Stop restart observation",
   ["取消重开确认"]="Cancel restart confirmation",
   ["重开确认已取消；未放弃本轮"]="Restart confirmation cancelled. This run was not abandoned.",
   ["确认后按原版放弃本轮、返回据点并自动开局。取消观察不撤销已发生的放弃；本轮配装与资源按原版重置。"]="Confirm to abandon the run, return to the hub and start again using native rules. Cancelling observation does not undo abandonment; run equipment and resources reset normally."
  };
  foreach(var pair in additions)English[pair.Key]=pair.Value;
 }
}
