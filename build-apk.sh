#!/usr/bin/env bash
# Builds a Release APK for local sideload testing; see build-apk.ps1.
set -euo pipefail

exec pwsh -NoProfile -File "$(dirname "$0")/build-apk.ps1" "$@"
