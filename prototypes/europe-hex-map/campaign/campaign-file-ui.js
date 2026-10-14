"use strict";
(()=>{
 function resolve(c){
  if(c?.kind==='source'){
   const entry=CampaignNavigation.find(c.source),d=window.PANZER_SITUATION_LIBRARY.find(d=>d.situation.number===c.source);
   if(!entry||!d)throw Error('This Situation is not available in this version.');
   return{prefix:'campaign-atlas:source-operation:'+entry.campaignId+':'+d.id+':',url:'index.html?exercise=source&source='+c.source,validate:w=>CampaignWorld.validate(w,SourceCommand.create(d))};
  }
  if(c?.kind==='normandy')return{prefix:NormandyRepository.KEY,url:'index.html?exercise=normandy',validate:w=>CampaignWorld.validate(w,{...NormandyCommand,...NormandySituation})};
  if(c?.kind==='ardennes')return{prefix:CampaignWorld.KEY,url:'index.html?exercise=ardennes',validate:w=>CampaignWorld.validate(w,ArdennesCommand)};
  throw Error('Unknown campaign type.');
 }
 const storage=CampaignSaveStorage.wrap(localStorage),menu=document.querySelector('.campaign-menu details>div');if(!menu)return;
 const make=(tag,text)=>{const n=document.createElement(tag);if(text)n.textContent=text;return n;};
 const status=make('p');status.setAttribute('role','status');
 const save=make('button','Save campaign to file'),load=make('button','Load campaign from file'),input=make('input');input.type='file';input.accept='.json,application/json';input.hidden=true;input.setAttribute('aria-label','Campaign save file');
 save.onclick=async()=>{save.disabled=true;try{const params=new URLSearchParams(location.search),kind=params.get('exercise'),campaign={kind,...(kind==='source'?{source:Number(params.get('source'))}:{})},file=await (window.AslSituation?.captureCampaign(CampaignFile.capture(storage,campaign,resolve),message=>status.textContent=message)||CampaignFile.capture(storage,campaign,resolve));const blob=new Blob([JSON.stringify(file)],{type:'application/json'});if(blob.size>50*1024*1024)throw Error('Campaign file exceeds the 50 MB load limit. Your current campaign is retained.');const url=URL.createObjectURL(blob),a=make('a');a.href=url;a.download='campaign-atlas-'+kind+(campaign.source?'-'+campaign.source:'')+'-'+file.world.id.slice(0,8)+'.json';a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);status.textContent='Campaign file saved. Includes progress, formation and squad deployments, and supply adjustments.';}catch(e){status.textContent=e.message;}finally{save.disabled=false;}};
 load.onclick=()=>{input.value='';input.click();};
 input.onchange=async()=>{try{const selected=input.files[0];if(!selected)return;if(selected.size>50*1024*1024)throw Error('Save file is too large (maximum 50 MB).');const file=JSON.parse(await selected.text());CampaignFile.check(file,resolve);window.AslSituation?.checkCampaign(file);await window.AslPilot?.validateCampaign(file);
  const dialog=make('dialog'),title=make('h2','Load campaign?'),description=make('p',(file.world.title||'Campaign')+' | saved '+file.savedAt),note=make('p','This opens the saved campaign, including its deployments and supply positions. Your current campaign is retained. A previous save with the same identity is backed up.'),accept=make('button','Load campaign'),cancel=make('button','Cancel'),error=make('p');error.setAttribute('role','alert');dialog.append(title,description,note,accept,cancel,error);document.body.append(dialog);dialog.onclose=()=>dialog.remove();let restoreController;const cancelLoad=()=>{if(accept.disabled){restoreController?.abort();error.textContent='Cancelling restore...';}else dialog.close();};cancel.onclick=cancelLoad;dialog.oncancel=e=>{if(accept.disabled){e.preventDefault();cancelLoad();}};accept.onclick=async()=>{accept.disabled=true;restoreController=new AbortController();try{const restored=await(window.AslSituation?.restoreCampaign(file,message=>error.textContent=message,restoreController.signal)||file);const url=CampaignFile.install(storage,restored,resolve);location.href=url;}catch(e){error.textContent=e.message;accept.disabled=false;cancel.disabled=false;}};dialog.showModal();
 }catch(e){status.textContent='Unable to load campaign: '+e.message;}};
 menu.append(save,load,input,status);
})();
