#!/bin/bash
# The build outputs a pass makes outside the repository: registered when they are made, removed at the earliest opportunity (the user,
# 2026-10-19: "These outside bins must be cleaned-up at the earliest opportunity", a standing rule enforced and part of every plan; it
# covers bins made from that day on: what was there before is the user's to clear by hand, and this script never touches it).
#
#   bash code-builders/build-and-test/outside_outputs.sh register <path>   record a build output or a worktree as it is made
#   bash code-builders/build-and-test/outside_outputs.sh list              the registered outputs that still exist
#   bash code-builders/build-and-test/outside_outputs.sh clean             remove them, and empty the register
#
# What registers: gate.sh, for the baseline's copy of main (main-src, main.tar, main-bin) and the proof tools it builds (proof-main,
# proof-gate); and a session, for anything else it builds outside the repository: Claude's Studio (studio-6670), a "before" Studio's
# worktree, a harness's output. The register is $WORK/outside-outputs.txt, one absolute path a line. The proof store, the baseline cache,
# logs, and results are never registered: they last from pass to pass. The gate's ci stage runs `clean` once CI has passed.
#
# A delete removes one file at a time, and a large tree takes half an hour (2026-10-19). So `clean` first moves each output into one
# trash folder on the same drive, which is instant whatever its size, and then deletes the trash in the background, with a detached,
# multi-threaded robocopy that handles long paths; it does not wait for it. An interrupted delete is finished by the next clean.
source "$(dirname "${BASH_SOURCE[0]}")/../paths.sh"
MODE="${1:-list}"
REGISTER="$WORK/outside-outputs.txt"
PARENT="$(dirname "$WORK")"

case "$MODE" in
register)
  [ -n "$2" ] || { echo "STOP: register takes the path of the output"; exit 2; }
  path=$(cygpath -m "$(realpath -m "$2")")
  case "$path" in "$(cygpath -m "$R")"*) echo "STOP: $path is inside the repository; build it outside (the standing rules)"; exit 2 ;; esac
  mkdir -p "$WORK"; touch "$REGISTER"
  grep -qxF "$path" "$REGISTER" || echo "$path" >> "$REGISTER"
  exit 0
  ;;
list|clean) ;;
*) echo "STOP: the mode is register, list, or clean"; exit 2 ;;
esac

[ -f "$REGISTER" ] || { echo "nothing registered"; exit 0; }
present=()
while read -r path; do [ -n "$path" ] && [ -e "$path" ] && present+=("$path"); done < "$REGISTER"
if [ ${#present[@]} -eq 0 ]; then echo "nothing registered is left"; [ "$MODE" = "clean" ] && : > "$REGISTER"; exit 0; fi
for p in "${present[@]}"; do echo "registered: $p"; done
[ "$MODE" = "list" ] && exit 0

# A running program holds its DLLs: stop it first rather than move half a folder.
running=$(powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='dotnet.exe'\" | ForEach-Object { \$_.CommandLine }" 2>/dev/null \
  | grep -iF -f <(printf '%s\n' "${present[@]}") | head -3)
if [ -n "$running" ]; then echo "STOP: a program runs from a registered output; stop it first:"; echo "$running"; exit 1; fi

TRASH="$PARENT/asl-trash"
mkdir -p "$TRASH"
stamp=$(date +%Y%m%d-%H%M%S)
failed=0
worktree=0
worktrees=$(git -C "$R" worktree list --porcelain)
for p in "${present[@]}"; do
  grep -qixF "worktree $p" <<< "$worktrees" && worktree=1
  destination="$TRASH/$stamp-$(basename "$p")"
  if [ -f "$p" ]; then mv "$p" "$destination" && echo "moved out: $p" || { echo "NOT MOVED: $p"; failed=1; }; continue; fi
  if powershell -NoProfile -Command "[System.IO.Directory]::Move('$(cygpath -w "$p")', '$(cygpath -w "$destination")')" 2>/dev/null; then
    echo "moved out: $p"
  else
    echo "NOT MOVED (something holds it; stop it and run clean again): $p"; failed=1
  fi
done
[ "$worktree" = "1" ] && git -C "$R" worktree prune && echo "git's record of the moved worktrees pruned"
# Only what was moved leaves the register; what could not be moved stays for the next clean.
remaining=()
for p in "${present[@]}"; do [ -e "$p" ] && remaining+=("$p"); done
printf '%s\n' "${remaining[@]}" | grep . > "$REGISTER.new"; mv "$REGISTER.new" "$REGISTER"

TRASH_W=$(cygpath -w "$TRASH")
powershell -NoProfile -Command "Start-Process -WindowStyle Hidden -FilePath cmd.exe -ArgumentList '/c', 'mkdir \"%TEMP%\asl-empty\" 2>nul & robocopy \"%TEMP%\asl-empty\" \"$TRASH_W\" /MIR /MT:32 /NFL /NDL /NJH /NJS /NP /R:0 /W:0 & rd /s /q \"\\\\?\\$TRASH_W\"'" \
  && echo "the trash ($TRASH) is being deleted in the background"
exit $failed
