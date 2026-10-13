"use strict";
window.CampaignAirSupportCard={
 button(d,base,host){
  const el=(tag,text)=>{const n=document.createElement(tag);if(text!==undefined)n.textContent=text;return n;};
  const trigger=el('button','Air support ('+d.airSupport.groups.reduce((n,g)=>n+g.instanceIds.length,0)+')');trigger.type='button';trigger.setAttribute('aria-expanded','false');trigger.setAttribute('aria-controls','campaignAirSupportCard');
  trigger.onclick=()=>{
   const prior=host.querySelector('#campaignAirSupportCard');if(prior){prior.remove();trigger.setAttribute('aria-expanded','false');return;}
   const card=el('section');card.id='campaignAirSupportCard';card.className='campaign-air-support-card';card.setAttribute('role','dialog');card.setAttribute('aria-label','Air support');card.tabIndex=-1;
   const close=el('button','Close');close.type='button';const dismiss=()=>{card.remove();trigger.setAttribute('aria-expanded','false');trigger.focus();};close.onclick=dismiss;
   card.append(close,el('h3','Air support'),el('p',d.airSupport.instructions),el('p','Support roster only. Aircraft stay off the ground map; arrival, flight activation and air attacks are not implemented.'));
   for(const g of d.airSupport.groups){const row=el('section');row.append(el('h4',g.id+' | '+g.role+' | '+g.instanceIds.length+' aircraft | earliest turn '+g.availableFromTurn));const instance=d.instances.find(i=>i.id===g.instanceIds[0]),type=d.counters.find(c=>c.id===instance.counterTypeId),art=CampaignAppearance.counterImage(base+type.artwork,type.side,type.label,type.nationality,window.ScenarioAffiliation?.group(d,type.id));art.style.width='64px';art.style.height='64px';row.append(art,el('p',type.label+': '+g.instanceIds.join(', ')));card.append(row);}
   card.onkeydown=e=>{if(e.key==='Escape'){e.stopPropagation();dismiss();}};host.append(card);trigger.setAttribute('aria-expanded','true');card.focus();
  };return trigger;
 }
};
