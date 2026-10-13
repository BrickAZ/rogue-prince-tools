// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
// Additional game linking permission: see GAME-LINKING-EXCEPTION.txt.

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using RoguePrince.CheatMenu.Core;
using UnityEngine;

namespace RoguePrince.CheatMenu;

[BepInPlugin(Id, "RoguePrince.CheatMenu", Version)]
public sealed class Plugin : BasePlugin
{
    public const string Id = "local.rogueprince.cheatmenu";
    public const string Version = "0.1.36";
    internal static readonly RecentMenuLog RecentLog = new(64);
    internal static ConfigEntry<string> Language;
    internal static ManualLogSource MenuLog;
    internal static ConfigEntry<KeyCode> Hotkey;
    internal static ConfigEntry<float> Scale;
    internal static ConfigEntry<float> PositionX;
    internal static ConfigEntry<float> PositionY;
    private Harmony harmony;
    public override void Load()
    {
        MenuPauseInputPatch.Available=false;
        MenuLog = Log;
        Language = Config.Bind("Interface", "Language", "zh-CN", "语言/language: zh-CN = 中文, en-US = English. Applies to this menu only.");
        MenuText.SetLanguage(Language.Value);
        Language.Value = MenuText.LanguageCode;
        Hotkey = Config.Bind("Interface", "ToggleKey", KeyCode.F9, "菜单快捷键；F8 留给原有 HUD");
        if (Hotkey.Value < KeyCode.F1 || Hotkey.Value > KeyCode.F12 || Hotkey.Value == KeyCode.F8) Hotkey.Value = KeyCode.F9;
        Scale = Config.Bind("Interface", "Scale", 1f, new ConfigDescription("界面缩放", new AcceptableValueRange<float>(0.65f, 1.5f)));
        PositionX = Config.Bind("Interface", "X", 380f, "菜单位置 X");
        PositionY = Config.Bind("Interface", "Y", 90f, "菜单位置 Y");
        try
        {
            harmony = new Harmony(Id);
            harmony.PatchAll(typeof(InfiniteEnergyPatch));
            GameplayActions.EnergyPatchAvailable = true;
        }
        catch (Exception e) { Log.LogError("无限能量补丁未启用：" + e); }
        try
        {
            harmony ??= new Harmony(Id);
            harmony.PatchAll(typeof(MobPreAiPatch));
            harmony.PatchAll(typeof(MobPostAiPatch));
            harmony.PatchAll(typeof(MobDestroyedPatch));
            harmony.PatchAll(typeof(BossEncounterEffectPatch));
            harmony.PatchAll(typeof(BossCutsceneActorPatch));
            MobAiControl.Available = true;
        }
        catch (Exception e) { Log.LogError("怪物召唤控制未启用：" + e); }
        try
        {
            harmony ??= new Harmony(Id);
            harmony.PatchAll(typeof(NextRescueGeneratePatch));
            harmony.PatchAll(typeof(NextRescueParametersPatch));
            harmony.PatchAll(typeof(NextRescueRunResetPatch));
            NextRescueActions.Available = true;
        }
        catch (Exception e) { Log.LogError("营救生成控制未启用：" + e); }
        try
        {
            harmony ??= new Harmony(Id);
            harmony.PatchAll(typeof(PhotoCameraUpdatedPatch));
            harmony.PatchAll(typeof(PhotoCameraDisabledPatch));
            PhotoActions.Available=true;
        }
        catch(Exception e){Log.LogError("Screenshot camera confirmation patch unavailable: "+e);}
        PatchGroup("combat",()=>CombatActions.DamageAvailable=true,typeof(CombatDamagePatch),typeof(CombatHealthPatch),typeof(CombatKillPatch));
        PatchGroup("motion",()=>CombatActions.MotionAvailable=true,typeof(CombatBumpPatch),typeof(CombatJumpPatch),typeof(CombatJumpTriggerPatch),typeof(CombatMovementScopePatch),typeof(CombatMovePatch));
        PatchGroup("attack",()=>CombatActions.AttackAvailable=true,typeof(CombatActorPatch),typeof(CombatAttackStepPatch));
        PatchGroup("tool",()=>CombatActions.ToolAvailable=true,typeof(CombatToolPatch),typeof(CombatToolCostCheckPatch),typeof(CombatToolCostFirePatch));
        PatchGroup("tool_uses",()=>ToolLimitActions.Available=true,typeof(ToolUsesPatch));
        PatchGroup("practice",()=>CombatActions.PracticeAvailable=true,typeof(CombatPracticeExitPatch));
        PatchGroup("loadout",()=>LoadoutActions.Available=true,typeof(LoadoutAffixPickupPatch),typeof(LoadoutAffixRemovePatch));
        PatchGroup("reward",()=>RewardActions.Available=true,typeof(RewardDefinePatch),typeof(RewardDeliveryPatch));
        PatchGroup("scene_target",()=>SceneActions.TargetAvailable=true,typeof(SceneTargetPatch),typeof(SceneTargetPrePatch),typeof(SceneTargetPostPatch));
        PatchGroup("boss_target",()=>BossTargetActions.Available=true,typeof(NogaiTargetOrientationPatch),typeof(NogaiTargetWalkPatch),typeof(NogaiTargetAttackPatch),typeof(KaaliTargetOrientationPatch),typeof(KaaliTargetMeleePatch),typeof(KaaliTargetRangePatch));
        PatchGroup("boss_target_attack",()=>BossTargetActions.AdditionalAttackAvailable=true,typeof(BaatarTargetAttackPatch),typeof(CorruptedNogaiTargetAttackPatch));
        PatchGroup("boss_target_extended",()=>BossTargetActions.ExtendedAvailable=true,typeof(AltanTargetAttackPatch),typeof(BerudeTargetFillerPatch));
        PatchGroup("scene_spawn",()=>SceneActions.SpawnAvailable=true,typeof(SceneArenaSpawnPatch),typeof(SceneNogaiSpawnPatch),typeof(SceneMoleSpawnPatch),typeof(SceneSingleSummonPatch));
        PatchGroup("scene_run",()=>RunTools.Available=true,typeof(RunSeedGeneratePatch),typeof(RunDifficultyPatch),typeof(SceneRunResetPatch),typeof(SceneEncounterLoadPatch));
        PatchGroup("run_restart",()=>RunRestartActions.Available=true,typeof(RunRestartGenerationPatch));
        PatchGroup("frame_observation",()=>FrameObservationActions.Available=true,typeof(FrameObservationFixedPatch),typeof(FrameObservationUpdatePatch),typeof(FrameObservationLatePatch));
        PatchGroup("menu_pause_input",()=>MenuPauseInputPatch.Available=true,typeof(MenuPauseInputPatch));
        AddComponent<MenuController>();
        LogEvent("plugin_loaded version=" + Version + " key=" + Hotkey.Value + " language=" + MenuText.LanguageCode);
    }
    internal static void LogEvent(string message) { RecentLog.Add(message); MenuLog.LogInfo("[RPCheat] " + message); }
    private void PatchGroup(string name,Action available,params Type[] patches)
    {try{harmony??=new Harmony(Id);foreach(var patch in patches)harmony.PatchAll(patch);available();LogEvent("patch_group_ready "+name);}catch(Exception e){Log.LogError("[RPCheat] patch_group_unavailable "+name+" "+e);}}
    public override bool Unload()
    {
        try{MenuController.Instance?.Shutdown(false);}
        catch(Exception e){Log.LogWarning("CheatMenu unload deferred: "+e);return false;}
        harmony?.UnpatchSelf();
        MenuPauseInputPatch.Available=false;
        CombatActions.Current=null;RewardActions.Current=null;
        SceneActions.Current=null;RunTools.Current=null;SceneActions.TargetAvailable=SceneActions.SpawnAvailable=RunTools.Available=false;
        RunRestartActions.Current=null;RunRestartActions.Available=false;
        FrameObservationActions.Current=null;FrameObservationActions.Available=false;
        ToolLimitActions.Available=false;
        BossTargetActions.Current=null;BossTargetActions.Available=BossTargetActions.AdditionalAttackAvailable=BossTargetActions.ExtendedAvailable=false;
        return true;
    }
}

