"use strict";
// Shared map actions: preserve the original buttons and their event handlers.
window.CampaignMapToolbar=(()=>{
 const ns='http://www.w3.org/2000/svg';
 const map='<path d="M3 8l8-3 10 3 8-3v20l-8 3-10-3-8 3z"/><path class="icon-detail" d="M11 5v20M21 8v20"/>';
 const tile='<rect x="4" y="5" width="20" height="20" rx="2"/><path class="icon-detail" d="M8 10h12v10H8zM8 10l12 10M20 10L8 20"/>';
 const squad='<rect x="3" y="5" width="12" height="12" rx="2"/><rect x="17" y="5" width="12" height="12" rx="2"/><rect x="10" y="19" width="12" height="11" rx="2"/>';
 const pen='<path class="icon-accent" d="M19 26l2-7 7-7 4 4-7 7z"/>';
 const play='<path class="icon-accent" d="M21 17l10 7-10 7z"/>';
 const icons={continent:map+'<circle class="icon-accent" cx="18" cy="15" r="4"/>',logistics:'<path d="M3 6h18v17H3zM21 13h6l5 7v3H21z"/><circle cx="9" cy="26" r="4"/><circle cx="26" cy="26" r="4"/><path class="icon-accent" d="M7 10h10v8H7z"/>',card:'<rect x="6" y="3" width="21" height="27" rx="2"/><path class="icon-accent" d="M10 7h13v5H10z"/><path class="icon-detail" d="M10 17h13M10 22h8M10 26h13"/>',formation:tile+pen,formationGame:tile+play,squad:squad+pen,squadGame:squad+play,return:map+'<path class="icon-accent" d="M17 12H9V7L1 15l8 8v-5h8z"/>',export:'<path d="M5 3h15l7 7v19H5z"/><path class="icon-detail" d="M20 3v8h7"/><path class="icon-accent" d="M13 26v-9H8l8-8 8 8h-5v9z"/>',clear:'<path d="M8 9h17l-2 21H10zM6 5h21v4H6zM12 2h9v3h-9z"/><path class="icon-detail" d="M14 13v12M20 13v12"/>',air:'<path d="M14 2h5v10l12 7v4l-12-4v7l5 3v2l-7-2-7 2v-2l4-3v-7L2 23v-4l12-7z"/>',deploy:map+'<path class="icon-accent" d="M13 2h6v10h5l-8 8-8-8h5z"/>',fit:'<path class="icon-line" d="M3 12V3h9M20 3h9v9M29 20v9h-9M12 29H3v-9"/><path d="M11 11h10v10H11z"/>',focus:'<circle class="icon-line" cx="16" cy="16" r="10"/><path class="icon-line" d="M16 1v8M16 23v8M1 16h8M23 16h8"/><circle class="icon-accent" cx="16" cy="16" r="3"/>',complete:tile+'<path class="icon-accent" d="M13 22l5 5L30 13l3 3-15 16-8-8z"/>'};
 function key(label){if(/continental map/i.test(label))return'continent';if(/advanced logistics/i.test(label))return'logistics';if(/air support/i.test(label))return'air';if(/deploy full/i.test(label))return'deploy';if(/complete/i.test(label))return'complete';if(/fit (map|operation)/i.test(label))return'fit';if(/focus selected/i.test(label))return'focus';if(/export/i.test(label))return'export';if(/clear/i.test(label))return'clear';if(/return/i.test(label))return'return';if(/situation card/i.test(label))return'card';if(/squad/i.test(label))return /deployment|setup|review/i.test(label)?'squad':'squadGame';if(/formation setup|review deployment/i.test(label))return'formation';if(/game map|situation map/i.test(label))return'formationGame';return null;}
 function decorate(button){const label=button.dataset.iconLabel||button.textContent.trim(),id=key(label);if(!id||button.dataset.iconLabel)return id;
  button.dataset.iconLabel=label;button.title=label;button.setAttribute('aria-label',label);button.classList.add('atlas-icon-button');button.replaceChildren();
  const svg=document.createElementNS(ns,'svg');svg.setAttribute('viewBox','0 0 34 34');svg.setAttribute('aria-hidden','true');svg.setAttribute('focusable','false');svg.innerHTML=icons[id];button.append(svg);return id;
 }
 function restore(){for(const b of document.querySelectorAll('[data-toolbar-hidden]')){b.hidden=false;delete b.dataset.toolbarHidden;}}
 function mount(header){if(!header)return;restore();const nav=document.querySelector('[data-situation-navigation]'),group=document.createElement('div');group.className='atlas-map-actions';group.setAttribute('role','group');group.setAttribute('aria-label','Situation workflow');const used=new Set();
  if(nav){for(const b of [...nav.querySelectorAll('button')]){const id=decorate(b);if(id)used.add(id);group.append(b);}nav.hidden=true;}
  for(const b of header.querySelectorAll('button')){if(b.closest('.squad-details-card'))continue;const id=decorate(b);if(id&&used.has(id)){b.hidden=true;b.dataset.toolbarHidden='';}else if(id)used.add(id);}
  if(group.children.length){const actions=header.querySelector(".panzer-header-actions");if(actions)header.insertBefore(group,actions);else header.append(group);}
  for(const b of document.querySelectorAll('.operation-command button,.operation-command a'))if(/^(Review Situation Card|Open Situation map)$/.test(b.textContent.trim())){b.hidden=true;b.dataset.toolbarHidden='';}
 }
 function overview(){
  const tools=document.getElementById('sourceMapActions');if(!tools)return;
  tools.querySelector('.atlas-map-actions')?.remove();
  const nav=document.querySelector('[data-situation-navigation]'),group=document.createElement('div');group.className='atlas-map-actions';group.setAttribute('role','group');group.setAttribute('aria-label','Situation workflow');
  const used=new Set();
  if(nav){for(const original of nav.querySelectorAll('button')){const b=original.cloneNode(true);b.onclick=()=>original.click();const id=decorate(b);if(id)used.add(id);group.append(b);}nav.hidden=true;}
  for(const b of tools.querySelectorAll(':scope > button')){b.hidden=false;const id=decorate(b);if(id&&used.has(id))b.hidden=true;else if(id)used.add(id);}
  const summary=tools.querySelector('.advanced-logistics > summary');if(summary){decorate(summary);const box=summary.parentElement;if(!box.querySelector(':scope > div')){const content=document.createElement('div');for(const child of [...box.children])if(child!==summary)content.append(child);box.append(content);}}
  if(group.children.length)tools.prepend(group);
  for(const b of document.querySelectorAll('.operation-command button,.operation-command a'))if(/^(Review Situation Card|Open Situation map)$/.test(b.textContent.trim())){b.hidden=true;b.dataset.toolbarHidden='';}
 }
 return{mount,restore,overview};
})();
