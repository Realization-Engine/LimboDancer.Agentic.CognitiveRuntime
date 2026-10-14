#!/bin/bash
# Pass 50, the gate: one game's fire plans dumped in full from each side (PROOF_DUMP_FIRE), to name a fact the skip lists miss.
# dump_fire.sh <game>; the texts go to dbg50/main.txt and dbg50/gate.txt.
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
GAME=${1:-pass16-demo2}
GATE="$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/gate/Debug/net10.0/"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
cd "$S" || exit 1
cp proof/Program.cs proof-main/Program.cs
dotnet build proof-main/Proof.csproj --disable-build-servers -m:1 -v:q 2>&1 | grep -E "error|Error" | head -3
dotnet build proof/Proof.csproj "-p:StudioBin=$GATE" "-p:OutDir=$S/proof-gate/" --disable-build-servers -m:1 -v:q 2>&1 | grep -E "error|Error" | head -3
cmp -s proof-gate/LimboDancer.Domains.Asl.Play.dll "$GATE/LimboDancer.Domains.Asl.Play.dll" || { echo "STOP: the copy is not the gate's build"; exit 1; }
cmp -s proof-main/bin/Debug/net10.0/LimboDancer.Domains.Asl.Play.dll main-bin/LimboDancer.Domains.Asl.Play.dll || { echo "STOP: the copy is not the baseline's build"; exit 1; }
mkdir -p dbg50; rm -f dbg50/*
(unset PROOF_SKIP PROOF_SKIP_JSON; PROOF_DUMP_FIRE="$S/dbg50/main.txt" dotnet proof-main/bin/Debug/net10.0/Proof.dll sweep dbg50/sweep-main.txt store "$GAME" > dbg50/main.log 2>&1)
eval "$(grep -E '^export PROOF_SKIP' gate50-proofs.sh)"
PROOF_DUMP_FIRE="$S/dbg50/gate.txt" dotnet proof-gate/Proof.dll sweep dbg50/sweep-gate.txt store "$GAME" > dbg50/gate.log 2>&1
wc -c dbg50/main.txt dbg50/gate.txt
python "$S/pass50/dump_fire_diff.py" < /dev/null
