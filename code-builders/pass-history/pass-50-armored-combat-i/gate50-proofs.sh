#!/bin/bash
# Pass 50, the gate's proofs: the proof tool rebuilt against bin/gate with its copy compared byte for byte, then the text list, the replay
# digest, and the sweep into <out>/ (default after-50-gate), each compared with before-50/. PROOF_SKIP names the properties pass 50 alone (main holds pass 45; its list on this side made every fire plan differ in the first run)
# added; PROOF_SKIP_JSON the keys they added to the records kept as JSON ("key{}" a flat object only, "key[{}]" an array of flat objects only).
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
OUT="$S/${1:-after-50-gate}"
SWEEP="p31c-tw,pass16-demo2,p26-demo,p25-visual,p35-interdict-trap,p35-rally-enemy-turn,p35-smoke-hill,p35-tow2,p35-bog,p35-mud,p40-tpbf-mixed,p30-armor,p40c-ovr,p50-repair2,p50-bff-move2,p50-mg-turn2"
GATE="$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/gate/Debug/net10.0/"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
export PROOF_SKIP="GameState.VehicleWeaponShots,FireResolved.TurretFacing,RepairAttempted.Weapon,FireVehicleFire.Weapons,FireVehicleFire.OtherPhase,FireVehicleFire.InWoodsOrBuilding,FireWeaponEffect.VehicleWeapon,OrdnanceVehicleFirer.Weapons,FireSpentFacts.VehicleMgUnfired,VehicleStepped.Turret,OrdnanceVehicleFirer.MpInLos,OrdnanceVehicleFirer.BoundingFirer"
export PROOF_SKIP_JSON='weapons{},weapons[{}],mpInLos,boundingFirer,otherPhase,inWoodsOrBuilding,vehicleWeapon,turretFacing'
echo "PROOF_SKIP_JSON=$PROOF_SKIP_JSON"
while [ ! -f "$S/before-50/DONE" ]; do sleep 20; done
cd "$R" || exit 1
echo "[$(date +%H:%M:%S)] head $(git rev-parse --short HEAD); proof-gate against bin/gate"
dotnet build "$S/proof/Proof.csproj" "-p:StudioBin=$GATE" "-p:OutDir=$S/proof-gate/" --disable-build-servers -m:1 -v:q > "$S/build-proof-gate-50.log" 2>&1
tail -3 "$S/build-proof-gate-50.log"
if ! grep -q "Build succeeded" "$S/build-proof-gate-50.log"; then echo "STOP: the proof tool did not build"; exit 1; fi
for d in Play Rules Units MapStudio; do
  if ! cmp -s "$S/proof-gate/LimboDancer.Domains.Asl.$d.dll" "$GATE/LimboDancer.Domains.Asl.$d.dll"; then echo "STOP: the copy of $d is not the gate's build"; exit 1; fi
done
mkdir -p "$OUT"
python "$S/textlist.py" "$OUT/textlist.txt" < /dev/null
echo "text list lines that differ: $(diff "$S/before-50/textlist.txt" "$OUT/textlist.txt" | grep -c '^[<>]')"
echo "[$(date +%H:%M:%S)] replay"
dotnet "$S/proof-gate/Proof.dll" replay "$OUT/replay.txt" "$S/store" > "$OUT/replay.log" 2>&1 || echo "replay exit $?"
tail -1 "$OUT/replay.log" | head -c 300; echo
echo "replay lines: $(wc -l < "$S/before-50/replay.txt") before, $(wc -l < "$OUT/replay.txt") now; that differ: $(diff "$S/before-50/replay.txt" "$OUT/replay.txt" | grep -c '^[<>]'); outside the p50 games: $(diff "$S/before-50/replay.txt" "$OUT/replay.txt" | grep '^[<>]' | grep -vc '^[<>] p50-')"
echo "[$(date +%H:%M:%S)] sweep $SWEEP"
dotnet "$S/proof-gate/Proof.dll" sweep "$OUT/sweep.txt" "$S/store" "$SWEEP" > "$OUT/sweep.log" 2>&1 || echo "sweep exit $?"
grep -m1 "lines ->" "$OUT/sweep.log" | head -c 300; echo
python "$S/pass45/sweep_pairs45.py" "$S/before-50/sweep.txt" "$OUT/sweep.txt" < /dev/null > "$OUT/sweep-pairs.txt"
echo "sweep groups that differ: $(wc -l < "$OUT/sweep-pairs.txt") (in $OUT/sweep-pairs.txt)"
touch "$OUT/DONE"
echo "[$(date +%H:%M:%S)] DONE proofs"
