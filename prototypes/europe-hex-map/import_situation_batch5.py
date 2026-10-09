"""Import PL05/06/07/12 from the archived original cards; reference-only packages."""
import copy,hashlib,json,math,random
from transcribe_panzer_maps import clip,DIRECTIONS
from compile_panzer_lines import curve
from pathlib import Path
import pypdfium2 as pdf
ROOT=Path(__file__).resolve().parent
ARCHIVE=Path(r'E:\Archive\WWII-Docs\1-PanzerBlitz-PanzerLeader\Original-Rules-and-Situations')
OUT=ROOT/'assets/panzer-shared'
def read(p):return json.loads(p.read_text(encoding='utf-8'))
def dump(p,d):p.write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8',newline='\n')
TYPES={};SOURCES={};BOARDS={};SHEETS={}
BASE=read(ROOT/'sources/situations/panzer-leader-17.json')
for n in [4,8,9,10,11,13,14,15,16,17,18,19,20]:
 d=read(ROOT/f'sources/situations/panzer-leader-{n:02}.json');SOURCES.update(d['sources']);TYPES.update({c['id']:copy.deepcopy(c) for c in d['counters']});BOARDS.update({b['id']:copy.deepcopy(b) for b in d['boards']})
def token(id,label,f,sheet,counter,x,y,kind='unit'):
 path=ARCHIVE/('Counters' if sheet.startswith('PB') else 'PanzerLeader/Counters')/(sheet+' Colour.pdf');SOURCES[sheet]={'path':str(path),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'page':1}
 if sheet not in SHEETS:SHEETS[sheet]=pdf.PdfDocument(str(path))[0].render(scale=3).to_pil()
 SHEETS[sheet].crop((x*3,y*3,(x+45)*3,(y+45)*3)).save(OUT/f'counters/{id}.png')
 c=copy.deepcopy(TYPES['us-rifle']);c.update(id=id,label=label,kind=kind,nationality=id.split('-')[0],side='German' if id.startswith('de-') else 'Allied',factors=dict(zip(['attack','weaponType','range','defense','movement'],f)) if f else None,artwork=f'counters/{id}.png',sourceSheet=sheet,sourceCounterId=counter,sourceCropPointsTopLeft=[x,y,45,45],match='numeric-factors-and-weapon-type-verified',notes=['Imaginative Strategist artwork matched to the printed card; optional annotations do not enable additional rules.']);TYPES[id]=c

for args in [
 ('uk-sexton','Sexton',[35,'(H)',35,7,8],'PL Commonwealth 4','6A01',495,0),
 ('uk-mg','British Machine gun',[2,'I',2,4,1],'PL Commonwealth 1','1D01',405,495),
 ('uk-107mortar','British 107 mm mortar',[10,'M',17,2,0],'PL Commonwealth 1','L01',360,360),
 ('uk-typhoon-rockets','Typhoon, rocket armed',None,'PL Commonwealth 4','Typhoon rockets',315,360,'aircraft'),
]:token(*args)
TYPES['uk-typhoon-rockets']['match']='source-symbol-verified'
TYPES['uk-typhoon-rockets']['notes']=['IS rocket-armed Typhoon matches the source role. Printed aircraft annotations are not ground combat factors. Air attacks remain unimplemented.']
for old,new in [('be-76mortar','uk-76mortar'),('be-achilles','uk-achilles')]:
 c=copy.deepcopy(TYPES[old]);c.update(id=new,nationality='uk',label=c['label'].replace('Belgian ','British '),notes=['Commonwealth equipment artwork matched to the printed card.']);TYPES[new]=c
