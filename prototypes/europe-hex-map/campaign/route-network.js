"use strict";
// Deterministic shortest alternate path on intersecting transcribed road segments.
window.CampaignRouteNetwork=(()=>{
 const distance=(a,b)=>Math.hypot(a.x-b.x,a.y-b.y),mix=(a,b,t)=>({x:a.x+(b.x-a.x)*t,y:a.y+(b.y-a.y)*t});
 function path(d,b,side='Allied',destination=null){
  const net=b.sides[side],placement=b.layout.find(p=>p.boardId===net.boardId),segments=[];
  for(const r of d.mapModel.routes.filter(r=>r.kind==='road'&&r.geometry.boardId===net.boardId)){const points=r.geometry.coordinates.map(([u,v])=>SquadSpace.project(placement,u,v));for(let i=1;i<points.length;i++)segments.push({a:points[i-1],b:points[i],cuts:[0,1]});}
  const param=(s,p)=>{const dx=s.b.x-s.a.x,dy=s.b.y-s.a.y,t=((p.x-s.a.x)*dx+(p.y-s.a.y)*dy)/(dx*dx+dy*dy);return t>=-1e-7&&t<=1+1e-7&&distance(mix(s.a,s.b,t),p)<.02?Math.max(0,Math.min(1,t)):null;};
  // Only segments with overlapping bounds can intersect. Sweep along x instead of
  // comparing every pair of the thousands of sampled road segments.
  const ordered=segments.map(s=>({s,minX:Math.min(s.a.x,s.b.x),maxX:Math.max(s.a.x,s.b.x),minY:Math.min(s.a.y,s.b.y),maxY:Math.max(s.a.y,s.b.y)})).sort((a,b)=>a.minX-b.minX);
  for(let i=0;i<ordered.length;i++)for(let j=i+1;j<ordered.length;j++){const a=ordered[i],b=ordered[j];if(b.minX>a.maxX+.02)break;if(b.minY>a.maxY+.02||a.minY>b.maxY+.02)continue;const s=a.s,t=b.s,rx=s.b.x-s.a.x,ry=s.b.y-s.a.y,sx=t.b.x-t.a.x,sy=t.b.y-t.a.y,den=rx*sy-ry*sx;if(Math.abs(den)<1e-8){for(const p of [t.a,t.b]){const u=param(s,p);if(u!==null)s.cuts.push(u);}for(const p of [s.a,s.b]){const v=param(t,p);if(v!==null)t.cuts.push(v);}continue;}const u=((t.a.x-s.a.x)*sy-(t.a.y-s.a.y)*sx)/den,v=((t.a.x-s.a.x)*ry-(t.a.y-s.a.y)*rx)/den;if(u>=0&&u<=1&&v>=0&&v<=1){s.cuts.push(u);t.cuts.push(v);}}
  const start=net.points[0],target=destination||net.points.at(-1),blocked=destination?null:CampaignSupplySpatial.point(net.points,.6);
  const projected=segments.map(s=>{const dx=s.b.x-s.a.x,dy=s.b.y-s.a.y,t=Math.max(0,Math.min(1,((target.x-s.a.x)*dx+(target.y-s.a.y)*dy)/(dx*dx+dy*dy)));return mix(s.a,s.b,t);}).sort((a,b)=>distance(a,target)-distance(b,target));
  const ends=destination?projected.filter(p=>distance(p,target)<=113):[target];if(!ends.length)return null;
  for(const s of segments)for(const p of [start,...ends]){const t=param(s,p);if(t!==null)s.cuts.push(t);}
  const id=p=>p.x.toFixed(2)+','+p.y.toFixed(2),nodes=new Map(),adj=new Map();let removed=0;
  for(const s of segments){const cuts=[...new Set(s.cuts)].sort((a,b)=>a-b);for(let i=1;i<cuts.length;i++){const a=mix(s.a,s.b,cuts[i-1]),z=mix(s.a,s.b,cuts[i]);if(distance(a,z)<.001)continue;if(blocked&&param({a,b:z},blocked)!==null){removed++;continue;}const ka=id(a),kz=id(z);nodes.set(ka,a);nodes.set(kz,z);if(!adj.has(ka))adj.set(ka,[]);if(!adj.has(kz))adj.set(kz,[]);adj.get(ka).push([kz,distance(a,z)]);adj.get(kz).push([ka,distance(a,z)]);}}
  if(!destination&&!removed)return null;const from=id(start),cost=new Map([[from,0]]),prev=new Map(),done=new Set();
  while(true){let current=null;for(const [key,c]of cost)if(!done.has(key)&&(current===null||c<cost.get(current)))current=key;if(current===null)break;done.add(current);for(const [next,length]of adj.get(current)||[])if(!cost.has(next)||cost.get(next)>cost.get(current)+length){cost.set(next,cost.get(current)+length);prev.set(next,current);}}
  // A nearer but disconnected road must not hide another reachable approach to the same hex.
  const reachable=ends.filter(p=>cost.has(id(p))).sort((a,b)=>distance(a,target)-distance(b,target)||cost.get(id(a))-cost.get(id(b)));
  if(!reachable.length)return null;const end=reachable[0],to=id(end);
  const points=[];let k=to;while(k){points.unshift(nodes.get(k)||start);if(k===from)break;k=prev.get(k);}if(destination&&distance(points.at(-1),target)>.01)points.push(target);if(destination){const dense=[points[0]];for(let i=1;i<points.length;i++){const count=Math.ceil(distance(points[i-1],points[i])/20);for(let j=1;j<=count;j++)dense.push(mix(points[i-1],points[i],j/count));}points.splice(0,points.length,...dense);}return{points,blocked,km:cost.get(to)*b.metresPerUnit/1000,minutes:Math.max(5,Math.ceil(cost.get(to)*b.metresPerUnit/1000/12*60/5)*5)+5,provenance:'Alternate path follows intersecting source-road segments; no cross-country shortcut.'};
 }
 const alternate=(d,b,side='Allied')=>path(d,b,side);
 const to=(d,b,side,destination)=>path(d,b,side,destination);
 return{alternate,to};
})();
