"use strict";
const TacticalHandoff={
 prepare(s){
  if(s.formation)throw new Error("Prepare the tactical roster before opening the formation map.");
  if(s.tacticalTest)throw new Error("The tactical test is already prepared.");
  if(s.engagements.length!==2||s.engagements.some(e=>e.status!=="draft"))throw new Error("Prepare the two engagement drafts first.");
  if(s.assets.some(a=>a.catalogDefinition))throw new Error("This roster already has catalog mappings; review it before replacing them.");
  const definitions=new Set(TACTICAL_REFERENCE.definitions.map(d=>d.id));
  for(const id of ["american-squad","american-leader-8-0","attacker-squad","attacker-leader-8-0"])if(!definitions.has(id))throw new Error("Required catalog definition is unavailable.");
  for(const a of s.assets){a.catalogDefinition="american-squad";a.side="american";}
  for(const p of s.platoons){const id=p.id+":leader";s.assets.push({id,formationId:p.id,kind:"leader",catalogDefinition:"american-leader-8-0",side:"american",status:"available"});
   const e=s.engagements.find(e=>e.assetIds.some(a=>s.assets.find(x=>x.id===a).formationId===p.id));
   if(e)e.assetIds.push(id);else s.reserveIds.push(id);
  }
  const e=s.engagements[0],opponent=s.id+":test-opposition";
  for(const [suffix,kind,definition] of [["squad-1","squad","attacker-squad"],["squad-2","squad","attacker-squad"],["leader","leader","attacker-leader-8-0"]]){
   const id=opponent+":"+suffix;s.assets.push({id,formationId:opponent,kind,catalogDefinition:definition,side:"german",status:"available"});e.assetIds.push(id);
  }
  s.reservations=s.engagements.flatMap(e=>e.assetIds.map(assetId=>({assetId,engagementId:e.id,...e.window,kind:"planning-hold"})));
  s.revision++;
  const card=JSON.parse(JSON.stringify(TACTICAL_REFERENCE.card));
  card.id="formation-"+s.campaignSeed.replace(/[^a-z0-9]/g,"").slice(0,8)+"-"+s.parentMissionId.replace(/[^a-z0-9-]/g,"-").slice(0,28)+"-access";
  s.catalog=TACTICAL_REFERENCE.catalog;s.catalogSha256=TACTICAL_REFERENCE.catalogSha256;
  s.provenance.roster="Authored catalog-backed company: three platoons, each with three American squads and one 8-0 leader. First engagement adds two German squads and one 8-0 leader as test opposition. Not a historical strength return.";
  s.tacticalTest={kind:"representative-terrain-test",engagementId:e.id,situationRevision:s.revision,scenarioCard:card,campaignAdmission:"blocked",reconciliation:"disabled",templateValidation:"Engine-tested template; validate exported card in ASL Studio before play",assetBindings:e.assetIds.map(id=>{const a=s.assets.find(a=>a.id===id);return {assetId:id,definition:a.catalogDefinition,side:a.side,group:a.side+"-1",area:"deployment"};})};
  return s;
 },
 validate(s){
  const t=s.tacticalTest,e=s.engagements.find(e=>e.id===t.engagementId);
  if(t.campaignAdmission!=="blocked"||t.reconciliation!=="disabled"||t.situationRevision!==s.revision||!e||t.assetBindings.length!==e.assetIds.length||new Set(t.assetBindings.map(b=>b.assetId)).size!==e.assetIds.length)throw new Error("Invalid tactical test lineage or admission state.");
  for(const b of t.assetBindings){const a=s.assets.find(a=>a.id===b.assetId);if(!e.assetIds.includes(b.assetId)||!a||a.catalogDefinition!==b.definition||a.side!==b.side)throw new Error("Tactical asset binding does not match the situation roster.");}
  for(const side of t.scenarioCard.sides)for(const group of side.groups)for(const unit of group.units){if(t.assetBindings.filter(b=>b.side===side.side&&b.definition===unit.definition).length!==unit.count)throw new Error("Scenario Card force counts do not match persistent assets.");}
  return s;
 },
 async bundle(s){
  if(!s.tacticalTest)throw new Error("Prepare the tactical test first.");
  const cardText=JSON.stringify(s.tacticalTest.scenarioCard,null,2);
  const hash=[...new Uint8Array(await crypto.subtle.digest("SHA-256",new TextEncoder().encode(cardText)))].map(b=>b.toString(16).padStart(2,"0")).join("");
  return {format:"formation-tactical-test/1",campaignExecutable:false,campaignInterval:s.campaignInterval,parent:{situationId:s.id,situationRevision:s.revision,missionId:s.parentMissionId,campaignSeed:s.campaignSeed,geographyHash:s.geographyHash,referenceHash:s.referenceHash},catalog:{id:s.catalog,sha256:s.catalogSha256},...JSON.parse(JSON.stringify(s.tacticalTest)),cardSha256:hash,cardText};
 }
};
