"""Reproduce first Western Front batch from archived source cards and IS sheets.
Requires Pillow, pypdfium2, jsonschema. Does not admit formation combat.
"""
import copy, hashlib, json, math, random, shutil
from pathlib import Path
import pypdfium2 as pdf
from PIL import Image
from transcribe_panzer_maps import clip,DIRECTIONS
from compile_panzer_lines import curve
ROOT=Path(__file__).resolve().parent
ARCHIVE=Path(r'E:\Archive\WWII-Docs\1-PanzerBlitz-PanzerLeader\Original-Rules-and-Situations')
BASE=json.loads((ROOT/'sources/situations/panzer-leader-04.json').read_text(encoding='utf-8'))
OUT=ROOT/'assets/panzer-shared';OUT.mkdir(exist_ok=True)
SOURCES=copy.deepcopy(BASE['sources'])
def dump(p,d):p.write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8',newline='\n')
def source(key,path):
 SOURCES[key]={'path':str(path),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'page':1}
 return key
SHEETS={}
def render(path,scale=2):
 doc=pdf.PdfDocument(str(path));return doc[0].render(scale=scale).to_pil().convert('RGB')
# Existing verified counter crops remain shared, byte-for-byte.
TYPES={c['id']:copy.deepcopy(c) for c in BASE['counters']}
(OUT/'counters').mkdir(exist_ok=True)
for c in TYPES.values():shutil.copyfile(ROOT/'assets/panzer-leader-04'/c['artwork'],OUT/c['artwork'])
def token(id,label,side,factors,sheet,counter,x,y):
 path=ARCHIVE/('Counters' if sheet.startswith('PB') else 'PanzerLeader/Counters')/(sheet+' Colour.pdf')
 if sheet not in SHEETS:SHEETS[sheet]=render(path,3)
 key=source(sheet,path);im=SHEETS[sheet];im.crop((x*3,y*3,(x+45)*3,(y+45)*3)).save(OUT/f'counters/{id}.png')
 c=copy.deepcopy(BASE['counters'][0]);c.update(id=id,label=label,side=side,quantity=1,factors=dict(zip(['attack','weaponType','range','defense','movement'],factors)),artwork=f'counters/{id}.png',sourceSheet=key,sourceCounterId=counter,sourceCropPointsTopLeft=[x,y,45,45],match='numeric-factors-and-weapon-type-verified',notes=['Matched visually to archived Imaginative Strategist sheet; printed card factors retained. Artwork annotations do not automatically enable optional rules.'])
 TYPES[id]=c
