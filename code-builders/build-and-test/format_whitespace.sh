#!/bin/bash
# dotnet format whitespace on named files of one project: format_whitespace.sh <project folder name> <file relative to src/ASL>...
source "$(dirname "${BASH_SOURCE[0]}")/../paths.sh"
cd "$R/src/ASL" || exit 1
PROJECT=$1
shift
DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1 dotnet format whitespace "$PROJECT/$(basename "$PROJECT").csproj" --include "$@" -v q 2>&1 | tail -3
git -C "$R" diff --stat -- "$@" | tail -1
