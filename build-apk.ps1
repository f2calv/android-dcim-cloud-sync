#!/usr/bin/env pwsh
#Requires -Version 7.4
# Builds a Release APK for local sideload testing with the central dotnet-maui skill script; see its help for parameters.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3.0

$scripts = @(
    (Join-Path $HOME '.copilot/skills/dotnet-maui/scripts/Build-Apk.ps1'),
    (Join-Path (Split-Path $PSScriptRoot -Parent) '.github/.github/skills/dotnet-maui/scripts/Build-Apk.ps1')
)
$implementation = $scripts | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
if (-not $implementation) { throw "Build-Apk.ps1 was not found. Link the central skills folder or clone the central repository beside this repository." }
& $implementation -RepositoryRoot $PSScriptRoot -Project 'src/CasCap.App.Maui/CasCap.App.Maui.csproj' `
    -KeyAlias 'dcim-sideload' -KeyVaultKeystoreSecret 'dcim-android-sideload-keystore' `
    -KeyVaultPasswordSecret 'dcim-android-sideload-keystore-password' @args
exit $LASTEXITCODE
