#!/bin/bash
# The merge gate of a rule pass, one script for every pass (written after pass 50; repaired after pass 55's gate, 2026-10-13).
# The script and its helpers are in the repository; what they write goes to the working folder that paths.sh names.
#
#   bash code-builders/merge-gate/gate.sh <pass> <base commit> <stage>
#
# Before the gate, in the building session:
#   base      the baseline of the games made before the pass: the base's own sources built, text list, replay digest, sweep. Kept under
#             baseline-cache/<key> and reused while the base commit, the older games, BOTH_SKIP, the sweep list, and the tool are the same.
#             Run it once when the branch is cut (about 25 min alone); the gate then finds it made.
#   strict    the pass's changed test files formatted, then the solution built with warnings as errors. Run it before the documents commit:
#             the pass's test script builds with warnings not as errors, and nothing else compiles a test file strictly before the gate.
#   strict-check   says whether the strict build passed for the tree as it stands. A review or a prompt says so only when this does.
#
# The gate's stages, in the order they are started (pass 55 ran them side by side and each took two to three times as long):
#   baseline  the pass's own games copied into the store and replayed and swept on the baseline's side; the rest reused from base
#   build     the solution with warnings as errors into bin/gate; the proof tool compiled against it          (about 6 min)
#   trial     the trial games, the pass's own games, and its extra sweep games on both sides; must pass       (about 3 min)
#   proofs    text list, replay digest, sweep on the gate's side, each compared with the baseline            (about 25 min; refuses without a
#             passed trial of this very build and these very skip lists)
#   docker    the Docker Linux check on the committed branch, started beside proofs                           (about 25 min)
#   suite     every test project from bin/gate, CI's Node viewport test, the chart supplement                 (about 35 min; after proofs)
#
# What a pass supplies is gate/pass<pass>.conf (see pass55.conf): its recorded games, extra sweep and trial games, the JSON keys it added,
# BOTH_SKIP (what changed in every record for a reason that is no rule: the catalog's identity, GameEvent.RulePackage after the packages
# are pinned again), and CODE_MOVE=1 for a pass that moves code without changing rules, which sweeps every cut of p31c-tw.
# PROOF_SKIP is computed from the diff base..HEAD (new_properties.py); it is never carried over from another pass.
source "$(dirname "${BASH_SOURCE[0]}")/../paths.sh"
S="$WORK"
G="$TOOLS/merge-gate"
T="5a7d1f000000400080000000000057d0"
PASS="$1"; BASE="$2"; STAGE="$3"
SWEEP_SMALL="pass16-demo2,p26-demo,p25-visual,p35-interdict-trap,p35-rally-enemy-turn,p35-smoke-hill,p35-tow2,p35-bog,p35-mud,p40-tpbf-mixed,p30-armor,p40c-ovr"
TRIAL_BASE="pass16-demo2,p26-demo,p25-visual,p31c-tw-r459,p31c-tw-r471"
GATE="$R/src/ASL/LimboDancer.Domains.Asl.MapStudio/bin/gate/Debug/net10.0"
MAINTOOL="$S/proof-main/bin/Debug/net10.0"
if [ -z "$PASS" ] || [ -z "$BASE" ] || [ -z "$STAGE" ]; then sed -n 2,26p "$0"; exit 2; fi
if [ ! -f "$G/pass-configs/pass$PASS.conf" ]; then echo "STOP: write $G/pass-configs/pass$PASS.conf first (pass55.conf is the form)"; exit 2; fi
OWN=""; SWEEP_EXTRA=""; TRIAL_EXTRA=""; SKIP_JSON=""; SKIP_ADD=""; BOTH_SKIP=""; PRESENT=""; ABSENT=""; CODE_MOVE=""
source "$G/pass-configs/pass$PASS.conf"
# The user (2026-10-13): a rule pass sweeps p31c-tw at each of its eight phases at four points of the game; a pass that moves code, at every cut.
if [ "$CODE_MOVE" = "1" ]; then TW="p31c-tw"; else TW=$(python "$G/pick_sweep_cuts.py" p31c-tw 4 < /dev/null); fi
SWEEP_OLD="$TW,$SWEEP_SMALL"
exact() { echo "$1" | tr ' ' ',' | tr -s ',' | sed -e 's/^,//' -e 's/,$//' -e 's/[^,][^,]*/=&/g'; }
OWN_COMMA=$(echo "$OWN" | tr ' ' ',' | tr -s ',' | sed -e 's/^,//' -e 's/,$//')
OWN_EXACT=$(exact "$OWN"); EXTRA_EXACT=$(exact "$SWEEP_EXTRA")
SWEEP="$SWEEP_OLD${EXTRA_EXACT:+,$EXTRA_EXACT}"
TRIAL="$TRIAL_BASE${TRIAL_EXTRA:+,$TRIAL_EXTRA}"
BEFORE="$S/before-$PASS"; AFTER="$S/after-$PASS"; TRIALDIR="$S/trial-$PASS"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
unset PROOF_SKIP PROOF_SKIP_JSON PROOF_DUMP_FIRE PROOF_DUMP_EVENTS PROOF_DUMP_STATE PROOF_EXCLUDE PROOF_REPLAY_CUTS
BASEFULL=$(git -C "$R" rev-parse --verify -q "$BASE^{commit}") || { echo "STOP: no commit $BASE"; exit 2; }
now() { date +%H:%M:%S; }
stop() { echo "STOP: $*"; exit 1; }

