#!/bin/bash
# Pass 72's cut: the games of pass 70 (the pass before the previous one) leave the live folder and the proof store, moved, never deleted
# (the user, 2026-10-25). p70-perf-tw stays in the live folder: it is the measurement copy passes 70 and 71 measured on.
T="5a7d1f000000400080000000000057d0"
L="E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/boards/units/live/$T"
S="E:/Archive/GitHub/dlandi/asl-gate-work/store/$T"
D="E:/Archive/GitHub/dlandi/asl-retired-2026-10-27"
mkdir -p "$D/live" "$D/store"
for f in "$L"/p70-*.game.json; do
  case "$(basename "$f")" in p70-perf-tw.game.json) continue ;; esac
  mv "$f" "$D/live/"
done
for f in "$S"/p70-*.game.json; do mv "$f" "$D/store/"; done
echo "live left: $(ls "$L" | grep -c '^p70-')  store left: $(ls "$S" | grep -c '^p70-')"
echo "retired: live $(ls "$D/live" | wc -l)  store $(ls "$D/store" | wc -l)"
