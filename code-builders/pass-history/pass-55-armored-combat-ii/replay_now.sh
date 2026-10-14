#!/bin/bash
# The replay digest over a store with the build in bin/p31b: replay_now.sh <tag> [store] [base suffix]. The proof tool is rebuilt against
# bin/p31b and its copy compared byte for byte; the digest goes to after-55-<tag>/replay.txt and is compared with the base's digest
# (after-55-base<suffix>/replay.txt, made by base.sh from main's own sources). Both sides run with one and the same PROOF_SKIP:
# GameState.Catalog, and what pass 55 has added so far (PROOF_SKIP_55, PROOF_SKIP_JSON_55, comma-separated).
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
TAG=$1
STORE="${2:-$S/store}"
REF="$S/after-55-base$3/replay.txt"
BIN="$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/p31b/"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
export PROOF_SKIP="GameState.Catalog,DefinitionReference.Catalog${PROOF_SKIP_55:+,$PROOF_SKIP_55}"
export PROOF_SKIP_JSON="$PROOF_SKIP_JSON_55"
echo "PROOF_SKIP=$PROOF_SKIP"
echo "PROOF_SKIP_JSON=$PROOF_SKIP_JSON"
if [ ! -f "$REF" ]; then echo "STOP: no base digest at $REF"; exit 1; fi
dotnet build "$S/proof/Proof.csproj" "-p:StudioBin=$BIN" "-p:OutDir=$S/proof-p55/" --disable-build-servers -m:1 -v:q > "$S/pass55/build-proof.log" 2>&1
tail -3 "$S/pass55/build-proof.log"
if ! grep -q "Build succeeded" "$S/pass55/build-proof.log"; then echo "STOP: the proof tool did not build"; grep -m5 "error" "$S/pass55/build-proof.log"; exit 1; fi
for d in Play Rules Units; do
  if ! cmp -s "$S/proof-p55/LimboDancer.Domains.Asl.$d.dll" "$BIN/LimboDancer.Domains.Asl.$d.dll"; then echo "STOP: the copy of $d is not the build of bin/p31b"; exit 1; fi
done
echo "[$(date +%H:%M:%S)] the proof tool is built and copied; bin/p31b is free"
mkdir -p "$S/after-55-$TAG"
dotnet "$S/proof-p55/Proof.dll" replay "$S/after-55-$TAG/replay.txt" "$STORE" > "$S/after-55-$TAG/replay.log" 2>&1 || echo "replay exit $?"
tail -1 "$S/after-55-$TAG/replay.log" | head -c 300; echo
N=$(wc -l < "$S/after-55-$TAG/replay.txt"); D=$(diff "$REF" "$S/after-55-$TAG/replay.txt" | grep -c '^[<>]')
echo "lines: $(wc -l < "$REF") base, $N now; lines that differ: $D"
if [ "$D" -gt $((N / 2)) ]; then echo "VOID RUN: more than half the lines differ; find out why before reading anything else"; fi
diff "$REF" "$S/after-55-$TAG/replay.txt" | grep '^[<>]' | cut -c1-220 | head -20
echo "[$(date +%H:%M:%S)] DONE"
