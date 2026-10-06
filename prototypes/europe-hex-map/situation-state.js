"use strict";
// Situation planning and draft handoff only. No tactical engine or invented historical roster.
const SituationModel={
 create(regional,missionId,sector,config){
  const mission=regional.missions.find(m=>m.id===missionId),force=regional.forces.find(f=>f.id===mission?.recipient);
  if(!mission||force?.echelon!=="Battalion"||!["planned","executing"].includes(mission.status))throw new Error("Receive and plan a battalion mission before creating its Situation Card.");
  if(!sector||!config.cells.some(c=>c.id===sector.id))throw new Error("Select a regional sector as the situation's geographic anchor.");
  const start=Date.parse(regional.clock.current),end=start+120*60000;
  if(end>Date.parse(regional.clock.endExclusive))throw new Error("Insufficient time remains in this campaign for the two-hour Situation Card.");
  const id="situation:"+mission.id;
  const platoons=[1,2,3].map(n=>({id:id+":platoon-"+n,name:"Illustrative platoon "+n,parentId:id+":company"}));
  const assets=platoons.flatMap(p=>[1,2,3].map(n=>({id:p.id+":squad-"+n,formationId:p.id,kind:"squad",catalogDefinition:null,status:"available"})));
  return {format:"formation-situation-card/1",id,revision:1,campaignSeed:regional.seed,geographyHash:regional.baseHash,referenceHash:regional.configHash,parentMissionId:mission.id,parentFormationId:force.id,
   title:mission.title+": formation situation",date:regional.clock.current.slice(0,10),campaignInterval:{campaignId:regional.clock.campaignId,start:new Date(start).toISOString(),endExclusive:new Date(end).toISOString()},area:{sectorId:sector.id,center:{x:sector.x,y:sector.y},coordinateSystem:"campaign-local EPSG:3035 km",tacticalFootprint:null},
   provenance:{basis:"authored planning exercise",sourceIds:[mission.sourceId],roster:"Illustrative company with three platoons of three squad placeholders. Not a historical strength return or an ASL order of battle."},
   company:{id:id+":company",name:"Illustrative rifle company",parentId:force.id},platoons,assets,
   objective:{intent:mission.intent,success:"Secure the assigned local objective and retain access for follow-on forces within the situation deadline.",evaluation:"Pending concrete control locations, deadline and authoritative campaign results; scenario wins are not added together."},
   window:{startMinute:0,endMinute:120,basis:"Authored relative planning window, not a historical time or an ASL-turn conversion"},
   support:{allocations:[],note:"No leaders, crews, weapons, vehicles or supporting assets allocated; requests require explicit roster identities."},
   reserveIds:assets.filter(a=>a.formationId===platoons[2].id).map(a=>a.id),engagements:[],reservations:[],outcome:"unassessed"};
 },
 validate(s,regional){
  if(!s.campaignInterval||s.campaignInterval.campaignId!==regional.clock.campaignId||!Number.isFinite(Date.parse(s.campaignInterval.start))||!Number.isFinite(Date.parse(s.campaignInterval.endExclusive))||Date.parse(s.campaignInterval.start)<Date.parse(regional.clock.opening)||Date.parse(s.campaignInterval.endExclusive)>Date.parse(regional.clock.endExclusive)||Date.parse(s.campaignInterval.start)>=Date.parse(s.campaignInterval.endExclusive))throw new Error("Situation interval is outside its dated campaign.");
  if(s.format!=="formation-situation-card/1"||s.campaignSeed!==regional.seed||s.geographyHash!==regional.baseHash||s.referenceHash!==regional.configHash||!regional.missions.some(m=>m.id===s.parentMissionId&&m.recipient===s.parentFormationId))throw new Error("Saved Situation Card does not match its campaign and mission.");
  if(!Array.isArray(s.assets)||!Array.isArray(s.engagements)||!Array.isArray(s.reservations)||new Set(s.assets.map(a=>a.id)).size!==s.assets.length)throw new Error("Invalid situation roster.");
  for(const e of s.engagements){const other={...s,reservations:s.reservations.filter(r=>r.engagementId!==e.id)};this.checkAllocation(other,e);}
  if(s.tacticalTest)TacticalHandoff.validate(s);
  if(s.formation)FormationModel.validate(s.formation,s);
  return s;
 },
 decompose(s){
  if(s.engagements.length)throw new Error("This situation already has engagement drafts; repeated decomposition cannot duplicate forces.");
  const specs=[{key:"approach",title:"Establish access",objective:"Secure access to the parent objective.",platoon:0,start:0,end:60,dependsOn:[]},{key:"objective",title:"Secure the local objective",objective:"Gain the local objective while preserving follow-on access.",platoon:1,start:60,end:120,dependsOn:[s.id+":approach"]}];
  const next=specs.map(spec=>({id:s.id+":"+spec.key,title:spec.title,objective:spec.objective,assetIds:s.assets.filter(a=>a.formationId===s.platoons[spec.platoon].id).map(a=>a.id),window:{startMinute:spec.start,endMinute:spec.end},dependsOn:spec.dependsOn,area:{sectorId:s.area.sectorId,tacticalFootprint:null},status:"draft",trigger:"Authored engagement candidate. Requires confirmed contact, geometry and a mission decision before admission."}));
  for(const e of next)this.checkAllocation(s,e);
  s.engagements=next;s.reservations=next.flatMap(e=>e.assetIds.map(assetId=>({assetId,engagementId:e.id,...e.window,kind:"planning-hold"})));s.revision++;return s;
 },
 checkAllocation(s,e){
  if(!Number.isFinite(e.window.startMinute)||!Number.isFinite(e.window.endMinute)||e.window.startMinute< s.window.startMinute||e.window.endMinute>s.window.endMinute||e.window.startMinute>=e.window.endMinute)throw new Error("Engagement interval must fit inside the Situation Card.");
  if(new Set(e.assetIds).size!==e.assetIds.length||!e.assetIds.length)throw new Error("Engagement assets must be distinct and nonempty.");
  for(const id of e.assetIds){if(!s.assets.some(a=>a.id===id&&a.status==="available"))throw new Error("Unknown or unavailable asset.");if(s.reserveIds.includes(id))throw new Error("A reserve asset needs an explicit release before allocation.");if(s.reservations.some(r=>r.assetId===id&&r.startMinute<e.window.endMinute&&e.window.startMinute<r.endMinute))throw new Error("Asset already reserved during this interval.");}
 },
 draft(s,id){
  const e=s.engagements.find(x=>x.id===id);if(!e)throw new Error("Select an engagement draft.");
  const [year,month,day]=s.date.split("-").map(Number);
  return {format:"formation-asl-handoff-draft/1",executable:false,
   parent:{situationId:s.id,situationRevision:s.revision,missionId:s.parentMissionId,campaignSeed:s.campaignSeed,geographyHash:s.geographyHash,referenceHash:s.referenceHash},
   campaignInterval:s.campaignInterval,engagement:{...JSON.parse(JSON.stringify(e)),absoluteStart:new Date(Date.parse(s.campaignInterval.start)+e.window.startMinute*60000).toISOString(),absoluteEnd:new Date(Date.parse(s.campaignInterval.start)+e.window.endMinute*60000).toISOString()},assetBindings:e.assetIds.map(id=>({assetId:id,definition:s.assets.find(a=>a.id===id).catalogDefinition,side:null,group:null})),
   scenarioCard:{format:"asl-scenario-card/1",id:e.id,title:e.title,catalog:null,source:{basis:"Authored formation engagement draft",legacy:"No legacy Situation Card is reproduced",adaptation:["Derived from "+s.id]},place:s.area.sectorId,date:{day,month,year},introduction:e.objective,boards:[],north:null,playableArea:null,turns:null,scenarioDefender:null,sides:[],specialRules:[],victoryConditions:null,aftermath:null},
   admission:{status:"blocked",engineValidation:"not-run",requirements:["Confirm engagement trigger and both participating sides.","Resolve every asset to the registered ASL catalog, including leaders, crews and equipment.","Compile the actual tactical footprint and validate its ASL board package and setup locations.","Specify turns, first setup/movement, edges, ELR, SAN and applicable special rules.","Translate the engagement objective into supported, enforceable Scenario Card victory conditions.","Validate with ScenarioCards.Parse/Validate and bind the admitted card hash, current campaign revision and exclusive execution reservations."]},
   reconciliation:{status:"not-implemented",requires:["Admitted card hash and original asset identities","Authoritative surviving assets, losses and expenditure","Elapsed campaign time and resulting locations","Objective/control facts and support release"],policy:"Apply once to the matching situation revision. Reject stale or duplicate results; do not count scenario victories as situation victory."}};
 }
};
