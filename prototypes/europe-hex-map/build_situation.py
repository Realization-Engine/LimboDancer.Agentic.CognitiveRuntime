"""Validate the canonical Situation before emitting the offline Atlas payload."""
import json
from collections import Counter
from pathlib import Path
from jsonschema import Draft202012Validator, FormatChecker
ROOT=Path(__file__).resolve().parent

def validate(data):
    schema=json.loads((ROOT/'schemas/situation.schema.json').read_text(encoding='utf-8'))
    Draft202012Validator.check_schema(schema)
    Draft202012Validator(schema,format_checker=FormatChecker()).validate(data)
    def require(ok,message):
        if not ok: raise ValueError(message)
    def unique(items,label):
        ids=[x['id'] for x in items];require(len(set(ids))==len(ids),'Duplicate '+label);return {x['id']:x for x in items}
    counters=unique(data['counters'],'counter type');instances=unique(data['instances'],'counter instance');boards=unique(data['boards'],'board');formations=unique(data['formations'],'formation')
    zones=unique(data['mapModel']['zones'],'zone');places=unique(data['mapModel']['places'],'place');routes=unique(data['mapModel']['routes'],'route');objectives=unique(data['rules']['objectives'],'objective')
    sides=set(data['setupOrder']);require(len(sides)==len(data['setupOrder']) and data['firstSide'] in sides,'Invalid player order')
    require(len(data['rules']['setup'])==len(sides),'Setup side count')
    for n,setup in enumerate(data['rules']['setup']):
        require(setup['order']==n+1 and setup['side']==data['setupOrder'][n],'Setup order mismatch')
        require(all(b in boards for b in setup['boardIds']) and all(z in zones for z in setup['zoneIds']),'Unknown setup board or zone')
        require(data['setupBoards'][setup['side']] in setup['boardIds'],'Setup board mismatch')
    counts=Counter(i['counterTypeId'] for i in instances.values())
    require(set(counts)<=set(counters),'Unknown counter type')
    for c in counters.values():
        require(c['side'] in sides and counts[c['id']]==c['quantity'],'Counter side or quantity mismatch')
        require(c['sourceSheet'] in data['sources'],'Unknown counter source')
    for side in sides:require(sum(c['quantity'] for c in counters.values() if c['side']==side)==data['totals'][side],'Side total mismatch')
    require(data['totals']['types']==len(counters),'Type total mismatch')
    hexes={}
    for b in boards.values():
        local=unique(b['hexes'],'hex');coords=[(h['q'],h['r']) for h in local.values()];require(len(set(coords))==len(coords),'Duplicate hex coordinates')
        if b['grid']['coverage']=='complete':require(len(local)>0 and len(local)==b['grid']['expectedHexCount'],'Complete board has missing hexes')
        if b['grid']['coverage']=='untranscribed':require(not local,'Untranscribed board contains hexes')
        for h in local.values():
            require({e['direction'] for e in h['edges']}==set(range(6)),'Hex must have six distinct edges')
            require(set(h['placeIds'])<=set(places),'Unknown place')
            for e in h['edges']:require(set(e['routeIds'])<=set(routes),'Unknown edge route')
            hexes[(b['id'],h['id'])]=h
    def hexref(ref):
        require((ref['boardId'],ref['hexId']) in hexes,'Unknown hex reference')
        return hexes[(ref['boardId'],ref['hexId'])]
    for (bid,hid),h in hexes.items():
        for edge in h['edges']:
            if edge['neighbor']:
                neighbor=hexref(edge['neighbor']);require(any(e['neighbor']=={'boardId':bid,'hexId':hid} and e['direction']==(edge['direction']+3)%6 and e['sourceObstacle']==edge['sourceObstacle'] for e in neighbor['edges']),'Nonreciprocal hex neighbor or obstacle')
                if edge['neighbor']['boardId']==bid:
                    dq,dr=[(1,0),(1,-1),(0,-1),(-1,0),(-1,1),(0,1)][edge['direction']]
                    require((neighbor['q'],neighbor['r'])==(h['q']+dq,h['r']+dr),'Invalid axial neighbor')
    for z in zones.values():
        for h in z['hexes']:hexref(h)
        if z['resolution']=='resolved':require(bool(z['hexes']),'Resolved zone is empty')
    for p in places.values():
        for h in p['hexes']:hexref(h)
    for route in routes.values():
        require(route['geometry']['boardId'] in boards,'Unknown route geometry board')
        require(all(h['boardId']==route['geometry']['boardId'] for h in route['path']),'Route crosses coordinate frames')
        for h in route['path']:hexref(h)
        for a,b in zip(route['path'],route['path'][1:]):
            require(any(e['neighbor']==b and route['id'] in e['routeIds'] for e in hexref(a)['edges']),'Route path is disconnected')
    for seam in data['mapModel']['seams']:
        a=hexref(seam['from']);hexref(seam['to']);require(any(e['direction']==seam['direction'] and e['neighbor']==seam['to'] for e in a['edges']),'Seam does not match edge topology')
    for f in formations.values():
        require(f['side'] in sides,'Unknown formation side');seen={f['id']};parent=f['parentId']
        while parent:
            require(parent in formations and parent not in seen,'Invalid formation hierarchy');seen.add(parent);parent=formations[parent]['parentId']
    for i in instances.values():
        if i['formationId']:require(i['formationId'] in formations,'Unknown formation')
        if i['initialHex']:hexref(i['initialHex'])
        if i['availability']['entryZoneId']:require(i['availability']['entryZoneId'] in zones,'Unknown entry zone')
    def predicate(p,trail):
        if p['op'] in ('all','any'):
            for c in p['children']:predicate(c,trail)
        else:
            require(p['side'] in sides,'Unknown objective side')
            if p['op']=='control-all':require(p['zoneId'] in zones,'Unknown objective zone')
            else:
                target=p['objectiveId'];require(target in objectives and target not in trail,'Unknown or cyclic objective');predicate(objectives[target]['predicate'],trail|{target})
    for o in objectives.values():predicate(o['predicate'],{o['id']})
    if data['admission']['status']!='reference-only':
        require(all(b['grid']['coverage']=='complete' and b['assembly']['rotationDegrees'] is not None and b['assembly']['translationMeters'] is not None for b in boards.values()),'Playable setup requires complete board geometry')
        require(all(h['terrain']['base']!='unknown' and all(e['barrier']!='unknown' and e['crossing']!='unknown' for e in h['edges']) for h in hexes.values()),'Playable setup has unknown terrain')
        require(all(z['resolution']=='resolved' for z in zones.values()),'Playable setup has unresolved zones')
    if data['admission']['status']=='engine-ready':
        require(not data['admission']['blockers'] and data['rules']['combatEngine'] and data['rules']['terrainRuleProfile'],'Engine admission incomplete')
        require(all(c['organization']['decompositionStatus']=='verified' for c in counters.values()),'Unresolved force decomposition')
    if 'parentCampaign' in data:
        parent=data['parentCampaign']
        require(parent['startDate']<=data['situation']['printedDate']<=parent['endDate'],'Situation date lies outside parent campaign window')
    return data

