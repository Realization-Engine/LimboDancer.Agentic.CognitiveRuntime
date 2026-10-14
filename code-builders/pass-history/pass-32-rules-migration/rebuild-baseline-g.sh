#!/bin/bash
# The true 0919e8e baseline (the prepare-g.sh archive path was relative to the repository, so main-src stayed a 12402fc archive).
set -e
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
cd "$S"
rm -rf main-src main-bin main.tar before-g
mkdir -p main-src before-g
git -C "$R" archive --format=tar -o "$S/main.tar" 0919e8e
tar -xf main.tar -C main-src
grep -c "ChargeNeighbors" main-src/src/ASL/LimboDancer.Domains.Asl.Play/GamePlanner.Rout.cs
echo "[$(date +%H:%M:%S)] build 0919e8e"
dotnet build main-src/src/ASL/LimboDancer.Domains.Asl.MapStudio/LimboDancer.Domains.Asl.MapStudio.csproj -c Debug -o main-bin --disable-build-servers -m:1 -v:q > build-main-0919e8e.log 2>&1
tail -2 build-main-0919e8e.log
dotnet build proof-main/Proof.csproj --disable-build-servers -m:1 -v:q > build-proof-main-0919e8e.log 2>&1
ls -l --time-style=+%H:%M proof-main/bin/Debug/net10.0/LimboDancer.Domains.Asl.Play.dll | awk '{print $6}'
python textlist.py before-g/textlist.txt main-src/src/ASL < /dev/null
echo "[$(date +%H:%M:%S)] replay"
dotnet proof-main/bin/Debug/net10.0/Proof.dll replay before-g/replay.txt store > before-g/replay.log 2>&1; tail -1 before-g/replay.log | head -c 200; echo
echo "[$(date +%H:%M:%S)] sweep"
dotnet proof-main/bin/Debug/net10.0/Proof.dll sweep before-g/sweep.txt store p31c-tw > before-g/sweep.log 2>&1; grep -m1 "lines ->" before-g/sweep.log | head -c 200; echo
for f in replay sweep; do cmp -s before-g/$f.txt after-g11/$f.txt && echo "$f: EQUAL" || echo "$f: DIFFERENT ($(diff before-g/$f.txt after-g11/$f.txt | grep -c '^[<>]') lines)"; done
echo "[$(date +%H:%M:%S)] DONE baseline"
