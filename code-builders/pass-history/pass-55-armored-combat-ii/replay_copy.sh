#!/bin/bash
# The replay digest over the whole store from the proof tool's present copy (proof-p55, built by replay_now.sh), compared with the base's:
# replay_copy.sh <tag>. For a long run that must not hold bin/p31b. Same skip lists as replay_now.sh.
S="/e/Archive/GitHub/dlandi/pass32-tools"
TAG=$1
REF="$S/after-55-base/replay.txt"
export PROOF_SKIP="GameState.Catalog,DefinitionReference.Catalog${PROOF_SKIP_55:+,$PROOF_SKIP_55}"
export PROOF_SKIP_JSON="$PROOF_SKIP_JSON_55"
echo "PROOF_SKIP=$PROOF_SKIP"
echo "PROOF_SKIP_JSON=$PROOF_SKIP_JSON"
while [ ! -f "$REF" ] || ! grep -q "lines ->" "$S/after-55-base/replay.log" 2>/dev/null; do sleep 15; done
mkdir -p "$S/after-55-$TAG"
dotnet "$S/proof-p55/Proof.dll" replay "$S/after-55-$TAG/replay.txt" "$S/store" > "$S/after-55-$TAG/replay.log" 2>&1 || echo "replay exit $?"
tail -1 "$S/after-55-$TAG/replay.log" | head -c 300; echo
N=$(wc -l < "$S/after-55-$TAG/replay.txt"); D=$(diff "$REF" "$S/after-55-$TAG/replay.txt" | grep -c '^[<>]')
echo "lines: $(wc -l < "$REF") base, $N now; lines that differ: $D"
if [ "$D" -gt $((N / 2)) ]; then echo "VOID RUN: more than half the lines differ; find out why before reading anything else"; fi
diff "$REF" "$S/after-55-$TAG/replay.txt" | grep '^[<>]' | cut -c1-220 | head -20
echo "[$(date +%H:%M:%S)] DONE"
