#!/bin/bash
# Commit the working tree's tracked changes and the new files named: ASL_PASS_BRANCH=<branch> commit_tracked.sh <message file> [new file paths, relative to the repository root]...
# Stages explicit paths only; never the boards folder.
source "$(dirname "${BASH_SOURCE[0]}")/../paths.sh"
BRANCH=$(git -C "$R" branch --show-current)
if [ -z "$ASL_PASS_BRANCH" ] || [ "$BRANCH" != "$ASL_PASS_BRANCH" ]; then echo "the branch is $BRANCH, and ASL_PASS_BRANCH says '$ASL_PASS_BRANCH'"; exit 1; fi
MESSAGE=$1
shift
LIST=$WORK/commit-paths.txt
git -C "$R" -c core.quotepath=false diff --name-only > "$LIST"
for NEW in "$@"; do echo "$NEW" >> "$LIST"; done
if grep -q -E "boards/|\.claude/|\.png$|\.jpg$" "$LIST"; then echo "a path that is never committed is in the list"; cat "$LIST"; exit 1; fi
git -C "$R" add --pathspec-from-file="$LIST" || exit 1
git -C "$R" diff --cached --ignore-cr-at-eol --stat | tail -1
git -C "$R" commit -q -F "$MESSAGE" || exit 1
git -C "$R" log -1 --oneline
git -C "$R" status --short | grep -v "src/ASL/boards/" | head
