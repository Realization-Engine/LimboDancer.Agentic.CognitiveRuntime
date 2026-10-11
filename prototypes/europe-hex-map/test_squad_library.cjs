const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict'),path=require('node:path');
const ctx=vm.createContext({window:{},console,Map,Set});
for(const f of JSON.parse(fs.readFileSync(path.join(__dirname,'situation-data/scripts.json'))))vm.runInContext(fs.readFileSync(path.join(__dirname,f),'utf8'),ctx);
vm.runInContext(fs.readFileSync(path.join(__dirname,'panzer-situation-state.js'),'utf8'),ctx);
const P=vm.runInContext('PanzerSituationModel',ctx);
for(const f of ['asl-counter-definitions','organization-profiles','library-profiles','source-composition','tactical-estimates','spatial-frame','terrain-refinement','deployment-state','engagement-repository']){vm.runInContext(fs.readFileSync(path.join(__dirname,'tactical',f+'.js'),'utf8'),ctx);Object.assign(ctx,ctx.window);}
const M=ctx.SquadDeployment;let total=0;
// Independent regression expectations transcribed from the original table, printed pp.19-22.
for(const [id,n] of Object.entries({'de-pziv-w':4,'de-pziv':5,'de-panther-w':4,'de-panther':5,'de-tiger':5,'de-kingtiger':5,'uk-sherman':4,'uk-cromwell-goodwood':4,'uk-bren':6,'uk-sexton':6,'uk-25pdr':6,'de-quad20':3,'de-grille':2,'de-maultier-goodwood':6,'de-234-2':7,'us-m16':6,'us-m8':6,'ca-76mortar':4}))assert.equal(ctx.SquadProfiles.types[id].count,n,id);
assert.equal(ctx.SquadProfiles.types['de-panther'].sourceDesignation,'SS');assert.equal(ctx.SquadProfiles.types['de-panther-w'].affiliation,undefined);
assert.equal(ctx.SquadAslDefinitions.types['uk-bren'].values['movement-type'],'fully-tracked');
assert.equal(ctx.SquadAslDefinitions.types['de-nebel'].values['gun-type'],'rocket');
assert.ok(ctx.SquadAslDefinitions.types['de-quad20'].values.ife>ctx.SquadAslDefinitions.types['de-20mm'].values.ife);
assert.equal(ctx.SquadAslDefinitions.types['de-wespe'].values.cmg,undefined);
assert.equal(ctx.SquadAslDefinitions.types['de-wespe'].values.designation,'Wespe');

function formation(d){
 const s=P.create(d);
 for(const side of d.setupOrder){
  // Spread the test formation roster evenly within its authored setup zones.
  // Deliberately overcrowded formation saves are tested separately.
  for(const i of P.ground(d).filter(i=>d.counters.find(c=>c.id===i.counterTypeId).side===side&&!s.placements[i.id])){
   if(s.placements[i.id]||(d.deployment?.loads||[]).some(l=>l.passengerId===i.id))continue;
   const candidate=d.boards.flatMap(b=>b.hexes.map(h=>({b,h}))).filter(({b,h})=>P.boards(d,i.id).includes(b.id)&&P.allowed(d,i.id,b.id,h.id,s));
   const occupancy=({b,h})=>Object.values(s.placements).filter(p=>p.board===b.id&&p.hexId===h.id).length;
   candidate.sort((a,b)=>occupancy(a)-occupancy(b));const target=candidate[0];assert.ok(target,i.id);P.place(s,d,i.id,target.b.id,target.h.imageCenter.u,target.h.imageCenter.v);
  }
  P.next(s,d);
 }
 P.launch(s,d);return s;
}
for(const d of ctx.window.PANZER_SITUATION_LIBRARY){
 const s=formation(d),ground=M.ground(d);for(const i of ground)assert.ok(ctx.SquadProfiles.types[i.counterTypeId],i.counterTypeId);
 const sourceBefore=JSON.stringify(d);
 const state=M.expandAll(M.create(d,s,ground[0].id),d,s);M.validate(state,d,s);
 assert.equal(JSON.stringify(d),sourceBefore,'Source roster and rules unchanged');
 for(const p of state.parents){assert.ok(ctx.SquadProfiles.types[p.typeId].evidence,'Missing source evidence: '+p.typeId);for(const u of state.units.filter(u=>u.parentId===p.id))assert.ok(ctx.SquadTactical.definition(p.typeId,u));}
 if(d.situation.number===5){const children=state.units.filter(u=>u.parentId==='uk-sherman-01');assert.equal(children.filter(u=>u.definitionId==='uk-sherman').length,3);assert.equal(children.filter(u=>u.definitionId==='uk-firefly').length,1);const bad=JSON.parse(JSON.stringify(state));bad.units.find(u=>u.parentId==='uk-sherman-01').definitionId='de-tiger';assert.throws(()=>M.validate(bad,d,s),/member definition/);const old=JSON.parse(JSON.stringify(state));old.profileVersion='playable-1';assert.throws(()=>M.validate(old,d,s),/profile changed/);}
 assert.equal(state.parents.length,ground.length);assert.deepEqual(M.expandAll(state,d,s),state);
 for(const u of state.units){assert.ok(ctx.SquadAslDefinitions.types[state.parents.find(p=>p.id===u.parentId).typeId]);if(u.loadedOn)assert.deepEqual(u.hex,state.units.find(c=>c.id===u.loadedOn).hex);}
 total+=state.units.length;console.log('PASS',d.situation.number,ground.length,'parents',state.units.length,'children');
}
console.log('PASS: full imported library;',total,'squad-level counters.');
const A=require('./ardennes-command-state.js');let serial=0;
for(const mission of ['M14','M15'])for(const choice of ['baseline','supplied','reinforced']){
 let op=A.initial('squad-integration-'+mission+'-'+choice,'operational',10);
 const act=(type,data={})=>op=A.execute(op,{id:'s'+ ++serial,revision:op.revision,type,...data});
 act('allocate',{plan:{main:mission,reserve:mission,delivery:mission},sourceHashes:{M14:'fixture',M15:'fixture'}});
 while(op.time<50)act('advance');act('commit',{mission,choice});
 const source=ctx.window.PANZER_SITUATION_LIBRARY.find(d=>d.situation.number===Number(mission.slice(1))),d=A.variant(op,mission,source),plan=formation(d);
 const full=M.expandAll(M.create(d,plan,M.ground(d)[0].id),d,plan);
 assert.equal(full.parents.length,d.instances.length);assert.equal(full.units.filter(u=>u.parentId.startsWith('reserve-r1')).length,choice==='reinforced'?8:0);
 const store={},repo=ctx.SquadRepository.scoped({load:()=>store[mission],save:s=>store[mission]=s,clear:()=>delete store[mission]});
 repo.save(d,plan,full);assert.deepEqual(repo.load(d,plan),full);repo.clear();assert.equal(repo.load(d,plan),null);
 console.log('PASS campaign',mission,choice,full.units.length,'children and scoped persistence');
}
