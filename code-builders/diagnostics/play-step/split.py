"""Split a Play follower run by phase.

Inputs: the record (game.json), the browser's step log (JSON list of [t_ms, wall_ms, revision]),
and optionally the dotnet-counters CSV of the same run.

A step's server work is its interval in the browser less the follow loop's one-second pause.
Counter samples (one a second) are prorated over the steps they overlap.
"""
import collections
import csv
import datetime
import json
import statistics
import sys

record, steplog = sys.argv[1], sys.argv[2]
counters = sys.argv[3] if len(sys.argv) > 3 else None

events = json.load(open(record, encoding="utf-8"))["events"]
changes = sorted((e["revision"], e["payload"]["phase"]) for e in events if e["type"] == "phase-changed")


def phase_at(rev):
    current = "setup"
    for r, p in changes:
        if r <= rev:
            current = p
        else:
            break
    return current


log = json.load(open(steplog, encoding="utf-8"))
steps = []
for (t0, w0, r0), (t1, w1, r1) in zip(log, log[1:]):
    steps.append({"rev": r1, "from": r0, "phase": phase_at(r1), "work": t1 - t0 - 1000.0, "w0": w0, "w1": w1})

if counters:
    samples = collections.defaultdict(dict)
    with open(counters, encoding="utf-8") as f:
        for row in csv.DictReader(f):
            stamp = datetime.datetime.strptime(row["Timestamp"], "%m/%d/%Y %H:%M:%S").timestamp() * 1000
            samples[stamp][row["Counter Name"]] = float(row["Mean/Increment"])
    alloc = "dotnet.gc.heap.total_allocated (By / 1 sec)"
    cpu_u = "dotnet.process.cpu.time (s / 1 sec)[cpu.mode=user]"
    cpu_s = "dotnet.process.cpu.time (s / 1 sec)[cpu.mode=system]"
    gen0 = "dotnet.gc.collections ({collection} / 1 sec)[gc.heap.generation=gen0]"
    gen2 = "dotnet.gc.collections ({collection} / 1 sec)[gc.heap.generation=gen2]"
    for s in steps:
        a = c = g0 = g2 = 0.0
        for end, vals in samples.items():
            start = end - 1000
            overlap = max(0.0, min(end, s["w1"]) - max(start, s["w0"])) / 1000.0
            if overlap:
                a += vals.get(alloc, 0) * overlap
                c += (vals.get(cpu_u, 0) + vals.get(cpu_s, 0)) * overlap
                g0 += vals.get(gen0, 0) * overlap
                g2 += vals.get(gen2, 0) * overlap
        s.update(alloc=a / 1e6, cpu=c * 1000, gen0=g0, gen2=g2)


def summary(name, group):
    work = [s["work"] for s in group]
    line = f"{name:<14} steps {len(group):>4}  work median {statistics.median(work):7.0f} ms  mean {statistics.mean(work):7.0f}  p90 {sorted(work)[int(len(work) * 0.9) - 1 if len(work) > 1 else 0]:7.0f}  max {max(work):7.0f}"
    if counters:
        line += f"  | alloc mean {statistics.mean(s['alloc'] for s in group):8.1f} MB  median {statistics.median(s['alloc'] for s in group):7.1f}  cpu mean {statistics.mean(s['cpu'] for s in group):6.0f} ms"
    print(line)


setup = [s for s in steps if s["phase"] == "setup"]
play = [s for s in steps if s["phase"] != "setup"]
print(f"steps {len(steps)} (setup {len(setup)}, play {len(play)}), revisions {log[0][2]} to {log[-1][2]}")
summary("all play", play)
summary("rtph", [s for s in play if s["phase"] == "rtph"])
summary("not rtph", [s for s in play if s["phase"] != "rtph"])
for p in ["rph", "pfph", "mph", "dfph", "afph", "rtph", "aph", "ccph"]:
    g = [s for s in play if s["phase"] == p]
    if g:
        summary(p, g)
print("\nslowest steps:")
for s in sorted(steps, key=lambda s: -s["work"])[:15]:
    extra = f" alloc {s['alloc']:8.1f} MB cpu {s['cpu']:6.0f} ms gen2 {s['gen2']:.1f}" if counters else ""
    print(f"  rev {s['from']:>4} -> {s['rev']:>4} {s['phase']:<5} work {s['work']:7.0f} ms{extra}")
if counters:
    print("\nlargest allocation:")
    for s in sorted(steps, key=lambda s: -s["alloc"])[:10]:
        print(f"  rev {s['from']:>4} -> {s['rev']:>4} {s['phase']:<5} alloc {s['alloc']:8.1f} MB work {s['work']:7.0f} ms cpu {s['cpu']:6.0f} ms gen2 {s['gen2']:.1f}")
json.dump(steps, open(steplog.replace(".json", "-steps.json"), "w", encoding="utf-8"))
