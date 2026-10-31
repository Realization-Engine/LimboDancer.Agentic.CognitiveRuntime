"""Compare the four PR85 runs against the thresholds declared in MEASUREMENT.md."""
import json
import sys
from pathlib import Path


root = Path(sys.argv[1])
runs = {name: json.loads((root / f"{name}.json").read_text(encoding="utf-8"))
        for name in ("baseline-1", "current-1", "current-2", "baseline-2")}


def key(row):
    return row["workload"], row["operation"], row["cold"]


indexed = {name: {key(row): row for row in run["results"]} for name, run in runs.items()}
keys = set(indexed["baseline-1"])
assert all(set(rows) == keys for rows in indexed.values()), "Workloads differ"
hashes = [{item["workload"]: item["sha256"] for item in run["inputs"]} for run in runs.values()]
assert all(item == hashes[0] for item in hashes), "Input files differ"
stops = []
print("| Workload | Operation | Added bytes, pairs 1 / 2 | Added median us, pairs 1 / 2 | Added p90 us, pairs 1 / 2 |")
print("|---|---|---:|---:|---:|")
for item in sorted(keys):
    workload, operation, cold = item
    deltas = []
    exceeds = []
    for number in (1, 2):
        baseline = indexed[f"baseline-{number}"][item]
        current = indexed[f"current-{number}"][item]
        if current["answer"] != baseline["answer"] and workload != "mortar-pair":
            stops.append(f"Unexpected answer difference: {item}, pair {number}")
        if operation == "fire-command":
            byte_limit = max(65536, baseline["bytes"] * .10)
            median_limit = max(2000, baseline["medianUs"] * .20)
            p90_limit = max(2000, baseline["p90Us"] * .20)
        else:
            byte_limit = 32768 if cold else 4096
            median_limit = p90_limit = 2000 if cold else 250
        delta = tuple(current[field] - baseline[field] for field in ("bytes", "medianUs", "p90Us"))
        deltas.append(delta)
        exceeds.append(tuple(value > limit for value, limit in zip(delta, (byte_limit, median_limit, p90_limit))))
    if any(left and right for left, right in zip(*exceeds)):
        stops.append(f"Threshold exceeded in both pairs: {item}")
    columns = [" / ".join(f"{pair[index]:.1f}" for pair in deltas) for index in range(3)]
    temperature = "fresh" if cold else "warm"
    print(f"| {workload} | {operation}, {temperature} | " + " | ".join(columns) + " |")
print()
for stop in stops:
    print("STOP:", stop)
print(f"Compared {len(keys)} workloads in two alternating pairs; {len(stops)} stops.")
raise SystemExit(bool(stops))
