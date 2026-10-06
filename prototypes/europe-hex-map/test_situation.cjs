const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
const ctx=vm.createContext({});for(const name of ['regional-state','situation-state'])vm.runInContext(fs.readFileSync(__dirname+'/'+name+'.js','utf8'),ctx);
const R=vm.runInContext('RegionalModel',ctx),S=vm.runInContext('SituationModel',ctx),config=JSON.parse(fs.readFileSync(__dirname+'/regional-campaign.json','utf8'));
const state=R.create(config,'campaign-a','geo'),mission=state.missions.find(m=>m.id==='1-18-approaches'),sector=config.cells[0];
assert.throws(()=>S.create(state,mission.id,sector,config));mission.status='planned';
assert.throws(()=>S.create(state,mission.id,null,config));
const card=S.create(state,mission.id,sector,config);state.situations=[card];
assert.equal(card.assets.length,9);assert.equal(card.reserveIds.length,3);S.decompose(card);
assert.equal(card.engagements.length,2);assert.equal(card.reservations.length,6);
assert.equal(new Set(card.reservations.map(r=>r.assetId)).size,6);assert.throws(()=>S.decompose(card));
assert.throws(()=>S.checkAllocation(card,{assetIds:card.engagements[0].assetIds,window:{startMinute:30,endMinute:80}}));
assert.throws(()=>S.checkAllocation(card,{assetIds:card.reserveIds,window:{startMinute:0,endMinute:60}}));
assert.throws(()=>S.checkAllocation(card,{assetIds:['unknown'],window:{startMinute:0,endMinute:60}}));
assert.throws(()=>S.checkAllocation(card,{assetIds:[],window:{startMinute:0,endMinute:130}}));
for(const e of card.engagements){const d=S.draft(card,e.id);assert.equal(d.executable,false);assert.equal(d.scenarioCard.format,'asl-scenario-card/1');assert.equal(d.admission.status,'blocked');assert.equal(d.assetBindings.length,3);assert.equal(d.scenarioCard.turns,null);assert.equal(d.parent.situationRevision,card.revision);assert.equal(d.reconciliation.status,'not-implemented');}
assert.equal(card.outcome,'unassessed');assert.equal(R.validate(JSON.parse(JSON.stringify(state)),config,'campaign-a','geo').situations.length,1);
const wrong=JSON.parse(JSON.stringify(card));wrong.campaignSeed='campaign-b';assert.throws(()=>S.validate(wrong,state));
assert.equal(R.create(config,'campaign-b','geo').situations,undefined);
console.log('Situation checks passed: lineage, identity conservation, reserves, overlapping allocations, windows, blocked draft exports and persistence.');
