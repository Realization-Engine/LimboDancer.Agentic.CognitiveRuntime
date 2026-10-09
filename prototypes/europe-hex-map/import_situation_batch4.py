"""Import PL09/10/11 from the archived original cards; reference-only packages."""
import copy,hashlib,json
from pathlib import Path
import pypdfium2 as pdf
ROOT=Path(__file__).resolve().parent
ARCHIVE=Path(r'E:\Archive\WWII-Docs\1-PanzerBlitz-PanzerLeader\Original-Rules-and-Situations')
OUT=ROOT/'assets/panzer-shared'
def read(p):return json.loads(p.read_text(encoding='utf-8'))
def dump(p,d):p.write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8',newline='\n')
TYPES={};SOURCES={};BOARDS={};SHEETS={}
BASE=read(ROOT/'sources/situations/panzer-leader-17.json')
for n in [4,8,13,14,15,16,17,18,19,20]:
 d=read(ROOT/f'sources/situations/panzer-leader-{n:02}.json');SOURCES.update(d['sources']);TYPES.update({c['id']:copy.deepcopy(c) for c in d['counters']});BOARDS.update({b['id']:copy.deepcopy(b) for b in d['boards']})
def token(id,label,f,sheet,counter,x,y,kind='unit'):
 path=ARCHIVE/('Counters' if sheet.startswith('PB') else 'PanzerLeader/Counters')/(sheet+' Colour.pdf');SOURCES[sheet]={'path':str(path),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'page':1}
 if sheet not in SHEETS:SHEETS[sheet]=pdf.PdfDocument(str(path))[0].render(scale=3).to_pil()
 SHEETS[sheet].crop((x*3,y*3,(x+45)*3,(y+45)*3)).save(OUT/f'counters/{id}.png')
 c=copy.deepcopy(TYPES['us-rifle']);c.update(id=id,label=label,kind=kind,nationality=id.split('-')[0],side='German' if id.startswith('de-') else 'Allied',factors=dict(zip(['attack','weaponType','range','defense','movement'],f)) if f else None,artwork=f'counters/{id}.png',sourceSheet=sheet,sourceCounterId=counter,sourceCropPointsTopLeft=[x,y,45,45],match='numeric-factors-and-weapon-type-verified',notes=['Imaginative Strategist artwork matched to the printed card; optional annotations do not enable additional rules.']);TYPES[id]=c
