# Copyright (C) 2026 BrickZhou/青春啊砖在he边看月亮
# SPDX-License-Identifier: GPL-3.0-only
# This download helper is original code; BepInEx belongs to its own authors.
param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'Downloads'))
$ErrorActionPreference = 'Stop'
$uri = 'https://builds.bepinex.dev/projects/bepinex_be/788/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788%2B5b766a3.zip'
$name = 'BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.788+5b766a3.zip'
$expected = 'F4CC496BD098A0DF4164B81E3737297707F13A47C2478DBA2F60EEFAB784817A'
$null = New-Item -ItemType Directory -Path $OutputDirectory -Force
$target = Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) $name
if (Test-Path -LiteralPath $target) {
    if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $expected) {
        throw 'An existing file has a different SHA-256. It was not overwritten. Choose another output directory.'
    }
    Write-Output "Verified existing official archive: $target"
    return
}
$temporary = $target + '.download-' + [Guid]::NewGuid().ToString('N')
try {
    Invoke-WebRequest -Uri $uri -OutFile $temporary
    if ((Get-FileHash -LiteralPath $temporary -Algorithm SHA256).Hash -ne $expected) {
        throw 'SHA-256 mismatch. Download rejected.'
    }
    Move-Item -LiteralPath $temporary -Destination $target
    Write-Output "Downloaded and verified official BepInEx archive: $target"
    Write-Output 'Close the game; extract the archive contents into the game root. See README.md.'
} finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary }
}
