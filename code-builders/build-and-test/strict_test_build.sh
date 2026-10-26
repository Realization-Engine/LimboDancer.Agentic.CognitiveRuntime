#!/bin/bash
# Build one test project with warnings as errors, as CI and the gate's strict stage do: strict_test_build.sh <Rules|Play|Units|MapStudio>
# Prints "exit N" and the errors. run_test_classes.sh builds with warnings NOT as errors, so a style error in a test file (CA1861, IDE0055, xUnit2031)
# passes every filtered run and is first seen by the strict stage or by the user's Visual Studio (pass 71, 2026-10-26: two CA1861 in a page test file).
# A building agent runs it for each test project it changed before reporting. The build goes outside the repository, into
# $WORK/test-strict-bin/<project>/, registered with outside_outputs.sh, so the gate's ci stage removes it.
source "$(dirname "${BASH_SOURCE[0]}")/../paths.sh"
A=$R/src/ASL
PROJECT=$1
if [ -z "$PROJECT" ]; then sed -n 2,6p "$0"; exit 2; fi
BIN=$WORK/test-strict-bin/$PROJECT/
mkdir -p "$BIN" "$WORK/test-classes"
bash "$TOOLS/build-and-test/outside_outputs.sh" register "$WORK/test-strict-bin" || exit 2
LOG=$WORK/test-classes/$PROJECT-strict.log
DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1 dotnet build "$A/tests/LimboDancer.Domains.Asl.$PROJECT.Tests/LimboDancer.Domains.Asl.$PROJECT.Tests.csproj" \
  --disable-build-servers -m:1 "-p:OutDir=$BIN" -warnaserror -v:q -nologo > "$LOG" 2>&1
echo "exit $?"
grep -E " error |Warn|Error\(s\)" "$LOG" | sed -E 's/\[E:.*$//' | sort -u | head -30
