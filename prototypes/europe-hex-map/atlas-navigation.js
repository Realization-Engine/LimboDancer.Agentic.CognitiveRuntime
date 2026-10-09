"use strict";
(()=>{
 const $=id=>document.getElementById(id),el=(tag,text)=>{const n=document.createElement(tag);if(text)n.textContent=text;return n;};
 document.body.classList.add('unified-navigation');
 // Retain old bindings and saves for compatibility, but remove retired controls from interaction.
 for(const id of ['workflowStart','workflowExplore','workflowBrief','regionalWorkspace','regionalEntry','commandNavigation','formationControls','campaignActions','newCampaign']){const n=$(id);if(n){n.classList.add('retired-control');n.inert=true;}}
 const oldActions=$('campaignActions')?.closest('details');if(oldActions){oldActions.classList.add('retired-control');oldActions.inert=true;}for(const id of ['mode-planning','planPanel']){const n=$(id);if(n){n.classList.add('retired-control');n.inert=true;}}const reference=$('workspaceControls');if(reference){$('mapControls').append(reference);reference.querySelector('summary').textContent='Theater reference geography';}
 const choose=$('workflowChoose');for(const n of [...choose.children])if(n.id!=='legacyCampaignLibrary'){n.classList.add('retired-control');n.inert=true;}
 const utilities=document.querySelector('.map-utilities details>div');
 const library=el('details');library.append(el('summary','Standalone Situation reference library'),el('p','Original imported cards and saved deployments. These are independent references, not command decisions.'));
 if(document.body.classList.contains('ardennes-active')){const a=el('a','Standalone Situation reference library');a.href='index.html?referenceLibrary=1';utilities.append(a);}else{library.append($('legacyCampaignLibrary'));utilities.append(library);if(new URLSearchParams(location.search).has('referenceLibrary')){document.querySelector('.map-utilities details').open=true;library.open=true;}}
 library.addEventListener('click',e=>{if(e.target.tagName==='BUTTON'){document.querySelector('.map-utilities details').open=false;}});
 for(const [label,url]of [['Transport research','transport-research.html'],['North African transport research','southern-transport-research.html'],['North African terrain sources','africa-terrain-research.html']]){const a=el('a',label);a.href=url;utilities.append(a);}
 const image=el('details');image.append(el('summary','Atlas image export'));for(const id of ['exportImage','imageExportStatus'])if($(id))image.append($(id));const caption=$('imageCaption');if(caption)image.append(caption.closest('label'));utilities.append(image);if(document.body.classList.contains('ardennes-active'))image.hidden=true;
 const oldExports=$('exportCampaign')?.closest('details');if(oldExports){oldExports.classList.add('retired-control');oldExports.inert=true;}
 const menu=el('nav');menu.className='campaign-menu';menu.setAttribute('aria-label','Campaign');const details=el('details');details.append(el('summary','Campaign'));const content=el('div');details.append(content);menu.append(details);document.querySelector('header').insertBefore(menu,document.querySelector('.map-utilities'));
 const open=el('a','Open / resume Ardennes operation');open.href='index.html?exercise=ardennes';content.append(open);
 const overview=el('a','Campaign Atlas overview');overview.href='index.html';content.append(overview);
 const note=el('p','Command progress saves automatically in this browser.');content.append(note);
 const actions=el('div');actions.id='campaignSessionActions';content.append(actions);
 function refresh(){actions.replaceChildren();const api=window.ArdennesSessionActions;if(!api)return;const button=(text,fn)=>{const b=el('button',text);b.onclick=()=>{try{fn();details.open=false;}catch(e){note.textContent=e.message;}};actions.append(b);};button('New operation (archive current)',api.startNew);button('Export saved operation',api.exportSave);const archive=el('details');archive.append(el('summary','Archived operations'));for(const entry of api.archives()){const b=el('button',entry.label);b.onclick=()=>{try{api.restore(entry.key);details.open=false;}catch(e){note.textContent=e.message;}};archive.append(b);}if(!api.archives().length)archive.append(el('p','No archived operations.'));actions.append(archive);}
 details.addEventListener('toggle',()=>{if(details.open)refresh();});
 const launch=el('section');launch.id='campaignLauncher';launch.append(el('h2','Campaign command'));const a=el('a','Open / resume Ardennes operation');a.href='index.html?exercise=ardennes';launch.append(a,el('p','Select a headquarters, allocate support and follow the operation on the map.'));
 $('gameControls').prepend(launch);
 const hint=$('workflowHint');if(hint)hint.textContent='Choose Campaign to open an operation. Map Utilities contains standalone references and appearance tools.';
})();