for args in [
 ('uk-rifle','British Rifle',[2,'I',2,6,1],'PL Commonwealth 2','1E01',0,0),
 ('uk-engineer','British Engineer',[3,'I',2,10,1],'PL Commonwealth 1','1A01',450,360),
 ('uk-6pdr','British 6 pounder (57 mm)',[9,'A',5,2,0],'PL Commonwealth 1','A01',0,0),
 ('uk-76mortar','British 76 mm mortar',[3,'M',8,3,1],'PL Commonwealth 1','K01',135,360),
 ('uk-truck','British Truck',[0,'C',0,1,14],'PL Commonwealth 3','4A49',0,0),
 ('uk-achilles','Achilles',[16,'A',10,6,9],'PL Commonwealth 4','8B01',0,360),
 ('uk-valentine-bridge-carrier','Valentine bridge carrier (printed card)',[1,'I',2,10,6],'PL Commonwealth 8','9j01',360,495),
 ('uk-tank-bridge','Tank bridge',None,'PL Commonwealth 4','Tank Bridge',450,360,'bridge-equipment'),
 ('de-pziii','Panzer III',[8,'A',6,7,9],'PL German 2','9M01',450,180),
 ('de-150ig','150 mm infantry gun',[20,'H',12,2,0],'PB German 1','J01',135,360),
 ('de-mg','German Machine gun',[3,'I',2,10,1],'PL German 1','1A01',45,135),
]:token(*args)
# Preserve the printed bridge-carrier profile rather than substitute a gun tank.
original=pdf.PdfDocument(BASE['situation']['sourcePdf'])[41].render(scale=2.5).to_pil()
original.crop((458,287,527,375)).save(OUT/'counters/uk-valentine-bridge-carrier.png')
TYPES['uk-valentine-bridge-carrier'].update(artworkProvider='Original Avalon Hill card, explicit artwork exception',sourceSheet='original-card',sourceCounterId='PL09-Valentine-bridge-carrier',sourceCropPointsTopLeft=[183.2,114.8,27.6,35.2],match='printed-source-counter',notes=['Original-card Valentine bridge-carrier artwork: 1/I/2, defense 10, movement 6. No exact IS match was identified; standard Valentine gun-tank artwork has incompatible factors. No silent substitution.'])
TYPES['uk-tank-bridge']['match']='source-symbol-verified';TYPES['uk-tank-bridge']['notes']=['Printed bridge capacity 32 is not defense. Bridge equipment has no ground combat factors. Placement is an un-emplaced setup location; construction, loading and crossing effects remain unimplemented.']
# The original Arnhem card prints an 81 mm profile. Commonwealth sheets instead show 76 mm / range 8.
c=copy.deepcopy(TYPES['us-81mm']);c.update(id='uk-81mm',label='British 81 mm mortar (printed card)',nationality='uk',notes=['Uses the matching 3/M/12, defense 3, movement 1 American IS 81 mm crop with British palette. Commonwealth 76 mm artwork would have the wrong range. Printed legacy profile, not a historical equipment correction.']);TYPES[c['id']]=c
for old,new in [('uk-rifle','be-rifle'),('uk-engineer','be-engineer'),('uk-25pdr','be-25pdr'),('uk-76mortar','be-76mortar'),('uk-daimler','be-daimler'),('uk-achilles','be-achilles'),('uk-bren','be-bren'),('uk-truck','be-truck')]:
 c=copy.deepcopy(TYPES[old]);c.update(id=new,nationality='am',label='Belgian '+c['label'].replace('British ',''));c['notes'].append('Belgian Brigade as printed; Commonwealth equipment artwork, Allied Minor palette. Nationality does not imply British command ownership.');TYPES[new]=c
R9=[('us-rifle',9),('us-mg',3),('us-81mm',2),('uk-25pdr',3),('uk-sherman',8),('uk-valentine-bridge-carrier',1),('uk-tank-bridge',1),('uk-bren',3),('uk-truck',6),('de-rifle',9),('de-88mm',2),('de-20mm',2),('de-quad20',2),('de-81mm',1),('de-120mm',1),('de-grille',3),('de-stug',1),('de-halftrack',10),('de-truck',6)]
R10=[('uk-rifle',9),('uk-engineer',3),('uk-6pdr',2),('uk-81mm',2),('de-rifle',12),('de-88mm',1),('de-20mm',2),('de-150ig',1),('de-81mm',2),('de-grille',2),('de-pziii',2),('de-pziv',1),('de-tiger',2),('de-halftrack',1),('de-wagon',5)]
R11=[('be-rifle',9),('be-engineer',3),('be-25pdr',2),('be-76mortar',1),('be-daimler',4),('be-achilles',2),('be-bren',8),('be-truck',6),('us-rifle',6),('us-scout',3),('us-m20',3),('us-m8',3),('us-m5',3),('us-m4-75',3),('us-halftrack',9),('de-rifle',18),('de-security',1),('de-mg',3),('de-75mm',3),('de-75ig',4),('de-105how',2),('de-81mm',2),('de-120mm',2),('de-pziv-w',2),('de-panther-w',5),('de-truck',5),('de-wagon',6)]
token('de-pziv-w','Panzer IV (W)',[11,'A',8,7,8],'PL German 2','9N01',0,360)
# Main transverse watercourse, from traced source geometry. Rotation converts the half-plane to the card's compass direction.
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

