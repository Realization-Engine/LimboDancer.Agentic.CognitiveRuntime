#!/bin/bash
# Pass 45, the gate's replay digest and sweep again: the first run's proof tool did not compile against bin/gate (MayDismantle is no longer
# static) and ran on the copies of 2026-10-11. The tool is rebuilt, its copies' dates are checked against the build, and the script stops if
# they are older. The properties the pass added to the records are left out of the dump (PROOF_SKIP).
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
SWEEP="p31c-tw,pass16-demo2,p26-demo,p25-visual,p35-interdict-trap,p35-rally-enemy-turn,p35-smoke-hill,p35-tow2,p35-bog,p35-mud,p40-tpbf-mixed"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
export PROOF_SKIP="FireArithmetic.ResidualFpIfForfeited,FireAttack.CoweringMarksAndWeaponResidual,FireAttack.MgBoreSighted,FireDefinition.SprayingFire,FireDirector.DirectedTheseMgs,FireFirer.Berserk,FireResolution.CoweredUnitIds,FireResolution.FirerCounters,FireRolls.CowerSelection,FireWeaponEffect.Removed,FirerLocationFacts.Ordnance,FirerRecordFacts.Berserk,FirerRecordFacts.Inexperienced,MovingUnitFacts.Cx,FireKindFacts.EveryFirstFireMarkedFiresAlone"
cd "$S" || exit 1
dotnet build "$S/proof/Proof.csproj" "-p:StudioBin=$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/gate/Debug/net10.0/" "-p:OutDir=$S/proof-gate/" --disable-build-servers -m:1 -v:q > "$S/build-proof-gate-45b.log" 2>&1
tail -3 "$S/build-proof-gate-45b.log"
if ! grep -q "Build succeeded" "$S/build-proof-gate-45b.log"; then echo "STOP: the proof tool did not build"; exit 1; fi
ls -l --time-style=+%F_%H:%M "$S/proof-gate/LimboDancer.Domains.Asl.Play.dll" "$S/proof-gate/Proof.dll" "$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/gate/Debug/net10.0/LimboDancer.Domains.Asl.Play.dll" | awk '{print $6, $7}'
if ! cmp -s "$S/proof-gate/LimboDancer.Domains.Asl.Play.dll" "$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/gate/Debug/net10.0/LimboDancer.Domains.Asl.Play.dll"; then echo "STOP: the copy is not the gate's build"; exit 1; fi
mkdir -p after-45-gate2
echo "[$(date +%H:%M:%S)] replay"
dotnet "$S/proof-gate/Proof.dll" replay "$S/after-45-gate2/replay.txt" "$S/store" > "$S/after-45-gate2/replay.log" 2>&1 || echo "replay exit $?"
tail -1 "$S/after-45-gate2/replay.log" | head -c 300; echo
echo "[$(date +%H:%M:%S)] sweep"
dotnet "$S/proof-gate/Proof.dll" sweep "$S/after-45-gate2/sweep.txt" "$S/store" "$SWEEP" > "$S/after-45-gate2/sweep.log" 2>&1 || echo "sweep exit $?"
grep -m1 "lines ->" "$S/after-45-gate2/sweep.log" | head -c 300; echo
touch after-45-gate2/DONE
echo "[$(date +%H:%M:%S)] DONE"
