"""Increment 3j: rewrite the files the increment changed or added with LF line endings, UTF-8 without BOM (the files the editor wrote with CRLF)."""
import subprocess
import sys

ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP"
changed = subprocess.run(["git", "-C", ROOT, "diff", "--name-only"], capture_output=True, text=True, check=True).stdout.split()
added = subprocess.run(["git", "-C", ROOT, "ls-files", "--others", "--exclude-standard", "src/ASL", "code-builders/pass-history/pass-70-infantry-iii"],
                       capture_output=True, text=True, check=True).stdout.split()
for name in changed + added:
    if not name.startswith(("src/ASL/", "code-builders/pass-history/pass-70-infantry-iii/")) or name.startswith("src/ASL/boards/"):
        continue
    path = f"{ROOT}/{name}"
    with open(path, "rb") as handle:
        data = handle.read()
    fixed = data[3:] if data.startswith(b"\xef\xbb\xbf") else data
    fixed = fixed.replace(b"\r\n", b"\n")
    if fixed != data:
        with open(path, "wb") as handle:
            handle.write(fixed)
        print("fixed", name)
sys.stdout.flush()
