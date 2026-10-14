#!/bin/bash
# Run test classes of one test project into a folder of its own: run_test_classes.sh <Rules|Play|Units|MapStudio> <filter> [more dotnet test args]
# It builds with warnings NOT as errors, to keep a filtered run short: it says nothing about the strict build (merge-gate/gate.sh strict).
# Prints the counts and the failed tests' names and messages.
source "$(dirname "${BASH_SOURCE[0]}")/../paths.sh"
A=$R/src/ASL
PROJECT=$1
FILTER=$2
shift 2
OUT=$WORK/test-classes
mkdir -p "$OUT"
rm -f "$OUT/$PROJECT.trx"
DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1 dotnet test "$A/tests/LimboDancer.Domains.Asl.$PROJECT.Tests/LimboDancer.Domains.Asl.$PROJECT.Tests.csproj" \
  --disable-build-servers -m:1 -p:BaseOutputPath=bin/classes/ -p:TreatWarningsAsErrors=false -v:q -nologo \
  --filter "$FILTER" --logger "trx;LogFileName=$PROJECT.trx" --results-directory "$OUT" "$@" > "$OUT/$PROJECT.log" 2>&1
echo "exit $?"
grep -E "error |Passed!|Failed!|Total tests|No test matches" "$OUT/$PROJECT.log" | sed -E 's/\[E:.*$//' | sort -u | head -20
PYTHONIOENCODING=utf-8 python "$TOOLS/diagnostics/failed_tests_of_trx.py" "$OUT/$PROJECT.trx" < /dev/null