# Source-card exceptions: no change to original numeric profiles to fit replacement artwork.
original=pdf.PdfDocument(BASE['situation']['sourcePdf'])[39].render(scale=2.5).to_pil()
for id,label,f,box in [
 ('uk-cromwell-goodwood','Cromwell (printed Goodwood profile)',[9,'A',8,8,7],(435,286,501,374)),
 ('de-maultier-goodwood','Maultier (printed Goodwood profile)',[50,'(H)',12,4,10],(773,379,837,465))]:
 c=copy.deepcopy(TYPES['us-rifle']);c.update(id=id,label=label,nationality=id.split('-')[0],side='German' if id.startswith('de-') else 'Allied',kind='unit',factors=dict(zip(['attack','weaponType','range','defense','movement'],f)),artwork=f'counters/{id}.png',artworkProvider='Original Avalon Hill card, explicit artwork exception',sourceSheet='original-card',sourceCounterId='PL05-'+id,sourceCropPointsTopLeft=[box[0]/2.5,box[1]/2.5,(box[2]-box[0])/2.5,(box[3]-box[1])/2.5],match='printed-source-counter',notes=['Original-card artwork retained: available IS profile differs. Printed factors are preserved; historical equipment correction is outside this import.']);TYPES[id]=c;original.crop(box).save(OUT/c['artwork'])
for old in ['uk-rifle','uk-mg','uk-scout','uk-engineer','uk-6pdr','uk-25pdr','uk-76mortar','uk-107mortar','uk-truck','uk-bren']:
 c=copy.deepcopy(TYPES[old]);c.update(id=old.replace('uk-','ca-'),nationality='ca',label='Canadian '+c['label'].replace('British ',''));c['notes'].append('Canadian force identity; shared Commonwealth palette and equipment artwork.');TYPES[c['id']]=c
TYPES['de-block-reichswald']=copy.deepcopy(TYPES['de-block']);TYPES['de-block-reichswald'].update(id='de-block-reichswald',label='Block',notes=['One positional obstacle, placed within the German setup zone. No combat factors. Not fixed to the Remagen bridge.'])
# Board B uses the same source-coordinate grid as A/C/D. All terrain remains interpreted.
pdfpath=ARCHIVE/'PanzerLeader/MapBoards/PL Map B Full.pdf';SOURCES['board-B']={'path':str(pdfpath),'sha256':hashlib.sha256(pdfpath.read_bytes()).hexdigest(),'page':1}
pdf.PdfDocument(str(pdfpath))[0].render(scale=2).to_pil().resize((1130,3138)).save(OUT/'board-B.png')
review={
 'woods':'B7 B10 C6 C9 E9 F9 F10 G9 L9 M8 M9 P10 Q9 Q10 R10 S10 S5 T5 V9 W8 W9 X6 X7 X9 X10 Y6 Y7 Y8 Z7 Z8 Z9 CC9 DD9 EE9 FF9'.split(),
 'marsh':[],
 'hilltop':'A4 B5 C5 D6 D7 E7 I5 I6 I7 J5 K5 L5 M4 N5 N6 O6 R4 R5 R6 R7 S4 T4 U4 V4 W4 X5 Y4 Z5 AA5 BB6 CC5 DD5 EE3 EE4 EE5 FF4 FF5 GG3 GG4 GG5'.split(),
 'slope':[],
 'places':{'volle':['L7'],'rieux':['N3','O3','P3'],'lomarre':['U6','V7'],'fratelle':['BB8']}}
