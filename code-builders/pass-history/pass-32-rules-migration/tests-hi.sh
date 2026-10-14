#!/bin/bash
# The block's tests: Rules, Units, and Play test projects, each built into bin/gate with warnings as errors, then run with --no-build and a trx.
cd /e/Archive/GitHub/dlandi/LimboDancer.MCP || exit 1
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
S="/e/Archive/GitHub/dlandi/pass32-tools/tests-hi"
mkdir -p "$S"
for p in LimboDancer.Domains.Asl.Rules.Tests LimboDancer.Domains.Asl.Units.Tests LimboDancer.Domains.Asl.Play.Tests; do
  start=$(date +%H:%M:%S)
  dotnet build "src/ASL/tests/$p/$p.csproj" -c Debug "-p:BaseOutputPath=bin/gate/" --disable-build-servers -m:1 --warnaserror -v:q > "$S/$p.build.log" 2>&1
  build=$?
  dotnet test "src/ASL/tests/$p/$p.csproj" "-p:BaseOutputPath=bin/gate/" --no-build --disable-build-servers -v:q -nologo --logger "trx;LogFileName=$S/$p.trx" > "$S/$p.log" 2>&1
  code=$?
  line=$(grep -E "^(Passed!|Failed!)" "$S/$p.log" | tail -1)
  echo "$p | $start | $(date +%H:%M:%S) | build exit $build | test exit $code | $line"
done
echo DONE
