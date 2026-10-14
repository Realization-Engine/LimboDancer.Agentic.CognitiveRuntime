#!/usr/bin/env bash
cd /e/Archive/GitHub/dlandi/pass32-tools || exit 1
for f in pass11-check-3 p26-check pass7-check; do
  dotnet proof/bin/Debug/net10.0/Proof.dll sweep "after/sweep-$f.txt" store "$f" > "after/sweep-$f.log" 2>&1
  if cmp -s "before/sweep-$f.txt" "after/sweep-$f.txt"; then echo "$f EQUAL $(wc -l < "after/sweep-$f.txt")"; else echo "$f DIFF"; fi
done
