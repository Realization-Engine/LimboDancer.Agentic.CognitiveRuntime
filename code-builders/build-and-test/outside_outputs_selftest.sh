#!/bin/bash
# A trial of outside_outputs.sh on a throwaway working folder (never the real one): register a worktree and two outputs, list, clean, and
# check that the register empties, the outputs are gone at once, git no longer records the worktree, an unregistered folder is untouched,
# a path inside the repository is refused, and a command line names an output whatever its case and slashes.
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/../.." && pwd)"
TEST="E:/Archive/GitHub/dlandi/asl-outside-selftest"
rm -rf "$TEST"; git -C "$REPO" worktree prune; mkdir -p "$TEST/work/built/bin" "$TEST/work/old-unregistered"
echo dll > "$TEST/work/built/bin/a.dll"; echo tar > "$TEST/work/main.tar"; echo keep > "$TEST/work/old-unregistered/x.dll"
export ASL_GATE_WORK="$TEST/work"
ok=1
# The matching the two checks use (2026-10-19: grep -i with -F aborts on this machine, so neither check ever matched): case and slashes aside.
bash "$HERE/outside_outputs.sh" _mentions '"C:\Program Files\dotnet\dotnet.exe" E:\Archive\GitHub\Dlandi\asl-gate-work\Studio-A3\x.dll' \
  "E:/Archive/GitHub/dlandi/asl-gate-work/studio-a3" || { echo "FAIL: a command line naming the output in other case and slashes"; ok=0; }
bash "$HERE/outside_outputs.sh" _mentions "dotnet E:/Archive/GitHub/dlandi/asl-gate-work/other/x.dll" \
  "E:/Archive/GitHub/dlandi/asl-gate-work/studio-a3" && { echo "FAIL: a command line naming another folder"; ok=0; }
# A worktree registered as an output is moved, and git's record of it pruned.
git -C "$REPO" worktree add --detach "$TEST/work/wt" HEAD > /dev/null 2>&1 || { echo "FAIL: no trial worktree"; ok=0; }
bash "$HERE/outside_outputs.sh" register "$TEST/work/wt" || ok=0
bash "$HERE/outside_outputs.sh" register "$TEST/work/built" || ok=0
bash "$HERE/outside_outputs.sh" register "$TEST/work/main.tar" || ok=0
bash "$HERE/outside_outputs.sh" register "$HERE" > /dev/null 2>&1 && { echo "FAIL: a path inside the repository was registered"; ok=0; }
[ "$(bash "$HERE/outside_outputs.sh" list | grep -c '^registered:')" = "3" ] || { echo "FAIL: list"; ok=0; }
bash "$HERE/outside_outputs.sh" clean || ok=0
[ -e "$TEST/work/wt" ] && { echo "FAIL: the worktree still there"; ok=0; }
git -C "$REPO" worktree list | grep -q "asl-outside-selftest" && { echo "FAIL: git still records the moved worktree"; ok=0; }
[ -e "$TEST/work/built" ] && { echo "FAIL: built still there"; ok=0; }
[ -e "$TEST/work/main.tar" ] && { echo "FAIL: main.tar still there"; ok=0; }
[ -e "$TEST/work/old-unregistered/x.dll" ] || { echo "FAIL: an unregistered folder was touched"; ok=0; }
[ -s "$TEST/work/outside-outputs.txt" ] && { echo "FAIL: the register is not empty"; ok=0; }
echo "trash after clean: $(ls "$TEST/asl-trash" 2>/dev/null | wc -l) entries (deleted in the background)"
[ "$ok" = "1" ] && echo "SELFTEST PASSED" || echo "SELFTEST FAILED"
