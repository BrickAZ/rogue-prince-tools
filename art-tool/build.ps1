# Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
# SPDX-License-Identifier: GPL-3.0-only

param(
    [string]$Python = 'python',
    [string]$DependencyPath = (Join-Path $PSScriptRoot '.build-deps')
)
$ErrorActionPreference = 'Stop'
$env:PYTHONPATH = $DependencyPath
$env:ROGUE_BUILD_DEPS = $DependencyPath
$env:PYTHONIOENCODING = 'utf-8'
Push-Location -LiteralPath $PSScriptRoot
try {
    & $Python -m PyInstaller --noconfirm --distpath (Join-Path $PSScriptRoot 'dist') --workpath (Join-Path $PSScriptRoot '.build') (Join-Path $PSScriptRoot 'RogueArtExporter.spec')
    if ($LASTEXITCODE -ne 0) { throw 'Portable executable build failed' }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '打开美术资源库.cmd') -Destination (Join-Path $PSScriptRoot 'dist')
} finally { Pop-Location }