# Crop coordinates are PDF points measured from the sheet's top left.
for args in [
 ('us-armored-rifle','Armored infantry','Allied',[4,'I',2,10,1],'PL American 2','1F01',0,495),
 ('us-engineer','Engineer','Allied',[1,'I',1,6,1],'PL American 1','1A01',495,360),
 ('us-m7','M7 Priest','Allied',[40,'(H)',32,7,8],'PL American 4','6B01',405,0),
 ('us-40mm','40 mm AA','Allied',[8,'H',12,2,0],'PL American 1','E01',0,180),
 ('us-m10','M10 tank destroyer','Allied',[14,'A',10,6,9],'PL American 4','8A01',0,360),
 ('de-smg','SMG','German',[6,'I',1,6,1],'PL German 1','1D01',45,630),
 ('de-88mm','88 mm','German',[20,'A',20,1,0],'PB German 1','E01',135,135),
 ('de-37mm','37 mm AA','German',[6,'H',12,1,0],'PB German 1','H01',180,270),
 ('de-234-1','SdKfz 234/1','German',[2,'H',4,3,16],'PB German 4','5F01',405,0),
 ('de-234-2','SdKfz 234/2 Puma','German',[6,'A',5,3,14],'PB German 4','5G01',0,180),
 ('de-234-4','SdKfz 234/4','German',[13,'A',8,3,14],'PB German 4','5I01',90,180),
 ('de-lynx','Lynx','German',[2,'A',4,6,10],'PB German 6','9B01',180,0),
 ('de-wirbelwind','Wirbelwind','German',[14,'H',10,6,8],'PB German 4','7B01',45,360),
 ('de-pziv','Panzer IV H','German',[14,'A',8,8,8],'PB German 6','9I01',315,360),
 ('de-panther','Panther','German',[16,'A',12,12,10],'PB German 6','9J01',45,495),
 ('de-tiger','Tiger I','German',[15,'A',12,12,8],'PB German 6','9K01',405,495),
 ('de-kingtiger','King Tiger','German',[20,'A',12,16,6],'PB German 6','9L01',0,630),
]:token(*args)
# Board D: source-aligned, explicitly interpreted terrain, same admission as A/C.
pdfpath=ARCHIVE/'PanzerLeader/MapBoards/PL Map D Full.pdf';source('board-D',pdfpath)
render(pdfpath).resize((1130,3138)).save(OUT/'board-D.png')
for bid in ['A','C']:shutil.copyfile(ROOT/f'assets/panzer-leader-04/board-{bid}.png',OUT/f'board-{bid}.png')
review={
 'woods':'C5 D5 E4 E5 F3 F4 I2 J2 J3 K2 K3 L2 L3 M3 M8 M9 N3 N8 N9 T7 U6 U7 U8 V5 V6 W1 W2 W5 X3 Z9 AA2 AA9 BB8 CC8 EE2 FF2 FF3 FF4'.split(),
 'marsh':'C1 D1 D2 E2'.split(),
 'hilltop':'I4 I5 I6 O6 P6 Y6 Y7 Z4 Z5'.split(),
 'slope':'H2 H3 H4 H5 H6 H7 I3 I7 I8 J3 J4 J5 J6 N5 N6 N7 O5 O7 P5 P7 Q5 Q6 X4 X5 X6 X7 X8 Y4 Y5 Y8 Z3 Z6 Z7 AA4 AA5 EE7 EE8 FF7 FF8'.split(),
 'places':{'einkel':['D3'],'merden':['C7','D8'],'nece':['S3','T3','T4','T5'],'artain':['CC5','CC6','DD6']}}
# Control points measured on the source preview (738 x 2048), not geographic coordinates.
paths=[('D-road-main','road',[(185,0),(161,50),(166,112),(177,146),(159,183),(174,216),(177,238),(158,263)]),
 ('D-road-spine','road',[(561,0),(519,58),(487,113),(508,179),(478,244),(420,337),(374,410),(337,440),(230,442),(200,479),(207,535),(251,574),(307,578),(327,615),(301,665),(260,698),(216,704),(178,752),(151,830),(170,890),(158,951),(171,982),(221,1039),(207,1100),(190,1156),(217,1210),(190,1280),(226,1361),(277,1467),(318,1538),(350,1603),(324,1669),(355,1740),(388,1795),(382,1830),(373,1860),(408,1898),(464,1918),(498,1967),(522,2048)]),
 ('D-road-cross','road',[(0,1016),(25,1050),(51,1110),(86,1166),(114,1213),(187,1214),(294,1208),(410,1211),(487,1205),(528,1176),(568,1154),(627,1125),(659,1086),(711,1022),(738,1021)]),
 ('D-road-artain-east','road',[(183,2048),(201,2015),(205,1961),(241,1880),(253,1820),(284,1784),(318,1779),(352,1800),(370,1830)]),
 ('D-stream-north','stream',[(0,508),(31,509),(48,477),(50,459),(80,429),(99,393),(96,361),(71,315),(53,270),(49,237),(62,212),(92,191)]),
 ('D-stream-east','stream',[(738,509),(672,510),(643,524),(620,551),(600,585),(569,617),(551,638),(504,644),(472,659),(453,681),(447,701),(457,718)]),
 ('D-stream-south','stream',[(0,1532),(67,1530),(97,1535),(122,1554),(143,1585),(154,1627),(183,1669),(218,1704),(252,1720),(311,1722),(362,1720),(420,1720),(451,1702),(482,1663),(530,1658),(572,1640),(599,1606),(627,1590),(665,1588),(679,1570),(690,1538),(709,1528),(738,1535)])]