header() {
  echo "[$(now)] pass $PASS, stage $STAGE: head $(git -C "$R" rev-parse --short HEAD) on $(git -C "$R" branch --show-current), baseline ${BASEFULL:0:8}, SDK $(dotnet --version)"
  local main fork; main=$(git -C "$R" rev-parse -q --verify origin/main); fork=$(git -C "$R" merge-base HEAD "$main")
  if [ "$fork" = "$(git -C "$R" rev-parse HEAD)" ] && [ "$main" != "$BASEFULL" ]; then
    echo "NOTE: this branch is already merged into origin/main (${main:0:8})."
  elif [ "$main" != "$BASEFULL" ]; then
    echo "NOTE: origin/main is ${main:0:8}, not the baseline (as of the last fetch). Files changed on both sides since the branch was cut:"
    git -C "$R" -c core.quotepath=false diff --name-only "$fork..$main" | sort > "$S/.main-files"
    git -C "$R" -c core.quotepath=false diff --name-only "$fork..HEAD" | sort > "$S/.head-files"
    comm -12 "$S/.main-files" "$S/.head-files" | sed 's/^/  /'
    echo "  ($(grep -c '^src/ASL/' "$S/.main-files") of main's $(wc -l < "$S/.main-files") changed files are under src/ASL)"
  fi
}

# The gate's proof tool, rebuilt against bin/gate; stops unless its copies are the gate's build byte for byte.
tool() {
  mkdir -p "$S/proof"; cp "$TOOLS/proof-tool/Program.cs" "$TOOLS/proof-tool/Proof.csproj" "$S/proof/"
  dotnet build "$S/proof/Proof.csproj" "-p:StudioBin=$GATE/" "-p:OutDir=$S/proof-gate/" --disable-build-servers -m:1 -v:q > "$S/build-proof-gate-$PASS.log" 2>&1
  if ! grep -q "Build succeeded" "$S/build-proof-gate-$PASS.log"; then echo "STOP: the proof tool did not build against bin/gate"; grep -m3 "error" "$S/build-proof-gate-$PASS.log"; exit 1; fi
  for d in Play Rules Units Maps MapStudio; do
    if ! cmp -s "$S/proof-gate/LimboDancer.Domains.Asl.$d.dll" "$GATE/LimboDancer.Domains.Asl.$d.dll"; then stop "the tool's copy of $d is not the gate's build"; fi
  done
}

