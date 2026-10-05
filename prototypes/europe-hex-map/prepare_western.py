"""Compile a reference theater presentation, independently of campaign geography."""
import json,math,hashlib
from pathlib import Path
from shapely.geometry import shape,Point,Polygon,mapping
from shapely.ops import transform,unary_union
from pyproj import Transformer
ROOT=Path(__file__).resolve().parent

def prepare():
 manifest=json.loads((ROOT/'sources/western-manifest.json').read_text(encoding='utf-8'))
 for src in manifest['sources']:assert hashlib.sha256((ROOT/'sources'/src['file']).read_bytes()).hexdigest()==src['sha256']
 forward=Transformer.from_crs(4326,3035,always_xy=True)
 def project(x,y,z=None):
  a,b=forward.transform(x,y);return (a-4321000)/1000,(3210000-b)/1000
 towns=[]
 raw=json.loads((ROOT/'sources/ne_10m_populated_places-western.geojson').read_text(encoding='utf-8'))
 for f in raw['features']:
  p=f['properties'];x,y=project(*f['geometry']['coordinates'])
  towns.append({'name':p.get('NAME') or p.get('NAMEASCII'),'x':round(x,3),'y':round(y,3),'rank':p.get('SCALERANK',10)})
 rivers=[]
 for f in json.loads((ROOT/'sources/ne_10m_rivers_lake_centerlines-western.geojson').read_text(encoding='utf-8'))['features']:
  rivers.append({'name':f['properties'].get('name') or 'Unnamed watercourse','geometry':mapping(transform(project,shape(f['geometry'])).simplify(.25))})
 base=json.loads((ROOT/'map.json').read_text(encoding='utf-8'))
 land=unary_union([shape(f['geometry']) for f in base['features'] if f['properties']['layer']=='land'])
 theater=json.loads((ROOT/'theaters.json').read_text(encoding='utf-8'))['theaters'][0]
 region=Polygon(theater['boundary']);cells=[];radius=15
 for r in range(-40,60):
  for q in range(-85,55):
   x=math.sqrt(3)*radius*(q+r/2);y=1.5*radius*r
   if not region.buffer(30).contains(Point(x,y)):continue
   vertices=[[round(x+radius*math.cos(math.radians(60*i-30)),3),round(y+radius*math.sin(math.radians(60*i-30)),3)] for i in range(6)]
   poly=Polygon(vertices)
   if not land.intersects(poly):continue
   cells.append({'id':f'western:26km:{q}:{r}','x':round(x,3),'y':round(y,3),'vertices':vertices,'towns':[t['name'] for t in towns if poly.covers(Point(t['x'],t['y']))]})
 # Reconstruct full-precision lattice vertices before union so rounded display
 # coordinates cannot leave hairline seams between adjoining sectors.
 from shapely import union_all
 footprint_polys=[]
 for cell in cells:
  q,r=map(int,cell['id'].split(':')[-2:])
  cx=math.sqrt(3)*radius*(q+r/2);cy=1.5*radius*r
  footprint_polys.append(Polygon([(cx+radius*math.cos(math.radians(60*i-30)),cy+radius*math.sin(math.radians(60*i-30))) for i in range(6)]))
 footprint=union_all(footprint_polys,grid_size=0.000001)
 src1='https://www.ibiblio.org/hyperwar/USA/USA-E-Logistics2/USA-E-Logistics2-5.html'
 src2='https://www.ibiblio.org/hyperwar/USA/USA-E-Logistics2/USA-E-Logistics2-6.html'
 # Coordinates are approximate town centers, not facility or railway-station positions.
 specs=[('Antwerp',4.40,51.22,'port','Port clearance involved onward rail, road and water transport.',src1),('Cherbourg',-1.62,49.64,'port','Cotentin supplies fed inland transport and transfer points.',src1),('Paris',2.35,48.86,'transfer','Rail transfer points supported the final stages of the Red Ball service.',src1),('Granville',-1.60,48.84,'transfer','A rail transfer point for supplies moved from the Cotentin area.',src1),('Dol',-1.75,48.55,'transfer','A rail transfer point near the base of the Cherbourg Peninsula.',src1),('Liege',5.58,50.63,'depot','Part of the forward logistics concentration along the Meuse.',src2),('Namur',4.87,50.47,'depot','Part of the Meuse forward logistics area.',src2),('Charleroi',4.44,50.41,'depot','The forward logistics concentration extended west to this area.',src2),('Mons',3.95,50.45,'depot','An advance depot area served by transport from Antwerp.',src1),('Soissons',3.32,49.38,'route','A Red Ball route extension served First Army through this area.',src1),('Melun',2.66,48.54,'route','The eastern road route passed this area in support of Third Army.',src1),('Sommesous',4.20,48.74,'route','The eastern road route extended here in support of Third Army.',src1)]
 sites=[]
 for name,lon,lat,kind,note,url in specs:
  x,y=project(lon,lat);sites.append({'name':name,'x':round(x,3),'y':round(y,3),'kind':kind,'note':note,'source':url,'period':'1944-1945 historical reference; not current campaign state'})
 data={'hexFootprint':mapping(footprint),'sourceManifest':manifest,'gridAcrossFlatsKm':math.sqrt(3)*radius,'cells':cells,'towns':towns,'rivers':rivers,'sites':sites,'scope':'Reference geography and dated historical logistics examples; no operational availability or capacity inferred'}
 (ROOT/'western-theater.json').write_text(json.dumps(data,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
 return data
