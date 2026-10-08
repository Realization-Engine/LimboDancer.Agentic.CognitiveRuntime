// Presentation only: command authority and delivery rules remain in RegionalModel.
// Loaded after app.js so the original renderer has completed initialization.
let workflowStage="choose",workflowIdentity=null,workflowSectorPackage=null;
const workflowDrafts=new Map();
const workflowStages=["choose","brief","command","prepare","maneuver","review"];
const workflowNode=id=>document.getElementById(id);
function workflowMission(){return activeRegion&&regionalState?.missions.find(m=>m.id===regionalMission);}
function workflowDraftKey(){
 const m=workflowMission();if(!m)return null;
 const action=["plan","report","assess"].find(a=>!workflowNode("regional-"+a).disabled);
 return action?"atlas-draft-v1|"+regionalKey()+"|"+regionalHQ+"|"+m.id+"|"+action:null;
}
function rememberWorkflowDraft(){
 const key=workflowDraftKey();if(!key)return;
 const value=workflowNode("regionalText").value;
 workflowDrafts.set(key,value);
 try{localStorage.setItem(key,value);workflowNode("workflowDraftStatus").textContent="Draft saved in this browser. Not submitted.";}
 catch{workflowNode("workflowDraftStatus").textContent="Draft kept for this session only. Browser storage is unavailable; copy your text before closing.";}
}
function restoreWorkflowDraft(){
 const key=workflowDraftKey();if(!key){workflowNode("workflowDraftStatus").textContent="";return;}
 try{if(!workflowDrafts.has(key)){const saved=localStorage.getItem(key);if(saved!==null&&saved!==undefined)workflowDrafts.set(key,saved);}}catch{}
 if(workflowDrafts.has(key))workflowNode("regionalText").value=workflowDrafts.get(key);
}
function showWorkflowStage(stage){
 if(window.CampaignSituationWorkflow?.active){window.CampaignSituationWorkflow.showStage(stage);return;}
 if(!workflowStages.includes(stage)||workflowNode("stage-"+stage).disabled)return;
 if(formationOpen&&stage!=="maneuver")closeFormation();
 if(stage==="maneuver"&&!formationOpen){openFormation();if(!formationOpen)return;}
 workflowStage=stage;renderWorkflow();workflowNode("workflowInspector").focus?.();
}
function renderWorkflow(){
 if(window.CampaignSituationWorkflow?.active){window.CampaignSituationWorkflow.render();return;}
 window.CampaignSituationWorkflow?.hide();
 const active=!!activeRegion&&!!regionalState,m=workflowMission();
 const force=active?regionalState.forces.find(f=>f.id===regionalHQ):null;
 const card=active?regionalState.situations?.find(s=>s.parentMissionId===regionalMission):null;
 const known=m?RegionalModel.status(m,regionalHQ):"";
 const canPrepare=!!m&&m.recipient===regionalHQ&&force?.echelon==="Battalion"&&(["planned","executing"].includes(m.status)||!!card);
 const identity=[active?regionalKey():"atlas",regionalHQ,regionalMission,known].join("|");
 if(identity!==workflowIdentity){workflowIdentity=identity;workflowStage=!active?"choose":canPrepare?"prepare":["reported","assessed"].includes(known)?"review":"command";}
 if(formationOpen)workflowStage="maneuver";
 else if(workflowStage==="maneuver")workflowStage=canPrepare?"prepare":"command";
 if(!active)workflowStage="choose";
 const availability={choose:true,brief:active,command:active,prepare:canPrepare,maneuver:!!card&&(!!card.formation||!!card.tacticalTest),review:active};
 if(!availability[workflowStage])workflowStage=active?"command":"choose";
 const reasons={brief:"Open a dated exercise first.",command:"Open a dated exercise first.",prepare:"Receive and save a battalion mission plan first.",maneuver:"Prepare a Situation and choose its setup first.",review:"Open an exercise to review communications and exports."};
 for(const stage of workflowStages){const b=workflowNode("stage-"+stage);b.disabled=!availability[stage];b.setAttribute("aria-current",stage===workflowStage?"step":"false");b.title=availability[stage]?"Open "+stage:reasons[stage];}
 workflowNode("shell").setAttribute("data-workflow-stage",workflowStage);
 workflowNode("workflowContext").textContent=active?"Normandy, 8 June 1944 / "+force.name:activeTheater?activeTheater.name+" / Reference geography":"Campaign Atlas / Explore or choose an exercise";
 workflowNode("workflowStatus").textContent=active?(m?m.title+" · "+known:"Select a mission"):"Geography browsing";
 workflowNode("commandNavigation").style.display=active?"":"none";
 workflowNode("workflowChoose").hidden=workflowStage!=="choose";
 workflowNode("workflowBrief").hidden=workflowStage!=="brief";
 workflowNode("regionalWorkspace").style.display=active&&["command","prepare","review"].includes(workflowStage)?"":"none";
 workflowNode("formationControls").style.display=formationOpen?"":"none";
 workflowNode("preparationPanel").hidden=!canPrepare||workflowStage==="command";
 workflowNode("missionInspectorTitle").textContent=workflowStage==="prepare"?"Campaign Situation Card":workflowStage==="review"?"Review and handoff":"Mission and next action";
 workflowNode("workflowBriefText").textContent=DATA.regionalCampaign.campaign.purpose+" "+DATA.regionalCampaign.campaign.deploymentStatus;
 const hints={choose:"Open Normandy to begin or resume an exercise. Use the theater selector to explore geography.",brief:"Review the objective and limitations, then continue to the command workspace.",command:workflowNode("regionalNext").textContent,prepare:card?"Review the Campaign Situation Card before opening the campaign map.":"Select a sector on the map or in the inspector, then create a Situation Card.",maneuver:"Select a counter. Movement is manual; contact pauses play without resolving combat.",review:"Review delivered communications, submit available reports, or export records. ASL test results cannot update this campaign."};
 workflowNode("workflowHint").textContent=hints[workflowStage];
 const writing=["plan","report","assess"].find(a=>!workflowNode("regional-"+a).disabled);
 const labels={plan:"Mission plan",report:"Report to issuing headquarters",assess:"Assessment of received report"};
 workflowNode("regionalTextLabel").textContent=labels[writing]||"Mission text";
 workflowNode("regionalText").placeholder=writing==="plan"?"Describe objectives, boundaries, timing and support requests.":writing==="report"?"Report what occurred; results are not generated automatically.":"Record your assessment of the received report.";
 for(const a of ["assign","plan","execute","report","assess"])workflowNode("regional-"+a).setAttribute("class",!workflowNode("regional-"+a).disabled?"primary":"");
 const picker=workflowNode("workflowSector");
 if(active&&workflowSectorPackage!==activeRegion.id){
  picker.replaceChildren();const empty=document.createElement("option");empty.value="";empty.textContent="Select on the map or choose here";picker.append(empty);
  for(const cell of DATA.regionalCampaign.cells){const option=document.createElement("option");option.value=cell.id;option.textContent=cell.id+(cell.towns.length?" / "+cell.towns.join(", "):"");picker.append(option);}
  workflowSectorPackage=activeRegion.id;
 }
 picker.value=regionalSector?.id||"";
 restoreWorkflowDraft();
 window.CampaignSituationWorkflow?.renderParent();
}
for(const stage of workflowStages)workflowNode("stage-"+stage).onclick=()=>showWorkflowStage(stage);
workflowNode("workflowStart").onclick=()=>{
 if(!activeRegion){chooseTheater(DATA.regionalCampaign.theaterId);enterRegional();}
 if(activeRegion&&regionalState)showWorkflowStage("brief");
};
workflowNode("workflowBegin").onclick=()=>showWorkflowStage("command");
workflowNode("workflowExplore").onclick=()=>{if(formationOpen)closeFormation();returnEurope();renderWorkflow();workflowNode("theaterChoice").focus?.();};
workflowNode("workflowSector").onchange=e=>{const cell=DATA.regionalCampaign.cells.find(c=>c.id===e.target.value);if(activeRegion&&cell)selectWestern(cell);};
workflowNode("regionalText").addEventListener("input",rememberWorkflowDraft);

