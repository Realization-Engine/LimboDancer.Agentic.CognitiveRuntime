#!/bin/bash
# How the branch's commits reach main (the user, 2026-10-13): by a pull request and a green CI run, or straight.
#
#   how_to_reach_main.sh [branch, default the one checked out]
#
# A pull request and a green CI run, before the merge, for:
#   code, project files, lock files, the workflow, and anything under code-builders;
#   any commit that adds, removes, or renames a file, a document included: some tests read the whole repository, and the merge that
#   brought code-builders in broke one of them (an architecture test that lists every .py file) while changing no file of the solution.
# Straight to main, with the CI run read afterwards, only for edits to the text of documents that already exist (.md, outside code-builders).
#
# It also says whether main has moved since the branch was cut, and which files changed on both sides. Exit 0 straight, 3 pull request.
source "$(dirname "${BASH_SOURCE[0]}")/../paths.sh"
BRANCH="${1:-$(git -C "$R" branch --show-current)}"
git -C "$R" fetch -q origin || { echo "STOP: the fetch failed"; exit 1; }
MAIN=$(git -C "$R" rev-parse origin/main); FORK=$(git -C "$R" merge-base "$BRANCH" origin/main)
AHEAD=$(git -C "$R" rev-list --count "origin/main..$BRANCH")
echo "$BRANCH: $AHEAD commits that origin/main (${MAIN:0:8}) lacks; cut at ${FORK:0:8}"
if [ "$AHEAD" = "0" ]; then echo "nothing to bring to main"; exit 0; fi
if [ "$FORK" != "$MAIN" ]; then
  echo "main has moved by $(git -C "$R" rev-list --count "$FORK..origin/main") commits since. Files changed on both sides (the user hears of these first):"
  comm -12 <(git -C "$R" -c core.quotepath=false diff --name-only "$FORK..origin/main" | sort) <(git -C "$R" -c core.quotepath=false diff --name-only "$FORK..$BRANCH" | sort) | sed 's/^/  /'
fi
REASONS=$(git -C "$R" -c core.quotepath=false diff --name-status "$FORK..$BRANCH" | awk -F'\t' '
  $1 != "M" { print "  " $1 " " $NF "   (a file added, removed, or renamed)"; next }
  $2 ~ /^code-builders\// { print "  M " $2 "   (under code-builders)"; next }
  $2 !~ /\.md$/ { print "  M " $2 "   (not a document)"; next }')
if [ -n "$REASONS" ]; then
  echo "PULL REQUEST and a green CI run before the merge. $(echo "$REASONS" | wc -l) of the changes ask for it; the first of them:"
  echo "$REASONS" | head -12
  exit 3
fi
echo "STRAIGHT to main: every change is an edit to the text of an existing document. Read the CI run after the push."
