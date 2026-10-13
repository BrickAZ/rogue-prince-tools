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
    & $Dotnet run --project 'tests/RoguePrince.SceneBoundary.Tests' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'CheatMenu scene-boundary checks failed' }
    & $Dotnet run --project 'tests/RoguePrince.BossPhase.Tests' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'CheatMenu boss-phase checks failed' }
    & $Dotnet run --project 'tests/RoguePrince.RestartBoundary.Tests' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'CheatMenu native restart checks failed' }
    & $Dotnet run --project 'tests/RoguePrince.WorldReport.Tests' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'CheatMenu scene report checks failed' }
    & $Dotnet run --project 'tests/RoguePrince.OverlayBoundary.Tests' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'CheatMenu receiver overlay checks failed' }
    & $Dotnet run --project 'tests/RoguePrince.DamageStatistics.Tests' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'CheatMenu damage statistics checks failed' }
    & $Dotnet run --project 'tests/RoguePrince.ToolLimits.Tests' -c Release --nologo "-p:GameDir=$GameDirectory"
    if ($LASTEXITCODE -ne 0) { throw 'CheatMenu tool limit checks failed' }
    & $Dotnet run --project 'tests/RoguePrince.BossTargets.Tests' -c Release --nologo "-p:GameDir=$GameDirectory"
    if ($LASTEXITCODE -ne 0) { throw 'CheatMenu boss target checks failed' }
    & $Dotnet run --project 'tests/RoguePrince.FrameObservation.Tests' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'CheatMenu native frame observation checks failed' }
    & $Dotnet run --project 'tests/RoguePrince.AbilityLoadout.Tests' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'CheatMenu ability loadout checks failed' }
    & $Dotnet run --project 'tests/RoguePrince.PlayerCheckpoint.Tests' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'CheatMenu player checkpoint checks failed' }
    & $Dotnet run --project 'tests/RoguePrince.LocalizedText.Tests' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'CheatMenu localized text checks failed' }
    & $Dotnet run --project 'tests/RoguePrince.BossSpawn.Tests' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'CheatMenu Boss summon bootstrap checks failed' }
    & $Dotnet run --project 'tests/RoguePrince.MenuRendering.Tests' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'CheatMenu rendering and input recovery checks failed' }
    & $Dotnet run --project 'tests/RoguePrince.InputLease.Tests' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'CheatMenu native pause input scope checks failed' }
} finally { Pop-Location }

