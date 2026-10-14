#!/bin/bash
# Pass 40, the gate's own side: the solution build with warnings as errors into bin/gate, the whole local suite one project at a time, CI's Node
# viewport test, the chart supplement, then the proof-gate tool bound to bin/gate and the three proofs into after-40-gate/. The comparison with
# before-40/ is made by hand once both sides are done. Progress goes to this script's output; each step has its own log.
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
SWEEP="p31c-tw,pass16-demo2,p26-demo,p25-visual,p35-interdict-trap,p35-rally-enemy-turn,p35-smoke-hill,p35-tow2,p35-bog,p35-mud"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
cd "$R" || exit 1
echo "[$(date +%H:%M:%S)] head $(git rev-parse --short HEAD) on $(git branch --show-current)"
echo "[$(date +%H:%M:%S)] solution build, warnings as errors"
dotnet build src/ASL/LimboDancer.Domains.Asl.sln -c Debug "-p:BaseOutputPath=bin/gate/" --warnaserror --disable-build-servers -m:1 -v:q > "$S/gate-build-40.log" 2>&1
echo "build exit $?"; tail -4 "$S/gate-build-40.log"
if ! grep -q "Build succeeded" "$S/gate-build-40.log"; then echo "STOP: the build failed"; exit 1; fi
echo "[$(date +%H:%M:%S)] suite"
rm -rf "$S/suite"
bash "$S/suite.sh"
echo "[$(date +%H:%M:%S)] node viewport test"
node --test src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/boardViewport.pointer.test.mjs > "$S/node-40.log" 2>&1
echo "node exit $?"; grep -E "^# (tests|pass|fail)" "$S/node-40.log" | tr '\n' ' '; echo
echo "[$(date +%H:%M:%S)] chart supplement"
bash "$S/chart.sh" > "$S/chart-40.log" 2>&1
tail -4 "$S/chart-40.log"
echo "[$(date +%H:%M:%S)] proof-gate against bin/gate"
while [ ! -f "$S/store.ready" ]; do sleep 5; done
dotnet build "$S/proof/Proof.csproj" "-p:StudioBin=$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/gate/Debug/net10.0/" "-p:OutDir=$S/proof-gate/" --disable-build-servers -m:1 -v:q > "$S/build-proof-gate-40.log" 2>&1
tail -2 "$S/build-proof-gate-40.log"
ls -l --time-style=+%F_%H:%M "$S/proof-gate/LimboDancer.Domains.Asl.Play.dll" "$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/gate/Debug/net10.0/LimboDancer.Domains.Asl.Play.dll" | awk '{print $6, $7}'
mkdir -p "$S/after-40-gate"
python "$S/textlist.py" "$S/after-40-gate/textlist.txt" < /dev/null
echo "[$(date +%H:%M:%S)] replay"
dotnet "$S/proof-gate/Proof.dll" replay "$S/after-40-gate/replay.txt" "$S/store" > "$S/after-40-gate/replay.log" 2>&1 || echo "replay exit $?"
tail -1 "$S/after-40-gate/replay.log" | head -c 300; echo
echo "[$(date +%H:%M:%S)] sweep $SWEEP"
dotnet "$S/proof-gate/Proof.dll" sweep "$S/after-40-gate/sweep.txt" "$S/store" "$SWEEP" > "$S/after-40-gate/sweep.log" 2>&1 || echo "sweep exit $?"
grep -m1 "lines ->" "$S/after-40-gate/sweep.log" | head -c 300; echo
touch "$S/after-40-gate/DONE"
echo "[$(date +%H:%M:%S)] DONE gate"
