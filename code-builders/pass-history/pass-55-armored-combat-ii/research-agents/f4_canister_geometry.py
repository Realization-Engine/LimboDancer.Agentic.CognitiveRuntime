"""C8.4 Canister: which vertices can be aimed at, in the code's hex frame.

Frame (BoardGeometry.Neighbor, GamePlanner.Bearing): flat-topped hexes in columns, HexIndex(column, row),
odd columns half a hex higher, y grows downward. A facing f (UnitFacing order) is the hexspine at 60*f degrees
counterclockwise from east. A hexside d (HexsideDirection order) is 0 north, clockwise.
"""
import itertools
import math

S3 = math.sqrt(3)
SIDES = ["North", "NorthEast", "SouthEast", "South", "SouthWest", "NorthWest"]
FACINGS = ["East", "NorthEast", "NorthWest", "West", "SouthWest", "SouthEast"]


def neighbor(hex_, side):
    c, r = hex_
    even = c % 2 == 0
    if side == 0:
        return (c, r - 1)
    if side == 1:
        return (c + 1, r + (0 if even else -1))
    if side == 2:
        return (c + 1, r + (1 if even else 0))
    if side == 3:
        return (c, r + 1)
    if side == 4:
        return (c - 1, r + (1 if even else 0))
    return (c - 1, r + (0 if even else -1))


def center(hex_):
    c, r = hex_
    return (1.5 * c, S3 * (r - 0.5 * (c % 2)))


def bearing(a, b):
    (x1, y1), (x2, y2) = center(a), center(b)
    return math.degrees(math.atan2(-(y2 - y1), x2 - x1))


def off(one, two):
    return abs(((one - two) % 360 + 540) % 360 - 180)


def within_arc(a, b, facing):
    return off(bearing(a, b), facing * 60) <= 30 + 1e-6


def ring(hex_, most):
    seen = {hex_: 0}
    frontier = [hex_]
    for d in range(1, most + 1):
        nxt = []
        for h in frontier:
            for s in range(6):
                n = neighbor(h, s)
                if n not in seen:
                    seen[n] = d
                    nxt.append(n)
        frontier = nxt
    return seen


def vertices(hex_):
    x, y = center(hex_)
    return [(round(x + math.cos(math.radians(a)), 6), round(y - math.sin(math.radians(a)), 6)) for a in range(0, 360, 60)]


def build(firer, most=5):
    dist = ring(firer, most)
    by_vertex = {}
    for h in dist:
        for v in vertices(h):
            by_vertex.setdefault(v, set()).add(h)
    # vertex graph: two vertices one hexside apart are at distance 1.0
    full = {v: hs for v, hs in by_vertex.items() if len(hs) == 3}
    return dist, full


def vertex_graph_distance(full, starts, banned_hex):
    """Hexsides walked from any start vertex, never along a hexside of the firing hex."""
    pts = list(full)
    adj = {v: [] for v in pts}
    for a, b in itertools.combinations(pts, 2):
        if abs(math.hypot(a[0] - b[0], a[1] - b[1]) - 1.0) < 1e-4:
            if banned_hex in full[a] and banned_hex in full[b]:
                continue
            adj[a].append(b)
            adj[b].append(a)
    d = {s: 0 for s in starts}
    frontier = list(starts)
    while frontier:
        nxt = []
        for v in frontier:
            for w in adj[v]:
                if w not in d:
                    d[w] = d[v] + 1
                    nxt.append(w)
        frontier = nxt
    return d


def segment_hexes(firer, vertex, dist):
    """Hexes the LOS from the firer's center to the vertex runs through, by sampling; a sample on a boundary counts for both."""
    (x1, y1) = center(firer)
    (x2, y2) = vertex
    touched = {}
    for i in range(1, 2000):
        t = i / 2000.0
        x, y = x1 + (x2 - x1) * t, y1 + (y2 - y1) * t
        near = sorted(dist, key=lambda h: math.hypot(center(h)[0] - x, center(h)[1] - y))
        d0 = math.hypot(center(near[0])[0] - x, center(near[0])[1] - y)
        for h in near[:3]:
            if math.hypot(center(h)[0] - x, center(h)[1] - y) - d0 < 1e-6:
                touched[h] = touched.get(h, 0) + 1
    return touched


