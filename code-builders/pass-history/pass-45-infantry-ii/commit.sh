#!/bin/bash
# Commit the working tree's tracked changes and the new files named: commit.sh <message file> [new file paths, relative to the repository root]...
# Stages explicit paths only; never the boards folder.
cd /e/Archive/GitHub/dlandi/LimboDancer.MCP || exit 1
BRANCH=$(git branch --show-current)
if [ "$BRANCH" != "feature/asl-backlog-pass-45" ]; then echo "wrong branch: $BRANCH"; exit 1; fi
MESSAGE=$1
shift
LIST=/e/Archive/GitHub/dlandi/pass32-tools/pass45/paths.txt
git -c core.quotepath=false diff --name-only > "$LIST"
for NEW in "$@"; do echo "$NEW" >> "$LIST"; done
if grep -q -E "boards/|\.claude/|\.png$|\.jpg$" "$LIST"; then echo "a path that is never committed is in the list"; cat "$LIST"; exit 1; fi
git add --pathspec-from-file="$LIST" || exit 1
git diff --cached --ignore-cr-at-eol --stat | tail -1
git commit -q -F "$MESSAGE" || exit 1
git log -1 --oneline
git status --short | grep -v "src/ASL/boards/" | head
