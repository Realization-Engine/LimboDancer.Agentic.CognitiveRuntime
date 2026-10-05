"use strict";
// Serialize the current SVG view, including CSS and visibility, before rasterizing.
async function exportMapPng(svg, filename) {
 const width=Math.max(1,Math.round(svg.clientWidth)),height=Math.max(1,Math.round(svg.clientHeight));
 const scale=Math.min(2,4096/Math.max(width,height));
 const clone=svg.cloneNode(true);
 clone.setAttribute("xmlns","http://www.w3.org/2000/svg");
 clone.setAttribute("width",width);clone.setAttribute("height",height);
 const properties=["display","visibility","opacity","fill","fill-opacity","fill-rule","stroke","stroke-opacity","stroke-width","stroke-dasharray","stroke-dashoffset","stroke-linecap","stroke-linejoin","stroke-miterlimit","vector-effect","paint-order","font-family","font-size","font-weight","font-style","letter-spacing","text-anchor","dominant-baseline"];
 const originals=[svg,...svg.querySelectorAll("*")],copies=[clone,...clone.querySelectorAll("*")];
 originals.forEach((node,i)=>{const style=getComputedStyle(node);for(const property of properties)copies[i].style.setProperty(property,style.getPropertyValue(property));});
 const url=URL.createObjectURL(new Blob([new XMLSerializer().serializeToString(clone)],{type:"image/svg+xml;charset=utf-8"}));
 try {
  const image=new Image();
  await new Promise((resolve,reject)=>{image.onload=resolve;image.onerror=()=>reject(new Error("Could not render the map image."));image.src=url;});
  const canvas=document.createElement("canvas");canvas.width=Math.max(1,Math.round(width*scale));canvas.height=Math.max(1,Math.round(height*scale));
  const context=canvas.getContext("2d");if(!context)throw new Error("Image export is unavailable in this browser.");
  context.fillStyle="#dce8e8";context.fillRect(0,0,canvas.width,canvas.height);
  context.drawImage(image,0,0,canvas.width,canvas.height);
  const png=await new Promise((resolve,reject)=>canvas.toBlob(blob=>blob?resolve(blob):reject(new Error("Could not encode the PNG image.")),"image/png"));
  const output=URL.createObjectURL(png);
  try {const a=document.createElement("a");a.href=output;a.download=filename;document.body.append(a);a.click();a.remove();}
  finally {setTimeout(()=>URL.revokeObjectURL(output),1000);}
 } finally {URL.revokeObjectURL(url);}
}
