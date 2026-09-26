@echo off
chcp 65001 >nul
rem Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
rem SPDX-License-Identifier: GPL-3.0-only
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Download-BepInEx.ps1"
if errorlevel 1 (
    echo 下载或校验失败。请查看上方信息。Download or verification failed.
    pause
    exit /b 1
)
echo 请关闭游戏，把 Downloads 中 ZIP 里的文件解压到游戏根目录。
echo Close the game, then extract the ZIP contents from Downloads into the game root.
pause
