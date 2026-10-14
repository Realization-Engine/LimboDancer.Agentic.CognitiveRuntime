#!/bin/bash
# Commit the tracked changes outside src/ASL/docs, and any new file under src/ASL outside docs and boards: ASL_PASS_BRANCH=<branch> commit_code.sh <message file>
source "$(dirname "${BASH_SOURCE[0]}")/../paths.sh"
BRANCH=$(git -C "$R" branch --show-current)
if [ -z "$ASL_PASS_BRANCH" ] || [ "$BRANCH" != "$ASL_PASS_BRANCH" ]; then echo "the branch is $BRANCH, and ASL_PASS_BRANCH says '$ASL_PASS_BRANCH'"; exit 1; fi
LIST=$WORK/commit-paths.txt
git -C "$R" -c core.quotepath=false diff --name-only | grep -v '^src/ASL/docs/' > "$LIST"
git -C "$R" -c core.quotepath=false status --short | grep '^??' | cut -c4- | sed 's/^"//; s/"$//' | grep '^src/ASL/' | grep -v -E 'boards/|^src/ASL/docs/' >> "$LIST"
if grep -q -E "boards/|\.claude/|\.codex/|\.png$|\.jpg$" "$LIST"; then echo "a path that is never committed is in the list"; cat "$LIST"; exit 1; fi
wc -l < "$LIST"
git -C "$R" add --pathspec-from-file="$LIST" || exit 1
git -C "$R" diff --cached --ignore-cr-at-eol --stat | tail -1
git -C "$R" commit -q -F "$1" || exit 1
git -C "$R" log -1 --oneline
git -C "$R" status --short | grep -v "src/ASL/boards/" | head -12
