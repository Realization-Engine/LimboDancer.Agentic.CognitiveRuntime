#!/bin/bash
# Pass 32.j proofs: the pass build (bin/p31b, proof) over the store, into after-j/, compared with the baseline 042d14a in before-j/ (rebuild-baseline-j.sh).
#   proofs-j.sh prepare : rebuild the proof against bin/p31b and check the copies' dates
#   proofs-j.sh after   : textlist, replay digest, sweep of p31c-tw, pass16-demo2, p26-demo, p25-visual into after-j/
#   proofs-j.sh compare : the three comparisons
#   proofs-j.sh gate    : the same against proof-gate (bound to bin/gate) into after-j-gate/, then compare
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
side="$1"
run() {
  tool="$1"; out="$2"
  mkdir -p "$S/$out"
  echo "[$(date +%H:%M:%S)] textlist"
  python "$S/textlist.py" "$S/$out/textlist.txt" < /dev/null
  echo "[$(date +%H:%M:%S)] replay"
  dotnet "$S/$tool/bin/Debug/net10.0/Proof.dll" replay "$S/$out/replay.txt" "$S/store" > "$S/$out/replay.log" 2>&1
  tail -1 "$S/$out/replay.log" | head -c 300; echo
  echo "[$(date +%H:%M:%S)] sweep p31c-tw,pass16-demo2,p26-demo,p25-visual"
  dotnet "$S/$tool/bin/Debug/net10.0/Proof.dll" sweep "$S/$out/sweep.txt" "$S/store" p31c-tw,pass16-demo2,p26-demo,p25-visual > "$S/$out/sweep.log" 2>&1
  grep -m1 "lines ->" "$S/$out/sweep.log" | head -c 300; echo
}
rungate() {
  out="$1"
  mkdir -p "$S/$out"
  echo "[$(date +%H:%M:%S)] textlist"
  python "$S/textlist.py" "$S/$out/textlist.txt" < /dev/null
  echo "[$(date +%H:%M:%S)] replay"
  dotnet "$S/proof-gate/Proof.dll" replay "$S/$out/replay.txt" "$S/store" > "$S/$out/replay.log" 2>&1
  tail -1 "$S/$out/replay.log" | head -c 300; echo
  echo "[$(date +%H:%M:%S)] sweep p31c-tw,pass16-demo2,p26-demo,p25-visual"
  dotnet "$S/proof-gate/Proof.dll" sweep "$S/$out/sweep.txt" "$S/store" p31c-tw,pass16-demo2,p26-demo,p25-visual > "$S/$out/sweep.log" 2>&1
  grep -m1 "lines ->" "$S/$out/sweep.log" | head -c 300; echo
}
compare() {
  out="$1"
  for f in textlist replay sweep; do
    if cmp -s "$S/before-j/$f.txt" "$S/$out/$f.txt"; then echo "$f: EQUAL ($(wc -l < "$S/$out/$f.txt") lines)"; else echo "$f: DIFFERENT ($(diff "$S/before-j/$f.txt" "$S/$out/$f.txt" | grep -c '^[<>]') lines)"; diff "$S/before-j/$f.txt" "$S/$out/$f.txt" | head -60; fi
  done
}
case "$side" in
  prepare)
    echo "[$(date +%H:%M:%S)] rebuild proof against bin/p31b"
    dotnet build "$S/proof/Proof.csproj" --disable-build-servers -m:1 -v:q > "$S/build-proof-j.log" 2>&1; tail -2 "$S/build-proof-j.log"
    ls -l --time-style=+%F_%H:%M "$S/proof/bin/Debug/net10.0/LimboDancer.Domains.Asl.Play.dll" "$S/proof/bin/Debug/net10.0/LimboDancer.Domains.Asl.Rules.dll" "$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/p31b/LimboDancer.Domains.Asl.Play.dll" | awk '{print $6, $7}'
    ;;
  after) run proof after-j ;;
  compare) compare after-j ;;
  gate)
    echo "[$(date +%H:%M:%S)] rebuild proof-gate against bin/gate"
    dotnet build "$S/proof/Proof.csproj" "-p:StudioBin=$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/gate/Debug/net10.0/" "-p:OutDir=$S/proof-gate/" --disable-build-servers -m:1 -v:q > "$S/build-proof-gate-j.log" 2>&1; tail -2 "$S/build-proof-gate-j.log"
    ls -l --time-style=+%F_%H:%M "$S/proof-gate/LimboDancer.Domains.Asl.Play.dll" "$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/gate/Debug/net10.0/LimboDancer.Domains.Asl.Play.dll" | awk '{print $6, $7}'
    rungate after-j-gate
    compare after-j-gate
    ;;
  *) echo "usage: proofs-j.sh prepare|after|compare|gate"; exit 2 ;;
esac
echo "[$(date +%H:%M:%S)] DONE $side"
