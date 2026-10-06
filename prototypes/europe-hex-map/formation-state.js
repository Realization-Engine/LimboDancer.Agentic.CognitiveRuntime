"use strict";
const FormationModel={
 phases:["Command and Rally","Preparatory Fire","Movement","Defensive Fire","Advancing Fire","Rout","Advance","Close Combat"],
 hash(text){let h=2166136261;for(const c of text)h=Math.imul(h^c.charCodeAt(0),16777619);return h>>>0;},
 create(s){
  const radius=.25/Math.sqrt(3),r0=Math.round(s.area.center.y/(1.5*radius)),q0=Math.round(s.area.center.x/.25-r0/2),cells=[];
  for(let dr=-4;dr<=4;dr++)for(let dq=-5;dq<=5;dq++){
   const q=q0+dq,r=r0+dr,id=`F250:${q}:${r}`,h=this.hash(s.campaignSeed+"|formation-terrain-v1|"+id);
   cells.push({id,q,r,x:.25*(q+r/2),y:1.5*radius*r,terrain:h%10<2?"woods":h%10<5?"fields":"open"});
  }
  const locate=(dq,dr)=>cells.find(c=>c.q===q0+dq&&c.r===r0+dr).id;
  const counters=s.platoons.map((p,i)=>({id:p.id,name:"Platoon "+(i+1),side:"american",cellId:locate(-4,i-1),posture:"maneuver",spent:0,assetIds:s.assets.filter(a=>a.formationId===p.id).map(a=>a.id)}));
  const enemy=s.assets.filter(a=>a.side==="german");if(enemy.length)counters.push({id:s.id+":test-opposition",name:"German detachment",side:"german",cellId:locate(3,0),posture:"hold",spent:0,assetIds:enemy.map(a=>a.id)});
  return {version:1,clock:{current:s.campaignInterval.start,endExclusive:s.campaignInterval.endExclusive},sourceRevision:s.revision,seed:s.campaignSeed,geographyHash:s.geographyHash,cells,counters,objectiveCellId:locate(1,0),turn:1,side:"american",phase:0,events:[],contacts:[],outcome:"unassessed",terrainBasis:"Seeded illustrative terrain, not a historical reconstruction or ASL terrain package"};
 },
 validate(f,s){
  if(f.version!==1||f.sourceRevision!==s.revision||f.seed!==s.campaignSeed||f.geographyHash!==s.geographyHash||!Number.isInteger(f.phase)||f.phase<0||f.phase>7||!Number.isInteger(f.turn)||f.turn<1||!["american","german"].includes(f.side))throw new Error("Formation state does not match its situation.");
  if(!f.clock||f.clock.endExclusive!==s.campaignInterval.endExclusive||!Number.isFinite(Date.parse(f.clock.current))||Date.parse(f.clock.current)!==Date.parse(s.campaignInterval.start)+(f.turn-1)*6*60000||Date.parse(f.clock.current)>Date.parse(f.clock.endExclusive))throw new Error("Formation time does not match its turn or campaign interval.");
  const expected=this.create(s);
  if(JSON.stringify(f.cells)!==JSON.stringify(expected.cells)||f.objectiveCellId!==expected.objectiveCellId||f.counters.length!==expected.counters.length||new Set(f.counters.map(c=>c.id)).size!==f.counters.length||new Set(f.counters.map(c=>c.cellId)).size!==f.counters.length)throw new Error("Formation geography or counters are invalid.");
  for(const c of f.counters){const original=expected.counters.find(o=>o.id===c.id);if(!original||c.side!==original.side||JSON.stringify(c.assetIds)!==JSON.stringify(original.assetIds)||!f.cells.some(cell=>cell.id===c.cellId)||!Number.isInteger(c.spent)||c.spent<0||c.spent>4||!["hold","maneuver"].includes(c.posture))throw new Error("Formation counter identity, position or budget is invalid.");}
  if(!Array.isArray(f.contacts)||!Array.isArray(f.events))throw new Error("Formation event state is invalid.");
  return f;
 },
 distance(a,b){return (Math.abs(a.q-b.q)+Math.abs(a.r-b.r)+Math.abs(a.q+a.r-b.q-b.r))/2;},
 move(f,counterId,targetId){
  if(Date.parse(f.clock.current)>=Date.parse(f.clock.endExclusive))throw new Error("Situation time window exhausted.");
  const unit=f.counters.find(c=>c.id===counterId),to=f.cells.find(c=>c.id===targetId);
  if(!unit||unit.side!==f.side||f.phase!==2||unit.posture!=="maneuver")throw new Error("Select an active-side counter with maneuver orders during Movement.");
  if(f.contacts.length)throw new Error("Contact requires tactical resolution. Formation movement is paused; no combat is auto-resolved.");
  const from=f.cells.find(c=>c.id===unit.cellId);if(!to||this.distance(from,to)!==1)throw new Error("Choose an adjacent hex.");
  if(f.counters.some(c=>c.cellId===to.id))throw new Error("That hex is occupied. This prototype allows one formation counter per hex.");
  const cost=to.terrain==="woods"?2:1;if(unit.spent+cost>4)throw new Error("This counter has spent its four movement points for this player turn.");
  unit.cellId=to.id;unit.spent+=cost;f.events.push({kind:"move",turn:f.turn,side:f.side,phase:f.phase,counterId,targetId,cost});
  for(const other of f.counters.filter(c=>c.side!==unit.side))if(this.distance(to,f.cells.find(c=>c.id===other.cellId))<=1)f.contacts.push({id:"contact-"+(f.contacts.length+1),counterIds:[unit.id,other.id],cellIds:[unit.cellId,other.cellId],turn:f.turn,side:f.side,status:"awaiting-engagement-admission"});
 },
 order(f,id){const c=f.counters.find(c=>c.id===id);if(!c||c.side!==f.side||f.phase!==0)throw new Error("Orders may change only for the active side during Command and Rally.");c.posture=c.posture==="hold"?"maneuver":"hold";f.events.push({kind:"order",counterId:id,posture:c.posture,turn:f.turn,side:f.side});},
 next(f){
  if(f.contacts.length)throw new Error("Resolve the contact through a validated tactical engagement before advancing. This integration is not yet available.");
  if(Date.parse(f.clock.current)>=Date.parse(f.clock.endExclusive)||f.phase===7&&f.side==="german"&&Date.parse(f.clock.current)+6*60000>Date.parse(f.clock.endExclusive))throw new Error("Situation time window exhausted.");
  f.events.push({campaignTime:f.clock.current,kind:"phase-completed",turn:f.turn,side:f.side,phase:this.phases[f.phase],note:f.phase===0||f.phase===2?"Manual maneuver prototype":"Passed; combat/recovery effects not implemented"});
  f.phase++;if(f.phase===8){f.phase=0;if(f.side==="american")f.side="german";else{f.side="american";f.turn++;f.clock.current=new Date(Date.parse(f.clock.current)+6*60000).toISOString();}for(const c of f.counters)c.spent=0;}
 }
};
