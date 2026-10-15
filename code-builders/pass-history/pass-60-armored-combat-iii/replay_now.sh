#!/bin/bash
# The replay digest of the older games with the build in bin/p31b, against the gate's cached baseline: replay_now.sh <tag>.
# The proof tool is rebuilt against bin/p31b and its copy compared byte for byte, so bin/p31b is free once it says so. The skip list is
# the gate's: new_properties.py from the base to HEAD (commit first), and BOTH_SKIP of pass60.conf, on this side; BOTH_SKIP alone was the
# baseline's. Output under $WORK/p60-<tag>/.
source "$(dirname "${BASH_SOURCE[0]}")/../../paths.sh"
S="$WORK"; G="$TOOLS/merge-gate"; TAG=$1
BASE=380b4a7b
KEY=2b82cee3968bff1d
REF="$S/baseline-cache/$KEY/replay.txt"
BIN="$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/p31b/"
OUT="$S/p60-$TAG"
source "$G/pass-configs/pass60.conf"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
unset PROOF_SKIP_JSON PROOF_DUMP_FIRE PROOF_DUMP_EVENTS PROOF_DUMP_STATE PROOF_EXCLUDE PROOF_REPLAY_CUTS
export PROOF_SKIP="$(python "$G/new_properties.py" "$(git -C "$R" rev-parse "$BASE")" < /dev/null)${BOTH_SKIP:+,$BOTH_SKIP}"
export PROOF_SKIP="${PROOF_SKIP#,}"
[ -n "$SKIP_JSON" ] && export PROOF_SKIP_JSON="$SKIP_JSON"
echo "PROOF_SKIP=$PROOF_SKIP"
echo "PROOF_SKIP_JSON=${PROOF_SKIP_JSON-}"
[ -f "$REF" ] || { echo "STOP: no baseline digest at $REF"; exit 1; }
mkdir -p "$S/proof-p60-src" "$OUT"; cp "$TOOLS/proof-tool/Program.cs" "$TOOLS/proof-tool/Proof.csproj" "$S/proof-p60-src/"
dotnet build "$S/proof-p60-src/Proof.csproj" "-p:StudioBin=$BIN" "-p:OutDir=$S/proof-p60/" --disable-build-servers -m:1 -v:q > "$OUT/build-proof.log" 2>&1
if ! grep -q "Build succeeded" "$OUT/build-proof.log"; then echo "STOP: the proof tool did not build"; grep -m5 "error" "$OUT/build-proof.log"; exit 1; fi
for d in Play Rules Units Maps; do
  cmp -s "$S/proof-p60/LimboDancer.Domains.Asl.$d.dll" "$BIN/LimboDancer.Domains.Asl.$d.dll" || { echo "STOP: the copy of $d is not the build of bin/p31b"; exit 1; }
done
echo "[$(date +%H:%M:%S)] the proof tool is built and copied; bin/p31b is free"
EXCL=$(echo "$OWN" | tr ' ' ',' | tr -s ',' | sed -e 's/^,//' -e 's/,$//')
PROOF_EXCLUDE="$EXCL" dotnet "$S/proof-p60/Proof.dll" replay "$OUT/replay.txt" "$S/store" > "$OUT/replay.log" 2>&1 || echo "replay exit $?"
grep -m1 "lines ->" "$OUT/replay.log" | head -c 200; echo
N=$(wc -l < "$OUT/replay.txt"); D=$(diff "$REF" "$OUT/replay.txt" | grep -c '^[<>]')
echo "[$(date +%H:%M:%S)] lines: $(wc -l < "$REF") baseline, $N now; lines that differ: $D"
if [ "$D" -gt $((N / 2)) ]; then echo "VOID RUN: more than half the lines differ; find out why before reading anything else"; fi
diff "$REF" "$OUT/replay.txt" | grep '^[<>]' | cut -c1-200 | head -20
