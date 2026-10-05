"""Static geographic preview, independent of the browser viewer."""
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parent
data=json.loads((ROOT/"map.json").read_text())
im=Image.new("RGB",(1700,1570),"#f5f4ed"); d=ImageDraw.Draw(im)
def font(size,bold=False):
    return ImageFont.truetype("C:/Windows/Fonts/"+("segoeuib.ttf" if bold else "segoeui.ttf"),size)
d.text((55,28),"EUROPE / CAMPAIGN ATLAS",font=font(30,True),fill="#263c3b")
d.text((55,73),"Mountain ranges and year-2000 tree cover  |  104 km hex spacing  |  Reference geography",font=font(18),fill="#647573")
scale=.295
def xy(p): return ((p[0]+2400)*scale+80,(p[1]+2500)*scale+130)
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
for c in data["cities"]:
    x,y=xy([c["x"],c["y"]]); d.ellipse((x-3,y-3,x+3,y+3),fill="#315d50")
    d.text((x+7,y-16),c["name"],font=font(17),fill="#263c3b",stroke_width=2,stroke_fill="#f5f4ed")
d.text((55,1510),"Brown: mountain regions. Green: tree-cover reference (2000). Modern road/rail overlays available in the interactive map.",font=font(17),fill="#647573")
im.save(ROOT/"preview.png")