def name_offset(firer, hex_, L, R):
    """Express a hex as i steps of L and j steps of R from the firer."""
    for i in range(0, 5):
        for j in range(0, 5):
            h = firer
            for _ in range(i):
                h = neighbor(h, L)
            for _ in range(j):
                h = neighbor(h, R)
            if h == hex_:
                return "%dL+%dR" % (i, j)
    return "?"


def run(firer):
    dist, full = build(firer)
    print("=== firer HexIndex", firer, "(column is %s)" % ("even" if firer[0] % 2 == 0 else "odd"))
    own = [v for v, hs in full.items() if firer in hs]
    totals = {}
    seen_vertices = {}
    for f in range(6):
        L, R = (1 - f) % 6, (2 - f) % 6
        ca = {h for h in dist if h != firer and within_arc(firer, h, f)}
        assert neighbor(firer, L) in ca and neighbor(firer, R) in ca
        assert sum(1 for h in ca if dist[h] == 1) == 2
        spine = [v for v in own if neighbor(firer, L) in full[v] and neighbor(firer, R) in full[v]]
        assert len(spine) == 1
        inside = {v: hs for v, hs in full.items() if hs <= ca}
        readings = {}
        # Reading A: two hexsides walked from the CA hexspine's vertex of the firing hex.
        dA = vertex_graph_distance(full, spine, firer)
        readings["A: 2 hexsides walked from the CA spine vertex"] = [v for v in full if dA.get(v) == 2]
        # Reading B: two hexsides walked from any vertex of the firing hex, cluster wholly in the CA.
        dB = vertex_graph_distance(full, own, firer)
        readings["B: 2 hexsides walked from any firing-hex vertex, cluster in CA"] = [v for v in inside if dB.get(v) == 2]
        readings["B3: 3 hexsides walked from any firing-hex vertex, cluster in CA"] = [v for v in inside if dB.get(v) == 3]
        # Reading C: the three hexes all in the CA and all at range 2 or 3 (the outer vertices of the range-2 ring).
        readings["C: cluster in CA, every hex at range 2 or 3"] = [v for v, hs in inside.items() if all(dist[h] in (2, 3) for h in hs)]
        # Reading D: nearest hex of the cluster at range 2 (cluster may leave the CA).
        readings["D: nearest hex at range 2, farthest 3, any CA membership, vertex bearing within 30 deg"] = [
            v for v, hs in full.items() if sorted(dist[h] for h in hs)[0] == 2 and max(dist[h] for h in hs) == 3
            and off(math.degrees(math.atan2(-(v[1] - center(firer)[1]), v[0] - center(firer)[0])), f * 60) <= 30 + 1e-6]
        for key, vs in readings.items():
            totals.setdefault(key, []).append(len(vs))
        print("facing %d %-9s L=%s R=%s" % (f, FACINGS[f], SIDES[L], SIDES[R]))
        chosen = readings["C: cluster in CA, every hex at range 2 or 3"]
        # order the clusters left to right (by bearing, counterclockwise first)
        def ang(v):
            b = math.degrees(math.atan2(-(v[1] - center(firer)[1]), v[0] - center(firer)[0]))
            return -(((b - f * 60) + 540) % 360 - 180)
        for index, v in enumerate(sorted(chosen, key=ang), 1):
            seen_vertices.setdefault(v, []).append(f)
            hs = sorted(full[v], key=lambda h: (dist[h], name_offset(firer, h, L, R)))
            touched = segment_hexes(firer, v, dist)
            adjacent = sorted(name_offset(firer, h, L, R) for h in touched if dist[h] == 1)
            first = sorted(name_offset(firer, h, L, R) for h in touched if dist[h] >= 2)
            print("   aim %d: off-axis %+6.1f deg, cluster %s ranges %s; adjacent hex(es) on the LOS %s; LOS then runs in %s; walked %s" % (
                index, -ang(v), [name_offset(firer, h, L, R) for h in hs], [dist[h] for h in hs], adjacent, first, dB.get(v)))
    print("counts per facing by reading:")
    for key, counts in totals.items():
        print("   %-90s %s" % (key, counts))
    outer = [v for v, hs in full.items() if all(dist[h] in (2, 3) for h in hs)]
    print("outer vertices of the range-2 ring: %d; each in exactly one CA's five: %s" % (
        len(outer), all(len(seen_vertices.get(v, [])) == 1 for v in outer)))


run((10, 10))
run((11, 10))
