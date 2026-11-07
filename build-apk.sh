#!/usr/bin/env bash
# Builds a Release APK for local sideload testing; see .scripts/Build-Apk.ps1 for parameters.
set -euo pipefail

exec pwsh -NoProfile -File "$(dirname "$0")/build-apk.ps1" "$@"
