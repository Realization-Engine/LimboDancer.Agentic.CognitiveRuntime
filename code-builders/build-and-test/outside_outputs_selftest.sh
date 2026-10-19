#!/bin/bash
# A trial of outside_outputs.sh on a throwaway working folder (never the real one): register two outputs, list, clean, and check that the
# register empties, the outputs are gone at once, an unregistered folder is untouched, and a path inside the repository is refused.
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEST="E:/Archive/GitHub/dlandi/asl-outside-selftest"
rm -rf "$TEST"; mkdir -p "$TEST/work/built/bin" "$TEST/work/old-unregistered"
echo dll > "$TEST/work/built/bin/a.dll"; echo tar > "$TEST/work/main.tar"; echo keep > "$TEST/work/old-unregistered/x.dll"
export ASL_GATE_WORK="$TEST/work"
ok=1
bash "$HERE/outside_outputs.sh" register "$TEST/work/built" || ok=0
bash "$HERE/outside_outputs.sh" register "$TEST/work/main.tar" || ok=0
bash "$HERE/outside_outputs.sh" register "$HERE" > /dev/null 2>&1 && { echo "FAIL: a path inside the repository was registered"; ok=0; }
[ "$(bash "$HERE/outside_outputs.sh" list | grep -c '^registered:')" = "2" ] || { echo "FAIL: list"; ok=0; }
bash "$HERE/outside_outputs.sh" clean || ok=0
[ -e "$TEST/work/built" ] && { echo "FAIL: built still there"; ok=0; }
[ -e "$TEST/work/main.tar" ] && { echo "FAIL: main.tar still there"; ok=0; }
[ -e "$TEST/work/old-unregistered/x.dll" ] || { echo "FAIL: an unregistered folder was touched"; ok=0; }
[ -s "$TEST/work/outside-outputs.txt" ] && { echo "FAIL: the register is not empty"; ok=0; }
echo "trash after clean: $(ls "$TEST/asl-trash" 2>/dev/null | wc -l) entries (deleted in the background)"
[ "$ok" = "1" ] && echo "SELFTEST PASSED" || echo "SELFTEST FAILED"
