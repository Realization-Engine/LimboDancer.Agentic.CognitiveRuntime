"""Added lines of the pass's diff that carry a string literal with a space in it: t3_strings.py <out> <path>..."""
import io
import re
import subprocess
import sys

REPO = "E:/Archive/GitHub/dlandi/LimboDancer.MCP"
out = io.open(sys.argv[1], "w", encoding="utf-8")
for path in sys.argv[2:]:
    text = subprocess.run(["git", "-C", REPO, "diff", "-U0", "846eb5a3..HEAD", "--", path],
                          capture_output=True, stdin=subprocess.DEVNULL).stdout.decode("utf-8", "replace")
    name, line = "", 0
    for row in text.splitlines():
        if row.startswith("+++ "):
            name = row[6:]
            continue
        match = re.match(r"@@ -\S+ \+(\d+)", row)
        if match:
            line = int(match.group(1))
            continue
        if row.startswith("+") and not row.startswith("+++"):
            body = row[1:]
            if re.search(r'"[^"]*[A-Za-z]{3,} [^"]*"', body) or "play." in body or ("razor" in name and re.search(r">[^<]*[a-z]{3,} [a-z]", body)):
                out.write("%s:%d: %s\n" % (name.split("/")[-1], line, body.strip()))
            line += 1
out.close()
