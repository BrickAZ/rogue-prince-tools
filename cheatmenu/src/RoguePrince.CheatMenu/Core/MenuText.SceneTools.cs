// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
namespace RoguePrince.CheatMenu.Core;
public static partial class MenuText
{
 private static void AddSceneTranslations()
 {
  var text=new Dictionary<string,string>
  {
   ["受击显示读取失败，已关闭；可重新开启"]="Receiving shapes could not be read. Overlay disabled; you can enable it again.",
   ["读取当前已生成的碰撞体、攻击与受击形状；关闭菜单后显示。地图揭示仅作用于当前地图。"]="Read generated colliders, attack and receiving shapes. Close the menu to view them. Reveal affects the current map only.",
   ["原生受击碰撞体（盒 / 球 / 胶囊）"]="Native receiving shapes (box / sphere / capsule)",
   ["受击：蓝角色 / 红其他 · 形状不代表当前一定能受伤"]="Receiving: blue player / red others. Shapes do not guarantee damage can be received.",
   ["受击扫描半径30米，最多200个形状；网格/未知形状跳过并计数。无敌、队伍、条件与直接伤害无几何预测。"]="30 m radius; up to 200 receiving shapes. Mesh / unknown shapes are skipped and counted. No prediction of invulnerability, teams, conditions or direct hits.",
   ["受击形状 {0} · 跳过网格/未知 {1} · 扫描上限 {2} · 线条上限 {3}"]="Receiving shapes {0} / omitted mesh or unknown {1} / scan limit {2} / line limit {3}",
   ["已达"]="reached",["未达"]="not reached",
   ["竞技场队列与结算一起暂停，关闭后继续。另拦截鼹鼠及腐化诺盖的两个召唤入口；取消的单次召唤不补发。其他剧情刷怪器未接入。"]="Arena queues and completion pause together, then resume. Blocks Mole and two Corrupted Nogai summon entries. Cancelled summons are not replayed. Other scripted spawners are not connected.",
   ["Boss 场地练习"]="Boss arenas",["跨地图书签"]="Cross-map bookmark",["新局种子与等级"]="New-run settings",["场景显示"]="Scene overlays",["房间敌人控制"]="Room enemies",["镜头与飞行"]="Camera and flight",["后续遭遇"]="Next encounter",
   ["Boss 场地已失效，请刷新"]="Boss arena is unavailable. Refresh the list.", ["请求重建原生场地：{0}"]="Requested native arena reload: {0}",
   ["已记录本轮地图与安全位置"]="Saved this run's map and safe position.",["书签本轮或种子已变化，未执行"]="Bookmark run or seed changed. No action taken.",
   ["地图拓扑不可读，未执行"]="Map topology is unavailable. No action taken.",["重建地图与书签拓扑不一致，未写入旧坐标"]="Rebuilt map differs from the bookmark. Old coordinates were not applied.",
   ["场景请求失效，未自动重试"]="Scene request failed or expired. No automatic retry.",["原生场地已重建，保持当前配装"]="Native scene rebuilt. Current loadout follows normal scene transfer.",["已返回跨房间书签"]="Returned to the cross-room bookmark.",
   ["原生补丁不可用"]="Required native hooks are unavailable.",["普通怪物目标过滤已开启"]="Ordinary enemy target filtering enabled.",["普通怪物目标过滤已关闭"]="Ordinary enemy target filtering disabled.",
   ["当前房间竞技场与已接入召唤暂停"]="Room arena and supported summons paused.",["刷怪队列已恢复原生运行"]="Native spawn queues resumed.",["已请求原生死亡：{0} 个普通怪物"]="Requested native deaths for {0} ordinary enemies.",
   ["地图尚未就绪"]="Map is not ready.",["已揭示当前地图"]="Current map revealed.",["场景显示已切换"]="Scene overlay toggled.",["觉醒设置尚未就绪"]="Awakening settings are not ready.",
   ["已预约下一次原生新局的种子与等级"]="Seed and optional level reserved for the next native new run.",["新局预约已取消"]="New-run reservation cancelled.",["已恢复原生觉醒等级"]="Native awakening level restored.",
   ["起始关卡生成种子已设为 {0}"]="Starting biome generation seed set to {0}.",["新局预约失败，已取消"]="New-run reservation failed and was cancelled.",
   ["当前出口不支持该遭遇；未执行"]="Current exits do not support this encounter. No action taken.",["已预约原生出口候选：{0}"]="Reserved native exit candidate: {0}",["遭遇预约已取消"]="Encounter reservation cancelled.",
   ["遭遇预约已交给原生加载流程"]="Reserved encounter handed to native loading.",["进入其他关卡，遭遇预约已取消"]="Another scene is loading. Encounter reservation cancelled.",["本轮已变化，场景预约已取消"]="Run context changed. Scene reservations cancelled.",
   ["请先恢复镜头与飞行模式"]="Restore camera and flight mode first.",["当前场景缺少唯一的原生调试组件"]="This scene has no uniquely bound native debug component.",["镜头或输入状态不支持接管"]="Camera or input state does not support takeover.",
   ["镜头状态回读失败"]="Camera state readback failed.",["自由镜头已开启；使用原生镜头输入绑定"]="Free camera enabled. Use the native camera input bindings.",["飞行状态回读失败"]="Flight state readback failed.",
   ["飞行穿墙已开启；WASD 移动，重新打开菜单可退出"]="Flight and noclip enabled. Move with WASD; reopen the menu to exit.",["镜头或飞行恢复未完成，请重试"]="Camera or flight restoration is incomplete. Retry.",["镜头与飞行已恢复"]="Camera and flight restored.",
   ["出口 / 路线"]="Exit / route",["传送点"]="Teleport",["地图标记"]="Map marker",
   ["碰撞：青角色 / 橙怪物 / 黄地形 · 攻击：绿角色 / 粉其他 · 网格仅包围盒"]="Colliders: cyan player / orange enemies / yellow terrain. Attacks: green player / pink others. Meshes: bounds only.",
   ["场景工具"]="Scene tools",["先完成当前请求或恢复操作"]="Complete the pending request or recovery first.",
   ["进入原生 Boss 场地并保持当前配装；重复挑战会重新加载地图。死亡与奖励仍遵循原版。"]="Enter a native boss arena with the current loadout. Repeat reloads the map. Death and rewards follow normal game rules.",
   ["进入 / 重建场地"]="Enter / rebuild arena",["重打当前 Boss"]="Retry current boss",["选择完整配装"]="Choose full loadout",["先在配装页应用预设，再进入场地。"]="Apply a preset on the loadout page before entering the arena.",
   ["记录本轮的关卡、种子、地图指纹与安全位置。返回时先加载目标关卡，再核对拓扑；不匹配则只提示。"]="Capture this run's scene, seed, topology and safe position. Returning loads the destination, then checks topology. A mismatch only shows a message.",
   ["记录地图书签"]="Capture map bookmark",["返回地图书签"]="Return to map bookmark",["删除地图书签"]="Delete map bookmark",["书签已删除"]="Bookmark deleted.",
   ["仅本轮保留；新局清除。随机地图重建可能变化，因此不能保证任意旧坐标都能返回。"]="Kept only for this run; cleared on a new run. Randomly rebuilt maps can differ, so old coordinates cannot always be restored.",
   ["种子"]="Seed",["填入当前种子"]="Use current seed",["种子已填入"]="Seed filled in.",["预约原生新局"]="Reserve next new run",["覆盖觉醒等级（不改觉醒卡）"]="Override level (keep awakening cards)",["取消新局预约"]="Cancel new-run reserve",
   ["按原版方式开始下一轮后生效。可反复填入同一种子；其他随机源、解锁、觉醒卡和路线仍影响结果。"]="Applies when you start the next run normally. You can reuse the same seed; other RNG, unlocks, awakening cards and routes still affect results.",
   ["恢复原生觉醒等级"]="Restore native level",["本轮覆盖等级：{0}"]="Run level override: {0}",["觉醒等级：原生"]="Awakening level: native",
   ["自动放弃本轮并重新开局尚未接入；不以跳到第一关代替原生新局初始化。预约不会修改解锁或直接写存档。"]="Automatic abandon and restart is not connected yet. New runs must use normal initialization. The reservation does not change unlocks or directly write saves.",
   ["读取当前已生成的对象与原生攻击框；关闭菜单后显示。地图揭示仅作用于当前地图。"]="Read generated objects and active native attack shapes. Visible after closing the menu. Reveal affects the current map only.",
   ["揭示当前地图"]="Reveal current map",["出口 / 传送 / 特殊标记"]="Exit / teleport / special markers",["实体与地形碰撞框"]="Entity / terrain colliders",["当前原生攻击判定框"]="Active native attack shapes",
   ["实体 Collider 不等同于全部受击判定；最多显示附近 200 个碰撞体与 2500 条线。"]="Entity colliders do not represent every damage-receiving shape. Shows up to 200 nearby colliders and 2500 lines.",
   ["普通怪物不锁定玩家"]="Ordinary enemies ignore player",
   ["只过滤本房间普通怪物对玩家的目标；不关闭 AI，不取消已经发出的攻击，Boss 专用目标逻辑不在范围内。"]="Filters ordinary enemies' player targets in this room. AI remains active; attacks already launched persist. Boss-specific targeting is outside this scope.",
   ["冻结房间竞技场 / 已接入召唤"]="Pause room arena / supported summons",["冻结刷怪"]="Pause spawning",
   ["竞技场队列与完成结算一起暂停，关闭后继续。另阻止鼹鼠、腐化诺盖已定位召唤入口；其他剧情刷怪器未接入。"]="Arena queues and completion processing pause together, then resume when disabled. Also blocks verified Mole and Corrupted Nogai summon entries. Other scripted spawners are not connected.",
   ["全房间清除普通怪物（原生死亡）"]="Clear ordinary room enemies (native death)",["清除普通怪物"]="Clear ordinary enemies",
   ["开启原生自由镜头"]="Enable native free camera",["自由镜头"]="Free camera",["开启原生飞行穿墙"]="Enable native flight / noclip",["飞行穿墙"]="Flight / noclip",
   ["需要当前场景已加载对应调试组件。飞行使用 WASD；结束时若位置不安全，会先返回开启时的平地。失焦、换房、切关或死亡时恢复。"]="Requires the scene's native debug component. Flight uses WASD. An unsafe exit returns to the starting ground first. Restores on focus loss, room / scene changes or death.",
   ["恢复镜头与飞行"]="Restore camera and flight",["严格逐帧推进：未接入"]="Exact frame stepping: not connected",
   ["游戏逻辑、Unity 物理与动画有独立调度，暂无法保证同步只走一帧。当前速度调节仍可用于慢速观察。"]="Game logic, Unity physics and animation use separate schedules. A synchronized single step is not guaranteed yet. Existing speed controls support slow observation.",
   ["只预约当前原生出口支持的关卡；之后使用正常出口进入。若进入其他关卡或开始新局，预约取消。"]="Reserve only a candidate supported by this scene's native exits, then use a normal exit. Loading another scene or starting a new run cancels the reservation.",
   ["下一出口选择：{0}"]="Next exit choice: {0}",["预约遭遇"]="Reserve encounter",["当前没有可预约的原生出口候选"]="No supported native exit candidates are available.",["取消遭遇预约"]="Cancel encounter reserve",["营救预约设置"]="Rescue reservation settings",
   ["种子必须是 32 位有符号整数"]="Seed must be a signed 32-bit integer.",["觉醒等级超出当前游戏范围"]="Awakening level is outside this game's range."
  };foreach(var pair in text)English[pair.Key]=pair.Value;
 }
}
