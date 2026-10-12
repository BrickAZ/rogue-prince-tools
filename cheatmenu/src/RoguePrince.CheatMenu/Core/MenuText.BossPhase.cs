// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;
public static partial class MenuText
{
 private static void AddBossPhaseTranslations()
 {
  foreach(var pair in new Dictionary<string,string>{
   ["阶段练习请求无效或已有请求，未执行"]="Invalid phase or a request is already active; no action.",
   ["场地请求结果不确定；保留观察，不会重新加载"]="Arena request uncertain; observing without another reload.",
   ["正在重建 {0} 场地，目标阶段 {1}"]="Rebuilding {0} arena for phase {1}.",
   ["阶段练习作用域已变化；自动流程停止"]="Practice owner changed; automatic steps stopped.",
   ["阶段练习超时；未重复加载或发送伤害"]="Practice timed out; no repeated reload or damage.",
   ["场地重建未确认；阶段练习停止"]="Arena rebuild unconfirmed; phase practice stopped.",
   ["新场地 Boss 不唯一；未发送阶段伤害"]="New arena boss is ambiguous; no phase damage sent.",
   ["已绑定新 {0}，等待原生转换结束"]="New {0} bound; waiting for native transition.",
   ["原生阶段推进未确认；自动流程停止"]="Native phase advance unconfirmed; automatic steps stopped.",
   ["阶段索引不符合目标；自动流程停止"]="Unexpected phase index; automatic steps stopped.",
   ["已观察到目标阶段 {0}，原生转换动作已退出"]="Observed phase {0}; native transition action is inactive.",
   ["阶段请求结果不确定；保留观察，不重复伤害"]="Phase result uncertain; observing without repeated damage.",
   ["正在等待原生阶段 {0} → {1}"]="Waiting for native phase {0} -> {1}.",
   ["阶段练习读取失败；自动流程停止"]="Practice read failed; automatic steps stopped.",
   ["已停止阶段练习；已发生的重建与伤害不会撤销"]="Practice stopped; prior rebuild and damage are not undone.",
   ["选择阶段 1 / 2 / 3：重建当前原生场地，再逐段降低 Boss 生命触发转换。返回较早阶段也会重建；进度与奖励沿用原版。"]="Choose phase 1 / 2 / 3. Rebuild this native arena, then lower boss HP through each native transition. Earlier phases also rebuild; normal progress and rewards apply.",
   ["阶段 {0}"]="Phase {0}",
   ["重建到所选阶段"]="Rebuild to selected phase",
   ["停止观察不会撤销已发生的场地重建或伤害。转换演出由原版处理；同一实例直接倒退尚未接入。"]="Stopping does not undo arena reloads or damage. Native cutscenes continue; reversing the same boss instance is unavailable.",
   ["Boss 阶段"]="Boss phases",
   ["当前阶段不支持推进，未执行"]="Current phase cannot advance; no action.",
   ["阶段阈值已到达或无效，未执行"]="Threshold reached or invalid; no action.",
   ["阶段请求尚在观察或目标已失效"]="A phase request is pending or the target expired.",
   ["请进入正式 Boss 场地，未执行"]="Enter a native boss arena; no action.",
   ["当前房间没有支持的原生 Boss"]="No supported native boss in this room.",
   ["目标 Boss、脑组件或房间已变化，未执行"]="Boss, brain or room changed; no action.",
   ["Boss 尚未初始化、已死亡或 AI 暂停，未执行"]="Boss is not initialized, dead or AI is held; no action.",
   ["原生 Boss 调试入口未就绪，未执行"]="Native boss debug entry unavailable; no action.",
   ["原生入口未产生预期生命变化；阶段未确认"]="Native entry did not change HP as expected; phase unconfirmed.",
   ["已提交原生阶段伤害，等待 Boss 控制器回读"]="Native phase damage submitted; awaiting controller readback.",
   ["原生阶段请求结果不确定，保留观察；未重复发送伤害"]="Native phase result uncertain; observation retained, damage not repeated.",
   ["已观察到原生阶段推进：{0} → {1}"]="Native phase index advanced: {0} -> {1}.",
   ["阶段观察已取消；未向新对象写入"]="Observation cancelled; replacement objects were not written.",
   ["阶段观察超时；未重复发送伤害"]="Phase observation timed out; damage was not repeated.",
   ["原生阶段发生非预期变化；未重复发送伤害"]="Unexpected native phase change; damage was not repeated.",
   ["取消阶段观察"]="Cancel observation",
   ["取消只停止观察，不撤销已发生的原生伤害。"]="Cancel stops observation; it does not undo native damage.",
   ["推进下一阶段"]="Advance next phase",
   ["刷新当前 Boss"]="Refresh bosses",
   ["返回场地练习"]="Arena practice",
   ["原生推进会降低 Boss 生命以触发转换；只支持已初始化的阶段 1 或 2，最终阶段拒绝。"]="Native advance lowers boss HP to trigger transition. Only initialized phases 1 or 2 are eligible; the final phase is refused.",
   ["不支持倒退或任意阶段瞬切；演出、场地与召唤仍由原生控制器处理，需要实机确认。"]="No reverse or arbitrary phase jump. Native controllers handle cutscenes, arena and summons; live validation is pending.",
   ["{0} · 原生阶段 {1} · 生命 {2}/{3}"]="{0} | native phase {1} | HP {2}/{3}",
   ["观察中，请关闭菜单让原生 Boss 控制器运行。"]="Observing. Close the menu to let the native boss controller run.",
   ["镜头或截图模式尚未恢复"]="Restore camera or screenshot mode first."
  })English[pair.Key]=pair.Value;
 }
}
