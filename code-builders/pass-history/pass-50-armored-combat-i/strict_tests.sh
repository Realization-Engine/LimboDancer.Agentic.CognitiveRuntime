#!/bin/bash
# Before the documents: the pass's test files formatted, and the two test projects built with warnings as errors (IDE0055, CA1861).
cd /e/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL || exit 1
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
R=tests/LimboDancer.Domains.Asl.Rules.Tests
P=tests/LimboDancer.Domains.Asl.Play.Tests
dotnet format whitespace $R/LimboDancer.Domains.Asl.Rules.Tests.csproj --include $R/ScenarioA1Pass50RulesTests.cs $R/ScenarioA1Pass7Tests.cs -v q 2>&1 | tail -2
dotnet format whitespace $P/LimboDancer.Domains.Asl.Play.Tests.csproj --include $P/BacklogPass50Tests.cs -v q 2>&1 | tail -2
for T in $R/LimboDancer.Domains.Asl.Rules.Tests.csproj $P/LimboDancer.Domains.Asl.Play.Tests.csproj; do
  dotnet build $T --disable-build-servers -m:1 -p:BaseOutputPath=bin/p50/ --warnaserror -v:q -nologo > /e/Archive/GitHub/dlandi/pass32-tools/pass50/strict.log 2>&1
  echo "$T exit $?"; grep -E "error" /e/Archive/GitHub/dlandi/pass32-tools/pass50/strict.log | sed -E 's/\[E:.*$//' | sort -u | head -12
done
git -C /e/Archive/GitHub/dlandi/LimboDancer.MCP status --short | grep -v boards/
