#!/bin/bash
# The 8c8f64a baseline for passes 32.h and 32.i: an archive of main's own sources, checked for a member only 32.g added, built into main-bin, the
# proof-main tool rebuilt against it, then the text list, the replay digest, and the sweep over the enlarged store into before-hi/.
set -e
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
cd "$S"
rm -rf main-src main-bin main.tar before-hi
mkdir -p main-src before-hi
git -C "$R" archive --format=tar -o "$S/main.tar" 8c8f64a
tar -xf main.tar -C main-src
echo "check (expect 1 or more): $(grep -c "ScenarioA1CloseCombatProjection" main-src/src/ASL/LimboDancer.Domains.Asl.Units/State/GameProjector.cs) (expect 0): $(ls main-src/src/ASL/LimboDancer.Domains.Asl.Rules | grep -c ScenarioA1SequenceCalculator)"
echo "[$(date +%H:%M:%S)] build 8c8f64a"
dotnet build main-src/src/ASL/LimboDancer.Domains.Asl.MapStudio/LimboDancer.Domains.Asl.MapStudio.csproj -c Debug -o main-bin --disable-build-servers -m:1 -v:q > build-main-8c8f64a.log 2>&1
tail -2 build-main-8c8f64a.log
dotnet build proof-main/Proof.csproj --disable-build-servers -m:1 -v:q > build-proof-main-8c8f64a.log 2>&1
ls -l --time-style=+%F_%H:%M proof-main/bin/Debug/net10.0/LimboDancer.Domains.Asl.Play.dll main-bin/LimboDancer.Domains.Asl.Play.dll | awk '{print $6, $7}'
python textlist.py before-hi/textlist.txt main-src/src/ASL < /dev/null
echo "[$(date +%H:%M:%S)] replay"
dotnet proof-main/bin/Debug/net10.0/Proof.dll replay before-hi/replay.txt store > before-hi/replay.log 2>&1; tail -1 before-hi/replay.log | head -c 300; echo
echo "[$(date +%H:%M:%S)] sweep p31c-tw,pass16-demo2"
dotnet proof-main/bin/Debug/net10.0/Proof.dll sweep before-hi/sweep.txt store p31c-tw,pass16-demo2 > before-hi/sweep.log 2>&1; grep -m1 "lines ->" before-hi/sweep.log | head -c 300; echo
echo "[$(date +%H:%M:%S)] DONE baseline"
