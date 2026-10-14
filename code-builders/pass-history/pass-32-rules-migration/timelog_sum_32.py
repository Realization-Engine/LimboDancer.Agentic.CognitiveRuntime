"""Sums the Duration column (H:MM) of every time-log section whose heading names pass 32 (32.a to 32.j, the fixes rolled into them), per section and in all."""
import pathlib
import re

PATH = pathlib.Path("E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Unit Time Log.md")
total = 0
section = None
sums = {}
for line in PATH.read_text(encoding="utf-8").split("\n"):
    if line.startswith("## "):
        section = line[3:].strip()
        continue
    if section is None or not re.search(r"\b32\.[a-j]\b|Pass 32\b|passes 32", section):
        continue
    cells = line.split(" | ")
    if len(cells) < 5 or not line.startswith("| "):
        continue
    match = re.fullmatch(r"(\d+):(\d\d)", cells[3].strip())
    if not match:
        continue
    minutes = int(match.group(1)) * 60 + int(match.group(2))
    sums[section] = sums.get(section, 0) + minutes
    total += minutes
for name, minutes in sums.items():
    print(f"{minutes // 60}:{minutes % 60:02d}  {name[:110]}")
print(f"ALL {total // 60}:{total % 60:02d}")
