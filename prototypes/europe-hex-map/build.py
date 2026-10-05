"""Build an offline Europe hex-map prototype from pinned Natural Earth inputs."""
import argparse, hashlib, json, math, html as html_module
from pathlib import Path
from pyproj import Transformer
from shapely.geometry import shape, mapping, Polygon, box, Point
from shapely.ops import transform, unary_union
from shapely import make_valid
ROOT = Path(__file__).resolve().parent
RADIUS = 60.0
BOUNDS = (-2400., -2500., 2800., 3300.)
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
            if key=="lakes" and "nasser" in str(f["properties"]).lower(): continue
            g = shape(f["geometry"])
            if key=="rivers": g=g.difference(box(-13,20,36,37.5))
            if not g.intersects(box(-40,10,65,85)): continue
            # Clip geographic input before projection to avoid remote antipodal artifacts.
            g = make_valid(transform(project, make_valid(g.intersection(box(-40,10,65,85)))))
            if not g.intersects(AREA.buffer(150)): continue
            p = f["properties"]
            features.append((g, p.get("name_en") or p.get("name") or "Unnamed", p.get("ne_id")))
        layers[key] = features
    for key in ("rivers","lakes","saltBasins"):
        name={"rivers":"africa_rivers","lakes":"africa_lakes","saltBasins":"africa_salt_basins"}[key]
        for f in read(name):
            p=f["properties"];g=make_valid(transform(project,shape(f["geometry"])))
            layers.setdefault(key,[]).append((g,p.get("name_en") or p.get("name") or ("Unnamed salt basin" if key=="saltBasins" else "Unnamed waterway"),p.get("ne_id")))
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
    for f in read("north_africa_places"):
        p=f["properties"];lon,lat=f["geometry"]["coordinates"];x,y=project(lon,lat)
        cities.append({"name":p.get("NAMEASCII") or p["NAME"],"x":round(x,3),"y":round(y,3),"lon":lon,"lat":lat})
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
    for r in range(-30,math.ceil(BOUNDS[3]/(RADIUS*1.5))+1):
        for q in range(-45,46):
            x,y=center(q,r)
            if AREA.contains(Point(x,y)): cells.append(cell(q,r,land,lakes))
    vector=[]
    for layer in ("land","lakes","rivers","saltBasins"):
        for i,(g,name,sourceid) in enumerate(layers[layer]):
            clipped=g.intersection(AREA)
            if clipped.is_empty: continue
            vector.append({"type":"Feature","geometry":mapping(clipped),"properties":{"layer":layer,"name":name,"id":str(sourceid) if sourceid else f"{layer}:{i}"}})
    # Exact intersections, not center-sample guesses, associate named waterways with cells.
    rivers=[(g,n) for g,n,_ in layers["rivers"]]
    for c in cells:
        g=polygon(c["q"],c["r"])
        c["rivers"]=sorted({n for line,n in rivers if line.intersects(g)})
        for key in ("lakes","saltBasins"):
            c[key]=sorted({n for geom,n,_ in layers[key] if geom.intersects(g)})
        c["cities"]=[a["name"] for a in cities if g.covers(Point(a["x"],a["y"]))]
    import overlays, sys
    terrain_labels=overlays.enrich(cells,vector,land,lakes,sys.modules[__name__])
    from terrain_review import attach
    terrain_review=attach(cells,vector,sys.modules[__name__])
    meta={"format":"europe-hex-prototype-v1","generator":"1.4.0","projection":"EPSG:3035",
          "localTransform":"x=(E-4321000)/1000; y=(3210000-N)/1000",
          "radiusKm":RADIUS,"acrossFlatsKm":round(math.sqrt(3)*RADIUS,3),"bounds":BOUNDS,
          "sourceManifest":manifest,"historicalStatus":"Contemporary generalized reference, not WWII-admitted",
          "unmodeled":["numeric elevation","WWII forest coverage","WWII roads and railways","WWII boundaries","historical crossings","lower-scale terrain generation"],"overlayPolicy":"Mountains are generalized region outlines; forest is GLC2000-derived; roads and multi-track rail are contemporary reference only."}
    transport=json.loads((ROOT/"sources/transport-1939-review.json").read_text(encoding="utf-8-sig"))
    assert transport["baselineDate"]=="1939-09-01"
    # No feature is admitted by relabeling a modern reference. Future admission needs
    # a reviewed importer and per-segment temporal validation, not just JSON edits.
    assert not transport["features"], "Historical transport importer not implemented"
    research={"status":"modern-research-only","features":[f for f in vector if f["properties"]["layer"] in ("roads","railways")],"cellSummaries":{}}
    vector=[f for f in vector if f["properties"]["layer"] not in ("roads","railways")]
    for c in cells:
        research["cellSummaries"][c["id"]]={"roadKm":c.pop("roadsReferenceKm"),"railKm":c.pop("railwaysReferenceKm")}
        c["historicalTransport"]={"baselineDate":"1939-09-01","coverage":"unknown","roadKm":None,"railKm":None}
    meta["transportBaseline"]=transport
    data={"metadata":meta,"cells":cells,"features":vector,"cities":cities,"terrainLabels":terrain_labels,"terrainReview":terrain_review}
    data["metadata"]["baseHash"]=digest(data)
    (ROOT/"map.json").write_text(canonical(data),encoding="utf-8")
    (ROOT/"research-transport.json").write_text(canonical(research),encoding="utf-8")
    pilot=json.loads((ROOT/"sources/historical-transport-pilot.json").read_text(encoding="utf-8"))
    pilot["sourceHash"]=digest(pilot)
    assert pilot["gameAdmitted"] is False
    source_ids={s["id"] for s in pilot["sources"]}
    assert len(source_ids)==len(pilot["sources"])
    assert len({f["properties"]["id"] for f in pilot["features"]})==len(pilot["features"])
    region_names={r["name"] for r in pilot["regions"]}
    for feature in pilot["features"]:
        props=feature["properties"]
        assert props["region"] in region_names
        assert all(id in source_ids for id in props.get("corroboratingSourceIds",[]))
        assert props["existenceByYear"] < 1939 and props["sourceId"] in source_ids
        assert props["gameAdmitted"] is False and props["alignmentStatus"]=="schematic"
        feature["geometry"]=mapping(transform(project,shape(feature["geometry"])))
    # Human-readable evidence register, generated from the same reviewed source records.
    esc=html_module.escape
    report=['<!doctype html><meta charset="utf-8"><title>Prewar transport research</title><style>body{font:16px/1.6 system-ui;max-width:1000px;margin:40px auto;padding:20px;color:#263c3b}table{border-collapse:collapse;width:100%}td,th{padding:12px;text-align:left;border-bottom:1px solid #ddd}small{color:#586762}</style><h1>Prewar transport research register</h1>',
        '<p>Reviewed '+esc(pilot["reviewedOn"])+'. Baseline: 1 September 1939. '+esc(pilot["coverage"])+'.</p>',
        '<p>'+esc(pilot["geometryPolicy"])+'</p><p>'+esc(pilot["regionPolicy"])+'</p>',
        '<p>Opening and upgrade dates establish existence by a date. They do not establish uninterrupted service, 1939 border access, track gauge, capacity, bridges or military usability. Indexed-excerpt records need full-page verification. All plotted connections remain research-only.</p>']
    report.append('<h2>Historical railway GIS acquired</h2><p><a href="https://doi.org/10.34847/nkl.6296qx69">Bárbara Polo Martín: European railways, 1920-1940</a>. Downloaded and inspected 14,427 mapped segments. The map displays 12,933 records classified ML or SL in TYPE_1940, with 0.5 km simplification. This is a period research layer, not a verified September 1939 network. Original files and repository metadata are retained with checksums. License: CC-BY-NC-4.0.</p><p>7,108 records have opening year zero; eight have opening years after 1939. Some opening and decade fields conflict. Exact service dates, source code meanings, completeness, mainline selection, junction topology and military capacity require further review. See README for the acquisition audit and next steps.</p>')
    for region in pilot["regions"]:
        report.append('<h2>'+esc(region["name"])+'</h2><p>'+esc(region["gaps"])+'</p><table><tr><th>Connection</th><th>Evidence by</th><th>Evidence and source</th></tr>')
        for feature in pilot["features"]:
            props=feature["properties"]
            if props["region"]!=region["name"]:continue
            src=next(s for s in pilot["sources"] if s["id"]==props["sourceId"])
            report.append('<tr><td>'+esc(props["name"])+'<br><small>'+esc(props["mode"])+'</small></td><td>'+str(props["existenceByYear"])+'</td><td>'+esc(props["dateNote"]+' '+src["finding"])+'<br><a href="'+esc(src["url"],quote=True)+'">'+esc(src["title"])+'</a><br><small>'+esc(src.get("locator",""))+'; '+esc(src.get("access","page-read"))+'</small></td></tr>')
            for other_id in props.get("corroboratingSourceIds",[]):
                other=next(s for s in pilot["sources"] if s["id"]==other_id)
                report.append('<tr><td colspan="3">Additional evidence: '+esc(other["finding"])+' <a href="'+esc(other["url"],quote=True)+'">'+esc(other["title"])+'</a></td></tr>')
        report.append('</table>')
    report.append('<h2>Period-map research leads</h2><p>These catalog records have not supplied admitted route geometry.</p>')
    for candidate in pilot.get("mapCandidates",[]):
        report.append('<h3><a href="'+esc(candidate["url"],quote=True)+'">'+esc(candidate["title"])+'</a></h3><p>'+esc(candidate["status"])+'. '+esc(candidate["finding"])+'</p><p>Next: '+esc(candidate["nextStep"])+'</p>')
    (ROOT/"transport-research.html").write_text('\n'.join(report),encoding="utf-8")
    pilot["places"]={name:list(project(*xy)) for name,xy in pilot["places"].items()}
    pilot["projection"]="local EPSG:3035 kilometers, same transform as map.json"
    (ROOT/"historical-transport-pilot.json").write_text(canonical(pilot),encoding="utf-8")
    from prepare_historical_rail import prepare
    historical_network=prepare()
    from prepare_western import prepare as prepare_western
    western=prepare_western()
    from prepare_southern_transport import prepare as prepare_south
    southern=prepare_south(sys.modules[__name__])
    from prepare_theaters import prepare as prepare_theaters
    workspaces=prepare_theaters(sys.modules[__name__],western,pilot,southern)
    view_data={**data,"theaterWorkspaces":workspaces,"southernTransport":southern,"researchTransport":research,"historicalPilot":pilot,"historicalRailNetwork":historical_network,"westernTheater":western,"theaterDefinitions":json.loads((ROOT/"theaters.json").read_text(encoding="utf-8"))}
    app=(ROOT/"app.template.js").read_text(encoding="utf-8").replace("__WESTERN_SCRIPT__",(ROOT/"theater-view.js").read_text(encoding="utf-8-sig"))
    (ROOT/"app.js").write_text(app,encoding="utf-8",newline="\n")
    (ROOT/"map-data.js").write_text("window.CAMPAIGN_MAP_DATA="+canonical(view_data)+";\n",encoding="utf-8",newline="\n")
    html=(ROOT/"viewer.html").read_text(encoding="utf-8")
    (ROOT/"index.html").write_text(html,encoding="utf-8",newline="\n")
    # Standards-based geographic hex export. Exact source feature topology remains in map.json.
    geo={"type":"FeatureCollection","features":[{"type":"Feature","properties":{k:v for k,v in c.items() if k not in ("vertices","x","y")},
          "geometry":{"type":"Polygon","coordinates":[[list(unproject(x,y)) for x,y in list(polygon(c["q"],c["r"]).exterior.coords)]]}} for c in cells]}
    (ROOT/"hexes.geojson").write_text(canonical(geo),encoding="utf-8")
    print(f"Built {len(cells)} cells, {len(vector)} features, {len(cities)} cities; base {meta['baseHash'][:16]}")
if __name__=="__main__": build()
