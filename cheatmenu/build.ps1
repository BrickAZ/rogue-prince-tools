# Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
# SPDX-License-Identifier: GPL-3.0-only
# Additional game linking permission: see GAME-LINKING-EXCEPTION.txt.

param(
    [Parameter(Mandatory=$true)][string]$GameDirectory,
    [string]$Dotnet = 'dotnet'
)
$ErrorActionPreference = 'Stop'
Push-Location -LiteralPath $PSScriptRoot
try {
    & $Dotnet build 'src/RoguePrince.CheatMenu' -c Release --nologo "-p:GameDir=$GameDirectory"
    if ($LASTEXITCODE -ne 0) { throw 'CheatMenu build failed' }
    & $Dotnet run --project 'tests/RoguePrince.CheatMenu.Tests' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'CheatMenu offline checks failed' }
} finally { Pop-Location }
