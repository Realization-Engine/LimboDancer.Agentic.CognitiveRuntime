"""Import PL13/15/17 from original cards and archived Imaginative Strategist sheets.
Run after the first batch importer. Idempotent; retains existing campaign numbers.
"""
import copy, hashlib, json, math
from pathlib import Path
import pypdfium2 as pdf
ROOT=Path(__file__).resolve().parent
ARCHIVE=Path(r'E:\Archive\WWII-Docs\1-PanzerBlitz-PanzerLeader\Original-Rules-and-Situations')
OUT=ROOT/'assets/panzer-shared'
def read(p):return json.loads(p.read_text(encoding='utf-8'))
def dump(p,d):p.write_text(json.dumps(d,indent=2)+'\n',encoding='utf-8',newline='\n')
BASE=read(ROOT/'sources/situations/panzer-leader-14.json');A=read(ROOT/'sources/situations/panzer-leader-04.json')
TYPES={};SOURCES={};BOARDS={};SHEETS={}
for n in [4,8,14,16]:
 d=read(ROOT/f'sources/situations/panzer-leader-{n:02}.json');SOURCES.update(d['sources']);TYPES.update({c['id']:copy.deepcopy(c) for c in d['counters']});BOARDS.update({b['id']:copy.deepcopy(b) for b in d['boards']})
def token(id,label,f,sheet,counter,x,y):
 path=ARCHIVE/('Counters' if sheet.startswith('PB') else 'PanzerLeader/Counters')/(sheet+' Colour.pdf')
 SOURCES[sheet]={'path':str(path),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'page':1}
 if sheet not in SHEETS:SHEETS[sheet]=pdf.PdfDocument(str(path))[0].render(scale=3).to_pil()
 SHEETS[sheet].crop((x*3,y*3,(x+45)*3,(y+45)*3)).save(OUT/f'counters/{id}.png')
 c=copy.deepcopy(TYPES['us-rifle']);c.update(id=id,label=label,side='German' if id.startswith('de-') else 'Allied',factors=dict(zip(['attack','weaponType','range','defense','movement'],f)),artwork=f'counters/{id}.png',sourceSheet=sheet,sourceCounterId=counter,sourceCropPointsTopLeft=[x,y,45,45],match='numeric-factors-and-weapon-type-verified',notes=['Matched to archived Imaginative Strategist artwork; optional artwork annotations do not enable additional rules.'])
 TYPES[id]=c
