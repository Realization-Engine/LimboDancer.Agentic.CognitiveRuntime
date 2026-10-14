#!/bin/bash
# Pass 55's gate: the baseline's sweep again, after the proof tool learned to pass over a game the baseline cannot read.
#   resweep_main.sh build          the tool rebuilt against the baseline's build, its copy checked
#   resweep_main.sh small          the pass's four sweep games only, into the scratch file (the short run before the long one)
#   resweep_main.sh full           the gate's whole sweep list, into before-55/sweep.txt
S="/e/Archive/GitHub/dlandi/pass32-tools"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
unset PROOF_SKIP PROOF_SKIP_JSON PROOF_DUMP_FIRE
export PROOF_SKIP="GameState.Catalog,DefinitionReference.Catalog"
SWEEP="p31c-tw,pass16-demo2,p26-demo,p25-visual,p35-interdict-trap,p35-rally-enemy-turn,p35-smoke-hill,p35-tow2,p35-bog,p35-mud,p40-tpbf-mixed,p30-armor,p40c-ovr,p55-can-used,p55-duel-miss,p55-mot-pass,p55-al-fire"
case "$1" in
build)
  cp "$S/proof/Program.cs" "$S/proof-main/Program.cs"
  dotnet build "$S/proof-main/Proof.csproj" --disable-build-servers -m:1 -v:q > "$S/build-proof-main-55b.log" 2>&1
  grep -E "Build succeeded|error" "$S/build-proof-main-55b.log" | head -5
  cmp -s "$S/proof-main/bin/Debug/net10.0/LimboDancer.Domains.Asl.Play.dll" "$S/main-bin/LimboDancer.Domains.Asl.Play.dll" && echo "the copy is the baseline's build" || echo "STOP: the copy is not the baseline's build"
  ;;
small)
  dotnet "$S/proof-main/bin/Debug/net10.0/Proof.dll" sweep "$S/pass55/resweep-small.txt" "$S/store" "p55-can-used,p55-duel-miss,p55-mot-pass,p55-al-fire" > "$S/pass55/resweep-small.log" 2>&1
  echo "exit $?"; tail -3 "$S/pass55/resweep-small.log" | cut -c1-200
  cut -f1,2 "$S/pass55/resweep-small.txt" | sort | uniq -c | sort -rn | head -12
  ;;
full)
  date +%H:%M:%S
  dotnet "$S/proof-main/bin/Debug/net10.0/Proof.dll" sweep "$S/before-55/sweep.txt" "$S/store" "$SWEEP" > "$S/before-55/sweep.log" 2>&1
  echo "exit $?"; grep -m1 "lines ->" "$S/before-55/sweep.log" | head -c 200; echo
  date +%H:%M:%S
  ;;
esac