# The baseline's build and its proof tool: the build is kept while the base commit is the same; the tool is always compiled again, since
# it must compile against both sides.
main_tool() {
  if [ "$(cat "$S/main-bin/BASE" 2>/dev/null)" != "$BASEFULL" ]; then
    rm -rf "$S/main-src" "$S/main-bin" "$S/main.tar"; mkdir -p "$S/main-src"
    git -C "$R" archive --format=tar -o "$S/main.tar" "$BASEFULL" && tar -xf "$S/main.tar" -C "$S/main-src" || stop "the archive failed"
    if [ -n "$PRESENT" ] && ! grep -rqs "$PRESENT" "$S/main-src/src/ASL" --include=*.cs; then stop "the baseline's sources lack $PRESENT"; fi
    if [ -n "$ABSENT" ] && grep -rqs "$ABSENT" "$S/main-src/src/ASL" --include=*.cs; then stop "the baseline's sources hold $ABSENT"; fi
    echo "[$(now)] build of the baseline"
    dotnet build "$S/main-src/src/ASL/LimboDancer.Domains.Asl.MapStudio/LimboDancer.Domains.Asl.MapStudio.csproj" -c Debug -o "$S/main-bin" --disable-build-servers -m:1 -v:q > "$S/build-main-$PASS.log" 2>&1
    grep -q "Build succeeded" "$S/build-main-$PASS.log" || stop "the baseline did not build"
    echo "$BASEFULL" > "$S/main-bin/BASE"
  else
    echo "[$(now)] the baseline's build of ${BASEFULL:0:8} is kept"
  fi
  mkdir -p "$S/proof-main"; cp "$TOOLS/proof-tool/Program.cs" "$S/proof-main/Program.cs"; cp "$TOOLS/proof-tool/Proof.Baseline.csproj" "$S/proof-main/Proof.csproj"
  dotnet build "$S/proof-main/Proof.csproj" "-p:StudioBin=$S/main-bin/" --disable-build-servers -m:1 -v:q > "$S/build-proof-main-$PASS.log" 2>&1
  if ! grep -q "Build succeeded" "$S/build-proof-main-$PASS.log"; then echo "STOP: the proof tool did not build against the baseline (it must compile against both sides)"; grep -m3 "error" "$S/build-proof-main-$PASS.log"; exit 1; fi
  cmp -s "$MAINTOOL/LimboDancer.Domains.Asl.Play.dll" "$S/main-bin/LimboDancer.Domains.Asl.Play.dll" || stop "the tool's copy is not the baseline's build"
}

# The skip lists of the gate's side, and the signature a passed trial is bound to: the build and both lists.
skips() {
  GATE_SKIP=$(python "$G/new_properties.py" "$BASEFULL" < /dev/null)${SKIP_ADD:+,$SKIP_ADD}${BOTH_SKIP:+,$BOTH_SKIP}
  GATE_SKIP_JSON="$SKIP_JSON"
  SIGNATURE="$(cat "$GATE/LimboDancer.Domains.Asl.Play.dll" "$GATE/LimboDancer.Domains.Asl.Rules.dll" "$GATE/LimboDancer.Domains.Asl.Units.dll" | sha256sum | cut -c1-16) $(echo "$GATE_SKIP|$GATE_SKIP_JSON|$BASEFULL|$SWEEP" | sha256sum | cut -c1-16)"
  echo "BOTH_SKIP (left out on the baseline's side too): $BOTH_SKIP"
  echo "PROOF_SKIP ($(echo "$GATE_SKIP" | tr ',' '\n' | grep -c .) names): $GATE_SKIP"
  echo "PROOF_SKIP_JSON: $GATE_SKIP_JSON"
}

