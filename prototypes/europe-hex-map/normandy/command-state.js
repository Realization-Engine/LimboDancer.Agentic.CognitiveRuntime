"use strict";
// Dated operation rules. Notes never drive commands; replay is authoritative.
const NormandyCommand=(()=>{
 const clone=x=>JSON.parse(JSON.stringify(x)),check=(ok,text)=>{if(!ok)throw Error(text);};
 const clock=t=>{const h=6+Math.floor(t/60);return(h>=24?'Day +'+Math.floor(h/24)+' ':'')+String(h%24).padStart(2,'0')+':'+String(t%60).padStart(2,'0');};
 const initial=(id,profile='operational',staff=10,supplyProfile=globalThis.window?.CampaignSupplySpatial?.profile([4])||null)=>({supplyProfile,version:1,id,profile,staff,time:0,revision:0,commands:[],events:[],queue:[],plan:null,sourceHashes:{},stock:{ammunition:'depot',fuel:'depot'},reserve:'HQ',missions:{M04:{state:'Draft',received:false,readyAt:null,choice:null,committedAt:null}}});
 function ready(s,choice){const m=s.missions.M04;
  if(m.choice)return 'Mission already committed.';
  if(!m.received)return 'Wait for the regiment to receive its order.';
  if(s.time<m.readyAt)return 'Preparation completes at '+clock(m.readyAt)+'.';
  if(!['baseline','supplied','reinforced'].includes(choice))return 'Select a mission plan.';
  if(choice!=='baseline'&&s.plan?.supply==='retain')return 'Supplies were retained at HQ. Choose the baseline plan or start a new operation.';
  if(choice!=='baseline'&&s.stock.ammunition!=='M04')return 'Ammunition must arrive at the St. Lo supply point.';
  if(choice==='reinforced'&&s.reserve!=='M04')return 'Assign the reserve and wait for fuel delivery and transfer.';
  return '';
 }
 function execute(old,c){const s=clone(old),same=s.commands.find(x=>x.id===c.id);if(same){check(JSON.stringify(same)===JSON.stringify(c),'Command ID reused.');return s;}
  check(typeof c.id==='string'&&c.id.length&&c.revision===s.revision,'Command state changed. Reload before editing.');
  const event=text=>s.events.push({at:s.time,text});const schedule=(at,kind)=>s.queue.push({at,kind});
  if(globalThis.window?.CampaignLogistics?.handles(c)){window.CampaignLogistics.apply(s,c);}else if(c.type==='allocate'){
   check(!s.plan,'Allocation already committed.');const plan=s.profile==='essentials'?{supply:'send',reserve:'attach'}:c.plan;
   check(['send','retain'].includes(plan?.supply)&&['attach','retain'].includes(plan?.reserve),'Choose supply and reserve allocation.');
   check(plan.reserve!=='attach'||plan.supply==='send','The reserve needs the fuel shipment. Send supplies or retain the reserve.');
   check(typeof c.sourceHashes?.M04==='string','Source card identity is required.');
   s.plan=clone(plan);s.sourceHashes=clone(c.sourceHashes);s.missions.M04.state='Order in transit';schedule(5,'order');
   if(plan.supply==='send')schedule(s.staff,'dispatch');event('Operation HQ issued the St. Lo mission and locked its support allocation.');
  }else if(c.type==='advance'){
   check(s.queue.length,'No scheduled event remains.');s.time=Math.min(...s.queue.map(e=>e.at));const due=s.queue.filter(e=>e.at===s.time);s.queue=s.queue.filter(e=>e.at!==s.time);
   for(const e of due){if(globalThis.window?.CampaignLogistics?.process(s,e))continue;const m=s.missions.M04;
    if(e.kind==='order'){m.received=true;m.readyAt=s.time+s.staff;m.state='Preparing';schedule(m.readyAt,'prepared');event('St. Lo Regiment HQ received its order.');}
    if(e.kind==='prepared'){m.state='Prepared';event('Regiment preparation complete.');}
    if(e.kind==='dispatch'){s.stock.ammunition='transit';if(s.plan.reserve==='attach')s.stock.fuel='transit';s.transport={depart:s.time,arrival:s.time+(s.supplyProfile?.routes[4]?.minutes||30),status:'transit'};schedule(s.time+(s.supplyProfile?.routes[4]?.minutes||30),'arrival');event('Transport left Normandy Forward Depot for St. Lo supply point.');}
    if(e.kind==='arrival'){s.stock.ammunition='M04';if(s.plan.reserve==='attach'){s.stock.fuel='M04';schedule(s.time+10,'reserve');}s.transport.status='delivered';event('Supplies delivered to St. Lo supply point.');}
    if(e.kind==='reserve'){s.reserve='M04';event('Two Sherman formation counters transferred to St. Lo Regiment HQ.');}
   }
  }else if(c.type==='commit'){
   check(!ready(s,c.choice),ready(s,c.choice));const m=s.missions.M04;m.choice=c.choice;m.committedAt=s.time;m.state='Committed';
   if(c.choice!=='baseline')s.stock.ammunition='issued';if(c.choice==='reinforced'){s.stock.fuel='issued';s.reserve='committed';}event('St. Lo '+c.choice+' mission committed. Situation deployment unlocked.');
  }else throw Error('Unknown Normandy command.');
  globalThis.window?.CampaignLogistics?.after(s,c);s.commands.push(clone(c));s.revision++;return s;
 }
 function restore(raw){const saved=typeof raw==='string'?JSON.parse(raw):raw;check(saved?.version===1&&/^[a-zA-Z0-9-]+$/.test(saved.id),'Invalid Normandy operation.');check(['essentials','operational'].includes(saved.profile)&&[10,20].includes(saved.staff),'Invalid operation settings.');let s=initial(saved.id,saved.profile,saved.staff,saved.supplyProfile??null);for(const c of saved.commands)s=execute(s,c);return s;}
 function at(saved,t){let s=initial(saved.id,saved.profile,saved.staff,saved.supplyProfile??null);for(const c of saved.commands){const next=execute(s,c);if(next.time>t)break;s=next;}return s;}
 return {initial,execute,restore,ready,clock,at};
})();
if(typeof module!=='undefined')module.exports=NormandyCommand;
