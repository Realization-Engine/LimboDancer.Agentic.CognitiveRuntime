const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict'),path=require('node:path');
const ctx=vm.createContext({window:{}});for(const f of ['panzer-situation-data.js','panzer-situation-state.js'])vm.runInContext(fs.readFileSync(path.join(__dirname,f),'utf8'),ctx);
const M=vm.runInContext('PanzerSituationModel',ctx),d=ctx.window.PANZER_SITUATION_LIBRARY.find(d=>d.situation.number===14),s=M.create(d);
const type=d.counters.find(c=>c.label==='Armored infantry'&&c.side===s.stage),units=d.instances.filter(i=>i.counterTypeId===type.id),b=d.boards.find(b=>M.boards(d,units[0].id).includes(b.id)),h=b.hexes.find(h=>units.every(i=>M.allowed(d,i.id,b.id,h.id,s)));
assert.ok(h);M.place(s,d,units[0].id,b.id,h.imageCenter.u,h.imageCenter.v);const before=JSON.stringify(s.placements[units[0].id]);
M.deployType(s,d,type.id,b.id,h.id);assert.equal(JSON.stringify(s.placements[units[0].id]),before);assert.ok(units.every(i=>s.placements[i.id]));assert.equal(s.placements[units[1].id].hexId,h.id);M.validate(s,d);
const dist=p=>{const x=b.hexes.find(h=>h.id===p.hexId);return Math.max(Math.abs(x.q-h.q),Math.abs(x.r-h.r),Math.abs(x.q+x.r-h.q-h.r));};const rings=units.slice(1).map(i=>dist(s.placements[i.id]));assert.equal(JSON.stringify(rings),JSON.stringify([...rings].sort((a,b)=>a-b)));
const other=d.instances.find(i=>i.counterTypeId!==type.id&&d.counters.find(c=>c.id===i.counterTypeId).side===s.stage&&!(d.deployment.loads||[]).some(l=>l.passengerId===i.id||l.carrierId===i.id));const ref=d.deployment.allowedHexes[other.id][0],ob=d.boards.find(b=>b.id===ref.boardId),oh=ob.hexes.find(h=>h.id===ref.hexId);M.place(s,d,other.id,ob.id,oh.imageCenter.u,oh.imageCenter.v);const otherBefore=JSON.stringify(s.placements[other.id]);M.clearType(s,d,type.id);assert.ok(units.every(i=>!s.placements[i.id]));assert.equal(JSON.stringify(s.placements[other.id]),otherBefore);
const restricted=JSON.parse(JSON.stringify(d));for(const i of units)restricted.deployment.allowedHexes[i.id]=[{boardId:b.id,hexId:h.id}];const previous=JSON.stringify(s.placements);assert.throws(()=>M.deployType(s,restricted,type.id,b.id,h.id),/Not enough/);assert.equal(JSON.stringify(s.placements),previous);
console.log('Bulk deployment: remaining-only, origin, ring ordering, legal placements, type-scoped clear and atomic failure passed.');


const automatic=M.create(d);M.autoDeploySide(automatic,d);M.validate(automatic,d);
for(const id of ['us-engineer-01','us-engineer-02','us-engineer-03','us-engineer-04','us-engineer-05','us-engineer-06']){const pos=automatic.placements[id];assert.ok(pos);assert.ok(d.deployment.allowedHexes[id].some(r=>r.boardId===pos.board&&r.hexId===pos.hexId));}
const engineer=d.counters.find(c=>c.id==='us-engineer');M.clearType(automatic,d,engineer.id);M.autoDeploySide(automatic,d);assert.ok(!automatic.placements['us-engineer-01']);M.autoDeploy(automatic,d,engineer.id);assert.ok(automatic.placements['us-engineer-06']);M.validate(automatic,d);
console.log('Automatic side setup respects both engineer groups; Clear All survives re-entry; Deploy All restores prescribed positions.');
