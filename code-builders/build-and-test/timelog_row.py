"""Appends a row to the ASL Unit Time Log with its end read from the clock, so no end time is ever typed from memory.

The rule (Pass Standing Rules, section 0, point 10): read the clock before writing a time. On 2026-10-21 end times were typed three times
without it (13:17, 12:34, 13:01 for 12:07, 12:23, 12:51); this script is the stop.

Usage: python timelog_row.py <log file> <start HH:MM> "<task>" "<note>" [--began-yesterday]
The row goes after the last row of the log's last table; its end is now, and its duration end minus start. A task begun before midnight
names --began-yesterday, so the start is read on the day before; without it a start after the clock still stops.
"""
import datetime
import sys

path, start, task, note = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
now = datetime.datetime.now()
begun = now.replace(hour=int(start[:2]), minute=int(start[3:5]), second=0, microsecond=0)
if "--began-yesterday" in sys.argv[5:]:
    begun -= datetime.timedelta(days=1)
minutes = int((now - begun).total_seconds() // 60)
if minutes < 0:
    sys.exit(f"STOP: the start {start} is after the clock's {now:%H:%M}")
row = f"| {task} | {start} | {now:%H:%M} | {minutes // 60}:{minutes % 60:02d} | | {note} |"
lines = open(path, encoding="utf-8").read().split("\n")
last = max(index for index, line in enumerate(lines) if line.startswith("| "))
lines.insert(last + 1, row)
open(path, "w", encoding="utf-8", newline="\n").write("\n".join(lines))
print(row)
