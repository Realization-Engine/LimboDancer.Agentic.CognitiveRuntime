#!/bin/bash
# Pass 32.g proofs over the store with the APh and CCPh cuts (127 p31c-tw cuts): the pass build (bin/p31b, proof) into after-g/, compared with the
# baseline 0919e8e in before/ (prepare-g.sh).
#   proofs-g.sh after   : textlist, replay digest, sweep into after-g/
#   proofs-g.sh compare : the three comparisons
S="/e/Archive/GitHub/dlandi/pass32-tools"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
side="$1"
case "$side" in
  after)
    mkdir -p "$S/after-g"
    echo "[$(date +%H:%M:%S)] textlist"
    python "$S/textlist.py" "$S/after-g/textlist.txt" < /dev/null
    echo "[$(date +%H:%M:%S)] replay"
    dotnet "$S/proof/bin/Debug/net10.0/Proof.dll" replay "$S/after-g/replay.txt" "$S/store" > "$S/after-g/replay.log" 2>&1
    tail -1 "$S/after-g/replay.log" | head -c 300; echo
    echo "[$(date +%H:%M:%S)] sweep p31c-tw (127 cuts)"
    dotnet "$S/proof/bin/Debug/net10.0/Proof.dll" sweep "$S/after-g/sweep.txt" "$S/store" p31c-tw > "$S/after-g/sweep.log" 2>&1
    grep -m1 "lines ->" "$S/after-g/sweep.log" | head -c 300; echo
    ;;
  compare)
    for f in textlist replay sweep; do
      if cmp -s "$S/before/$f.txt" "$S/after-g/$f.txt"; then echo "$f: EQUAL ($(wc -l < "$S/after-g/$f.txt") lines)"; else echo "$f: DIFFERENT ($(diff "$S/before/$f.txt" "$S/after-g/$f.txt" | grep -c '^[<>]') lines)"; diff "$S/before/$f.txt" "$S/after-g/$f.txt" | head -40; fi
    done
    ;;
  *) echo "usage: proofs-g.sh after|compare"; exit 2 ;;
esac
echo "[$(date +%H:%M:%S)] DONE $side"
