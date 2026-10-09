"use strict";
// Pure command model. Persist commands and replay them; UI notes are not simulation input.
const ArdennesCommand=(()=>{
 const VERSION=1,KEY='campaign-atlas:ardennes-command:v1',clone=x=>JSON.parse(JSON.stringify(x));
 const names={M14:'Crossing defense (Bulge: Thrust)',M15:'Ridge defense (Elsenborn)'};
 const check=(ok,msg)=>{if(!ok)throw Error(msg);};
 const clock=t=>String(6+Math.floor(t/60)).padStart(2,'0')+':'+String(t%60).padStart(2,'0');
 const initial=(id,profile='operational',staff=10,test=false)=>({version:VERSION,id,profile,staff,test,revision:0,time:0,commands:[],events:[],queue:[],serial:0,plan:null,vacant:false,stock:{A14:'depot',A15:'depot',F1:'depot'},reserve:'unallocated',trips:[],missions:Object.fromEntries(Object.keys(names).map(id=>[id,{id,state:'Draft',received:false,readyAt:null,choice:null,committedAt:null,assessment:null}])),sourceHashes:{}});
 const event=(s,text)=>s.events.push({at:s.time,text});
 const schedule=(s,at,kind,data={})=>s.queue.push({at,kind,data,seq:++s.serial});
 function preview(s,p){
  const plan=s.profile==='essentials'?{main:p.main,reserve:p.main,delivery:p.main}:clone(p);
  check(['M14','M15'].includes(plan.main),'Select a main effort.');check(['M14','M15','retain'].includes(plan.reserve),'Select a reserve destination.');check(['M14','M15','guns'].includes(plan.delivery),'Select a delivery plan.');
  check(plan.delivery==='guns'||plan.delivery===plan.reserve,'Support-first must match the reserve destination.');
  const first= s.time+s.staff,second=first+60;
  const trips=plan.delivery==='guns'?[{cargo:['A14','A15'],route:'mixed',depart:first},...(plan.reserve==='retain'?[]:[{cargo:['F1'],route:plan.reserve,depart:second}])]:[{cargo:[plan.delivery==='M14'?'A14':'A15','F1'],route:plan.delivery,depart:first},{cargo:[plan.delivery==='M14'?'A15':'A14'],route:plan.delivery==='M14'?'M15':'M14',depart:second}];
  return {plan,trips,rows:Object.keys(names).map(id=>{const ammo=trips.find(t=>t.cargo.includes(id==='M14'?'A14':'A15')).depart+30,baseline=s.time+5+(id===plan.main?0:s.staff)+s.staff,fuel=trips.find(t=>t.cargo.includes('F1'));return{id,baseline,supplied:Math.max(baseline,ammo),reinforced:plan.reserve===id?Math.max(baseline,ammo,fuel.depart+40):null};})};
 }
 function ready(s,id,choice){
  const m=s.missions[id];if(!m)return 'Unknown mission.';
  if(s.vacant)return 'Deputy succession is pending.';
  if(m.committedAt!==null)return 'This mission already has a committed variant.';
  if(!m.received)return 'Order has not arrived.';
  if(s.time<m.readyAt)return 'Preparation completes at '+clock(m.readyAt)+'.';
  if(s.time>130)return 'The 08:10 commitment deadline has passed.';
  if(!['baseline','supplied','reinforced'].includes(choice))return 'Select a defense plan.';
  if(choice!=='baseline'&&s.stock[id==='M14'?'A14':'A15']!==id)return 'Mission ammunition has not been received.';
  if(choice==='reinforced'&&(s.reserve!==id||s.stock.F1!==id||!m.reserveReady))return 'Assigned reserve, fuel and transfer must be ready.';
  return '';
 }
 const activeRoutes=t=>t.route==='mixed'?['M14','M15']:[t.route];
 function dispatch(s,t){
  if(activeRoutes(t).some(id=>s.closed?.includes(id))){t.status='held-depot';event(s,t.id+' held at depot: route interrupted.');return;}
  if(s.trips.some(x=>x!==t&&['transit','held','returning'].includes(x.status))){t.status='waiting-transport';return;}
  t.status='transit';for(const c of t.cargo){check(s.stock[c]==='depot','Cargo no longer at depot.');s.stock[c]=t.id;}
  t.arrival=s.time+30;schedule(s,t.arrival,'arrival',{id:t.id});event(s,t.id+' dispatched with '+t.cargo.join(', ')+'.');
 }
 function process(s,e){
  const m=s.missions[e.data.id],t=s.trips.find(t=>t.id===e.data.id);
  if(e.kind==='order'){m.received=true;m.readyAt=s.time+s.staff;m.state='Received';schedule(s,m.readyAt,'prepared',{id:m.id});event(s,m.id+' order received.');}
  if(e.kind==='prepared'){m.state='Planned';event(s,m.id+' preparation complete.');}
  if(e.kind==='dispatch')dispatch(s,t);
  if(e.kind==='arrival'&&t.status==='transit'){for(const c of t.cargo)s.stock[c]=c==='F1'?s.plan.reserve:c==='A14'?'M14':'M15';t.status='returning';schedule(s,s.time+30,'return',{id:t.id});event(s,t.id+' delivered '+t.cargo.join(', ')+'.');if(t.cargo.includes('F1'))schedule(s,s.time+10,'reserve',{id:s.plan.reserve});}
  if(e.kind==='return'){if(t.recalled)for(const c of t.cargo)s.stock[c]='depot';t.status='complete';event(s,t.id+' returned to depot.');const next=s.trips.find(x=>x.status==='waiting-transport');if(next)dispatch(s,next);}
  if(e.kind==='reserve'){m.reserveReady=true;event(s,'R1 transfer to '+m.id+' complete.');}
  if(e.kind==='succession'){s.vacant=false;event(s,'Deputy assumed command.');}
  if(e.kind==='report'){m.state='ReportDelivered';m.report=clone(m.result);event(s,m.id+' outcome report delivered.');}
 }
 function apply(s,c){
  check(c&&typeof c.id==='string'&&c.id.length>0,'A command ID is required.');
  const previous=s.commands.find(x=>x.id===c.id);if(previous){check(JSON.stringify(previous)===JSON.stringify(c),'Command ID reused with different content.');return s;}
  check(c.revision===s.revision,'State changed. Reload the current exercise before committing.');
  if(c.type==='allocate'){check(!s.plan,'Allocation already committed.');check(!s.vacant,'No active commander.');const p=preview(s,c.plan);s.plan=p.plan;s.reserve=p.plan.reserve;s.sourceHashes=clone(c.sourceHashes||{});s.trips=p.trips.map((t,i)=>({...t,id:'T'+(i+1),status:'scheduled'}));for(const t of s.trips)schedule(s,t.depart,'dispatch',{id:t.id});for(const id of Object.keys(names)){s.missions[id].state='InTransit';schedule(s,s.time+5+(id===s.plan.main?0:s.staff),'order',{id});}event(s,'Parent allocation committed. R1: '+s.reserve+'.');}
  else if(c.type==='advance'){check(s.queue.length,'No scheduled event remains.');const rank={arrival:0,return:0,reserve:0,succession:0,report:1,order:1,prepared:2,dispatch:3};s.queue.sort((a,b)=>a.at-b.at||(rank[a.kind]??0)-(rank[b.kind]??0)||a.seq-b.seq);const time=s.queue[0].at;s.time=time;while(s.queue.some(x=>x.at===time)){s.queue.sort((a,b)=>a.at-b.at||(rank[a.kind]??0)-(rank[b.kind]??0)||a.seq-b.seq);process(s,s.queue.shift());}}
  else if(c.type==='commit'){check(s.plan,'Allocate resources first.');check(!ready(s,c.mission,c.choice),ready(s,c.mission,c.choice));const m=s.missions[c.mission];m.choice=c.choice;m.committedAt=s.time;m.state='Committed';if(c.choice!=='baseline')s.stock[m.id==='M14'?'A14':'A15']='consumed:'+m.id;if(c.choice==='reinforced'){s.stock.F1='consumed:'+m.id;s.reserve='committed:'+m.id;}event(s,m.id+' committed '+c.choice+' defense. Combat resolver unavailable.');}
  else if(c.type==='fixture'){check(s.test,'Outcome fixtures require a test session.');const m=s.missions[c.mission];check(m?.state==='Committed','Commit a variant before applying a fixture.');check(['held','lost'].includes(c.outcome),'Unknown fixture.');check(s.time>=m.committedAt+60,'Fixture end must be at least 60 minutes after commitment.');m.result={id:c.id,fixture:true,occurredAt:s.time,held:c.outcome==='held',reserveSurvivors:m.choice==='reinforced'?(c.outcome==='held'?2:1):0};m.state='ReportPending';if(m.choice==='reinforced')s.reserve='survivors:'+m.result.reserveSurvivors;event(s,m.id+' TEST outcome reconciled; no combat was run.');schedule(s,s.time+5,'report',{id:m.id});}
  else if(c.type==='fixture-clock'){check(s.test,'Test session required.');const m=s.missions[c.mission];check(m?.state==='Committed','Commit a variant first.');schedule(s,Math.max(s.time+1,m.committedAt+60),'fixture-boundary');event(s,'TEST outcome boundary scheduled.');}
  else if(c.type==='assess'){const m=s.missions[c.mission];check(m?.state==='ReportDelivered','Wait for delivered report.');check(['continue','request','failure'].includes(c.action),'Unknown assessment.');check(c.action!=='continue'||m.report.held,'Reported objective was lost.');check(c.action!=='failure'||!m.report.held,'Report does not establish failure.');m.assessment=c.action;m.state='Assessed';event(s,m.id+' assessed: '+c.action+'. No resources granted.');}
  else if(c.type==='interrupt'){check(s.test,'Disruption fixture requires test session.');check(['M14','M15'].includes(c.route),'Unknown route.');s.closed=[...new Set([...(s.closed||[]),c.route])];for(const t of s.trips.filter(t=>t.status==='transit'&&activeRoutes(t).includes(c.route))){t.remaining=Math.max(1,t.arrival-s.time);t.status='held';s.queue=s.queue.filter(e=>!(e.kind==='arrival'&&e.data.id===t.id));}event(s,'TEST route '+c.route+' interrupted.');}
  else if(c.type==='reopen'){check(s.test,'Test session required.');s.closed=(s.closed||[]).filter(x=>x!==c.route);for(const t of s.trips){if(activeRoutes(t).some(r=>s.closed.includes(r)))continue;if(t.status==='held'){t.status='transit';t.arrival=s.time+t.remaining;schedule(s,t.arrival,'arrival',{id:t.id});}else if(t.status==='held-depot')dispatch(s,t);}event(s,'TEST route '+c.route+' reopened.');}
  else if(c.type==='recall'){check(s.test,'Test session required.');const t=s.trips.find(t=>t.id===c.trip);check(t?.status==='held','Only held cargo can be recalled.');t.status='returning';t.recalled=true;schedule(s,s.time+30,'return',{id:t.id});event(s,t.id+' recalled; cargo remains in transit until depot receipt.');}
  else if(c.type==='vacancy'){check(s.test&&!s.vacant,'Available only in a test session with an active commander.');s.vacant=true;schedule(s,s.time+10,'succession');event(s,'TEST commander unavailable; deputy pending.');}
  else throw Error('Unknown command.');
  s.commands.push(clone(c));s.revision++;return s;
 }
 function execute(s,c){return apply(clone(s),c);}
 function restore(raw){const saved=typeof raw==='string'?JSON.parse(raw):raw;check(saved.version===VERSION,'Unsupported exercise version.');check(typeof saved.id==='string'&&/^[a-zA-Z0-9-]+$/.test(saved.id),'Invalid exercise identity.');check(['operational','essentials'].includes(saved.profile)&&[10,20].includes(saved.staff)&&typeof saved.test==='boolean','Invalid exercise configuration.');let s=initial(saved.id,saved.profile,saved.staff,saved.test);for(const c of saved.commands)s=execute(s,c);return s;}
 function variant(s,id,source){const m=s.missions[id];check(m?.committedAt!==null&&m?.choice,'Commit the mission first.');check(source.situation.number===(id==='M14'?14:15),'Wrong source Situation.');const d=clone(source),drop=id==='M14'?'us-57mm-03':'us-105mm-02',changes=[];d.id=s.id+'-'+id.toLowerCase();d.situation.title=source.situation.title+' [Campaign '+m.choice+']';delete d.parentMapLocation;
  if(m.choice==='baseline'){d.instances=d.instances.filter(i=>i.id!==drop);delete d.deployment.allowedHexes[drop];delete d.deployment.instructions[drop];changes.push('Withheld '+drop+' pending ammunition.');}else changes.push('Restored '+drop+' with issued mission ammunition.');
  if(m.choice==='reinforced'){const template=d.instances.find(i=>i.counterTypeId==='us-m10');for(let j=1;j<=2;j++){const iid='reserve-r1-m10-0'+j;d.instances.push({...clone(template),id:iid,formationId:null,setupGroup:'Authored reserve R1'});d.deployment.allowedHexes[iid]=d.boards.find(b=>b.id==='C').hexes.filter(h=>h.setupAllowed).map(h=>({boardId:'C',hexId:h.id}));d.deployment.instructions[iid]='Authored R1 addition: deploy on C.';}changes.push('Added two persistent R1 M10 counters; fuel issued and transfer completed.');d.rules.setup.find(r=>r.side==='Allied').instructions+=' Authored R1 reserve may deploy on C.';}
  for(const c of d.counters)c.quantity=d.instances.filter(i=>i.counterTypeId===c.id).length;d.counters=d.counters.filter(c=>c.quantity);d.totals={Allied:d.instances.filter(i=>d.counters.find(c=>c.id===i.counterTypeId).side==='Allied').length,German:d.instances.filter(i=>d.counters.find(c=>c.id===i.counterTypeId).side==='German').length,types:d.counters.length};
  d.parentCampaign={id:s.id,title:'Authored Ardennes command exercise',startDate:'1944-12-18',endDate:'1944-12-18',commandSnapshotId:s.id,timeBasis:'Authored coordination fixture, not a historical shared headquarters.'};d.campaignAssignment={campaignId:s.id,situationId:d.id,number:id==='M14'?1:2};d.campaign.parentCampaignId=s.id;
  d.briefing='CAMPAIGN VARIANT. '+changes.join(' ')+' Committed '+clock(m.committedAt)+'. Source: '+source.situation.title+'. '+source.briefing;d.admission.blockers.unshift('Authored command variant: combat, supply-to-ASL mapping and reserve decomposition are not admitted.');d.sourceRules.execution='Deployment and inspection only; no combat execution.';return d;
 }
 return{VERSION,KEY,names,initial,preview,ready,execute,restore,variant,clock};
})();
if(typeof module!=='undefined')module.exports=ArdennesCommand;
