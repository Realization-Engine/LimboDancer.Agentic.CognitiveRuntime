"use strict";
window.SquadWorkspace = (() => {
 const ns="http://www.w3.org/2000/svg";
 const el=(tag,text)=>{const n=document.createElement(tag);if(text)n.textContent=text;return n;};
 const node=(tag,attrs,text)=>{const n=document.createElementNS(ns,tag);for(const [k,v]of Object.entries(attrs))n.setAttribute(k,v);if(text)n.textContent=text;return n;};
 const button=(text,fn)=>{const b=el("button",text);b.type="button";b.onclick=fn;return b;};
 let active=null;
 function open(d,plan,parentId,options={}){
  if(active)active.close(false);
  window.CampaignSupplyRelocation?.bind(d,plan);
  const repository=options.storage?SquadRepository.scoped(options.storage):SquadRepository;
  const dialog=el(options.host?"section":"dialog");dialog.className="squad-workspace";dialog.setAttribute("aria-label","Squad map setup");
  const header=el("header"),close=button("Return to formation map",()=>dialog.close());
  header.append(el("h2",d.situation.title+" | Squad map setup"),close);dialog.append(header);
  const status=el("p");status.setAttribute("role","status");dialog.append(status);
  if(options.host){dialog.classList.add("squad-inline");options.host.replaceChildren(dialog);}else{document.body.append(dialog);dialog.showModal();}
  active=dialog;
  let cleanup=()=>{};
  const finish=(notify=true)=>{cleanup();active=null;dialog.remove();if(notify)options.onClose?.();};
  if(options.host)dialog.close=finish;else dialog.onclose=()=>finish();
  let state;
  const download=(data,name)=>{const url=URL.createObjectURL(new Blob([JSON.stringify(data,null,2)],{type:"application/json"}));const a=el("a");a.href=url;a.download=name;a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);};
  try{state=repository.load(d,plan)||SquadDeployment.create(d,plan,parentId);repository.save(d,plan,state);}
  catch(e){
   status.textContent=e.message;
   const prior=repository.raw?.(d);
   if(prior){
    dialog.append(button("Export previous squad setup",()=>download(prior,d.id+"-previous-squad-setup.json")));
    dialog.append(button("Rebuild with audited conversions (keeps backup)",()=>{
     try{
      let next=SquadDeployment.create(d,plan,parentId);
      if(prior.settings?.deploymentRadius!==undefined)next=SquadDeployment.setRadius(next,d,plan,Math.max(6,prior.settings.deploymentRadius));
      if(prior.scope==="full")next=SquadDeployment.expandAll(next,d,plan);
      else for(const p of prior.parents||[])if(plan.placements[p.id])next=SquadDeployment.add(next,SquadTerrain.create(d,next.center),d,plan,p.id);
      repository.archive(d,prior);repository.save(d,plan,next);dialog.close(false);open(d,plan,parentId,options);
     }catch(err){status.textContent=err.message+" Previous setup retained.";}
    }));
   }
   dialog.append(button("Clear saved squad setup",()=>{if(confirm("Clear this squad setup? Formation deployment is retained.")){repository.clear(d);dialog.close();open(d,plan,parentId,options);}}));
   return;
  }
  const requestedGame=options.mode==='game',game=requestedGame&&SquadLifecycle.ready(state,d);
  if(game){state=SquadLifecycle.launch(state,d,plan);repository.save(d,plan,state);}
  dialog.setAttribute('data-squad-mode',game?'game':'setup');dialog.setAttribute('aria-label',game?'Squad game map':'Squad map setup');
  header.querySelector('h2').textContent=d.situation.title+(game?' | Squad game map':' | Squad map setup');
  options.onModeChange?.(game?'squad-game':'squad');
  const changeMode=mode=>{dialog.close(false);open(d,plan,parentId,{...options,mode});};
  const stageButton=button(game?'Review squad deployment':'Complete squad setup and open game map',()=>{
   try{if(!game){state=SquadLifecycle.launch(state,d,plan);repository.save(d,plan,state);}changeMode(game?'setup':'game');}catch(e){reportError(e.message);}
  });header.append(stageButton);
  let terrain=SquadTerrain.create(d,state.center,state.scope==="full"?null:14);
  const controls=el("div");controls.className="squad-controls";
  const relocate=game?null:(side,kind,point)=>{
   const changes=CampaignSupplyRelocation.move(d,plan,side,kind,point,state.units);
   if(!options.onSupplyChange)throw Error('Open this setup through its Situation to save supply changes.');
   options.onSupplyChange(changes);plan.supplyAdjustments=changes;CampaignSupplyRelocation.bind(d,plan);render();
  };
  const supplyLabel=el('label'),supplyToggle=el('input');supplyToggle.type='checkbox';supplyToggle.checked=true;supplyToggle.onchange=()=>render();supplyLabel.append(supplyToggle,el('span','Supply'));controls.append(supplyLabel);
  controls.append(button("Deploy full Situation",()=>attempt(()=>{
   const next=SquadDeployment.expandAll(state,d,plan);repository.save(d,plan,next);state=next;terrain=SquadTerrain.create(d,state.center,null);drawGrid();render();fit();
  })),button("Fit map",()=>fit()),button("Focus selected",()=>focus()),button("Export squad setup",()=>{
   const url=URL.createObjectURL(new Blob([JSON.stringify({...state,supply:d.supplyContext||null,supplyBinding:window.CampaignSupplySpatial?.build(d),aslDefinitions:SquadAslDefinitions,compositionProfiles:SquadProfiles,qualityVersion:ScenarioTroopQuality.version,affiliationVersion:ScenarioAffiliation.version,formationAffiliations:ScenarioAffiliation.roster(d),units:state.units.map(u=>({...u,identity:ScenarioAffiliation.resolve(d,u),effectiveDefinition:ScenarioTroopQuality.definition(d,state.parents.find(p=>p.id===u.parentId).typeId,u)})),sourceCounters:d.counters,sourceRules:d.sourceRules},null,2)],{type:"application/json"}));
   const a=el("a");a.href=url;a.download=d.id+"-squad-setup.json";a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);
  }),button("Clear squad setup",()=>{if(confirm("Clear this squad setup and start again from the selected parent?")){
   repository.clear(d);dialog.close();open(d,plan,parentId,options);
  }}));
  const radius=el("input");radius.type="number";radius.min="0";radius.max="50";radius.step="1";radius.value=state.settings?.deploymentRadius??state.parents[0].radius;radius.setAttribute("aria-label","Deployment radius in hexes");
  const radiusLabel=el("label","Deployment radius (hexes) ");radiusLabel.append(radius);
  controls.append(radiusLabel,button("Apply radius",()=>attempt(()=>{
   const next=SquadDeployment.setRadius(state,d,plan,Number(radius.value));repository.save(d,plan,next);state=next;terrain=SquadTerrain.create(d,state.center,state.scope==="full"?null:14);drawGrid();render();
  })));header.insertBefore(controls,close);
  if(game){radiusLabel.hidden=true;for(const b of controls.querySelectorAll('button'))if(['Deploy full Situation','Clear squad setup','Apply radius'].includes(b.textContent))b.hidden=true;}
  const main=el("div");main.className="squad-main";
  const svg=node("svg",{role:"img","aria-label":"Squad deployment map",tabindex:"0"});
  main.append(svg);dialog.append(main);
  const info=el('details');info.className='squad-details';info.append(el('summary','Details'));
  const detailCard=el('div');detailCard.className='squad-details-card';
  const section=(title,items)=>{const part=el('section');part.append(el('h3',title));const list=el('ul');for(const text of items)list.append(el('li',text));part.append(list);detailCard.append(part);return part;};
  const overview=section('Current Situation',[]);overview.querySelector('ul').remove();overview.append(status);
  const counts=el('p');counts.className='squad-detail-counts';overview.insertBefore(counts,status);
  section('Map controls',[
   'Wheel: zoom. Space + drag or middle-button drag: pan.',
   'Units dropdown: filter by type, nationality or ID. Select a counter to center it without changing zoom.',
   'Hover or focus a counter for its information card. Press I to keep the card open.',
   'Fit map shows the whole Situation. Focus selected zooms to the selected formation.'
  ]);
  const deployment=section('Deployment',[
   game?'Deployment is locked. Use Review squad deployment to reposition counters.':'Select a counter and click a legal hex, or drag it. Highlighted hexes show permitted positions.',
   'The deployment radius is adjustable and saved with this Situation; new setups default to 6 hexes. Equipment remains at radius zero.',
   'Maximum 2 friendly counters per hex. Water blocks all units; marsh blocks vehicles. Woods permit initial deployment.',
   'Aircraft remain off-map. Guns and mortars include their crews.'
  ]);
  const radiusStatus=el('p');radiusStatus.className='squad-detail-counts';deployment.insertBefore(radiusStatus,deployment.querySelector('ul'));
  section('Map scale',[
   'Squad hex centers: 40 m. Source formation spacing: nominally 250 m.',
   'Roads, streams, buildings and seed follow the joined source boards. Terrain classification inherits the source map.'
  ]);
  section('Counter values and limitations',[
   'Values use the ASL catalog or labeled provisional ASL estimates. Only the central Panzer artwork is reused, not its printed combat factors.',
   'Composition follows the original Unit Function Table where available. Infantry groupings and unmatched values remain game estimates.',
   'Leaders and separate support weapons are not generated.',
   'Combat and movement rules are not implemented. This view is not a historical geographic reconstruction.'
  ]);
  info.append(detailCard);controls.append(info);
  const alert=el('p');alert.className='squad-action-alert';alert.setAttribute('role','alert');alert.hidden=true;dialog.insertBefore(alert,main);
  let warningTimer=null,statusBeforeWarning='';
  function clearWarning(){clearTimeout(warningTimer);warningTimer=null;alert.hidden=true;alert.textContent='';}
  function reportError(message){
   if(alert.hidden)statusBeforeWarning=status.textContent;
   clearTimeout(warningTimer);status.textContent=message;alert.textContent=message;alert.hidden=false;
   warningTimer=setTimeout(()=>{clearWarning();status.textContent=statusBeforeWarning;},5000);
  }
  const more=el('details');more.className='squad-details squad-more';more.append(el('summary','More'));
  const extra=el('div');extra.className='squad-details-card';
  for(const child of [...controls.children])if(child===radiusLabel||['Export squad setup','Clear squad setup','Apply radius'].includes(child.textContent))extra.append(child);
  more.append(extra);controls.append(more);
  info.addEventListener('keydown',e=>{if(e.key==='Escape'){info.open=false;info.querySelector('summary').focus();}});
  const dismissDetails=e=>{if(!info.contains(e.target))info.open=false;if(!more.contains(e.target))more.open=false;};
  document.addEventListener('pointerdown',dismissDetails);
  const cards=SquadInfoCard.bind(dialog);
  const artwork=node("g",{});SquadTerrain.draw(artwork,d,terrain);svg.append(artwork);
  const grid=node("path",{fill:"none",stroke:"#536349","stroke-width":.25,"pointer-events":"none"});svg.append(grid);
  function drawGrid(){grid.setAttribute("d",[...terrain.cells.values()].map(h=>"M"+SquadSpace.polygon(h).map(p=>p.join(",")).join("L")+"Z").join(""));}
  drawGrid();
  const supplyLayer=node("g",{});svg.append(supplyLayer);
  const overlay=node("g",{});svg.append(overlay);
  let selected=state.units[0].id;
  const picker=SquadUnitPicker.create(controls,d,id=>{
   selected=id;render();
   const c=SquadSpace.xy(state.units.find(u=>u.id===id).hex),v=svg.getAttribute('viewBox').split(/\s+/).map(Number);
   svg.setAttribute('viewBox',[c.x-v[2]/2,c.y-v[3]/2,v[2],v[3]].join(' '));
  });
  const deployButton=[...controls.children].find(child=>child.tagName==='BUTTON'&&child.textContent==='Deploy full Situation');
  if(deployButton)controls.prepend(deployButton);
  function fit(){if(state.scope==="full"){svg.setAttribute("viewBox",[0,0,Math.max(...terrain.layout.map(p=>p.x+p.w)),Math.max(...terrain.layout.map(p=>p.y+p.h))].join(" "));return;}const c=SquadSpace.xy(state.center),extent=SquadSpace.step*(state.radius+1);svg.setAttribute("viewBox",[c.x-extent,c.y-extent,extent*2,extent*2].join(" "));}
  function focus(){const u=state.units.find(u=>u.id===selected),p=state.parents.find(p=>p.id===u.parentId),c=SquadSpace.xy(p.anchor),extent=SquadSpace.step*Math.max(8,p.radius+2);svg.setAttribute("viewBox",[c.x-extent,c.y-extent,extent*2,extent*2].join(" "));}
  function attempt(fn){try{fn();clearWarning();status.textContent=state.units.length+" counters from "+state.parents.length+" formations saved. Select a counter and click a hex, or drag it. Wheel: zoom. Space + drag or middle drag: pan.";}catch(e){reportError(e.message);}}
  function render(){
   counts.textContent=state.units.length+' counters / '+state.parents.length+' formations';radiusStatus.textContent='Current deployment radius: '+state.settings.deploymentRadius+' hexes';
   stageButton.disabled=!game&&!SquadLifecycle.ready(state,d);
   picker.update(state,selected);overlay.replaceChildren();supplyLayer.replaceChildren();if(supplyToggle.checked&&window.CampaignSupplyView)for(const side of ['Allied','German'])CampaignSupplyView.overlay(supplyLayer,d,{side,size:SquadSpace.step,host:dialog,placements:plan.placements,units:state.units,onRelocate:relocate});
   const unit=state.units.find(u=>u.id===selected),parent=state.parents.find(p=>p.id===unit?.parentId);
   for(const h of !game&&parent?SquadSpace.disk(parent.anchor,parent.radius).map(h=>terrain.cells.get(SquadSpace.id(h))).filter(Boolean):[]){
    const allowed=unit&&SquadDeployment.legal(state,terrain,unit,h);
    const within=parent&&SquadSpace.distance(h,parent.anchor)<=parent.radius;
    const poly=node("polygon",{points:SquadSpace.polygon(h).map(p=>p.join(",")).join(" "),fill:allowed?"#e3b54a":"none","fill-opacity":".20",stroke:within?"#936600":"#536349","stroke-width":within?".7":".25"});
    poly.append(node("title",{},h.q+","+h.r+" | "+h.terrain+" | source "+h.boardId+"/"+h.parentHexId));overlay.append(poly);
   }
   if(!game&&parent){const c=SquadSpace.xy(parent.anchor);overlay.append(node("circle",{cx:c.x,cy:c.y,r:3,fill:"none",stroke:"#a23f17","stroke-width":1}));}
   for(const u of state.units){
    const p=SquadSpace.xy(u.hex),stack=state.units.filter(v=>SquadSpace.id(v.hex)===SquadSpace.id(u.hex)),offset=stack.indexOf(u)*4-2*(stack.length-1);
    const g=node("g",{"data-squad-unit":u.id,transform:"translate("+(p.x+offset)+" "+(p.y+offset)+")",role:"button",tabindex:0,"aria-label":u.parentId+" "+u.label});
    const type=d.counters.find(c=>c.id===state.parents.find(p=>p.id===u.parentId).typeId);
    SquadCounterArt.draw(g,d,type,u,u.id===selected);
    cards.attach(g,d,type,u,state.parents.find(p=>p.id===u.parentId));
    g.onkeydown=e=>{if(e.key==="Enter"||e.key===" "){e.preventDefault();selected=u.id;render();}};
    overlay.append(g);
   }

  }
  const unbind=SquadViewport.bind(svg,(id,h)=>{if(game)return;attempt(()=>{
   const next=JSON.parse(JSON.stringify(state));SquadDeployment.move(next,terrain,id||selected,h);
   repository.save(d,plan,next);state=next;selected=id||selected;render();
  });},id=>{selected=id;render();});
  cleanup=()=>{clearWarning();unbind();cards.close();picker.destroy();document.removeEventListener('pointerdown',dismissDetails);};
  (state.scope==="full"?fit:focus)();render();status.textContent=game?'Deployment locked. Inspect counters and supply.':'Deployment editable. Select a counter to see its legal hexes.';
  window.CampaignMapToolbar?.mount(header);
  if(requestedGame&&!game)reportError('Deploy the full Situation, then complete squad setup to open the game map.');
 }
 return {open,close(notify=true){active?.close(notify);}};
})();