evidence={'status':'interpreted','sourceIds':['board-D'],'note':'First-pass manual transcription from archived Imaginative Strategist board D; requires terrain/rules review before combat admission.'}
bd=copy.deepcopy(BASE['boards'][0]);bd.update(id='D',image='board-D.png',source=str(pdfpath),sha256=SOURCES['board-D']['sha256']);bd['assembly']['evidence']['note']='Display orientation is card-specific; geographic assembly and seam rules are not admitted.'
lookup={};cells=[];w,h=1130,3138;step=113;rad=step/math.sqrt(3)
for row in range(33):
 letter=chr(65+row) if row<26 else chr(65+row-26)*2
 for col in range(11 if row%2 else 10):
  local=f'{letter}{col+1}';x=col*step+(0 if row%2 else step/2);y=3+row*step*math.sqrt(3)/2;q=col-(row+1)//2
  poly=[[x+rad*math.cos(math.radians(a)),y+rad*math.sin(math.radians(a))] for a in [-30,30,90,150,210,270]];half=any(a<0 or a>w or b<0 or b>h for a,b in poly)
  for axis,lim,g in [(0,0,True),(0,w,False),(1,0,True),(1,h,False)]:poly=clip(poly,axis,lim,g)
  places=[p for p,ids in review['places'].items() if local in ids];features=[k for k in ['hilltop','slope'] if local in review[k]]
  c={'id':f'{letter}-{col+1}','q':q,'r':row,'label':f'D-{letter}-{col+1}','terrain':{'base':'town' if places else 'marsh' if local in review['marsh'] else 'woods' if local in review['woods'] else 'open','features':features,'elevationMeters':None,'elevationLevel':2 if 'hilltop' in features else 1 if 'slope' in features else 0,'evidence':evidence},'edges':[],'placeIds':places,'playable':True,'imageCenter':{'u':round(x/w,8),'v':round(y/h,8)},'imagePolygon':[{'u':round(a/w,8),'v':round(b/h,8)} for a,b in poly],'boundaryHalfHex':half,'setupAllowed':not half};cells.append(c);lookup[q,row]=c
for c in cells:
 for direction,(dq,dr) in enumerate(DIRECTIONS):
  n=lookup.get((c['q']+dq,c['r']+dr));c['edges'].append({'direction':direction,'neighbor':{'boardId':'D','hexId':n['id']} if n else None,'barrier':'unknown','crossing':'unknown','routeIds':[],'sourceObstacle':'unknown','evidence':evidence})
bd['hexes']=cells;routes=[]
for id,kind,controls in paths:
 pts=[[max(0,min(1,x/738)),max(0,min(1,y/2048))] for x,y in curve(controls)];steps=[]
 for x,y in pts:
  c=min(cells,key=lambda c:((c['imageCenter']['u']-x)*w)**2+((c['imageCenter']['v']-y)*h)**2)
  if kind not in c['terrain']['features']:c['terrain']['features'].append(kind)
  if not steps or c['id']!=steps[-1]['hexId']:steps.append({'boardId':'D','hexId':c['id']})
 byid={c['id']:c for c in cells}
 for a,b in zip(steps,steps[1:]):
  for u,v in [(a,b),(b,a)]:
   edge=next(e for e in byid[u['hexId']]['edges'] if e['neighbor']==v);edge['routeIds'].append(id)
 routes.append({'id':id,'kind':kind,'path':steps,'condition':'unknown','evidence':evidence,'geometry':{'type':'LineString','boardId':'D','coordinateSystem':'normalized-source-image','coordinates':pts}})
places=[{'id':name,'name':name.title(),'hexes':[{'boardId':'D','hexId':c['id']} for c in cells if name in c['placeIds']],'evidence':evidence} for name in review['places']]
features=[]
for c in cells:
 if c['terrain']['base']!='town':continue
 rng=random.Random('board-D:'+c['id']);u=c['imageCenter']['u'];v=c['imageCenter']['v']
 for n in range(8):
  x=u+(rng.random()-.5)*.06;y=v+(rng.random()-.5)*.018;dx=.007;dy=.003
  features.append({'id':c['id']+f'-roof-{n}','boardId':'D','kind':'building','polygon':[[x-dx,y-dy],[x+dx,y-dy],[x+dx,y+dy],[x-dx,y+dy]],'color':'#8b95a2' if n%3 else '#a45b3b','basis':'source-guided-illustration'})
