#!/bin/bash
# Pass 70's replay check of its own games after the fix rounds (the whole-pass check): every p70-*.game.json of the live folder, copied into a store
# of their own under the working folder (so the live folder, where the user's Studio appends to its own game, is never read by the tool), replayed
# by the proof tool built against my Studio's build (tool_check_3a.sh builds it). Each game must replay to its last event: no ERROR, UNREADABLE,
# or NOSTATE line.
source "$(dirname "${BASH_SOURCE[0]}")/../../paths.sh"
S="$WORK"; T="5a7d1f000000400080000000000057d0"
LIVE="$R/src/ASL/boards/units/live/$T"
O="$S/replay-70"; rm -rf "$O"; mkdir -p "$O/store/$T"
bash "$TOOLS/build-and-test/outside_outputs.sh" register "$O"
for f in "$LIVE"/p70-*.game.json; do
  case "$(basename "$f")" in p70-perf-tw.game.json) continue ;; esac
  cp "$f" "$O/store/$T/"
done
GAMES=$(ls "$O/store/$T" | sed -n 's/\.game\.json$//p' | paste -sd, -)
echo "games: $(ls "$O/store/$T" | wc -l)"
dotnet "$S/tool-check-70/pass/out/Proof.dll" replay "$O/replay.txt" "$O/store" "$GAMES" > "$O/replay.log" 2>&1; echo "replay exit $?"
echo "games replayed: $(cut -f1 "$O/replay.txt" | sort -u | wc -l)"
awk -F'\t' '$2=="ERROR" || $2=="UNREADABLE" || $2=="NOSTATE"' "$O/replay.txt" | cut -c1-240
tail -2 "$O/replay.log" | cut -c1-200