// Observe existing render paths, including saved Situations and map selections.
const workflowRegionalRenderer=renderRegional;
renderRegional=function(){workflowRegionalRenderer();renderWorkflow();};
const workflowFormationRenderer=renderFormation;
renderFormation=function(){workflowFormationRenderer();renderWorkflow();};
const workflowSituationRenderer=renderSituation;
renderSituation=function(){workflowSituationRenderer();renderWorkflow();};
const workflowRegionalAction=changeRegional;
changeRegional=function(action){
 const key=workflowDraftKey(),before=workflowMission()?.status;
 rememberWorkflowDraft();workflowRegionalAction(action);
 if(key&&["plan","report","assess"].includes(action)&&workflowMission()?.status!==before){
  workflowDrafts.delete(key);try{localStorage.removeItem(key);}catch{}
  workflowNode("regionalText").value="";workflowNode("workflowDraftStatus").textContent="Submitted and saved.";
 }
};
const workflowSavedSituation=workflowNode("openSavedSituation").onclick;
workflowNode("openSavedSituation").onclick=()=>{
 const card=regionalState?.situations?.find(s=>s.id===workflowNode("savedSituationChoice").value);
 if(card&&!card.formation&&!card.tacticalTest){regionalHQ=card.parentFormationId;regionalMission=card.parentMissionId;renderRegional();showWorkflowStage("prepare");}
 else workflowSavedSituation();
};
const workflowClearExercise=clearRegionalExercise;
clearRegionalExercise=function(){
 const prefix="atlas-draft-v1|regional-exercise-v1-"+seed+"-";
 workflowClearExercise();
 if(!workflowNode("regionalNotice").textContent.startsWith("Cleared."))return;
 for(const key of workflowDrafts.keys())if(key.startsWith(prefix))workflowDrafts.delete(key);
 try{
  const keys=[];for(let i=0;i<localStorage.length;i++){const key=localStorage.key(i);if(key?.startsWith(prefix))keys.push(key);}
  for(const key of keys)localStorage.removeItem(key);
 }catch{workflowNode("workflowDraftStatus").textContent="Exercise cleared, but stored drafts could not be removed.";}
};
workflowNode("regionalClear").onclick=clearRegionalExercise;
renderWorkflow();
