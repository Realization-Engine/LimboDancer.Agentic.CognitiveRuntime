#!/bin/bash
# Run test classes of one test project into a folder of its own: run_test_classes.sh <Rules|Play|Units|MapStudio> <filter> [more dotnet test args]
# It builds with warnings NOT as errors, to keep a filtered run short: it says nothing about the strict build (merge-gate/gate.sh strict).
# Prints the counts and the failed tests' names and messages.
# The build goes outside the repository, into $WORK/test-classes-bin/<project>/ (the user, 2026-10-22): a folder under a project's bin
# (bin/classes until then) is one the gate's build would copy into itself, so the gate refused to start after every filtered run. The folder
# is registered with outside_outputs.sh, so the gate's ci stage removes it once CI passes; the next run builds it again.
source "$(dirname "${BASH_SOURCE[0]}")/../paths.sh"
A=$R/src/ASL
PROJECT=$1
FILTER=$2
shift 2
OUT=$WORK/test-classes
BIN=$WORK/test-classes-bin/$PROJECT/
mkdir -p "$OUT" "$BIN"
bash "$TOOLS/build-and-test/outside_outputs.sh" register "$WORK/test-classes-bin" || exit 2
rm -f "$OUT/$PROJECT.trx"
DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1 dotnet test "$A/tests/LimboDancer.Domains.Asl.$PROJECT.Tests/LimboDancer.Domains.Asl.$PROJECT.Tests.csproj" \
  --disable-build-servers -m:1 "-p:OutDir=$BIN" -p:TreatWarningsAsErrors=false -v:q -nologo \
  --filter "$FILTER" --logger "trx;LogFileName=$PROJECT.trx" --results-directory "$OUT" "$@" > "$OUT/$PROJECT.log" 2>&1
echo "exit $?"
grep -E "error |Passed!|Failed!|Total tests|No test matches" "$OUT/$PROJECT.log" | sed -E 's/\[E:.*$//' | sort -u | head -20
PYTHONIOENCODING=utf-8 python "$TOOLS/diagnostics/failed_tests_of_trx.py" "$OUT/$PROJECT.trx" < /dev/null