# A run of the tool on one side. It stops the stage unless the tool exited 0 and wrote its file, and no game ended in an ERROR line:
# pass 55's baseline was marked done after its sweep had ended in a null reference.
main_side() { ( unset PROOF_SKIP PROOF_SKIP_JSON; export PROOF_SKIP="$BOTH_SKIP" PROOF_EXCLUDE="$EXCLUDE"; dotnet "$MAINTOOL/Proof.dll" "$@" ); }
gate_side() { ( export PROOF_SKIP="$GATE_SKIP" PROOF_SKIP_JSON="$GATE_SKIP_JSON"; dotnet "$S/proof-gate/Proof.dll" "$@" ); }
run() {
  local side="$1" mode="$2" out="$3"; shift 3
  rm -f "$out"
  "$side" "$mode" "$out" "$S/store" "$@" > "${out%.txt}.log" 2>&1 || stop "the $mode of $side exited $? (${out%.txt}.log)"
  [ -s "$out" ] || stop "the $mode of $side wrote nothing to $out"
  if awk -F'\t' '$2=="ERROR"' "$out" | grep -q .; then awk -F'\t' '$2=="ERROR"' "$out" | head -5 | cut -c1-300; stop "a game ended in an ERROR line in $out"; fi
  echo "[$(now)] $(grep -m1 "lines ->" "${out%.txt}.log" | head -c 160)"
}

# The baseline of the games made before the pass, made once and kept.
base() {
  main_tool
  KEY=$(python "$G/baseline_key.py" "$BASEFULL" "$BOTH_SKIP" "$SWEEP_OLD" "$OWN" < /dev/null) || stop "no key for the baseline"
  CACHE="$S/baseline-cache/$KEY"
  if [ -f "$CACHE/DONE" ]; then echo "[$(now)] the baseline $KEY of the older games is reused (made $(cat "$CACHE/DONE"))"; return; fi
  echo "[$(now)] the baseline $KEY of the older games is made: $(ls "$S/store/$T/"*.game.json | wc -l) games in the store, the pass's own left out"
  rm -rf "$CACHE"; mkdir -p "$CACHE"
  python "$G/text_list.py" "$CACHE/textlist.txt" "$S/main-src/src/ASL" < /dev/null
  EXCLUDE="$OWN_COMMA"
  run main_side replay "$CACHE/replay.txt"
  run main_side sweep "$CACHE/sweep.txt" "$SWEEP_OLD"
  EXCLUDE=""
  date "+%F %H:%M" > "$CACHE/DONE"
}

# What says that the strict build passed for the tree as it stands: every tracked or new file under src/ASL outside docs and boards.
tree_key() { (cd "$R" && git ls-files -z -c -o --exclude-standard -- src/ASL ':(exclude)src/ASL/docs' ':(exclude)src/ASL/boards' | xargs -0 sha256sum 2>/dev/null | sha256sum | cut -c1-16); }
strict_build() {
  dotnet build "$R/src/ASL/LimboDancer.Domains.Asl.sln" -c Debug "-p:BaseOutputPath=bin/gate/" --warnaserror --disable-build-servers -m:1 -v:q > "$S/gate-build-$PASS.log" 2>&1
  tail -4 "$S/gate-build-$PASS.log"
  if ! grep -q "Build succeeded" "$S/gate-build-$PASS.log"; then rm -f "$S/strict-ok"; echo "STOP: the build failed"; grep -E "error|warning" "$S/gate-build-$PASS.log" | sed 's/\[.*//' | sort -u | head -40; exit 1; fi
  echo "$(tree_key) $(git -C "$R" rev-parse --short HEAD) $(date "+%F %H:%M") $S/gate-build-$PASS.log" > "$S/strict-ok"
}

EXCLUDE=""
header
case "$STAGE" in
base)
  base
  ;;
strict)
  CHANGED=$( (git -C "$R" -c core.quotepath=false diff --name-only "$BASEFULL" -- 'src/ASL/tests/*.cs'; git -C "$R" ls-files --others --exclude-standard -- 'src/ASL/tests/*.cs') | sort -u)
  for project in $(echo "$CHANGED" | cut -d/ -f4 | sort -u); do
    FILES=$(echo "$CHANGED" | grep "/$project/" | sed 's#^src/ASL/##' | while read -r f; do [ -f "$R/src/ASL/$f" ] && echo "$f"; done)
    [ -n "$FILES" ] || continue
    echo "[$(now)] whitespace: $project, $(echo "$FILES" | wc -l) files"
    (cd "$R/src/ASL" && dotnet format whitespace "tests/$project/$project.csproj" --include $FILES -v q 2>&1 | tail -2)
  done
  git -C "$R" status --short -- src/ASL/tests | head -20
  strict_build
  echo "the strict build passed for this tree; a file the formatter changed is still to be committed"
  ;;
