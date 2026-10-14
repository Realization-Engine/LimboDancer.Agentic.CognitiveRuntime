#!/bin/bash
# An increment's close: step.sh <tag> <design section file> <message file>
# Appends the section to the design, commits the tracked changes and every new file under src/ASL (never boards/), recomputes the skip list
# from the diff since the base, and trials the replay digest on the six games of the trial store.
R=/e/Archive/GitHub/dlandi/LimboDancer.MCP
P=/e/Archive/GitHub/dlandi/pass32-tools/pass55
TAG=$1
mapfile -t NEW < <(git -C "$R" -c core.quotepath=false status --short | grep '^??' | cut -c4- | sed 's/^"//; s/"$//' | grep '^src/ASL/' | grep -v 'boards/')
printf 'new file: %s\n' "${NEW[@]}"
PYTHONIOENCODING=utf-8 python "$P/append.py" "$2" < /dev/null || exit 1
bash "$P/commit.sh" "$3" "${NEW[@]}" || exit 1
PYTHONIOENCODING=utf-8 python /e/Archive/GitHub/dlandi/pass32-tools/gate/new_properties.py 846eb5a3 < /dev/null > "$P/skip55.txt"
echo "skip list: $(tr ',' '\n' < "$P/skip55.txt" | grep -c .) names"
PROOF_SKIP_55="$(tr -d '\r\n' < "$P/skip55.txt")" bash "$P/replay_now.sh" "$TAG-trial" "$P/trial-store" -trial 2>&1 | tail -3
date +%H:%M
