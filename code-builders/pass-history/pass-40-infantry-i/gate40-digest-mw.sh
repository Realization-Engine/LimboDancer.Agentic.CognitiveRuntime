#!/bin/bash
# Pass 40, the replay digest a third time on both sides, with the state's MovedWeapons left out as well as the pass's eight new properties
# (the baseline's build has only MovedWeapons of these). What still differs then has another cause than the moved MG of task 40.7.
S="/e/Archive/GitHub/dlandi/pass32-tools"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
export PROOF_SKIP="GameState.MovedWeapons,GameState.HazardousMovers,GameState.PortagedThisPhase,GameState.InfantryOverrunNtcs,MovementState.RoadRate,MovementState.Dash,MovementState.IpcTo,MovementState.ChangedLocation,MovementState.InfantryOverrun"
cd "$S" || exit 1
python pass40/patch_proof_skip_main.py
dotnet build proof-main/Proof.csproj --disable-build-servers -m:1 -v:q > build-proof-main-mw.log 2>&1
tail -2 build-proof-main-mw.log
mkdir -p before-40-mw after-40-mw
echo "[$(date +%H:%M:%S)] baseline replay"
dotnet proof-main/bin/Debug/net10.0/Proof.dll replay before-40-mw/replay.txt store > before-40-mw/replay.log 2>&1 || echo "baseline replay exit $?"
tail -1 before-40-mw/replay.log | head -c 200; echo
echo "[$(date +%H:%M:%S)] gate replay"
dotnet proof-gate/Proof.dll replay after-40-mw/replay.txt store > after-40-mw/replay.log 2>&1 || echo "gate replay exit $?"
tail -1 after-40-mw/replay.log | head -c 200; echo
touch after-40-mw/DONE
echo "[$(date +%H:%M:%S)] DONE"
