#!/bin/bash
# Pass 32.f proofs over the store with the RPh and RtPh cuts added.
#   proofs-f.sh before : the baseline sweep with proof-main (bound to main-bin, 12402fc) into before/sweep.txt
#   proofs-f.sh after  : the replay digest and the sweep with proof (bound to bin/p31b) into after/, then the comparison of all three
S="/e/Archive/GitHub/dlandi/pass32-tools"
side="$1"
case "$side" in
  before)
    echo "[$(date +%H:%M:%S)] baseline sweep p31c-tw (95 cuts)"
    dotnet "$S/proof-main/bin/Debug/net10.0/Proof.dll" sweep "$S/before/sweep.txt" "$S/store" p31c-tw > "$S/before/sweep.log" 2>&1
    grep -m1 "lines ->" "$S/before/sweep.log" | head -c 300; echo
    ;;
  after)
    echo "[$(date +%H:%M:%S)] textlist"
    python "$S/textlist.py" "$S/after/textlist.txt" < /dev/null
    echo "[$(date +%H:%M:%S)] replay"
    dotnet "$S/proof/bin/Debug/net10.0/Proof.dll" replay "$S/after/replay.txt" "$S/store" > "$S/after/replay.log" 2>&1
    tail -1 "$S/after/replay.log" | head -c 300; echo
    echo "[$(date +%H:%M:%S)] sweep p31c-tw (95 cuts)"
    dotnet "$S/proof/bin/Debug/net10.0/Proof.dll" sweep "$S/after/sweep.txt" "$S/store" p31c-tw > "$S/after/sweep.log" 2>&1
    grep -m1 "lines ->" "$S/after/sweep.log" | head -c 300; echo
    ;;
  compare)
    for f in textlist replay sweep; do
      if cmp -s "$S/before/$f.txt" "$S/after/$f.txt"; then echo "$f: EQUAL ($(wc -l < "$S/after/$f.txt") lines)"; else echo "$f: DIFFERENT"; diff "$S/before/$f.txt" "$S/after/$f.txt" | head -40; fi
    done
    ;;
  *) echo "usage: proofs-f.sh before|after|compare"; exit 2 ;;
esac
echo "[$(date +%H:%M:%S)] DONE $side"
