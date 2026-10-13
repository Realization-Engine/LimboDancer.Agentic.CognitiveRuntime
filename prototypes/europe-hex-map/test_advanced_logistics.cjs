const fs=require('fs'),vm=require('vm'),assert=require('node:assert/strict'),path=require('path');
const c=vm.createContext({window:{},console,Map,Set});function load(f){vm.runInContext(fs.readFileSync(path.join(__dirname,f),'utf8'),c);Object.assign(c,c.window);}
for(const f of JSON.parse(fs.readFileSync(path.join(__dirname,'situation-data/scripts.json'))))load(f);
for(const f of ['asl-counter-definitions','organization-profiles','library-profiles','source-composition','tactical-estimates','spatial-frame'])load('tactical/'+f+'.js');
for(const f of ['campaign/geography.js','campaign/route-network.js','campaign/logistics.js','campaign/supply-placement.js','campaign/supply-spatial.js','campaign/supply-state.js','normandy/command-state.js','campaign/source-command.js','ardennes-command-state.js'])load(f);
const N=vm.runInContext('NormandyCommand',c),A=vm.runInContext('ArdennesCommand',c);let serial=0;
const act=(M,s,type,data={})=>M.execute(s,{id:'adv-'+ ++serial,revision:s.revision,type,...data});
for(const kind of ['normandy','source','ardennes'])for(const challenge of ['normal','roadblock','threat']){
 const M=kind==='normandy'?N:kind==='ardennes'?A:c.SourceCommand.create(c.PANZER_SITUATION_LIBRARY.find(d=>d.situation.number===11));let s=M.initial('test-'+kind+'-'+challenge);s=act(M,s,'logistics:enable',{challenge});
 s=act(M,s,'allocate',{plan:kind==='ardennes'?{main:'M14',reserve:'M14',delivery:'M14'}:kind==='source'?{delivery:'standard'}:{supply:'send',reserve:'attach'},sourceHashes:{M04:'x',M14:'x',M15:'x'}});
 let guard=0,held=false;while(s.queue.length&&guard++<40){s=act(M,s,'advance');const route=kind==='ardennes'?'M14':'M04',r=s.logistics.routes[route];if(['blocked','threatened'].includes(r.status)&&!r.repairAt&&!r.escort){held=true;const t=s.transport||s.trips.find(t=>t.status==='held');assert.equal(t.status,'held');assert.ok(!Object.values(s.stock).includes(route));s=act(M,s,r.status==='blocked'?'logistics:repair':'logistics:escort',{route});}}
 assert.ok(guard<40);assert.equal(held,challenge!=='normal');assert.equal(s.stock[kind==='ardennes'?'A14':'ammunition'],kind==='ardennes'?'M14':'M04');assert.ok((s.trips||[s.transport]).every(t=>t.status==='complete'));assert.equal(JSON.stringify(M.restore(s)),JSON.stringify(s));console.log('PASS',kind,challenge,s.time);
}
let s=N.initial('recall');s=act(N,s,'logistics:enable',{challenge:'roadblock'});s=act(N,s,'allocate',{plan:{supply:'send',reserve:'attach'},sourceHashes:{M04:'x'}});while(s.transport?.status!=='held')s=act(N,s,'advance');s=act(N,s,'logistics:recall',{trip:'transport'});assert.equal(s.stock.ammunition,'transit');while(s.queue.length)s=act(N,s,'advance');assert.equal(s.stock.ammunition,'depot');s=act(N,s,'logistics:repair',{route:'M04'});while(s.queue.length)s=act(N,s,'advance');s=act(N,s,'logistics:redispatch',{trip:'transport'});while(s.queue.length)s=act(N,s,'advance');assert.equal(s.stock.ammunition,'M04');assert.equal(JSON.stringify(N.restore(s)),JSON.stringify(s));
load('map-data.js');for(const cell of c.CAMPAIGN_MAP_DATA.cells.filter((_,i)=>i%100===0)){const p=c.CampaignGeography.atlas(c.CampaignGeography.project(cell.lon,cell.lat));assert.ok(Math.hypot(p.x-cell.x,p.y-cell.y)<.003,'Projection agrees with atlas');}
for(const d of c.PANZER_SITUATION_LIBRARY){const b=c.CampaignSupplySpatial.build(d);for(const n of b.sides.Allied.nodes){const p=c.CampaignSupplySpatial.toWorld(b,n.position),back=c.CampaignSupplySpatial.fromWorld(b,p);assert.ok(Math.hypot(back.x-n.position.x,back.y-n.position.y)<1e-7);assert.ok(p.x>2e6&&p.y>1e6);}assert.equal(c.CampaignGeography.footprint(b).length,4);}
console.log('PASS recall cargo conservation, redispatch, deterministic replay, continental projection against atlas cells, all 17 board registrations.');