for args in [
 ('us-scout','US Scout',[1,'I',2,3,1],'PL American 1','1B01',180,495),
 ('us-76mm','76 mm AT',[12,'A',10,2,0],'PL American 1','B01',315,0),
 ('us-155mm','155 mm howitzer',[60,'(H)',36,2,0],'PL American 1','I01',495,180),
 ('us-8inch','8 inch howitzer',[80,'(H)',40,2,0],'PL American 1','J01',45,360),
 ('us-105mm','105 mm howitzer',[40,'(H)',32,2,0],'PL American 1','H01',360,180),
 ('us-m20','M20 scout car',[2,'I',4,3,15],'PL American 4','5A01',0,0),
 ('us-m8','M8 armored car',[3,'A',5,3,15],'PL American 4','5B01',90,0),
 ('us-m16','M16 AA',[8,'I',4,3,10],'PL American 4','6E01',90,135),
 ('de-wespe','Wespe',[40,'(H)',32,5,8],'PL German 2','6C01',90,180),
 ('de-hummel','Hummel',[60,'(H)',24,6,8],'PL German 2','6E01',315,180),
 ('de-75ig','75 mm infantry gun',[2,'H',12,2,0],'PB German 1','I01',0,360),
 ('de-75how','75 mm howitzer',[20,'(H)',28,2,0],'PL German 1','K01',0,0),
 ('de-105how','105 mm howitzer',[40,'(H)',32,2,0],'PL German 1','L01',45,0),
 ('de-150how','150 mm howitzer',[60,'(H)',36,2,0],'PL German 1','M01',315,0),
 ('de-quad20','Quad 20 mm AA',[14,'H',10,1,0],'PB German 1','G01',495,135),
 ('de-nebel','Nebelwerfer',[60,'(H)',16,1,0],'PL German 1','Q01',450,0),
 ('de-grille','Grille',[10,'H',12,5,6],'PB German 4','7A01',495,180),
 ('de-panther-w','Panther (W)',[13,'A',12,11,10],'PL German 2','9O01',315,360),
 ('uk-scout','British Scout',[1,'I',2,3,1],'PL Commonwealth 1','1C01',315,495),
 ('uk-25pdr','British 25 pounder',[35,'(H)',35,2,0],'PL Commonwealth 1','G01',360,135),
 ('uk-m3','British M3 scout car',[2,'C(I)',2,2,14],'PL Commonwealth 3','4B01',360,0),
 ('uk-daimler','Daimler',[3,'A',5,3,16],'PL Commonwealth 4','5B01',135,0),
 ('uk-mgcarrier','British MG carrier',[4,'H',12,4,10],'PL Commonwealth 4','6B01',135,135),
 ('uk-sherman','British Sherman',[10,'A',8,8,8],'PL Commonwealth 5','9B01',135,0),
 ('uk-bren','Bren carrier',[2,'C(I)',2,2,10],'PL Commonwealth 3','4D01',45,360),
]:token(*args)
TYPES['uk-mgcarrier']['notes'].append('Source card shows the 4/H/12, defense 4, movement 10 carrier; matched Commonwealth 4 artwork labels it Recon HQ (6B01). This is a recorded artwork-label difference, not a change to its factors.')
# A positional marker, not a combat unit with invented zero factors.
(OUT/'counters/us-block.svg').write_text('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128"><rect width="128" height="128" fill="#eee8d8"/><path d="M25 18L103 110M103 18L25 110" stroke="#111" stroke-width="16"/></svg>\n',encoding='utf-8')
SOURCES['original-card']={'path':BASE['situation']['sourcePdf'],'sha256':hashlib.sha256(Path(BASE['situation']['sourcePdf']).read_bytes()).hexdigest(),'page':45}
block=copy.deepcopy(TYPES['us-rifle']);block.update(id='us-block',label='Roadblock',kind='block',factors=None,artwork='counters/us-block.svg',artworkProvider='Authored symbol from original card',sourceSheet=next(k for k,v in SOURCES.items() if '1025016933' in v['path']),sourceCounterId='PL15-X',sourceCropPointsTopLeft=[],match='source-symbol-verified',notes=['One X block marker on Situation 15. Original unit-function table, PDF page 23, identifies X as Blocks. Authored symbol; no matching Imaginative Strategist marker was identified. This is not a combat unit.'])
TYPES['us-block']=block

