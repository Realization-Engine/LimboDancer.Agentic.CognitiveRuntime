"use strict";
// Supply positions are derived in the oriented board frame from source setup zones.
window.CampaignSupplyPlacement=(()=>{
 const distance=(a,b)=>Math.hypot(a.x-b.x,a.y-b.y);
 function zones(d,layout){
  const types=new Map(d.counters.map(c=>[c.id,c])),sets={Allied:new Set(),German:new Set()};
  for(const i of d.instances){const c=types.get(i.counterTypeId);if(!c||c.kind==='aircraft')continue;const refs=d.deployment?.allowedHexes?.[i.id];
   for(const r of refs||d.boards.filter(b=>b.id===d.setupBoards[c.side]).flatMap(b=>b.hexes.filter(h=>h.setupAllowed).map(h=>({boardId:b.id,hexId:h.id}))))sets[c.side].add(r.boardId+':'+r.hexId);
  }
  const cells=d.boards.flatMap(b=>b.hexes.filter(h=>h.setupAllowed).map(h=>({id:b.id+':'+h.id,boardId:b.id,hexId:h.id,h,p:SquadSpace.project(layout.find(p=>p.boardId===b.id),h.imageCenter.u,h.imageCenter.v)})));
  // Arnhem's separation rule protects the entire Allied enclave, not just the first placed counter.
  for(const rule of d.deployment?.minimumSeparation||[]){const other=cells.filter(c=>sets[rule.fromSide].has(c.id));for(const c of cells)if(other.some(o=>o.boardId===c.boardId&&Math.max(Math.abs(c.h.q-o.h.q),Math.abs(c.h.r-o.h.r),Math.abs(c.h.q+c.h.r-o.h.q-o.h.r))<rule.hexes))sets[rule.side].delete(c.id);}
  return {cells,sets};
 }
 function build(d,layout,side){
  if(d.situation.number===15&&side==='Allied'){
   const source=window.PANZER_SITUATION_LIBRARY.find(d=>d.situation.number===14),oldLayout=SquadSpace.layout(source),base=build(source,oldLayout,side),from=oldLayout.find(p=>p.boardId==='C'),to=layout.find(p=>p.boardId==='C');
   const convert=p=>{const uv=SquadSpace.unproject(from,p);return SquadSpace.project(to,uv.u,uv.v);};
   return {...base,points:base.points.map(convert),entryPath:base.entryPath.map(convert),entry:convert(base.entry),depot:convert(base.depot),receipt:convert(base.receipt),command:{...base.command,p:convert(base.command.p)},hq:{...base.hq,p:convert(base.hq.p)}};
  }
  const z=zones(d,layout),opponent=side==='Allied'?'German':'Allied',enemy=z.cells.filter(c=>z.sets[opponent].has(c.id)),own=z.cells.filter(c=>z.sets[side].has(c.id)&&!z.sets[opponent].has(c.id)&&!['water','marsh'].includes(c.h.terrain.base));
  const friendly=new Set(own.map(c=>c.id)),nearest=(p,cells)=>cells.reduce((best,c)=>!best||distance(c.p,p)<distance(best.p,p)?c:best,null);
  const bins=new Map();for(const c of z.cells){const key=c.boardId+':'+Math.floor(c.p.x/100)+':'+Math.floor(c.p.y/100);if(!bins.has(key))bins.set(key,[]);bins.get(key).push(c);}
  const safe=(p,boardId)=>{const nearby=[];for(let x=-1;x<=1;x++)for(let y=-1;y<=1;y++)nearby.push(...bins.get(boardId+':'+(Math.floor(p.x/100)+x)+':'+(Math.floor(p.y/100)+y))||[]);const c=nearest(p,nearby);return c&&friendly.has(c.id)&&distance(c.p,p)<75;};
  const candidates=[];
  for(const road of d.mapModel.routes.filter(r=>r.kind==='road')){const boardId=road.geometry.boardId,frame=layout.find(p=>p.boardId===boardId),raw=road.geometry.coordinates.map(([u,v])=>SquadSpace.project(frame,u,v));let run=[];
   const finish=()=>{if(run.length>1)candidates.push({boardId,roadId:road.id,points:run});run=[];};
   for(let i=1;i<raw.length;i++){const a=raw[i-1],b=raw[i],count=Math.max(1,Math.ceil(distance(a,b)/12));for(let j=i===1?0:1;j<=count;j++){const t=j/count,p={x:a.x+(b.x-a.x)*t,y:a.y+(b.y-a.y)*t};if(safe(p,boardId))run.push(p);else finish();}}finish();
  }
  const length=ps=>ps.slice(1).reduce((s,p,i)=>s+distance(p,ps[i]),0),clearance=p=>enemy.length?Math.min(...enemy.map(c=>distance(c.p,p))):10000;
  candidates.sort((a,b)=>length(b.points)-length(a.points)||a.roadId.localeCompare(b.roadId));
  if(!candidates.length)throw Error('No friendly source-road supply corridor: '+d.situation.number+' '+side);
  for(const c of candidates)if(clearance(c.points[0])<clearance(c.points.at(-1)))c.points.reverse();
  // Prefer a receiving hex with a complete two-formation-hex footprint clear of enemy setup.
  const radius=2,hexDistance=(a,b)=>Math.max(Math.abs(a.q-b.q),Math.abs(a.r-b.r),Math.abs(a.q+a.r-b.q-b.r));
  const options=candidates.flatMap(candidate=>{const boardCells=z.cells.filter(c=>c.boardId===candidate.boardId);return boardCells.filter(c=>friendly.has(c.id)).map(c=>{const area=boardCells.filter(x=>hexDistance(x.h,c.h)<=radius);const roadIndex=candidate.points.reduce((best,p,i)=>distance(p,c.p)<distance(candidate.points[best],c.p)?i:best,0);return {c,area,roadIndex,candidate};}).filter(o=>o.area.length>=12&&!o.area.some(c=>z.sets[opponent].has(c.id))&&distance(candidate.points[o.roadIndex],o.c.p)<226&&o.roadIndex>0&&Array.from({length:21},(_,i)=>{const start=candidate.points[o.roadIndex],t=i/20;return safe({x:start.x+(o.c.p.x-start.x)*t,y:start.y+(o.c.p.y-start.y)*t},candidate.boardId);}).every(Boolean));});
  options.sort((a,b)=>b.area.length-a.area.length||a.area.filter(c=>!friendly.has(c.id)).length-b.area.filter(c=>!friendly.has(c.id)).length||Math.abs(a.roadIndex/a.candidate.points.length-.75)-Math.abs(b.roadIndex/b.candidate.points.length-.75));
  const selected=options[0];if(!selected)throw Error('No receiving area with a two-hex margin: '+d.situation.number+' '+side);
  const chosen=selected.candidate;
  const points=chosen.points.slice(0,Math.max(1,selected.roadIndex)+1),at=f=>points[Math.round((points.length-1)*f)],entry=points[0],depot=at(.12),receipt=selected.c.p,command=nearest(at(.3),own.filter(c=>c.boardId===chosen.boardId)),hq=selected.c;
  const receivingArea={radius,scale:'formation',edgeClipped:selected.area.length<19,center:{boardId:chosen.boardId,hexId:hq.hexId},hexes:selected.area.filter(c=>!['water','marsh'].includes(c.h.terrain.base)).map(c=>({boardId:c.boardId,hexId:c.hexId})),label:'Campaign supply setup adjustment'};
  // A local pocket is not evidence of an open line back to the continental rear base.
  const isolated=(d.situation.number===10&&side==='Allied')||(d.situation.number===18&&side==='Allied');
  return {...chosen,receivingArea,points:[...points.slice(Math.round((points.length-1)*.12)),receipt],entryPath:points.slice(0,Math.round((points.length-1)*.12)+1),entry,depot,receipt,command,hq,isolated,
   provenance:'Source setup-zone constrained road segment, oriented away from opposing setup areas. Local distribution only; a road is not proof of territorial control.',
   coverage:'Local distribution on board '+chosen.boardId+'. Other deployment pockets require separately secured connections.'};
 }
 function conflicts(d,b,side,placements,units=[]){
  const net=b.sides[side],types=new Map(d.counters.map(c=>[c.id,c])),ids=new Map(d.instances.map(i=>[i.id,types.get(i.counterTypeId)?.side]));
  const result=Object.entries(placements||{}).filter(([id,p])=>ids.get(id)&&ids.get(id)!==side&&b.layout.some(x=>x.boardId===p.board)).filter(([,p])=>{const q=SquadSpace.project(b.layout.find(x=>x.boardId===p.board),p.x,p.y);return net.points.some(x=>distance(x,q)<80);}).map(([id])=>id);
  for(const unit of units)if(unit.side!==side&&unit.hex&&net.points.some(p=>distance(p,SquadSpace.xy(unit.hex))<SquadSpace.step))result.push(unit.id);return result;
 }
 return {zones,build,conflicts};
})();
