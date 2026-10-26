#!/bin/bash
# Pass 70, 3b: the recorded games that hold an Interdiction, a fire at loaded PRC, an Unprotected crew, or Riders replay as they did. The proof tool, built
# against main's build (main-bin) and against my Studio's (studio-6670), sweeps the p65 games and dl-tractor-works-02 from the gate's store; no line may be
# ERROR, UNREADABLE, or NOSTATE, and every line that differs is named (3a's GameView lines of dl-tractor-works-02 are expected). Outputs go under the
# working folder and are registered.
source "$(dirname "${BASH_SOURCE[0]}")/../../paths.sh"
S="$WORK"; O="$S/tool-check-70b"
rm -rf "$O"; mkdir -p "$O/base" "$O/pass"
bash "$TOOLS/build-and-test/outside_outputs.sh" register "$O"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
cp "$TOOLS/proof-tool/Program.cs" "$O/base/Program.cs"; cp "$TOOLS/proof-tool/Proof.Baseline.csproj" "$O/base/Proof.csproj"
dotnet build "$O/base/Proof.csproj" "-p:StudioBin=$S/main-bin/" "-p:OutDir=$O/base/out/" --disable-build-servers -m:1 -v:q > "$O/base.log" 2>&1
grep -q "Build succeeded" "$O/base.log" && echo "base tool: built" || { echo "base tool: FAILED"; grep -m5 error "$O/base.log"; exit 1; }
cp "$TOOLS/proof-tool/Program.cs" "$TOOLS/proof-tool/Proof.csproj" "$O/pass/"
dotnet build "$O/pass/Proof.csproj" "-p:StudioBin=$S/studio-6670/" "-p:OutDir=$O/pass/out/" --disable-build-servers -m:1 -v:q > "$O/pass.log" 2>&1
grep -q "Build succeeded" "$O/pass.log" && echo "pass tool: built" || { echo "pass tool: FAILED"; grep -m5 error "$O/pass.log"; exit 1; }
NAMES=$(ls "$S/store/"*/p65-*.game.json | xargs -n1 basename | sed 's/\.game\.json$//' | paste -sd, -)
GAMES="=dl-tractor-works-02,$NAMES"
dotnet "$O/base/out/Proof.dll" sweep "$O/base-sweep.txt" "$S/store" "$GAMES" > "$O/base-sweep.log" 2>&1; echo "base sweep exit $?"
dotnet "$O/pass/out/Proof.dll" sweep "$O/pass-sweep.txt" "$S/store" "$GAMES" > "$O/pass-sweep.log" 2>&1; echo "pass sweep exit $?"
wc -l "$O/base-sweep.txt" "$O/pass-sweep.txt"
awk -F'\t' '$2=="ERROR" || $2=="UNREADABLE" || $2=="NOSTATE"' "$O/base-sweep.txt" "$O/pass-sweep.txt" | cut -c1-200
echo "lines that differ: $(diff "$O/base-sweep.txt" "$O/pass-sweep.txt" | grep -c '^[<>]')"
diff "$O/base-sweep.txt" "$O/pass-sweep.txt" | grep '^[<>]' | cut -f1-3 | cut -c1-160 | head -20
