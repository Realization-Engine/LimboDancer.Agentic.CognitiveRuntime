#!/bin/bash
# Pass 50, the gate's baseline: the proof store brought up to date with the pass's twelve recorded games, then main at b4ce31ef from its own
# sources (an archive, checked for a member of pass 45 and for none of pass 50), built into main-bin, the proof-main tool rebuilt against it
# (its Program.cs is the gate tool's, since main now holds pass 45), and the text list, the replay digest, and the sweep into before-50/.
set -e
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
T="5a7d1f000000400080000000000057d0"
BASE="b4ce31ef"
SWEEP="p31c-tw,pass16-demo2,p26-demo,p25-visual,p35-interdict-trap,p35-rally-enemy-turn,p35-smoke-hill,p35-tow2,p35-bog,p35-mud,p40-tpbf-mixed,p30-armor,p40c-ovr,p50-repair2,p50-bff-move2,p50-mg-turn2"
OWN="p50-mg-both2 p50-mg-turn2 p50-mg-first2 p50-repair2 p50-repair-ctl2 p50-bff-outset2 p50-bff-move2 p50-afph2 p50-afph-ctl2 p50-same-rear p50-same-front p50-walk320"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
unset PROOF_SKIP PROOF_SKIP_JSON
cd "$S"
rm -f store.ready
echo "[$(date +%H:%M:%S)] store before: $(ls store/$T/*.game.json | wc -l) games"
for g in $OWN; do cp "$R/src/ASL/boards/units/live/$T/$g.game.json" "store/$T/"; done
echo "[$(date +%H:%M:%S)] store after: $(ls store/$T/*.game.json | wc -l) games"
touch store.ready
rm -rf main-src main-bin main.tar before-50
mkdir -p main-src before-50
git -C "$R" archive --format=tar -o "$S/main.tar" $BASE
tar -xf main.tar -C main-src
echo "check (expect 1 or more): $(grep -rl ScenarioA1UpperLevelEncirclement main-src/src/ASL/LimboDancer.Domains.Asl.Rules | wc -l) (expect 0): $(grep -rl VehicleWeaponShots main-src/src/ASL --include=*.cs | wc -l)"
echo "[$(date +%H:%M:%S)] build $BASE"
dotnet build main-src/src/ASL/LimboDancer.Domains.Asl.MapStudio/LimboDancer.Domains.Asl.MapStudio.csproj -c Debug -o main-bin --disable-build-servers -m:1 -v:q > build-main-$BASE.log 2>&1
tail -3 build-main-$BASE.log
cp proof/Program.cs proof-main/Program.cs
dotnet build proof-main/Proof.csproj --disable-build-servers -m:1 -v:q > build-proof-main-$BASE.log 2>&1
tail -2 build-proof-main-$BASE.log
if ! grep -q "Build succeeded" build-proof-main-$BASE.log; then echo "STOP: the baseline's proof tool did not build"; exit 1; fi
if ! cmp -s proof-main/bin/Debug/net10.0/LimboDancer.Domains.Asl.Play.dll main-bin/LimboDancer.Domains.Asl.Play.dll; then echo "STOP: the copy is not the baseline's build"; exit 1; fi
python textlist.py before-50/textlist.txt main-src/src/ASL < /dev/null
echo "[$(date +%H:%M:%S)] replay"
dotnet proof-main/bin/Debug/net10.0/Proof.dll replay before-50/replay.txt store > before-50/replay.log 2>&1 || echo "replay exit $?"
tail -1 before-50/replay.log | head -c 300; echo
echo "[$(date +%H:%M:%S)] sweep $SWEEP"
dotnet proof-main/bin/Debug/net10.0/Proof.dll sweep before-50/sweep.txt store "$SWEEP" > before-50/sweep.log 2>&1 || echo "sweep exit $?"
grep -m1 "lines ->" before-50/sweep.log | head -c 300; echo
touch before-50/DONE
echo "[$(date +%H:%M:%S)] DONE baseline"
