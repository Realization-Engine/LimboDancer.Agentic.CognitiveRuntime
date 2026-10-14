#!/bin/bash
# The gate-build proofs for passes 32.h and 32.i: proof-gate (bound to bin/gate/Debug/net10.0/) over the store into after-hi-gate/, compared with before-hi/.
S="/e/Archive/GitHub/dlandi/pass32-tools"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
case "$1" in
  run)
    mkdir -p "$S/after-hi-gate"
    python "$S/textlist.py" "$S/after-hi-gate/textlist.txt" < /dev/null
    echo "[$(date +%H:%M:%S)] replay"
    dotnet "$S/proof-gate/Proof.dll" replay "$S/after-hi-gate/replay.txt" "$S/store" > "$S/after-hi-gate/replay.log" 2>&1
    tail -1 "$S/after-hi-gate/replay.log" | head -c 300; echo
    echo "[$(date +%H:%M:%S)] sweep p31c-tw,pass16-demo2"
    dotnet "$S/proof-gate/Proof.dll" sweep "$S/after-hi-gate/sweep.txt" "$S/store" p31c-tw,pass16-demo2 > "$S/after-hi-gate/sweep.log" 2>&1
    grep -m1 "lines ->" "$S/after-hi-gate/sweep.log" | head -c 300; echo
    ;;
  compare)
    for f in textlist replay sweep; do
      if cmp -s "$S/before-hi/$f.txt" "$S/after-hi-gate/$f.txt"; then echo "$f: EQUAL ($(wc -l < "$S/after-hi-gate/$f.txt") lines)"; else echo "$f: DIFFERENT ($(diff "$S/before-hi/$f.txt" "$S/after-hi-gate/$f.txt" | grep -c '^[<>]') lines)"; diff "$S/before-hi/$f.txt" "$S/after-hi-gate/$f.txt" | head -60; fi
    done
    ;;
esac
echo "[$(date +%H:%M:%S)] DONE $1"
