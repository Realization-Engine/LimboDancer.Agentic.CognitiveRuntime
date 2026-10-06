"""Compile one bounded regional reference view on the common coordinate system."""
import json,math
from shapely.geometry import Polygon,Point,shape,mapping,box
from shapely.ops import transform,unary_union
from shapely import union_all

def prepare(api,workspaces):
    d=json.loads((api.ROOT/'sources/regional-campaign.json').read_text(encoding='utf-8'))
    d['sourceHash']=api.digest(d)
    region=transform(api.project,box(*d['bounds']))
    base=json.loads((api.ROOT/'map.json').read_text(encoding='utf-8'))
    land=unary_union([shape(f['geometry']) for f in base['features'] if f['properties']['layer']=='land'])
    western=workspaces[d['theaterId']];d['towns']=[t for t in western['towns'] if region.covers(Point(t['x'],t['y']))]
    left,top,right,bottom=region.bounds;d['view']=[left-10,top-10,right-left+20,bottom-top+20]
    cells=[];polys=[];radius=7.5
    for r in range(math.floor(top/(radius*1.5))-1,math.ceil(bottom/(radius*1.5))+2):
        for q in range(math.floor(left/(math.sqrt(3)*radius)-r/2)-1,math.ceil(right/(math.sqrt(3)*radius)-r/2)+2):
            x=math.sqrt(3)*radius*(q+r/2);y=1.5*radius*r
            if not region.covers(Point(x,y)):continue
            poly=Polygon([(x+radius*math.cos(math.radians(60*i-30)),y+radius*math.sin(math.radians(60*i-30))) for i in range(6)])
            if not poly.intersects(land):continue
            polys.append(poly);cells.append(dict(id=f'EU13:{q}:{r}',x=x,y=y,vertices=list(poly.exterior.coords)[:-1],towns=[t['name'] for t in d['towns'] if poly.covers(Point(t['x'],t['y']))]))
    footprint=union_all(polys,grid_size=.000001);d['hexFootprint']=mapping(footprint);d['cells']=cells;d['gridAcrossFlatsKm']=math.sqrt(3)*radius
    d['rivers']=[dict(name=f['name'],geometry=mapping(shape(f['geometry']).intersection(footprint))) for f in western['rivers'] if shape(f['geometry']).intersects(footprint)]
    for o in d['objectives']:o['x'],o['y']=api.project(o['lon'],o['lat'])
    (api.ROOT/'regional-campaign.json').write_text(api.canonical(d),encoding='utf-8')
    return d
