#!/bin/bash
# Pass 55's gate: the sweep on both sides with an event's rule package reference left out as well (its digest changed with the catalog),
# to show that nothing else differs in a plan made before the pass.
#   sweep_nopkg.sh small     three small games and eight cuts of p31c-tw that differed most
#   sweep_nopkg.sh full      the gate's whole sweep list, both sides side by side
S="/e/Archive/GitHub/dlandi/pass32-tools"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
unset PROOF_SKIP PROOF_SKIP_JSON PROOF_DUMP_FIRE PROOF_DUMP_EVENTS PROOF_DUMP_STATE
BOTH="GameState.Catalog,DefinitionReference.Catalog,GameEvent.RulePackage"
NEW=$(python "$S/gate/new_properties.py" 846eb5a3 < /dev/null)
case "$1" in
small) GAMES="p25-visual,p35-mud,p35-smoke-hill,p31c-tw-r250,p31c-tw-r382,p31c-tw-r459,p31c-tw-r646,p31c-tw-r693,p31c-tw-r607,p31c-tw-r542,p31c-tw-r710"; OUT="$S/nopkg55-small" ;;
full) GAMES="p31c-tw,pass16-demo2,p26-demo,p25-visual,p35-interdict-trap,p35-rally-enemy-turn,p35-smoke-hill,p35-tow2,p35-bog,p35-mud,p40-tpbf-mixed,p30-armor,p40c-ovr,p55-can-used,p55-duel-miss,p55-mot-pass,p55-al-fire"; OUT="$S/nopkg55-full" ;;
*) echo "small or full"; exit 2 ;;
esac
rm -rf "$OUT"; mkdir -p "$OUT"
date +%H:%M:%S
(export PROOF_SKIP="$BOTH"; dotnet "$S/proof-main/bin/Debug/net10.0/Proof.dll" sweep "$OUT/main.txt" "$S/store" "$GAMES" > "$OUT/main.log" 2>&1; echo "main exit $?") &
(export PROOF_SKIP="$NEW,$BOTH"; dotnet "$S/proof-gate/Proof.dll" sweep "$OUT/gate.txt" "$S/store" "$GAMES" > "$OUT/gate.log" 2>&1; echo "gate exit $?") &
wait
date +%H:%M:%S
wc -l "$OUT/main.txt" "$OUT/gate.txt"
python "$S/gate/sweep_pairs.py" "$OUT/main.txt" "$OUT/gate.txt" < /dev/null > "$OUT/pairs.txt"
echo "groups that differ, those not new on the gate's side:"
grep -v "NEW AFTER" "$OUT/pairs.txt"
echo "(end; $(grep -c 'NEW AFTER' "$OUT/pairs.txt") groups new on the gate's side)"
