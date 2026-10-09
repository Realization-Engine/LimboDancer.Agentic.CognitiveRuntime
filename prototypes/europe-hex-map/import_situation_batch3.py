"""Reproduce PL18/19/20 from original scanned cards. Reference-only, not combat admission."""
import copy,hashlib,json
from pathlib import Path
import pypdfium2 as pdf
ROOT=Path(__file__).resolve().parent
ARCHIVE=Path(r'E:\Archive\WWII-Docs\1-PanzerBlitz-PanzerLeader\Original-Rules-and-Situations')
OUT=ROOT/'assets/panzer-shared'
def read(p):return json.loads(p.read_text(encoding='utf-8'))
def dump(p,d):p.write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8',newline='\n')
TYPES={};SOURCES={};BOARDS={};SHEETS={}
for n in [4,8,13,14,15,16,17]:
 d=read(ROOT/f'sources/situations/panzer-leader-{n:02}.json');SOURCES.update(d['sources']);TYPES.update({c['id']:copy.deepcopy(c) for c in d['counters']});BOARDS.update({b['id']:copy.deepcopy(b) for b in d['boards']})
BASE=d

def token(id,label,f,sheet,counter,x,y,kind='unit'):
 path=ARCHIVE/'PanzerLeader/Counters'/(sheet+' Colour.pdf');SOURCES[sheet]={'path':str(path),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'page':1}
 if sheet not in SHEETS:SHEETS[sheet]=pdf.PdfDocument(str(path))[0].render(scale=3).to_pil()
 SHEETS[sheet].crop((x*3,y*3,(x+45)*3,(y+45)*3)).save(OUT/f'counters/{id}.png')
 c=copy.deepcopy(TYPES['us-rifle']);c.update(id=id,label=label,kind=kind,nationality=id.split('-')[0],side='German' if id.startswith('de-') else 'Allied',factors=dict(zip(['attack','weaponType','range','defense','movement'],f)) if f else None,artwork=f'counters/{id}.png',sourceSheet=sheet,sourceCounterId=counter,sourceCropPointsTopLeft=[x,y,45,45],match='source-type-verified' if kind=='aircraft' else 'numeric-factors-and-weapon-type-verified',notes=['Matched visually to archived Imaginative Strategist sheet. Aircraft artwork factors are source annotations, not an implemented air combat profile.' if kind=='aircraft' else 'Printed factors matched to archived Imaginative Strategist artwork.'])
 TYPES[id]=c
for args in [
 ('us-m18','M18 Hellcat',[14,'A',10,4,12],'PL American 4','8B01',135,360),
 ('us-m36','M36 tank destroyer',[15,'A',12,6,9],'PL American 4','8C01',360,360),
 ('us-m24','M24 Chaffee',[11,'A',8,7,11],'PL American 5','9B01',315,0),
 ('us-90mm','90 mm AT',[15,'A',20,1,0],'PL American 1','C01',495,0),
 ('de-170how','170 mm howitzer',[50,'(H)',80,2,0],'PL German 1','N01',405,0),
 ('de-security','Security infantry',[2,'I',2,5,1],'PL German 1','1B01',315,135),
 ('us-l5','L-5 observation aircraft',None,'PL American 6','L-5',405,630,'aircraft'),
 ('us-p47-bombs','P-47 fighter-bomber (bombs)',None,'PL American 5','P-47 Bombs 01',0,540,'aircraft'),
 ('us-p47-rockets','P-47 fighter-bomber (rockets)',None,'PL American 5','P-47 Rockets 01',360,540,'aircraft'),
]:token(*args)
block=copy.deepcopy(TYPES['us-block']);block.update(id='de-block',side='German',nationality='de',sourceCounterId='PL20-X',notes=['One German X block marker on Situation 20; original unit-function table identifies X as Blocks. Authored symbol, not a claimed Imaginative Strategist crop. Must start on the bridge hex.']);TYPES['de-block']=block
R18=[('us-rifle',18),('us-scout',3),('us-155mm',1),('us-81mm',2),('us-m20',3),('us-m8',3),('us-m7',3),('us-m4-105',2),('us-m10',2),('us-m18',2),('us-m5',3),('us-halftrack',3),('us-truck',7),('us-l5',1),('us-p47-bombs',5),('us-p47-rockets',5),('de-smg',9),('de-rifle',9),('de-20mm',4),('de-75how',2),('de-105how',2),('de-81mm',2),('de-120mm',2),('de-234-1',3),('de-234-2',1),('de-234-4',1),('de-wirbelwind',1),('de-hetzer',2),('de-lynx',1),('de-pziv',5),('de-truck',6),('de-wagon',6)]
R19=[('us-armored-rifle',9),('us-rifle',18),('us-mg',3),('us-engineer',3),('us-57mm',3),('us-90mm',2),('us-40mm',2),('us-105mm',6),('us-155mm',2),('us-8inch',1),('us-81mm',3),('us-m20',3),('us-m8',3),('us-m7',3),('us-m4-105',2),('us-m5',3),('us-m4-75',9),('us-m4-76',9),('us-halftrack',17),('us-truck',6),('us-l5',1),('us-p47-bombs',5),('de-smg',6),('de-rifle',12),('de-engineer',1),('de-75mm',3),('de-88mm',3),('de-20mm',4),('de-quad20',2),('de-75how',3),('de-150how',1),('de-170how',2),('de-81mm',8),('de-120mm',2),('de-pziv',5),('de-panther',5),('de-halftrack',10),('de-truck',6),('de-wagon',6)]
R20=[('us-armored-rifle',3),('us-engineer',2),('us-m10',2),('us-m36',1),('us-m24',1),('us-halftrack',5),('de-rifle',3),('de-security',1),('de-engineer',1),('de-20mm',1),('de-81mm',1),('de-halftrack',2),('de-wagon',1),('de-block',1)]
def dist(a,b):return max(abs(a['q']-b['q']),abs(a['r']-b['r']),abs(a['q']+a['r']-b['q']-b['r']))
def river_v(d,h):
 rt=next(r for r in d['mapModel']['routes'] if r['id']=='D-stream-south');u=h['imageCenter']['u'];vs=[]
 for a,b in zip(rt['geometry']['coordinates'],rt['geometry']['coordinates'][1:]):
  if min(a[0],b[0])<=u<=max(a[0],b[0]) and a[0]!=b[0]:vs.append(a[1]+(u-a[0])*(b[1]-a[1])/(b[0]-a[0]))
 return vs

