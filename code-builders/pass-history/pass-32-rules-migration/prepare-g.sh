#!/bin/bash
# Pass 32.g's proof preparation: cuts at every APh and CCPh entry, the fix build's proofs over the enlarged store, the main baseline (3f96c38) for the
# upstairs fix's differences, then the pass's baseline (0919e8e) into before/.
set -e
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
cd "$S"
echo "[$(date +%H:%M:%S)] cuts aph, ccph"
dotnet proof/bin/Debug/net10.0/Proof.dll cut cut-aph.txt store p31c-tw aph | tail -1
dotnet proof/bin/Debug/net10.0/Proof.dll cut cut-ccph.txt store p31c-tw ccph | tail -1
ls store/*/ | grep -c "p31c-tw-r"

echo "[$(date +%H:%M:%S)] fix build (bin/p31b) proofs into after-fix/"
mkdir -p after-fix
python textlist.py after-fix/textlist.txt < /dev/null
dotnet proof/bin/Debug/net10.0/Proof.dll replay after-fix/replay.txt store > after-fix/replay.log 2>&1; tail -1 after-fix/replay.log | head -c 200; echo
dotnet proof/bin/Debug/net10.0/Proof.dll sweep after-fix/sweep.txt store p31c-tw > after-fix/sweep.log 2>&1; grep -m1 "lines ->" after-fix/sweep.log | head -c 200; echo

build_baseline() {
  local commit="$1" out="$2"
  echo "[$(date +%H:%M:%S)] archive $commit"
  rm -rf main-src main-bin
  mkdir -p main-src
  git -C "$R" archive --format=tar -o main.tar "$commit"
  tar -xf main.tar -C main-src
  echo "[$(date +%H:%M:%S)] build MapStudio"
  dotnet build main-src/src/ASL/LimboDancer.Domains.Asl.MapStudio/LimboDancer.Domains.Asl.MapStudio.csproj -c Debug -o main-bin --disable-build-servers -m:1 -v:q > "build-main-$commit.log" 2>&1
  tail -2 "build-main-$commit.log"
  dotnet build proof-main/Proof.csproj --disable-build-servers -m:1 -v:q > "build-proof-main-$commit.log" 2>&1
  mkdir -p "$out"
  echo "[$(date +%H:%M:%S)] baseline $commit proofs into $out"
  python textlist.py "$out/textlist.txt" main-src/src/ASL < /dev/null
  dotnet proof-main/bin/Debug/net10.0/Proof.dll replay "$out/replay.txt" store > "$out/replay.log" 2>&1; tail -1 "$out/replay.log" | head -c 200; echo
  dotnet proof-main/bin/Debug/net10.0/Proof.dll sweep "$out/sweep.txt" store p31c-tw > "$out/sweep.log" 2>&1; grep -m1 "lines ->" "$out/sweep.log" | head -c 200; echo
}

build_baseline 3f96c38 before-main
echo "[$(date +%H:%M:%S)] the fix against main:"
for f in textlist replay sweep; do
  if cmp -s "before-main/$f.txt" "after-fix/$f.txt"; then echo "$f: EQUAL"; else echo "$f: DIFFERENT ($(diff before-main/$f.txt after-fix/$f.txt | grep -c '^[<>]') lines)"; fi
done

rm -rf before-32f; mv before before-32f
build_baseline 0919e8e before
echo "[$(date +%H:%M:%S)] the pass baseline against the fix build:"
for f in textlist replay sweep; do
  if cmp -s "before/$f.txt" "after-fix/$f.txt"; then echo "$f: EQUAL"; else echo "$f: DIFFERENT"; fi
done
echo "[$(date +%H:%M:%S)] PREPARED"
