"use strict";
const PanzerSituationModel={
 create(data){return {version:2,situationId:data.id,stage:data.setupOrder[0],placements:{},selectedIds:[],notes:"",engagements:[]};},
 validate(s,d){
  if(s.version!==2||s.situationId!==d.id||!["German","Allied","ready"].includes(s.stage)||!s.placements||!Array.isArray(s.engagements)||typeof s.notes!=="string")throw Error("Invalid saved Situation plan.");
  for(const [id,p] of Object.entries(s.placements)){const instance=d.instances.find(i=>i.id===id),type=d.counters.find(c=>c.id===instance?.counterTypeId);if(!type||!this.boards(d,id).includes(p.board)||!Number.isFinite(p.x)||!Number.isFinite(p.y)||p.x<0||p.x>1||p.y<0||p.y>1)throw Error("Invalid counter placement.");const hex=d.boards.find(b=>b.id===p.board)?.hexes.find(h=>h.id===p.hexId);if(!hex||!this.allowed(d,id,p.board,hex.id,s)||Math.abs(p.x-hex.imageCenter.u)>1e-7||Math.abs(p.y-hex.imageCenter.v)>1e-7)throw Error("Invalid setup hex or unsnapped placement.");}
  if((s.stage!==d.setupOrder[0]&&!this.complete(s,d,d.setupOrder[0]))||(s.stage==="ready"&&!this.complete(s,d,d.setupOrder[1])))throw Error("Saved setup is incomplete.");
  if(s.gameLaunched!==undefined&&typeof s.gameLaunched!=="boolean")throw Error("Invalid game launch state.");
  if(s.gameLaunched&&s.stage!=="ready")throw Error("Complete setup before launching the game map.");
  for(const load of d.deployment?.loads||[]){const a=s.placements[load.passengerId],b=s.placements[load.carrierId];if(!!a!==!!b||a&&(a.board!==b.board||a.hexId!==b.hexId))throw Error("Loaded mortar must remain with its assigned truck.");}
  const held=new Set();
  for(const e of s.engagements){if(e.parentSituationId!==d.id||e.status!=="blocked"||e.executable!==false||!Array.isArray(e.counterIds)||!e.counterIds.length)throw Error("Invalid engagement draft.");for(const id of e.counterIds){if(!d.instances.some(i=>i.id===id)||held.has(id)||!s.placements[id])throw Error("Invalid or duplicate engagement allocation.");held.add(id);}}
  return s;
 },
 hexAt(d,board,u,v){
  return d.boards.find(b=>b.id===board)?.hexes.find(h=>{let inside=false;const p=h.imagePolygon;for(let i=0,j=p.length-1;i<p.length;j=i++){if((p[i].v>v)!==(p[j].v>v)&&u<(p[j].u-p[i].u)*(v-p[i].v)/(p[j].v-p[i].v)+p[i].u)inside=!inside;}return inside;});
 },
 ground(d){return d.instances.filter(i=>d.counters.find(c=>c.id===i.counterTypeId)?.kind!=="aircraft");},
 complete(s,d,side){return this.ground(d).filter(i=>d.counters.find(c=>c.id===i.counterTypeId).side===side).every(i=>s.placements[i.id]);},
 boards(d,id){const i=d.instances.find(i=>i.id===id),c=d.counters.find(c=>c.id===i?.counterTypeId);return [...new Set(d.deployment?.allowedHexes[id]?.map(r=>r.boardId)||[d.setupBoards[c?.side]])];},
 allowed(d,id,board,hexId,s){const h=d.boards.find(b=>b.id===board)?.hexes.find(h=>h.id===hexId);if(!h?.setupAllowed)return false;const refs=d.deployment?.allowedHexes[id];if(refs&&!refs.some(r=>r.boardId===board&&r.hexId===hexId))return false;
  const instance=d.instances.find(i=>i.id===id),side=d.counters.find(c=>c.id===instance?.counterTypeId)?.side;
  for(const rule of d.deployment?.minimumSeparation||[]){if(rule.side!==side)continue;if(!s)return true;for(const [other,p]of Object.entries(s.placements)){const oi=d.instances.find(i=>i.id===other),oc=d.counters.find(c=>c.id===oi?.counterTypeId);if(oc?.side!==rule.fromSide||p.board!==board)continue;const oh=d.boards.find(b=>b.id===board).hexes.find(h=>h.id===p.hexId);if(oh&&Math.max(Math.abs(h.q-oh.q),Math.abs(h.r-oh.r),Math.abs(h.q+h.r-oh.q-oh.r))<rule.hexes)return false;}}
  return true;},
 place(s,d,id,board,x,y){
  const i=d.instances.find(i=>i.id===id),c=d.counters.find(c=>c.id===i?.counterTypeId);
  if(!c||s.stage!==c.side||!this.boards(d,id).includes(board))throw Error("Place the active side on its assigned board.");
  if(!Number.isFinite(x)||!Number.isFinite(y)||x<0||x>1||y<0||y>1)throw Error("Select a point on the board.");
  const hex=this.hexAt(d,board,x,y);if(!hex||!hex.setupAllowed)throw Error("Choose a complete interior hex. Boundary half-hexes are excluded from this setup plan.");
  if(!this.allowed(d,id,board,hex.id,s))throw Error("Outside this unit's setup zone. "+d.deployment.instructions[id]);
  const load=(d.deployment?.loads||[]).find(l=>l.passengerId===id);
  if(load)throw Error("This mortar starts loaded. Place or move its assigned truck: "+load.carrierId+".");
  s.placements[id]={board,hexId:hex.id,x:hex.imageCenter.u,y:hex.imageCenter.v};
  for(const l of d.deployment?.loads||[])if(l.carrierId===id)s.placements[l.passengerId]={...s.placements[id]};
 },
 autoDeploy(s,d,typeId=null){
  const trial=JSON.parse(JSON.stringify(s));const pending=this.ground(d).filter(i=>!trial.placements[i.id]&&(!typeId||i.counterTypeId===typeId)&&d.counters.find(c=>c.id===i.counterTypeId).side===s.stage&&d.deployment?.allowedHexes[i.id]?.length);
  for(const unit of pending){if(trial.placements[unit.id])continue;const load=(d.deployment.loads||[]).find(l=>l.passengerId===unit.id);if(load)continue;
   const refs=d.deployment.allowedHexes[unit.id],occupied=new Set(Object.values(trial.placements).map(p=>p.board+":"+p.hexId));
   const legal=refs.filter(r=>this.boards(d,unit.id).includes(r.boardId)&&this.allowed(d,unit.id,r.boardId,r.hexId,trial));
   const target=legal.find(r=>!occupied.has(r.boardId+":"+r.hexId))||legal[0];
   if(!target)throw Error("No legal prescribed setup hex for "+unit.id+". No automatic placements were applied.");
   const h=d.boards.find(b=>b.id===target.boardId).hexes.find(h=>h.id===target.hexId);this.place(trial,d,unit.id,target.boardId,h.imageCenter.u,h.imageCenter.v);
  }
  s.placements=trial.placements;
 },
 autoDeploySide(s,d){if(s.stage==="ready"||(s.autoDeploymentSides||[]).includes(s.stage))return;this.autoDeploy(s,d);s.autoDeploymentSides=[...(s.autoDeploymentSides||[]),s.stage];},
 bulkUnits(s,d,typeId){const type=d.counters.find(c=>c.id===typeId);if(!type||type.side!==s.stage||type.kind==="aircraft")throw Error("Choose a unit type belonging to the active setup side.");const units=this.ground(d).filter(i=>i.counterTypeId===typeId);const ids=new Set(units.map(i=>i.id));if((d.deployment?.loads||[]).some(l=>ids.has(l.carrierId)||ids.has(l.passengerId)))throw Error("Loaded units must be deployed with their assigned transport individually.");return units;},
 deployType(s,d,typeId,boardId,hexId){
  const pending=this.bulkUnits(s,d,typeId).filter(i=>!s.placements[i.id]);if(!pending.length)return;
  const board=d.boards.find(b=>b.id===boardId),origin=board?.hexes.find(h=>h.id===hexId);
  if(!origin||!this.boards(d,pending[0].id).includes(boardId)||!this.allowed(d,pending[0].id,boardId,hexId,s))throw Error("Choose a highlighted starting hex for this unit type.");
  const trial=JSON.parse(JSON.stringify(s));const used=new Set(Object.values(trial.placements).filter(p=>p.board===boardId).map(p=>p.hexId));
  const distance=h=>Math.max(Math.abs(h.q-origin.q),Math.abs(h.r-origin.r),Math.abs(h.q+h.r-origin.q-origin.r));
  const ring=board.hexes.filter(h=>h.setupAllowed).sort((a,b)=>distance(a)-distance(b)||Math.atan2(a.imageCenter.v-origin.imageCenter.v,a.imageCenter.u-origin.imageCenter.u)-Math.atan2(b.imageCenter.v-origin.imageCenter.v,b.imageCenter.u-origin.imageCenter.u)||a.id.localeCompare(b.id));
  for(const [index,unit]of pending.entries()){
   const target=index===0?origin:ring.find(h=>!used.has(h.id)&&this.allowed(d,unit.id,boardId,h.id,trial));
   if(!target||!this.allowed(d,unit.id,boardId,target.id,trial))throw Error("Not enough legal hexes on this board for the remaining units. Choose another starting hex or deploy individually.");
   this.place(trial,d,unit.id,boardId,target.imageCenter.u,target.imageCenter.v);used.add(target.id);
  }
  s.placements=trial.placements;
 },
 clearType(s,d,typeId){const units=this.bulkUnits(s,d,typeId);for(const unit of units)delete s.placements[unit.id];},
 next(s,d){if(s.stage==="ready"||!this.complete(s,d,s.stage))throw Error("Place every counter for this side before continuing.");this.validate(s,d);s.stage=d.setupOrder[d.setupOrder.indexOf(s.stage)+1]||"ready";},
 launch(s,d){this.validate(s,d);if(s.stage!=="ready")throw Error("Complete both sides setup before launching the game map.");s.gameLaunched=true;},
 draft(s,d,ids,intent){
  if(s.stage!=="ready")throw Error("Complete both sides' setup first.");
  const unique=[...new Set(ids)],types=unique.map(id=>d.counters.find(c=>c.id===d.instances.find(i=>i.id===id)?.counterTypeId));
  if(!unique.length||types.some(t=>!t||["block","aircraft","bridge-equipment"].includes(t.kind))||!types.some(t=>t.side==="Allied")||!types.some(t=>t.side==="German"))throw Error("Select participating counters from both sides.");
  if(!intent.trim())throw Error("Describe this engagement's local objective.");
  if(s.engagements.some(e=>e.counterIds.some(id=>unique.includes(id))))throw Error("A counter is already held by another engagement draft. Clear this plan to release those planning holds.");
  const e={id:d.id+":engagement-"+(s.engagements.length+1),counterIds:unique,intent:intent.trim(),status:"blocked",executable:false,parentSituationId:d.id,date:d.situation.printedDate,scenarioCard:{format:"asl-scenario-card/1",title:d.situation.title+": "+intent.trim(),date:{year:Number(d.situation.printedDate.slice(0,4)),month:Number(d.situation.printedDate.slice(5,7)),day:Number(d.situation.printedDate.slice(8,10))},boards:[],sides:[],victoryConditions:null},requirements:["Resolve each parent counter into persistent squad, leader, crew, weapon and vehicle identities using a dated organization source.","Define contact, footprint and interval; compile and validate the ASL board package.","Specify setup, turns, participating sides and enforceable local victory conditions.","Validate the completed Scenario Card through the ASL engine before execution."],parentObjective:d.victory,placements:Object.fromEntries(unique.map(id=>[id,{...s.placements[id]}]))};s.engagements.push(e);return e;
 }
};
