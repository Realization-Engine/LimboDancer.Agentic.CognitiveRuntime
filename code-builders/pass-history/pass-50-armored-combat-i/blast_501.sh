#!/bin/bash
# Tasks 50.1 and 50.2: the Studio build, then the test classes the change touches, one project after another.
T=/e/Archive/GitHub/dlandi/pass32-tools/pass50
echo "== studio"; bash $T/build_studio.sh
echo "== Units (whole: the state's records changed)"; bash $T/test.sh Units "FullyQualifiedName~LimboDancer"
echo "== Play"; bash $T/test.sh Play "FullyQualifiedName~BacklogPass50Tests|FullyQualifiedName~BacklogPass6Tests|FullyQualifiedName~BacklogPass7Tests|FullyQualifiedName~VehicleStepsTests|FullyQualifiedName~BacklogPass11Tests|FullyQualifiedName~RallyAndFireStepsTests|FullyQualifiedName~OrdnanceStepsTests|FullyQualifiedName~BacklogPass35Tests|FullyQualifiedName~BacklogPass8Tests"
echo "== MapStudio"; bash $T/test.sh MapStudio "FullyQualifiedName~PlayPageVehicleTests|FullyQualifiedName~PlayPageFireTests|FullyQualifiedName~PlayPageOrdnanceTests|FullyQualifiedName~RallyAndRecordComponentTests|FullyQualifiedName~VehicleComponentTests|FullyQualifiedName~PlayComponentTests|FullyQualifiedName~PlayPagePass35Tests"
echo "== done"; date +%H:%M
