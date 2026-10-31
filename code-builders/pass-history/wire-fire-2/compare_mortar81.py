"""Compare the four 81mm target-advice runs (mortar81-*.json) against the thresholds declared in MEASUREMENT.md.

Main reads the 81mm as an ordinary Gun, so the answers may differ; each is printed, and only the cost is held to the thresholds."""
import json
import sys
from pathlib import Path

root = Path(sys.argv[1])
runs = {name: json.loads((root / f"mortar81-{name}.json").read_text(encoding="utf-8"))
        for name in ("baseline-1", "current-1", "current-2", "baseline-2")}
indexed = {name: {(row["workload"], row["operation"], row["cold"]): row for row in run["results"]} for name, run in runs.items()}
keys = set(indexed["baseline-1"])
assert all(set(rows) == keys for rows in indexed.values()), "Workloads differ"
hashes = [{item["workload"]: item["sha256"] for item in run["inputs"]} for run in runs.values()]
assert all(item == hashes[0] for item in hashes), "Input files differ"
stops = []
print("| Workload | Read | Added bytes, pairs 1 / 2 | Added median us, pairs 1 / 2 | Added p90 us, pairs 1 / 2 |")
print("|---|---|---:|---:|---:|")
answers = []
for item in sorted(keys):
    workload, operation, cold = item
    deltas, exceeds = [], []
    for number in (1, 2):
        baseline = indexed[f"baseline-{number}"][item]
        current = indexed[f"current-{number}"][item]
        if number == 1:
            answers.append((workload, cold, baseline["answer"], current["answer"]))
        byte_limit = 32768 if cold else 4096
        time_limit = 2000 if cold else 250
        delta = tuple(current[field] - baseline[field] for field in ("bytes", "medianUs", "p90Us"))
        deltas.append(delta)
        exceeds.append(tuple(value > limit for value, limit in zip(delta, (byte_limit, time_limit, time_limit))))
    if any(left and right for left, right in zip(*exceeds)):
        stops.append(f"Threshold exceeded in both pairs: {item}")
    columns = [" / ".join(f"{pair[index]:.1f}" for pair in deltas) for index in range(3)]
    print(f"| {workload} | {operation}, {'fresh' if cold else 'warm'} | " + " | ".join(columns) + " |")
print()
for workload, cold, before, after in answers:
    print(f"{workload} ({'fresh' if cold else 'warm'}): main {before!r}; PR 85 {after!r}")
print()
for stop in stops:
    print("STOP:", stop)
print(f"Compared {len(keys)} workloads in two alternating pairs; {len(stops)} stops.")
raise SystemExit(bool(stops))
