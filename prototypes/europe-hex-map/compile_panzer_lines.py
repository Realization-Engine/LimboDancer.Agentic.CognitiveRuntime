"""Compile reviewed raster-local centerlines into schema-backed SVG geometry."""
import json,math
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parent

def linear(controls):
    result=[]
    for a,b in zip(controls,controls[1:]):
        n=max(1,math.ceil(math.dist(a,b)))
        result.extend([[a[k]+(b[k]-a[k])*j/n for k in (0,1)] for j in range(n)])
    return result+[controls[-1]]

def curve(controls):
    result=[]
    for i in range(len(controls)-1):
        p0=controls[max(0,i-1)];p1=controls[i];p2=controls[i+1];p3=controls[min(len(controls)-1,i+2)]
        steps=max(4,math.ceil(math.dist(p1,p2)))
        for j in range(steps):
            t=j/steps
            result.append([.5*((2*p1[k])+(-p0[k]+p2[k])*t+(2*p0[k]-5*p1[k]+4*p2[k]-p3[k])*t*t+(-p0[k]+3*p1[k]-3*p2[k]+p3[k])*t*t*t) for k in (0,1)])
    return result+[controls[-1]]

def compile_lines(data):
    source=json.loads((ROOT/'sources/situations/panzer-leader-04-lines.json').read_text())
    width,height=source['coordinateFrame']['width'],source['coordinateFrame']['height'];routes=[]
    for b in data['boards']:
        for h in b['hexes']:
            h['terrain']['features']=[f for f in h['terrain']['features'] if f not in ('road','stream')]
            for e in h['edges']:e['routeIds']=[]
    for item in source['paths']:
        b=next(b for b in data['boards'] if b['id']==item['boardId']);points=[[round(max(0,min(1,x/width)),7),round(max(0,min(1,y/height)),7)] for x,y in (linear(item['tracedPoints']) if 'tracedPoints' in item else curve(item['controlPoints']))]
        steps=[]
        for x,y in points:
            h=min(b['hexes'],key=lambda h:((h['imageCenter']['u']-x)*width)**2+((h['imageCenter']['v']-y)*height)**2)
            if not steps or steps[-1]!=h['id']:steps.append(h['id'])
            if item['kind'] not in h['terrain']['features']:h['terrain']['features'].append(item['kind'])
        lookup={h['id']:h for h in b['hexes']}
        for a,z in zip(steps,steps[1:]):
            for u,v in [(a,z),(z,a)]:
                edge=next((e for e in lookup[u]['edges'] if e['neighbor']=={'boardId':b['id'],'hexId':v}),None)
                if edge is None:raise ValueError('Nonadjacent route steps: '+item['id']+' '+u+' '+v)
                if item['id'] not in edge['routeIds']:edge['routeIds'].append(item['id'])
        routes.append({'id':item['id'],'kind':item['kind'],'path':[{'boardId':b['id'],'hexId':h} for h in steps],'condition':'unknown','evidence':{'status':'interpreted','sourceIds':['board-'+b['id']],'note':'Centerline traced against archived source-board artwork; subhex display geometry, not surveyed alignment or an admitted movement network.'},'geometry':{'type':'LineString','boardId':b['id'],'coordinateSystem':'normalized-source-image','coordinates':points}})
    data['mapModel']['routes']=routes
    return data

def build():
    target=ROOT/'sources/situations/panzer-leader-04.json';data=compile_lines(json.loads(target.read_text()))
    for b in data['boards']:
        src=Image.open(ROOT/'assets/panzer-leader-04'/b['image']).convert('RGB').resize((1130,3138));review=src.copy();draw=ImageDraw.Draw(review)
        for route in sorted([r for r in data['mapModel']['routes'] if r['geometry']['boardId']==b['id']],key=lambda r:r['kind']!='stream'):
            pts=[(x*1130,y*3138) for x,y in route['geometry']['coordinates']]
            draw.line(pts,fill='#0088ff' if route['kind']=='stream' else '#e11e83',width=3)
        review.save(ROOT/f'assets/panzer-leader-04/board-{b["id"]}-line-review.png')
    target.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8',newline='\n')
    print('Compiled',len(data['mapModel']['routes']),'curved road/stream paths')
if __name__=='__main__':build()