def validate_registry(registry,data):
    def check(ok,message):
        if not ok:raise ValueError(message)
    check(registry.get('version')==1 and isinstance(registry.get('campaigns'),list),'Invalid campaign registry')
    campaign_ids=set();situation_ids=set();matched=False
    for campaign in registry['campaigns']:
        check(campaign['id'] not in campaign_ids,'Duplicate campaign ID');campaign_ids.add(campaign['id'])
        numbers=set()
        for situation in campaign['situations']:
            number=situation['number']
            check(type(number) is int and number>0,'Invalid Situation number')
            check(number not in numbers,'Duplicate Situation number within campaign');numbers.add(number)
            check(situation['id'] not in situation_ids,'Duplicate Situation ID');situation_ids.add(situation['id'])
            if situation['id']==data['campaignAssignment']['situationId']:
                check(campaign['id']==data['parentCampaign']['id']==data['campaignAssignment']['campaignId'],'Campaign assignment mismatch')
                check(number==data['campaignAssignment']['number'] and situation['sourcePackageId']==data['id'],'Situation assignment mismatch');matched=True
        check(type(campaign['nextSituationNumber']) is int and campaign['nextSituationNumber']>max(numbers,default=0),'Next Situation number must not reuse an assigned number')
    check(matched,'Situation is not registered in its campaign')
    return registry

def build():
    data=validate(json.loads((ROOT/'sources/situations/panzer-leader-04.json').read_text(encoding='utf-8')))
    registry=validate_registry(json.loads((ROOT/'sources/campaign-registry.json').read_text(encoding='utf-8')),data)
    (ROOT/'panzer-situation-data.js').write_text('window.PANZER_SITUATION_DATA='+json.dumps(data,separators=(',',':'))+';\nwindow.CAMPAIGN_REGISTRY='+json.dumps(registry,separators=(',',':'))+';\n',encoding='utf-8',newline='\n')
    (ROOT/'assets/panzer-leader-04/counter-manifest.json').write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8',newline='\n')
    print('Validated and built Situation '+data['id']+' ('+data['admission']['status']+')')
if __name__=='__main__':build()
