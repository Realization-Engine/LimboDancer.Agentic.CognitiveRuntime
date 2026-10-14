#!/bin/bash
# Rebuild before/ from a commit's sources: archive into main-src, build MapStudio into main-bin, rebuild proof-main, run the proofs.
#   rebuild-before.sh <commit>
set -e
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
commit="$1"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
echo "[$(date +%H:%M:%S)] archive $commit"
rm -rf "$S/main-src" "$S/main-bin"
mkdir -p "$S/main-src"
git -C "$R" archive --format=tar -o "$S/main.tar" "$commit"
tar -xf "$S/main.tar" -C "$S/main-src"
echo "[$(date +%H:%M:%S)] build MapStudio"
dotnet build "$S/main-src/src/ASL/LimboDancer.Domains.Asl.MapStudio/LimboDancer.Domains.Asl.MapStudio.csproj" -c Debug -o "$S/main-bin" --disable-build-servers -m:1 -v:q > "$S/build-main.log" 2>&1
tail -3 "$S/build-main.log"
echo "[$(date +%H:%M:%S)] build proof-main"
dotnet build "$S/proof-main/Proof.csproj" --disable-build-servers -m:1 -v:q > "$S/build-proof-main.log" 2>&1
tail -3 "$S/build-proof-main.log"
mv "$S/before" "$S/before-32d"
bash "$S/proofs.sh" before
for f in textlist replay sweep; do
  if cmp -s "$S/before/$f.txt" "$S/after/$f.txt"; then echo "$f: before equals 32.d after"; else echo "$f: DIFFERENT from 32.d after"; fi
done
echo "[$(date +%H:%M:%S)] REBUILT"
