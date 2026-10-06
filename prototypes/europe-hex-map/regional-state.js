"use strict";
// A manual planning exercise. No combat, real-time clock, or operational simulation.
const RegionalModel={
 create(config,seed,baseHash){return {schema:1,clock:{campaignId:config.campaign.id,opening:config.campaign.opening,current:config.campaign.opening,endExclusive:config.campaign.endExclusive},configHash:config.sourceHash,seed,baseHash,step:0,forces:JSON.parse(JSON.stringify(config.forces)),missions:config.missions.map(m=>({...m,status:"draft",knownToIssuer:"draft",plan:"",report:"",assessment:"",sectorId:null,assessmentDelivered:false})),messages:[],events:[]};},
 validate(s,c,seed,hash){
  if(!s||s.schema!==1||s.configHash!==c.sourceHash||s.seed!==seed||s.baseHash!==hash||!Number.isInteger(s.step)||s.step<0||!Array.isArray(s.messages)||!Array.isArray(s.events)||!Array.isArray(s.forces)||!Array.isArray(s.missions)||s.missions.length!==c.missions.length)throw new Error("Saved exercise does not match this campaign or reference package.");
  if(!s.clock||s.clock.campaignId!==c.campaign.id||s.clock.opening!==c.campaign.opening||s.clock.endExclusive!==c.campaign.endExclusive||!Number.isFinite(Date.parse(s.clock.current))||Date.parse(s.clock.current)<Date.parse(s.clock.opening)||Date.parse(s.clock.current)>Date.parse(s.clock.endExclusive))throw new Error("Saved campaign clock is invalid.");
  for(const m of s.missions){const def=c.missions.find(x=>x.id===m.id);if(!def||m.issuer!==def.issuer||m.recipient!==def.recipient||!["draft","assigned","received","planned","executing","reported","assessed"].includes(m.status))throw new Error("Saved mission state is invalid.");}
  if(new Set(s.missions.map(m=>m.id)).size!==c.missions.length||s.forces.length!==c.forces.length||new Set(s.forces.map(f=>f.id)).size!==c.forces.length)throw new Error("Saved hierarchy is incomplete or duplicated.");
  for(const f of s.forces){const def=c.forces.find(x=>x.id===f.id);if(!def||f.parentId!==def.parentId||f.echelon!==def.echelon)throw new Error("Saved formation hierarchy is invalid.");}
  for(const m of s.missions){const def=c.missions.find(x=>x.id===m.id);if(m.parentId!==def.parentId||m.objectiveId!==def.objectiveId)throw new Error("Saved mission linkage is invalid.");}
  if(s.situations){if(!Array.isArray(s.situations)||new Set(s.situations.map(x=>x.parentMissionId)).size!==s.situations.length)throw new Error("Invalid Situation Card collection.");for(const card of s.situations)SituationModel.validate(card,s);}
  return s;
 },
 queue(s,m,kind,from,to,text){s.messages.push({id:"message-"+(s.messages.length+1),missionId:m.id,kind,from,to,text,sentAtCampaignTime:s.clock.current,sentAt:s.step,dueAt:s.step+1,deliveredAt:null});},
 act(s,id,action,hq,text="",sectorId=null){
  const m=s.missions.find(x=>x.id===id);if(!m)throw new Error("Select a mission.");
  const require=(condition,message)=>{if(!condition)throw new Error(message);};
  if(action==="assign"){
   require(hq===m.issuer&&m.status==="draft","Only the issuing headquarters can assign a draft mission.");
   if(m.parentId){const parent=s.missions.find(x=>x.id===m.parentId);require(parent&&parent.recipient===hq&&["planned","executing"].includes(parent.status),"Receive and plan the parent mission before issuing subordinate missions.");}
   m.status="assigned";m.knownToIssuer="assigned";this.queue(s,m,"order",m.issuer,m.recipient,m.intent);
  }else if(action==="plan"){
   require(hq===m.recipient&&m.status==="received","The recipient must receive the order before planning.");require(text.trim(),"Enter a plan before saving.");m.plan=text.trim().slice(0,2000);m.sectorId=sectorId;m.status="planned";
  }else if(action==="execute"){
   require(hq===m.recipient&&m.status==="planned","Only the recipient can begin a planned mission.");m.status="executing";
  }else if(action==="report"){
   require(hq===m.recipient&&m.status==="executing","Only the recipient can report an executing mission.");require(text.trim(),"Enter a report; no result is generated automatically.");require(s.missions.filter(x=>x.parentId===m.id).every(x=>x.status==="assessed"),"Assess subordinate reports before reporting the parent mission.");m.report=text.trim().slice(0,2000);m.status="reported";this.queue(s,m,"report",m.recipient,m.issuer,m.report);
  }else if(action==="assess"){
   require(hq===m.issuer&&m.status==="reported"&&m.knownToIssuer==="reported","The issuing headquarters must receive the report before assessing it.");require(text.trim(),"Record an assessment.");m.assessment=text.trim().slice(0,2000);m.status="assessed";m.knownToIssuer="assessed";this.queue(s,m,"assessment",m.issuer,m.recipient,m.assessment);
  }else throw new Error("Unsupported mission action.");
  s.events.push({step:s.step,missionId:id,headquarters:hq,action});return s;
 },
 advance(s){
  s.step++;
  for(const msg of [...s.messages]){
   if(msg.deliveredAt!==null||msg.dueAt>s.step)continue;
   const m=s.missions.find(x=>x.id===msg.missionId);msg.deliveredAt=s.step;msg.deliveredAtCampaignTime=s.clock.current;
   if(msg.kind==="order"){m.status="received";this.queue(s,m,"receipt",m.recipient,m.issuer,"Order received.");}
   if(msg.kind==="receipt"&&m.knownToIssuer==="assigned")m.knownToIssuer="received";
   if(msg.kind==="report")m.knownToIssuer="reported";
   if(msg.kind==="assessment")m.assessmentDelivered=true;
  }
  s.events.push({step:s.step,action:"advance-communications"});return s;
 },
 divisionSummary(s,id){
  const division=s.forces.find(f=>f.id===id&&f.echelon==="Division");if(!division)throw new Error("Select a division.");
  const regiments=s.forces.filter(f=>f.parentId===id&&f.echelon==="Regiment");
  return regiments.map(r=>{const mission=s.missions.find(m=>m.issuer===id&&m.recipient===r.id);return {id:r.id,name:r.name,battalions:s.forces.filter(f=>f.parentId===r.id&&f.echelon==="Battalion").length,status:mission?this.status(mission,id):"no mission",openingReport:r.openingReport};});
 },
 status(m,hq){return hq===m.issuer?m.knownToIssuer:(m.status==="assessed"&&!m.assessmentDelivered?"reported":m.status);},
 messages(s,hq){return s.messages.filter(m=>m.from===hq||(m.to===hq&&m.deliveredAt!==null));},
 admission(){return ["Extend the implemented 18th Infantry battalions through dated company and platoon organizations.","Select and reserve actual squads, leaders, crews, vehicles and shared support.","Compile and validate an ASL terrain and board package for the engagement area.","Complete the Scenario Card: sides, order of battle, setup, turns, special rules and victory conditions.","Bind the issued card ID/hash, campaign revision, time interval and reconciliation contract."];}
};
