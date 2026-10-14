#!/bin/bash
# Pass 55's gate: one game's planned events dumped in full from each side (PROOF_DUMP_EVENTS), with the gate's own skip lists.
#   dump_events.sh build         both tools rebuilt, each copy checked against its build
#   dump_events.sh run <game>    the texts go to dbg55/main.txt and dbg55/gate.txt
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
GATE="$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/gate/Debug/net10.0"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
unset PROOF_SKIP PROOF_SKIP_JSON PROOF_DUMP_FIRE PROOF_DUMP_EVENTS
BOTH="GameState.Catalog,DefinitionReference.Catalog"
case "$1" in
build)
  cp "$S/proof/Program.cs" "$S/proof-main/Program.cs"
  dotnet build "$S/proof-main/Proof.csproj" --disable-build-servers -m:1 -v:q 2>&1 | grep -E "error|Build succeeded" | head -3
  dotnet build "$S/proof/Proof.csproj" "-p:StudioBin=$GATE/" "-p:OutDir=$S/proof-gate/" --disable-build-servers -m:1 -v:q 2>&1 | grep -E "error|Build succeeded" | head -3
  cmp -s "$S/proof-gate/LimboDancer.Domains.Asl.Play.dll" "$GATE/LimboDancer.Domains.Asl.Play.dll" || echo "STOP: the gate copy is not the gate's build"
  cmp -s "$S/proof-main/bin/Debug/net10.0/LimboDancer.Domains.Asl.Play.dll" "$S/main-bin/LimboDancer.Domains.Asl.Play.dll" || echo "STOP: the main copy is not the baseline's build"
  ;;
run)
  GAME="$2"
  mkdir -p "$S/dbg55"; rm -f "$S/dbg55/"*
  NEW=$(python "$S/gate/new_properties.py" 846eb5a3 < /dev/null)
  (export PROOF_SKIP="$BOTH" PROOF_DUMP_EVENTS="$S/dbg55/main.txt"; dotnet "$S/proof-main/bin/Debug/net10.0/Proof.dll" sweep "$S/dbg55/sweep-main.txt" "$S/store" "$GAME" > "$S/dbg55/main.log" 2>&1)
  (export PROOF_SKIP="$NEW,$BOTH" PROOF_DUMP_EVENTS="$S/dbg55/gate.txt"; dotnet "$S/proof-gate/Proof.dll" sweep "$S/dbg55/sweep-gate.txt" "$S/store" "$GAME" > "$S/dbg55/gate.log" 2>&1)
  wc -c "$S/dbg55/main.txt" "$S/dbg55/gate.txt"
  diff "$S/dbg55/main.txt" "$S/dbg55/gate.txt" | grep -c '^[<>]'
  diff "$S/dbg55/main.txt" "$S/dbg55/gate.txt" | head -40 | cut -c1-600
  ;;
esac
