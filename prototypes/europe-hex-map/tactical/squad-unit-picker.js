"use strict";
window.SquadUnitPicker = (() => {
 const el=(tag,text)=>{const n=document.createElement(tag);if(text)n.textContent=text;return n;};
 function create(toolbar,d,onSelect){
  const root=el('div');root.className='squad-unit-picker';
  const trigger=el('button','Units by type');trigger.type='button';trigger.setAttribute('aria-expanded','false');trigger.setAttribute('aria-controls','squad-unit-options');
  const panel=el('div');panel.id='squad-unit-options';panel.className='squad-unit-options';panel.hidden=true;panel.setAttribute('role','region');panel.setAttribute('aria-label','Units by type');
  const searchBar=el('div');searchBar.className='squad-unit-search';
  const search=el('input');search.type='search';search.placeholder='Filter units...';search.setAttribute('aria-label','Filter units');
  const results=el('div');results.className='squad-unit-results';
  const empty=el('p','No matching units.');empty.hidden=true;empty.setAttribute('role','status');
  searchBar.append(search);panel.append(searchBar,results,empty);
  let savedExpanded=null;
  function filter(){
   const terms=search.value.toLowerCase().trim().split(/\s+/).filter(Boolean);
   if(terms.length&&!savedExpanded)savedExpanded=new Set([...results.querySelectorAll('details[open]')].map(n=>n.dataset.type));
   let total=0;
   for(const group of results.querySelectorAll('details')){
    let count=0;
    for(const b of group.querySelectorAll('button')){b.hidden=!terms.every(term=>b.dataset.search.includes(term));if(!b.hidden)count++;}
    for(const heading of group.querySelectorAll('h4')){let b=heading.nextElementSibling,visible=false;while(b&&b.tagName==='BUTTON'){visible ||= !b.hidden;b=b.nextElementSibling;}heading.hidden=!visible;}
    group.hidden=!count;group.querySelector('summary').textContent=group.dataset.type+' ('+count+')';
    if(terms.length)group.open=!!count;else if(savedExpanded)group.open=savedExpanded.has(group.dataset.type);
    total+=count;
   }
   empty.hidden=!!total;if(!terms.length)savedExpanded=null;
  }
  search.oninput=filter;
  root.append(trigger,panel);toolbar.prepend(root);
  const close=()=>{panel.hidden=true;trigger.setAttribute('aria-expanded','false');};
  trigger.onclick=()=>{const open=panel.hidden;panel.hidden=!open;trigger.setAttribute('aria-expanded',String(open));if(open){panel.scrollTop=0;search.focus();}};
  const outside=e=>{if(!root.contains(e.target))close();};
  const keyboard=e=>{if(e.key==='Escape'&&!panel.hidden){e.preventDefault();close();trigger.focus();}};
  document.addEventListener('pointerdown',outside);root.addEventListener('keydown',keyboard);
  function update(state,selected){
   const expanded=new Set([...panel.querySelectorAll('details[open]')].map(n=>n.dataset.type));results.replaceChildren();
   const groups=new Map();
   for(const u of state.units){const p=state.parents.find(p=>p.id===u.parentId),type=d.counters.find(t=>t.id===p.typeId),name=(type.label||u.kind).replace(/^(British|German|American|French|Canadian) /,'');if(!groups.has(name))groups.set(name,[]);groups.get(name).push({u,p,type});}
   for(const [name,entries] of [...groups].sort(([a],[b])=>a.localeCompare(b))){
    const group=el('details');group.dataset.type=name;group.open=expanded.has(name);group.append(el('summary',name+' ('+entries.length+')'));
    for(const nation of [...new Set(entries.map(x=>x.u.nationality))].sort()){
     group.append(el('h4',({de:'German',uk:'British',us:'American',fr:'French',pl:'Polish',ca:'Canadian'})[nation]||nation));
     for(const {u,p,type} of entries.filter(x=>x.u.nationality===nation)){
      const b=el('button');b.type='button';b.dataset.search=[name,type.label,u.label,u.id,p.id,u.side,u.nationality,({de:'German',uk:'British',us:'American',fr:'French',pl:'Polish',ca:'Canadian'})[u.nationality]||''].join(' ').toLowerCase();b.setAttribute('aria-pressed',String(u.id===selected));
      const art=document.createElementNS('http://www.w3.org/2000/svg','svg');art.setAttribute('viewBox','-10 -10 20 20');art.setAttribute('aria-hidden','true');SquadCounterArt.draw(art,d,type,u,false);
      const label=el('span',u.label);label.append(el('small',u.id),el('small','Formation: '+p.id+' | '+(u.hex?'Deployed':'Not deployed')));b.append(art,label);
      b.onclick=()=>{close();onSelect(u.id);trigger.focus();};group.append(b);
     }
    }results.append(group);
   }
   filter();
   const current=state.units.find(u=>u.id===selected);trigger.textContent=current?current.label+' \u25be':'Units by type \u25be';trigger.title=current?current.id:'Units by type';trigger.setAttribute('aria-label','Units by type'+(current?': '+current.label:''));
  }
  return {update,destroy(){document.removeEventListener('pointerdown',outside);root.remove();}};
 }
 return {create};
})();
