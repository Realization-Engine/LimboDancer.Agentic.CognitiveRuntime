"""Read a dotnet-sampled-thread-time speedscope trace of a Play follower run, split by step and phase.

Steps are bounded by the opens of the page's FollowNext frame (the n-th open is the n-th step of the run).
For each step window: the inclusive time of chosen frames (outermost occurrence on a thread's stack, all threads),
and the self time of leaf frames on stacks that hold a LimboDancer frame (so idle threads are not counted).

Usage: trace_split.py <trace.speedscope.json> <steplog.json> <record.game.json>
"""
import bisect
import collections
import json
import sys

trace_path, steplog_path, record_path = sys.argv[1:4]

events = json.load(open(record_path, encoding="utf-8"))["events"]
changes = sorted((e["revision"], e["payload"]["phase"]) for e in events if e["type"] == "phase-changed")


def phase_at(rev):
    current = "setup"
    for r, p in changes:
        if r <= rev:
            current = p
        else:
            break
    return current


doc = json.load(open(trace_path, encoding="utf-8"))
frames = [f["name"] for f in doc["shared"]["frames"]]

WATCH = {
    "FollowNext": "Play.FollowNext",
    "Play.Refresh": "Play.Refresh",
    "Play render (BuildRenderTree)": "Pages.Play.BuildRenderTree",
    "LivePlay.FollowStep": "LivePlay.FollowStep",
    "RoutObligations (page)": "Play.get_RoutObligations",
    "RoutChoices (page)": "Play.get_RoutChoices",
    "MustRout": "GamePlanner.MustRout",
    "RoutAdvice": "GamePlanner.RoutAdvice",
    "RoutSurrenderAdvice": "GamePlanner.RoutSurrenderAdvice",
    "RoutScan.Enemies": "RoutScan.Enemies",
    "Encircled": "GamePlanner.Encircled(",
    "UpperLevelEncircled": "GamePlanner.UpperLevelEncircled",
    "CutOffUpstairs (page)": "Play.get_CutOffUpstairs",
    "UnitRows (page)": "Play.get_UnitRows",
    "StudioBoardCatalog.TryGetBoard": "StudioBoardCatalog.TryGetBoard",
    "CampaignPlayableFootprints.Read": "CampaignPlayableFootprints.Read",
    "GamePlanner.Composed": "GamePlanner.Composed",
    "GameState.At": "GameState.At(",
    "GameState.Location": "GameState.Location(",
    "GamePlanner.InfantryStep": "GamePlanner.InfantryStep",
    "GamePlanner.Los": "GamePlanner.Los(",
    "UnitOverlayBuilder.Build": "UnitOverlayBuilder.Build",
    "GamePlanner.Replay": "GamePlanner.Replay",
    "LivePlay.History": "LivePlay.History",
    "GC (gen collect)": "GarbageCollect",
}
watch_ids = {}
for i, name in enumerate(frames):
    for label, needle in WATCH.items():
        if needle in name:
            watch_ids.setdefault(i, []).append(label)
follow_ids = {i for i, n in enumerate(frames) if "Play.FollowNext" in n}
ld_ids = {i for i, n in enumerate(frames) if "LimboDancer" in n}

# Pass 1: the step boundaries, from FollowNext opens on any thread.
starts = []
for prof in doc["profiles"]:
    for ev in prof["events"]:
        if ev["type"] == "O" and ev["frame"] in follow_ids:
            starts.append(ev["at"])
starts.sort()
# FollowNext may be nested in itself? keep opens at least 50 ms apart.
bounds = []
for s in starts:
    if not bounds or s - bounds[-1] > 50:
        bounds.append(s)

log = json.load(open(steplog_path, encoding="utf-8"))
revs = [r for _, _, r in log]
print(f"FollowNext opens: {len(bounds)}; step log entries: {len(log)} (revisions {revs[0]} to {revs[-1]})")
# The run's steps are the log entries after the first (the first is the state before Run).
run_revs = revs[1:]
n = min(len(bounds), len(run_revs))
if len(bounds) != len(run_revs):
    print(f"WARNING: {len(bounds)} FollowNext opens against {len(run_revs)} run steps; pairing the last {n}")
    bounds = bounds[-n:]
    run_revs = run_revs[-n:]