coast=[(110,0),(120,70),(123,200),(127,300),(103,380),(105,450),(106,550),(83,620),(76,700),(83,790),(110,860),(79,940),(118,1020),(80,1100),(87,1200),(70,1300),(87,1400),(106,1510),(77,1580),(75,1670),(110,1750),(100,1840),(72,1900),(90,2048)]
paths=[
 ('B-road-spine','road',[(550,0),(570,45),(573,65),(552,117),(526,173),(525,204),(539,256),(566,303),(587,343),(607,387),(603,416),(580,450),(546,500),(517,550),(487,603),(460,652),(444,707),(464,754),(501,817),(539,875),(573,926),(592,959),(581,990),(549,1051),(509,1118),(465,1174),(423,1241),(405,1278),(420,1308),(444,1340),(437,1368),(419,1404),(417,1431),(449,1486),(482,1533),(511,1584),(506,1615),(490,1650),(494,1684),(506,1723),(503,1763),(491,1803),(492,1820),(518,1865),(541,1910),(533,1938),(514,1971),(510,2002),(548,2048)]),
 ('B-road-rieux','road',[(444,707),(416,750),(383,772),(300,773),(255,789),(221,829),(183,893),(162,939),(164,961),(220,965),(310,962),(352,976),(399,997),(474,1012),(591,1020),(738,1028)]),
 ('B-road-southwest','road',[(294,1767),(292,1805),(257,1824),(254,1867),(223,1884),(188,1865),(147,1890),(147,1940),(183,1964),(183,2000),(145,2025),(145,2048)]),
 ('B-stream-major','stream',[(125,320),(188,321),(248,320),(302,323),(351,321),(377,330),(389,360),(420,370),(476,382),(533,389),(555,407),(574,440),(600,450),(645,454),(665,446),(682,469),(694,510),(738,519)]),
 ('B-stream-southeast','stream',[(738,1532),(717,1526),(696,1543),(681,1573),(667,1603),(670,1634),(684,1658),(701,1670),(684,1707),(664,1734),(672,1745)])]
evidence={'status':'interpreted','sourceIds':['board-B'],'note':'First-pass manual transcription from archived Imaginative Strategist board B; requires terrain/rules review before combat admission.'}
bd=copy.deepcopy(BASE['boards'][0]);bd.update(id='B',image='board-B.png',source=str(pdfpath),sha256=SOURCES['board-B']['sha256']);bd['assembly']['evidence']['note']='Display orientation is card-specific; geographic assembly and seam rules are not admitted.'
lookup={};cells=[];w,h=1130,3138;step=113;rad=step/math.sqrt(3)
for row in range(33):
 letter=chr(65+row) if row<26 else chr(65+row-26)*2
 for col in range(11 if row%2 else 10):
  local=f'{letter}{col+1}';x=col*step+(0 if row%2 else step/2);y=3+row*step*math.sqrt(3)/2;q=col-(row+1)//2
  poly=[[x+rad*math.cos(math.radians(a)),y+rad*math.sin(math.radians(a))] for a in [-30,30,90,150,210,270]];half=any(a<0 or a>w or b<0 or b>h for a,b in poly)
  for axis,lim,g in [(0,0,True),(0,w,False),(1,0,True),(1,h,False)]:poly=clip(poly,axis,lim,g)
  places=[p for p,ids in review['places'].items() if local in ids];features=[k for k in ['hilltop','slope'] if local in review[k]]
  c={'id':f'{letter}-{col+1}','q':q,'r':row,'label':f'B-{letter}-{col+1}','terrain':{'base':'town' if places else 'marsh' if local in review['marsh'] else 'woods' if local in review['woods'] else 'open','features':features,'elevationMeters':None,'elevationLevel':2 if 'hilltop' in features else 1 if 'slope' in features else 0,'evidence':evidence},'edges':[],'placeIds':places,'playable':True,'imageCenter':{'u':round(x/w,8),'v':round(y/h,8)},'imagePolygon':[{'u':round(a/w,8),'v':round(b/h,8)} for a,b in poly],'boundaryHalfHex':half,'setupAllowed':not half};cells.append(c);lookup[q,row]=c
for c in cells:
 for direction,(dq,dr) in enumerate(DIRECTIONS):
  n=lookup.get((c['q']+dq,c['r']+dr));c['edges'].append({'direction':direction,'neighbor':{'boardId':'B','hexId':n['id']} if n else None,'barrier':'unknown','crossing':'unknown','routeIds':[],'sourceObstacle':'unknown','evidence':evidence})
