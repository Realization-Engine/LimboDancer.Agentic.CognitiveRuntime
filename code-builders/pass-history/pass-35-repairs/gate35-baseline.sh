#!/bin/bash
# Pass 35, the gate's baseline: the proof store brought up to date with the live folder, then main at f3b7c47 from its own sources (an archive,
# checked for a member of pass 32 and for none of pass 35), built into main-bin, the proof-main tool rebuilt against it, and the text list, the
# replay digest, and the sweep into before-35/. Progress goes to this script's output; each proof has its own log.
set -e
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
T="5a7d1f000000400080000000000057d0"
SWEEP="p31c-tw,pass16-demo2,p26-demo,p25-visual,p35-interdict-trap,p35-rally-enemy-turn,p35-smoke-hill,p35-tow2,p35-bog,p35-mud"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
cd "$S"
echo "[$(date +%H:%M:%S)] store before: $(ls store/$T/*.game.json | wc -l) games"
cp -u "$R/src/ASL/boards/units/live/$T/"*.game.json "store/$T/"
echo "[$(date +%H:%M:%S)] store after: $(ls store/$T/*.game.json | wc -l) games"
touch store.ready
rm -rf main-src main-bin main.tar before-35
mkdir -p main-src before-35
git -C "$R" archive --format=tar -o "$S/main.tar" f3b7c47
tar -xf main.tar -C main-src
echo "check (expect 1 or more): $(ls main-src/src/ASL/LimboDancer.Domains.Asl.Rules | grep -c ScenarioA1SequenceCalculator) (expect 0): $(grep -rc "UnbuiltCounterBar" main-src/src/ASL/LimboDancer.Domains.Asl.Rules | grep -vc ':0$')"
echo "[$(date +%H:%M:%S)] build f3b7c47"
dotnet build main-src/src/ASL/LimboDancer.Domains.Asl.MapStudio/LimboDancer.Domains.Asl.MapStudio.csproj -c Debug -o main-bin --disable-build-servers -m:1 -v:q > build-main-f3b7c47.log 2>&1
tail -3 build-main-f3b7c47.log
dotnet build proof-main/Proof.csproj --disable-build-servers -m:1 -v:q > build-proof-main-f3b7c47.log 2>&1
tail -2 build-proof-main-f3b7c47.log
ls -l --time-style=+%F_%H:%M proof-main/bin/Debug/net10.0/LimboDancer.Domains.Asl.Play.dll main-bin/LimboDancer.Domains.Asl.Play.dll | awk '{print $6, $7}'
python textlist.py before-35/textlist.txt main-src/src/ASL < /dev/null
echo "[$(date +%H:%M:%S)] replay"
dotnet proof-main/bin/Debug/net10.0/Proof.dll replay before-35/replay.txt store > before-35/replay.log 2>&1 || echo "replay exit $?"
tail -1 before-35/replay.log | head -c 300; echo
echo "[$(date +%H:%M:%S)] sweep $SWEEP"
dotnet proof-main/bin/Debug/net10.0/Proof.dll sweep before-35/sweep.txt store "$SWEEP" > before-35/sweep.log 2>&1 || echo "sweep exit $?"
grep -m1 "lines ->" before-35/sweep.log | head -c 300; echo
touch before-35/DONE
echo "[$(date +%H:%M:%S)] DONE baseline"
