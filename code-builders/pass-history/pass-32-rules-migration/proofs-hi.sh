#!/bin/bash
# Pass 32.h and 32.i proofs: the pass build (bin/p31b, proof) over the store with the PFPh, DFPh, and AFPh cuts, into after-hi/, compared with
# the baseline 8c8f64a in before-hi/ (rebuild-baseline-hi.sh).
#   proofs-hi.sh prepare : rebuild the proof against bin/p31b, check the copies' dates, cut p31c-tw at every PFPh, DFPh, and AFPh entry
#   proofs-hi.sh after   : textlist, replay digest, sweep of p31c-tw and pass16-demo2 into after-hi/
#   proofs-hi.sh compare : the three comparisons
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
side="$1"
case "$side" in
  prepare)
    echo "[$(date +%H:%M:%S)] rebuild proof against bin/p31b"
    dotnet build "$S/proof/Proof.csproj" --disable-build-servers -m:1 -v:q > "$S/build-proof-hi.log" 2>&1; tail -2 "$S/build-proof-hi.log"
    ls -l --time-style=+%F_%H:%M "$S/proof/bin/Debug/net10.0/LimboDancer.Domains.Asl.Play.dll" "$S/proof/bin/Debug/net10.0/LimboDancer.Domains.Asl.Rules.dll" "$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/p31b/LimboDancer.Domains.Asl.Play.dll" | awk '{print $6, $7}'
    echo "[$(date +%H:%M:%S)] cuts pfph, dfph, afph"
    for phase in pfph dfph afph; do dotnet "$S/proof/bin/Debug/net10.0/Proof.dll" cut "$S/cut-$phase.txt" "$S/store" p31c-tw "$phase" | tail -1; done
    echo "store games: $(ls $S/store/*/ | wc -l); p31c-tw cuts: $(ls $S/store/*/ | grep -c 'p31c-tw-r')"
    ;;
  after)
    mkdir -p "$S/after-hi"
    echo "[$(date +%H:%M:%S)] textlist"
    python "$S/textlist.py" "$S/after-hi/textlist.txt" < /dev/null
    echo "[$(date +%H:%M:%S)] replay"
    dotnet "$S/proof/bin/Debug/net10.0/Proof.dll" replay "$S/after-hi/replay.txt" "$S/store" > "$S/after-hi/replay.log" 2>&1
    tail -1 "$S/after-hi/replay.log" | head -c 300; echo
    echo "[$(date +%H:%M:%S)] sweep p31c-tw,pass16-demo2"
    dotnet "$S/proof/bin/Debug/net10.0/Proof.dll" sweep "$S/after-hi/sweep.txt" "$S/store" p31c-tw,pass16-demo2 > "$S/after-hi/sweep.log" 2>&1
    grep -m1 "lines ->" "$S/after-hi/sweep.log" | head -c 300; echo
    ;;
  compare)
    for f in textlist replay sweep; do
      if cmp -s "$S/before-hi/$f.txt" "$S/after-hi/$f.txt"; then echo "$f: EQUAL ($(wc -l < "$S/after-hi/$f.txt") lines)"; else echo "$f: DIFFERENT ($(diff "$S/before-hi/$f.txt" "$S/after-hi/$f.txt" | grep -c '^[<>]') lines)"; diff "$S/before-hi/$f.txt" "$S/after-hi/$f.txt" | head -60; fi
    done
    ;;
  *) echo "usage: proofs-hi.sh prepare|after|compare"; exit 2 ;;
esac
echo "[$(date +%H:%M:%S)] DONE $side"
