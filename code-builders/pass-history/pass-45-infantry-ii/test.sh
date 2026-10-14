#!/bin/bash
# Run test classes of one test project into a folder of its own: test.sh <Rules|Play|Units|MapStudio> <filter> [more dotnet test args]
# Prints the counts and the failed tests' names and messages.
cd /e/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL || exit 1
PROJECT=$1
FILTER=$2
shift 2
OUT=/e/Archive/GitHub/dlandi/pass32-tools/pass45/trx
mkdir -p "$OUT"
rm -f "$OUT/$PROJECT.trx"
DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1 dotnet test "tests/LimboDancer.Domains.Asl.$PROJECT.Tests/LimboDancer.Domains.Asl.$PROJECT.Tests.csproj" \
  --disable-build-servers -m:1 -p:BaseOutputPath=bin/p45/ -p:TreatWarningsAsErrors=false -v:q -nologo \
  --filter "$FILTER" --logger "trx;LogFileName=$PROJECT.trx" --results-directory "$OUT" "$@" > "$OUT/$PROJECT.log" 2>&1
echo "exit $?"
grep -E "error |Passed!|Failed!|Total tests|No test matches" "$OUT/$PROJECT.log" | sed -E 's/\[E:.*$//' | sort -u | head -20
PYTHONIOENCODING=utf-8 python /e/Archive/GitHub/dlandi/pass32-tools/pass45/trx_failed.py "$OUT/$PROJECT.trx" < /dev/null
