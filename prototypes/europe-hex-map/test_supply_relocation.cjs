const fs=require('fs'),vm=require('vm'),assert=require('node:assert/strict');
const c=vm.createContext({window:{},console,Map,Set});function load(f){vm.runInContext(fs.readFileSync(f,'utf8'),c);Object.assign(c,c.window);}
for(const f of JSON.parse(fs.readFileSync('situation-data/scripts.json')))load(f);
for(const f of ['asl-counter-definitions','organization-profiles','library-profiles','source-composition','scenario-affiliation','tactical-estimates','scenario-troop-quality','spatial-frame','terrain-refinement','deployment-state'])load('tactical/'+f+'.js');
for(const f of ['campaign/supply-relocation.js','campaign/route-network.js','campaign/supply-placement.js','campaign/supply-spatial.js','campaign/supply-deployment.js','panzer-situation-state.js','tactical/squad-lifecycle.js'])load(f);
const P=vm.runInContext('PanzerSituationModel',c);

const d=c.PANZER_SITUATION_LIBRARY.find(x=>x.situation.number===5),plan=P.create(d);c.CampaignSupplyRelocation.bind(d,plan);
const original=c.CampaignSupplySpatial.build(d),net=original.sides.German,frame=original.layout.find(x=>x.boardId===net.boardId),board=d.boards.find(x=>x.id===net.boardId);
const h=board.hexes.find(h=>h.id!==net.nodes.find(n=>n.kind==='hq').hexId&&h.setupAllowed&&!['water','marsh'].includes(h.terrain.base));
const target=c.SquadSpace.project(frame,h.imageCenter.u,h.imageCenter.v);
plan.supplyAdjustments=c.CampaignSupplyRelocation.move(d,plan,'German','hq',target);c.CampaignSupplyRelocation.bind(d,plan);
assert.equal(c.CampaignSupplySpatial.build(d).sides.German.nodes.find(n=>n.kind==='hq').hexId,h.id);
const saved=JSON.parse(JSON.stringify(plan));c.CampaignSupplyRelocation.bind(d,saved);assert.equal(c.CampaignSupplySpatial.build(d).sides.German.nodes.find(n=>n.kind==='hq').hexId,h.id);
assert.throws(()=>c.CampaignSupplyRelocation.move(d,saved,'German','hq',{x:-10000,y:-10000}),/land hex/);
let moved=null;for(const h of board.hexes.filter(h=>h.setupAllowed&&!['water','marsh'].includes(h.terrain.base)&&h.id!==net.receivingArea.center.hexId)){try{const p=c.SquadSpace.project(frame,h.imageCenter.u,h.imageCenter.v);moved=c.CampaignSupplyRelocation.move(d,saved,'German','supply',p);break;}catch{}}
assert.ok(moved,'connected relocation candidate');saved.supplyAdjustments=moved;c.CampaignSupplyRelocation.bind(d,saved);
const result=c.CampaignSupplySpatial.build(d).sides.German;
assert.equal(result.receivingArea.radius,2);assert.notEqual(result.receivingArea.center.hexId,net.receivingArea.center.hexId);
assert.ok(c.CampaignSupplyRelocation.retained(d,'German').length);assert.equal(JSON.stringify(result.points.at(-1)),JSON.stringify(result.nodes.find(n=>n.kind==='supply').position));
console.log('PASS HQ move, save/reload, invalid target, connected SUP route and retained setup area');

// The visible road from the German rear connects to Wiln, beyond the initial SUP.
for(const hexId of ['J-7','K-6','K-7']){const h=board.hexes.find(h=>h.id===hexId);const changes=c.CampaignSupplyRelocation.move(d,saved,'German','supply',c.SquadSpace.project(frame,h.imageCenter.u,h.imageCenter.v));assert.equal(changes.German.supply.hexId,hexId);}
console.log('PASS receiving-point extension along the connected road into Wiln');

// Repeated validation/render reads reuse the route; changed saves must invalidate it.
const routeTo=c.CampaignRouteNetwork.to;let routeCalls=0;
c.CampaignRouteNetwork.to=(...args)=>{routeCalls++;return routeTo(...args);};
const start=performance.now(),first=c.CampaignSupplySpatial.build(d),initialCalls=routeCalls;
for(let i=0;i<100;i++)assert.equal(c.CampaignSupplySpatial.build(d),first);
assert.equal(routeCalls,initialCalls,'unchanged reads must not rebuild roads');
const copy=JSON.parse(JSON.stringify(saved));c.CampaignSupplyRelocation.bind(d,copy);
assert.equal(c.CampaignSupplySpatial.build(d),first,'equivalent imported save reuses result');
copy.supplyAdjustments.German.hq={boardId:board.id,hexId:net.nodes.find(n=>n.kind==='hq').hexId};
const changed=c.CampaignSupplySpatial.build(d);
assert.notEqual(changed,first,'in-place adjustment edit invalidates result');
assert.equal(changed.sides.German.nodes.find(n=>n.kind==='hq').hexId,net.nodes.find(n=>n.kind==='hq').hexId);
const baseCopy=JSON.parse(JSON.stringify(original));
assert.notEqual(c.CampaignSupplyRelocation.apply(d,baseCopy),changed,'new base binding invalidates result');
c.CampaignSupplyRelocation.bind(d,{...saved,supplyAdjustments:null});
assert.equal(c.CampaignSupplyRelocation.apply(d,original),original,'cleared adjustments restore base');
c.CampaignRouteNetwork.to=routeTo;
console.log('PASS route reuse and changed-save/base/reset invalidation; 100 cached reads verified');
