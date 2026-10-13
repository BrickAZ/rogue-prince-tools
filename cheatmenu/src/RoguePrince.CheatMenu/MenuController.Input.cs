// Copyright (C) 2026 BrickZhou. SPDX-License-Identifier: GPL-3.0-only
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine.InputSystem;
namespace RoguePrince.CheatMenu;

public sealed partial class MenuController
{
    [HideFromIl2Cpp]
    internal static bool ShouldBlockNativePause()
        => Instance!=null && (Instance.visible || Instance.inputLease.Active);

    [HideFromIl2Cpp]
    private static bool ConfirmStillHeld()
    {
        var pad=Gamepad.current;var keyboard=Keyboard.current;var mouse=Mouse.current;
        return (pad!=null&&(pad.buttonSouth.isPressed||pad.buttonEast.isPressed||pad.rightStickButton.isPressed)) ||
            (keyboard!=null&&(keyboard.enterKey.isPressed||keyboard.spaceKey.isPressed||keyboard.escapeKey.isPressed)) ||
            (mouse!=null&&mouse.leftButton.isPressed);
    }
}

// Game input actions precede IMGUI's Event.Use. This scoped prefix prevents the
// same Escape/B press from opening native pause while CheatMenu owns the input.
[HarmonyPatch(typeof(PauseInputController),nameof(PauseInputController.OnInputActionPerformed))]
internal static class MenuPauseInputPatch
{
    internal static bool Available;
    [HarmonyPrefix]
    private static bool Prefix()
    {
        if(!MenuController.ShouldBlockNativePause())return true;
        Plugin.LogEvent("menu_pause_input_suppressed scope=cheatmenu_input_lease");
        return false;
    }
}
