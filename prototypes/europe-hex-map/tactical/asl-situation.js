"use strict";
window.AslSituation = (() => {
 const copy=value=>JSON.parse(JSON.stringify(value)), hash=value=>typeof value==='string'&&/^[a-f0-9]{64}$/.test(value);
 async function api(path,body,method=body?'POST':'GET'){
  const response=await fetch('/api/asl-situation'+path,{method,headers:body?{'Content-Type':'application/json'}:{},body:body?JSON.stringify(body):undefined});
  if(response.status===404)throw Error('Complete ASL setup needs the campaign host. Open this campaign on port 8093.');
  if(response.status===204)return null;
  const text=await response.text();let value;try{value=JSON.parse(text);}catch{throw Error('The campaign host could not complete this request. Your saved campaign is retained.');}
  if(!response.ok)throw Error(value?.error||'Situation request failed.');return value;
 }
 async function wait(job,report=()=>{},signal){
  for(;;){if(signal?.aborted){await api('/jobs/'+job,null,'DELETE');throw Error('Load cancelled. Your current campaign is unchanged.');}const state=await api('/jobs/'+job);report(state);
   if(state.status==='ready'){if(signal?.aborted)throw Error('Load cancelled. Your current campaign is unchanged.');return state.result;}
   if(state.status==='failed'||state.status==='cancelled')throw Error(state.error||'Preparation cancelled. Your accepted save is retained.');
   await new Promise(resolve=>setTimeout(resolve,600));
  }
 }
 function safeUrl(value){const url=new URL(value);if(url.protocol!=='http:'||!['127.0.0.1','localhost','[::1]'].includes(url.hostname)||url.pathname!=='/games/play'||!/^campaign-[a-f0-9]+$/.test(url.searchParams.get('game')||''))throw Error('Unexpected ASL game address.');return url.href;}
 function request(data,plan,storage){
  if(data.situation.number!==5||plan.stage!=='ready')throw Error('Complete Operation Goodwood formation setup first.');
  const formations=data.instances.filter(unit=>data.counters.find(type=>type.id===unit.counterTypeId)?.kind!=='aircraft').map(unit=>{
   const at=plan.placements[unit.id];if(!at)throw Error('Deploy '+unit.id+' on the Formation Map first.');
   return{unitId:unit.id,anchor:at.board+'/'+at.hexId};
  }).sort((a,b)=>a.unitId.localeCompare(b.unitId));
  const prior=storage.load();if(prior&&(prior.version!==1||prior.seed!==storage.seed||!hash(prior.save)||(prior.prepared!==undefined&&!hash(prior.prepared))))throw Error('The accepted Situation save needs compatibility review. It has been retained.');
  if(prior?.url)safeUrl(prior.url);
  return{campaignId:storage.campaignId,seed:storage.seed,formations,saved:prior?.save||null,
   campaignContext:JSON.stringify({sourceId:data.id,formationPlacements:Object.fromEntries(Object.entries(plan.placements).sort(([a],[b])=>a.localeCompare(b))),
    supplyAdjustments:plan.supplyAdjustments||{},supply:data.supplyContext||null,returnPath:'/index.html?exercise=source&source=5'})};
 }
 function checkCampaign(file){
  const refs=file.world?.aslSituations||{},bundles=file.aslSituations||{};
  if(typeof refs!=='object'||Array.isArray(refs)||typeof bundles!=='object'||Array.isArray(bundles))throw Error('Invalid complete ASL Situation saves.');
  for(const key of Object.keys(refs)){
   const ref=refs[key],bundle=bundles[key];
   if(file.campaign?.kind!=='source'||file.campaign.source!==5||key!=='M04'||ref?.version!==1||!hash(ref.save)||ref.seed!==file.world.mapSeed)
    throw Error('Unsupported complete ASL Situation reference.');
   if(!bundle?.save||!bundle.terrain||bundle.save.campaignId!==file.world.id||bundle.save.seed!==ref.seed)
    throw Error('This campaign file is missing its accepted complete ASL terrain or deployment. Save it again from the original campaign host.');
  }
  if(Object.keys(bundles).some(key=>!Object.hasOwn(refs,key)))throw Error('Unreferenced complete ASL Situation data.');
 }
 async function captureCampaign(file,report=()=>{}){
  const result=copy(file),refs=result.world.aslSituations||{};result.aslSituations={};
  for(const [key,ref]of Object.entries(refs)){
   if(ref?.version!==1||!hash(ref.save))throw Error('Invalid complete ASL Situation reference.');
   report('Saving accepted ASL terrain and deployment...');result.aslSituations[key]=await api('/saves/'+ref.save+'/export');
  }
  checkCampaign(result);if(!Object.keys(refs).length)delete result.aslSituations;return result;
 }
 async function restoreCampaign(file,report=()=>{},signal){
  checkCampaign(file);const result=copy(file);
  for(const [key,bundle]of Object.entries(result.aslSituations||{})){
   if(signal?.aborted)throw Error('Load cancelled. Your current campaign is unchanged.');
   const {job}=await api('/import',bundle),id=await wait(job,state=>report('Restoring ASL Situation: '+state.stage),signal);
   if(!hash(id))throw Error('Invalid restored Situation identity.');
   result.world.aslSituations[key]={version:1,seed:bundle.save.seed,save:id};
  }
  delete result.aslSituations;return result;
 }
 async function open(data,plan,storage){
  const el=(tag,text)=>{const node=document.createElement(tag);if(text!==undefined)node.textContent=text;return node;};
  const previousFocus=document.activeElement,dialog=el('dialog');dialog.className='asl-situation';dialog.setAttribute('aria-labelledby','aslSituationTitle');
  const header=el('header'),title=el('h2','Operation Goodwood | Complete ASL setup');title.id='aslSituationTitle';header.append(title);
  const status=el('p','Opening complete Situation...');status.setAttribute('role','status');
  const progress=el('progress');progress.setAttribute('aria-label','Situation preparation progress');
  const content=el('section'),actions=el('footer'),close=el('button','Return to Formation Map');close.type='button';actions.append(close);
  dialog.append(header,status,progress,content,actions);document.body.append(dialog);dialog.showModal();
  let alive=true,job=null,busy=false,ref=storage.load();
  const tell=text=>{if(alive)status.textContent=text;};
  const report=state=>{if(!alive)return;tell(state.stage+(state.total?' | '+state.done+' / '+state.total:''));if(state.total){progress.max=state.total;progress.value=state.done;}else progress.removeAttribute('value');};
  const finish=()=>{if(job)api('/jobs/'+job,null,'DELETE').catch(()=>{});dialog.close();};close.onclick=finish;dialog.oncancel=e=>{e.preventDefault();finish();};
  dialog.onclose=()=>{alive=false;dialog.remove();previousFocus?.focus();};
  async function run(path,body){busy=true;progress.hidden=false;const started=await api(path,body);job=started.job;if(!alive){await api('/jobs/'+job,null,'DELETE');throw Error('Closed');}const value=await wait(job,report);job=null;if(!alive)throw Error('Closed');return value;}
  function render(save){
   content.replaceChildren();actions.replaceChildren(close);progress.hidden=true;busy=false;
   const units=save.roster.ground.flatMap(group=>group.units),issues=new Map(save.setup.review.map(issue=>[issue.unitId,issue.reason]));
   const summary=el('div');summary.className='asl-situation-summary';
   for(const [label,value]of [['Ground formations',save.roster.ground.length],['ASL counters',units.length],['Need review',issues.size],['Off-map aircraft',save.roster.airSupport.aircraft.length]]){const card=el('div');card.append(el('strong',String(value)),el('span',label));summary.append(card);}content.append(summary);
   const search=el('input');search.type='search';search.placeholder='Filter formations, units or equipment';search.setAttribute('aria-label','Filter ASL roster');content.append(search);
   const roster=el('div');roster.className='asl-situation-roster';content.append(roster);
   const positions=new Map(save.setup.positions.map(p=>[p.unitId,p]));
   function list(){roster.replaceChildren();const term=search.value.trim().toLowerCase();
    for(const group of save.roster.ground){const matching=group.units.filter(unit=>(group.id+' '+group.sourceType+' '+unit.id+' '+unit.definition?.definition+' '+(unit.holding?.holder||'')+' '+(issues.get(unit.id)||'')).toLowerCase().includes(term));if(!matching.length)continue;
     const section=el('details'),heading=el('summary',group.id+' | '+group.sourceType+' | '+group.units.length+' counters'+(group.units.some(u=>issues.has(u.id))?' | Review needed':''));section.append(heading);section.open=!!term||group.units.some(u=>issues.has(u.id));
     for(const unit of matching){const row=el('article'),at=positions.get(unit.id);row.append(el('strong',unit.id),el('p',unit.definition?.definition||'Definition needs review'),el('p',at?'Position '+at.column+', '+at.row:'Not placed'));
      if(unit.holding)row.append(el('p','Operated by '+unit.holding.holder));const weapons=group.units.filter(w=>w.holding?.holder===unit.id);if(weapons.length)row.append(el('p','Operates '+weapons.map(w=>w.id).join(', ')));
      if(issues.has(unit.id)){const warning=el('p',issues.get(unit.id));warning.className='asl-situation-warning';row.append(warning);}section.append(row);
     }roster.append(section);
    }if(!roster.childElementCount)roster.append(el('p','No matching units.'));
   }search.oninput=list;list();
   const notes=el('details');notes.append(el('summary','Map and conversion details'),el('p','Complete joined boards B, A and C. Seed: '+save.seed),el('p','Manufactured game-default profiles are identified in the ASL catalog. Aircraft and campaign supply information remain reference records; no new combat rules are added.'),el('p','Existing pilot and earlier squad-map saves are retained separately. Their terrain and manual placements are not replaced by this complete-Situation setup.'));
   notes.append(el('p','The ASL board currently includes an exterior compilation guard. Boundary enforcement and full-scale UI acceptance are still in progress.'));content.append(notes);
   if(units.some(unit=>!positions.has(unit.id))){
    const fill=el('button','Place remaining counters');fill.type='button';actions.prepend(fill);
    fill.onclick=async()=>{if(busy)return;fill.disabled=true;try{
     const id=await run('/saves/'+ref.save+'/place-remaining',{});if(!hash(id))throw Error('Invalid Situation save identity.');
     const updated=await api('/saves/'+id);if(!alive)return;
     ref={version:1,seed:storage.seed,save:id};storage.save(ref);render(updated);
    }catch(e){if(alive){busy=false;job=null;progress.hidden=true;fill.disabled=false;tell(e.message);}}};
   }
   const prepare=el('button',ref?.url?'Recheck ASL handoff':'Prepare and open ASL setup');prepare.type='button';prepare.disabled=!save.setup.ready;
   prepare.onclick=async()=>{if(busy)return;prepare.disabled=true;try{
    const prepared=ref.prepared||await run('/saves/'+ref.save+'/prepare',{});ref={...ref,prepared};storage.save(ref);
    const url=safeUrl(await run('/handoffs/'+prepared+'/launch',{}));
    ref={...ref,url};storage.save(ref);render(save);tell('ASL setup is ready. Open the map below. Returning here preserves your Formation Map.');
   }catch(e){if(alive){busy=false;job=null;progress.hidden=true;prepare.disabled=!save.setup.ready;tell(e.message);}}};actions.prepend(prepare);
   if(ref?.url){const link=el('a','Open ASL map');link.href=safeUrl(ref.url);link.target='_blank';link.rel='noopener';link.className='asl-situation-open';actions.prepend(link);}
   tell(save.setup.ready?'All ground counters are placed. Prepare the handoff to validate them through ASL.':'Review the listed counters. Existing positions are retained. Return to the Formation Map to review their parent placements.');
  }
  try{
   const body=request(data,plan,storage),old=ref,id=await run('/start',body);if(!hash(id))throw Error('Invalid Situation save identity.');
   const save=await api('/saves/'+id);if(!alive)return;ref={version:1,seed:storage.seed,save:id,...(old?.save===id?{prepared:old.prepared,url:old.url}:{})};storage.save(ref);render(save);
  }catch(e){if(alive){busy=false;progress.hidden=true;tell(e.message);}}
 }
 return{open,request,checkCampaign,captureCampaign,restoreCampaign};
})();
