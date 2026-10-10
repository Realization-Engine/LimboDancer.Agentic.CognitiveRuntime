const fs=require("node:fs"),vm=require("node:vm"),assert=require("node:assert/strict"),path=require("node:path");
const ctx=vm.createContext({window:{},Map,Set,console}),root=__dirname;
for(const f of [...JSON.parse(fs.readFileSync(path.join(root,"situation-data/scripts.json"))),"panzer-situation-state.js"])vm.runInContext(fs.readFileSync(path.join(root,f),"utf8"),ctx);
for(const f of ["organization-profiles","spatial-frame","terrain-refinement","deployment-state","engagement-repository"]){
 vm.runInContext(fs.readFileSync(path.join(root,"tactical",f+".js"),"utf8"),ctx);
 Object.assign(ctx,ctx.window);
}
const d=ctx.window.PANZER_SITUATION_DATA,M=ctx.SquadDeployment,S=ctx.SquadSpace,T=ctx.SquadTerrain;
const plan={placements:{},engagements:[]};
const bd=d.boards.find(b=>b.id==="A"),h=bd.hexes.find(h=>h.setupAllowed&&h.terrain.base==="open"&&h.imageCenter.u>.3&&h.imageCenter.u<.7&&h.imageCenter.v>.4&&h.imageCenter.v<.6);
for(const id of ["de-rifle-01","de-rifle-02","de-engineer-01"])plan.placements[id]={board:"A",hexId:h.id,x:h.imageCenter.u,y:h.imageCenter.v};
let state=M.create(d,plan,"de-rifle-01"),terrain=T.create(d,state.center);
assert.equal(state.units.length,4);M.validate(state,d,plan);
for(const p of S.layout(d))for(const uv of [{u:.1,v:.2},{u:.7,v:.8}]){
 const round=S.unproject(p,S.project(p,uv.u,uv.v));assert.ok(Math.abs(round.u-uv.u)<1e-9&&Math.abs(round.v-uv.v)<1e-9);
}
assert.equal(S.disk({q:0,r:0},4).length,61);
for(const h of S.disk({q:0,r:0},10))assert.deepEqual(S.hex(S.xy(h)),h);
assert.deepEqual(M.create(d,plan,"de-rifle-01"),state);
state=M.add(state,terrain,d,plan,"de-engineer-01");assert.equal(state.units.length,8);
assert.equal(M.add(state,terrain,d,plan,"de-engineer-01").units.length,8);
const u=state.units[0],anchor=state.parents[0].anchor,before=JSON.stringify(u.hex);
assert.throws(()=>M.move(state,terrain,u.id,{q:anchor.q+5,r:anchor.r}),/Outside/);assert.equal(JSON.stringify(u.hex),before);
const boundary=S.disk(anchor,4).find(h=>S.distance(h,anchor)===4&&M.legal(state,terrain,u,h));
assert.ok(boundary);M.move(state,terrain,u.id,boundary);
const target=state.units[1].hex;M.move(state,terrain,u.id,target);
assert.throws(()=>M.move(state,terrain,state.units[2].id,target),/stacking/);
M.validate(JSON.parse(JSON.stringify(state)),d,plan);
const corrupt=JSON.parse(JSON.stringify(state));corrupt.units.push(corrupt.units[0]);assert.throws(()=>M.validate(corrupt,d,plan),/Invalid/);
const changed=JSON.parse(JSON.stringify(plan));changed.placements["de-rifle-01"].x+=.01;assert.throws(()=>M.validate(state,d,changed),/changed/);
const blocked={...terrain,cells:new Map([...terrain.cells].map(([k,h])=>[k,{...h,terrain:"water"}]))};
const snapshot=JSON.stringify(state);assert.throws(()=>M.add(state,blocked,d,plan,"de-rifle-02"),/Insufficient/);assert.equal(JSON.stringify(state),snapshot);
const storage=new Map();ctx.localStorage={getItem:k=>storage.get(k)||null,setItem:(k,v)=>storage.set(k,v),removeItem:k=>storage.delete(k)};
ctx.SquadRepository.save(d,plan,state);assert.deepEqual(ctx.SquadRepository.load(d,plan),state);
console.log("PASS: deterministic decomposition, rotations, 40m grid, overlapping areas, radius boundary, stacking, atomic failure, stale saves and persistence.");

const fullPlan=JSON.parse(fs.readFileSync(path.join(root,"tactical/st-lo-deployment.fixture.json")));
const initial=M.create(d,fullPlan,"de-rifle-01"),full=M.expandAll(initial,d,fullPlan);
assert.equal(full.parents.length,76);assert.equal(full.units.length,335);
assert.deepEqual(M.expandAll(full,d,fullPlan),full);
assert.deepEqual(full.units.find(u=>u.id===initial.units[0].id).hex,initial.units[0].hex);
assert.equal(initial.parents.length,1);
M.validate(full,d,fullPlan);ctx.SquadRepository.save(d,fullPlan,full);assert.deepEqual(ctx.SquadRepository.load(d,fullPlan),full);
for(const u of full.units){const p=full.parents.find(p=>p.id===u.parentId);assert.ok(S.distance(p.anchor,u.hex)<=4);}
vm.runInContext(fs.readFileSync(path.join(root,"tactical/asl-counter-definitions.js"),"utf8"),ctx);
const definitions=ctx.window.SquadAslDefinitions.types;
const catalog=JSON.parse(fs.readFileSync(path.join(root,"../../src/ASL/units/catalog/scenario-a1.catalog.json")));
for(const type of d.counters){assert.ok(definitions[type.id]);assert.ok(fs.existsSync(path.join(root,d.assetBase||"assets/panzer-leader-04/",type.artwork)));}
for(const def of Object.values(definitions).filter(d=>d.status==="catalog match")){
 const source=catalog.definitions.find(d=>d.id===def.definitionId);
 const values=Object.fromEntries(source.values.filter(v=>v.face==="front"&&v.attribute&&Object.hasOwn(v,"value")).map(v=>[v.attribute,v.value]));
 assert.equal(JSON.stringify(def.values),JSON.stringify(values));
}
console.log("PASS: all 76 St. Lo parents, 335 children, fixed radii, idempotent full deployment, persistence, artwork paths and ASL catalog matches.");
