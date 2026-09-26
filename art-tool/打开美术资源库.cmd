@echo off
chcp 65001 >nul
rem Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
rem SPDX-License-Identifier: GPL-3.0-only
if not exist "%~dp0ArtExports\index.html" (
    echo 还没有导出结果，请先运行“一键导出美术资源.exe”。
    echo BrickZhou/青春啊砖在he边看月亮
    pause
    exit /b 1
)
start "" "%~dp0ArtExports\index.html"
