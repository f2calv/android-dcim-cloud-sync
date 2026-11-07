#!/usr/bin/env pwsh
#Requires -Version 7.4
# Builds a Release APK for local sideload testing; see .scripts/Build-Apk.ps1 for parameters.
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 3.0

& (Join-Path $PSScriptRoot '.scripts/Build-Apk.ps1') -RepositoryRoot $PSScriptRoot @args
exit $LASTEXITCODE