// A connected loop proves rerouting without inventing an off-road shortcut.
const loop={mapModel:{routes:[{kind:'road',geometry:{boardId:'X',coordinates:[[0,0],[1,0]]}},{kind:'road',geometry:{boardId:'X',coordinates:[[0,0],[0,1],[1,1],[1,0]]}}]}};
const binding={layout:[{boardId:'X',x:0,y:0,w:1130,h:3138,clockwiseDegrees:0}],metresPerUnit:1,sides:{Allied:{boardId:'X',points:[{x:0,y:0},{x:1130,y:0}]}}};
const alternate=c.CampaignRouteNetwork.alternate(loop,binding);assert.ok(alternate&&alternate.points.length===4);assert.ok(alternate.km>1.13);
let r=N.initial('detour');r.supplyProfile.routes[4].binding=binding;r.supplyProfile.routes[4].detour=alternate;
r=act(N,r,'logistics:enable',{challenge:'roadblock'});r=act(N,r,'allocate',{plan:{supply:'send',reserve:'attach'},sourceHashes:{M04:'x'}});while(r.transport?.status!=='held')r=act(N,r,'advance');
const heldPosition=c.CampaignSupplySpatial.point(binding.sides.Allied.points,r.transport.frozenProgress);r=act(N,r,'logistics:reroute',{trip:'transport'});assert.deepEqual(JSON.parse(JSON.stringify(r.transport.detourPoints[0])),JSON.parse(JSON.stringify(heldPosition)));
while(r.queue.length)r=act(N,r,'advance');assert.equal(r.transport.status,'complete');assert.equal(r.stock.ammunition,'M04');assert.equal(JSON.stringify(N.restore(r)),JSON.stringify(r));
load('campaign/geographic-store.js');const mem=new Map(),storage={getItem:k=>mem.get(k)||null,setItem:(k,v)=>mem.set(k,v)};
c.CampaignGeographicStore.publish({id:'geo-test',revision:7,operation:s},storage);const record=c.CampaignGeographicStore.read(storage,4);assert.equal(record.operation.stock.ammunition,'M04');assert.equal(record.registration.crs,'EPSG:3035');assert.equal(record.revision,7);
const b14=c.CampaignSupplySpatial.build(c.PANZER_SITUATION_LIBRARY.find(d=>d.situation.number===14)),b15=c.CampaignSupplySpatial.build(c.PANZER_SITUATION_LIBRARY.find(d=>d.situation.number===15));
for(const kind of ['command','depot','entry']){const a=b14.sides.Allied.nodes.find(n=>n.kind===kind),b=b15.sides.Allied.nodes.find(n=>n.kind===kind),x=c.CampaignSupplySpatial.toWorld(b14,a.position),y=c.CampaignSupplySpatial.toWorld(b15,b.position);assert.equal(a.id,b.id);assert.ok(Math.hypot(x.x-y.x,x.y-y.y)<1e-6);}
console.log('PASS connected detour, no teleportation, return delivery, geographic persistence and shared Ardennes global anchors.');

const legacy=N.initial('legacy', 'operational',10,null);c.CampaignGeographicStore.publish({id:'legacy',revision:0,operation:legacy},storage,[4]);const legacyRecord=c.CampaignGeographicStore.read(storage,4);assert.equal(legacyRecord.operation.supplyProfile,null);assert.equal(legacyRecord.binding.sourceNumber,4);console.log('PASS legacy geographic publication preserves original scheduling contract.');
