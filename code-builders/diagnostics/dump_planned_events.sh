#!/bin/bash
# Pass 55's gate: one game's planned events dumped in full from each side (PROOF_DUMP_EVENTS), with the gate's own skip lists.
#   dump_planned_events.sh build                      both tools rebuilt, each copy checked against its build
#   dump_planned_events.sh run <game> <base commit>   the texts go to dump-events/main.txt and dump-events/gate.txt in the working folder
source "$(dirname "${BASH_SOURCE[0]}")/../paths.sh"
S="$WORK"
GATE="$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/gate/Debug/net10.0"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
unset PROOF_SKIP PROOF_SKIP_JSON PROOF_DUMP_FIRE PROOF_DUMP_EVENTS
BOTH="GameState.Catalog,DefinitionReference.Catalog"
case "$1" in
build)
  mkdir -p "$S/proof" "$S/proof-main"; cp "$TOOLS/proof-tool/Program.cs" "$TOOLS/proof-tool/Proof.csproj" "$S/proof/"
  cp "$TOOLS/proof-tool/Program.cs" "$S/proof-main/Program.cs"; cp "$TOOLS/proof-tool/Proof.Baseline.csproj" "$S/proof-main/Proof.csproj"
  dotnet build "$S/proof-main/Proof.csproj" "-p:StudioBin=$S/main-bin/" --disable-build-servers -m:1 -v:q 2>&1 | grep -E "error|Build succeeded" | head -3
  dotnet build "$S/proof/Proof.csproj" "-p:StudioBin=$GATE/" "-p:OutDir=$S/proof-gate/" --disable-build-servers -m:1 -v:q 2>&1 | grep -E "error|Build succeeded" | head -3
  cmp -s "$S/proof-gate/LimboDancer.Domains.Asl.Play.dll" "$GATE/LimboDancer.Domains.Asl.Play.dll" || echo "STOP: the gate copy is not the gate's build"
  cmp -s "$S/proof-main/bin/Debug/net10.0/LimboDancer.Domains.Asl.Play.dll" "$S/main-bin/LimboDancer.Domains.Asl.Play.dll" || echo "STOP: the main copy is not the baseline's build"
  ;;
run)
  GAME="$2"
  mkdir -p "$S/dump-events"; rm -f "$S/dump-events/"*
  NEW=$(python "$TOOLS/merge-gate/new_properties.py" "$3" < /dev/null) || exit 1
  (export PROOF_SKIP="$BOTH" PROOF_DUMP_EVENTS="$S/dump-events/main.txt"; dotnet "$S/proof-main/bin/Debug/net10.0/Proof.dll" sweep "$S/dump-events/sweep-main.txt" "$S/store" "$GAME" > "$S/dump-events/main.log" 2>&1)
  (export PROOF_SKIP="$NEW,$BOTH" PROOF_DUMP_EVENTS="$S/dump-events/gate.txt"; dotnet "$S/proof-gate/Proof.dll" sweep "$S/dump-events/sweep-gate.txt" "$S/store" "$GAME" > "$S/dump-events/gate.log" 2>&1)
  wc -c "$S/dump-events/main.txt" "$S/dump-events/gate.txt"
  diff "$S/dump-events/main.txt" "$S/dump-events/gate.txt" | grep -c '^[<>]'
  diff "$S/dump-events/main.txt" "$S/dump-events/gate.txt" | head -40 | cut -c1-600
  ;;
esac
