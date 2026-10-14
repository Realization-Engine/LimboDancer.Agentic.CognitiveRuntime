#!/bin/bash
# Build MapStudio into bin/p31b with warnings as errors (my Studio on 6671 must be stopped). The errors, or "ok".
cd /e/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL || exit 1
LOG=/e/Archive/GitHub/dlandi/pass32-tools/pass45/build_studio.log
DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1 dotnet build LimboDancer.Domains.Asl.MapStudio/LimboDancer.Domains.Asl.MapStudio.csproj \
  --disable-build-servers -m:1 -p:OutDir=bin/p31b/ --warnaserror -v:q -nologo > "$LOG" 2>&1
CODE=$?
grep -E "error|warning" "$LOG" | sed -E 's/\[E:.*$//' | sort -u | head -40
echo "exit $CODE"
tail -3 "$LOG"
