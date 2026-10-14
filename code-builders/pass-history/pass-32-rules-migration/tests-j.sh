#!/bin/bash
# Pass 32.j: the slice's rule-pass tests. Each test project is built into bin/gate with warnings as errors, then run with --no-build, a filter, and a trx.
cd /e/Archive/GitHub/dlandi/LimboDancer.MCP || exit 1
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
S="/e/Archive/GitHub/dlandi/pass32-tools/tests-j"
mkdir -p "$S"
run() {
  p="$1"; filter="$2"
  start=$(date +%H:%M:%S)
  dotnet build "src/ASL/tests/$p/$p.csproj" -c Debug "-p:BaseOutputPath=bin/gate/" --disable-build-servers -m:1 --warnaserror -v:q > "$S/$p.build.log" 2>&1
  build=$?
  if [ -n "$filter" ]; then
    dotnet test "src/ASL/tests/$p/$p.csproj" "-p:BaseOutputPath=bin/gate/" --no-build --disable-build-servers --filter "$filter" -v:q -nologo --logger "trx;LogFileName=$S/$p.trx" > "$S/$p.log" 2>&1
  else
    dotnet test "src/ASL/tests/$p/$p.csproj" "-p:BaseOutputPath=bin/gate/" --no-build --disable-build-servers -v:q -nologo --logger "trx;LogFileName=$S/$p.trx" > "$S/$p.log" 2>&1
  fi
  code=$?
  line=$(grep -E "^(Passed!|Failed!)" "$S/$p.log" | tail -1)
  echo "$p | $start | $(date +%H:%M:%S) | build exit $build | test exit $code | $line"
}
run LimboDancer.Domains.Asl.Rules.Tests "FullyQualifiedName~ScenarioA1Pass19bTests"
run LimboDancer.Domains.Asl.Units.Tests ""
run LimboDancer.Domains.Asl.Play.Tests "FullyQualifiedName~BacklogPass17Tests|FullyQualifiedName~BacklogPass19Tests|FullyQualifiedName~BacklogPass20Tests|FullyQualifiedName~BacklogPass20TablePlayerTests|FullyQualifiedName~BacklogPass21Tests|FullyQualifiedName~BacklogPass21TablePlayerTests|FullyQualifiedName~BacklogPass23Tests|FullyQualifiedName~BacklogPass24Tests|FullyQualifiedName~BacklogPass25Tests|FullyQualifiedName~BacklogPass26Tests|FullyQualifiedName~BacklogPass30Tests|FullyQualifiedName~BacklogPass30bTests"
run LimboDancer.Domains.Asl.MapStudio.Tests "FullyQualifiedName~PlayPagePass25Tests|FullyQualifiedName~PlayPagePass26Tests|FullyQualifiedName~SetupPlansPageTests|FullyQualifiedName~SetupAnswersPageTests|FullyQualifiedName~CardEditorPass28Tests"
echo DONE