bd['hexes']=cells;routes=[]
for id,kind,controls in paths:
 pts=[[max(0,min(1,x/738)),max(0,min(1,y/2048))] for x,y in curve(controls)];steps=[]
 for x,y in pts:
  c=min(cells,key=lambda c:((c['imageCenter']['u']-x)*w)**2+((c['imageCenter']['v']-y)*h)**2)
  if kind not in c['terrain']['features']:c['terrain']['features'].append(kind)
  if not steps or c['id']!=steps[-1]['hexId']:steps.append({'boardId':'B','hexId':c['id']})
 byid={c['id']:c for c in cells}
 for a,b in zip(steps,steps[1:]):
  for u,v in [(a,b),(b,a)]:
   edge=next(e for e in byid[u['hexId']]['edges'] if e['neighbor']==v);edge['routeIds'].append(id)
 routes.append({'id':id,'kind':kind,'path':steps,'condition':'unknown','evidence':evidence,'geometry':{'type':'LineString','boardId':'B','coordinateSystem':'normalized-source-image','coordinates':pts}})
places=[{'id':name,'name':name.title(),'hexes':[{'boardId':'B','hexId':c['id']} for c in cells if name in c['placeIds']],'evidence':evidence} for name in review['places']]
features=[]
for c in cells:
 if c['terrain']['base']!='town':continue
 rng=random.Random('board-B:'+c['id']);u=c['imageCenter']['u'];v=c['imageCenter']['v']
 for n in range(8):
  x=u+(rng.random()-.5)*.06;y=v+(rng.random()-.5)*.018;dx=.007;dy=.003
  features.append({'id':c['id']+f'-roof-{n}','boardId':'B','kind':'building','polygon':[[x-dx,y-dy],[x+dx,y-dy],[x+dx,y+dy],[x-dx,y+dy]],'color':'#8b95a2' if n%3 else '#a45b3b','basis':'source-guided-illustration'})
for c in cells:
 if c['id']=='H-9':
  c['terrain']['features'].append('bridge');u=c['imageCenter']['u'];v=c['imageCenter']['v'];features.append({'id':'B-bridge','boardId':'B','kind':'bridge','polygon':[[u-.008,v-.006],[u+.008,v-.006],[u+.008,v+.006],[u-.008,v+.006]],'color':'#c9bea0','basis':'source-guided-illustration'})
labels=[{'boardId':'B','text':p['name'],'position':{'Volle':[.50,.34],'Rieux':[.29,.465],'Lomarre':[.67,.625],'Fratelle':[.75,.823]}[p['name']]} for p in places]

dump(ROOT/'sources/situations/board-B-review.json',{'terrain':review,'paths':[{'id':i,'kind':k,'controlPoints':v} for i,k,v in paths],'coordinateFrame':[738,2048],'evidence':evidence})
def coastx(y):
 for (x0,y0),(x1,y1) in zip(coast,coast[1:]):
  if y0<=y<=y1:return x0+(x1-x0)*(y-y0)/(y1-y0)
 return coast[-1][0]
for h in cells:
 x=h['imageCenter']['u']*738;y=h['imageCenter']['v']*2048
 if x<coastx(y):h['terrain'].update(base='water',features=[],elevationLevel=0);h['playable']=False
 if any(p['u']*738<=coastx(p['v']*2048) for p in h['imagePolygon']):h['setupAllowed']=False
 if h['terrain']['base']!='water' and h['terrain']['elevationLevel']==0 and any(lookup.get((h['q']+dq,h['r']+dr),{}).get('terrain',{}).get('elevationLevel')==2 for dq,dr in DIRECTIONS):h['terrain']['elevationLevel']=1;h['terrain']['features'].append('slope')
