#!/bin/bash
# After commit 13: the Rules tests the block added and pass 16's, and Play's pass 16 table-player tests, built into bin/gate and run with filters.
cd /e/Archive/GitHub/dlandi/LimboDancer.MCP || exit 1
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
S="/e/Archive/GitHub/dlandi/pass32-tools/tests-hi"
run() {
  p="$1"; filter="$2"
  start=$(date +%H:%M:%S)
  dotnet build "src/ASL/tests/$p/$p.csproj" -c Debug "-p:BaseOutputPath=bin/gate/" --disable-build-servers -m:1 --warnaserror -v:q > "$S/$p.13.build.log" 2>&1
  build=$?
  dotnet test "src/ASL/tests/$p/$p.csproj" "-p:BaseOutputPath=bin/gate/" --no-build --disable-build-servers -v:q -nologo --filter "$filter" --logger "trx;LogFileName=$S/$p.13.trx" > "$S/$p.13.log" 2>&1
  code=$?
  line=$(grep -E "^(Passed!|Failed!)" "$S/$p.13.log" | tail -1)
  echo "$p | $filter | $start | $(date +%H:%M:%S) | build exit $build | test exit $code | $line"
}
run LimboDancer.Domains.Asl.Rules.Tests "FullyQualifiedName~ScenarioA1SequenceRulesTests|FullyQualifiedName~ScenarioA1Pass16Tests"
run LimboDancer.Domains.Asl.Play.Tests "FullyQualifiedName~BacklogPass16"
echo DONE
