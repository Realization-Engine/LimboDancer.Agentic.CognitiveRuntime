#!/bin/bash
# Pass 50, the gate's own side after gate50-build.sh: the whole local suite one project at a time from bin/gate, CI's Node viewport test, the
# chart supplement. The proofs are gate50-proofs.sh.
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
cd "$R" || exit 1
echo "[$(date +%H:%M:%S)] head $(git rev-parse --short HEAD) on $(git branch --show-current); suite"
rm -rf "$S/suite"
bash "$S/suite.sh"
echo "[$(date +%H:%M:%S)] node viewport test"
node --test src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/boardViewport.pointer.test.mjs > "$S/node-50.log" 2>&1
echo "node exit $?"; grep -E "^# (tests|pass|fail)" "$S/node-50.log" | tr '\n' ' '; echo
echo "[$(date +%H:%M:%S)] chart supplement"
bash "$S/chart.sh" > "$S/chart-50.log" 2>&1
tail -4 "$S/chart-50.log"
echo "[$(date +%H:%M:%S)] DONE gate"
