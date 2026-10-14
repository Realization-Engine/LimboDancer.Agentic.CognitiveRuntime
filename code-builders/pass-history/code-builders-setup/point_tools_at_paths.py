"""After bring_in_tools.py: the reusable tools stop naming this machine's folders (the user, 2026-10-13).

Every reusable Python tool takes the repository's root and the working folder from code-builders/paths.py, and every shell tool from
code-builders/paths.sh. The one-time scripts of pass-history are records and keep the paths they ran with.

Usage: python point_tools_at_paths.py <repository root> [--write]
"""
import os
import re
import sys

REPO_ROOT = sys.argv[1].rstrip("/")
NEW = REPO_ROOT + "/code-builders"
OLD_WORK = "Archive/GitHub/dlandi/pass32-tools"
OLD_REPO = "Archive/GitHub/dlandi/LimboDancer.MCP"
PY_IMPORT = ('import os as _os\nimport sys as _sys\n_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))\n'
             "from paths import REPO, WORK  # noqa: E402\n")
SH_SOURCE = 'source "$(dirname "${BASH_SOURCE[0]}")/../paths.sh"\n'


def python_tool(text):
    count = 0
    for old, name in ((OLD_WORK, "WORK"), (OLD_REPO, "REPO")):
        for prefix in ("E:/", "/e/"):
            pattern = re.compile(r'([rRfF]?)"' + re.escape(prefix + old))
            text, n = pattern.subn(lambda m: f'{name} + {m.group(1)}"', text)
            count += n
    text = text.replace('WORK + ""', "WORK").replace('REPO + ""', "REPO")
    if not count:
        return text, 0
    lines = text.split("\n")
    at = next((i for i, line in enumerate(lines) if line.startswith(("import ", "from "))), None)
    if at is None:
        at = next(i for i, line in enumerate(lines) if "WORK" in line or "REPO" in line)
    lines.insert(at, PY_IMPORT.rstrip("\n"))
    return "\n".join(lines), count


def shell_tool(text):
    count = 0
    lines = text.split("\n")
    kept = []
    for line in lines:
        if re.fullmatch(r'\s*S="?/e/' + re.escape(OLD_WORK) + r'"?\s*', line):
            kept.append('S="$WORK"')
            count += 1
        elif re.fullmatch(r'\s*R="?/e/' + re.escape(OLD_REPO) + r'"?\s*', line):
            count += 1
        else:
            kept.append(line)
    text = "\n".join(kept)
    for old, name in ((OLD_WORK, "$WORK"), (OLD_REPO, "$R")):
        for prefix in ("/e/", "E:/"):
            count += text.count(prefix + old)
            text = text.replace(prefix + old, name)
    if not count:
        return text, 0
    lines = text.split("\n")
    at = next(i for i, line in enumerate(lines) if not line.startswith("#"))
    lines.insert(at, SH_SOURCE.rstrip("\n"))
    return "\n".join(lines), count


changed = {}
for root, dirs, files in os.walk(NEW):
    dirs[:] = [d for d in dirs if d not in ("pass-history", "__pycache__")]
    for name in sorted(files):
        path = root.replace("\\", "/") + "/" + name
        if path in (NEW + "/paths.py", NEW + "/paths.sh") or not name.endswith((".py", ".sh")):
            continue
        with open(path, encoding="utf-8", newline="") as f:
            text = f.read()
        new, count = python_tool(text) if name.endswith(".py") else shell_tool(text)
        if count:
            changed[path] = new
            print(f"{count:3d}  {path[len(NEW) + 1:]}")
if "--write" in sys.argv:
    for path, text in changed.items():
        with open(path, "w", encoding="utf-8", newline="") as f:
            f.write(text)
    print("written:", len(changed), "files")
