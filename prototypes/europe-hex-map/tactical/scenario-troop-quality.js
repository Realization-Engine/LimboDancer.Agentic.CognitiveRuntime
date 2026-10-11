"use strict";
// Dated scenario ratings, separate from affiliation and source formation factors.
window.ScenarioTroopQuality = (() => {
 const version='scenario-quality-1';
 const catalog='src/ASL/units/catalog/scenario-a1.catalog.json';
 const sources={
  market:'https://www.ibiblio.org/hyperwar/USA/USA-E-Siegfried/USA-E-Siegfried-7.html',
  arnhem:'https://www.raf.mod.uk/what-we-do/our-history/air-historical-branch/second-world-war-thematic-studies/arnhem-the-air-reconnaissance-story/',
  peiper:'https://www.ibiblio.org/hyperwar/USA/USA-E-Ardennes/USA-E-Ardennes-5.html',
  elsenborn:'https://www.ibiblio.org/hyperwar/USA/USA-E-Ardennes/USA-E-Ardennes-6.html',
  marieulles:'https://www.ibiblio.org/hyperwar/USA/USA-E-Lorraine/USA-E-Lorraine-3.html'
 };
 const reviews={
  5:{note:'Goodwood: mixed 1st SS / 21st Panzer force. The eight-parent SS allocation is authored, not a documented quality split. Retain current values pending more specific evidence.'},
  8:{note:'Marieulles: probable 37th SS identity conflicts with the printed trainee account. Stubborn resistance alone does not establish an elite rating. Retain current values.',sources:[sources.marieulles]},
  9:{upgrade:true,note:'Nijmegen: represent the SS panzergrenadier core with higher morale. US Army history records the timely SS infantry reinforcement and defense. The card names 9th SS, while history includes 10th SS. This whole-roster rifle allocation is a playable interpretation, not a measured historical rating.',sources:[sources.market]},
  10:{upgrade:true,note:'Arnhem: Harzer\'s experienced SS core warrants a veteran rifle profile. The RAF history identifies an experienced nucleus; Army history describes its effective blocking of relief. The twelve rifle parents are the authored core, not every German attachment.',sources:[sources.arnhem,sources.market]},
  14:{upgrade:true,note:'Bulge: Thrust: Peiper\'s rifle infantry uses the veteran profile. Cole describes relative training advantages and notes that 1st SS avoided the Luftwaffe and over-age replacements assigned to other divisions. No extra firepower, range or vehicle performance is inferred.',sources:[sources.peiper]},
  15:{note:'Elsenborn: retain current infantry values. Cole records inadequate training in the attack and difficult infantry/armor cooperation. SS affiliation is insufficient for a blanket upgrade; existing engineer and SMG specialist profiles remain unchanged.',sources:[sources.elsenborn]}
 };
 function assessment(d,typeId,u){
  if(ScenarioAffiliation.resolve(d,u).affiliation!=='SS')return null;
  const review=reviews[Number(d.situation.number)];
  if(!review)return null;
  return {version,applied:!!review.upgrade&&typeId==='de-rifle',basis:'Authored scenario rating',note:review.note,sources:review.sources||[]};
 }
 function definition(d,typeId,u){
  const base=SquadTactical.definition(typeId,u),quality=assessment(d,typeId,u);
  if(!quality)return base;
  if(!quality.applied)return {...base,quality};
  // Front-face values of German attacker-elite-squad in catalog 1.13.0.
  return {definitionId:'attacker-elite-squad',status:'catalog match',source:catalog,
   rationale:'Scenario-assigned 4-6-8 elite profile. Historical evidence supports the interpretation, not these numerical values. Smoke exponent 2 follows the complete catalog profile.',
   values:{firepower:4,range:6,morale:8,class:'elite','smoke-exponent':2},quality,baseDefinitionId:base.definitionId,baseValues:{...base.values}};
 }
 return {version,assessment,definition};
})();
