"""Appends pass 65's section to the ASL Unit Time Log: the rows whose times notes.md read from the clock as the work happened.
The documents row and the gate's row are added after it by timelog_row.py, which reads the end from the clock."""
P = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Unit Time Log.md"
ROWS = [
    ("The frame: the prompt and standing rules read, four read-only agents (the code map, three rule readers), the design's sections 1 to 6 with R65.1 to R65.25, the building brief, the branch cut from d0a8f4cd, pass65.conf", "22:25", "22:58", "0:33", "", "Base stage 22:59 to 23:05 in the background"),
    ("2a, crews (part of 65.6): IsCrew, catalog 1.18.0, one crew-placement member, the PRC index", "22:59", "23:37", "0:38", "0:45", "Agent 22:59 to 23:27, then the session's diff read, Studio check, commit"),
    ("2b, 65.1: Passengers and Riders as targets", "23:37", "00:23", "0:46", "3:00", "Agent 23:37 to 00:15"),
    ("2c, 65.2: Passenger action", "00:23", "01:07", "0:44", "2:30", "Agent 00:23 to 00:55; the Rout panel and the director list fixed by the session"),
    ("2d, 65.3: loading and unloading completed", "01:07", "01:43", "0:36", "1:15", "Agent 01:07 to 01:38"),
    ("2e, 65.4: Riders and Bailing Out", "01:43", "02:23", "0:40", "1:30", "Agent 01:43 to 02:12"),
    ("2f, 65.5: Carriers", "02:23", "02:55", "0:32", "0:50", "Agent 02:23 to 02:50"),
    ("2g, 65.6: the crew's own acts", "02:55", "03:28", "0:33", "1:40", "Agent 02:55 to 03:22"),
    ("2h, 65.7: the Immobilization TC, Crew Survival, Brew Up", "03:28", "04:10", "0:42", "1:10", "Agent 03:28 to 04:00"),
    ("2i, 65.8: captured vehicles", "04:10", "04:51", "0:41", "1:40", "Agent 04:10 to 04:45"),
    ("2j, 65.9: wrecks and Scrounging", "04:51", "05:35", "0:44", "0:50", "Agent 04:51 to 05:28"),
    ("2k: a crew as a target and in CC, A7.308, D5.311 (R65.26)", "05:35", "06:12", "0:37", "", "Agent 05:35 to 06:05; the build of 2a to 2k: 7:13 against the frame's 15:35 and the plan's 7:00"),
    ("The five reviews in parallel, and the strict stage", "06:13", "06:28", "0:15", "", "Strict 06:13 to 06:18, passed"),
    ("Fix Round A: the first referee's Passenger findings", "06:28", "06:54", "0:26", "", ""),
    ("Fix Round B: the second referee's Riders-to-wrecks findings (R65.33 revised)", "06:54", "07:17", "0:23", "", ""),
    ("Fix Round C: the Rules boundary, memory and performance", "07:17", "07:56", "0:39", "", ""),
    ("Fix Round D: the page and the record lines", "07:56", "08:35", "0:39", "", "The four rounds: 2:07"),
    ("The whole-pass Studio check and the measurement (bisect at Rounds B and C, a dotnet-trace sample)", "08:36", "08:49", "0:13", "", "The seeming 6x was three Studios and a profiler on one audit file; alone, level with the base"),
]
HEAD = ["", "## Pass 65: Passengers, Riders, and crews, 2026-10-23 and 24", "",
        "One session under the user's word of 2026-10-23 (\"start on Pass 65 without my intervention\"), renewed on 2026-10-24, then \"push and merge when ready\". Times from midnight on are 2026-10-24.", "",
        "| Task | Start | End | Duration | Estimate | Note |", "|---|---|---|---|---|---|"]
text = open(P, encoding="utf-8").read()
assert text.count("## Pass 65:") == 0
lines = text.rstrip("\n").split("\n") + HEAD + [f"| {a} | {b} | {c} | {d} | {e} | {f} |" for a, b, c, d, e, f in ROWS]
open(P, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
print(len(ROWS), "rows")
