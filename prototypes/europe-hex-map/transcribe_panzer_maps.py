"""Compile reviewed row lists and raster-aligned hex geometry. Not engine admission."""
import json,math,hashlib
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parent
DIRECTIONS=[(1,0),(1,-1),(0,-1),(-1,0),(-1,1),(0,1)]
def clip(points,axis,bound,keep_greater):
    result=[]
    for a,b in zip(points,points[1:]+points[:1]):
        inside=lambda p:p[axis]>=bound if keep_greater else p[axis]<=bound
        if inside(a):result.append(a)
        if inside(a)!=inside(b):
            t=(bound-a[axis])/(b[axis]-a[axis]);v=[a[j]+t*(b[j]-a[j]) for j in (0,1)];v[axis]=bound;result.append(v)
    return result

def build():
    spec=json.loads((ROOT/'sources/situations/panzer-leader-04-terrain-review.json').read_text())
    target=ROOT/'sources/situations/panzer-leader-04.json';data=json.loads(target.read_text());w,h=1130,3138;step=113;rad=step/math.sqrt(3)
    for board in data['boards']:
        bid=board['id'];review=spec[bid];source='board-'+bid
        data['sources'][source]={'path':board['source'],'sha256':board['sha256'],'page':1}
        evidence={'status':'interpreted','sourceIds':[source],'note':'Manual first-pass row-by-row transcription of the Imaginative Strategist board. Not engine-admitted. Decorative spill and slope extent require review.'}
        edgeEvidence={'status':'interpreted','sourceIds':[source],'note':'Heavy colored LOS symbol sampled along the raster-aligned hex edge; compare source artwork before LOS admission. Barrier and crossing remain unknown.'}
        im=Image.open(ROOT/'assets/panzer-leader-04'/board['image']).convert('RGB').resize((w,h));cells=[];lookup={}
        towns={name:set(ids) for name,ids in review['places'].items()}
        for row in range(33):
            letter=chr(65+row) if row<26 else chr(65+row-26)*2
            for col in range(11 if row%2 else 10):
                local=f'{letter}{col+1}';x=col*step+(0 if row%2 else step/2);y=3+row*step*math.sqrt(3)/2;q=col-(row+1)//2
                poly=[[x+rad*math.cos(math.radians(a)),y+rad*math.sin(math.radians(a))] for a in [-30,30,90,150,210,270]]
                half=any(a<-.000001 or a>w+.000001 or b<-.000001 or b>h+.000001 for a,b in poly)
                for axis,lim,greater in [(0,0,True),(0,w,False),(1,0,True),(1,h,False)]:poly=clip(poly,axis,lim,greater)
                features=[f for f in ['road','stream','bridge','slope','hilltop'] if local in review[f]]
                placeIds=[name for name,ids in towns.items() if local in ids]
                base='town' if placeIds else 'marsh' if local in review['marsh'] else 'woods' if local in review['woods'] else 'open'
                cell={'id':f'{letter}-{col+1}','q':q,'r':row,'label':f'{bid}-{letter}-{col+1}','terrain':{'base':base,'features':features,'elevationMeters':None,'elevationLevel':2 if 'hilltop' in features else 1 if 'slope' in features else 0,'evidence':evidence},'edges':[],'placeIds':placeIds,'playable':True,'imageCenter':{'u':round(x/w,8),'v':round(y/h,8)},'imagePolygon':[{'u':round(a/w,8),'v':round(b/h,8)} for a,b in poly],'boundaryHalfHex':half,'setupAllowed':not half}
                cells.append(cell);lookup[q,row]=cell
        known={c['id'].replace('-','') for c in cells}
        for key in ['woods','marsh','stream','road','bridge','hilltop','slope']:
            if not set(review[key])<=known:raise ValueError('Bad transcription ID '+str(set(review[key])-known))
        # Read each shared edge once. Reciprocal records receive the same source symbol.
        cache={}
        for c in cells:
            x=c['imageCenter']['u']*w;y=c['imageCenter']['v']*h
            for direction,(dq,dr) in enumerate(DIRECTIONS):
                neighbor=lookup.get((c['q']+dq,c['r']+dr));pair=tuple(sorted([c['id'],neighbor['id']])) if neighbor else (c['id'],str(direction))
                if pair not in cache:
                    a=math.radians(-60*direction);nx,ny=math.cos(a),math.sin(a);mx,my=x+step/2*nx,y+step/2*ny
                    hits={'hilltop':0,'slope':0,'woods':0}
                    for t in range(-18,19):
                        present=set()
                        for offset in [-3,-2,-1,0,1,2,3]:
                            px=int(mx-t*ny+offset*nx);py=int(my+t*nx+offset*ny)
                            if not (0<=px<w and 0<=py<h):continue
                            R,G,B=im.getpixel((px,py))
                            if R>170 and G<80 and B<70:present.add('hilltop')
                            elif R>100 and 40<G<165 and B<70 and R>G*1.45:present.add('slope')
                            elif G>30 and R<140 and B<G*.65 and G>R*1.12:present.add('woods')
                        for kind in present:hits[kind]+=1
                    kind=max(hits,key=hits.get);cache[pair]=kind if hits[kind]>=28 else 'unknown'
                c['edges'].append({'direction':direction,'neighbor':{'boardId':bid,'hexId':neighbor['id']} if neighbor else None,'barrier':'unknown','crossing':'unknown','routeIds':[],'sourceObstacle':cache[pair],'evidence':edgeEvidence})
        board['hexes']=cells;board['grid'].update(orientation='pointy',expectedHexCount=346,coverage='partial')
        for name,ids in towns.items():
            place=next((p for p in data['mapModel']['places'] if p['id']==name),None)
            if not place:place={'id':name,'name':name.replace('-',' ').title(),'hexes':[],'evidence':evidence};data['mapModel']['places'].append(place)
            place['hexes']=[{'boardId':bid,'hexId':c['id']} for c in cells if name in c['placeIds']];place['evidence']=evidence
            zone=next((z for z in data['mapModel']['zones'] if z['id']==name),None)
            if zone:zone['hexes']=place['hexes'];zone['resolution']='unresolved'
        board['display']='Source-sheet orientation with 346 addressable hex fragments; first-pass terrain and LOS symbols, not admitted terrain. Boundary fragments remain separate until assembly aliases are resolved.'
        # Standalone review overlay, not a browser screenshot.
        preview=im.copy();draw=ImageDraw.Draw(preview);font=ImageFont.load_default(size=14)
        colors={'open':'#fff5bd','woods':'#47712b','town':'#a34227','marsh':'#397c91'}
        for c in cells:
            x=c['imageCenter']['u']*w;y=c['imageCenter']['v']*h
            draw.ellipse((x-6,y-6,x+6,y+6),fill=colors[c['terrain']['base']],outline='black')
            label=c['id'];draw.text((max(0,min(w-45,x-20)),max(0,min(h-18,y+8))),label,font=font,fill='black',stroke_width=2,stroke_fill='white')
        preview.save(ROOT/f'assets/panzer-leader-04/board-{bid}-transcription-review.png')
    data['admission']['blockers']=['First-pass hex terrain and colored LOS symbols require review','Road and stream memberships lack routed connectivity and crossing validation','Board rotations, boundary-hex aliases and seams unresolved','Victory membership requires review; east-of-stream subset unresolved','Dated force decomposition unresolved','Formation combat engine and terrain rules not connected']
    data['usage']='Schema-validated Situation reference and hex setup-planning package. First-pass map interpretation is not engine-admitted terrain.'
    from compile_panzer_lines import compile_lines
    compile_lines(data)
    target.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8',newline='\n')
    print('Compiled 692 hex fragments across A and C; terrain remains under review.')
if __name__=='__main__':build()
