"use strict";
// One set of navigation actions for every embedded campaign Situation.
window.SituationNavigation={
 render({host,title,view,stage,launched,complete,navigate,advance}){
  let nav=host.querySelector('[data-situation-navigation]');
  if(!nav){nav=document.createElement('section');nav.dataset.situationNavigation='';host.append(nav);}
  nav.replaceChildren();const heading=document.createElement('h2');heading.textContent='Situation workflow';nav.append(heading);
  const status=document.createElement('p');status.setAttribute('role','status');status.textContent=title+' | '+({card:'Situation Card',setup:stage==='ready'?'Formation setup complete':stage+' formation setup',game:'Formation game map',squad:'Squad deployment','squad-game':'Squad game map'}[view]);nav.append(status);
  const add=(label,action,disabled=false,current=false)=>{const b=document.createElement('button');b.type='button';b.textContent=label;b.disabled=disabled;if(current)b.setAttribute('aria-current','step');b.onclick=action;nav.append(b);};
  add('Situation Card',()=>navigate('card'),false,view==='card');
  add('Formation setup',()=>navigate('setup'),false,view==='setup');
  if(stage!=='ready')add('Complete '+stage+' setup',advance,!complete);
  add(launched?'Open formation game map':'Launch formation game map',()=>navigate('game'),stage!=='ready',view==='game');
  add('Open squad deployment',()=>navigate('squad'),!launched||stage!=='ready',view==='squad');
  add('Open squad game map',()=>navigate('squad-game'),!launched||stage!=='ready',view==='squad-game');
  if(view==='squad'){const p=document.createElement('p');p.textContent='Squad deployment is saved as you work. Complete squad setup to open the squad game map. Squad combat is not implemented.';nav.append(p);}
 },
 clear(){document.querySelector('[data-situation-navigation]')?.remove();}
};
