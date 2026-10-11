const fs=require('fs'),vm=require('vm'),assert=require('node:assert/strict'),path=require('path');
const c=vm.createContext({window:{},console,crypto:require('node:crypto').webcrypto,Map,Set});
for(const f of [...JSON.parse(fs.readFileSync(path.join(__dirname,'situation-data/scripts.json'))),'panzer-situation-state.js','campaign-world.js','normandy/command-state.js','normandy/situation.js','normandy/repository.js'])vm.runInContext(fs.readFileSync(path.join(__dirname,f),'utf8'),c);
for(const f of ['asl-counter-definitions','organization-profiles','library-profiles','source-composition','tactical-estimates','spatial-frame','terrain-refinement','deployment-state']){vm.runInContext(fs.readFileSync(path.join(__dirname,'tactical',f+'.js'),'utf8'),c);Object.assign(c,c.window);}
const M=vm.runInContext('NormandyCommand',c),V=vm.runInContext('NormandySituation',c),P=vm.runInContext('PanzerSituationModel',c),R=vm.runInContext('NormandyRepository',c),source=c.window.PANZER_SITUATION_DATA;
const original=JSON.stringify(source);let serial=0;
for(const choice of ['baseline','supplied','reinforced'])for(const staff of [10,20]){
 let s=M.initial('test-'+choice+staff,'operational',staff);const act=(type,data={})=>s=M.execute(s,{id:'n'+ ++serial,revision:s.revision,type,...data});
 assert.throws(()=>act('commit',{choice}),/Wait/);act('allocate',{plan:{supply:'send',reserve:'attach'},sourceHashes:{M04:'test'}});
 while(M.ready(s,choice))act('advance');act('commit',{choice});assert.equal(JSON.stringify(M.restore(s)),JSON.stringify(s));
 const d=V.variant(s,'M04',source);assert.equal(d.instances.length,choice==='baseline'?75:choice==='supplied'?76:78);
 const plan=P.create(d);for(const side of d.setupOrder){P.autoDeploySide(plan,d);assert.ok(P.complete(plan,d,side));P.next(plan,d);}P.launch(plan,d);P.validate(plan,d);
 const full=c.SquadDeployment.expandAll(c.SquadDeployment.create(d,plan,d.instances[0].id),d,plan);assert.equal(full.parents.length,d.instances.length);
 console.log('PASS',choice,staff,'minutes:',d.instances.length,'formations',full.units.length,'subordinates');
}
assert.equal(JSON.stringify(source),original);
let s=M.initial('retain');s=M.execute(s,{id:'a',revision:0,type:'allocate',plan:{supply:'retain',reserve:'retain'},sourceHashes:{M04:'test'}});while(s.queue.length)s=M.execute(s,{id:'q'+ ++serial,revision:s.revision,type:'advance'});assert.equal(M.ready(s,'baseline'),'');assert.match(M.ready(s,'supplied'),/retained/);assert.equal(M.at(s,0).missions.M04.received,false);
assert.throws(()=>M.execute(M.initial('bad'),{id:'b',revision:0,type:'allocate',plan:{supply:'retain',reserve:'attach'},sourceHashes:{M04:'test'}}),/fuel/);
const storage=new Map([['campaign-atlas:world:v1:active','ardennes-unchanged']]),store={getItem:k=>storage.get(k)||null,setItem:(k,v)=>storage.set(k,v)};
const repo=R.repository(store);repo.start('essentials',10);const id=repo.get().id;repo.execute({id:'z',revision:0,type:'allocate',sourceHashes:{M04:'test'}},source);assert.equal(storage.get('campaign-atlas:world:v1:active'),'ardennes-unchanged');const other=R.repository(store);other.load();repo.save({...repo.get(),notes:'persist'});assert.throws(()=>other.save(other.get()),/another tab/);assert.equal(R.repository(store).load().notes,'persist');repo.start('operational',20);assert.ok(storage.has(R.KEY+id));
console.log('PASS source immutability, supplies, replay, save isolation, stale writes and retained prior operation.');