strict-check)
  if [ -f "$S/strict-ok" ] && [ "$(cut -d' ' -f1 "$S/strict-ok")" = "$(tree_key)" ]; then echo "OK: the strict build passed for this tree: $(cat "$S/strict-ok")"; else stop "no strict build of this tree has passed (run the strict stage)"; fi
  ;;
baseline)
  echo "[$(now)] store before: $(ls "$S/store/$T/"*.game.json | wc -l) games"
  for g in $OWN; do cp "$R/src/ASL/boards/units/live/$T/$g.game.json" "$S/store/$T/" || stop "no live game $g"; done
  echo "[$(now)] store after: $(ls "$S/store/$T/"*.game.json | wc -l) games"
  base
  rm -rf "$BEFORE"; mkdir -p "$BEFORE"
  cp "$CACHE/textlist.txt" "$CACHE/replay.txt" "$CACHE/sweep.txt" "$BEFORE/"
  if [ -n "$OWN_EXACT" ]; then run main_side replay "$BEFORE/replay-own.txt" "$OWN_EXACT"; cat "$BEFORE/replay-own.txt" >> "$BEFORE/replay.txt"; fi
  if [ -n "$EXTRA_EXACT" ]; then run main_side sweep "$BEFORE/sweep-own.txt" "$EXTRA_EXACT"; cat "$BEFORE/sweep-own.txt" >> "$BEFORE/sweep.txt"; fi
  echo "the baseline: $(wc -l < "$BEFORE/replay.txt") replay lines, $(wc -l < "$BEFORE/sweep.txt") sweep lines; the pass's games it cannot read or bring to a state:"
  awk -F'\t' '$2=="UNREADABLE" || $2=="NOSTATE"' "$BEFORE/replay.txt" "$BEFORE/sweep.txt" | sort -u | tr '\n' ' '; echo
  echo "$BASEFULL" > "$BEFORE/TOOL"
  echo "$BASEFULL" > "$BEFORE/DONE"
  ;;
build)
  strict_build
  tool
  echo "the proof tool compiles against this build"
  ;;
trial)
  [ "$(cat "$BEFORE/TOOL" 2>/dev/null)" = "$BASEFULL" ] || stop "no baseline of ${BASEFULL:0:8} in $BEFORE; run the baseline stage first"
  tool; skips
  rm -rf "$TRIALDIR"; mkdir -p "$TRIALDIR"
  TRIAL_REPLAY="$TRIAL${OWN_EXACT:+,$OWN_EXACT}"; TRIAL_SWEEP="$TRIAL${EXTRA_EXACT:+,$EXTRA_EXACT}"
  echo "[$(now)] trial games: $TRIAL; and the pass's own: $OWN_COMMA; swept beside them: $SWEEP_EXTRA"
  run main_side replay "$TRIALDIR/replay-main.txt" "$TRIAL_REPLAY"
  run gate_side replay "$TRIALDIR/replay-gate.txt" "$TRIAL_REPLAY"
  run main_side sweep "$TRIALDIR/sweep-main.txt" "$TRIAL_SWEEP"
  run gate_side sweep "$TRIALDIR/sweep-gate.txt" "$TRIAL_SWEEP"
  if python "$G/trial_check.py" "$TRIALDIR" "$OWN_COMMA" < /dev/null; then echo "$SIGNATURE" > "$TRIALDIR/OK"; else exit 1; fi
  ;;
