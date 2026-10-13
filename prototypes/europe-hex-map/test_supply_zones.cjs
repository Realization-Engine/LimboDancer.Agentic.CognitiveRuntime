const fs=require('fs'),vm=require('vm'),assert=require('node:assert/strict');
const c=vm.createContext({window:{},console,Map,Set});function load(f){vm.runInContext(fs.readFileSync(f,'utf8'),c);Object.assign(c,c.window);}
for(const f of JSON.parse(fs.readFileSync('situation-data/scripts.json')))load(f);
for(const f of ['tactical/spatial-frame.js','campaign/supply-placement.js','campaign/supply-spatial.js','panzer-situation-state.js'])load(f);
const P=vm.runInContext('PanzerSituationModel',c),S=c.CampaignSupplySpatial;
for(const d of c.PANZER_SITUATION_LIBRARY){
 const b=S.build(d),plan=P.create(d);for(const side of d.setupOrder){P.autoDeploySide(plan,d);if(!P.complete(plan,d,side)){for(const i of P.ground(d).filter(i=>d.counters.find(t=>t.id===i.counterTypeId).side===side&&!plan.placements[i.id])){const board=d.boards.find(b=>b.id===d.setupBoards[side]),h=board.hexes.find(h=>h.setupAllowed&&!Object.values(plan.placements).some(p=>p.board===board.id&&p.hexId===h.id)&&P.allowed(d,i.id,board.id,h.id,plan));P.place(plan,d,i.id,board.id,h.imageCenter.u,h.imageCenter.v);}}P.next(plan,d);}P.validate(plan,d);
 const zones=c.CampaignSupplyPlacement.zones(d,b.layout);
 for(const side of ['Allied','German']){const net=b.sides[side],enemy=side==='Allied'?'German':'Allied',cells=zones.cells.filter(h=>h.boardId===net.boardId);
  for(const p of [...net.points,...net.entryPath,...net.nodes.map(n=>n.position)]){const h=cells.reduce((a,h)=>!a||Math.hypot(h.p.x-p.x,h.p.y-p.y)<Math.hypot(a.p.x-p.x,a.p.y-p.y)?h:a,null);assert.ok(zones.sets[side].has(h.id),d.situation.number+' '+side+' outside own zone '+h.id);assert.ok(!zones.sets[enemy].has(h.id),d.situation.number+' '+side+' enemy zone '+h.id);}
  assert.equal(c.CampaignSupplyPlacement.conflicts(d,b,side,plan.placements).length,0,d.situation.number+' '+side+' opposing deployed troops');
  const opponent=d.instances.find(i=>d.counters.find(t=>t.id===i.counterTypeId).side===enemy),uv=c.SquadSpace.unproject(b.layout.find(p=>p.boardId===net.boardId),net.points[0]);assert.ok(c.CampaignSupplyPlacement.conflicts(d,b,side,{[opponent.id]:{board:net.boardId,x:uv.u,y:uv.v}}).length);
 }
 const legacy=JSON.parse(JSON.stringify(b));legacy.version='board-supply-2';legacy.sides.Allied.nodes[0].position={x:-999,y:-999};assert.equal(S.resolve(d,legacy).version,S.version);assert.equal(legacy.sides.Allied.nodes[0].position.x,-999);
 console.log('PASS',d.situation.number,d.situation.title,'both sides, source zones, deployed forces, legacy map correction');
}