def dist(a,b):return max(abs(a['q']-b['q']),abs(a['r']-b['r']),abs(a['q']+a['r']-b['q']-b['r']))
def roster(group,values):return [(k,v,group) for k,v in values]
R13=roster('US', [('us-armored-rifle',9),('us-rifle',18),('us-scout',3),('us-57mm',3),('us-76mm',1),('us-155mm',1),('us-8inch',1),('us-81mm',3),('us-m20',3),('us-m8',3),('us-m7',3),('us-m16',2),('us-m4-105',2),('us-m5',3),('us-m4-75',9),('us-m4-76',3),('us-halftrack',17),('us-truck',4)])+roster('FBB',[('de-rifle',9),('de-engineer',1),('de-88mm',2),('de-wespe',2),('de-hummel',1),('de-wirbelwind',1),('de-pziv',6),('de-panther',6),('de-halftrack',10),('de-truck',2)])+roster('18VG',[('de-smg',9),('de-rifle',9),('de-engineer',2),('de-75mm',2),('de-37mm',1),('de-75ig',4),('de-75how',3),('de-105how',2),('de-150how',1),('de-81mm',3),('de-120mm',2),('de-hetzer',2),('de-truck',4),('de-wagon',6)])
R15=roster('US',[('us-rifle',18),('us-mg',3),('us-57mm',3),('us-105mm',2),('us-81mm',3),('us-m10',3),('us-m8',3),('us-m4-75',2),('us-halftrack',2),('us-truck',6),('us-block',1)])+roster('A',[('de-smg',3),('de-rifle',6),('de-engineer',1),('de-20mm',2),('de-wirbelwind',1),('de-pziv',1),('de-panther',6),('de-tiger',3),('de-kingtiger',3),('de-halftrack',9),('de-truck',6)])+roster('B',[('de-smg',6),('de-rifle',12),('de-engineer',2),('de-37mm',1),('de-81mm',2),('de-120mm',1),('de-truck',1),('de-wagon',4)])
R17=roster('US',[('us-armored-rifle',9),('us-rifle',3),('us-57mm',3),('us-155mm',2),('us-81mm',2),('us-m20',3),('us-m8',3),('us-m7',3),('us-m4-105',1),('us-m5',3),('us-m4-75',6),('us-m4-76',3),('us-halftrack',17)])+roster('British',[('uk-scout',3),('uk-25pdr',1),('uk-m3',3),('uk-daimler',5),('uk-mgcarrier',1),('uk-sherman',8),('uk-bren',1)])+roster('A',[('de-smg',3),('de-engineer',1),('de-81mm',2),('de-120mm',1),('de-234-1',3),('de-234-2',1),('de-234-4',1),('de-wespe',1),('de-hetzer',1),('de-stug',1),('de-lynx',1),('de-halftrack',3)])+roster('B',[('de-rifle',18),('de-20mm',2),('de-quad20',2),('de-81mm',1),('de-120mm',2),('de-nebel',1),('de-wespe',1),('de-grille',3),('de-panther-w',10),('de-halftrack',7),('de-truck',6)])

# Source-space stream polyline, not a guessed column: at 270 degrees west is lower v.
def west_stream(d,b,h):
 candidates=[rt for rt in d['mapModel']['routes'] if rt['geometry']['boardId']==b['id'] and rt['kind']=='stream']
 # Long transverse stream becomes the north-south stream after rotation.
 rt=max(candidates,key=lambda rt:max(p[0] for p in rt['geometry']['coordinates'])-min(p[0] for p in rt['geometry']['coordinates']))
 pts=rt['geometry']['coordinates'];u=h['imageCenter']['u'];vs=[]
 for a,z in zip(pts,pts[1:]):
  if min(a[0],z[0])<=u<=max(a[0],z[0]) and a[0]!=z[0]:vs.append(a[1]+(u-a[0])*(z[1]-a[1])/(z[0]-a[0]))
 return bool(vs) and h['imageCenter']['v']<min(vs) and 'stream' not in h['terrain']['features']

