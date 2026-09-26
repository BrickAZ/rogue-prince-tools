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
    public const string Version = "0.1.8";
    internal static ConfigEntry<string> Language;
    internal static ManualLogSource MenuLog;
    internal static ConfigEntry<KeyCode> Hotkey;
    internal static ConfigEntry<float> Scale;
    internal static ConfigEntry<float> PositionX;
    internal static ConfigEntry<float> PositionY;
    private Harmony harmony;
    public override void Load()
    {
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
        AddComponent<MenuController>();
        LogEvent("plugin_loaded version=" + Version + " key=" + Hotkey.Value + " language=" + MenuText.LanguageCode);
    }
    internal static void LogEvent(string message) => MenuLog.LogInfo("[RPCheat] " + message);
    public override bool Unload()
    {
        MenuController.Instance?.Shutdown(false);
        harmony?.UnpatchSelf();
        return true;
    }
}
