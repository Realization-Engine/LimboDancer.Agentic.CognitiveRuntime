#!/bin/bash
# Pass 40, the gate's replay digest and sweep again, with the eight properties the pass added to the game state left out of the dump
# (PROOF_SKIP), so the two sides compare what both builds hold. The proof tool is rebuilt against bin/gate first.
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
SWEEP="p31c-tw,pass16-demo2,p26-demo,p25-visual,p35-interdict-trap,p35-rally-enemy-turn,p35-smoke-hill,p35-tow2,p35-bog,p35-mud"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
export PROOF_SKIP="GameState.HazardousMovers,GameState.PortagedThisPhase,GameState.InfantryOverrunNtcs,MovementState.RoadRate,MovementState.Dash,MovementState.IpcTo,MovementState.ChangedLocation,MovementState.InfantryOverrun"
cd "$S" || exit 1
rm -f after-40-gate/DONE2
dotnet build "$S/proof/Proof.csproj" "-p:StudioBin=$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/gate/Debug/net10.0/" "-p:OutDir=$S/proof-gate/" --disable-build-servers -m:1 -v:q > "$S/build-proof-gate-40b.log" 2>&1
tail -2 "$S/build-proof-gate-40b.log"
mkdir -p after-40-skip
echo "[$(date +%H:%M:%S)] replay"
dotnet "$S/proof-gate/Proof.dll" replay "$S/after-40-skip/replay.txt" "$S/store" > "$S/after-40-skip/replay.log" 2>&1 || echo "replay exit $?"
tail -1 "$S/after-40-skip/replay.log" | head -c 300; echo
echo "[$(date +%H:%M:%S)] sweep"
dotnet "$S/proof-gate/Proof.dll" sweep "$S/after-40-skip/sweep.txt" "$S/store" "$SWEEP" > "$S/after-40-skip/sweep.log" 2>&1 || echo "sweep exit $?"
grep -m1 "lines ->" "$S/after-40-skip/sweep.log" | head -c 300; echo
touch after-40-gate/DONE2
echo "[$(date +%H:%M:%S)] DONE"
