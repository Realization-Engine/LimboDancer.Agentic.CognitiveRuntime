"use strict";
// The campaign supplies setup permissions. C# owns terrain, hit geometry and setup validation.
window.AslPilot = (() => {
 const ns='http://www.w3.org/2000/svg';
 const el=(tag,text)=>{const n=document.createElement(tag);if(text!==undefined)n.textContent=text;return n;};
 async function api(path,body,method=body?'POST':'GET',timings){
  const started=performance.now();
  const r=await fetch('/api/asl-pilot'+path,{method,headers:body?{'Content-Type':'application/json'}:{},body:body?JSON.stringify(body):undefined});
  if(r.status===404)throw Error('The ASL pilot requires the campaign pilot host. Open this campaign on http://127.0.0.1:8093 and load your campaign file.');
  const headersAt=performance.now(),text=r.status===204?null:await r.text(),bodyAt=performance.now(),value=text===null?null:JSON.parse(text);if(timings)Object.assign(timings,{serverTiming:r.headers?.get?.("Server-Timing")||null,requestToHeadersMs:headersAt-started,responseBodyMs:bodyAt-headersAt,jsonParseMs:performance.now()-bodyAt});if(!r.ok)throw Error(value?.error||'Pilot request failed.');return value;
 }
 async function open(d,plan,storage,formation=null,rebuild=false){
  // Opt-in local diagnostics only. Timings never enter campaign saves or terrain identities.
  const measuring=new URLSearchParams(location.search).get('pilotMetrics')==='1',started=performance.now(),metrics={},longTasks=[];
  let observer;
  if(measuring&&typeof PerformanceObserver!=='undefined'&&PerformanceObserver.supportedEntryTypes?.includes('longtask')){observer=new PerformanceObserver(list=>longTasks.push(...list.getEntries()));observer.observe({type:'longtask'});}
  const stopMetrics=()=>observer?.disconnect();
  const reportReady=()=>{if(!measuring)return;requestAnimationFrame(()=>requestAnimationFrame(()=>{const ready=performance.now();setTimeout(()=>{longTasks.push(...(observer?.takeRecords()||[]));stopMetrics();console.info('ASL_PILOT_PERFORMANCE '+JSON.stringify({...metrics,openToReadyMs:ready-started,longTaskCount:longTasks.filter(t=>t.startTime>=started&&t.startTime<ready).length,longTaskMaxMs:Math.max(0,...longTasks.filter(t=>t.startTime>=started&&t.startTime<ready).map(t=>t.duration)),longTaskTotalMs:longTasks.filter(t=>t.startTime>=started&&t.startTime<ready).reduce((n,t)=>n+t.duration,0),longTaskSupported:!!observer}));},0);}));};
  const priorFocus=document.activeElement,dialog=el('dialog');dialog.className='asl-pilot';dialog.setAttribute('aria-label','Goodwood ASL deployment pilot');
  const header=el('header'),title=el('strong','Goodwood | ASL deployment pilot'),tools=el('div'),notice=el('p','Opening saved terrain...'),canvas=el('div'),inspector=el('div');
  notice.className='asl-pilot-notice';notice.setAttribute('role','status');canvas.className='asl-pilot-map';inspector.className='asl-pilot-inspector';
  let generationId=null,alive=true,busy=false,view=null,saved=null,selected='',destination='',svg,overlay,timer,box,drag;
  const button=(label,fn)=>{const b=el('button',label);b.type='button';b.onclick=fn;return b;};
  const close=()=>{if(busy){tell('Wait for the current placement to finish.');return;}try{if(view&&saved?.setup&&box)persist({...saved,viewport:box,selected,destination});dialog.close();}catch(e){tell('Unable to save: '+e.message,true);}};dialog.oncancel=e=>{e.preventDefault();close();};const back=button('Return to formation map',close);
  header.append(title,tools,back);dialog.append(header,notice,canvas,inspector);document.body.append(dialog);dialog.showModal();
  dialog.onclose=()=>{alive=false;stopMetrics();clearTimeout(timer);if(!view&&generationId)api('/generate/'+generationId,null,'DELETE').catch(()=>{});dialog.remove();priorFocus?.focus();};
  const tell=(text,warning=false)=>{clearTimeout(timer);notice.textContent=text;notice.dataset.warning=String(warning);notice.hidden=!text;if(warning)timer=setTimeout(()=>{notice.hidden=true;},5000);};
  const persist=next=>{storage.save(next);saved=next;};
  const contextAt=performance.now();const context={campaignId:storage.campaignId,formationSignature:JSON.stringify({placements:Object.entries(plan.placements).sort(([a],[b])=>a.localeCompare(b)),supply:plan.supplyAdjustments||null}),
   alliedParents:CampaignSupplyDeployment.refs(d,'uk-rifle-01').map(r=>r.boardId+'/'+r.hexId).sort(),
   germanParents:CampaignSupplyDeployment.refs(d,'de-rifle-01').map(r=>r.boardId+'/'+r.hexId).sort()};
  try{
   metrics.contextMs=performance.now()-contextAt;
   if(formation){
    context.formation=formation;
    const unit=d.instances.find(i=>i.id===formation.unitId),type=d.counters.find(c=>c.id===unit?.counterTypeId);
    if(!unit||!['uk-rifle','de-rifle','uk-engineer','de-81mm'].includes(type?.id))throw Error('Select a deployed rifle, British engineer or German 81 mm mortar formation.');
    const refs=CampaignSupplyDeployment.refs(d,unit.id).map(r=>r.boardId+'/'+r.hexId).sort();
    context.alliedParents=type.side==='Allied'?refs:[];
    context.germanParents=type.side==='German'?refs:[];
    title.textContent='Goodwood | '+formation.unitId+' | '+formation.anchor;
   }
   saved=storage.load();
   if(!saved){saved={version:1,seed:storage.seed,context};if(!rebuild)persist(saved);}
   if(saved.version!==1||saved.seed!==storage.seed)throw Error('Unsupported pilot save or campaign seed mismatch. The saved deployment has been retained.');
   if(rebuild)saved={version:1,seed:storage.seed,context};
   if(formation && JSON.stringify(saved.context?.formation)!==JSON.stringify(formation))
    throw Error('This formation has moved from '+(saved.context?.formation?.anchor||'its saved anchor')+' to '+formation.anchor+'. Rebuild its local map to continue.');
   if(!formation && JSON.stringify(saved.context)!==JSON.stringify(context))throw Error('Formation placements or supply setup changed since this pilot was saved. Restore that formation setup to resume; the pilot has been retained.');
   if(!saved.bundle){
    const jobStart=await api('/generate',{seed:saved.seed,formation});generationId=jobStart.id;if(!alive){await api('/generate/'+generationId,null,'DELETE');return;}tell('Generating and validating ASL terrain. You can return to the formation map to cancel.');
    while(alive){const job=await api('/generate/'+generationId);if(job.status==='ready')break;if(job.status==='failed'||job.status==='cancelled')throw Error(job.error||'Generation cancelled. Reopen to retry with the same seed.');await new Promise(resolve=>setTimeout(resolve,500));}
   }
   if(!alive)return;
   metrics.mode=saved.bundle?'resume':'generate';metrics.beforeOpenMs=performance.now()-started;const openAt=performance.now();
   view=await api('/open',{seed:saved.seed,bundle:saved.bundle||null,context,setup:saved.setup||null,savedContext:saved.context,handoff:saved.handoff||null},'POST',measuring?metrics:undefined);metrics.openRequestMs=performance.now()-openAt;metrics.serverStagesMs=view.timingsMs;const buildAt=performance.now();
   if(!alive)return;
   const saveAt=performance.now();if(!saved.bundle||!saved.setup||JSON.stringify(saved.context)!==JSON.stringify(context)||saved.setup.contextHash!==view.setup.contextHash||saved.setup.unitCatalog!==view.setup.unitCatalog)persist({...saved,context,bundle:view.bundle,setup:view.setup});metrics.persistMs=performance.now()-saveAt;
   const select=el('select');select.setAttribute('aria-label','Pilot unit');
   for(const side of ['Allied','German']){const group=el('optgroup');group.label=side;for(const u of view.roster.filter(u=>u.side===side)){const o=el('option',u.label);o.value=u.id;group.append(o);}select.append(group);}
   const priorSelected=saved.selected?.replace('/detachment-','/crew-');selected=view.roster.some(u=>u.id===priorSelected)?priorSelected:view.roster[0].id;select.value=selected;select.onchange=()=>{selected=select.value;describe();};
   const hex=el('select');hex.setAttribute('aria-label','Destination ASL hex');const placeholder=el('option','Choose destination hex');placeholder.value='';hex.append(placeholder);
   for(const c of view.cells){const o=el('option',c.hex+' | '+c.terrain+' | Level '+(c.baseLevel??0));o.value=c.hex;hex.append(o);}hex.onchange=()=>{destination=hex.value;describe();};
   const deploy=button('Deploy pilot roster',()=>edit('deploy')),complete=button('Complete setup',()=>edit(saved.setup.complete?'review':'complete'));
   const fit=button('Fit map',()=>{box=[0,0,view.width,view.height];applyBox();});
   const parent=el('input');parent.type='checkbox';const parentLabel=el('label');parentLabel.append(parent,document.createTextNode('Formation parents'));parent.onchange=draw;
   const details=el('details'),summary=el('summary','Details'),card=el('div');details.append(summary,card);details.className='asl-pilot-details';
   for(const [heading,text] of [['Scope',formation?view.roster.length+' units from '+formation.unitId+' at '+formation.anchor+'. '+(['mortar','crew'].includes(view.roster[0].kind)?'Six mortars and six assigned crews have separate identities. Each pair moves together during setup. Mortars use explicitly manufactured game-default profiles. Their assigned crews and weapons are both included in ASL setup. Medium-mortar firing is not yet supported by the ASL engine.':view.roster[0].kind==='engineer'?'Four engineer squads are provisional. No assault-engineer traits or equipment allotment are inferred.':'Four rifle squads are provisional, not a verified historical personnel count.')+' Setup stays within this formation hex.':'Eight infantry squads representing uk-rifle-01 and de-rifle-01. This crop is not the full Goodwood Situation.'],
    ['Setup','Campaign formation permissions include the current supply-margin exception. Two friendly personnel units per hex (pilot limit, not general ASL stacking). A manned mortar shares its crew location and does not count as another personnel unit. Mortar detachments require open ground or paved road. Ground locations at admitted base levels only; bridge decks, streams and the two-hex crop boundary are inspection-only.'],
    ['Terrain','ASL B10.1: ground level is read at the hex centre. Source levels 0-2 map to ASL levels as a scenario assumption, not measured heights. No cliffs are inferred. 40 m ASL spacing. Generated stone buildings are plausible detail, not historical footprints. ASL map facts drive the display and setup checks.'],
    ['Availability','Deployment and inspection only. Movement, LOS, combat and campaign outcome reconciliation are not implemented.'],
    ['Saved map','Seed: '+saved.seed+' | Package: '+saved.bundle.packageVersion]]){card.append(el('h3',heading),el('p',text));}
   const count=el('span');count.className='asl-pilot-count';tools.append(select,deploy,fit,parentLabel,details,complete,count);
   inspector.append(hex,button('Place selected unit',()=>edit('move')),el('span'));
   const handoffDetails=el('details'),handoffCard=el('div');handoffDetails.className='asl-pilot-details';handoffDetails.append(el('summary','ASL handoff'),handoffCard);tools.append(handoffDetails);
   let prepared=view.handoff||null,aslLink=null;
   const prepare=button('Prepare ASL setup',async()=>{if(busy||!alive)return;busy=true;try{const result=await api('/handoff',{session:view.session,setup:saved.setup,action:'prepare'});if(!alive)return;persist({...saved,handoff:result});prepared=result;showHandoff();tell('ASL setup validated and saved to the campaign.');}catch(e){tell(e.message,true);}finally{busy=false;}});
   const launch=button('Open in ASL',async()=>{if(busy||!alive)return;busy=true;launch.disabled=true;try{const result=await api('/launch',{session:view.session,setup:saved.setup,action:'launch'});if(!alive)return;const link=el('a',result.existing?'Continue in ASL':'Enter ASL setup');link.href=result.url;link.target='_blank';link.rel='noopener';aslLink?.remove();aslLink=link;handoffCard.append(link);tell('ASL setup saved. Use the link to open it in a new tab.');}catch(e){tell(e.message,true);}finally{busy=false;launch.disabled=false;}});
   function showHandoff(){handoffCard.replaceChildren(el('p','Campaign defaults are accepted for setup. This prepares an ASL setup record; it does not start combat.'),prepare);if(prepared)handoffCard.append(launch);prepare.disabled=!saved.setup.complete;if(prepared){for(const row of prepared.units)handoffCard.append(el('p',row.label+' | '+row.status+': '+row.reason));}else handoffCard.append(el('p',view.handoffError||'Complete setup, then prepare the handoff.'));}
   showHandoff();
   const unitDetails=el('details'),unitCard=el('div');unitDetails.className='asl-pilot-details';unitDetails.append(el('summary','Unit details'),unitCard);unitCard.setAttribute('aria-label','Selected unit and equipment');tools.append(unitDetails);
   svg=new DOMParser().parseFromString(view.svg,'image/svg+xml').documentElement;
   svg.setAttribute('aria-label','Generated Goodwood ASL terrain');svg.setAttribute('role','img');canvas.tabIndex=0;canvas.setAttribute('role','region');canvas.setAttribute('aria-label','Pilot map. Arrow keys pan, plus and minus zoom, Home fits map.');
   overlay=document.createElementNS(ns,'g');svg.append(overlay);canvas.replaceChildren(svg);
   box=saved.viewport&&saved.viewport.length===4&&saved.viewport.every(Number.isFinite)&&saved.viewport[2]>0&&saved.viewport[3]>0?saved.viewport:[0,0,view.width,view.height];applyBox();
   const point=e=>{const p=svg.createSVGPoint();p.x=e.clientX;p.y=e.clientY;return p.matrixTransform(svg.getScreenCTM().inverse());};
      function zoom(factor,p={x:box[0]+box[2]/2,y:box[1]+box[3]/2}){const w=Math.max(180,Math.min(view.width*2,box[2]*factor)),ratio=w/box[2];box=[p.x-(p.x-box[0])*ratio,p.y-(p.y-box[1])*ratio,w,box[3]*ratio];applyBox();}
   svg.addEventListener('wheel',e=>{e.preventDefault();zoom(e.deltaY>0?1.15:1/1.15,point(e));},{passive:false});
   canvas.onkeydown=e=>{const step=box[2]*.1;if(e.key==='ArrowLeft')box[0]-=step;else if(e.key==='ArrowRight')box[0]+=step;else if(e.key==='ArrowUp')box[1]-=step;else if(e.key==='ArrowDown')box[1]+=step;else if(e.key==='+'||e.key==='=')zoom(1/1.15);else if(e.key==='-')zoom(1.15);else if(e.key==='Home')box=[0,0,view.width,view.height];else return;e.preventDefault();applyBox();};
   svg.onpointerdown=e=>{if(e.button!==0)return;drag={x:e.clientX,y:e.clientY,box:[...box],moved:false};svg.setPointerCapture(e.pointerId);};
   svg.onpointermove=e=>{if(!drag)return;const dx=e.clientX-drag.x,dy=e.clientY-drag.y;drag.moved ||= Math.hypot(dx,dy)>4;if(drag.moved){const scale=svg.getScreenCTM().a;box=[drag.box[0]-dx/scale,drag.box[1]-dy/scale,drag.box[2],drag.box[3]];applyBox();}};
   svg.onpointerup=e=>{const moved=drag?.moved;drag=null;if(moved)return;const p=point(e),c=view.cells.find(c=>contains(c.polygon,p));if(c){destination=c.hex;hex.value=c.hex;describe();}};
   svg.onpointercancel=()=>drag=null;
   destination=view.cells.some(c=>c.hex===saved.destination)?saved.destination:'';hex.value=destination;describe();draw();tell('Select a unit and a destination hex, then Place selected unit. Drag to pan; wheel to zoom. On the focused map, use arrow keys, +/- and Home.');metrics.buildAndPersistMs=performance.now()-buildAt;reportReady();
   function contains(poly,p){let inside=false;for(let i=0,j=poly.length-1;i<poly.length;j=i++){const a=poly[i],b=poly[j];if((a.y>p.y)!==(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;}return inside;}
   function applyBox(){svg.setAttribute('viewBox',box.join(' '));}
   function terrainAllowed(c){return !['mortar','crew'].includes(view.roster.find(u=>u.id===selected)?.kind)||['Open Ground','Paved Road'].includes(c.terrain);}
   function describe(){const inspected=view.roster.find(u=>u.id===selected),operator=view.roster.find(u=>u.id===inspected.holding?.holder),weapons=view.roster.filter(u=>u.holding?.holder===selected);unitCard.replaceChildren(el('strong',inspected.label),el('p','Identity: '+inspected.id));if(operator)unitCard.append(el('p','Operated by: '+operator.label+' ('+operator.id+'). Setup moves both together.'));for(const weapon of weapons)unitCard.append(el('p','Operates: '+weapon.label+' ('+weapon.id+').'));if(inspected.definition)unitCard.append(el('p','ASL catalog: '+inspected.definition.catalog.catalog+' @ '+inspected.definition.catalog.version+' / '+inspected.definition.definition));if(inspected.catalogStatus)unitCard.append(el('p',inspected.catalogStatus));if(inspected.allotmentStatus)unitCard.append(el('p',inspected.allotmentStatus));const c=view.cells.find(c=>c.hex===destination),u=view.roster.find(u=>u.id===selected),allowed=u.side==='Allied'?context.alliedParents:context.germanParents;inspector.lastChild.textContent=c?c.hex+' | '+c.terrain+' | Level '+(c.baseLevel??0)+' | Formation '+(c.parent||'none')+' | '+(c.admitted&&(!allowed.includes(c.parent)||(formation&&c.parent!==formation.anchor))?u.side+' setup is not allowed in this formation hex.':!terrainAllowed(c)?'Mortar detachments require open ground or paved road.':['mortar','crew'].includes(u.kind)&&c.admitted?'Mortar detachment setup admitted (pilot policy).':c.reason):'';draw();}
   function draw(){
    overlay.replaceChildren();const unit=view.roster.find(u=>u.id===selected),allowed=unit.side==='Allied'?context.alliedParents:context.germanParents;
    for(const c of view.cells){const poly=document.createElementNS(ns,'polygon');poly.setAttribute('points',c.polygon.map(p=>p.x+','+p.y).join(' '));const legal=c.admitted&&terrainAllowed(c)&&allowed.includes(c.parent)&&(!formation||c.parent===formation.anchor);poly.setAttribute('fill',legal&&!saved.setup.complete?'#8d9e49':'none');poly.setAttribute('fill-opacity','.16');poly.setAttribute('stroke',c.hex===destination?'#b97c20':'none');poly.setAttribute('stroke-width',c.hex===destination?'3':'.8');overlay.append(poly);
    }
    if(parent.checked){
     // Shared tactical edges disappear within one formation parent. The remaining edges show ownership boundaries.
     const edges=new Map(),groups=new Map(),key=p=>p.x.toFixed(3)+','+p.y.toFixed(3);
     for(const c of view.cells){if(!c.parent)continue;const group=groups.get(c.parent)||[];group.push(c);groups.set(c.parent,group);
      for(let i=0;i<c.polygon.length;i++){const a=c.polygon[i],b=c.polygon[(i+1)%c.polygon.length],k=[key(a),key(b)].sort().join('|'),edge=edges.get(k);if(edge)edge.shared=edge.parent===c.parent;else edges.set(k,{a,b,parent:c.parent,shared:false});}}
     const path=document.createElementNS(ns,'path');path.setAttribute('d',[...edges.values()].filter(e=>!e.shared).map(e=>'M'+e.a.x+','+e.a.y+'L'+e.b.x+','+e.b.y).join(' '));path.setAttribute('fill','none');path.setAttribute('stroke','#53682c');path.setAttribute('stroke-width','2');overlay.append(path);
     for(const [name,cells] of groups){const x=cells.reduce((n,c)=>n+c.x,0)/cells.length,y=cells.reduce((n,c)=>n+c.y,0)/cells.length,t=document.createElementNS(ns,'text');t.setAttribute('x',x);t.setAttribute('y',y);t.setAttribute('text-anchor','middle');t.setAttribute('font-size','16');t.setAttribute('fill','#20281a');t.setAttribute('stroke','#fffdf6');t.setAttribute('stroke-width','3');t.setAttribute('paint-order','stroke');t.textContent=name;overlay.append(t);}
    }
    for(const p of saved.setup.placements){const c=view.cells.find(c=>c.hex===p.hex),u=view.roster.find(u=>u.id===p.unitId),stack=saved.setup.placements.filter(q=>q.hex===p.hex),offset=stack.indexOf(p)*13-6;const g=document.createElementNS(ns,'g'),r=document.createElementNS(ns,'rect'),t=document.createElementNS(ns,'text');r.setAttribute('x',c.x-13+offset);r.setAttribute('y',c.y-13+offset);r.setAttribute('width','26');r.setAttribute('height','26');r.setAttribute('fill',u.side==='Allied'?'#dfc38c':'#30372b');r.setAttribute('stroke',u.id===selected?'#b97c20':'#fffdf6');r.setAttribute('stroke-width','2');t.setAttribute('x',c.x+offset);t.setAttribute('y',c.y+4+offset);t.setAttribute('text-anchor','middle');t.setAttribute('font-size','11');t.setAttribute('fill',u.side==='Allied'?'#20281a':'white');t.textContent=(u.symbol||'R')+u.id.split('-').pop();const title=document.createElementNS(ns,'title');title.textContent=u.label;g.append(title);g.append(r,t);overlay.append(g);}
    count.textContent=saved.setup.placements.length+'/'+view.roster.length+' deployed';for(const o of select.options){const unit=view.roster.find(u=>u.id===o.value),placement=saved.setup.placements.find(p=>p.unitId===o.value);o.textContent=unit.label+(placement?' | '+placement.hex:' | not placed');}complete.textContent=saved.setup.complete?'Review setup':'Complete setup';deploy.disabled=saved.setup.complete;select.disabled=false;
   }
   async function edit(action){if(busy||!alive)return;busy=true;try{const setup=await api('/edit',{session:view.session,setup:saved.setup,action,unitId:selected,hex:destination});if(!alive)return;persist({...saved,setup,viewport:box,handoff:null});prepared=null;view.handoffError=null;showHandoff();draw();tell(setup.complete?'Pilot setup complete. Saved to the campaign.':'Pilot deployment saved.');}catch(e){tell(e.message,true);}finally{busy=false;}}
  }catch(e){stopMetrics();tell(formation&&/Unsupported source terrain|outside covered/.test(e.message)?'This location is not supported by the pilot yet. Choose another supported formation. '+e.message:e.message);notice.dataset.warning='true';
   if(formation && saved?.version===1 && saved.seed===storage.seed){
    inspector.replaceChildren(el('p','Rebuild uses the current formation location and resets its unit placements. The existing saved map stays intact if generation, validation or saving fails, or if you cancel.'));
    inspector.append(button('Rebuild local map',()=>{dialog.close();return open(d,plan,storage,formation,true);}));
   }
  }
 }
 async function validateCampaign(file){
  if(!file.world?.aslPilots)return;
  for(const pilot of Object.values(file.world.aslPilots)){
   if(!pilot||pilot.version!==1||pilot.seed!==file.world.mapSeed||pilot.context?.campaignId!==file.world.id)throw Error('Invalid ASL pilot campaign binding.');
   if(pilot.bundle)await api('/open',{seed:pilot.seed,bundle:pilot.bundle,context:pilot.context,setup:pilot.setup});
   else if(pilot.setup)throw Error('ASL deployment is missing its accepted terrain package.');
  }
 }
 return {open,validateCampaign};
})();