def build(n,slug,title,date,roster,boards,rotations,order,victory,special,brief):
 d=copy.deepcopy(BASE);d.update(id=f'panzer-leader-{n:02}-{slug}',turnLimit={18:12,19:15,20:8}[n],firstSide='German' if n==18 else 'Allied',setupOrder=order,briefing=brief+' Attribution as printed; not an independently verified historical strength return.',victory=victory,specialRules=special)
 d['situation'].update(number=n,title=title,printedDate=date,sourcePdfPage=46 if n==18 else 47)
 if n==19:d['situation']['printedEndDate']='1945-01-01';d['situation']['dateStatus']='Printed span: December 31 through January 1, 1945. Start interpreted as 1944-12-31; engagement date requires resolution before ASL admission.'
 if n==20:d['parentCampaign']={'id':'rhine-march-1945','title':'Rhine crossing','startDate':'1945-03-07','endDate':'1945-03-31','commandSnapshotId':'rhine-situations-1945','timeBasis':'Dated legacy Situation collection; no live headquarters snapshot asserted.'}
 campaign=d['parentCampaign']['id'];d['campaignAssignment']={'campaignId':campaign,'situationId':campaign+':'+slug,'number':{18:6,19:7,20:1}[n]};d['campaign'].update(date=date,parentCampaignId=campaign)
 d['sources']=copy.deepcopy(SOURCES);d['sources']['original-card']['page']=d['situation']['sourcePdfPage'];d['boards']=[copy.deepcopy(BOARDS[b]) for b in boards]
 for field in ['places','routes']:d['mapModel'][field]=[x for x in d['mapModel'][field] if (x['hexes'][0]['boardId'] if field=='places' else x['geometry']['boardId']) in boards]
 d['mapModel']['zones']=[];d['mapModel']['seams']=[]
 d['illustration'].update(seed=d['id']+':art-v1',joinedLayout=[{'boardId':b,'clockwiseDegrees':a} for b,a in zip(boards,rotations)],northIndicators=[{'boardId':b,'clockwiseDegreesFromSheetUp':(360-a)%360} for b,a in zip(boards,rotations)],layoutAxis='horizontal')
 for field in ['features','labels']:d['illustration'][field]=[x for x in d['illustration'][field] if x['boardId'] in boards]
 d['counters']=[];d['instances']=[];d['formations']=[]
 for id,count in roster:
  c=copy.deepcopy(TYPES[id]);c.update(quantity=count,nationality=id.split('-')[0]);c.setdefault('kind','unit');d['counters'].append(c)
  for k in range(1,count+1):
   turn=4 if n==18 and c['kind']=='aircraft' else 1
   d['instances'].append({'id':f'{id}-{k:02}','counterTypeId':id,'artwork':c['artwork'],'formationId':None,'setupGroup':'Air support' if c['kind']=='aircraft' else c['side'],'initialHex':None,'availability':{'kind':'reinforcement' if turn>1 else 'at-start','turn':turn,'entryZoneId':None}})
 d['totals']={s:sum(c['quantity'] for c in d['counters'] if c['side']==s) for s in ['Allied','German']};d['totals']['types']=len(d['counters'])
 ids={18:{'Allied':['A','C'],'German':['D']},19:{'Allied':['A'],'German':['C']},20:{'Allied':['D'],'German':['D']}}[n]
 texts={18:{'Allied':'Ground units anywhere on A and C; aircraft retained off-map in the support roster.','German':'Anywhere on D.'},19:{'German':'On C, at least four hexes away from A.','Allied':'Ground units on A; aircraft retained off-map in the support roster.'},20:{'German':'North of the east-west river, and in Artain. The roadblock must be on the bridge.','Allied':'South of the east-west river, at least three hexes away from Artain.'}}[n]
 d['setupBoards']={s:bs[0] for s,bs in ids.items()};d['rules']['setup']=[{'side':s,'order':k+1,'boardIds':ids[s],'zoneIds':[],'instructions':texts[s]} for k,s in enumerate(order)];d['rules']['objectives']=[]
 allowed={};instructions={}
 for i in d['instances']:
  c=next(c for c in d['counters'] if c['id']==i['counterTypeId']);side=c['side'];refs=[]
  if c['kind']=='aircraft':allowed[i['id']]=[];instructions[i['id']]='Air support roster only; no ground placement. Earliest availability: turn '+str(i['availability']['turn'])+'.';continue
  for bid in ids[side]:
   b=next(b for b in d['boards'] if b['id']==bid);hs=[h for h in b['hexes'] if h['setupAllowed']]
   if n==19 and side=='German':
    # C is rotated 180: its source right edge is the display west edge adjoining A.
    edge=[h for h in b['hexes'] if any(p['u']>=.99999 for p in h['imagePolygon'])];hs=[h for h in hs if min(dist(h,e) for e in edge)>=4]
   if n==20:
    town=[h for h in b['hexes'] if 'artain' in h['placeIds']]
    if c['kind']=='block':hs=[h for h in hs if h['id']=='BB-6']
    elif side=='German':hs=[h for h in hs if 'artain' in h['placeIds'] or (river_v(d,h) and h['imageCenter']['v']<min(river_v(d,h)) and 'stream' not in h['terrain']['features'])]
    else:hs=[h for h in hs if river_v(d,h) and h['imageCenter']['v']>max(river_v(d,h)) and 'stream' not in h['terrain']['features'] and min(dist(h,t) for t in town)>=3]
   assert hs,(n,i['id']);refs += [{'boardId':bid,'hexId':h['id']} for h in hs]
  allowed[i['id']]=refs;instructions[i['id']]=texts[side]
 d['deployment']={'allowedHexes':allowed,'instructions':instructions,'loads':[],'evidence':'Source-card setup applied to interpreted geometry. Half-hexes excluded. Remagen treats north-bank positions and Artain as allowed areas, not their empty intersection. Bridge identified as D-BB-6; stream hexes excluded pending rules review.' if n==20 else 'Source-card setup applied to interpreted hex geometry; boundary half-hexes excluded. Aircraft remain off-map. Review before combat admission.'}
 if n in [18,19]:
  d['airSupport']={'status':'reference-only','instructions':('All aircraft earliest turn 4. P-47s form two flights of five: only one fighter-bomber flight on the board at a time; no mixed flights or repeat runs.' if n==18 else 'One L-5 observer and five P-47 bomb aircraft. No additional entry delay is printed on this card. Standard aircraft rules remain unimplemented.'),'maxConcurrentFighterFlights':1 if n==18 else None,'allowFlightMixing':False if n==18 else None,'allowRepeatRuns':False if n==18 else None,'groups':[]}
  for c in d['counters']:
   if c['kind']=='aircraft':d['airSupport']['groups'].append({'id':c['id']+'-flight','role':'observer' if c['id']=='us-l5' else 'fighter-bomber','instanceIds':[i['id'] for i in d['instances'] if i['counterTypeId']==c['id']],'availableFromTurn':4 if n==18 else 1})
 d['sourceRules']={'status':'transcribed-reference','objectives':victory,'specialRules':special,'execution':'Source clauses only. Combat, air activation, demolition, obstacle clearance, movement and objective scoring are not implemented.'}
 d['admission']['requiredCapabilities']=['restricted-setup','formation-combat']+(['scheduled-air-support','force-ratio-scoring' if n==19 else 'penetration-scoring'] if n!=20 else ['bridge-demolition','obstacle-clearance','crossing-restrictions'])
 d['admission']['blockers']=['Interpreted terrain, setup zones and seams require review before combat admission','Formation combat, source-rule execution and scoring not connected','Dated force decomposition and cross-Situation persistent assets unresolved']
 if n==19:d['admission']['blockers']+=['Printed ratio grades overlap; zero denominator and boundary precedence require review','Two-day source window requires a specific engagement date before executable ASL admission']
 im=pdf.PdfDocument(d['situation']['sourcePdf'])[d['situation']['sourcePdfPage']-1].render(scale=2).to_pil()
 # Exclude the preceding Situation turn track above the lower card on page 46.
 y0=int(im.height*.56) if n==18 else 0 if n==19 else im.height//2
 im.crop((0,y0,im.width,im.height if n!=19 else im.height//2)).save(OUT/f'card-{n:02}.png');d['sourceCardImage']=f'card-{n:02}.png'
 dump(ROOT/f'sources/situations/panzer-leader-{n:02}.json',d);return d
cards=[build(18,'bastogne-siege','Bastogne: Siege','1944-12-26',R18,['D','C','A'],[180,0,0],['Allied','German'],[
 'At game end count German combat units on A: more than 30 gives decisive German victory; 21-30 tactical German; 16-20 marginal German.',
 '11-15 gives marginal Allied victory; 6-10 tactical Allied; five or fewer decisive Allied.'
 ],'Allied aircraft may not enter until after turn three. Fighter-bombers are divided into two flights of five; no more than one flight on board at a time. Flights may not be mixed and repeat runs are not allowed.','Elements of Panzer Lehr and the 326th Volksgrenadier Division attack the U.S. 101st Airborne Division and attached units at Bastogne.'),
 build(19,'pattons-counter-offensive',"Patton's Counter Offensive",'1944-12-31',R19,['A','C','D'],[180,180,0],['German','Allied'],[
 'At game end compare Allied to German units on boards C and D. Printed ratio 3:1: decisive Allied victory; 2:1: tactical Allied victory.',
 'Printed ratio less than 2:1: tactical German victory; less than 3:2: decisive German victory. Overlapping bands, intermediate ratios and zero-denominator cases require reviewed scoring precedence.'
 ],'Use British Sherman counters to fill out M4/76 units, using M4/76 values. The digital package supplies nine matching US M4/76 counters with the printed US values.','Patton Third Army relief offensive: U.S. 11th Armored and 35th Infantry Divisions attack the 26th Volksgrenadier Division and Panzer Lehr elements.'),
 build(20,'remagen-bridge','Remagen Bridge','1945-03-07',R20,['D'],[0],['German','Allied'],[
 'Allied victory: control the bridge hex AND clear its block counter by game end.',
 'German victory: prevent the Allied conditions OR destroy the bridge.'
 ],'The German block starts on the bridge hex in the east-west river. No units, including infantry, may cross the stream. Allied vehicles may not enter the bridge hex until engineers remove the block. The German engineer needs no accompanying vehicle to perform demolition.','U.S. 9th Armored Division elements attempt to seize the Ludendorff Bridge at Remagen from a mixed German garrison.')]
registry=read(ROOT/'sources/campaign-registry.json')
for d in cards:
 c=next((c for c in registry['campaigns'] if c['id']==d['parentCampaign']['id']),None)
 if c is None:c={**d['parentCampaign'],'situations':[]};registry['campaigns'].append(c)
 a=d['campaignAssignment'];c['situations']=[x for x in c['situations'] if x['sourcePackageId']!=d['id']]+[{'id':a['situationId'],'number':a['number'],'title':d['situation']['title'],'sourcePackageId':d['id']}];c['situations'].sort(key=lambda x:x['number']);c['nextSituationNumber']=max(x['number'] for x in c['situations'])+1
dump(ROOT/'sources/campaign-registry.json',registry)
print([(d['situation']['number'],d['totals'],len([i for i in d['instances'] if d['deployment']['allowedHexes'][i['id']]])) for d in cards])
