"use strict";
// ETRS89 / LAEA Europe, the same EPSG:3035 frame used by build.py.
window.CampaignGeography=(()=>{
 const version='atlas-board-1',a=6378137,e2=0.00669438002290,e=Math.sqrt(e2),rad=Math.PI/180;
 const anchors={4:[-1.09,49.12],5:[-.25,49.18],6:[6.03,51.74],7:[6.18,48.69],8:[6.1,49],9:[5.86,51.84],10:[5.9,51.98],11:[5.75,51.5],12:[6.75,49.2],13:[6.25,50.33],14:[5.93,50.39],15:[5.93,50.39],16:[5.72,50],17:[5.01,50.23],18:[5.72,50],19:[5.7,49.9],20:[7.23,50.58]};
 function project(lon,lat){const q=p=>{const s=Math.sin(p);return(1-e2)*(s/(1-e2*s*s)-Math.log((1-e*s)/(1+e*s))/(2*e));},qp=q(Math.PI/2),b=Math.asin(q(lat*rad)/qp),b0=Math.asin(q(52*rad)/qp),rq=a*Math.sqrt(qp/2),d=a*Math.cos(52*rad)/Math.sqrt(1-e2*Math.sin(52*rad)**2)/(rq*Math.cos(b0)),dl=(lon-10)*rad,k=rq*Math.sqrt(2/(1+Math.sin(b0)*Math.sin(b)+Math.cos(b0)*Math.cos(b)*Math.cos(dl)));return{x:4321000+k*d*Math.cos(b)*Math.sin(dl),y:3210000+k/d*(Math.cos(b0)*Math.sin(b)-Math.sin(b0)*Math.cos(b)*Math.cos(dl))};}
 const atlas=p=>({x:(p.x-4321000)/1000,y:(3210000-p.y)/1000});
 function register(b){if(b.geography)return b.geography;const ll=anchors[b.sourceNumber];if(!ll)throw Error('Missing continental registration');return{version,crs:'EPSG:3035',anchor:project(...ll),referenceCenter:b.referenceBoard?{x:1130*b.metresPerUnit/2,y:3138*b.metresPerUnit/2}:{x:b.width*b.metresPerUnit/2,y:b.height*b.metresPerUnit/2},provenance:'Authored board footprint near the named operation. Source artwork is not surveyed historical geography.'};}
 function local(b,p){if(b.referenceBoard){const uv=SquadSpace.unproject(b.layout.find(x=>x.boardId===b.referenceBoard),p);return{x:uv.u*1130*b.metresPerUnit,y:uv.v*3138*b.metresPerUnit};}return{x:p.x*b.metresPerUnit,y:p.y*b.metresPerUnit};}
 function world(b,p){const r=register(b),v=local(b,p);return{x:r.anchor.x+v.x-r.referenceCenter.x,y:r.anchor.y-v.y+r.referenceCenter.y};}
 function board(b,p){const r=register(b),v={x:p.x-r.anchor.x+r.referenceCenter.x,y:r.anchor.y-p.y+r.referenceCenter.y};if(b.referenceBoard)return SquadSpace.project(b.layout.find(x=>x.boardId===b.referenceBoard),v.x/b.metresPerUnit/1130,v.y/b.metresPerUnit/3138);return{x:v.x/b.metresPerUnit,y:v.y/b.metresPerUnit};}
 const footprint=b=>[{x:0,y:0},{x:b.width,y:0},{x:b.width,y:b.height},{x:0,y:b.height}].map(p=>world(b,p));
 function features(d,b){return d.mapModel.routes.map(r=>({id:b.spaceId+':'+r.id,kind:r.kind,points:r.geometry.coordinates.map(([u,v])=>world(b,SquadSpace.project(b.layout.find(p=>p.boardId===r.geometry.boardId),u,v)))}));}
 return{version,anchors,project,atlas,register,world,board,footprint,features};
})();
