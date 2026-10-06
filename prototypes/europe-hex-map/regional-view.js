// Regional workspace shares the theater renderer and adds explicit command state.
let regionalState=null,regionalHQ="first-army",regionalMission="beachhead",regionalSector=null;
function regionalKey(){return "regional-exercise-v1-"+seed+"-"+DATA.regionalCampaign.sourceHash+"-"+DATA.metadata.baseHash;}
function loadRegionalState(){
 const raw=localStorage.getItem(regionalKey());
 regionalState=raw?RegionalModel.validate(JSON.parse(raw),DATA.regionalCampaign,seed,DATA.metadata.baseHash):RegionalModel.create(DATA.regionalCampaign,seed,DATA.metadata.baseHash);
}
function regionalForExport(){
 if(regionalState&&regionalState.seed===seed)return regionalState;
 const raw=localStorage.getItem(regionalKey());
 return raw?RegionalModel.validate(JSON.parse(raw),DATA.regionalCampaign,seed,DATA.metadata.baseHash):null;
}
function regionalNotice(text,state="pending"){const n=document.getElementById("regionalNotice");n.textContent=text;n.setAttribute("data-state",state);}
function enterRegional(){
 if(!activeTheater||activeTheater.id!==DATA.regionalCampaign.theaterId)return;
 try{loadRegionalState();}catch(error){regionalState=null;document.getElementById("regionalWorkspace").style.display="";document.getElementById("regionalWorkspace").open=true;regionalNotice("Could not load saved exercise: "+error.message,"error");return;}
 activeRegion=DATA.regionalCampaign;regionalSector=null;regionalHQ="first-army";regionalMission="beachhead";
 view=[...activeRegion.view];document.getElementById("regionChoice").value=activeRegion.id;
 document.getElementById("regionalEntry").open=true;document.getElementById("regionalWorkspace").open=true;
 document.getElementById("mapHeading").textContent=activeRegion.name;document.getElementById("breadcrumb").textContent="Whole Map › "+activeTheater.name+" › Normandy, 8 June 1944";document.getElementById("viewLevel").textContent="REGIONAL CAMPAIGN";renderView();
}
function changeRegional(action){
 try{
  if(!regionalState)throw new Error("Open the regional exercise first.");
  const next=JSON.parse(JSON.stringify(regionalState));
  if(action==="advance")RegionalModel.advance(next);
  else RegionalModel.act(next,regionalMission,action,regionalHQ,document.getElementById("regionalText").value,regionalSector?.id||null);
  localStorage.setItem(regionalKey(),JSON.stringify(next));regionalState=next;
  document.getElementById("regionalText").value="";regionalNotice("Exercise saved. No combat or real order has been executed.","success");renderRegional();updateTheaterCounters();
 }catch(error){regionalNotice(error.message,"error");}
}
function renderRegional(){
 renderCampaignActions();
 const eligible=activeTheater?.id===DATA.regionalCampaign.theaterId;
 document.getElementById("regionalEntry").style.display=activeTheater?"":"none";
 document.getElementById("regionChoice").disabled=!eligible;
 document.getElementById("campaignBrief").textContent=eligible?DATA.regionalCampaign.campaign.windowLabel+". "+DATA.regionalCampaign.campaign.purpose+" "+DATA.regionalCampaign.campaign.timeBasis+" "+DATA.regionalCampaign.campaign.deploymentStatus+" "+DATA.regionalCampaign.campaign.geographyStatus:"No dated campaign package is available for this theater yet. Geography browsing remains available; deployments and campaign dates are not inferred from another theater.";
 document.getElementById("regionalWorkspace").style.display=activeRegion?"":"none";
 if(formationOpen){document.getElementById("regionalWorkspace").style.display="none";document.getElementById("regionalEntry").style.display="none";renderFormation();return;}
 if(!activeRegion||!regionalState)return;
 const c=DATA.regionalCampaign,s=regionalState;
 document.getElementById("regionalStep").textContent="Campaign time: "+s.clock.current.replace("T"," ").replace("Z"," UTC")+" | Ends: "+c.campaign.endExclusive+" | Communication demonstration step "+s.step+" (no clock advance)";
 const hierarchy=document.getElementById("regionalHierarchy");hierarchy.replaceChildren();
 const current=s.forces.find(f=>f.id===regionalHQ);
 const trail=[];for(let node=current;node;node=s.forces.find(f=>f.id===node.parentId))trail.unshift(node);
 const navigate=id=>{regionalHQ=id;regionalMission=null;regionalSector=null;document.getElementById("regionalText").value="";renderRegional();};
 const crumbs=document.getElementById("regionalBreadcrumb");crumbs.replaceChildren();
 for(const node of trail){const b=document.createElement("button");b.textContent=node.name;b.disabled=node.id===regionalHQ;b.onclick=()=>navigate(node.id);crumbs.append(b);}
 const incoming=s.missions.filter(m=>m.recipient===regionalHQ);
 const planned=incoming.length===0||incoming.every(m=>["planned","executing","reported","assessed"].includes(m.status));
 const children=s.forces.filter(f=>f.parentId===regionalHQ);
 const outgoing=s.missions.filter(m=>m.issuer===regionalHQ);
 const ready=planned&&(incoming.length>0||outgoing.every(m=>!["draft","assigned"].includes(m.status)));
 if(ready)for(const child of children){const orders=outgoing.filter(m=>m.recipient===child.id),delivered=orders.length>0&&orders.every(m=>!["draft","assigned"].includes(m.status));const b=document.createElement("button");b.textContent=child.name+(delivered?"":" (assign and deliver order first)");b.disabled=!delivered;b.onclick=()=>navigate(child.id);hierarchy.append(b);}
 document.getElementById("regionalCurrent").textContent=current.name+" | "+current.echelon;
 document.getElementById("regionalNext").textContent=!planned?"Next: save your plan for the received mission. Subordinate orders unlock afterward.":outgoing.some(m=>m.status==="draft")?"Next: select a subordinate mission and Assign it, then advance communications to deliver the order.":outgoing.some(m=>m.status==="assigned")?"Next: advance communications to deliver the assigned order.":children.length?"Next: choose a direct subordinate below to continue. Use the command path above to return for reports or another branch.":"Next: select a map sector and create your Situation Card. Execution and reporting remain available for this headquarters.";
 const force=s.forces.find(f=>f.id===regionalHQ);
 document.getElementById("regionalForce").textContent=force.name+". Strength, readiness, supply and position: unknown. Formation identity persists; no force movement is simulated. "+(force.organizationNote||"")+(force.openingReport?" Opening context: "+force.openingReport.text+" ("+force.openingReport.asOf+").":"");
 const available=s.missions.filter(m=>(m.issuer===regionalHQ&&planned)||(m.recipient===regionalHQ&&!["draft","assigned"].includes(m.status)));
 if(!available.some(m=>m.id===regionalMission))regionalMission=available[0]?.id||null;
 const list=document.getElementById("regionalMissions");list.replaceChildren();
 for(const m of available){const b=document.createElement("button");b.textContent=m.title+" · "+RegionalModel.status(m,regionalHQ);b.setAttribute("aria-pressed",m.id===regionalMission?"true":"false");b.onclick=()=>{regionalMission=m.id;document.getElementById("regionalText").value="";renderRegional();};list.append(b);}
 const m=s.missions.find(m=>m.id===regionalMission),detail=document.getElementById("regionalMissionDetail");detail.replaceChildren();
 if(m){const p=document.createElement("p");p.textContent=m.intent+" Issuer: "+m.issuer+". Recipient: "+m.recipient+". Parent mission: "+(m.parentId||"none")+". Known status: "+RegionalModel.status(m,regionalHQ)+".";detail.append(p);
  for(const [label,key] of [["Planning area","area"],["Boundary","boundary"],["Timing","timing"],["Support","support"]]){if(m[key]){const note=document.createElement("p");note.textContent=label+": "+m[key];detail.append(note);}}
  const children=s.missions.filter(x=>x.parentId===m.id);
  if(children.length){const note=document.createElement("p");note.textContent="Subordinate missions: "+children.length+". Complete and assess their reports before reporting this mission.";detail.append(note);}
  if(m.recipient===regionalHQ&&m.plan){const plan=document.createElement("p");plan.textContent="Your plan: "+m.plan;detail.append(plan);}
  const source=c.sources.find(x=>x.id===m.sourceId);const a=document.createElement("a");a.textContent="Historical context (exercise wording is authored)";a.href=source.url;a.target="_blank";a.rel="noopener";detail.append(a);
 }else detail.textContent="No delivered mission at this headquarters. Advance communications or select the issuing headquarters.";
 for(const action of ["assign","plan","execute","report","assess"]){let enabled=false;if(m){const recipient=m.recipient===regionalHQ,issuer=m.issuer===regionalHQ;enabled=action==="assign"?issuer&&m.status==="draft"&&(!m.parentId||s.missions.some(p=>p.id===m.parentId&&["planned","executing"].includes(p.status))):action==="plan"?recipient&&m.status==="received":action==="execute"?recipient&&m.status==="planned":action==="report"?recipient&&m.status==="executing"&&s.missions.filter(x=>x.parentId===m.id).every(x=>x.status==="assessed"):issuer&&m.status==="reported"&&m.knownToIssuer==="reported";}document.getElementById("regional-"+action).disabled=!enabled;document.getElementById("regional-"+action).style.display=enabled?"":"none";}
 const writing=["plan","report","assess"].some(a=>!document.getElementById("regional-"+a).disabled);
 document.getElementById("regionalText").style.display=writing?"":"none";
 document.getElementById("regionalTextLabel").style.display=writing?"":"none";
 document.getElementById("regionalAdvance").style.display=s.messages.some(m=>m.deliveredAt===null&&(m.from===regionalHQ||m.to===regionalHQ))?"":"none";
 document.getElementById("regionalSector").textContent=regionalSector?"Selected sector: "+regionalSector.id+". Settlements: "+(regionalSector.towns.join(", ")||"none mapped"):"Select a 13 km sector to attach a geographic reference to a plan.";
 const messages=document.getElementById("regionalMessages");messages.replaceChildren();
 for(const message of RegionalModel.messages(s,regionalHQ)){const p=document.createElement("p");p.textContent=message.kind+" | "+message.from+" → "+message.to+" | "+(message.deliveredAt===null?"queued, due step "+message.dueAt:"delivered step "+message.deliveredAt)+": "+message.text;messages.append(p);}
 const blockers=document.getElementById("regionalAdmission");blockers.replaceChildren();for(const text of RegionalModel.admission()){const li=document.createElement("li");li.textContent=text;blockers.append(li);}
 renderSituation();
 document.getElementById("gridScale").textContent="13 km regional sectors. Shared reference geography; no tactical terrain or unit locations inferred.";
}
const regionalObjectives=element("g",{}),regionalObjectiveNodes=[];
for(const o of DATA.regionalCampaign.objectives){const node=element("g",{role:"button",tabindex:0,"aria-label":o.name+" objective reference"},regionalObjectives);element("circle",{r:5,fill:"#b77725",stroke:"#fff","stroke-width":1},node);const label=element("text",{x:9,y:4,"font-size":12,fill:"#594018","paint-order":"stroke",stroke:"#fff","stroke-width":3},node);label.textContent=o.name;const inspect=()=>{regionalNotice(o.name+": approximate objective reference, not a unit location.");};node.addEventListener("click",inspect);node.addEventListener("keydown",e=>{if(e.key==="Enter")inspect();});regionalObjectiveNodes.push({o,node});}
function updateRegional(){
 renderRegional();regionalObjectives.style.display=activeRegion?"":"none";
 if(!activeRegion)return;
 const scale=Math.max(view[2]/(svg.clientWidth||1200),view[3]/(svg.clientHeight||900));for(const {o,node} of regionalObjectiveNodes)node.setAttribute("transform",`translate(${o.x} ${o.y}) scale(${scale})`);
}
document.getElementById("regionChoice").onchange=e=>{if(e.target.value)enterRegional();else{activeRegion=null;regionalSector=null;enterTheater();}};
for(const a of ["assign","plan","execute","report","assess"])document.getElementById("regional-"+a).onclick=()=>changeRegional(a);
document.getElementById("regionalAdvance").onclick=()=>changeRegional("advance");
document.getElementById("regionalExport").onclick=()=>{if(regionalState)download("normandy-planning-exercise.json",{kind:"regional-planning-exercise",executable:false,referenceId:DATA.regionalCampaign.id,referenceSources:DATA.regionalCampaign.sources,state:regionalState,aslAdmissionRequirements:RegionalModel.admission()});};

