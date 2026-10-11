"use strict";
window.SquadDeployment = (() => {
 const version=1, limit=2;
 const ground=d=>d.instances.filter(i=>d.counters.find(c=>c.id===i.counterTypeId)?.kind!=="aircraft");
 function signature(d,plan){
  return JSON.stringify({id:d.id,seed:d.illustration.seed,layout:d.illustration.joinedLayout,profiles:SquadProfiles.version,
   placements:Object.entries(plan.placements).sort(([a],[b])=>a.localeCompare(b))});
 }
 function parent(d,plan,id,settings){
  const instance=d.instances.find(i=>i.id===id),profile=SquadProfiles.types[instance?.counterTypeId],placement=plan.placements[id];
  if(!profile||!placement)throw Error("Select a deployed, supported formation counter.");
  if(plan.engagements.some(e=>e.counterIds.includes(id)))throw Error("This counter is already assigned to an engagement draft.");
  const board=SquadSpace.layout(d).find(p=>p.boardId===placement.board);
  return {id,typeId:instance.counterTypeId,anchor:SquadSpace.hex(SquadSpace.project(board,placement.x,placement.y)),radius:profile.kind==="equipment"?0:(settings?.deploymentRadius??profile.radius)};
 }
 function legal(state,terrain,unit,h){
  const p=state.parents.find(p=>p.id===unit.parentId),cell=terrain.cells.get(SquadSpace.id(h));
  if(!p||!cell||SquadSpace.distance(h,p.anchor)>p.radius)return false;
  if(cell.terrain==="water"||unit.kind==="vehicle"&&cell.terrain==="marsh")return false;
  const d=terrain.definition;
  if(d){
   const refs=d.deployment?.allowedHexes?.[unit.parentId];
   if(refs&&!refs.some(r=>r.boardId===cell.boardId&&r.hexId===cell.parentHexId))return false;
   if(!refs&&d.setupBoards[unit.side]!==cell.boardId)return false;
  }
  if(unit.loadedOn){const carrier=state.units.find(u=>u.id===unit.loadedOn);return !!carrier&&SquadSpace.id(carrier.hex)===SquadSpace.id(h);}
  const occupants=state.units.filter(u=>u.id!==unit.id&&!u.loadedOn&&u.hex&&SquadSpace.id(u.hex)===SquadSpace.id(h));
  return occupants.length<limit&&!occupants.some(u=>u.side!==unit.side);
 }
 function add(state,terrain,d,plan,id){
  if(state.parents.some(p=>p.id===id))return state;
  const load=(d.deployment?.loads||[]).find(l=>l.passengerId===id);
  if(load&&!state.parents.some(p=>p.id===load.carrierId))state=add(state,terrain,d,plan,load.carrierId);
  const p=parent(d,plan,id,state.settings),profile=SquadProfiles.types[p.typeId];
  if(state.scope!=="full"&&SquadSpace.distance(p.anchor,state.center)+p.radius>state.radius)throw Error("That formation is outside this local footprint.");
  const next=JSON.parse(JSON.stringify(state)),type=d.counters.find(c=>c.id===p.typeId);
  next.parents.push(p);
  for(let i=0;i<profile.count;i++){
   const member=window.SquadTactical?.member(p.typeId,i);
   const unit={definitionId:member?.definitionId||p.typeId,id:id+"/squad-"+(i+1),parentId:id,kind:profile.kind,label:(member?.label||profile.label)+" "+(i+1),side:type.side,nationality:type.nationality||(type.side==="German"?"de":"us"),hex:null};
   // Prefer unoccupied cells, then permit legal friendly stacking.
   const options=SquadSpace.disk(p.anchor,p.radius);
   const carriers=load?next.units.filter(u=>u.parentId===load.carrierId):[];
   if(load)unit.loadedOn=carriers[i%carriers.length].id;
   const candidate=load?carriers[i%carriers.length].hex:options.find(h=>legal(next,terrain,unit,h)&&!next.units.some(u=>u.hex&&SquadSpace.id(u.hex)===SquadSpace.id(h)))||options.find(h=>legal(next,terrain,unit,h));
   if(!candidate||!legal(next,terrain,unit,candidate))throw Error("Insufficient legal space for "+id+". No counters were added. Increase the deployment radius setting or spread the parent formation counters within their permitted setup area and retry.");
   unit.hex=candidate;next.units.push(unit);
  }
  return next;
 }
 function create(d,plan,id){
  const settings={deploymentRadius:6},p=parent(d,plan,id,settings),state={version,settings,setupRevision:0,situationId:d.id,signature:signature(d,plan),profileVersion:SquadProfiles.version,terrainVersion:"source-grid-1",seed:d.illustration.seed,center:p.anchor,radius:14,parents:[],units:[]};
  return add(state,SquadTerrain.create(d,state.center),d,plan,id);
 }
 function validate(s,d,plan){
  if(s.version!==version||s.signature!==signature(d,plan)||s.terrainVersion!=="source-grid-1"||s.profileVersion!==SquadProfiles.version||s.seed!==d.illustration.seed||s.situationId!==d.id)throw Error("The formation plan or profile changed. Export the previous squad setup or rebuild with the current conversions.");
  if(!Number.isInteger(s.center?.q)||!Number.isInteger(s.center?.r)||s.radius!==14||!s.parents?.length||!Array.isArray(s.units))throw Error("Invalid squad setup.");
  if(s.settings&&(!Number.isSafeInteger(s.settings.deploymentRadius)||s.settings.deploymentRadius<0||s.settings.deploymentRadius>50))throw Error("Deployment radius must be a whole number from 0 to 50.");
  const terrain=SquadTerrain.create(d,s.center,s.scope==="full"?null:14),seen=new Set();
  if(s.scope!==undefined&&s.scope!=="full")throw Error("Invalid squad map scope.");
  let expectedCount=0;
  for(const p of s.parents){
   const expected=parent(d,plan,p.id,s.settings);
   if(seen.has(p.id)||JSON.stringify(p)!==JSON.stringify(expected)||(s.scope!=="full"&&SquadSpace.distance(p.anchor,s.center)+p.radius>s.radius))throw Error("Invalid parent deployment area.");
   seen.add(p.id);const profile=SquadProfiles.types[p.typeId];expectedCount+=profile.count;
   const type=d.counters.find(c=>c.id===p.typeId);
   for(let n=1;n<=profile.count;n++){
    const matches=s.units.filter(u=>u.id===p.id+"/squad-"+n),u=matches[0];
    const load=(d.deployment?.loads||[]).find(l=>l.passengerId===p.id),carriers=load?s.units.filter(c=>c.parentId===load.carrierId):[];
    const expectedCarrier=load?carriers[(n-1)%carriers.length]?.id:undefined;
    if(u?.loadedOn!==expectedCarrier)throw Error("Loaded weapon must remain with its assigned transport.");
    if(u?.definitionId!==(window.SquadTactical?.member(p.typeId,n-1)?.definitionId||p.typeId))throw Error("Invalid tactical member definition.");
    if(matches.length!==1||u.parentId!==p.id||u.kind!==profile.kind||u.side!==type.side||!Number.isInteger(u.hex?.q)||!Number.isInteger(u.hex?.r)||!legal(s,terrain,u,u.hex))throw Error("Invalid subordinate placement or roster.");
   }
  }
  if(expectedCount!==s.units.length)throw Error("Invalid subordinate count.");
  return s;
 }
 function expandAll(state,d,plan){
  if(ground(d).some(i=>!plan.placements[i.id]||!SquadProfiles.types[i.counterTypeId]))throw Error("Deploy all formation counters before expanding the full Situation.");
  let next={...JSON.parse(JSON.stringify(state)),scope:"full"};
  const terrain=SquadTerrain.create(d,next.center,null);
  // Place the most constrained groups first, preserving all existing placements.
  const pending=ground(d).filter(i=>!next.parents.some(p=>p.id===i.id));
  const freedom=i=>{const p=parent(d,plan,i.id,next.settings),type=d.counters.find(c=>c.id===i.counterTypeId),unit={id:"candidate",parentId:i.id,kind:SquadProfiles.types[i.counterTypeId].kind,side:type.side};return SquadSpace.disk(p.anchor,p.radius).filter(h=>legal({...next,parents:[...next.parents,p]},terrain,unit,h)).length;};
  pending.sort((a,b)=>freedom(a)-freedom(b)||a.id.localeCompare(b.id));
  for(const i of pending)next=add(next,terrain,d,plan,i.id);
  return validate(next,d,plan);
 }
 function setRadius(state,d,plan,radius){
  if(!Number.isSafeInteger(radius)||radius<0||radius>50)throw Error("Deployment radius must be a whole number from 0 to 50.");
  const next=JSON.parse(JSON.stringify(state));
  next.settings={...next.settings,deploymentRadius:radius};next.setupRevision=(next.setupRevision||0)+1;
  next.parents=next.parents.map(p=>parent(d,plan,p.id,next.settings));
  if(next.parents.some(p=>SquadSpace.distance(p.anchor,next.center)+p.radius>next.radius))next.scope="full";
  try{return validate(next,d,plan);}catch(e){throw Error("Radius change rejected: existing counters would violate the revised deployment area. Move them inward first. "+e.message);}
 }
 function move(state,terrain,id,h){
  const u=state.units.find(u=>u.id===id);
  if(u?.loadedOn)throw Error("Move the assigned transport: "+u.loadedOn);
  if(u?.kind==="equipment")throw Error("Equipment retains its formation setup position.");
  if(!u||!legal(state,terrain,u,h))throw Error("Outside this unit's deployment area, blocked terrain, or stacking limit reached.");
  const trial=JSON.parse(JSON.stringify(state));
  for(const child of trial.units.filter(c=>c.id===id||c.loadedOn===id))child.hex={q:h.q,r:h.r};
  if(trial.units.some(c=>(c.id===id||c.loadedOn===id)&&!legal(trial,terrain,c,c.hex)))throw Error("Transport destination violates its loaded weapon deployment area.");
  for(const child of state.units.filter(c=>c.id===id||c.loadedOn===id))child.hex={q:h.q,r:h.r};
 }
 return {ground,signature,parent,legal,add,create,validate,move,limit,expandAll,setRadius};
})();

