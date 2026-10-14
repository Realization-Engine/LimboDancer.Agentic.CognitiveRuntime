#!/bin/bash
# Replay the pass's own test games (p55-*) from the live folder with the build in bin/p31b, and print each game's diagnostics line when it
# holds any: own_replay.sh. Rebuilds the proof tool against bin/p31b and compares its copy byte for byte.
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
P="$S/pass55"
T="5a7d1f000000400080000000000057d0"
BIN="$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/p31b/"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
dotnet build "$S/proof/Proof.csproj" "-p:StudioBin=$BIN" "-p:OutDir=$S/proof-p55/" --disable-build-servers -m:1 -v:q > "$P/build-proof.log" 2>&1
if ! grep -q "Build succeeded" "$P/build-proof.log"; then echo "STOP: the proof tool did not build"; grep -m5 "error" "$P/build-proof.log"; exit 1; fi
for d in Play Rules Units; do
  cmp -s "$S/proof-p55/LimboDancer.Domains.Asl.$d.dll" "$BIN/LimboDancer.Domains.Asl.$d.dll" || { echo "STOP: the copy of $d is not the build of bin/p31b"; exit 1; }
done
rm -rf "$P/own-store"; mkdir -p "$P/own-store/$T"
cp "$R/src/ASL/boards/units/live/$T"/p55-*.game.json "$P/own-store/$T/"
echo "games: $(ls "$P/own-store/$T" | wc -l)"
dotnet "$S/proof-p55/Proof.dll" replay "$P/own-replay.txt" "$P/own-store" > "$P/own-replay.log" 2>&1 || echo "replay exit $?"
tail -1 "$P/own-replay.log" | head -c 200; echo
grep -P '\tD\t' "$P/own-replay.txt" > /dev/null 2>&1
python "$P/own_diag.py" "$P/own-replay.txt" < /dev/null
