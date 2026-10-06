const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
const c=JSON.parse(fs.readFileSync(__dirname+'/regional-campaign.json','utf8'));
const ctx=vm.createContext({});vm.runInContext(fs.readFileSync(__dirname+'/regional-state.js','utf8'),ctx);const R=vm.runInContext('RegionalModel',ctx),s=R.create(c,'seed','base');
assert.equal(c.campaign.scope.amphibiousAssault,false);
assert.equal(c.forces.filter(f=>f.echelon==='Division').length,2);
assert.equal(c.forces.filter(f=>f.echelon==='Regiment').length,6);
assert.equal(c.forces.filter(f=>f.echelon==='Battalion').length,18);
for(const id of ['1-id','29-id']){
 const summary=R.divisionSummary(s,id);assert.equal(summary.length,3);assert.ok(summary.every(r=>r.battalions===3&&r.status==='draft'));
}
assert.equal(new Set(c.forces.map(f=>f.id)).size,c.forces.length);
for(const f of c.forces.filter(f=>['Regiment','Battalion'].includes(f.echelon))){
 const m=c.missions.filter(m=>m.recipient===f.id);assert.equal(m.length,1);assert.equal(m[0].issuer,f.parentId);
 assert.ok(c.sources.some(x=>x.id===f.sourceId));
}
assert.equal(c.forces.filter(f=>f.id==='3-26').length,1);assert.equal(c.forces.find(f=>f.id==='3-26').parentId,'26-ir');
assert.ok(c.historicalAttachments.some(a=>a.unitId==='3-26'&&a.toId==='18-ir'));
// Exercise the newly playable western chain and verify delayed HQ knowledge.
for(const id of ['beachhead','westward','175-advance']){const m=s.missions.find(x=>x.id===id);R.act(s,id,'assign',m.issuer);R.advance(s);R.act(s,id,'plan',m.recipient,'Coordinate inland operations');}
assert.equal(R.divisionSummary(s,'29-id').find(x=>x.id==='175-ir').status,'assigned');
R.advance(s);assert.equal(R.divisionSummary(s,'29-id').find(x=>x.id==='175-ir').status,'received');
assert.equal(R.validate(JSON.parse(JSON.stringify(s)),c,'seed','base').forces.length,c.forces.length);
assert.ok(c.forces.every(f=>f.strength===null&&f.position===null));
assert.equal(s.clock.current,c.campaign.opening);
console.log('Historical divisions passed: six regiments, eighteen unique battalions, complete mission paths, delayed reports, persistence, no invented strengths or amphibious phase.');
