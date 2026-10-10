"use strict";
window.SquadSpace = (() => {
 const W=1130,H=3138, step=113*40/250;
 const distance=(a,b)=>Math.max(Math.abs(a.q-b.q),Math.abs(a.r-b.r),Math.abs(a.q+a.r-b.q-b.r));
 const xy=h=>({x:step*(h.q+h.r/2),y:step*Math.sqrt(3)/2*h.r});
 function hex(p){
  const r=p.y/(step*Math.sqrt(3)/2),q=p.x/step-r/2;
  let x=Math.round(q),z=Math.round(r),y=Math.round(-q-r);
  const dx=Math.abs(x-q),dy=Math.abs(y+q+r),dz=Math.abs(z-r);
  if(dx>dy&&dx>dz)x=-y-z;else if(dz>dy)z=-x-y;
  return {q:x||0,r:z||0};
 }
 const id=h=>h.q+","+h.r;
 function layout(d){
  let x=0,y=0;
  return d.illustration.joinedLayout.map(p=>{
   const turn=p.clockwiseDegrees,w=turn%180?H:W,h=turn%180?W:H;
   const item={...p,x,y,w,h};
   if(d.illustration.layoutAxis==="vertical")y+=h;else x+=w;
   return item;
  });
 }
 function project(p,u,v){
  const x=u*W,y=v*H,t=p.clockwiseDegrees;
  const a=t===90?[H-y,x]:t===180?[W-x,H-y]:t===270?[y,W-x]:[x,y];
  return {x:a[0]+p.x,y:a[1]+p.y};
 }
 function unproject(p,point){
  const x=point.x-p.x,y=point.y-p.y,t=p.clockwiseDegrees;
  const a=t===90?[y,H-x]:t===180?[W-x,H-y]:t===270?[W-y,x]:[x,y];
  return {u:a[0]/W,v:a[1]/H};
 }
 function inside(point,poly){
  let hit=false;
  for(let i=0,j=poly.length-1;i<poly.length;j=i++){
   const a=poly[i],b=poly[j];
   if((a.v>point.v)!==(b.v>point.v)&&point.u<(b.u-a.u)*(point.v-a.v)/(b.v-a.v)+a.u)hit=!hit;
  }return hit;
 }
 function disk(center,radius){
  const result=[];
  for(let q=center.q-radius;q<=center.q+radius;q++)
   for(let r=center.r-radius;r<=center.r+radius;r++)if(distance(center,{q,r})<=radius)result.push({q,r});
  return result.sort((a,b)=>distance(a,center)-distance(b,center)||a.q-b.q||a.r-b.r);
 }
 const polygon=h=>{const p=xy(h);return Array.from({length:6},(_,i)=>{const a=(30+60*i)*Math.PI/180;return [p.x+step/Math.sqrt(3)*Math.cos(a),p.y+step/Math.sqrt(3)*Math.sin(a)];});};
 return {W,H,step,distance,xy,hex,id,layout,project,unproject,inside,disk,polygon};
})();
