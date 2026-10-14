#!/bin/bash
# Pass 45, the gate after the fix to the fire record's check: the solution build with warnings as errors into bin/gate, the test projects the
# change can reach (Play, MapStudio), the proof tool rebuilt against bin/gate with its copy compared byte for byte, then the replay digest and
# the sweep into after-45-final/. PROOF_SKIP names the properties the pass added; PROOF_SKIP_JSON the keys it added to the records kept as JSON.
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
SWEEP="p31c-tw,pass16-demo2,p26-demo,p25-visual,p35-interdict-trap,p35-rally-enemy-turn,p35-smoke-hill,p35-tow2,p35-bog,p35-mud,p40-tpbf-mixed"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
export PROOF_SKIP="FireArithmetic.ResidualFpIfForfeited,FireAttack.CoweringMarksAndWeaponResidual,FireAttack.MgBoreSighted,FireDefinition.SprayingFire,FireDirector.DirectedTheseMgs,FireFirer.Berserk,FireResolution.CoweredUnitIds,FireResolution.FirerCounters,FireRolls.CowerSelection,FireWeaponEffect.Removed,FirerLocationFacts.Ordnance,FirerRecordFacts.Berserk,FirerRecordFacts.Inexperienced,MovingUnitFacts.Cx,FireKindFacts.EveryFirstFireMarkedFiresAlone"
export PROOF_SKIP_JSON="coweringMarksAndWeaponResidual"
cd "$R" || exit 1
echo "[$(date +%H:%M:%S)] head $(git rev-parse --short HEAD) on $(git branch --show-current)"
dotnet build src/ASL/LimboDancer.Domains.Asl.sln -c Debug "-p:BaseOutputPath=bin/gate/" --warnaserror --disable-build-servers -m:1 -v:q > "$S/gate-build-45c.log" 2>&1
echo "build exit $?"; tail -4 "$S/gate-build-45c.log"
if ! grep -q "Build succeeded" "$S/gate-build-45c.log"; then echo "STOP: the build failed"; exit 1; fi
dotnet build "$S/proof/Proof.csproj" "-p:StudioBin=$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/gate/Debug/net10.0/" "-p:OutDir=$S/proof-gate/" --disable-build-servers -m:1 -v:q > "$S/build-proof-gate-45c.log" 2>&1
tail -3 "$S/build-proof-gate-45c.log"
if ! grep -q "Build succeeded" "$S/build-proof-gate-45c.log"; then echo "STOP: the proof tool did not build"; exit 1; fi
if ! cmp -s "$S/proof-gate/LimboDancer.Domains.Asl.Play.dll" "$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/gate/Debug/net10.0/LimboDancer.Domains.Asl.Play.dll"; then echo "STOP: the copy is not the gate's build"; exit 1; fi
mkdir -p "$S/after-45-final" "$S/suite-final"
echo "[$(date +%H:%M:%S)] replay"
dotnet "$S/proof-gate/Proof.dll" replay "$S/after-45-final/replay.txt" "$S/store" > "$S/after-45-final/replay.log" 2>&1 || echo "replay exit $?"
tail -1 "$S/after-45-final/replay.log" | head -c 300; echo
echo "replay lines that differ outside the p45 games: $(diff "$S/before-45/replay.txt" "$S/after-45-final/replay.txt" | grep '^[<>]' | grep -vc '^[<>] p45-')"
echo "[$(date +%H:%M:%S)] tests"
for p in LimboDancer.Domains.Asl.Play.Tests LimboDancer.Domains.Asl.MapStudio.Tests; do
  dotnet test "src/ASL/tests/$p/$p.csproj" "-p:BaseOutputPath=bin/gate/" --no-build --disable-build-servers -v:q -nologo > "$S/suite-final/$p.log" 2>&1
  echo "$p | exit $? | $(grep -E "^(Passed!|Failed!)" "$S/suite-final/$p.log" | tail -1)"
done
echo "[$(date +%H:%M:%S)] sweep"
dotnet "$S/proof-gate/Proof.dll" sweep "$S/after-45-final/sweep.txt" "$S/store" "$SWEEP" > "$S/after-45-final/sweep.log" 2>&1 || echo "sweep exit $?"
grep -m1 "lines ->" "$S/after-45-final/sweep.log" | head -c 300; echo
python "$S/pass45/sweep_pairs45.py" "$S/before-45/sweep.txt" "$S/after-45-final/sweep.txt" < /dev/null
touch "$S/after-45-final/DONE"
echo "[$(date +%H:%M:%S)] DONE"