features.append({'id':'B-coastal-water','boardId':'B','kind':'water','polygon':[[0,0]]+[[x/738,y/2048] for x,y in coast]+[[0,1]],'color':'#79bac5','basis':'source-guided-illustration'})
BOARDS['B']=bd
# Merge board-specific source data without inheriting another card's roster or setup.
for field,items in [('places',places),('routes',routes)]:BASE['mapModel'][field]=[x for x in BASE['mapModel'][field] if (x['hexes'][0]['boardId'] if field=='places' else x['geometry']['boardId'])!='B']+items
BASE['illustration']['features']+=features;BASE['illustration']['labels']+=labels
review['coastControlPoints']=coast;review['slopePolicy']='Adjacent land hexes to interpreted hilltops, first-pass review required.'
dump(ROOT/'sources/situations/board-B-review.json',{'terrain':review,'paths':[{'id':i,'kind':k,'controlPoints':v} for i,k,v in paths],'coordinateFrame':[738,2048],'evidence':evidence})
R5=[('uk-rifle',12),('uk-engineer',2),('uk-25pdr',3),('uk-sexton',3),('uk-achilles',2),('uk-cromwell-goodwood',4),('uk-sherman',20),('uk-typhoon-rockets',8),('uk-bren',14),('de-rifle',15),('de-75mm',3),('de-88mm',4),('de-20mm',3),('de-quad20',2),('de-81mm',2),('de-nebel',2),('de-wespe',2),('de-maultier-goodwood',1),('de-hummel',1),('de-pziv-w',2),('de-pziv',3),('de-panther-w',3),('de-panther',4),('de-tiger',3),('de-halftrack',4)]
R6=[('ca-rifle',18),('ca-mg',2),('ca-scout',3),('ca-engineer',2),('ca-6pdr',3),('ca-25pdr',3),('ca-76mortar',2),('ca-107mortar',1),('ca-truck',6),('ca-bren',4),('de-rifle',9),('de-81mm',2),('de-120mm',1),('de-75how',2),('de-truck',2),('de-halftrack',2),('de-block-reichswald',1)]
R7=[('us-rifle',9),('us-57mm',3),('us-81mm',1),('us-m7',3),('us-m5',3),('us-m4-75',6),('us-m4-76',3),('us-halftrack',13),('de-smg',6),('de-rifle',3),('de-75mm',3),('de-75ig',2),('de-81mm',1),('de-grille',2),('de-hetzer',1),('de-truck',3),('de-wagon',2)]
R12=[('us-rifle',12),('us-mg',3),('us-57mm',3),('us-105mm',1),('us-81mm',3),('us-m20',3),('us-m8',3),('us-m5',3),('us-m4-75',3),('us-halftrack',3),('us-truck',4),('de-rifle',12),('de-engineer',1),('de-20mm',2),('de-81mm',3),('de-120mm',1),('de-234-1',3),('de-234-2',1),('de-234-4',1),('de-hetzer',2),('de-stug',1),('de-lynx',1),('de-pziv',3),('de-panther',4),('de-halftrack',10),('de-truck',6)]

STREAM_CACHE={}
def stream_side(d,b,h):
 key=(b,h["id"])
 if key in STREAM_CACHE:return STREAM_CACHE[key]
 routes=[r for r in d['mapModel']['routes'] if r['kind']=='stream' and r['geometry']['boardId']==b]
 rt=max(routes,key=lambda r:max(p[0] for p in r['geometry']['coordinates'])-min(p[0] for p in r['geometry']['coordinates']))
 u=h['imageCenter']['u'];vs=[]
 for a,z in zip(rt['geometry']['coordinates'],rt['geometry']['coordinates'][1:]):
  if min(a[0],z[0])<=u<=max(a[0],z[0]) and a[0]!=z[0]:vs.append(a[1]+(u-a[0])*(z[1]-a[1])/(z[0]-a[0]))
 result=None if not vs or 'stream' in h['terrain']['features'] else 'low' if h['imageCenter']['v']<min(vs) else 'high' if h['imageCenter']['v']>max(vs) else None
 STREAM_CACHE[key]=result
 return result

