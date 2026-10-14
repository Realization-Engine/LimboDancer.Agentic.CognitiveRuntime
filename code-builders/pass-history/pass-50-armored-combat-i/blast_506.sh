#!/bin/bash
# Task 50.6: the Studio build, the test classes the change touches, then the replay digest over the store.
T=/e/Archive/GitHub/dlandi/pass32-tools/pass50
echo "== studio"; bash $T/build_studio.sh
echo "== Rules"; bash $T/test.sh Rules "FullyQualifiedName~ScenarioA1Pass50RulesTests|FullyQualifiedName~ScenarioA1Pass7Tests|FullyQualifiedName~ScenarioA1Pass8Tests|FullyQualifiedName~ScenarioA1Pass9Tests|FullyQualifiedName~ScenarioA1OrdnanceTests|FullyQualifiedName~ScenarioA1Pass35RulesTests"
echo "== Play"; bash $T/test.sh Play "FullyQualifiedName~BacklogPass50Tests|FullyQualifiedName~BacklogPass7Tests|FullyQualifiedName~OrdnanceStepsTests|FullyQualifiedName~BacklogPass8Tests|FullyQualifiedName~BacklogPass9|FullyQualifiedName~BacklogPass35Tests"
echo "== MapStudio"; bash $T/test.sh MapStudio "FullyQualifiedName~PlayPageOrdnanceTests|FullyQualifiedName~PlayPagePass35Tests"
echo "== replay"
PROOF_SKIP_50="GameState.VehicleWeaponShots,FireResolved.TurretFacing,RepairAttempted.Weapon,FireVehicleFire.Weapons,FireVehicleFire.OtherPhase,FireVehicleFire.InWoodsOrBuilding,FireWeaponEffect.VehicleWeapon,OrdnanceVehicleFirer.Weapons,FireSpentFacts.VehicleMgUnfired,VehicleStepped.Turret,OrdnanceVehicleFirer.MpInLos,OrdnanceVehicleFirer.BoundingFirer" bash $T/replay_now.sh 2c
echo "== done"; date +%H:%M