def build(n,slug,title,date,boards,rotations,axis,order,roster,brief,victory,special):
 d=copy.deepcopy(BASE);d.update(id=f'panzer-leader-{n:02}-{slug}',turnLimit=10 if n==15 else 12,firstSide='Allied' if n==17 else 'German',setupOrder=order,briefing=brief+' Formations as printed on the original card; not an independently verified historical strength return.',victory=victory,specialRules=special)
 d['situation'].update(number=n,title=title,printedDate=date,sourcePdfPage={13:44,15:45,17:46}[n]);d['campaign']['date']=date
 local={13:3,15:4,17:5}[n];campaign=d['parentCampaign']['id'];d['campaignAssignment']={'campaignId':campaign,'situationId':campaign+':'+slug,'number':local}
 d['boards']=[copy.deepcopy(BOARDS[b]) for b in boards];d['sources']=copy.deepcopy(SOURCES)
 for field in ['places','routes']:
  merged={x['id']:copy.deepcopy(x) for p in [A,BASE] for x in p['mapModel'][field] if (x['hexes'][0]['boardId'] if field=='places' else x['geometry']['boardId']) in boards};d['mapModel'][field]=list(merged.values())
 d['mapModel']['zones']=[];d['mapModel']['seams']=[]
 d['illustration'].update(seed=d['id']+':art-v1',joinedLayout=[{'boardId':b,'clockwiseDegrees':a} for b,a in zip(boards,rotations)],northIndicators=[{'boardId':b,'clockwiseDegreesFromSheetUp':(360-a)%360} for b,a in zip(boards,rotations)],layoutAxis=axis)
 for field in ['features','labels']:
  vals=[]
  for p in [A,BASE]:
   for x in p['illustration'][field]:
    if x['boardId'] in boards and x not in vals:vals.append(copy.deepcopy(x))
  d['illustration'][field]=vals
 d['counters']=[];d['instances']=[];d['formations']=[];groups={}
 for id,count,group in roster:
  c=next((c for c in d['counters'] if c['id']==id),None)
  if not c:
   c=copy.deepcopy(TYPES[id]);c.update(quantity=0,nationality=id.split('-')[0]);c.setdefault('kind','unit');d['counters'].append(c)
  for k in range(count):
   iid=f'{id}-{c["quantity"]+1:02}';c['quantity']+=1;groups[iid]=group;d['instances'].append({'id':iid,'counterTypeId':id,'artwork':c['artwork'],'formationId':None,'setupGroup':group,'initialHex':None,'availability':{'kind':'at-start','turn':1,'entryZoneId':None}})
 d['totals']={s:sum(c['quantity'] for c in d['counters'] if c['side']==s) for s in ['Allied','German']};d['totals']['types']=len(d['counters'])
 boardids={13:{'Allied':['C','D'],'German':['C','D']},15:{'Allied':['C'],'German':['D']},17:{'Allied':['A','D'],'German':['C']}}[n]
 texts={13:{'Allied':'Anywhere on C or D, at least five hexes from the east edge.','German':'Within three hexes of the east edge: Fuehrer Begleit Brigade on C; 18th Volksgrenadier on D.'},15:{'Allied':'Anywhere on C, including the one roadblock.','German':'On D: Group A (12th SS Panzer) south of row Q; Group B (276th Volksgrenadier) north of row Q. Row Q is excluded.'},17:{'Allied':'Americans on A, British on D, west of the north-south stream.','German':'Group A (Recon battalion) within one hex of St. Athan; Group B (304 PG / 3 Panzer regiments) within two hexes of Wiln, on C.'}}[n]
 d['setupBoards']={s:bs[0] for s,bs in boardids.items()};d['rules']['setup']=[{'side':s,'order':k+1,'boardIds':boardids[s],'zoneIds':[],'instructions':texts[s]} for k,s in enumerate(order)];d['rules']['objectives']=[]
 allowed={};instructions={}
 for i in d['instances']:
  c=next(c for c in d['counters'] if c['id']==i['counterTypeId']);side=c['side'];group=groups[i['id']];refs=[]
  bs=boardids[side]
  if n==13 and side=='German':bs=['C' if group=='FBB' else 'D']
  if n==17 and side=='Allied':bs=['A' if group=='US' else 'D']
  for bid in bs:
   b=next(b for b in d['boards'] if b['id']==bid);hs=[h for h in b['hexes'] if h['setupAllowed']]
   if n==13:
    edge=[h for h in b['hexes'] if h['r']==(0 if bid=='C' else 32)]
    hs=[h for h in hs if (min(dist(h,e) for e in edge)>=5 if side=='Allied' else min(dist(h,e) for e in edge)<=3)]
   if n==15 and side=='German':hs=[h for h in hs if (h['r']<16 if group=='A' else h['r']>16)]
   if n==17:
    if side=='German':
     town='st-athan' if group=='A' else 'wiln';centers=[h for h in b['hexes'] if town in h['placeIds']];hs=[h for h in hs if min(dist(h,t) for t in centers)<=(1 if group=='A' else 2)]
    else:hs=[h for h in hs if west_stream(d,b,h)]
   refs += [{'boardId':bid,'hexId':h['id']} for h in hs]
  assert refs,(n,i['id']);allowed[i['id']]=refs;instructions[i['id']]=texts[side]+' Source contingent: '+group+'.'
 d['deployment']={'allowedHexes':allowed,'instructions':instructions,'loads':[],'evidence':'Source setup applied to interpreted board hexes and source-space stream geometry. Boundary half-hexes excluded. Review required before combat admission.'}
 d['sourceRules']={'status':'transcribed-reference','objectives':victory,'specialRules':special,'execution':'Reference clauses only. Combat, movement, obstacle effects, scoring and overlapping victory precedence are not implemented.'}
 d['admission']['requiredCapabilities']=['restricted-setup','graded-victory','formation-combat']+(['positional-obstacles','exit-corridor'] if n==15 else ['multi-board-setup','multi-contingent-forces'])
 d['admission']['blockers']=['Interpreted terrain and setup zones require review before combat admission','Formation combat, objective scoring and result reconciliation not connected','Dated force decomposition and persistent cross-Situation allocation unresolved']
 if n in [13,17]:d['admission']['blockers'].append('Overlapping victory grades require an explicit precedence policy')
 doc=pdf.PdfDocument(d['situation']['sourcePdf']);im=doc[d['situation']['sourcePdfPage']-1].render(scale=1.5).to_pil();im.crop((0,0,im.width,im.height//2)).save(OUT/f'card-{n:02}.png');d['sourceCardImage']=f'card-{n:02}.png'
 dump(ROOT/f'sources/situations/panzer-leader-{n:02}.json',d);return d
cards=[
 build(13,'fortified-goose-egg',"The 'Fortified Goose Egg'",'1944-12-17',['C','D'],[90,270],'vertical',['Allied','German'],R13,'Elements of the 18th Volksgrenadier Division and Fuehrer Begleit Brigade assault elements of the U.S. 106th Infantry Regiment and 7th Armored Division near St. Vith.',[
 'At game end: German control of 13 town hexes OR elimination of at least 33 Allied combat units: decisive German victory.',
 'German control of 11-12 town hexes: tactical German victory; 8-10: marginal German victory.',
 'German control of 6-7 town hexes: marginal Allied victory; 4-5: tactical Allied victory.',
 'German control of fewer than four town hexes OR Allied elimination of at least 25 German combat units: decisive Allied victory.'], 'None on the original card.'),
 build(15,'elsenborn-ridge','Elsenborn Ridge','1944-12-18',['C','D'],[180,180],'horizontal',['Allied','German'],R15,'Elements of the 12th SS Panzer Division and 276th Volksgrenadier Division attack remnants of the U.S. 2nd Division near Krinkelt and Rocherath.',[
 'At game end, German victory requires control of all town hexes on C AND exit of at least ten combat units off the west edge between J-10 and X-10 inclusive.',
 'Allied victory: prevent the German conditions.'], 'None on the original card. The force list includes one X block counter (a positional roadblock, not a combat unit).'),
 build(17,'turning-point-celles','Turning Point: Celles','1944-12-25',['A','C','D'],[270,90,270],'vertical',['German','Allied'],R17,'Elements of the U.S. 2nd Armored Division and British 29th Armored Brigade attack the German 2nd Panzer Division near Celles.',[
 'At game end, evaluate town hexes on C and enemy combat-unit losses.',
 'Allied decisive: control every town hex AND eliminate at least 25 German units. Allied tactical: control every town hex. Allied marginal: control every hex of one town.',
 'German decisive: control every town hex OR eliminate at least 32 Allied units. German tactical: control every hex of one town. German marginal: control at least one hex in each of both towns.'], 'Use Pz V (SS) counters to fill out Pz V (W) units, using the lower value. This package supplies ten correctly matched W counters, with factors 13/A/12, defense 11, movement 10.'),
]
reg=read(ROOT/'sources/campaign-registry.json');campaign=next(c for c in reg['campaigns'] if c['id']==BASE['parentCampaign']['id'])
for d in cards:
 assignment=d['campaignAssignment'];record={'id':assignment['situationId'],'number':assignment['number'],'title':d['situation']['title'],'sourcePackageId':d['id']}
 campaign['situations']=[x for x in campaign['situations'] if x['sourcePackageId']!=d['id']]+[record]
campaign['situations'].sort(key=lambda x:x['number']);campaign['nextSituationNumber']=max(x['number'] for x in campaign['situations'])+1;dump(ROOT/'sources/campaign-registry.json',reg)
print([(d['situation']['number'],d['totals']) for d in cards])
