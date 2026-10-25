#!/bin/bash
# The proof tool's own check after a change to it: a replay and a sweep of a few games on the baseline's side. About one minute.
# Expect: the replay names no cut; '=dl-tractor-works-02' is one game; a game the baseline cannot read is one UNREADABLE line and the run goes on.
# The games are the trimmed store's (2026-10-25): the swept game, its cuts, the other full game, and the previous pass's.
source "$(dirname "${BASH_SOURCE[0]}")/../paths.sh"
S="$WORK"
T="$S/tool-selftest"; rm -rf "$T"; mkdir -p "$T"
unset PROOF_SKIP PROOF_SKIP_JSON PROOF_DUMP_FIRE PROOF_DUMP_EVENTS PROOF_DUMP_STATE PROOF_REPLAY_CUTS
TOOL="$S/proof-main/bin/Debug/net10.0/Proof.dll"
dotnet "$TOOL" replay "$T/replay.txt" "$S/store" "p65-crew-gun,p65-tc-mortar,dl-tractor-works-02" > "$T/replay.log" 2>&1; echo "replay exit $?"
echo "replay games: $(cut -f1 "$T/replay.txt" | sort -u | tr '\n' ' ')"
PROOF_REPLAY_CUTS=1 dotnet "$TOOL" replay "$T/replay-cuts.txt" "$S/store" "dl-tractor-works-02-r118,dl-tractor-works-02-r153" > "$T/replay-cuts.log" 2>&1
echo "with PROOF_REPLAY_CUTS=1: $(cut -f1 "$T/replay-cuts.txt" | sort -u | tr '\n' ' ')"
dotnet "$TOOL" sweep "$T/sweep.txt" "$S/store" "=dl-tractor-works-02,dl-tractor-works-02-r118,p65-crew-gun,p65-capture-cc" > "$T/sweep.log" 2>&1; echo "sweep exit $?"
cut -f1 "$T/sweep.txt" | sort | uniq -c
awk -F'\t' '$2=="ERROR" || $2=="UNREADABLE" || $2=="NOSTATE"' "$T/sweep.txt" "$T/replay.txt" | cut -c1-200
tail -1 "$T/sweep.log" | cut -c1-120
