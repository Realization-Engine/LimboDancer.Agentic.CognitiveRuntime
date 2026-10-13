"use strict";
window.CampaignSupplyView=(()=>{
 const node=(tag,attrs={},text)=>{const e=document.createElementNS('http://www.w3.org/2000/svg',tag);for(const[k,v]of Object.entries(attrs))e.setAttribute(k,v);if(text!==undefined)e.textContent=text;return e;};
 function card(host,report,item){
  host.querySelector('.supply-map-card')?.remove();const card=document.createElement('aside');card.className='supply-map-card';card.setAttribute('role','region');card.setAttribute('aria-label','Supply information');
  const close=document.createElement('button');close.textContent='Close';close.onclick=()=>card.remove();card.append(close);
  const line=(tag,text)=>{const e=document.createElement(tag);e.textContent=text;card.append(e);};
  line('h3',item.label);const gp=CampaignSupplySpatial.toWorld(report.binding,item.position);line('small','Continental position: '+Math.round(gp.x)+' E, '+Math.round(gp.y)+' N | EPSG:3035');line('p','Board '+report.network.boardId+' | '+(item.hexId||report.network.roadId));
  line('p','Campaign supply setup adjustment: two formation hexes around SUP. '+(report.network.receivingArea?.edgeClipped?'Board edge clips this area; full margin unavailable. ':'')+(report.security?.status||'Deployment not assessed')+'. Coverage is not combat control.');
  line('p',report.network.coverage||'Local distribution corridor');if(report.network.isolated)line('p','Isolated force: local stocks only. No established rear supply corridor.');if(report.conflicts?.length)line('p','Opposing units block this corridor: '+report.conflicts.join(', '));line('p',report.network.km.toFixed(1)+' km | '+report.network.minutes+' min travel + 5 min receipt | '+report.routeStatus);
  for(const p of report.packages)line('p',p.commodity+': '+p.quantity+' package | '+p.status+' | '+p.effect);
  if(report.transport)line('p','Transport: '+report.transport.status+' | '+Math.round(report.transport.progress*100)+'% of route');
  line('p',report.note);line('small',report.network.provenance);host.append(card);return card;
 }
 function overlay(svg,d,{operation=d.supplyContext?.operation,mission=d.supplyContext?.mission||'M04',side='Allied',boardId=null,size=65,host=svg.parentElement,onSelect=null,placements=null,units=[],onRelocate=null}={}){
  host?.querySelector('.supply-map-card')?.remove();
  const report=CampaignSupplyState.inspect(d,operation,mission,side),b=report.binding,net=report.network;
  if(boardId&&net.boardId!==boardId)return;
  report.conflicts=CampaignSupplyPlacement.conflicts(d,b,side,placements,units);if(report.conflicts.length){report.closed=true;report.routeStatus='blocked by opposing deployed units';}
  report.security=window.CampaignSupplyDeployment?.security(d,b,side,placements,units);if(!report.closed&&report.security?.status.startsWith('exposed'))report.routeStatus=report.security.status;
  const p=b.layout.find(p=>p.boardId===net.boardId),position=x=>boardId?{x:x.x-p.x,y:x.y-p.y}:x;
  const layer=node('g',{'data-supply-layer':side,'data-supply-version':b.version,'data-supply-status':report.routeStatus});
  const area=net.receivingArea,board=d.boards.find(b=>b.id===area?.center.boardId),frame=b.layout.find(f=>f.boardId===board?.id);
  if(area)for(const ref of area.hexes){const h=board.hexes.find(h=>h.id===ref.hexId);layer.append(node('polygon',{points:h.imagePolygon.map(v=>position(SquadSpace.project(frame,v.u,v.v))).map(p=>p.x+','+p.y).join(' '),fill:side==='Allied'?'#147565':'#314c7e','fill-opacity':.09,stroke:side==='Allied'?'#147565':'#314c7e','stroke-opacity':.45,'stroke-width':1,'pointer-events':'none','data-supply-receiving-area':side}));}
  const points=net.points.map(position);layer.append(node('polyline',{points:points.map(p=>p.x+','+p.y).join(' '),fill:'none',stroke:report.closed?'#a52c23':(report.routeStatus==='threatened'||report.routeStatus.startsWith('exposed'))?'#b77725':side==='Allied'?'#147565':'#314c7e','stroke-width':size*.13,'stroke-dasharray':size*.22+' '+size*.12,'pointer-events':'none','data-supply-route':net.routeId}));
  if(report.transport?.detourPoints)layer.append(node('polyline',{points:report.transport.detourPoints.map(position).map(p=>p.x+','+p.y).join(' '),fill:'none',stroke:'#be8027','stroke-width':size*.12,'pointer-events':'none'}));
  const entries=net.nodes.filter(n=>n.kind!=='entry');
  if(side==='Allied'&&operation?.reserve&&operation.reserve!=='none'){const assigned=String(operation.reserve).includes(mission)||operation.reserve==='committed',base=net.nodes.find(n=>n.kind===(assigned?'hq':'command'));entries.push({id:net.routeId+':reserve',kind:'reserve',label:'Mobile reserve | '+operation.reserve,position:{x:base.position.x+60,y:base.position.y+100}});}
  if(report.transport)entries.push({id:net.routeId+':transport',kind:'transport',label:side+' supply transport',position:CampaignSupplySpatial.point(report.transport.detourPoints||net.points,report.transport.progress)});
  const symbols={depot:'DEP',supply:'SUP',hq:'HQ',transport:'TRK',command:'CMD',reserve:'R1'};
  const occupied=[];
  for(const item of entries){const anchor=position(item.position);let loc={...anchor};for(let i=0;i<30&&occupied.some(p=>Math.abs(p.x-loc.x)<size*1.12&&Math.abs(p.y-loc.y)<size*.85);i++){const angle=i*Math.PI/3,radius=size*(1+Math.floor(i/6));loc={x:anchor.x+Math.cos(angle)*radius,y:anchor.y+Math.sin(angle)*radius};}occupied.push(loc);if(loc.x!==anchor.x||loc.y!==anchor.y)layer.append(node('line',{x1:anchor.x,y1:anchor.y,x2:loc.x,y2:loc.y,stroke:'#314c39','stroke-width':size*.045,'pointer-events':'none'}),node('circle',{cx:anchor.x,cy:anchor.y,r:size*.06,fill:'#314c39','pointer-events':'none'}));const g=node('g',{transform:'translate('+loc.x+' '+loc.y+')',role:'button',tabindex:0,'aria-label':item.label+' | Board '+net.boardId,'data-supply-id':item.id,'data-supply-x':item.position.x,'data-supply-y':item.position.y});
   // Offset only the label, never the geographical anchor.
   g.append(node('rect',{x:-size/2,y:-size/3,width:size,height:size*.66,rx:size*.08,fill:side==='Allied'?'#146252':'#273c64',stroke:'#fffdf5','stroke-width':size*.045}),node('text',{'text-anchor':'middle',y:size*.09,fill:'white','font-size':size*.25,'font-weight':700},symbols[item.kind]));
   g.append(node('title',{},item.label+' | '+net.roadId));g.onclick=e=>{e.stopPropagation();onSelect?.(item,report);const panel=card(host,report,item);
   if(onRelocate&&['supply','hq'].includes(item.kind)){const move=document.createElement('button');move.textContent=item.kind==='supply'?'Extend supply line':'Move HQ';move.onclick=()=>CampaignSupplyRelocation.choose(svg,host,point=>{const world=boardId?{x:point.x+p.x,y:point.y+p.y}:point;onRelocate(side,item.kind,world);});panel.insertBefore(move,panel.querySelector('h3'));const help=document.createElement('p');help.textContent=item.kind==='supply'?'Select a destination farther along a connected road, or drag SUP there during setup. The receiving point and supply route move together.':'Select a destination hex, or drag HQ there during setup.';panel.insertBefore(help,panel.querySelector('h3'));}};g.onkeydown=e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();g.onclick(e);}};if(onRelocate&&['supply','hq'].includes(item.kind))CampaignSupplyRelocation.drag(g,svg,point=>onRelocate(side,item.kind,boardId?{x:point.x+p.x,y:point.y+p.y}:point),message=>{const panel=card(host,report,item),error=document.createElement('p');error.setAttribute('role','alert');error.textContent=message;panel.prepend(error);});layer.append(g);
  }svg.append(layer);return layer;
 }
 function terrain(svg,d){for(const p of SquadSpace.layout(d)){const g=node('g',{transform:'translate('+p.x+' '+p.y+')'}),inner=node('g',{transform:p.clockwiseDegrees===90?'translate(3138 0) rotate(90)':p.clockwiseDegrees===180?'translate(1130 3138) rotate(180)':p.clockwiseDegrees===270?'translate(0 1130) rotate(-90)':''});PanzerMapArt.draw(inner,d,d.boards.find(b=>b.id===p.boardId),null,false,true);CampaignAppearance.terrain(inner);g.append(inner);svg.append(g);}}
 function command(svg,d,operation,select,options={}){
  svg.parentElement?.querySelector(".supply-map-card")?.remove();svg.replaceChildren();const b=CampaignSupplySpatial.resolve(d,operation?.supplyProfile?.routes[d.situation.number]?.binding),scale=Math.min(900/b.width,382/b.height),x=(1000-b.width*scale-36)/2,y=(420-b.height*scale)/2;
  const group=node('g',{transform:'translate('+x+' '+y+') scale('+scale+')','data-integrated-source':b.sourceNumber});terrain(group,d);
  overlay(group,d,{operation,mission:options.mission||'M04',size:24/scale,host:svg.parentElement,onSelect:item=>{if(item.kind==='hq')select(options.mission||'M04');if(item.kind==='command')select('HQ');}});svg.append(group);
  svg.classList.add('integrated-command-map');
  svg.append(node('text',{transform:'translate('+(x+b.width*scale+22)+' 210) rotate(90)','text-anchor':'middle','dominant-baseline':'middle','font-size':14,'font-weight':700,'letter-spacing':1.6,fill:'#4a6326','pointer-events':'none'},d.situation.title.toUpperCase()));
  svg.append(node('desc',{},'Select DEP, SUP or HQ to inspect supplies and readiness. Command and supply use the joined formation boards. Positions are authored.'));
 }
 return{overlay,command,terrain,card};
})();
