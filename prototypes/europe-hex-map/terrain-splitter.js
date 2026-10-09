"use strict";
(()=>{
 const previews=document.getElementById("previews"),splitter=document.getElementById("paletteSplitter"),compare=document.getElementById("compare"),key="campaign-atlas:terrain-split:v1";
 let share=50,active=null;
 try{const saved=Number(localStorage.getItem(key));if(Number.isFinite(saved)&&saved>=15&&saved<=85)share=saved;}catch{}
 const apply=()=>{
  previews.style.setProperty("--default-share",share+"fr");
  previews.style.setProperty("--edited-share",(100-share)+"fr");
  splitter.setAttribute("aria-valuenow",String(share));
  splitter.setAttribute("aria-valuetext",share+"% default palette, "+(100-share)+"% edited palette");
 };
 const set=value=>{share=Math.max(15,Math.min(85,Math.round(value)));apply();};
 const save=()=>{try{localStorage.setItem(key,String(share));}catch{}};
 const stop=()=>{if(active===null)return;const id=active;active=null;if(splitter.hasPointerCapture(id))splitter.releasePointerCapture(id);splitter.removeAttribute("data-dragging");save();};
 const move=e=>{const bounds=previews.getBoundingClientRect(),bar=splitter.getBoundingClientRect().height,available=bounds.height-bar;if(available>0)set((e.clientY-bounds.top-bar/2)/available*100);};
 splitter.addEventListener("pointerdown",e=>{if(e.button!==0)return;e.preventDefault();active=e.pointerId;splitter.setPointerCapture(active);splitter.setAttribute("data-dragging","true");});
 splitter.addEventListener("pointermove",e=>{if(active!==e.pointerId)return;e.preventDefault();move(e);});
 splitter.addEventListener("pointerup",stop);
 splitter.addEventListener("pointercancel",stop);
 splitter.addEventListener("lostpointercapture",stop);
 window.addEventListener("blur",stop);
 splitter.addEventListener("dblclick",()=>{set(50);save();});
 splitter.addEventListener("keydown",e=>{
  const step=e.shiftKey?10:2;
  const value={ArrowUp:share-step,ArrowDown:share+step,Home:15,End:85,Enter:50}[e.key];
  if(value===undefined)return;e.preventDefault();set(value);save();
 });
 compare.addEventListener("change",()=>{stop();splitter.hidden=!compare.checked;});
 apply();
})();
