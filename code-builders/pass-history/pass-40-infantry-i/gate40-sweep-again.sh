#!/bin/bash
# Pass 40, the gate's sweep a third time: every property the pass added to a record that a plan, an event, or a view holds is left out of the
# dump (PROOF_SKIP, Type.Property), so a line differs from the baseline's only where the planner's answer does.
S="/e/Archive/GitHub/dlandi/pass32-tools"
SWEEP="p31c-tw,pass16-demo2,p26-demo,p25-visual,p35-interdict-trap,p35-rally-enemy-turn,p35-smoke-hill,p35-tow2,p35-bog,p35-mud"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
export PROOF_SKIP="GameState.HazardousMovers,GameState.PortagedThisPhase,GameState.InfantryOverrunNtcs,MovementState.RoadRate,MovementState.Dash,MovementState.IpcTo,MovementState.ChangedLocation,MovementState.InfantryOverrun,MovementStepped.RoadRate,MovementStepped.Dash,MovementStepped.IpcTo,MovementStepped.InfantryOverrun,FireAttack.FirerOverstack,FireAttack.TargetOverstack,FireAttack.Dash,CloseCombatUnit.Manning,OrdnanceShot.HazardousTarget,OrdnanceMovement.Dashing,InfantryEntry.OnRoad,SetupLocationFacts.OtherHalf,FireDefinition.Size"
cd "$S" || exit 1
mkdir -p after-40-skip2
echo "[$(date +%H:%M:%S)] sweep"
dotnet "$S/proof-gate/Proof.dll" sweep "$S/after-40-skip2/sweep.txt" "$S/store" "$SWEEP" > "$S/after-40-skip2/sweep.log" 2>&1 || echo "sweep exit $?"
grep -m1 "lines ->" "$S/after-40-skip2/sweep.log" | head -c 300; echo
touch after-40-skip2/DONE
echo "[$(date +%H:%M:%S)] DONE"
