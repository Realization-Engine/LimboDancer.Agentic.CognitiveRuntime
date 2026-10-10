"use strict";
window.SquadViewport = {
 bind(svg,onDrop,onPick){
  let space=false,gesture=null;
  const point=e=>{const p=new DOMPoint(e.clientX,e.clientY);return p.matrixTransform(svg.getScreenCTM().inverse());};
  const box=()=>svg.getAttribute("viewBox").split(" ").map(Number);
  const set=b=>svg.setAttribute("viewBox",b.join(" "));
  const down=e=>{
   if(e.code==="Space"&&!e.target.closest("input,select,textarea")){space=true;e.preventDefault();}
  };
  const up=e=>{if(e.code==="Space")space=false;};
  window.addEventListener("keydown",down);window.addEventListener("keyup",up);
  svg.onwheel=e=>{
   e.preventDefault();const p=point(e),b=box(),scale=Math.exp(Math.max(-1,Math.min(1,e.deltaY/500)));
   const w=Math.max(80,Math.min(9000,b[2]*scale)),ratio=w/b[2];
   set([p.x+(b[0]-p.x)*ratio,p.y+(b[1]-p.y)*ratio,w,b[3]*ratio]);
  };
  svg.onpointerdown=e=>{
   const unit=e.target.closest("[data-squad-unit]")?.getAttribute("data-squad-unit");
   if(e.button!==0&&e.button!==1)return;
   gesture={start:point(e),box:box(),pan:e.button===1||space,unit,x:e.clientX,y:e.clientY};
   svg.setPointerCapture(e.pointerId);e.preventDefault();
  };
  svg.onpointermove=e=>{
   if(!gesture?.pan)return;
   const b=gesture.box,rect=svg.getBoundingClientRect();
   const scale=Math.max(b[2]/rect.width,b[3]/rect.height);
   set([b[0]-(e.clientX-gesture.x)*scale,b[1]-(e.clientY-gesture.y)*scale,b[2],b[3]]);
  };
  svg.onpointerup=e=>{
   if(!gesture)return;
   const g=gesture;gesture=null;
   if(svg.hasPointerCapture(e.pointerId))svg.releasePointerCapture(e.pointerId);
   if(g.pan)return;
   if(g.unit&&Math.hypot(e.clientX-g.x,e.clientY-g.y)<4)onPick(g.unit);
   else onDrop(g.unit,SquadSpace.hex(point(e)));
  };
  svg.onpointercancel=()=>{gesture=null;};
  svg.onauxclick=e=>e.preventDefault();
  const blur=()=>{space=false;gesture=null;};window.addEventListener("blur",blur);
  return ()=>{window.removeEventListener("keydown",down);window.removeEventListener("keyup",up);window.removeEventListener("blur",blur);};
 }
};
