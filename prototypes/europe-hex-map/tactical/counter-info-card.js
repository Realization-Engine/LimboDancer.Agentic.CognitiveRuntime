"use strict";
window.SquadInfoCard = (()=>{
 const el=(tag,text,cls)=>{const n=document.createElement(tag);if(text!==undefined)n.textContent=String(text);if(cls)n.className=cls;return n;};
 const names={'designation':'Designation','firepower':'Firepower','range':'Range (hexes)','morale':'Morale','class':'Class','smoke-exponent':'Smoke exponent','caliber':'Caliber (mm)','caliber-suffix':'Barrel suffix','movement-type':'Movement type','movement-points':'Movement points','af-front':'Front armor','af-side':'Side armor','rate-of-fire':'Rate of fire','manhandling':'Manhandling','vehicle-role':'Role',aamg:'AA machine gun',ife:'Infantry fire equivalent','passenger-capacity':'Passenger capacity','range-minimum':'Minimum range','range-maximum':'Maximum range',weaponType:'Weapon type',attack:'Attack',defense:'Defense',movement:'Movement'};
 const value=v=>Array.isArray(v)?v.join(', '):typeof v==='object'?JSON.stringify(v):String(v);
 function table(values){const dl=el('dl',undefined,'counter-card-values');for(const [k,v] of Object.entries(values||{})){dl.append(el('dt',names[k]||k.replaceAll('-',' ')),el('dd',value(v)));}return dl;}
 function content(d,type,u,parent){
  const profile=SquadProfiles.types[type.id],def=ScenarioTroopQuality.definition(d,type.id,u),root=el('div');
  const identity=ScenarioAffiliation.resolve(d,u);
  root.append(el('h3',(identity.affiliation?identity.affiliation+' | ':'')+type.label),el('p',u.label+' | '+u.parentId,'counter-card-subtitle'));
  if(identity.formation)root.append(el('p',identity.formation,'counter-card-subtitle'),el('p',identity.basis+': '+identity.note));
  root.append(el('p',def.status==='catalog match'?'ASL catalog values':'Provisional tactical values','counter-card-badge'),table(def.values));
  if(def.quality){
   root.append(el('h4',def.quality.applied?'Scenario quality: veteran rifle infantry':'Scenario quality review'),el('p',def.quality.note));
   if(def.quality.applied)root.append(el('p','Authored assignment of ASL catalog values: 4-6-7 to 4-6-8; smoke exponent 1 to 2. Formation factors and deployment positions are unchanged.'));
   for(const [index,url] of def.quality.sources.entries()){const a=el('a','Historical evidence '+(index+1));a.href=url;a.target='_blank';a.rel='noopener';const para=el('p');para.append(a);root.append(para);}
  }
  const source=el('details');source.open=true;source.append(el('summary','Formation source and conversion'));
  source.append(el('p',profile.evidence?.composition||'Composition not documented.'),el('p',profile.count+' tactical counters per formation'+(profile.evidence?.personnel?' | '+profile.evidence.personnel+' source personnel':'')),el('p',profile.compositionRule));
  if(profile.members)source.append(el('p','Members: '+profile.members.map(m=>m.label).join(', ')));
  source.append(el('h4','Printed formation factors'),table(type.factors),el('p',(profile.evidence?.status||'unverified')+' | '+(profile.evidence?.source||'')+' | printed page '+(profile.evidence?.page||'?')));
  const scan=el('a','View original Unit Function Table page');scan.href='assets/unit-function-tables/page-'+profile.evidence.page+'.png';scan.target='_blank';scan.rel='noopener';source.append(scan);
  root.append(source);
  const more=el('details');more.append(el('summary','Evidence and additional information'),el('p',def.rationale||'ASL catalog definition: '+def.definitionId),el('p','Tactical source: '+def.source),el('p','Original artwork: '+(type.sourceSheet||'')+' | '+(type.sourceCounterId||'')),el('p','Source Situation '+d.situation.number+': '+d.situation.title));
  for(const note of type.notes||[])more.append(el('p',note));
  if(type.organization?.evidence?.note)more.append(el('p','Original import note (before this audit): '+type.organization.evidence.note));
  if(d.deployment?.instructions?.[u.parentId])more.append(el('p',d.deployment.instructions[u.parentId]));
  more.append(el('p','Deployment radius: '+parent.radius+' hexes. Center: '+parent.anchor.q+', '+parent.anchor.r+'.'),el('p','Personnel and supporting weapons listed above remain source information. They do not create additional independent squads, crews or weapons. Combat is not implemented.'));
  root.append(more);return root;
 }
 function bind(host){
  const card=el('aside',undefined,'squad-counter-card');card.hidden=true;card.setAttribute('aria-label','Counter information');card.setAttribute('role','region');
  const toolbar=el('div',undefined,'counter-card-toolbar'),pin=el('button','Keep open'),close=el('button','Close');pin.type=close.type='button';toolbar.append(pin,close);const body=el('div');card.append(toolbar,body);host.append(card);
  let pinned=false,timer;
  const hide=()=>{if(!pinned)card.hidden=true;};
  pin.onclick=()=>{pinned=!pinned;pin.textContent=pinned?'Unpin':'Keep open';pin.setAttribute('aria-pressed',String(pinned));};
  close.onclick=()=>{pinned=false;card.hidden=true;};
  card.onpointerenter=()=>clearTimeout(timer);card.onpointerleave=()=>{timer=setTimeout(hide,250);};
  const escape=e=>{if(e.key==='Escape'){pinned=false;card.hidden=true;}};host.addEventListener('keydown',escape);
  function show(d,type,u,parent,anchor){
   clearTimeout(timer);if(pinned&&!card.hidden)return;body.replaceChildren(content(d,type,u,parent));card.scrollTop=0;card.hidden=false;
   const rect=anchor.getBoundingClientRect(),bounds=host.getBoundingClientRect(),w=Math.min(360,innerWidth-24);
   card.style.width=w+'px';card.style.maxHeight=Math.max(180,Math.min(innerHeight-24,bounds.height-24))+'px';
   card.style.left=Math.max(12,Math.min(innerWidth-w-12,rect.right+12))+'px';card.style.top=Math.max(12,Math.min(innerHeight-card.offsetHeight-12,rect.top))+'px';
  }
  return {attach(g,d,type,u,parent){g.addEventListener('pointerenter',e=>{if(!e.buttons)show(d,type,u,parent,g);});g.addEventListener('pointerleave',()=>{timer=setTimeout(hide,250);});g.setAttribute('aria-keyshortcuts','I');g.addEventListener('keydown',e=>{if(e.key.toLowerCase()==='i'){e.preventDefault();pinned=false;show(d,type,u,parent,g);pinned=true;pin.textContent='Unpin';pin.setAttribute('aria-pressed','true');pin.focus();}});g.addEventListener('focus',()=>show(d,type,u,parent,g));g.addEventListener('blur',()=>{timer=setTimeout(hide,250);});},close(){clearTimeout(timer);host.removeEventListener('keydown',escape);card.remove();}};
 }
 return {bind,content};
})();
