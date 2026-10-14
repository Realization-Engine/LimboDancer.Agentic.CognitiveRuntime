#!/bin/bash
# Pass 50, the gate's build: the solution with warnings as errors into bin/gate. gate50-gate.sh runs the rest once the baseline is done.
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
TAG=${1:-50}
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
cd "$R" || exit 1
echo "[$(date +%H:%M:%S)] head $(git rev-parse --short HEAD) on $(git branch --show-current)"
dotnet build src/ASL/LimboDancer.Domains.Asl.sln -c Debug "-p:BaseOutputPath=bin/gate/" --warnaserror --disable-build-servers -m:1 -v:q > "$S/gate-build-$TAG.log" 2>&1
echo "build exit $?"; tail -4 "$S/gate-build-$TAG.log"
if ! grep -q "Build succeeded" "$S/gate-build-$TAG.log"; then echo "STOP: the build failed"; grep -E "error|warning" "$S/gate-build-$TAG.log" | sed 's/\[.*//' | sort -u | head -40; exit 1; fi
echo "[$(date +%H:%M:%S)] DONE build"
