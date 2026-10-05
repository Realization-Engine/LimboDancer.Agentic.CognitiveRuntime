"""Dated southern transport evidence, deliberately separate from gameplay geography."""
import json,html
from shapely.geometry import shape,mapping
from shapely.ops import transform

def prepare(api):
    data=json.loads((api.ROOT/"sources/southern-transport.json").read_text(encoding="utf-8"))
    data["sourceHash"]=api.digest(data)
    ids={s["id"] for s in data["sources"]}
    assert len({f["properties"]["id"] for f in data["features"]})==len(data["features"])
    esc=html.escape
    out=['<!doctype html><meta charset="utf-8"><title>Southern transport evidence</title><style>body{font:16px/1.6 system-ui;max-width:1000px;margin:40px auto;padding:20px;color:#263c3b}td,th{padding:10px;border-bottom:1px solid #ddd;text-align:left}table{border-collapse:collapse}</style><h1>North African transport evidence</h1><p>'+esc(data["geometryPolicy"])+'</p><p>By 1939 means existence evidence, not verified service on 1 September. Later evidence is displayed only when enabled. Route dates refer to the evidence used, not necessarily opening dates.</p>']
    out += ['<p>'+esc(g)+'</p>' for g in data["gaps"]]
    out.append('<table><tr><th>Connection</th><th>Evidence year</th><th>Source and qualification</th></tr>')
    for f in data["features"]:
        p=f["properties"];assert p["sourceId"] in ids and p["gameAdmitted"] is False
        src=next(s for s in data["sources"] if s["id"]==p["sourceId"])
        out.append('<tr><td>'+esc(p["name"]+' ('+p["mode"]+')')+'</td><td>'+str(p["evidenceYear"])+'</td><td>'+esc(p["note"])+' <a href="'+esc(src["url"],quote=True)+'">'+esc(src["title"])+'</a></td></tr>')
        f["geometry"]=mapping(transform(api.project,shape(f["geometry"])))
    out.append('</table><h2>Source findings</h2>')
    out += ['<p><strong>'+esc(s["title"])+':</strong> '+esc(s["finding"])+'</p>' for s in data["sources"]]
    (api.ROOT/"southern-transport-research.html").write_text('\n'.join(out),encoding="utf-8")
    (api.ROOT/"southern-transport.json").write_text(api.canonical(data),encoding="utf-8")
    return data
