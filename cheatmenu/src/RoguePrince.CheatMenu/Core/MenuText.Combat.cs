// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;
public static partial class MenuText
{
    private static void AddCombatTranslations()
    {
        var additions=new Dictionary<string,string>
        {
            ["工具无能量消耗"]="Tool no energy cost",
            ["当前工具没有就绪的原生能量槽消耗"]="No ready native energy-slot cost on this tool.",
            ["攻速只推进当前装备绑定的攻击动作；工具只绕过自身声明的冷却与能量槽消耗。"]="Attack scaling advances equipped attack actions. Tool bypass applies to its own declared cooldowns and energy-slot cost.",
            ["无消耗不修改共享资源。未识别的弹药、蓄力与特殊动作条件继续遵守原版规则。"]="No-cost mode does not change shared assets. Unidentified ammo, charge and special action conditions retain native rules.",
            ["切换记录"]="Recording toggled",["出口加载待确认"]="Exit loading pending",["原版出口已完成"]="Native exit completed",["手动停止"]="Stopped manually",
            ["切换房间"]="Room changed",["传送"]="Teleported",["清理"]="Cleared",["完成"]="Completed",
            ["加载"]="Loading",["死亡"]="Death",["重置"]="Reset",
            ["开始波次"]="Waves started",["传送或跳关"]="Teleport or menu travel",
            ["装备已被其他操作替换，请先移开冲突装备再恢复"]="Equipment was replaced elsewhere. Remove the conflicting equipment before retrying recovery.",
            ["伤害与生存"]="Damage / survival",["效果管理"]="Effects",["完整配装"]="Loadouts",["移动与工具"]="Motion / tools",["波次与练习"]="Waves / practice",["奖励容器"]="Reward containers",
            ["一击必杀"]="One-hit kills",["不死（仍受击）"]="Undying (still takes hits)",["伤害统计"]="Damage recording",["清空伤害统计"]="Clear damage stats",["使用选中的自有怪物"]="Use selected owned enemy",
            ["累计 {0:0.##} · 最后 {1:0.##} · 命中 {2}"]="Total {0:0.##} / Last {1:0.##} / Hits {2}",["有效战斗 {0:0.##} 秒 · DPS {1:0.##} · 目标 {2:0.##}"]="Active {0:0.##} s / DPS {1:0.##} / Target {2:0.##}",
            ["玩家伤害倍率"]="Player damage scale",["应用（0.1–10 倍）"]="Apply (0.1-10x)",["应用（0.25–3 倍）"]="Apply (0.25-3x)",["恢复全部战斗参数"]="Reset combat parameters",
            ["只修改当前玩家造成的扣血；原生不可致死规则保留。不死保留普通受击和击退。"]="Only damage dealt by the current player is scaled. Native nonlethal rules remain. Undying retains normal hit reactions and knockback.",
            ["伤害按实际生命减少统计；护盾吸收、免疫和治疗不计入。菜单、暂停和加载期间停止计时，特殊剧情死亡仍需实测。"]="Counts actual HP lost, excluding shields, immunity and healing. Timing pauses during menus, pause and loading. Special scripted deaths need live testing.",
            ["伤害统计已清空"]="Damage stats cleared.",["伤害倍率 {0:0.##} 倍"]="Damage scale {0:0.##}x",["统计目标"]="Recording target",["统计目标已更新"]="Recording target updated.",["战斗参数"]="Combat parameters",["战斗参数已恢复"]="Combat parameters reset.",
            ["已开启"]="Enabled.",["已关闭"]="Disabled.",["倍率超出范围"]="Multiplier out of range.",["输入值超出范围"]="Value out of range.",["请输入范围内的整数"]="Enter an integer within the range.",["当前战斗作用域未就绪"]="The current combat scope is not ready.",
            ["刷新已加载效果"]="Reload loaded effects",["请选择已加载的效果"]="Select a loaded effect",["效果库"]="Effect catalog",["已加载 {0} 种效果"]="Loaded {0} effects.",["目标层数"]="Target stacks",["时长（秒）"]="Duration (s)",
            ["设置效果 / 层数 / 时长"]="Set effect, stacks and duration",["移除选中效果"]="Remove selected effect",["移除当前所有效果"]="Remove all current effects",
            ["清空也会移除游戏增益；使用原生效果生命周期与免疫规则。"]="Clearing also removes game buffs. Uses native effect lifecycle and immunity rules.",
            ["效果组件尚未就绪"]="Effect component is not ready.",["时长请输入 0.1–3600 秒"]="Duration must be 0.1-3600 seconds.",["层数超出效果原生上限"]="Stacks exceed the native effect limit.",["角色当前不能获得该效果"]="The player cannot receive this effect now.",
            ["效果回读失败，原生规则未保留目标层数"]="Effect readback failed: native rules did not retain the requested stacks.",["效果层数与时长已设置"]="Effect stacks and duration set.",["原生效果移除未完成"]="Native effect removal is incomplete.",["选中效果已移除"]="Selected effect removed.",["已请求移除 {0} 种当前效果"]="Requested removal of {0} current effects.",
            ["效果回读失败，原生规则未保留目标层数或时长"]="Effect readback failed: native rules did not retain the requested stacks or duration.",
            ["保存当前完整配装"]="Save current loadout",["请选择完整配装"]="Select a loadout",["武器 {0} 级 · 工具 {1} 级 · 徽章 {2} 枚"]="Weapon tier {0} / Tool tier {1} / {2} medallions",
            ["应用完整配装"]="Apply loadout",["重试恢复原配装"]="Retry original loadout",["导入 JSON"]="Import JSON",["删除选中配装"]="Delete selected loadout",
            ["不增加徽章容量；不兼容配装拒绝执行，替换失败恢复原实例。"]="Medallion capacity is unchanged. Incompatible loadouts are rejected; replacement failures restore original instances.",
            ["配装文件只读"]="Loadout file is read-only",["完整配装已保存"]="Loadout saved.",["完整配装已应用"]="Loadout applied.",["配装已导入，应用时检查兼容性"]="Loadout imported; compatibility is checked on apply.",["配装 JSON 已复制"]="Loadout JSON copied.",["配装已删除"]="Loadout deleted.",
            ["配装恢复尚未完成"]="Loadout restoration is pending.",["配装作用域或词缀恢复未就绪"]="Loadout scope or affix recovery is not ready.",["主武器和工具必须同时装备"]="Equip both a main weapon and a tool first.",["配装补丁或恢复尚未就绪"]="Loadout patch or recovery is not ready.",
            ["主武器与工具类别不匹配"]="Main weapon and tool categories do not match.",["预设徽章已失效"]="A preset medallion is invalid.",["预设徽章超过当前栏位数"]="Preset medallions exceed current slot capacity.",["装备状态变化，请重试"]="Equipment changed; retry.",
            ["配装操作中角色或房间发生变化"]="Player or room changed during the loadout operation.",["原生装备添加失败"]="Native equipment addition failed.",["配装实例回读失败"]="Loadout instance readback failed.",["配装失败且恢复未完成，请重试恢复"]="Loadout failed and restoration is pending; retry recovery.",
            ["没有待恢复配装"]="No loadout recovery is pending.",["不能向新角色恢复旧配装"]="An old loadout cannot be restored onto a new player.",["原配装已恢复"]="Original loadout restored.",["配装版本或内容无效"]="Invalid loadout version or content.",["配装文本为空或过大"]="Loadout text is empty or too large.",["配装文本无效"]="Invalid loadout text.",
            ["移动速度倍率"]="Movement scale",["移动速度"]="Movement speed",["攻击速度倍率"]="Attack scale",["攻击速度"]="Attack speed",["速度读数 {0:0.##}"]="Speed readout {0:0.##}",["动作倍率 {0:0.##}"]="Action scale {0:0.##}",
            ["无限空中跳跃"]="Infinite air jumps",["玩家无击退"]="Player no knockback",["工具无冷却"]="Tool no cooldown",["重置当前工具冷却"]="Reset current tool cooldown",
            ["攻速只推进当前装备绑定的攻击动作；其他角色和共享动作资源不改动。工具只绕过自身动作声明的冷却。"]="Attack scaling advances only actions bound to equipped weapons. Other actors and shared assets remain unchanged. Tool bypass applies only to its declared action cooldowns.",
            ["能量限制使用角色页的无限能量。未识别的弹药、蓄力或特殊动作条件继续遵守原版规则。"]="Energy restrictions use Infinite Energy on the Player page. Unidentified ammo, charge and special action conditions retain native rules.",
            ["移动倍率已设置"]="Movement scale set.",["绑定武器动作倍率已设置"]="Bound weapon action scale set.",["工具冷却"]="Tool cooldown",["当前工具没有可识别的原生冷却"]="No identifiable native cooldown on this tool.",["工具冷却回读失败"]="Tool cooldown readback failed.",["当前工具冷却已重置"]="Current tool cooldown reset.",
            ["波次与木桩"]="Waves / dummy",["练习统计"]="Practice statistics",["选中普通怪物"]="Selected ordinary enemy",["波数"]="Waves",["每波数量"]="Per wave",["开始怪物波次"]="Start enemy waves",["怪物波次"]="Enemy waves",
            ["第 {0} / {1} 波 · 已生成 {2} · 存活 {3}"]="Wave {0}/{1} / Spawned {2} / Alive {3}",["停止波次"]="Stop waves",["停止并清理自有怪物"]="Stop and clear owned enemies",["选中目标设为固定木桩"]="Set selected target as dummy",["关闭木桩模式"]="Turn off dummy mode",
            ["木桩目标在自有怪物页选择。波次仅支持普通怪物；清理、切房、死亡和失焦会停止。"]="Select a dummy on the Owned Enemies page. Waves support ordinary enemies only; clearing, room changes, death or loss of focus stop them.",
            ["全部波次已完成"]="All waves completed.",["波次已停止：目标被清理或失效"]="Waves stopped: a target was cleared or invalidated.",["木桩已清理，自动恢复已停止"]="Dummy cleared; automatic recovery stopped.",["请先选择普通怪物"]="Select an ordinary enemy first.",
            ["怪物波次已开始"]="Enemy waves started.",["波次已停止，存活目标可在自有怪物页清理"]="Waves stopped. Clear living targets on the Owned Enemies page.",["木桩控制暂不可用"]="Dummy control is unavailable.",["请先选择自有普通怪物"]="Select an owned ordinary enemy first.",
            ["木桩已固定，受击后恢复生命并统计伤害"]="Dummy anchored; HP recovers after hits and damage is recorded.",["木桩模式已关闭"]="Dummy mode disabled.",["训练生成失败，已停止"]="Training spawn failed; stopped.",["训练木桩"]="Training dummy",["训练清理"]="Training cleanup",["波次 1–20，每波 1–10 只"]="1-20 waves; 1-10 enemies per wave.",
            ["自动房间分段"]="Automatic room segments",["复制统计 JSON"]="Copy stats JSON",["清空练习统计"]="Clear practice stats",["练习统计已清空"]="Practice statistics cleared.",["练习统计已复制"]="Practice statistics copied.",
            ["当前 {0} · {1:0.##} 秒 · 受击 {2}"]="Current {0} / {1:0.##} s / Hits taken {2}",["受击"]="Hits taken",
            ["记录分段、受击与死亡位置；波次全部完成或原版出口目标加载确认后记最佳，跳关和手动停止不算成功。"]="Records segments, hits taken and death locations. Best times require all waves completed or confirmation of a native exit destination; menu travel and manual stops are not successes.",
            ["物品页选中"]="Item selected on Items page",["奖励等级"]="Reward tier",["前往物品清单选择奖励"]="Choose a reward on the Items page",["生成包含所选物品的免费宝箱"]="Spawn free chest with selected item",["生成包含当前词缀草稿的宝箱"]="Spawn chest with current affix draft",["清理自有奖励容器"]="Clear owned reward containers",
            ["走近后用原版交互开箱，再拾取奖励。免费宝箱资源不支持时提示退出；切房后清理自有容器。"]="Approach and open using native interaction, then pick up the reward. Unsupported free chest resources abort with a message. Room changes clear owned containers.",
            ["无法唯一确定当前角色的拾取物预制体"]="Cannot uniquely resolve the current player's pickup prefab.",["奖励容器暂不可用，最多保留 8 个"]="Reward container unavailable; at most 8 are retained.",["当前免费宝箱预制体未唯一加载"]="No unique free chest prefab is loaded.",
            ["该宝箱包含钥匙或宫殿进度限制，未生成"]="This chest has key or palace progression restrictions; nothing spawned.",["奖励拾取物缺少原生物品组件"]="Reward pickup lacks the native item component.",["免费宝箱内容组件不唯一，已撤销"]="Free chest content component is not unique; cancelled.",
            ["奖励容器内容回读失败，已撤销"]="Reward content readback failed; cancelled.",["指定奖励宝箱已生成，请使用原版交互开箱"]="Reward chest spawned; open it using native interaction.",["宝箱已生成指定拾取物，等待玩家拾取"]="Chest created the selected pickup; awaiting collection.",["已清理本菜单的奖励容器"]="This menu's reward containers cleared.",
            ["death"]="Death",["reset"]="Reset",["cancel"]="Cancelled",["complete"]="Complete",["cleared"]="Cleared",["loading"]="Loading",["room change"]="Room change",["teleport"]="Teleport",["native exit"]="Native exit",["native exit pending"]="Native exit pending",["toggle"]="Toggle",["wave start"]="Wave start",["teleport / menu travel"]="Teleport / menu travel"
        };
        foreach(var row in additions)English[row.Key]=row.Value;
    }
}
