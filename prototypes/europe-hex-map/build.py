"""Build an offline Europe hex-map prototype from pinned Natural Earth inputs."""
import argparse, hashlib, json, math
from pathlib import Path
from pyproj import Transformer
from shapely.geometry import shape, mapping, Polygon, box, Point
from shapely.ops import transform, unary_union
from shapely import make_valid
ROOT = Path(__file__).resolve().parent
RADIUS = 60.0
BOUNDS = (-2400., -2500., 2800., 2100.)
AREA = box(*BOUNDS)
FORWARD = Transformer.from_crs("EPSG:4326", "EPSG:3035", always_xy=True)
INVERSE = Transformer.from_crs("EPSG:3035", "EPSG:4326", always_xy=True)
def project(x, y, z=None):
    X, Y = FORWARD.transform(x, y)
    return (X - 4321000.) / 1000., (3210000. - Y) / 1000.
def unproject(x, y):
    return INVERSE.transform(x * 1000 + 4321000, 3210000 - y * 1000)
def canonical(value):
    return json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":"))
def digest(value):
    return hashlib.sha256(canonical(value).encode()).hexdigest()
def read(name):
    return json.loads((ROOT / "sources" / (name + ".geojson")).read_text(encoding="utf-8-sig"))["features"]
def load():
    manifest = json.loads((ROOT / "sources/manifest.json").read_text(encoding="utf-8-sig"))
    for src in manifest["sources"]:
        data = (ROOT / "sources" / (src["name"] + ".geojson")).read_bytes()
        assert hashlib.sha256(data).hexdigest() == src["sha256"], "Source hash mismatch"
    layers = {}
    for key, name in [("land","ne_50m_land"),("lakes","ne_50m_lakes"),("rivers","ne_50m_rivers_lake_centerlines")]:
        features = []
        for f in read(name):
            g = shape(f["geometry"])
            if not g.intersects(box(-40,20,65,85)): continue
            # Clip geographic input before projection to avoid remote antipodal artifacts.
            g = transform(project, make_valid(g.intersection(box(-40,20,65,85))))
            if not g.intersects(AREA.buffer(150)): continue
            p = f["properties"]
            features.append((g, p.get("name_en") or p.get("name") or "Unnamed", p.get("ne_id")))
        layers[key] = features
    land = unary_union([f[0] for f in layers["land"]])
    lakes = unary_union([f[0] for f in layers["lakes"]])
    cities = []
    wanted = {"London","Paris","Berlin","Rome","Madrid","Lisbon","Warsaw","Vienna","Prague","Budapest","Bucharest","Athens","Sofia","Belgrade","Moscow","Kiev","Stockholm","Oslo","Helsinki","Copenhagen","Dublin","Istanbul","Amsterdam","Brussels","Reykjavik","St. Petersburg"}
    for f in read("ne_110m_populated_places"):
        p=f["properties"]; name=p.get("NAMEASCII") or p.get("NAME")
        if name not in wanted: continue
        lon,lat=f["geometry"]["coordinates"]; x,y=project(lon,lat)
        if AREA.contains(Point(x,y)):
            cities.append({"name":name,"x":round(x,3),"y":round(y,3),"lon":lon,"lat":lat})
    return manifest,layers,land,lakes,sorted(cities,key=lambda c:c["name"])
def center(q,r):
    return RADIUS*math.sqrt(3)*(q+r/2), RADIUS*1.5*r
def polygon(q,r):
    x,y=center(q,r)
    return Polygon([(x+RADIUS*math.cos(math.radians(60*i-30)),y+RADIUS*math.sin(math.radians(60*i-30))) for i in range(6)])
def cell(q,r,land,lakes):
    g=polygon(q,r); x,y=center(q,r)
    lf=g.intersection(land).area/g.area
    wf=g.intersection(lakes).area/g.area
    dry=max(0.,lf-wf)
    kind="land" if dry>=.95 else "water" if dry<=.02 else "coastal / mixed"
    lon,lat=unproject(x,y)
    return {"id":f"EU60:{q}:{r}","q":q,"r":r,"x":round(x,3),"y":round(y,3),
            "lon":round(lon,5),"lat":round(lat,5),"landFraction":round(dry,5),
            "lakeFraction":round(wf,5),"surface":kind,"elevation":None,"vegetation":None,
            "vertices":[[round(a,3),round(b,3)] for a,b in list(g.exterior.coords)[:-1]]}
def build():
    manifest,layers,land,lakes,cities=load()
    cells=[]
    for r in range(-30,31):
        for q in range(-45,46):
            x,y=center(q,r)
            if AREA.contains(Point(x,y)): cells.append(cell(q,r,land,lakes))
    vector=[]
    for layer in ("land","lakes","rivers"):
        for i,(g,name,sourceid) in enumerate(layers[layer]):
            clipped=g.intersection(AREA)
            if clipped.is_empty: continue
            vector.append({"type":"Feature","geometry":mapping(clipped),"properties":{"layer":layer,"name":name,"id":str(sourceid) if sourceid else f"{layer}:{i}"}})
    # Exact intersections, not center-sample guesses, associate named waterways with cells.
    rivers=[(g,n) for g,n,_ in layers["rivers"]]
    for c in cells:
        g=polygon(c["q"],c["r"])
        c["rivers"]=sorted({n for line,n in rivers if line.intersects(g)})
        c["cities"]=[a["name"] for a in cities if g.covers(Point(a["x"],a["y"]))]
    import overlays, sys
    terrain_labels=overlays.enrich(cells,vector,land,lakes,sys.modules[__name__])
    meta={"format":"europe-hex-prototype-v1","generator":"1.1.0","projection":"EPSG:3035",
          "localTransform":"x=(E-4321000)/1000; y=(3210000-N)/1000",
          "radiusKm":RADIUS,"acrossFlatsKm":round(math.sqrt(3)*RADIUS,3),"bounds":BOUNDS,
          "sourceManifest":manifest,"historicalStatus":"Contemporary generalized reference, not WWII-admitted",
          "unmodeled":["numeric elevation","WWII forest coverage","WWII roads and railways","WWII boundaries","historical crossings","lower-scale terrain generation"],"overlayPolicy":"Mountains are generalized region outlines; forest is GLC2000-derived; roads and multi-track rail are contemporary reference only."}
    data={"metadata":meta,"cells":cells,"features":vector,"cities":cities,"terrainLabels":terrain_labels}
    data["metadata"]["baseHash"]=digest(data)
    (ROOT/"map.json").write_text(canonical(data),encoding="utf-8")
    html=(ROOT/"viewer.html").read_text(encoding="utf-8").replace("__MAP_DATA__",canonical(data).replace("</","<\\/"))
    (ROOT/"index.html").write_text(html,encoding="utf-8",newline="\n")
    # Standards-based geographic hex export. Exact source feature topology remains in map.json.
    geo={"type":"FeatureCollection","features":[{"type":"Feature","properties":{k:v for k,v in c.items() if k not in ("vertices","x","y")},
          "geometry":{"type":"Polygon","coordinates":[[list(unproject(x,y)) for x,y in list(polygon(c["q"],c["r"]).exterior.coords)]]}} for c in cells]}
    (ROOT/"hexes.geojson").write_text(canonical(geo),encoding="utf-8")
    print(f"Built {len(cells)} cells, {len(vector)} features, {len(cities)} cities; base {meta['baseHash'][:16]}")
if __name__=="__main__": build()
