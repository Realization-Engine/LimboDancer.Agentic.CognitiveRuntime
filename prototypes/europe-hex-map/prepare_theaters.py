"""Uniform theater reference packages, sharing one 26 km lattice."""
import json,math,hashlib
from shapely.geometry import shape,Point,Polygon,mapping
from shapely.ops import transform,unary_union
from shapely import STRtree,union_all,make_valid

def prepare(api,western,pilot,southern):
    root=api.ROOT
    manifest=json.loads((root/'sources/theater-manifest.json').read_text(encoding='utf-8'))
    for s in manifest['sources']:assert hashlib.sha256((root/'sources'/s['file']).read_bytes()).hexdigest()==s['sha256']
    raw=json.loads((root/'sources/ne_10m_populated_places-theaters.geojson').read_text(encoding='utf-8'))
    towns=[]
    for f in raw['features']:
        p=f['properties'];x,y=api.project(*f['geometry']['coordinates'])
        towns.append(dict(name=p.get('NAME') or p.get('NAMEASCII'),x=round(x,3),y=round(y,3),rank=p.get('SCALERANK',10)))
    rivers=[]
    for f in json.loads((root/'sources/ne_10m_rivers_lake_centerlines-theaters.geojson').read_text(encoding='utf-8'))['features']:
        rivers.append((make_valid(transform(api.project,shape(f['geometry']))),f['properties'].get('name') or 'Unnamed watercourse'))
    base=json.loads((root/'map.json').read_text(encoding='utf-8'))
    land=unary_union([shape(f['geometry']) for f in base['features'] if f['properties']['layer']=='land'])
    features=[(shape(f['geometry']),f['properties']) for f in base['features'] if f['properties']['layer'] in ('mountains','lakes','saltBasins','forest')]
    ftree=STRtree([g for g,p in features])
    candidates=[]
    for package in [pilot,southern]:
        sources={s['id']:s for s in package['sources']}
        for f in package['features']:
            p=f['properties'];src=sources[p['sourceId']]
            for name,xy in zip(p['places'],f['geometry']['coordinates']):
                candidates.append(dict(name=name,x=xy[0],y=xy[1],kind=p['mode']+' connection',note=p['name']+'. Schematic transport evidence; no capacity or facility inferred.',source=src['url'],period=str(p.get('evidenceYear',p.get('existenceByYear')))+' existence evidence; baseline service unverified'))
    result={}
    for t in json.loads((root/'theaters.json').read_text(encoding='utf-8'))['theaters']:
        region=Polygon(t['boundary']).buffer(30).intersection(api.AREA)
        local=[p for p in towns if region.covers(Point(p['x'],p['y']))]
        points=[Point(p['x'],p['y']) for p in local];tree=STRtree(points)
        cells=[];polys=[];radius=15
        left,top,right,bottom=region.bounds
        for r in range(math.floor(top/22.5)-1,math.ceil(bottom/22.5)+2):
            for q in range(math.floor(left/(math.sqrt(3)*radius)-r/2)-1,math.ceil(right/(math.sqrt(3)*radius)-r/2)+2):
                x=math.sqrt(3)*radius*(q+r/2);y=22.5*r
                if not region.covers(Point(x,y)):continue
                poly=Polygon([(x+radius*math.cos(math.radians(60*i-30)),y+radius*math.sin(math.radians(60*i-30))) for i in range(6)])
                if not land.intersects(poly):continue
                hits=[features[int(i)] for i in ftree.query(poly,predicate='intersects')]
                cells.append(dict(id=f"{t['id']}:26km:{q}:{r}",coordinateId=f"EU26:{q}:{r}",x=round(x,3),y=round(y,3),vertices=[[round(a,3),round(b,3)] for a,b in list(poly.exterior.coords)[:-1]],towns=sorted({local[int(i)]['name'] for i in tree.query(poly,predicate='intersects')}),mountainRegions=sorted({p['name'] for g,p in hits if p['layer']=='mountains'}),waterNames=sorted({p['name'] for g,p in hits if p['layer'] in ('lakes','saltBasins')})))
                polys.append(poly)
        footprint=union_all(polys,grid_size=.000001)
        local_rivers=[dict(name=n,geometry=mapping(g.intersection(footprint).simplify(.25))) for g,n in rivers if g.intersects(footprint)]
        sites=list(western['sites']) if t['id']=='western' else []
        seen={s['name'] for s in sites}
        for site in candidates:
            if site['name'] not in seen and footprint.covers(Point(site['x'],site['y'])):
                sites.append(site);seen.add(site['name'])
        result[t['id']]=dict(name=t['name'],hexFootprint=mapping(footprint),sourceManifest=manifest,gridAcrossFlatsKm=math.sqrt(3)*radius,cells=cells,towns=local,rivers=local_rivers,sites=sites,scope='Reference geography and dated logistics evidence. Coverage is partial; no operational state inferred.')
    (root/'theater-workspaces.json').write_text(api.canonical(result),encoding='utf-8')
    return result
