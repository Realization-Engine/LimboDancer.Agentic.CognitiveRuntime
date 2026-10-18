#!/bin/bash
# Build MapStudio into bin/p31b with warnings as errors (my Studio on 6671 must be stopped). The errors, or "ok".
# Exits with the build's own code, so "build_studio.sh && run_test_classes.sh ..." stops at a failed build.
source "$(dirname "${BASH_SOURCE[0]}")/../paths.sh"
A=$R/src/ASL
LOG=$WORK/build-studio.log
DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1 dotnet build "$A/LimboDancer.Domains.Asl.MapStudio/LimboDancer.Domains.Asl.MapStudio.csproj" \
  --disable-build-servers -m:1 -p:OutDir=bin/p31b/ --warnaserror -v:q -nologo > "$LOG" 2>&1
CODE=$?
grep -E "error|warning" "$LOG" | sed -E 's/\[E:.*$//' | sort -u | head -40
echo "exit $CODE"
tail -3 "$LOG"
exit $CODE
