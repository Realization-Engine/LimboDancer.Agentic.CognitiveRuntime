#!/bin/bash
# Build MapStudio with warnings as errors into my Studio's folder outside the repository, $WORK/studio-6670 (launch config map-studio-p31b);
# my Studio on 6670 must be stopped. The errors, or "ok".
# Until 2026-10-23 it built into MapStudio's bin/p31b, a folder the rule of 2026-10-19 forbids: a gate build copies every folder under bin
# into itself, and gate.sh refuses while one is there. The folder is registered with outside_outputs.sh.
# Exits with the build's own code, so "build_studio.sh && run_test_classes.sh ..." stops at a failed build.
source "$(dirname "${BASH_SOURCE[0]}")/../paths.sh"
A=$R/src/ASL
LOG=$WORK/build-studio.log
OUT=$WORK/studio-6670/
bash "$TOOLS/build-and-test/outside_outputs.sh" register "$WORK/studio-6670" || exit 2
DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1 dotnet build "$A/LimboDancer.Domains.Asl.MapStudio/LimboDancer.Domains.Asl.MapStudio.csproj" \
  --disable-build-servers -m:1 "-p:OutDir=$OUT" --warnaserror -v:q -nologo > "$LOG" 2>&1
CODE=$?
grep -E "error|warning" "$LOG" | sed -E 's/\[E:.*$//' | sort -u | head -40
echo "exit $CODE"
tail -3 "$LOG"
exit $CODE
