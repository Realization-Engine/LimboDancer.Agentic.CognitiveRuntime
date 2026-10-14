#!/bin/bash
# The three proofs of a migration pass (design section 8; 32.a as built, section 16).
#   proofs.sh before   : textlist, replay digest, and sweep with proof-main (bound to main-bin) into before/
#   proofs.sh after    : the same with proof (bound to bin/p31b) into after/, then the comparison
#   proofs.sh textlist : the text list alone into after/ with the comparison
S="/e/Archive/GitHub/dlandi/pass32-tools"
side="$1"
case "$side" in
  before) tool="$S/proof-main/bin/Debug/net10.0/Proof.dll"; out="$S/before" ;;
  after|textlist|runs) tool="$S/proof/bin/Debug/net10.0/Proof.dll"; out="$S/after" ;;
  *) echo "usage: proofs.sh before|after|textlist|runs"; exit 2 ;;
esac
mkdir -p "$out"
if [ "$side" != "runs" ]; then
  echo "[$(date +%H:%M:%S)] textlist"
  python "$S/textlist.py" "$out/textlist.txt" < /dev/null
fi
if [ "$side" != "textlist" ]; then
  echo "[$(date +%H:%M:%S)] replay"
  dotnet "$tool" replay "$out/replay.txt" "$S/store" > "$out/replay.log" 2>&1
  tail -1 "$out/replay.log" | head -c 300; echo
  echo "[$(date +%H:%M:%S)] sweep p31c-tw"
  dotnet "$tool" sweep "$out/sweep.txt" "$S/store" p31c-tw > "$out/sweep.log" 2>&1
  grep -m1 "lines ->" "$out/sweep.log" | head -c 300; echo
fi
if [ "$side" != "before" ]; then
  echo "[$(date +%H:%M:%S)] compare"
  for f in textlist replay sweep; do
    if [ -f "$S/before/$f.txt" ] && [ -f "$S/after/$f.txt" ]; then
      if cmp -s "$S/before/$f.txt" "$S/after/$f.txt"; then echo "$f: EQUAL ($(wc -l < "$S/after/$f.txt") lines)"; else echo "$f: DIFFERENT"; diff "$S/before/$f.txt" "$S/after/$f.txt" | head -20; fi
    fi
  done
fi
echo "[$(date +%H:%M:%S)] DONE"
