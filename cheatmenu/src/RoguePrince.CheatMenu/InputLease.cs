// Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
// SPDX-License-Identifier: GPL-3.0-only
// Additional game linking permission: see GAME-LINKING-EXCEPTION.txt.

using RoguePrince.CheatMenu.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using GamePlayerInput = MotherBase.ActionFramework.PlayerInput;

namespace RoguePrince.CheatMenu;

internal sealed class InputLease
{
    private GamePlayerInput input;
    private bool resumeListener;
    private bool pauseOwned;
    private float previousScale;
    private CursorLockMode cursorLock;
    private bool cursorVisible;
    private bool cursorOwned;
    private readonly List<EventSystem> disabledEventSystems = new();
    private float nextUiScan;

    public void Acquire(GameSession game)
    {
        if (!pauseOwned && Time.timeScale > 0) { previousScale = Time.timeScale; Time.timeScale = 0; pauseOwned = true; }
        if (!cursorOwned) { cursorLock = Cursor.lockState; cursorVisible = Cursor.visible; cursorOwned = true; }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (Time.realtimeSinceStartup >= nextUiScan)
        {
            nextUiScan = Time.realtimeSinceStartup + 0.5f;
            foreach (var system in UnityEngine.Object.FindObjectsOfType<EventSystem>())
                if (system != null && system.enabled)
                {
                    disabledEventSystems.Add(system);
                    system.enabled = false;
                }
        }
        var next = game.Player != null && !game.Player.destroyed ? game.Player.GetComponent<GamePlayerInput>() : null;
        if (input != null && (next == null || next.Pointer != input.Pointer)) ReleaseInput();
        if (next == null) return;
        if (input == null) { input = next; resumeListener = next._isListeningUnityInput; }
        if (input._isListeningUnityInput) input.StopListenUnityInput();
        input.ResetInputStates();
        input.moveInputValue = Vector2.zero;
        input.aimInputValue = Vector2.zero;
        input.activeInputs = input.lastActiveInputs = 0;
    }

    private void ReleaseInput()
    {
        if (input != null)
        {
            input.ResetInputStates();
            if (resumeListener && input.gameObject.activeInHierarchy) input.StartListenUnityInput();
        }
        input = null;
        resumeListener = false;
    }

    public void Release(bool quitting = false)
    {
        try { if (!quitting) ReleaseInput(); }
        catch (Exception e) { Plugin.MenuLog.LogWarning(MenuText.Get("归还角色输入：") + e.Message); }
        finally
        {
            input = null;
            resumeListener = false;
            if (!quitting)
                foreach (var system in disabledEventSystems)
                    try { if (system != null) system.enabled = true; }
                    catch (Exception e) { Plugin.MenuLog.LogWarning(MenuText.Get("归还界面输入：") + e.Message); }
            disabledEventSystems.Clear();
            nextUiScan = 0;
            try { if (pauseOwned && Time.timeScale == 0) Time.timeScale = previousScale; }
            catch (Exception e) { Plugin.MenuLog.LogWarning(MenuText.Get("归还暂停状态：") + e.Message); }
            pauseOwned = false;
            try { if (cursorOwned) { Cursor.lockState = cursorLock; Cursor.visible = cursorVisible; } }
            catch (Exception e) { Plugin.MenuLog.LogWarning(MenuText.Get("归还鼠标状态：") + e.Message); }
            cursorOwned = false;
        }
    }
}