suite)
  rm -rf "$S/suite"
  bash "$G/run_test_suite.sh"
  node --test "$R/src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/boardViewport.pointer.test.mjs" > "$S/node-$PASS.log" 2>&1
  echo "node exit $?: $(grep -E "^. (tests|pass|fail) " "$S/node-$PASS.log" | tr '\n' ' ')"
  bash "$G/chart_supplement.sh" > "$S/chart-$PASS.log" 2>&1
  tail -2 "$S/chart-$PASS.log"
  echo "A project that failed: python code-builders/diagnostics/failed_tests_of_trx.py $S/suite/<project>.trx with PYTHONIOENCODING=utf-8"
  ;;
proofs)
  tool; skips
  if [ ! -f "$TRIALDIR/OK" ] || [ "$(cat "$TRIALDIR/OK")" != "$SIGNATURE" ]; then stop "no passed trial of this build and these skip lists. Run the trial stage first."; fi
  [ "$(cat "$BEFORE/DONE" 2>/dev/null)" = "$BASEFULL" ] || stop "no finished baseline of ${BASEFULL:0:8} in $BEFORE"
  [ -s "$BEFORE/replay.txt" ] && [ -s "$BEFORE/sweep.txt" ] && [ -s "$BEFORE/textlist.txt" ] || stop "the baseline in $BEFORE lacks a file"
  rm -rf "$AFTER"; mkdir -p "$AFTER"
  python "$G/text_list.py" "$AFTER/textlist.txt" < /dev/null
  echo "text list lines that differ: $(diff "$BEFORE/textlist.txt" "$AFTER/textlist.txt" | grep -c '^[<>]')"
  run gate_side replay "$AFTER/replay.txt"
  python "$G/compare_replay_digests.py" "$BEFORE/replay.txt" "$AFTER/replay.txt" < /dev/null > "$AFTER/replay-games.txt"; [ $? -ne 2 ] || { cat "$AFTER/replay-games.txt"; stop "a side of the replay holds an ERROR line or nothing"; }
  echo "replay lines: $(wc -l < "$BEFORE/replay.txt") before, $(wc -l < "$AFTER/replay.txt") now; games with a differing line (in $AFTER/replay-games.txt):"
  cat "$AFTER/replay-games.txt"
  run gate_side sweep "$AFTER/sweep.txt" "$SWEEP"
  python "$G/compare_sweeps.py" "$BEFORE/sweep.txt" "$AFTER/sweep.txt" < /dev/null > "$AFTER/sweep-pairs.txt" || stop "the sweep's comparison failed"
  echo "sweep groups that differ (all in $AFTER/sweep-pairs.txt); those not new on the gate's side:"
  grep -v "NEW AFTER" "$AFTER/sweep-pairs.txt"
  if [ ! -s "$AFTER/replay-games.txt" ] && [ ! -s "$AFTER/sweep-pairs.txt" ]; then echo "VOID: the digest and the sweep differ in nothing after a rule pass"; exit 1; fi
  echo "$SIGNATURE" > "$AFTER/DONE"
  ;;
docker)
  if [ -n "$(git -C "$R" status --porcelain --untracked-files=no)" ]; then stop "uncommitted changes; the container clones the committed branch"; fi
  IMAGE_SDK=$(MSYS_NO_PATHCONV=1 docker run --rm mcr.microsoft.com/dotnet/sdk:10.0 dotnet --version 2>/dev/null)
  echo "the image's SDK is $IMAGE_SDK, this machine's $(dotnet --version); a locked restore fails when the lock files were made under another patch (pull the image, or see the lock file's history)"
  (cd "$R" && bash src/ASL/tools/docker-check/docker_check.sh "$(git -C "$R" branch --show-current)") > "$S/docker-$PASS.log" 2>&1
  grep -E "^(Passed!|Failed!)|^== " "$S/docker-$PASS.log" | cut -c1-140
  ;;
*)
  echo "STOP: no stage '$STAGE'"; sed -n 2,26p "$0"; exit 2
  ;;
esac
echo "[$(now)] DONE $STAGE"
