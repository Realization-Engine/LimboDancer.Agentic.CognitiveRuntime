#!/bin/bash
# Pass 71's replay check of its own games (the whole-pass check): the proof tool built against my Studio's build (studio-6670), then every
# p71-*.game.json of the live folder copied into a store of its own under the working folder (the live folder, where the user's Studio appends to
# its own game, is never read by the tool) and replayed. Each game must replay to its last event: no ERROR, UNREADABLE, or NOSTATE line.
# Pass 70's replay_p70.sh and tool_check_3a.sh, in one script.
source "$(dirname "${BASH_SOURCE[0]}")/../../paths.sh"
S="$WORK"; T="5a7d1f000000400080000000000057d0"
LIVE="$R/src/ASL/boards/units/live/$T"
O="$S/replay-71"; rm -rf "$O"; mkdir -p "$O/store/$T" "$O/tool"
bash "$TOOLS/build-and-test/outside_outputs.sh" register "$O"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
cp "$TOOLS/proof-tool/Program.cs" "$TOOLS/proof-tool/Proof.csproj" "$O/tool/"
dotnet build "$O/tool/Proof.csproj" "-p:StudioBin=$S/studio-6670/" "-p:OutDir=$O/tool/out/" --disable-build-servers -m:1 -v:q > "$O/tool.log" 2>&1
grep -q "Build succeeded" "$O/tool.log" && echo "tool: built" || { echo "tool: FAILED"; grep -m5 error "$O/tool.log"; exit 1; }
cmp -s "$O/tool/out/LimboDancer.Domains.Asl.Play.dll" "$S/studio-6670/LimboDancer.Domains.Asl.Play.dll" && echo "tool's Play.dll is the Studio's" || { echo "STOP: the tool's copy is not the Studio's build"; exit 1; }
for f in "$LIVE"/p71-*.game.json; do cp "$f" "$O/store/$T/"; done
GAMES=$(ls "$O/store/$T" | sed -n 's/\.game\.json$//p' | paste -sd, -)
echo "games: $(ls "$O/store/$T" | wc -l)"
dotnet "$O/tool/out/Proof.dll" replay "$O/replay.txt" "$O/store" "$GAMES" > "$O/replay.log" 2>&1; echo "replay exit $?"
echo "games replayed: $(cut -f1 "$O/replay.txt" | sort -u | wc -l)"
awk -F'\t' '$2=="ERROR" || $2=="UNREADABLE" || $2=="NOSTATE"' "$O/replay.txt" | cut -c1-240
tail -2 "$O/replay.log" | cut -c1-200