function clearRegionalExercise(){
 try{
  if(!activeRegion||!regionalState)return;
  if(formationOpen)closeFormation();
  // Remove current and earlier reference-package versions for this campaign only.
  const prefix="regional-exercise-v1-"+seed+"-",keys=[];
  for(let i=0;i<localStorage.length;i++){const key=localStorage.key(i);if(key?.startsWith(prefix))keys.push(key);}
  for(const key of keys)localStorage.removeItem(key);
  if(westernSelected)westernCells.get(westernSelected.id)?.classList.remove("selected");
  westernSelected=null;regionalSector=null;regionalState=null;
  document.getElementById("regionalText").value="";
  enterRegional();
  regionalNotice("Cleared. Start again at First Army by assigning the beachhead mission.","success");
 }catch(error){regionalNotice("Could not finish clearing saved exercise data: "+error.message,"error");}
}
document.getElementById("regionalClear").onclick=clearRegionalExercise;

function renderCampaignActions(){
 const active=!!activeRegion&&!!regionalState,choice=document.getElementById("savedSituationChoice"),selected=choice.value;
 document.getElementById("regionalClear").disabled=!active;
 choice.replaceChildren();
 const cards=active?(regionalState.situations||[]):[];
 for(const card of cards){const option=document.createElement("option");option.value=card.id;option.textContent=card.title+" | "+card.parentFormationId;choice.append(option);}
 choice.value=cards.some(c=>c.id===selected)?selected:(cards[0]?.id||"");choice.disabled=!cards.length||formationOpen;
 document.getElementById("openSavedSituation").disabled=!cards.length||formationOpen;
 document.getElementById("campaignActionHint").textContent=!active?"Choose a theater, then a dated campaign inside Theater Workspace.":formationOpen?"Situation map is open. Clear campaign data restarts this campaign, keeping the map seed.":cards.length?"Open a saved Situation from any headquarters. Clear campaign data removes this campaign's plans, messages and Situations; the map seed is kept.":"No Situations in this campaign version. Follow the command workflow to a battalion, save its plan, select a sector and choose Create Situation Card. Older reference-version saves are kept separately and are not loaded here.";
}
document.getElementById("openSavedSituation").onclick=()=>{
 const card=activeRegion&&regionalState?.situations?.find(s=>s.id===document.getElementById("savedSituationChoice").value);
 if(!card||formationOpen)return;
 regionalHQ=card.parentFormationId;regionalMission=card.parentMissionId;regionalSector=null;
 document.getElementById("regionalText").value="";openFormation();
};
