#!/bin/bash
# One game dumped in full from the baseline's side and the gate's side, to name what differs when a hash differs and the text does not.
# Run it after the gate's baseline and build stages: it uses their builds, and the pass's own skip lists.
#
#   dump_both_sides.sh <pass> <base commit> events <game>           every planned event of the sweep of that game
#   dump_both_sides.sh <pass> <base commit> fire <game>             every fire plan of the sweep of that game
#   dump_both_sides.sh <pass> <base commit> state <game> <number>   one state of the replay of that game
#
# The texts go to <working folder>/dump/main.txt and gate.txt; the first difference is printed. A cut of a game is named with its cut,
# as p31c-tw-r459. With the skip lists in, a line that still differs is a fact the lists miss.
source "$(dirname "${BASH_SOURCE[0]}")/../paths.sh"
S="$WORK"
PASS="$1"; BASE="$2"; WHAT="$3"; GAME="$4"; NUMBER="$5"
if [ -z "$GAME" ] || { [ "$WHAT" = "state" ] && [ -z "$NUMBER" ]; }; then sed -n 2,10p "$0"; exit 2; fi
GATE="$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/gate/Debug/net10.0"
MAINTOOL="$S/proof-main/bin/Debug/net10.0"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
unset PROOF_SKIP PROOF_SKIP_JSON PROOF_DUMP_FIRE PROOF_DUMP_EVENTS PROOF_DUMP_STATE PROOF_EXCLUDE PROOF_REPLAY_CUTS
BOTH_SKIP=""; SKIP_JSON=""; SKIP_ADD=""
source "$TOOLS/merge-gate/pass-configs/pass$PASS.conf" || exit 2

# Both tools from the repository's source, each checked against the build it is bound to.
mkdir -p "$S/proof" "$S/proof-main"
cp "$TOOLS/proof-tool/Program.cs" "$TOOLS/proof-tool/Proof.csproj" "$S/proof/"
cp "$TOOLS/proof-tool/Program.cs" "$S/proof-main/Program.cs"; cp "$TOOLS/proof-tool/Proof.Baseline.csproj" "$S/proof-main/Proof.csproj"
dotnet build "$S/proof-main/Proof.csproj" "-p:StudioBin=$S/main-bin/" --disable-build-servers -m:1 -v:q 2>&1 | grep -E " error " | head -3
dotnet build "$S/proof/Proof.csproj" "-p:StudioBin=$GATE/" "-p:OutDir=$S/proof-gate/" --disable-build-servers -m:1 -v:q 2>&1 | grep -E " error " | head -3
cmp -s "$S/proof-gate/LimboDancer.Domains.Asl.Play.dll" "$GATE/LimboDancer.Domains.Asl.Play.dll" || { echo "STOP: the gate's tool is not the gate's build (run the build stage)"; exit 1; }
cmp -s "$MAINTOOL/LimboDancer.Domains.Asl.Play.dll" "$S/main-bin/LimboDancer.Domains.Asl.Play.dll" || { echo "STOP: the baseline's tool is not the baseline's build (run the baseline stage)"; exit 1; }

D="$S/dump"; rm -rf "$D"; mkdir -p "$D"
NEW=$(python "$TOOLS/merge-gate/new_properties.py" "$BASE" < /dev/null) || exit 1
GATE_SKIP="$NEW${SKIP_ADD:+,$SKIP_ADD}${BOTH_SKIP:+,$BOTH_SKIP}"
case "$WHAT" in
# A planned event's time is the clock's, which the gate's digest does not hash: it is left out of the dump on both sides.
events) MODE=sweep; VARIABLE=PROOF_DUMP_EVENTS; MAIN_VALUE="$D/main.txt"; GATE_VALUE="$D/gate.txt"; BOTH_SKIP="${BOTH_SKIP:+$BOTH_SKIP,}GameEvent.Time"; GATE_SKIP="$GATE_SKIP,GameEvent.Time" ;;
fire)   MODE=sweep; VARIABLE=PROOF_DUMP_FIRE; MAIN_VALUE="$D/main.txt"; GATE_VALUE="$D/gate.txt" ;;
state)  MODE=replay; VARIABLE=PROOF_DUMP_STATE; MAIN_VALUE="$D/main.txt|$GAME|$NUMBER"; GATE_VALUE="$D/gate.txt|$GAME|$NUMBER"; export PROOF_REPLAY_CUTS=1 ;;
*) sed -n 2,10p "$0"; exit 2 ;;
esac
(export PROOF_SKIP="$BOTH_SKIP" "$VARIABLE=$MAIN_VALUE"; dotnet "$MAINTOOL/Proof.dll" "$MODE" "$D/out-main.txt" "$S/store" "=$GAME" > "$D/main.log" 2>&1) || echo "the baseline's side exited $?"
(export PROOF_SKIP="$GATE_SKIP" PROOF_SKIP_JSON="$SKIP_JSON" "$VARIABLE=$GATE_VALUE"; dotnet "$S/proof-gate/Proof.dll" "$MODE" "$D/out-gate.txt" "$S/store" "=$GAME" > "$D/gate.log" 2>&1) || echo "the gate's side exited $?"
[ -f "$D/main.txt" ] && [ -f "$D/gate.txt" ] || { echo "STOP: a side wrote no dump (see $D/main.log and gate.log; is the game in the store, and the state number within its replay?)"; exit 1; }
echo "$(wc -c < "$D/main.txt") bytes on the baseline's side, $(wc -c < "$D/gate.txt") on the gate's; lines that differ: $(diff "$D/main.txt" "$D/gate.txt" | grep -c '^[<>]')"
python "$TOOLS/diagnostics/first_difference_of_dumps.py" "$D/main.txt" "$D/gate.txt" < /dev/null
