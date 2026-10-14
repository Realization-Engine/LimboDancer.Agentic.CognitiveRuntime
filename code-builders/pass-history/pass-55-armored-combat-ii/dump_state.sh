#!/bin/bash
# One state of one game in full from each side, and the lines that differ: dump_state.sh <game> <state number> [store]
# Rebuilds both proof tools from proof/Program.cs (the base's against pass55/base-bin, the branch's against bin/p31b).
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
P="$S/pass55"
STORE="${3:-$P/trial-store}"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
export PROOF_SKIP="GameState.Catalog,DefinitionReference.Catalog${PROOF_SKIP_55:+,$PROOF_SKIP_55}"
cp "$S/proof/Program.cs" "$S/proof-base55/Program.cs"
dotnet build "$S/proof-base55/Proof.csproj" "-p:StudioBin=$P/base-bin/" "-p:OutDir=$S/proof-base55/out/" --disable-build-servers -m:1 -v:q 2>&1 | grep -E "error|Build succeeded" | head -5
dotnet build "$S/proof/Proof.csproj" "-p:StudioBin=$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/p31b/" "-p:OutDir=$S/proof-p55/" --disable-build-servers -m:1 -v:q 2>&1 | grep -E "error|Build succeeded" | head -5
mkdir -p "$P/dump"
PROOF_DUMP_STATE="$P/dump/base.txt|$1|$2" dotnet "$S/proof-base55/out/Proof.dll" replay "$P/dump/base-replay.txt" "$STORE" > /dev/null 2>&1
PROOF_DUMP_STATE="$P/dump/now.txt|$1|$2" dotnet "$S/proof-p55/Proof.dll" replay "$P/dump/now-replay.txt" "$STORE" > /dev/null 2>&1
wc -c "$P/dump/base.txt" "$P/dump/now.txt"
python "$P/dump_diff.py" "$P/dump/base.txt" "$P/dump/now.txt" < /dev/null
