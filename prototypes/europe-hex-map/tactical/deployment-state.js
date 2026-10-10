"use strict";
window.SquadDeployment = (() => {
 const version=1, limit=2;
 function signature(d,plan){
  return JSON.stringify({id:d.id,seed:d.illustration.seed,layout:d.illustration.joinedLayout,profiles:SquadProfiles.version,
   placements:Object.entries(plan.placements).sort(([a],[b])=>a.localeCompare(b))});
 }
 function parent(d,plan,id){
  const instance=d.instances.find(i=>i.id===id),profile=SquadProfiles.types[instance?.counterTypeId],placement=plan.placements[id];
  if(!profile||!placement)throw Error("Select a deployed, supported formation counter.");
  if(plan.engagements.some(e=>e.counterIds.includes(id)))throw Error("This counter is already assigned to an engagement draft.");
  const board=SquadSpace.layout(d).find(p=>p.boardId===placement.board);
  return {id,typeId:instance.counterTypeId,anchor:SquadSpace.hex(SquadSpace.project(board,placement.x,placement.y)),radius:profile.radius};
 }
 function legal(state,terrain,unit,h){
  const p=state.parents.find(p=>p.id===unit.parentId),cell=terrain.cells.get(SquadSpace.id(h));
  if(!p||!cell||SquadSpace.distance(h,p.anchor)>p.radius)return false;
  if(cell.terrain==="water"||unit.kind==="vehicle"&&cell.terrain==="marsh")return false;
  const occupants=state.units.filter(u=>u.id!==unit.id&&u.hex&&SquadSpace.id(u.hex)===SquadSpace.id(h));
  return occupants.length<limit&&!occupants.some(u=>u.side!==unit.side);
 }
 function add(state,terrain,d,plan,id){
  if(state.parents.some(p=>p.id===id))return state;
  const p=parent(d,plan,id),profile=SquadProfiles.types[p.typeId];
  if(state.scope!=="full"&&SquadSpace.distance(p.anchor,state.center)+p.radius>state.radius)throw Error("That formation is outside this local footprint.");
  const next=JSON.parse(JSON.stringify(state)),type=d.counters.find(c=>c.id===p.typeId);
  next.parents.push(p);
  for(let i=0;i<profile.count;i++){
   const unit={id:id+"/squad-"+(i+1),parentId:id,kind:profile.kind,label:profile.label+" "+(i+1),side:type.side,nationality:type.nationality||(type.side==="German"?"de":"us"),hex:null};
   // Prefer unoccupied cells, then permit legal friendly stacking.
   const options=SquadSpace.disk(p.anchor,p.radius);
   const candidate=options.find(h=>legal(next,terrain,unit,h)&&!next.units.some(u=>u.hex&&SquadSpace.id(u.hex)===SquadSpace.id(h)))||options.find(h=>legal(next,terrain,unit,h));
   if(!candidate)throw Error("Insufficient legal space for "+id+". No counters were added.");
   unit.hex=candidate;next.units.push(unit);
  }
  return next;
 }
 function create(d,plan,id){
  const p=parent(d,plan,id),state={version,situationId:d.id,signature:signature(d,plan),profileVersion:SquadProfiles.version,terrainVersion:"source-grid-1",seed:d.illustration.seed,center:p.anchor,radius:14,parents:[],units:[]};
  return add(state,SquadTerrain.create(d,state.center),d,plan,id);
 }
 function validate(s,d,plan){
  if(s.version!==version||s.signature!==signature(d,plan)||s.terrainVersion!=="source-grid-1"||s.profileVersion!==SquadProfiles.version||s.seed!==d.illustration.seed||s.situationId!==d.id)throw Error("The formation plan or profile changed. Export or clear the saved squad setup before creating another.");
  if(!Number.isInteger(s.center?.q)||!Number.isInteger(s.center?.r)||s.radius!==14||!s.parents?.length||!Array.isArray(s.units))throw Error("Invalid squad setup.");
  const terrain=SquadTerrain.create(d,s.center,s.scope==="full"?null:14),seen=new Set();
  let expectedCount=0;
  for(const p of s.parents){
   const expected=parent(d,plan,p.id);
   if(seen.has(p.id)||JSON.stringify(p)!==JSON.stringify(expected)||(s.scope!=="full"&&SquadSpace.distance(p.anchor,s.center)+p.radius>s.radius))throw Error("Invalid parent deployment area.");
   seen.add(p.id);const profile=SquadProfiles.types[p.typeId];expectedCount+=profile.count;
   const type=d.counters.find(c=>c.id===p.typeId);
   for(let n=1;n<=profile.count;n++){
    const matches=s.units.filter(u=>u.id===p.id+"/squad-"+n),u=matches[0];
    if(matches.length!==1||u.parentId!==p.id||u.kind!==profile.kind||u.side!==type.side||!Number.isInteger(u.hex?.q)||!Number.isInteger(u.hex?.r)||!legal(s,terrain,u,u.hex))throw Error("Invalid subordinate placement or roster.");
   }
  }
  if(expectedCount!==s.units.length)throw Error("Invalid subordinate count.");
  return s;
 }
 function expandAll(state,d,plan){
  if(d.instances.some(i=>!plan.placements[i.id]||!SquadProfiles.types[i.counterTypeId]))throw Error("Deploy all formation counters before expanding the full Situation.");
  let next={...JSON.parse(JSON.stringify(state)),scope:"full"};
  const terrain=SquadTerrain.create(d,next.center,null);
  for(const i of d.instances)next=add(next,terrain,d,plan,i.id);
  return validate(next,d,plan);
 }
 function move(state,terrain,id,h){
  const u=state.units.find(u=>u.id===id);
  if(!u||!legal(state,terrain,u,h))throw Error("Outside this unit's deployment area, blocked terrain, or stacking limit reached.");
  u.hex={q:h.q,r:h.r};
 }
 return {signature,parent,legal,add,create,validate,move,limit,expandAll};
})();

