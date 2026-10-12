// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using Il2CppInterop.Runtime.Attributes;
using RoguePrince.CheatMenu.Core;
namespace RoguePrince.CheatMenu;
public sealed partial class MenuController
{
    private PlayerCheckpointActions playerCheckpoint;
    [HideFromIl2Cpp]
    private bool CheckpointReady() => SceneReady() && !sceneCamera.Active && !photo.HasState && !actions.InfiniteEnergy && !game.InventoryRecoveryBlocked;
    [HideFromIl2Cpp]
    private void DrawPlayerCheckpoint()
    {
        var ready=CheckpointReady();
        Text(28,230,866,66,MenuText.Get("记录并手动恢复当前房间的角色安全位置、生命和能量。只保存在本次会话内存中。"),18,false,true);
        Text(28,310,866,64,MenuText.Get("怪物、Boss、物品、地图、计时和状态效果不会回退；尚不支持死亡回档，也不隔离原版自动保存。"),17,true,true);
        Text(28,390,866,38,playerCheckpoint.Summary,17,false,true);
        if(Button("checkpointCapture",28,443,415,MenuText.Get("记录角色检查点"),ready,false,17))
            Do(MenuText.Get("角色练习检查点"),playerCheckpoint.Capture);
        if(Button("checkpointRestore",465,443,429,MenuText.Get("恢复位置、生命与能量"),ready&&playerCheckpoint.HasCheckpoint,false,17))
            Do(MenuText.Get("角色练习检查点"),()=>playerCheckpoint.Restore(ReleaseForTravel));
        if(Button("checkpointClear",28,491,415,MenuText.Get("清除角色检查点"),playerCheckpoint.HasCheckpoint&&!playerCheckpoint.RecoveryPending,false,17))
            Do(MenuText.Get("角色练习检查点"),playerCheckpoint.Clear);
        if(Button("checkpointRetry",465,491,429,MenuText.Get("重试请求前状态恢复"),playerCheckpoint.RecoveryPending,false,17))
            Do(MenuText.Get("角色练习检查点"),()=>{ReleaseForTravel();return playerCheckpoint.RetryRecovery();});
        Text(28,538,866,43,MenuText.Get("记录与恢复都需安全平地。换房、换角色、上限变化、加载、死亡或失焦后检查点失效。"),15,false,true);
        Text(28,587,866,33,playerCheckpoint.Status,14,!playerCheckpoint.Error,true);
    }
}
