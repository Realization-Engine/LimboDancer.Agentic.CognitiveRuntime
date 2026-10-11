"use strict";
// Organizational identity is independent of the printed equipment/composition profile.
window.ScenarioAffiliation = (() => {
 const version='scenario-affiliation-1';
 const entry=(affiliation,formation,basis,note)=>({affiliation,formation,basis,note});
 const army={4:'3rd Fallschirmjaeger Division / assault-gun support',6:'84th Infantry Division (card attribution)',7:'553rd Volksgrenadier Division',12:'Panzer Lehr',13:'18th Volksgrenadier / Fuehrer Begleit',16:'Panzer Lehr',17:'2nd Panzer Division',18:'Panzer Lehr / 326th Volksgrenadier (card attribution)',19:'26th Volksgrenadier / Panzer Lehr'};
 function resolve(d,unit){
  const id=typeof unit==='string'?unit:unit.parentId||unit.id;
  const i=d.instances.find(x=>x.id===id),t=d.counters.find(x=>x.id===i?.counterTypeId);
  if(!i||t?.side!=='German')return entry(null,'','not applicable','');
  const n=Number(d.situation.number),infantry=['de-rifle','de-smg','de-engineer'].includes(t.id);
  if(n===15)return i.setupGroup==='A'
   ?entry('SS','12th SS Panzer Division','Printed card group A','Group B remains Army. The card prints 276 VG; historical accounts differ on that division number.')
   :entry('Heer','276th Volksgrenadier Division','Printed card group B','Division number retained as printed.');
  if(n===14)return entry('SS','Kampfgruppe Peiper / 1st SS Panzer Division','Card formation, historically corroborated','Affiliation assigned to the represented battle group, including its pioneers and support. Identity alone grants no strength bonus; scenario quality is assessed separately.');
  if(n===9)return entry('SS','9th SS Panzer Division (card attribution)','Printed Situation attribution','Historical accounts place 10th SS elements at Nijmegen. The card division number is retained, not independently confirmed.');
  if(n===10){
   if(infantry)return entry('SS','Kampfgruppe Harzer / 9th SS infantry core','Historically supported core; authored counter assignment','The 12 Rifle counters represent the SS core. Wider battle attachments were mixed.');
   return entry(null,'Kampfgruppe Harzer attachments','Unresolved attachment affiliation','SS command alone does not establish the service of attached armor, guns or transport.');
  }
  if(n===8&&infantry)return entry('SS','37th SS Panzergrenadier Regiment (probable)','Historical interpretation','Cole identifies probable elements of I and II/37th SS at Marieulles. This conflicts with the card trainee description; exact identity and date remain qualified.');
  if(n===5){
   if(t.id==='de-rifle')return Number(id.split('-').at(-1))<=8
    ?entry('SS','1st SS Panzer Division','Authored allocation','Eight of the 15 Rifle counters assigned to SS and seven to 21st Panzer for playable mixed forces. The card does not specify this split.')
    :entry('Heer','21st Panzer Division','Authored allocation','Seven of the 15 Rifle counters assigned to Army; eight to SS. Allocation is a game assumption.');
   if(['de-pziv','de-panther'].includes(t.id))return entry('SS','1st SS Panzer Division','Card tank variant within a mixed force','Assignment follows the SS tank variant in this mixed 1st SS / 21st Panzer roster.');
   if(['de-pziv-w','de-panther-w'].includes(t.id))return entry('Heer','21st Panzer Division','Card tank variant within a mixed force','Army variant retained in this mixed roster.');
   return entry(null,'Goodwood mixed support','Unresolved attachment affiliation','The pooled support roster does not identify each parent formation.');
  }
  if(army[n])return entry(n===4?'Luftwaffe / Heer':'Heer',army[n],'Printed Situation formation','Printed tank type and equipment quantity are retained even where the table calls that type SS. That label does not override scenario affiliation.');
  return entry(null,n===11?'Mixed Anticlimax force':n===20?'Mixed Remagen garrison':'German supporting forces','Unresolved affiliation','No SS assignment established for this counter. Neutral German palette retained.');
 }
 function group(d,typeId){
  const entries=d.instances.filter(i=>i.counterTypeId===typeId).map(i=>resolve(d,i));
  return entries.length&&entries.every(x=>x.affiliation==='SS')?'SS':undefined;
 }
 function roster(d){return d.instances.map(i=>({id:i.id,...resolve(d,i)}));}
 return {version,resolve,group,roster};
})();