registry=read(ROOT/'sources/campaign-registry.json')
normandy=next(c for c in registry['campaigns'] if c['id']=='normandy-inland-june-1944');normandy['endDate']='1944-07-18';normandy['timeBasis']='Source-card browsing window extended through Goodwood. Stable campaign ID retained; June command snapshot does not describe July forces.'
def parent(id,title,start,end):return {'id':id,'title':title,'startDate':start,'endDate':end,'commandSnapshotId':id+'-source-collection','timeBasis':'Printed source-card collection; no live headquarters snapshot or verified strength return asserted.'}
P5=parent(normandy['id'],normandy['title'],normandy['startDate'],normandy['endDate'])
P6=parent('reichswald-source-1944','Reichswald source-card study (date unverified)','1944-09-07','1944-09-07')
P7=parent('lorraine-september-1944','Lorraine advance','1944-09-14','1944-09-30')
P12=parent('saar-november-1944','Saar counterattack','1944-11-25','1944-11-25')

def build(n,slug,title,date,roster,boards,rot,axis,order,parent,num,brief,victory,special):
 d=copy.deepcopy(BASE);d.update(id=f'panzer-leader-{n:02}-{slug}',turnLimit=10,firstSide='German' if n==12 else 'Allied',setupOrder=order,briefing=brief+' Formations and date as printed, not independently verified historical strengths.',victory=victory,specialRules=special)
 d['situation'].update(number=n,title=title,printedDate=date,sourcePdfPage={5:40,6:40,7:41,12:43}[n]);d['situation'].pop('printedEndDate',None)
 d['parentCampaign']=parent;campaign=parent['id'];d['campaignAssignment']={'campaignId':campaign,'situationId':campaign+':'+slug,'number':num};d['campaign'].update(date=date,parentCampaignId=campaign)
 d.pop('parentMapLocation',None);d.pop('airSupport',None)
 d['sources']=copy.deepcopy(SOURCES);d['sources']['original-card']['page']=d['situation']['sourcePdfPage'];d['boards']=[copy.deepcopy(BOARDS[b]) for b in boards]
 for field in ['places','routes']:d['mapModel'][field]=[x for x in d['mapModel'][field] if (x['hexes'][0]['boardId'] if field=='places' else x['geometry']['boardId']) in boards]
 d['mapModel']['zones']=[];d['mapModel']['seams']=[]
 d['illustration'].update(seed=d['id']+':art-v1',joinedLayout=[{'boardId':b,'clockwiseDegrees':r} for b,r in zip(boards,rot)],northIndicators=[{'boardId':b,'clockwiseDegreesFromSheetUp':(360-r)%360} for b,r in zip(boards,rot)],layoutAxis=axis)
 for field in ['features','labels']:d['illustration'][field]=[x for x in d['illustration'][field] if x['boardId'] in boards]
 d['counters']=[];d['instances']=[];d['formations']=[]
 for id,count in roster:
  c=copy.deepcopy(TYPES[id]);c['quantity']=count;c.setdefault('kind','unit');d['counters'].append(c)
  for k in range(1,count+1):d['instances'].append({'id':f'{id}-{k:02}','counterTypeId':id,'artwork':c['artwork'],'formationId':None,'setupGroup':'Canadian' if id.startswith('ca-') else c['side'],'initialHex':None,'availability':{'kind':'at-start','turn':1,'entryZoneId':None}})
 d['totals']={s:sum(c['quantity'] for c in d['counters'] if c['side']==s) for s in ['Allied','German']};d['totals']['types']=len(d['counters'])
 texts={5:{'German':'On C, or south of the major stream on A; no Grancelles town hex.','Allied':'North of the major stream on A and B. All armored-type units start on A. Aircraft remain off-map.'},6:{'German':'On D, north of row H.','Allied':'On D, south of row I.'},7:{'German':'On D, east of row H.','Allied':'On D, west of row H.'},12:{'Allied':'On board D.','German':'On board C.'}}[n]
 allowedBoards={5:{'Allied':['A','B'],'German':['A','C']},6:{'Allied':['D'],'German':['D']},7:{'Allied':['D'],'German':['D']},12:{'Allied':['D'],'German':['C']}}[n]
 d['setupBoards']={s:bs[0] for s,bs in allowedBoards.items()};d['rules']['setup']=[{'side':s,'order':k+1,'boardIds':allowedBoards[s],'zoneIds':[],'instructions':texts[s]} for k,s in enumerate(order)];d['rules']['objectives']=[]
 allowed={};instructions={}
 armor={'uk-sexton','uk-achilles','uk-cromwell-goodwood','uk-sherman','uk-bren'}
 for i in d['instances']:
  c=next(c for c in d['counters'] if c['id']==i['counterTypeId']);side=c['side'];refs=[]
  for b in d['boards']:
   bid=b['id']
   if bid not in allowedBoards[side] or c['kind']=='aircraft':continue
   hs=[h for h in b['hexes'] if h['setupAllowed']]
   if n==5:
    if side=='Allied':
     if c['id'] in armor and bid!='A':continue
     hs=[h for h in hs if stream_side(d,bid,h)=='high']
    elif bid=='A':hs=[h for h in hs if stream_side(d,bid,h)=='low' and 'grancelles-all' not in h['placeIds']]
   if n==6:hs=[h for h in hs if h['r']>7] if side=='German' else [h for h in hs if h['r']<8]
   if n==7:hs=[h for h in hs if h['r']>7] if side=='German' else [h for h in hs if h['r']<7]
   refs += [{'boardId':bid,'hexId':h['id']} for h in hs]
  assert refs or c['kind']=='aircraft',(n,i['id']);allowed[i['id']]=refs;instructions[i['id']]=texts[side]
 d['deployment']={'allowedHexes':allowed,'instructions':instructions,'loads':[],'evidence':'Printed setup applied to card-oriented source coordinates. Boundary fragments, water and stream hexes excluded as applicable. Goodwood stream half-planes and armored classification require rules review before combat admission.'}
 if n==5:d['airSupport']={'status':'reference-only','instructions':'Eight rocket-armed Typhoons; no additional card entry delay. No more than five aircraft may be on the map at once. This is an aircraft-count cap, not a flight-count cap.','maxConcurrentAircraft':5,'maxConcurrentFighterFlights':None,'allowFlightMixing':None,'allowRepeatRuns':None,'groups':[{'id':'goodwood-typhoon-roster','role':'fighter-bomber','instanceIds':[i['id'] for i in d['instances'] if i['counterTypeId']=='uk-typhoon-rockets'],'availableFromTurn':1}]}
 if n==12:d['mapModel']['zones']=[{'id':'saar-west-exits','description':'Printed German exits on west edge of A: P-1, Q-1, R-1, S-1.','hexes':[{'boardId':'A','hexId':h} for h in ['P-1','Q-1','R-1','S-1']],'resolution':'unresolved'}]
 d['sourceRules']={'status':'transcribed-reference','objectives':victory,'specialRules':special,'execution':'Setup and game-map inspection only. Combat, movement, air operations, exit scoring and victory adjudication are not implemented.'}
 d['admission']['requiredCapabilities']=['restricted-setup','formation-combat','graded-victory']+(['air-support','concurrent-aircraft-limit'] if n==5 else ['exit-scoring'] if n==12 else [])
 d['admission']['blockers']=['Interpreted terrain, setup zones and board joins require review before combat admission','Formation combat, source scoring and persistent force decomposition unresolved']
 if n==5:d['admission']['blockers']+=['Board B first-pass coastal terrain requires review','Cromwell and Maultier retain original-card artwork exceptions','Loss-ratio zero denominator and aircraft limit require execution rules']
 if n==6:d['admission']['blockers']+=['Printed 7 September 1944 Reichswald attribution remains historically unverified; do not substitute a 1945 date','Overlapping victory grades require precedence review']
 if n==12:d['admission']['blockers'].append('Overlapping elimination and exit victory grades require precedence review')
 im=pdf.PdfDocument(d['situation']['sourcePdf'])[d['situation']['sourcePdfPage']-1].render(scale=2).to_pil();half=1 if n in [6,12] else 0;im.crop((0,half*im.height//2,im.width,(half+1)*im.height//2)).save(OUT/f'card-{n:02}.png');d['sourceCardImage']=f'card-{n:02}.png'
 dump(ROOT/f'sources/situations/panzer-leader-{n:02}.json',d);return d
cards=[
 build(5,'goodwood','Operation Goodwood','1944-07-18',R5,['B','A','C'],[90,90,90],'vertical',['German','Allied'],P5,2,'British 11th and Guards Armoured Divisions encounter remnants of German 1st SS and 21st Panzer Divisions.', ['Allied victory: control every town hex and bridge hex on A at game end, with Allied units eliminated divided by German units eliminated strictly less than 3:2.','German victory: prevent Allied victory. Zero-denominator handling remains unresolved.'],'Use US M4/75 and M4/76 counters to fill British Shermans, using British values; use M3 halftracks to fill Bren counters. Digital setup supplies the full matching British roster. No more than five aircraft on the map at any time.'),
 build(6,'reichswald','The Reichswald','1944-09-07',R6,['D'],[180],'horizontal',['German','Allied'],P6,1,'Elements of 2nd Canadian Infantry Division attack German 84th Infantry Division near Reichswald. Historical date and attribution require review; the printed date is retained.', ['Allied victory requires all four Nece town hexes and no more than 10 Allied losses: 0-3 decisive, 4-6 tactical, 7-10 marginal.','German victory: hold at least one Nece town hex for decisive victory, or eliminate more than 10 Allied units for tactical victory. Overlapping conditions require precedence review.'],'None printed.'),
 build(7,'nancy','Encirclement of Nancy','1944-09-14',R7,['D','A'],[270,90],'horizontal',['German','Allied'],P7,2,'US 4th Armored Division attacks German 553rd Volksgrenadier Division.', ['Allied victory: control all Grancelles town hexes at game end.','German victory: control at least one Grancelles town hex and lose fewer than 12 German combat units.','Any other result is a draw.'],'None printed.'),
 build(12,'saar','Prelude: The Saar','1944-11-25',R12,['A','D','C'],[0,0,0],'horizontal',['Allied','German'],P12,1,'Panzer Lehr attacks elements of US 106th Cavalry Group and 114th Infantry Regiment.', ['Fewer than 10 German units exited, or more than 15 German units eliminated: decisive Allied victory.','10-20 German units exited: tactical Allied; 21-25: marginal Allied; 26-30: marginal German; 31-35: tactical German.','More than 35 German units exited, or more than 18 Allied combat units eliminated: decisive German victory. Conflicting exit/elimination grades require precedence review.'],'German units may exit west edge of board A at P-1, Q-1, R-1 and S-1 during any friendly movement phase.')]
for d in cards:
 c=next((c for c in registry['campaigns'] if c['id']==d['parentCampaign']['id']),None)
 if c is None:c={**d['parentCampaign'],'situations':[]};registry['campaigns'].append(c)
 a=d['campaignAssignment'];c['situations']=[x for x in c['situations'] if x['sourcePackageId']!=d['id']]+[{'id':a['situationId'],'number':a['number'],'title':d['situation']['title'],'sourcePackageId':d['id']}];c['situations'].sort(key=lambda x:x['number']);c['nextSituationNumber']=max(x['number'] for x in c['situations'])+1
# Keep the existing St. Lo parent browsing window consistent with its extended registry.
stlo=read(ROOT/'sources/situations/panzer-leader-04.json');stlo['parentCampaign']['endDate']=P5['endDate'];dump(ROOT/'sources/situations/panzer-leader-04.json',stlo)
dump(ROOT/'sources/campaign-registry.json',registry)
print([(d['situation']['number'],d['totals']) for d in cards])
