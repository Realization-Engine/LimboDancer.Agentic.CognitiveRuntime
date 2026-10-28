"""Appends pass 72's section to the ASL Unit Time Log (times read from the notes' clock reads, file times, and commit times).

    python timelog_section.py < /dev/null
"""
from pathlib import Path

LOG = Path(__file__).resolve().parents[3] / "src" / "ASL" / "docs" / "ASL Unit Time Log.md"
text = LOG.read_text(encoding="utf-8")
assert "## Pass 72:" not in text
rows = [
    ("The preparation: pass 71's state read, the prompt `Pass 72 Prompt I.md` written", "", "18:52", "", "", "The clock was not read at its start; no time is given"),
    ("The frame: the branch cut, pass 70's games retired, `base` (18:54 to 19:00), the pages read, three read-only frame agents, the design's sections 1 to 7 with R72.1 to R72.14, the briefs", "18:53", "19:07", "0:14", "", "Frame agents 18:55 to about 19:04"),
    ("The base measurement (main's build, alone)", "19:08", "19:10", "0:02", "", "The pass 70 script rewritten for today's page"),
    ("5a: the charge (R72.3 to R72.6), its Studio check, one fix round", "19:10", "20:11", "1:01", "0:55", ""),
    ("5b: the return to normal (R72.7), its Studio check, one game rewritten", "20:12", "20:59", "0:47", "0:20", "TPBF in the firer's own Location made to work in Play"),
    ("5c: the SSR tokens, A19.132, A19.32 (R72.9, R72.10, R72.13 to R72.15)", "21:00", "21:26", "0:26", "0:30", ""),
    ("5d: Ammunition Shortage (R72.11, R72.12, R72.16)", "21:27", "22:03", "0:36", "0:40", ""),
    ("5e: the hero's weapons (R72.8); the review briefs", "22:04", "22:25", "0:21", "0:25", ""),
    ("The four reviews beside `strict`; rulings R72.17 to R72.22; the fix brief", "22:26", "22:36", "0:10", "", ""),
    ("Fix Round A (the charge, Battle Hardening), its Studio check", "22:36", "22:57", "0:21", "", ""),
    ("Fix Round B (fire, ordnance, substitution, hero, texts, perf); R4.3 by the session", "22:58", "23:30", "0:32", "", ""),
    ("The whole-pass Studio check: 15 games played, the hex audit, 320 pixels, the measurement (the hidden page set aside)", "23:31", "23:46", "0:15", "", ""),
]
lines = [
    "",
    "## Pass 72: Infantry V: Heat of Battle and Unit Substitution, 2026-10-27",
    "",
    "One session under the user's word of 2026-10-27 (\"Run Pass 72 without my intervention up to the merge gate.\"). The times are the clock reads of the "
    "pass's notes (`code-builders/pass-history/pass-72-heat-of-battle/notes.md`), the files' and the commits' own times. Estimates are the frame's calibrated "
    "ones (design, section 2). The build of 5a to 5e ran 19:10 to 22:25 (3:15) against the forecast's 2:50; the plan gave 1:40 of building.",
    "",
    "| Task | Start | End | Duration | Estimate | Note |",
    "|---|---|---|---|---|---|",
]
lines += [f"| {t} | {s} | {e} | {d} | {est} | {n} |" for (t, s, e, d, est, n) in rows]
if not text.endswith("\n"):
    text += "\n"
LOG.write_text(text + "\n".join(lines) + "\n", encoding="utf-8", newline="\n")
print("ok", len(rows), "rows")
