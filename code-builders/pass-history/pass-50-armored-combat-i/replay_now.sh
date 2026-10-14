#!/bin/bash
# The replay digest over the proof store with the build in bin/p31b: replay_now.sh <tag>. The proof tool is rebuilt against bin/p31b and its copy
# compared byte for byte; the digest goes to after-50-<tag>/replay.txt and is compared with pass 45's last (main's code at b4ce31ef).
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
TAG=$1
BIN="$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/p31b/"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
export PROOF_SKIP="FireArithmetic.ResidualFpIfForfeited,FireAttack.CoweringMarksAndWeaponResidual,FireAttack.MgBoreSighted,FireDefinition.SprayingFire,FireDirector.DirectedTheseMgs,FireFirer.Berserk,FireResolution.CoweredUnitIds,FireResolution.FirerCounters,FireRolls.CowerSelection,FireWeaponEffect.Removed,FirerLocationFacts.Ordnance,FirerRecordFacts.Berserk,FirerRecordFacts.Inexperienced,MovingUnitFacts.Cx,FireKindFacts.EveryFirstFireMarkedFiresAlone,${PROOF_SKIP_50}"
export PROOF_SKIP_JSON="coweringMarksAndWeaponResidual"
dotnet build "$S/proof/Proof.csproj" "-p:StudioBin=$BIN" "-p:OutDir=$S/proof-p50/" --disable-build-servers -m:1 -v:q > "$S/pass50/build-proof.log" 2>&1
tail -3 "$S/pass50/build-proof.log"
if ! grep -q "Build succeeded" "$S/pass50/build-proof.log"; then echo "STOP: the proof tool did not build"; grep -m5 "error" "$S/pass50/build-proof.log"; exit 1; fi
if ! cmp -s "$S/proof-p50/LimboDancer.Domains.Asl.Play.dll" "$BIN/LimboDancer.Domains.Asl.Play.dll"; then echo "STOP: the copy is not the build of bin/p31b"; exit 1; fi
mkdir -p "$S/after-50-$TAG"
echo "[$(date +%H:%M:%S)] replay"
dotnet "$S/proof-p50/Proof.dll" replay "$S/after-50-$TAG/replay.txt" "$S/store" > "$S/after-50-$TAG/replay.log" 2>&1 || echo "replay exit $?"
tail -1 "$S/after-50-$TAG/replay.log" | head -c 300; echo
echo "lines: $(wc -l < "$S/after-45-final/replay.txt") before, $(wc -l < "$S/after-50-$TAG/replay.txt") now"
echo "lines that differ: $(diff "$S/after-45-final/replay.txt" "$S/after-50-$TAG/replay.txt" | grep -c '^[<>]')"
diff "$S/after-45-final/replay.txt" "$S/after-50-$TAG/replay.txt" | grep '^[<>]' | cut -c1-200 | head -20
echo "[$(date +%H:%M:%S)] DONE"
