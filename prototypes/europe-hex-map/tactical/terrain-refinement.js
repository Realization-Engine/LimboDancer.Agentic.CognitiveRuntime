"use strict";
// A stable fine grid over the same source geometry; no independent terrain reroll.
window.SquadTerrain = (() => {
 const cache=new WeakMap();
 function create(d,center,radius=14){
  const key=radius===null?"full":JSON.stringify([center,radius]);
  if(cache.get(d)?.has(key))return cache.get(d).get(key);
  const space=SquadSpace, layout=space.layout(d), cells=new Map();
  const candidates=[];
  if(radius===null){
   const w=Math.max(...layout.map(p=>p.x+p.w)),height=Math.max(...layout.map(p=>p.y+p.h));
   for(let r=0;r<=Math.ceil(height/(space.step*Math.sqrt(3)/2));r++)for(let q=Math.floor(-r/2);q<=Math.ceil(w/space.step-r/2);q++)candidates.push({q,r});
  }else candidates.push(...space.disk(center,radius));
  for(const h of candidates){
   const point=space.xy(h),p=layout.find(p=>point.x>=p.x&&point.x<=p.x+p.w&&point.y>=p.y&&point.y<=p.y+p.h);
   if(!p)continue;
   const uv=space.unproject(p,point),board=d.boards.find(b=>b.id===p.boardId);
   const parent=board.hexes.find(h=>space.inside(uv,h.imagePolygon));
   if(!parent||parent.playable===false)continue;
   const features=d.illustration.features.filter(f=>f.boardId===board.id&&f.polygon&&space.inside(uv,f.polygon.map(([u,v])=>({u,v}))));
   const bridge=features.some(f=>f.kind==="bridge");
   const water=features.some(f=>f.kind==="water")||parent.terrain.base==="water";
   cells.set(space.id(h),{...h,boardId:board.id,parentHexId:parent.id,terrain:water&&!bridge?"water":parent.terrain.base,bridge});
  }
  const result={cells,layout,center,radius,seed:d.illustration.seed,version:"source-grid-1"};
  if(!cache.has(d))cache.set(d,new Map());cache.get(d).set(key,result);return result;
 }
 function draw(svg,d,terrain){
  const ns="http://www.w3.org/2000/svg";
  for(const p of terrain.layout){
   const nested=document.createElementNS(ns,"svg");
   nested.setAttribute("x",p.x);nested.setAttribute("y",p.y);nested.setAttribute("width",p.w);nested.setAttribute("height",p.h);
   nested.setAttribute("viewBox","0 0 "+p.w+" "+p.h);
   const g=document.createElementNS(ns,"g"),t=p.clockwiseDegrees;
   g.setAttribute("transform",t===90?"translate(3138 0) rotate(90)":t===180?"translate(1130 3138) rotate(180)":t===270?"translate(0 1130) rotate(-90)":"");
   PanzerMapArt.draw(g,d,d.boards.find(b=>b.id===p.boardId),null,false,true);
   // Replace the coarse grid, retaining the source features and their stable IDs.
   g.querySelectorAll('polygon[stroke="#746f52"]').forEach(n=>n.remove());
   CampaignAppearance.terrain(g);nested.append(g);svg.append(nested);
  }
 }
 return {create,draw};
})();
