"use strict";
// Setup-only edits belong to the saved Situation plan, shared by formation and squad views.
window.CampaignSupplyRelocation=(()=>{
 const resolved=new WeakMap();
 const plans=new WeakMap(),distance=(a,b)=>Math.hypot(a.x-b.x,a.y-b.y);
 const bind=(d,plan)=>plans.set(d,plan);
 function apply(d,base){
  const changes=plans.get(d)?.supplyAdjustments;if(!changes)return base;
  const revision=JSON.stringify(changes),cached=resolved.get(d);
  if(cached?.base===base&&cached.revision===revision)return cached.binding;
  const b=JSON.parse(JSON.stringify(base));
  for(const side of ['Allied','German']){const edit=changes[side];if(!edit)continue;const n=b.sides[side];n.provenance='Player-adjusted campaign setup. Original troop setup restrictions are retained, with receiving-area additions. Roads do not establish territorial control.';
   for(const kind of ['hq','supply'])if(edit[kind]){const r=edit[kind],h=d.boards.find(x=>x.id===r.boardId)?.hexes.find(x=>x.id===r.hexId);if(!h||!h.setupAllowed||['water','marsh'].includes(h.terrain.base)||r.boardId!==n.boardId)throw Error('Invalid saved supply position.');const node=n.nodes.find(x=>x.kind===kind);node.position=SquadSpace.project(b.layout.find(x=>x.boardId===r.boardId),h.imageCenter.u,h.imageCenter.v);node.hexId=h.id;}
   if(edit.supply){const route=CampaignRouteNetwork.to(d,b,side,n.nodes.find(x=>x.kind==='supply').position);if(!route)throw Error('No connected road to receiving point.');n.points=route.points;n.km=CampaignSupplySpatial.length(n.points)*b.metresPerUnit/1000;n.minutes=Math.max(5,Math.ceil(n.km/12*60/5)*5);n.receivingArea=edit.area;n.coverage='Player-adjusted supply corridor. Deployment coverage is assessed separately.';}
  }resolved.set(d,{base,revision,binding:b});return b;
 }
 function move(d,plan,side,kind,position,units=[]){
  if(!['supply','hq'].includes(kind))throw Error('Only receiving points and mission HQs can be repositioned.');
  bind(d,plan);const binding=CampaignSupplySpatial.build(d),net=binding.sides[side],frame=binding.layout.find(x=>x.boardId===net.boardId),board=d.boards.find(x=>x.id===net.boardId),uv=SquadSpace.unproject(frame,position),h=PanzerSituationModel.hexAt(d,board.id,uv.u,uv.v);
  if(!h?.setupAllowed||['water','marsh'].includes(h.terrain.base))throw Error('Choose a land hex on Board '+board.id+'.');
  const p=SquadSpace.project(frame,h.imageCenter.u,h.imageCenter.v),enemy=side==='Allied'?'German':'Allied',expanded=new Set(units.map(u=>u.parentId));
  const enemies=[...Object.entries(plan.placements).filter(([id])=>!expanded.has(id)&&d.counters.find(t=>t.id===d.instances.find(i=>i.id===id)?.counterTypeId)?.side===enemy).map(([,r])=>SquadSpace.project(binding.layout.find(x=>x.boardId===r.board),r.x,r.y)),...units.filter(u=>u.side===enemy&&u.hex).map(u=>SquadSpace.xy(u.hex))];
  if(enemies.some(e=>distance(e,p)<80))throw Error('That hex is occupied by opposing forces.');
  const changes=JSON.parse(JSON.stringify(plan.supplyAdjustments||{})),edit=changes[side]||{};edit[kind]={boardId:board.id,hexId:h.id};
  if(kind==='supply'){
   const cells=board.hexes.filter(x=>x.setupAllowed&&SquadSpace.distance(x,h)<=2),refs=cells.filter(x=>!['water','marsh'].includes(x.terrain.base)).map(x=>({boardId:board.id,hexId:x.id}));
   if(enemies.some(e=>cells.some(x=>distance(e,SquadSpace.project(frame,x.imageCenter.u,x.imageCenter.v))<80)))throw Error('Choose a receiving point with a two-hex margin clear of opposing troops.');
   edit.retainedHexes=[...new Map([...(edit.retainedHexes||[]),...net.receivingArea.hexes].map(r=>[r.boardId+':'+r.hexId,r])).values()];
   edit.area={radius:2,scale:'formation',edgeClipped:cells.length<19,center:edit.supply,hexes:refs,label:'Player-adjusted campaign supply setup'};
  }
  changes[side]=edit;const trial={...plan,supplyAdjustments:changes};bind(d,trial);
  try{const next=CampaignSupplySpatial.build(d);if(CampaignSupplyPlacement.conflicts(d,next,side,plan.placements,units).length)throw Error('The supply route crosses opposing deployed forces. Choose another position.');return changes;}finally{bind(d,plan);}
 }
 function drag(marker,svg,onPoint,onError){
  let start=null,moved=false,suppress=false;
  marker.style.cursor='grab';
  marker.addEventListener('pointerdown',e=>{if(e.button!==0)return;e.stopPropagation();start={x:e.clientX,y:e.clientY};moved=false;marker.setPointerCapture(e.pointerId);});
  marker.addEventListener('pointermove',e=>{if(!start)return;e.stopPropagation();moved ||= Math.hypot(e.clientX-start.x,e.clientY-start.y)>4;if(moved)marker.style.cursor='grabbing';});
  marker.addEventListener('pointerup',e=>{if(!start)return;e.stopPropagation();start=null;marker.style.cursor='grab';if(marker.hasPointerCapture(e.pointerId))marker.releasePointerCapture(e.pointerId);if(!moved)return;suppress=true;const p=new DOMPoint(e.clientX,e.clientY).matrixTransform(svg.getScreenCTM().inverse());try{onPoint(p);}catch(err){onError(err.message);}});
  marker.addEventListener('pointercancel',()=>{start=null;moved=false;marker.style.cursor='grab';});
  marker.addEventListener('click',e=>{if(suppress){suppress=false;e.preventDefault();e.stopImmediatePropagation();}},true);
 }
 function retained(d,side){return plans.get(d)?.supplyAdjustments?.[side]?.retainedHexes||[];}
 // Select destination without invoking the map's troop-placement or pan handlers.
 function choose(svg,host,onPoint){
  const root=svg.ownerSVGElement||svg,notice=document.createElement('aside');notice.className='supply-map-card';notice.textContent='Select a destination hex. Escape cancels. ';const cancel=document.createElement('button');cancel.textContent='Cancel';notice.append(cancel);host.querySelector('.supply-map-card')?.remove();host.append(notice);
  const stop=e=>{e.stopImmediatePropagation();};
  const finish=()=>{root.removeEventListener('pointerdown',stop,true);root.removeEventListener('pointerup',stop,true);root.removeEventListener('click',click,true);document.removeEventListener('keydown',key,true);observer.disconnect();notice.remove();};
  const observer=new MutationObserver(()=>{if(!root.isConnected)finish();});observer.observe(host,{childList:true,subtree:true});
  const click=e=>{e.preventDefault();e.stopImmediatePropagation();const p=new DOMPoint(e.clientX,e.clientY).matrixTransform(svg.getScreenCTM().inverse());try{onPoint(p);finish();}catch(err){notice.firstChild.textContent=err.message+' Select another hex or cancel. ';}};
  const key=e=>{if(e.key==='Escape'){e.stopImmediatePropagation();finish();}};cancel.onclick=finish;
  root.addEventListener('pointerdown',stop,true);root.addEventListener('pointerup',stop,true);root.addEventListener('click',click,true);document.addEventListener('keydown',key,true);
 }
 return{bind,apply,move,retained,choose,drag,revision:d=>JSON.stringify(plans.get(d)?.supplyAdjustments||null)};
})();
