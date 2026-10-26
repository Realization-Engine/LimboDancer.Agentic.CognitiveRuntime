#!/bin/bash
# Pass 70, 3a: the proof tool now builds the sweep's view through GamePlanner.ViewOf where the build has it (ruling R70.19). Checklist point 4:
# build the changed tool against both sides at once and run it small. Against main's build (main-bin) it must fall back to GameView.Of; against
# my Studio's build (studio-6670) it must take the planner's entry. Outputs go under the working folder and are registered.
source "$(dirname "${BASH_SOURCE[0]}")/../../paths.sh"
S="$WORK"; O="$S/tool-check-70"
rm -rf "$O"; mkdir -p "$O/base" "$O/pass"
bash "$TOOLS/build-and-test/outside_outputs.sh" register "$O"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
cp "$TOOLS/proof-tool/Program.cs" "$O/base/Program.cs"; cp "$TOOLS/proof-tool/Proof.Baseline.csproj" "$O/base/Proof.csproj"
dotnet build "$O/base/Proof.csproj" "-p:StudioBin=$S/main-bin/" "-p:OutDir=$O/base/out/" --disable-build-servers -m:1 -v:q > "$O/base.log" 2>&1
grep -q "Build succeeded" "$O/base.log" && echo "base tool: built" || { echo "base tool: FAILED"; grep -m5 error "$O/base.log"; exit 1; }
cp "$TOOLS/proof-tool/Program.cs" "$TOOLS/proof-tool/Proof.csproj" "$O/pass/"
dotnet build "$O/pass/Proof.csproj" "-p:StudioBin=$S/studio-6670/" "-p:OutDir=$O/pass/out/" --disable-build-servers -m:1 -v:q > "$O/pass.log" 2>&1
grep -q "Build succeeded" "$O/pass.log" && echo "pass tool: built" || { echo "pass tool: FAILED"; grep -m5 error "$O/pass.log"; exit 1; }
GAMES="=dl-tractor-works-02,p65-crew-gun"
dotnet "$O/base/out/Proof.dll" sweep "$O/base-sweep.txt" "$S/store" "$GAMES" > "$O/base-sweep.log" 2>&1; echo "base sweep exit $?"
dotnet "$O/pass/out/Proof.dll" sweep "$O/pass-sweep.txt" "$S/store" "$GAMES" > "$O/pass-sweep.log" 2>&1; echo "pass sweep exit $?"
wc -l "$O/base-sweep.txt" "$O/pass-sweep.txt"
awk -F'\t' '$2=="ERROR" || $2=="UNREADABLE" || $2=="NOSTATE"' "$O/base-sweep.txt" "$O/pass-sweep.txt" | cut -c1-200
echo "lines that differ: $(diff "$O/base-sweep.txt" "$O/pass-sweep.txt" | grep -c '^[<>]')"
diff "$O/base-sweep.txt" "$O/pass-sweep.txt" | grep '^[<>]' | cut -f1-3 | cut -c1-160 | head -12
