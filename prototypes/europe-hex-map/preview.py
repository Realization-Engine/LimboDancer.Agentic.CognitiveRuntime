"""Static geographic preview, independent of the browser viewer."""
import json, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parent
data=json.loads((ROOT/"map.json").read_text())
im=Image.new("RGB",(1700,1570),"#f5f4ed"); d=ImageDraw.Draw(im)
def font(size,bold=False):
    return ImageFont.truetype("C:/Windows/Fonts/"+("segoeuib.ttf" if bold else "segoeui.ttf"),size)
d.text((55,28),"EUROPE / CAMPAIGN ATLAS",font=font(30,True),fill="#263c3b")
d.text((55,73),"Mountain ranges and year-2000 tree cover  |  104 km hex spacing  |  Reference geography",font=font(18),fill="#647573")
pilot_mode="--pilot" in sys.argv
pilot=json.loads((ROOT/"historical-transport-pilot.json").read_text()) if pilot_mode else None
scale=.295
if pilot_mode:
    pts=[xy for f in pilot["features"] for xy in f["geometry"]["coordinates"]]
    left=min(x for x,y in pts)-100;top=min(y for x,y in pts)-40
    scale=min(1450/(max(x for x,y in pts)-left+100),1260/(max(y for x,y in pts)-top+40))

def xy(p): return ((p[0]-left)*scale+80,(p[1]-top)*scale+130) if pilot_mode else ((p[0]+2400)*scale+80,(p[1]+2500)*scale+130)
d.rectangle((80,130,1614,1487),fill="#dce8e8")
def drawgeom(g,layer):
    typ=g["type"]; coords=g.get("coordinates")
    if typ=="GeometryCollection":
        for child in g["geometries"]: drawgeom(child,layer)
    elif typ=="MultiPolygon":
        for poly in coords: drawgeom({"type":"Polygon","coordinates":poly},layer)
    elif typ=="Polygon":
        color={"land":"#d8ddc3","lakes":"#accbd0","forest":"#a4b997","mountains":"#b3ab92"}[layer]
        d.polygon([xy(p) for p in coords[0]],fill=color)
        for ring in coords[1:]:d.polygon([xy(p) for p in ring],fill="#dce8e8" if layer=="land" else "#d8ddc3")
    elif typ=="MultiLineString":
        for line in coords:drawgeom({"type":"LineString","coordinates":line},layer)
    elif typ=="LineString":d.line([xy(p) for p in coords],fill="#729fae",width=2)
order={"land":0,"forest":1,"mountains":2,"lakes":3,"rivers":4}
for f in sorted([f for f in data["features"] if f["properties"]["layer"] in order],key=lambda f:order[f["properties"]["layer"]]):drawgeom(f["geometry"],f["properties"]["layer"])
for c in data["cells"]:
    pts=[xy(p) for p in c["vertices"]];d.line(pts+[pts[0]],fill="#a0b6ac",width=1)
for c in ([] if pilot_mode else data["cities"]):
    x,y=xy([c["x"],c["y"]]); d.ellipse((x-3,y-3,x+3,y+3),fill="#315d50")
    d.text((x+7,y-16),c["name"],font=font(17),fill="#263c3b",stroke_width=2,stroke_fill="#f5f4ed")
d.text((55,1510),"Brown: mountain regions. Green: tree-cover reference (2000). 1939 transport pending validation; modern routes are research-only.",font=font(17),fill="#647573")
if pilot_mode:
    for f in pilot["features"]:
        d.line([xy(p) for p in f["geometry"]["coordinates"]],fill="#bc591c" if f["properties"]["mode"]=="road" else "#773f86",width=5)
    for name,point in pilot["places"].items():
        if scale<1 and name not in {"London","Paris","Lisbon","Madrid","Berlin","Milan","Rome","Oslo","Stockholm","Helsinki","Warsaw","Bucharest","Istanbul","Moscow","Odesa","Riga"}:continue
        x,y=xy(point);d.ellipse((x-4,y-4,x+4,y+4),fill="#773f86")
        # Offset the two closely spaced road endpoints for a legible static preview.
        offset={"Voorburg":(-115,15),"Zoetermeer":(12,12),"The Hague":(-100,-23),"Rotterdam":(8,0)}.get(name,(8,-24))
        d.text((x+offset[0],y+offset[1]),name,font=font(17),fill="#452b4c",stroke_width=2,stroke_fill="#f5f4ed")
    d.rectangle((0,0,1700,115),fill="#f5f4ed")
    d.text((55,28),"PREWAR TRANSPORT / EUROPE REGIONAL SURVEY",font=font(30,True),fill="#263c3b")
    d.text((55,73),"Purple: rail connections. Orange: roads. Schematic town links, not surveyed alignments.",font=font(18),fill="#647573")
    d.rectangle((0,1490,1700,1570),fill="#f5f4ed")
    d.text((55,1510),"Selected prewar existence evidence only. Operation on 1 Sep 1939 unverified. 55 connections / 11 regions. Source details in the research register.",font=font(17),fill="#647573")
if "--historical-rail" in sys.argv:
    network=json.loads((ROOT/"historical-rail-network.json").read_text(encoding="utf-8"))
    for f in network["features"]:
        if f["properties"]["displayTier"]>0:continue
        g=f["geometry"]
        lines=[g["coordinates"]] if g["type"]=="LineString" else g["coordinates"]
        for line in lines:d.line([xy(p) for p in line],fill="#71384c",width=2)
    d.rectangle((0,0,1700,115),fill="#f5f4ed")
    d.text((55,28),"HISTORICAL RAILWAY ATLAS / 1920-1940",font=font(30,True),fill="#263c3b")
    d.text((55,73),"Major corridor candidates; zoom in on the interactive map for more railway detail",font=font(18),fill="#647573")
    d.rectangle((0,1490,1700,1570),fill="#f5f4ed")
    d.text((55,1510),"Source: Barbara Polo Martin / NAKALA. CC-BY-NC-4.0. Simplified geometry. Geographic gaps remain.",font=font(17),fill="#647573")
    im.save(ROOT/"historical-rail-preview.png")
else:
    im.save(ROOT/("historical-pilot-preview.png" if pilot_mode else "preview.png"))