def build(n,slug,title,date,roster,boards,rot,axis,order,brief,victory,special):
 d=copy.deepcopy(BASE);d.update(id=f'panzer-leader-{n:02}-{slug}',turnLimit=12 if n==11 else 10,firstSide='German' if n==10 else 'Allied',setupOrder=order,briefing=brief+' Formations and date as printed, not independently verified historical strengths.',victory=victory,specialRules=special)
 d['situation'].update(number=n,title=title,printedDate=date,sourcePdfPage=43 if n==11 else 42)
 d['parentCampaign']={'id':'market-garden-september-1944','title':'Market-Garden ground operations','startDate':'1944-09-20','endDate':'1944-09-29','commandSnapshotId':'market-garden-situations-1944','timeBasis':'Dated source-card collection; no live headquarters snapshot asserted.'}
 campaign=d['parentCampaign']['id'];d['campaignAssignment']={'campaignId':campaign,'situationId':campaign+':'+slug,'number':n-8};d['campaign'].update(date=date,parentCampaignId=campaign)
 d['sources']=copy.deepcopy(SOURCES);d['sources']['original-card']['page']=d['situation']['sourcePdfPage'];d['boards']=[copy.deepcopy(BOARDS[b]) for b in boards]
 for field in ['places','routes']:d['mapModel'][field]=[x for x in d['mapModel'][field] if (x['hexes'][0]['boardId'] if field=='places' else x['geometry']['boardId']) in boards]
 d['mapModel']['zones']=[];d['mapModel']['seams']=[]
 d['illustration'].update(seed=d['id']+':art-v1',joinedLayout=[{'boardId':b,'clockwiseDegrees':r} for b,r in zip(boards,rot)],northIndicators=[{'boardId':b,'clockwiseDegreesFromSheetUp':(360-r)%360} for b,r in zip(boards,rot)],layoutAxis=axis)
 for field in ['features','labels']:d['illustration'][field]=[x for x in d['illustration'][field] if x['boardId'] in boards]
 d['counters']=[];d['instances']=[];d['formations']=[]
 for id,count in roster:
  c=copy.deepcopy(TYPES[id]);c['quantity']=count;c.setdefault('kind','unit');d['counters'].append(c)
  for k in range(1,count+1):d['instances'].append({'id':f'{id}-{k:02}','counterTypeId':id,'artwork':c['artwork'],'formationId':None,'setupGroup':'Belgian Brigade' if id.startswith('be-') else 'US 113 Cavalry Group' if n==11 and id.startswith('us-') else c['side'],'initialHex':None,'availability':{'kind':'at-start','turn':1,'entryZoneId':None}})
 d['totals']={s:sum(c['quantity'] for c in d['counters'] if c['side']==s) for s in ['Allied','German']};d['totals']['types']=len(d['counters'])
 texts={9:{'German':'North of the major stream, on D and A.','Allied':'South of the major stream, on D and A. Tank bridge is un-emplaced equipment during setup.'},10:{'Allied':'In Grancelles.','German':'At least three hexes from the nearest deployed Allied unit.'},11:{'German':'East of the major stream.','Allied':'Belgians on A, west of the major stream; Americans on D, west of the major stream.'}}[n]
 allowedBoards={'Allied':boards if n!=11 else ['A','D'],'German':boards};d['setupBoards']={s:bs[0] for s,bs in allowedBoards.items()};d['rules']['setup']=[{'side':s,'order':k+1,'boardIds':allowedBoards[s],'zoneIds':[],'instructions':texts[s]} for k,s in enumerate(order)];d['rules']['objectives']=[]
 allowed={};instructions={}
 for i in d['instances']:
  c=next(c for c in d['counters'] if c['id']==i['counterTypeId']);side=c['side'];refs=[]
  for b in d['boards']:
   bid=b['id'];hs=[h for h in b['hexes'] if h['setupAllowed']]
   if n==10 and side=='Allied':hs=[h for h in hs if 'grancelles-all' in h['placeIds']]
   if n==9:hs=[h for h in hs if stream_side(d,bid,h)==('high' if side=='German' else 'low')]
   if n==11:
    if side=='Allied' and bid!=('A' if i['id'].startswith('be-') else 'D'):continue
    # 270-degree A: west is low v; 90-degree D/C: west is high v.
    west='low' if bid=='A' else 'high';want=west if side=='Allied' else ('high' if west=='low' else 'low')
    hs=[h for h in hs if stream_side(d,bid,h)==want]
   refs += [{'boardId':bid,'hexId':h['id']} for h in hs]
  assert refs,(n,i['id']);allowed[i['id']]=refs;instructions[i['id']]=texts[side]
 d['deployment']={'allowedHexes':allowed,'instructions':instructions,'loads':[],'evidence':'Original-card setup over interpreted source stream traces. Stream hexes and boundary fragments excluded. Arnhem distance is measured from actual Allied placements, not every possible town hex.'}
 if n==10:d['deployment']['minimumSeparation']=[{'side':'German','fromSide':'Allied','hexes':3}]
 d['sourceRules']={'status':'transcribed-reference','objectives':victory,'specialRules':special,'execution':'Setup only. Combat, movement, bridge emplacement, conditional sector release and scoring are not implemented.'}
 d['admission']['requiredCapabilities']=['restricted-setup','formation-combat', {9:'bridge-emplacement',10:'timed-town-control',11:'conditional-sector-release'}[n]]
 d['admission']['blockers']=['Interpreted terrain, stream zones and joins require review before combat admission','Formation combat, source scoring and persistent force decomposition unresolved']
 if n==9:d['admission']['blockers'].append('Original Valentine bridge-carrier artwork exception retained; bridge transport/emplacement requires rules implementation')
 if n==10:d['admission']['blockers'].append('Printed timing grades require precedence review; British mortar uses source-card 81 mm profile')
 if n==11:d['admission']['blockers'].append('3:1 sector-release trigger, eligible unit counting and zero-enemy case require rules adjudication')
 im=pdf.PdfDocument(d['situation']['sourcePdf'])[d['situation']['sourcePdfPage']-1].render(scale=2).to_pil();half=1 if n==10 else 0;im.crop((0,half*im.height//2,im.width,(half+1)*im.height//2)).save(OUT/f'card-{n:02}.png');d['sourceCardImage']=f'card-{n:02}.png'
 dump(ROOT/f'sources/situations/panzer-leader-{n:02}.json',d);return d
cards=[build(9,'nijmegen','Operation Market: Nijmegen','1944-09-20',R9,['D','A'],[180,180],'horizontal',['German','Allied'],'Elements of U.S. 82nd Airborne Division and Irish Guards assault German 9th SS Panzer Division at Nijmegen.', ['Allied victory: control two bridge hexes north of hex row Q at game end, including a tank bridge if emplaced and intact.','German victory: prevent the Allied condition.'],'None printed.'),build(10,'arnhem','Operation Market: Arnhem','1944-09-22',R10,['A'],[180],'horizontal',['Allied','German'],'Kampfgruppe Harzer attacks elements of the 1st British Parachute Brigade at Arnhem.', ['German control of all Grancelles town hexes at end of turn 5: decisive German; turn 7: tactical German; turn 8: marginal German.','Control at end of turn 9: marginal British; turn 10: tactical British. Failure to control all at game end: decisive British. Timing-band precedence requires review.'],'None printed.'),build(11,'anticlimax','Operation Garden: Anticlimax','1944-09-29',R11,['A','D','C'],[270,90,90],'vertical',['German','Allied'],'Elements of U.S. 113th Cavalry Group and 1st Belgian Brigade engage German 176th Infantry Division, Division Erdmann and Kampfgruppe Walther.', ['Count German combat units remaining on A and D at game end: fewer than 10 decisive Allied; 10-15 tactical Allied; 16-20 marginal Allied.','21-25 marginal German; 26-30 tactical German; more than 30 decisive German.'],'Belgians must remain on A and Americans on C and/or D until one group outnumbers the German units on its respective board sections by at least 3:1. Then both groups may operate anywhere. Movement permissions are source rules, not active setup exemptions.')]
registry=read(ROOT/'sources/campaign-registry.json')
for d in cards:
 c=next((c for c in registry['campaigns'] if c['id']==d['parentCampaign']['id']),None)
 if c is None:c={**d['parentCampaign'],'situations':[]};registry['campaigns'].append(c)
 a=d['campaignAssignment'];c['situations']=[x for x in c['situations'] if x['sourcePackageId']!=d['id']]+[{'id':a['situationId'],'number':a['number'],'title':d['situation']['title'],'sourcePackageId':d['id']}];c['situations'].sort(key=lambda x:x['number']);c['nextSituationNumber']=max(x['number'] for x in c['situations'])+1
dump(ROOT/'sources/campaign-registry.json',registry)
print([(d['situation']['number'],d['totals']) for d in cards])
