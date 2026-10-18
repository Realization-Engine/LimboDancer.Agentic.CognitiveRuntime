import collections, re, sys

# Totals a dotnet-gcdump heapstat report by type, folding the size buckets together.
def load(path):
    totals = collections.Counter()
    counts = collections.Counter()
    header = []
    for line in open(path, encoding="utf-8", errors="replace"):
        if "GC Heap" in line:
            header.append(line.strip())
        match = re.match(r"\s*([\d,]+)\s+([\d,]+)\s+(.+?)\s+\[", line)
        if not match:
            continue
        size, count, name = match.groups()
        name = re.sub(r"\s*\(Bytes > [^)]*\)", "", name)
        totals[name] += int(size.replace(",", ""))
        counts[name] += int(count.replace(",", ""))
    return header, totals, counts

for path in sys.argv[1:]:
    header, totals, counts = load(path)
    print(path.split("\\")[-1].split("/")[-1], *header)
    for name, size in totals.most_common(14):
        print(f"  {size / 1048576:7.2f} MB {counts[name]:>9,}  {name[:110]}")