windows = bounds + [bounds[-1] + 1500]  # the last step: 1.5 s


def window_of(t):
    k = bisect.bisect_right(windows, t) - 1
    return k if 0 <= k < n else None


incl = [collections.Counter() for _ in range(n)]
self_t = [collections.Counter() for _ in range(n)]


def add_span(counter_list, key, t0, t1):
    """Add the span [t0, t1) to key, split across step windows."""
    while t0 < t1:
        k = window_of(t0)
        if k is None:
            # before the first window or after the last: skip to the next boundary
            nxt = windows[0] if t0 < windows[0] else t1
            t0 = max(t0, min(nxt, t1)) if nxt > t0 else t1
            continue
        end = min(t1, windows[k + 1])
        counter_list[k][key] += end - t0
        t0 = end


for prof in doc["profiles"]:
    stack = []
    depth_of = collections.Counter()
    opened = {}
    ld_depth = 0
    last_t = None
    for ev in prof["events"]:
        t = ev["at"]
        if last_t is not None and stack and ld_depth > 0 and t > last_t:
            add_span(self_t, stack[-1], last_t, t)
        last_t = t
        f = ev["frame"]
        if ev["type"] == "O":
            stack.append(f)
            if f in ld_ids:
                ld_depth += 1
            if f in watch_ids:
                depth_of[f] += 1
                if depth_of[f] == 1:
                    opened[f] = t
        else:
            if stack and stack[-1] == f:
                stack.pop()
            elif f in stack:
                idx = len(stack) - 1 - stack[::-1].index(f)
                del stack[idx]
            if f in ld_ids:
                ld_depth = max(0, ld_depth - 1)
            if f in watch_ids and depth_of[f] > 0:
                depth_of[f] -= 1
                if depth_of[f] == 0:
                    for label in watch_ids[f]:
                        add_span(incl, label, opened[f], t)

phases = [phase_at(r) for r in run_revs]
groups = {"rtph": [k for k in range(n) if phases[k] == "rtph"], "not rtph": [k for k in range(n) if phases[k] not in ("rtph", "setup")]}
for name, ks in groups.items():
    print(f"\n== {name}: {len(ks)} steps; inclusive ms a step (mean), all threads")
    total = collections.Counter()
    for k in ks:
        total.update(incl[k])
    for label, v in sorted(total.items(), key=lambda kv: -kv[1]):
        print(f"  {label:<34} {v / max(1, len(ks)):8.1f}")
    st = collections.Counter()
    for k in ks:
        st.update(self_t[k])
    print(f"  -- top self time a step (stacks holding LimboDancer frames)")
    for f, v in st.most_common(25):
        print(f"  {v / max(1, len(ks)):8.1f}  {frames[f][:150]}")

print("\n== per step (rtph steps and the slowest others): FollowNext, render, RoutObligations, MustRout, Encircled, TryGetBoard")
order = sorted(range(n), key=lambda k: -(incl[k]["FollowNext"] + incl[k]["Play render (BuildRenderTree)"]))
for k in sorted(set(groups["rtph"]) | set(order[:8])):
    c = incl[k]
    print(f"  rev {run_revs[k]:>4} {phases[k]:<5} follow {c['FollowNext']:7.0f} render {c['Play render (BuildRenderTree)']:7.0f} routObl {c['RoutObligations (page)']:7.0f} mustRout {c['MustRout']:7.0f} "
          f"routAdv {c['RoutAdvice']:6.0f} enemies {c['RoutScan.Enemies']:6.0f} encircled {c['Encircled']:6.0f} upper {c['UpperLevelEncircled']:6.0f} board {c['StudioBoardCatalog.TryGetBoard']:6.0f} at {c['GameState.At']:6.0f} rows {c['UnitRows (page)']:5.0f}")
