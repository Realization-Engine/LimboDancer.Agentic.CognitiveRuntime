#!/bin/bash
# The 042d14a baseline for pass 32.j: an archive of main's own sources, checked for a member only the 32.h/32.i block added and for no S10 file,
# built into main-bin, the proof-main tool rebuilt against it, then the text list, the replay digest, and the sweep over four games into before-j/.
set -e
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
cd "$S"
rm -rf main-src main-bin main.tar before-j
mkdir -p main-src before-j
git -C "$R" archive --format=tar -o "$S/main.tar" 042d14a
tar -xf main.tar -C main-src
echo "check (expect 1 or more): $(grep -c "ScenarioA1SequenceProjection" main-src/src/ASL/LimboDancer.Domains.Asl.Units/State/GameProjector.cs) (expect 0): $(ls main-src/src/ASL/LimboDancer.Domains.Asl.Rules | grep -c ScenarioA1SetupCalculator)"
echo "[$(date +%H:%M:%S)] build 042d14a"
dotnet build main-src/src/ASL/LimboDancer.Domains.Asl.MapStudio/LimboDancer.Domains.Asl.MapStudio.csproj -c Debug -o main-bin --disable-build-servers -m:1 -v:q > build-main-042d14a.log 2>&1
tail -2 build-main-042d14a.log
dotnet build proof-main/Proof.csproj --disable-build-servers -m:1 -v:q > build-proof-main-042d14a.log 2>&1
ls -l --time-style=+%F_%H:%M proof-main/bin/Debug/net10.0/LimboDancer.Domains.Asl.Play.dll main-bin/LimboDancer.Domains.Asl.Play.dll | awk '{print $6, $7}'
python textlist.py before-j/textlist.txt main-src/src/ASL < /dev/null
echo "[$(date +%H:%M:%S)] replay"
dotnet proof-main/bin/Debug/net10.0/Proof.dll replay before-j/replay.txt store > before-j/replay.log 2>&1; tail -1 before-j/replay.log | head -c 300; echo
echo "[$(date +%H:%M:%S)] sweep p31c-tw,pass16-demo2,p26-demo,p25-visual"
dotnet proof-main/bin/Debug/net10.0/Proof.dll sweep before-j/sweep.txt store p31c-tw,pass16-demo2,p26-demo,p25-visual > before-j/sweep.log 2>&1; grep -m1 "lines ->" before-j/sweep.log | head -c 300; echo
echo "[$(date +%H:%M:%S)] DONE baseline"
