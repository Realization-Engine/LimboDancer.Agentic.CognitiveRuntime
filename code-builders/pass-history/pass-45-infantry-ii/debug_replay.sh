#!/bin/bash
# Pass 45, the gate: why guards-06-dlandi stops at revision 563. A build of MapStudio with a temporary dump of the two fact texts into bin/dbg45,
# the proof tool bound to it in a folder of its own, and a replay of that one game.
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
T="5a7d1f000000400080000000000057d0"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
GAMES="${1:-guards-06-dlandi}"
rm -rf "$S/dbg-store" "$S/dbg-out"
mkdir -p "$S/dbg-store/$T" "$S/dbg-out"
for g in ${GAMES//,/ }; do cp "$S/store/$T/$g.game.json" "$S/dbg-store/$T/"; done
cd "$R/src/ASL" || exit 1
dotnet build LimboDancer.Domains.Asl.MapStudio/LimboDancer.Domains.Asl.MapStudio.csproj -c Debug "-p:OutDir=bin/dbg45/" -p:TreatWarningsAsErrors=false --disable-build-servers -m:1 -v:q > "$S/dbg-build.log" 2>&1
tail -3 "$S/dbg-build.log"
dotnet build "$S/proof/Proof.csproj" "-p:StudioBin=$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/dbg45/" "-p:OutDir=$S/proof-dbg/" --disable-build-servers -m:1 -v:q > "$S/dbg-proof-build.log" 2>&1
tail -2 "$S/dbg-proof-build.log"
export TEMP_FACTS_DUMP="$S/dbg-out/facts.txt"
dotnet "$S/proof-dbg/Proof.dll" replay "$S/dbg-out/replay.txt" "$S/dbg-store" > "$S/dbg-out/replay.log" 2>&1
grep -P "\tD\t" "$S/dbg-out/replay.txt" | cut -c1-300
python "$S/pass45/facts_diff.py" "$S/dbg-out/facts.txt" < /dev/null
