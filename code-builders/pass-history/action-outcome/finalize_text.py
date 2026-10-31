"""Normalize only this worktree's changed text files to the repository's LF/UTF-8 contract."""
from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[3]
paths = set()
for arguments in (["diff", "--name-only", "-z"], ["ls-files", "--others", "--exclude-standard", "-z"]):
    paths.update(subprocess.check_output(["git", "-C", str(root), *arguments]).decode("utf-8").strip("\0").split("\0"))
changed = 0
for name in sorted(paths):
    if not name.startswith(("src/ASL/", "code-builders/")) or name.startswith("src/ASL/boards/"):
        continue
    path = root / name
    if path.suffix not in {".cs", ".razor", ".css", ".json", ".md", ".py", ".ps1", ".txt"} or not path.is_file():
        continue
    data = path.read_bytes()
    normalized = data.decode("utf-8-sig").replace("\r\n", "\n").encode("utf-8")
    if data != normalized:
        path.write_bytes(normalized)
        changed += 1
print(f"Normalized {changed} changed text files; no unchanged source or board files touched.")
