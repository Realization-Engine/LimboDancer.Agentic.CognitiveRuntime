#!/bin/bash
# The gate's local suite: every test project one at a time, --no-build, from bin/gate, with a trx each; one summary line a project.
source "$(dirname "${BASH_SOURCE[0]}")/../paths.sh"
cd "$R" || exit 1
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
S="$WORK/suite"
mkdir -p "$S"
for p in LimboDancer.Dice.Tests LimboDancer.Domains.Asl.Rules.Tests LimboDancer.Domains.Asl.Units.Tests LimboDancer.Domains.Asl.Maps.Tests LimboDancer.Domains.Asl.Maps.Vasl.Tests LimboDancer.Domains.Asl.Maps.Rendering.Tests LimboDancer.Domains.Asl.Units.Rendering.Tests LimboDancer.Domains.Asl.Units.CounterSheets.Tests LimboDancer.Domains.Asl.MapStudio.Tests LimboDancer.Domains.Asl.Play.Tests LimboDancer.Domains.Asl.Authoring.Tests; do
  start=$(date +%H:%M:%S)
  dotnet test "src/ASL/tests/$p/$p.csproj" "-p:BaseOutputPath=bin/gate/" --no-build --disable-build-servers -v:q -nologo --logger "trx;LogFileName=$S/$p.trx" > "$S/$p.log" 2>&1
  code=$?
  line=$(grep -E "^(Passed!|Failed!)" "$S/$p.log" | tail -1)
  echo "$p | $start | $(date +%H:%M:%S) | exit $code | $line"
done
echo DONE
