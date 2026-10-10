"use strict";
window.SquadWorkspace = (() => {
 const ns="http://www.w3.org/2000/svg";
 const el=(tag,text)=>{const n=document.createElement(tag);if(text)n.textContent=text;return n;};
 const node=(tag,attrs,text)=>{const n=document.createElementNS(ns,tag);for(const [k,v]of Object.entries(attrs))n.setAttribute(k,v);if(text)n.textContent=text;return n;};
 const button=(text,fn)=>{const b=el("button",text);b.type="button";b.onclick=fn;return b;};
 let active=null;
 function open(d,plan,parentId){
  if(active){active.close();return;}
  const dialog=el("dialog");dialog.className="squad-workspace";dialog.setAttribute("aria-label","Squad map setup");
  const header=el("header"),close=button("Return to formation map",()=>dialog.close());
  header.append(el("h2",d.situation.title+" | Squad map setup"),close);dialog.append(header);
  const status=el("p");status.setAttribute("role","status");dialog.append(status);
  document.body.append(dialog);dialog.showModal();active=dialog;
  let cleanup=()=>{};
  dialog.onclose=()=>{cleanup();active=null;dialog.remove();};
  let state;
  try{state=SquadRepository.load(d,plan)||SquadDeployment.create(d,plan,parentId);SquadRepository.save(d,plan,state);}
  catch(e){
   status.textContent=e.message;
   dialog.append(button("Clear saved squad setup",()=>{if(confirm("Clear this squad setup? Formation deployment is retained.")){SquadRepository.clear(d);dialog.close();open(d,plan,parentId);}}));
   return;
  }
  let terrain=SquadTerrain.create(d,state.center,state.scope==="full"?null:14);
  const controls=el("div");controls.className="squad-controls";
  const select=el("select");select.setAttribute("aria-label","Add nearby formation");
  for(const i of d.instances.filter(i=>plan.placements[i.id]&&SquadProfiles.types[i.counterTypeId])){
   try{const p=SquadDeployment.parent(d,plan,i.id);if(SquadSpace.distance(p.anchor,state.center)+p.radius>state.radius)continue;
    const o=el("option",i.id);o.value=i.id;select.append(o);
   }catch{}
  }
  const add=button("Add formation",()=>attempt(()=>{
   const next=SquadDeployment.add(state,terrain,d,plan,select.value);SquadRepository.save(d,plan,next);state=next;selected=state.units.find(u=>u.parentId===select.value)?.id;render();
  }));
  controls.append(button("Deploy full Situation",()=>attempt(()=>{
   const next=SquadDeployment.expandAll(state,d,plan);SquadRepository.save(d,plan,next);state=next;terrain=SquadTerrain.create(d,state.center,null);drawGrid();render();fit();
  })),select,add,button("Fit map",()=>fit()),button("Focus selected",()=>focus()),button("Export squad setup",()=>{
   const url=URL.createObjectURL(new Blob([JSON.stringify({...state,aslDefinitions:SquadAslDefinitions},null,2)],{type:"application/json"}));
   const a=el("a");a.href=url;a.download=d.id+"-squad-setup.json";a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);
  }),button("Clear squad setup",()=>{if(confirm("Clear this squad setup and start again from the selected parent?")){
   SquadRepository.clear(d);dialog.close();open(d,plan,parentId);
  }}));dialog.append(controls);
  const main=el("div");main.className="squad-main";
  const roster=el("aside"),svg=node("svg",{role:"img","aria-label":"Squad deployment map",tabindex:"0"});
  main.append(roster,svg);dialog.append(main);
  const info=el("details");info.append(el("summary","Scale and provisional rules"),el("p","40 m between hex centers; source formation spacing is nominally 250 m. Same joined-board roads, streams, buildings and seed. Terrain classification inherits the source map; this is setup, not ASL combat or a geographic reconstruction."),el("p","All St. Lo unit types have provisional decomposition profiles. Gun and mortar counters include their crews. Only central Panzer illustrations are reused. Counter values come from the ASL catalog or labeled provisional ASL estimates. Radius: 4 hexes per parent. Maximum 2 friendly counters per hex. Water blocks all units; marsh blocks vehicles. Woods permit initial deployment; movement rules are separate. Leaders and separate support weapons are not generated. Profiles are provisional game defaults."));
  dialog.append(info);
  const artwork=node("g",{});SquadTerrain.draw(artwork,d,terrain);svg.append(artwork);
  const grid=node("path",{fill:"none",stroke:"#536349","stroke-width":.25,"pointer-events":"none"});svg.append(grid);
  function drawGrid(){grid.setAttribute("d",[...terrain.cells.values()].map(h=>"M"+SquadSpace.polygon(h).map(p=>p.join(",")).join("L")+"Z").join(""));}
  drawGrid();
  const overlay=node("g",{});svg.append(overlay);
  let selected=state.units[0].id;
  function fit(){if(state.scope==="full"){svg.setAttribute("viewBox",[0,0,Math.max(...terrain.layout.map(p=>p.x+p.w)),Math.max(...terrain.layout.map(p=>p.y+p.h))].join(" "));return;}const c=SquadSpace.xy(state.center),extent=SquadSpace.step*(state.radius+1);svg.setAttribute("viewBox",[c.x-extent,c.y-extent,extent*2,extent*2].join(" "));}
  function focus(){const u=state.units.find(u=>u.id===selected),p=state.parents.find(p=>p.id===u.parentId),c=SquadSpace.xy(p.anchor),extent=SquadSpace.step*8;svg.setAttribute("viewBox",[c.x-extent,c.y-extent,extent*2,extent*2].join(" "));}
  function attempt(fn){try{fn();status.textContent=state.units.length+" counters from "+state.parents.length+" formations saved. Select a counter and click a hex, or drag it. Wheel: zoom. Space + drag or middle drag: pan.";}catch(e){status.textContent=e.message;}}
  function render(){
   roster.replaceChildren();overlay.replaceChildren();
   const chosen=state.units.find(u=>u.id===selected),profile=SquadAslDefinitions.types[state.parents.find(p=>p.id===chosen.parentId).typeId];
   const stats=el("details");stats.append(el("summary","Selected unit: ASL values ("+profile.status+")"));
   const table=el("table");for(const [k,v] of Object.entries(profile.values)){const row=el("tr");row.append(el("th",k),el("td",String(v)));table.append(row);}stats.append(table,el("p",profile.rationale||"Direct ASL catalog definition: "+profile.definitionId));roster.append(stats);
   const unit=state.units.find(u=>u.id===selected),parent=state.parents.find(p=>p.id===unit?.parentId);
   for(const p of state.parents){
    const group=el("section");group.append(el("h3",p.id),el("small","Radius "+p.radius+" | "+p.anchor.q+", "+p.anchor.r));
    for(const u of state.units.filter(u=>u.parentId===p.id)){
     const b=button(u.label,()=>{selected=u.id;render();});b.setAttribute("aria-pressed",String(u.id===selected));b.title=u.id;group.append(b);
    }roster.append(group);
   }
   for(const h of parent?SquadSpace.disk(parent.anchor,parent.radius).map(h=>terrain.cells.get(SquadSpace.id(h))).filter(Boolean):[]){
    const allowed=unit&&SquadDeployment.legal(state,terrain,unit,h);
    const within=parent&&SquadSpace.distance(h,parent.anchor)<=parent.radius;
    const poly=node("polygon",{points:SquadSpace.polygon(h).map(p=>p.join(",")).join(" "),fill:allowed?"#e3b54a":"none","fill-opacity":".20",stroke:within?"#936600":"#536349","stroke-width":within?".7":".25"});
    poly.append(node("title",{},h.q+","+h.r+" | "+h.terrain+" | source "+h.boardId+"/"+h.parentHexId));overlay.append(poly);
   }
   if(parent){const c=SquadSpace.xy(parent.anchor);overlay.append(node("circle",{cx:c.x,cy:c.y,r:3,fill:"none",stroke:"#a23f17","stroke-width":1}));}
   for(const u of state.units){
    const p=SquadSpace.xy(u.hex),stack=state.units.filter(v=>SquadSpace.id(v.hex)===SquadSpace.id(u.hex)),offset=stack.indexOf(u)*4-2*(stack.length-1);
    const g=node("g",{"data-squad-unit":u.id,transform:"translate("+(p.x+offset)+" "+(p.y+offset)+")",role:"button",tabindex:0,"aria-label":u.parentId+" "+u.label});
    const type=d.counters.find(c=>c.id===state.parents.find(p=>p.id===u.parentId).typeId);
    SquadCounterArt.draw(g,d,type,u,u.id===selected);
    g.onkeydown=e=>{if(e.key==="Enter"||e.key===" "){e.preventDefault();selected=u.id;render();}};
    overlay.append(g);
   }
   for(const option of select.options)option.disabled=state.parents.some(p=>p.id===option.value);
   if(select.selectedOptions[0]?.disabled)select.value=[...select.options].find(o=>!o.disabled)?.value||"";
   add.disabled=!select.value;
  }
  cleanup=SquadViewport.bind(svg,(id,h)=>attempt(()=>{
   const next=JSON.parse(JSON.stringify(state));SquadDeployment.move(next,terrain,id||selected,h);
   SquadRepository.save(d,plan,next);state=next;selected=id||selected;render();
  }),id=>{selected=id;render();});
  (state.scope==="full"?fit:focus)();render();status.textContent="Saved squad setup | "+state.units.length+" counters from "+state.parents.length+" formations. Highlighted hexes are legal for the selected unit.";
 }
 return {open};
})();
