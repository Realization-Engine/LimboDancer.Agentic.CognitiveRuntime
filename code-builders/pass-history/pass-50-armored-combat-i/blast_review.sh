#!/bin/bash
# The reviews' fixes: the test classes the pass touches, one project after another, then the replay digest over the store.
T=/e/Archive/GitHub/dlandi/pass32-tools/pass50
echo "== Rules"; bash $T/test.sh Rules "FullyQualifiedName~ScenarioA1Pass50RulesTests|FullyQualifiedName~ScenarioA1Pass7Tests|FullyQualifiedName~ScenarioA1Pass8Tests|FullyQualifiedName~ScenarioA1Pass9Tests|FullyQualifiedName~ScenarioA1OrdnanceTests|FullyQualifiedName~ScenarioA1VehicleMovementRulesTests|FullyQualifiedName~ScenarioA1VehicleRulesTests|FullyQualifiedName~ScenarioA1Pass35RulesTests|FullyQualifiedName~ScenarioA1VehicleFireTests|FullyQualifiedName~ScenarioA1RoutRallyRulesTests|FullyQualifiedName~ScenarioA1Pass11Tests"
echo "== Units (whole)"; bash $T/test.sh Units "FullyQualifiedName~LimboDancer"
echo "== Play"; bash $T/test.sh Play "FullyQualifiedName~BacklogPass50Tests|FullyQualifiedName~BacklogPass6Tests|FullyQualifiedName~BacklogPass7Tests|FullyQualifiedName~VehicleStepsTests|FullyQualifiedName~BacklogPass11Tests|FullyQualifiedName~OrdnanceStepsTests|FullyQualifiedName~BacklogPass35Tests|FullyQualifiedName~BacklogPass8Tests|FullyQualifiedName~BacklogPass26Tests|FullyQualifiedName~BacklogPass9|FullyQualifiedName~RallyAndFireStepsTests"
echo "== MapStudio (whole)"; bash $T/test.sh MapStudio "FullyQualifiedName~LimboDancer"
echo "== replay"
PROOF_SKIP_50="GameState.VehicleWeaponShots,FireResolved.TurretFacing,RepairAttempted.Weapon,FireVehicleFire.Weapons,FireVehicleFire.OtherPhase,FireVehicleFire.InWoodsOrBuilding,FireWeaponEffect.VehicleWeapon,OrdnanceVehicleFirer.Weapons,FireSpentFacts.VehicleMgUnfired,VehicleStepped.Turret,OrdnanceVehicleFirer.MpInLos,OrdnanceVehicleFirer.BoundingFirer" bash $T/replay_now.sh 2d
echo "== done"; date +%H:%M
