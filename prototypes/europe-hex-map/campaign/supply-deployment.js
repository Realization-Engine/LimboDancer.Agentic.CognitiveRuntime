"use strict";
// Campaign additions to source setup, shared by formation and squad placement.
window.CampaignSupplyDeployment=(()=>{
 const cache=new WeakMap();
 const type=(d,id)=>d.counters.find(c=>c.id===d.instances.find(i=>i.id===id)?.counterTypeId);
 function area(d,side){return CampaignSupplySpatial.build(d).sides[side].receivingArea;}
 function eligible(d,id){const t=type(d,id);return t&&!['aircraft','block','bridge-equipment'].includes(t.kind)&&!(d.deployment?.loads||[]).some(l=>l.passengerId===id||l.carrierId===id);}
 function refs(d,id){if(!cache.has(d))cache.set(d,new Map());const memo=cache.get(d),revision=window.CampaignSupplyRelocation?.revision(d)||'';if(memo.revision!==revision){memo.clear();memo.revision=revision;}if(memo.has(id))return memo.get(id);const t=type(d,id),original=d.deployment?.allowedHexes?.[id]||d.boards.filter(b=>b.id===d.setupBoards[t?.side]).flatMap(b=>b.hexes.filter(h=>h.setupAllowed).map(h=>({boardId:b.id,hexId:h.id})));const result=eligible(d,id)?[...new Map([...original,...area(d,t.side).hexes,...(window.CampaignSupplyRelocation?.retained(d,t.side)||[])].map(r=>[r.boardId+':'+r.hexId,r])).values()]:original;memo.set(id,result);return result;}
 function seed(s,d,typeId){
  const a=area(d,s.stage),center=d.boards.find(b=>b.id===a.center.boardId).hexes.find(h=>h.id===a.center.hexId);
  const candidates=d.instances.filter(i=>type(d,i.id)?.side===s.stage&&eligible(d,i.id)&&!s.placements[i.id]&&(!typeId||i.counterTypeId===typeId)&&(window.SquadProfiles?.types[i.counterTypeId]?.kind==='squad'||/rifle|engineer/.test(i.counterTypeId)));
  const occupied=new Set(Object.values(s.placements).map(p=>p.board+':'+p.hexId));
  const already=Object.entries(s.placements).filter(([id,p])=>type(d,id)?.side===s.stage&&a.hexes.some(r=>r.boardId===p.board&&r.hexId===p.hexId)).length;
  const locations=a.hexes.map(r=>({...r,h:d.boards.find(b=>b.id===r.boardId).hexes.find(h=>h.id===r.hexId)})).sort((a,b)=>Math.abs(SquadSpace.distance(a.h,center)-1)-Math.abs(SquadSpace.distance(b.h,center)-1)||a.hexId.localeCompare(b.hexId));
  for(const i of candidates.slice(0,Math.max(0,3-already))){const r=locations.find(r=>!occupied.has(r.boardId+':'+r.hexId)&&PanzerSituationModel.allowed(d,i.id,r.boardId,r.hexId,s));if(!r)break;PanzerSituationModel.place(s,d,i.id,r.boardId,r.h.imageCenter.u,r.h.imageCenter.v);occupied.add(r.boardId+':'+r.hexId);}
 }
 function security(d,b,side,placements,units=[]){
  if(!placements)return {status:'deployment not assessed'};
  const net=b.sides[side],a=net.receivingArea,expanded=new Set(units.map(u=>u.parentId)),friendly=[...Object.entries(placements).filter(([id])=>type(d,id)?.side===side&&!expanded.has(id)).map(([,p])=>SquadSpace.project(b.layout.find(f=>f.boardId===p.board),p.x,p.y)),...units.filter(u=>u.side===side&&u.hex).map(u=>SquadSpace.xy(u.hex))];
  const receiving=friendly.filter(p=>Math.hypot(p.x-net.nodes.find(n=>n.kind==='supply').position.x,p.y-net.nodes.find(n=>n.kind==='supply').position.y)<=226).length;
  const exposed=net.points.some(p=>!friendly.some(f=>Math.hypot(p.x-f.x,p.y-f.y)<=339));
  return {status:receiving<1?'exposed receiving point':exposed?'exposed approach':'friendly deployment coverage',receiving,edgeClipped:a.edgeClipped};
 }
 return {area,eligible,refs,seed,security};
})();
