"use strict";
const PanzerSituationModel={
 create(data){return {version:2,situationId:data.id,stage:"German",placements:{},selectedIds:[],notes:"",engagements:[]};},
 validate(s,d){
  if(s.version!==2||s.situationId!==d.id||!["German","Allied","ready"].includes(s.stage)||!s.placements||!Array.isArray(s.engagements)||typeof s.notes!=="string")throw Error("Invalid saved Situation plan.");
  for(const [id,p] of Object.entries(s.placements)){const instance=d.instances.find(i=>i.id===id),type=d.counters.find(c=>c.id===instance?.counterTypeId);if(!type||p.board!==d.setupBoards[type.side]||!Number.isFinite(p.x)||!Number.isFinite(p.y)||p.x<0||p.x>1||p.y<0||p.y>1)throw Error("Invalid counter placement.");const hex=d.boards.find(b=>b.id===p.board)?.hexes.find(h=>h.id===p.hexId);if(!hex||!hex.setupAllowed||Math.abs(p.x-hex.imageCenter.u)>1e-7||Math.abs(p.y-hex.imageCenter.v)>1e-7)throw Error("Invalid setup hex or unsnapped placement.");}
  if(s.stage!=="German"&&!this.complete(s,d,"German")||s.stage==="ready"&&!this.complete(s,d,"Allied"))throw Error("Saved setup is incomplete.");
  if(s.gameLaunched!==undefined&&typeof s.gameLaunched!=="boolean")throw Error("Invalid game launch state.");
  if(s.gameLaunched&&s.stage!=="ready")throw Error("Complete setup before launching the game map.");
  const held=new Set();
  for(const e of s.engagements){if(e.parentSituationId!==d.id||e.status!=="blocked"||e.executable!==false||!Array.isArray(e.counterIds)||!e.counterIds.length)throw Error("Invalid engagement draft.");for(const id of e.counterIds){if(!d.instances.some(i=>i.id===id)||held.has(id)||!s.placements[id])throw Error("Invalid or duplicate engagement allocation.");held.add(id);}}
  return s;
 },
 hexAt(d,board,u,v){
  return d.boards.find(b=>b.id===board)?.hexes.find(h=>{let inside=false;const p=h.imagePolygon;for(let i=0,j=p.length-1;i<p.length;j=i++){if((p[i].v>v)!==(p[j].v>v)&&u<(p[j].u-p[i].u)*(v-p[i].v)/(p[j].v-p[i].v)+p[i].u)inside=!inside;}return inside;});
 },
 complete(s,d,side){return d.instances.filter(i=>d.counters.find(c=>c.id===i.counterTypeId).side===side).every(i=>s.placements[i.id]);},
 place(s,d,id,board,x,y){const i=d.instances.find(i=>i.id===id),c=d.counters.find(c=>c.id===i?.counterTypeId);if(!c||s.stage!==c.side||board!==d.setupBoards[c.side])throw Error("Place the active side on its assigned board.");if(!Number.isFinite(x)||!Number.isFinite(y)||x<0||x>1||y<0||y>1)throw Error("Select a point on the board.");const hex=this.hexAt(d,board,x,y);if(!hex||!hex.setupAllowed)throw Error("Choose a complete interior hex. Boundary half-hexes are excluded from this setup plan.");s.placements[id]={board,hexId:hex.id,x:hex.imageCenter.u,y:hex.imageCenter.v};},
 next(s,d){if(s.stage==="ready"||!this.complete(s,d,s.stage))throw Error("Place every counter for this side before continuing.");s.stage=s.stage==="German"?"Allied":"ready";},
 launch(s,d){this.validate(s,d);if(s.stage!=="ready")throw Error("Complete both sides setup before launching the game map.");s.gameLaunched=true;},
 draft(s,d,ids,intent){
  if(s.stage!=="ready")throw Error("Complete both sides' setup first.");
  const unique=[...new Set(ids)],types=unique.map(id=>d.counters.find(c=>c.id===d.instances.find(i=>i.id===id)?.counterTypeId));
  if(!unique.length||types.some(t=>!t)||!types.some(t=>t.side==="Allied")||!types.some(t=>t.side==="German"))throw Error("Select participating counters from both sides.");
  if(!intent.trim())throw Error("Describe this engagement's local objective.");
  if(s.engagements.some(e=>e.counterIds.some(id=>unique.includes(id))))throw Error("A counter is already held by another engagement draft. Clear this plan to release those planning holds.");
  const e={id:d.id+":engagement-"+(s.engagements.length+1),counterIds:unique,intent:intent.trim(),status:"blocked",executable:false,parentSituationId:d.id,date:d.situation.printedDate,scenarioCard:{format:"asl-scenario-card/1",title:"St. Lo: "+intent.trim(),date:{year:1944,month:6,day:29},boards:[],sides:[],victoryConditions:null},requirements:["Resolve each parent counter into persistent squad, leader, crew, weapon and vehicle identities using a dated organization source.","Define contact, footprint and interval; compile and validate the ASL board package.","Specify setup, turns, participating sides and enforceable local victory conditions.","Validate the completed Scenario Card through the ASL engine before execution."],parentObjective:d.victory,placements:Object.fromEntries(unique.map(id=>[id,{...s.placements[id]}]))};s.engagements.push(e);return e;
 }
};
