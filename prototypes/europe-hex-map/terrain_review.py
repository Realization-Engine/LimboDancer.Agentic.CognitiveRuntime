"""Attach regional evidence without presenting discussion areas as terrain polygons."""
import json,html
from shapely.geometry import box
from shapely.ops import transform

def attach(cells,vector,api):
    review=json.loads((api.ROOT/"sources/africa-terrain-review.json").read_text(encoding="utf-8"))
    regions=[(r,transform(api.project,box(*r["bounds"]))) for r in review["records"]]
    for c in cells:
        g=api.polygon(c["q"],c["r"])
        c["periodTerrainContext"]=[r["id"] for r,area in regions if area.intersects(g)]
    for f in vector:
        p=f["properties"]
        if p["layer"]=="saltBasins":
            p.update(status="Salt basin / playa; seasonal water and passability unverified",permanentOpenWater=False,source="africa_salt_basins")
        elif p["layer"] in ("rivers","lakes"):
            p["status"]="Generalized alignment; wartime banks, flow and crossings unverified"
    esc=html.escape
    lines=['<!doctype html><meta charset="utf-8"><title>North African terrain evidence</title><style>body{font:17px/1.6 system-ui;max-width:900px;margin:40px auto;padding:20px;color:#263c3b}</style><h1>North African terrain evidence</h1>']
    lines.extend('<p>'+esc(review[k])+'</p>' for k in ("geometryPolicy","forestReview"))
    lines.append('<p>Mountain regions, rivers, Great Bitter Lake and salt basins use pinned Natural Earth geometry. Historical sources establish terrain character, not surveyed wartime outlines. River lines do not imply perennial water. Salt basins are not counted as open water.</p>')
    for r in review["records"]:
        lines.append('<h2>'+esc(r["title"])+'</h2><p>'+esc(r["period"])+': '+esc(r["finding"])+'</p><a href="'+esc(r["url"],quote=True)+'">Historical evidence</a>')
    lines.append('<h2>Map leads for further digitization</h2>')
    for r in review["mapLeads"]:
        lines.append('<p><a href="'+esc(r["url"],quote=True)+'">Map catalog</a>: '+esc(r["status"])+'</p>')
    (api.ROOT/"africa-terrain-research.html").write_text('\n'.join(lines),encoding="utf-8")
    return review