for c in cells:
 if c['id']=='BB-6':
  c['terrain']['features'].append('bridge');u=c['imageCenter']['u'];v=c['imageCenter']['v'];features.append({'id':'D-bridge','boardId':'D','kind':'bridge','polygon':[[u-.008,v-.006],[u+.008,v-.006],[u+.008,v+.006],[u-.008,v+.006]],'color':'#c9bea0','basis':'source-guided-illustration'})
labels=[{'boardId':'D','text':p['name'],'position':[{'Einkel':.15,'Merden':.74,'Nece':.34,'Artain':.59}[p['name']],{'Einkel':.07,'Merden':.07,'Nece':.56,'Artain':.905}[p['name']]]} for p in places]
dump(ROOT/'sources/situations/board-D-review.json',{'terrain':review,'paths':[{'id':i,'kind':k,'controlPoints':v} for i,k,v in paths],'coordinateFrame':[738,2048],'evidence':evidence})
def dist(a,b):return max(abs(a['q']-b['q']),abs(a['r']-b['r']),abs((a['q']+a['r'])-(b['q']+b['r'])))
def build(n,slug,title,date,boards,rotations,order,roster,brief,victory,special,campaign,num):
 d=copy.deepcopy(BASE);d.update(id=f'panzer-leader-{n:02}-{slug}',assetBase='assets/panzer-shared/',turnLimit=10,firstSide='German' if n==14 else 'Allied',setupOrder=order,briefing=brief+' Attribution as printed on the source card; not an independently verified strength return.',victory=victory,specialRules=special,parentCampaign=campaign,campaignAssignment={'campaignId':campaign['id'],'situationId':campaign['id']+':'+slug,'number':num})
 d['situation'].update(number=n,title=title,printedDate=date,sourcePdfPage={8:41,14:44,16:45}[n]);d['campaign'].update(date=date,parentCampaignId=campaign['id']);d.pop('parentMapLocation',None)
 d['boards']=[copy.deepcopy(bd if b=='D' else next(x for x in BASE['boards'] if x['id']==b)) for b in boards]
 d['sources']=copy.deepcopy(SOURCES);d['mapModel']['zones']=[];d['mapModel']['places']=[copy.deepcopy(p) for p in BASE['mapModel']['places'] if p['hexes'][0]['boardId'] in boards]+(copy.deepcopy(places) if 'D' in boards else []);d['mapModel']['routes']=[copy.deepcopy(p) for p in BASE['mapModel']['routes'] if p['geometry']['boardId'] in boards]+(copy.deepcopy(routes) if 'D' in boards else []);d['mapModel']['seams']=[]
 d['illustration'].update(seed=d['id']+':art-v1',features=[copy.deepcopy(f) for f in BASE['illustration']['features'] if f['boardId'] in boards]+copy.deepcopy(features),labels=[copy.deepcopy(f) for f in BASE['illustration']['labels'] if f['boardId'] in boards]+copy.deepcopy(labels),joinedLayout=[{'boardId':b,'clockwiseDegrees':a} for b,a in zip(boards,rotations)],northIndicators=[{'boardId':b,'clockwiseDegreesFromSheetUp':(360-a)%360} for b,a in zip(boards,rotations)],layoutAxis='vertical')
 d['counters']=[];d['instances']=[];d['formations']=[];groups={}
 for id,count,group in roster:
  if not any(c['id']==id for c in d['counters']):c=copy.deepcopy(TYPES[id]);c['quantity']=0;d['counters'].append(c)
  c=next(c for c in d['counters'] if c['id']==id)
  for j in range(count):
   iid=id+f'-{c["quantity"]+1:02}';d['instances'].append({'id':iid,'counterTypeId':id,'artwork':c['artwork'],'formationId':None,'initialHex':None,'availability':{'kind':'at-start','turn':1,'entryZoneId':None}});groups[iid]=group;c['quantity']+=1
 d['totals']={side:sum(c['quantity'] for c in d['counters'] if c['side']==side) for side in ['Allied','German']};d['totals']['types']=len(d['counters'])
 d['setupBoards']={'German':'A' if n==8 else 'D','Allied':'D' if n in [8,16] else 'C'}
 texts={8:{'German':'Set up first in Grancelles on board A.','Allied':'Set up second anywhere on board D.'},14:{'Allied':'Group A within two hexes of Wiln; Group B in St. Athan, on C.','German':'Set up second on D, east of hex row P.'},16:{'German':'On roads east of the north-south stream, at least one hex away from Artain.','Allied':'On the road running west from Artain; mortars loaded in trucks.'}}[n]
 d['rules']['setup']=[{'side':s,'order':j+1,'boardIds':[d['setupBoards'][s]],'zoneIds':[],'instructions':texts[s]} for j,s in enumerate(order)];d['rules']['objectives']=[]
 allowed={};reason={}
 for i in d['instances']:
  side=next(c['side'] for c in d['counters'] if c['id']==i['counterTypeId']);b=next(b for b in d['boards'] if b['id']==d['setupBoards'][side]);hs=[h for h in b['hexes'] if h['setupAllowed']]
  if n==8 and side=='German':hs=[h for h in hs if 'grancelles-all' in h['placeIds']]
  if n==14 and side=='Allied':
   town='wiln' if groups[i['id']]=='A' else 'st-athan';centers=[h for h in b['hexes'] if town in h['placeIds']];hs=[h for h in hs if min(dist(h,t) for t in centers)<= (2 if town=='wiln' else 0)]
  if n==14 and side=='German':hs=[h for h in hs if h['r']>15]
  if n==16:
   town=[h for h in b['hexes'] if 'artain' in h['placeIds']]
   if side=='Allied':ids={ref['hexId'] for rt in routes if rt['id']=='D-road-spine' for ref in rt['path']};hs=[h for h in hs if h['id'] in ids and h['r']<28]
   else:hs=[h for h in hs if 'road' in h['terrain']['features'] and h['r']>=28 and min(dist(h,t) for t in town)>=1]
  assert hs,(n,i['id']);allowed[i['id']]=[{'boardId':b['id'],'hexId':h['id']} for h in hs];reason[i['id']]=texts[side]+(' Group '+groups[i['id']]+'.' if groups[i['id']] else '')
 d['deployment']={'allowedHexes':allowed,'instructions':reason,'loads':[{'passengerId':f'us-81mm-{k:02}','carrierId':f'us-truck-{k:02}'} for k in [1,2]] if n==16 else [],'evidence':'Source-card setup restrictions applied to interpreted board geometry. Artain town hexes are excluded from German setup; review required before combat admission.'}
 if n in [8,16]:
  place=next(p for p in d['mapModel']['places'] if p['id']==('grancelles-all' if n==8 else 'artain'))
  d['mapModel']['zones']=[{'id':place['id'],'description':place['name'],'hexes':place['hexes'],'resolution':'unresolved'}]
  pred={'op':'control-all' if n==8 else 'control-at-least','side':'Allied','zoneId':place['id']}
  if n==16:pred['count']=2
  d['rules']['objectives']=[{'id':'allied-control','evaluateAt':'end-of-game','predicate':pred}]
 else:d['rules']['objectives']=[{'id':'german-exit','evaluateAt':'end-of-game','predicate':{'op':'exit-at-least','side':'German','edge':'west','count':15,'combatOnly':True}}]
 d['sourceRules']={'status':'transcribed-reference','objectives':victory,'specialRules':special,'execution':'Not executable: formation combat and victory adjudication are not implemented.'}
 d['admission']['requiredCapabilities']+=['restricted-setup']+(['transport-loading'] if n==16 else ['bridge-demolition','turn-deadlines'] if n==14 else ['graded-victory'])
 d['admission']['blockers']=['Interpreted terrain and source setup zones require review before combat admission','Legacy victory conditions preserved as structured source clauses; adjudication engine not connected','Dated force decomposition unresolved','Formation combat and tactical result reconciliation not connected']
 doc=pdf.PdfDocument(d['situation']['sourcePdf']);im=doc[d['situation']['sourcePdfPage']-1].render(scale=1.5).to_pil();im.crop((0,im.height//2,im.width,im.height)).save(OUT/f'card-{n:02}.png');d['sourceCardImage']=f'card-{n:02}.png'
 dump(ROOT/f'sources/situations/panzer-leader-{n:02}.json',d);return d
lor={'id':'lorraine-september-1944','title':'Lorraine advance','startDate':'1944-09-14','endDate':'1944-09-30','commandSnapshotId':'lorraine-situations-1944','timeBasis':'Dated legacy Situation collection; no live headquarters snapshot or geographic force deployment asserted.'}
ard={'id':'ardennes-december-1944','title':'Ardennes operations','startDate':'1944-12-16','endDate':'1945-01-01','commandSnapshotId':'ardennes-situations-1944','timeBasis':lor['timeBasis']}
def roster(xs):return [(k,v,None) for k,v in xs]
a=build(8,'marieulles','Marieulles','1944-09-16',['A','D'],[90,270],['German','Allied'],roster([('us-armored-rifle',9),('us-57mm',3),('us-81mm',1),('us-m7',1),('us-m4-75',3),('us-halftrack',13),('de-rifle',6),('de-smg',3),('de-88mm',3),('de-truck',3)]),'Elements of the U.S. 7th Armored Division engage German officer trainees at Marieulles.', ['All Grancelles town hexes controlled by Allies at end of turn 5: tactical Allied victory; turn 6: marginal Allied victory; turn 7: draw; turn 10: marginal German victory.','If Allies do not control all Grancelles town hexes at end of turn 10: tactical German victory.','Winning side improves its victory grade by one step if it lost less than 50 percent of its combat units. Intermediate-turn interpretation requires adjudication review.'],'None on the original card.',lor,1)
b=build(16,'bastogne-prelude','Bastogne: Prelude','1944-12-19',['D'],[270],['German','Allied'],roster([('us-rifle',18),('us-81mm',2),('us-m7',2),('us-m5',3),('us-truck',2),('de-rifle',15),('de-engineer',1),('de-81mm',1),('de-234-1',3),('de-234-2',1),('de-234-4',1),('de-lynx',1),('de-stug',1)]),'Elements of the U.S. 101st Airborne Division and supporting units engage Panzer Lehr units probing east of Bastogne.',['Allied: control at least two town hexes in Artain at the end of the game.','German: prevent the Allied victory condition.'],'Allied mortar units must be loaded in trucks at the start. The setup planner pairs each mortar with one of the two trucks; moving a truck also moves its loaded mortar.',ard,2)
c=build(14,'bulge-thrust','Bulge: Thrust','1944-12-18',['C','D'],[90,270],['Allied','German'],[(k,v,'A') for k,v in [('us-armored-rifle',3),('us-engineer',3),('us-57mm',1),('us-40mm',1),('us-m10',2),('us-halftrack',1),('us-truck',5)]]+[(k,v,'B') for k,v in [('us-engineer',3),('us-57mm',2),('us-truck',3)]]+roster([('de-rifle',9),('de-engineer',3),('de-88mm',2),('de-37mm',1),('de-wirbelwind',1),('de-pziv',6),('de-panther',6),('de-tiger',1),('de-kingtiger',1),('de-halftrack',10),('de-truck',6)]),'Kampfgruppe Peiper assaults mixed engineer and support units at Stavelot and Trois Ponts.',['German: exit 15 combat units off the west map edge before the end of the game.','Allied: prevent the German victory condition.'],'Allied engineers may attempt bridge demolition only when German units are within five hexes of that bridge. All German units must enter C east of row R before the end of turn two or be eliminated; once on C they cannot re-enter D. All Allied units must remain on C. These turn-execution rules are recorded, not simulated by the setup planner.',ard,1)
registry=json.loads((ROOT/'sources/campaign-registry.json').read_text(encoding='utf-8'))
for campaign,cards in [(lor,[a]),(ard,[c,b])]:
 entry=next((x for x in registry['campaigns'] if x['id']==campaign['id']),None)
 if entry is None:entry={**campaign,'situations':[]};registry['campaigns'].append(entry)
 for d in cards:
  entry['situations']=[x for x in entry['situations'] if x['sourcePackageId']!=d['id']]+[{'id':d['campaignAssignment']['situationId'],'number':d['campaignAssignment']['number'],'title':d['situation']['title'],'sourcePackageId':d['id']}]
 entry['situations'].sort(key=lambda x:x['number']);entry['nextSituationNumber']=max(x['number'] for x in entry['situations'])+1
dump(ROOT/'sources/campaign-registry.json',registry)
print('Imported',[(d['situation']['number'],d['totals']) for d in [a,c,b]])
