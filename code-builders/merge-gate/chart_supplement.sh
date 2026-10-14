#!/bin/bash
# The gate's chart supplement: CI's "Regenerate bounded Scenario A1 chart supplement" step, run locally from bin/gate with --no-build, compared with Python.
source "$(dirname "${BASH_SOURCE[0]}")/../paths.sh"
cd "$R" || exit 1
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
S="$WORK/chart"
mkdir -p "$S"
SW="$(cygpath -w "$S")"
dotnet run --project src/ASL/LimboDancer.Domains.Asl.Authoring.Cli --no-build --disable-build-servers "-p:BaseOutputPath=bin/gate/" -- \
  --source-commit a3254ff1d492dbdd28483d86f5b42437b48e80d4 \
  --registry-output "$SW\\asl-source-registry.json" \
  --verification-output "$SW\\asl-verification-sample.json" \
  --a1-supplement-candidate docs/ASL/SourceRegistry/asl-scenario-a1.backmatter-chart-candidate.json \
  --a1-supplement-output "$SW\\asl-scenario-a1.supplementary-source-registry.json"
echo "cli exit $?"
python "$TOOLS/merge-gate/chart_compare.py" < /dev/null
