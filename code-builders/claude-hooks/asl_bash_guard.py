"""Refuses a Bash call that breaks one of the ASL passes' standing rules.

A PreToolUse hook of Claude Code for the Bash tool: the call's JSON arrives on standard input. Exit code 2 blocks the call and hands the
reason on standard error back to Claude; any other outcome lets the call through. `check(command)` returns the reasons, for the test.
"""
import json
import re
import sys

REPO = "/e/archive/github/dlandi/limbodancer.mcp"


def check(command):
    reasons = []
    if re.search(r"<<-?\s*['\"]?[A-Za-z_]", command):
        reasons.append("a heredoc: write the script with the Write tool and run the file")
    if re.search(r"\bsed\s+(-\w+\s+)*-i\b", command):
        reasons.append("sed -i: use the Edit tool")
    # Pass 60: py and python.exe too; a building agent's multi-line -c went through under one of them.
    if re.search(r"\b(python3?|py)(\.exe)?\"?\s+(-\w+\s+)*-c\b", command) and "\n" in command:
        reasons.append("a multi-line python -c: write the script with the Write tool and run the file")
    # SVG memory plan, 2026-10-18: open(f, 'w').write(open(f).read()...) in one -c line truncated three files before reading them.
    if (re.search(r"\b(python3?|py)(\.exe)?\"?\s+(-\w+\s+)*-c\b", command) and re.search(r"open\([^)]*['\"][wa]\+?['\"]", command)
            and ".read(" in command):
        reasons.append("a python -c that reads and rewrites files: write the script with the Write tool, reading each file whole before opening it for writing")
    if re.search(r"\b(python3?|py)(\.exe)?\"?\s+-(\s|$)", command):
        reasons.append("a bare 'python -': write the script with the Write tool and run the file")
    for target in re.findall(r"(?:^|&&|;|\|\||\n)\s*cd\s+\"?([^\s\"&;|)]+)", command):
        path = target.replace("\\", "/").rstrip("/").lower()
        if re.match(r"^[a-z]:/", path):
            path = "/" + path[0] + path[2:]
        if not path.startswith("/") or path.startswith(REPO + "/"):
            reasons.append(f"cd into '{target}': it moves the session's working directory; use absolute paths, or cd to the repository root or outside it")
    if re.search(r"\bgit\b[^\n;&|]*\badd\s+(-A\b|--all\b|\.(\s|$)|\"?src/ASL/?\"?(\s|$))", command):
        reasons.append("git add of everything: stage explicit paths")
    if re.search(r"\bgit\b[^\n;&|]*\bpush\b[^\n;&|]*(--force\b|--force-with-lease\b|\s-f\b)", command):
        reasons.append("a force push")
    return reasons


if __name__ == "__main__":
    try:
        call = json.load(sys.stdin)
    except ValueError:
        sys.exit(0)
    found = check(call.get("tool_input", {}).get("command", "") or "")
    if found:
        print("Refused by the standing rules: " + "; ".join(found), file=sys.stderr)
        sys.exit(2)
