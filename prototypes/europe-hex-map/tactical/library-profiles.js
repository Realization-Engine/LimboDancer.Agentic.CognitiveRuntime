"use strict";
// Explicit game defaults for the imported library. No Panzer combat factors are converted.
(()=>{
 const catalog={"attacker-2nd-line-squad":{"source":"src/ASL/units/catalog/scenario-a1.catalog.json","definitionId":"attacker-2nd-line-squad","status":"catalog match","values":{"firepower":4,"range":4,"morale":7,"class":"2nd-line"}},"attacker-mmg":{"source":"src/ASL/units/catalog/scenario-a1.catalog.json","definitionId":"attacker-mmg","status":"catalog match","values":{"size":"medium","firepower":5,"range":12,"breakdown":12,"rate-of-fire":3,"portage":4}},"attacker-inf-gun":{"source":"src/ASL/units/catalog/scenario-a1.catalog.json","definitionId":"attacker-inf-gun","status":"catalog match","values":{"designation":"7.5cm leIG 18","gun-type":"inf","caliber":75,"caliber-suffix":"star","rate-of-fire":2,"breakdown":12,"range-maximum":115,"special-ammo":["H7"],"manhandling":10,"target-size":"small","bpv":34,"dates":"32-45"}},"attacker-tank":{"source":"src/ASL/units/catalog/scenario-a1.catalog.json","definitionId":"attacker-tank","status":"catalog match","values":{"designation":"PzKpfw IIIH","movement-type":"fully-tracked","movement-points":13,"af-front":6,"af-side":3,"turret-af-front":"inferior","turret-af-side":"superior","ma-type":"t","caliber":50,"rate-of-fire":2,"breakdown":12,"special-ammo":["A4^1 A5^2"],"bmg":3,"cmg":5}},"american-elite-squad":{"source":"src/ASL/units/catalog/scenario-a1.catalog.json","definitionId":"american-elite-squad","status":"catalog match","values":{"firepower":6,"range":6,"morale":7,"class":"elite","class-variant":"square","smoke-exponent":3}},"british-elite-squad":{"source":"src/ASL/units/catalog/scenario-a1.catalog.json","definitionId":"british-elite-squad","status":"catalog match","values":{"firepower":4,"range":5,"morale":8,"class":"elite","class-variant":"square","smoke-exponent":2}},"british-squad":{"source":"src/ASL/units/catalog/scenario-a1.catalog.json","definitionId":"british-squad","status":"catalog match","values":{"firepower":4,"range":5,"morale":7,"class":"1st-line","smoke-exponent":2}},"attacker-elite-squad-5-4-8":{"source":"src/ASL/units/catalog/scenario-a1.catalog.json","definitionId":"attacker-elite-squad-5-4-8","status":"catalog match","values":{"firepower":5,"range":4,"morale":8,"class":"elite","class-variant":"circle","smoke-exponent":2}},"british-lmg":{"source":"src/ASL/units/catalog/scenario-a1.catalog.json","definitionId":"british-lmg","status":"catalog match","values":{"size":"light","firepower":2,"range":7,"breakdown":11,"rate-of-fire":1,"portage":1}}};
 const rows=[
 [
  "uk-rifle",
  4,
  "squad",
  "british-squad",
  {}
 ],
 [
  "uk-engineer",
  4,
  "squad",
  "british-elite-squad",
  {}
 ],
 [
  "uk-25pdr",
  4,
  "gun",
  "de-75mm",
  {
   "caliber": 87.6,
   "caliber-suffix": "",
   "designation": "25 pounder",
   "gun-type": "art"
  }
 ],
 [
  "uk-truck",
  5,
  "vehicle",
  "us-truck",
  {}
 ],
 [
  "uk-bren",
  5,
  "vehicle",
  "de-halftrack",
  {
   "designation": "Bren carrier",
   "movement-points": 16,
   "passenger-capacity": 5
  }
 ],
 [
  "ca-rifle",
  4,
  "squad",
  "british-squad",
  {}
 ],
 [
  "ca-engineer",
  4,
  "squad",
  "british-elite-squad",
  {}
 ],
 [
  "ca-25pdr",
  4,
  "gun",
  "de-75mm",
  {
   "caliber": 87.6,
   "caliber-suffix": "",
   "designation": "25 pounder",
   "gun-type": "art"
  }
 ],
 [
  "ca-truck",
  5,
  "vehicle",
  "us-truck",
  {}
 ],
 [
  "ca-bren",
  5,
  "vehicle",
  "de-halftrack",
  {
   "designation": "Bren carrier",
   "movement-points": 16,
   "passenger-capacity": 5
  }
 ],
 [
  "be-rifle",
  4,
  "squad",
  "british-squad",
  {}
 ],
 [
  "be-engineer",
  4,
  "squad",
  "british-elite-squad",
  {}
 ],
 [
  "be-25pdr",
  4,
  "gun",
  "de-75mm",
  {
   "caliber": 87.6,
   "caliber-suffix": "",
   "designation": "25 pounder",
   "gun-type": "art"
  }
 ],
 [
  "be-truck",
  5,
  "vehicle",
  "us-truck",
  {}
 ],
 [
  "be-bren",
  5,
  "vehicle",
  "de-halftrack",
  {
   "designation": "Bren carrier",
   "movement-points": 16,
   "passenger-capacity": 5
  }
 ],
 [
  "uk-scout",
  3,
  "squad",
  "british-squad",
  {}
 ],
 [
  "uk-6pdr",
  4,
  "gun",
  "us-57mm",
  {}
 ],
 [
  "ca-scout",
  3,
  "squad",
  "british-squad",
  {}
 ],
 [
  "ca-6pdr",
  4,
  "gun",
  "us-57mm",
  {}
 ],
 [
  "ca-76mortar",
  6,
  "mortar",
  "us-81mm",
  {
   "caliber": 76
  }
 ],
 [
  "be-76mortar",
  6,
  "mortar",
  "us-81mm",
  {
   "caliber": 76
  }
 ],
 [
  "ca-mg",
  6,
  "team",
  "attacker-mmg",
  {}
 ],
 [
  "ca-107mortar",
  4,
  "mortar",
  "de-120mm",
  {
   "caliber": 107
  }
 ],
 [
  "uk-81mm",
  6,
  "mortar",
  "us-81mm",
  {}
 ],
 [
  "uk-sexton",
  4,
  "vehicle",
  "us-m4-105",
  {
   "caliber": 87.6,
   "af-front": 2,
   "af-side": 1
  }
 ],
 [
  "uk-achilles",
  4,
  "vehicle",
  "us-m4-76",
  {
   "caliber": 76.2,
   "movement-points": 12,
   "af-front": 4,
   "af-side": 2
  }
 ],
 [
  "uk-daimler",
  3,
  "vehicle",
  "us-m5",
  {
   "caliber": 40,
   "movement-points": 30,
   "af-front": 1,
   "af-side": 1,
   "movement-type": "armored-car"
  }
 ],
 [
  "be-achilles",
  4,
  "vehicle",
  "us-m4-76",
  {
   "caliber": 76.2,
   "movement-points": 12,
   "af-front": 4,
   "af-side": 2
  }
 ],
 [
  "be-daimler",
  3,
  "vehicle",
  "us-m5",
  {
   "caliber": 40,
   "movement-points": 30,
   "af-front": 1,
   "af-side": 1,
   "movement-type": "armored-car"
  }
 ],
 [
  "uk-cromwell-goodwood",
  5,
  "vehicle",
  "us-m4-75",
  {
   "movement-points": 18
  }
 ],
 [
  "uk-sherman",
  5,
  "vehicle",
  "us-m4-75",
  {}
 ],
 [
  "uk-m3",
  5,
  "vehicle",
  "de-halftrack",
  {
   "movement-type": "armored-car",
   "movement-points": 28
  }
 ],
 [
  "uk-mgcarrier",
  5,
  "vehicle",
  "de-halftrack",
  {}
 ],
 [
  "uk-valentine-bridge-carrier",
  3,
  "vehicle",
  "us-m5",
  {
   "caliber": 0,
   "movement-points": 10,
   "af-front": 6,
   "af-side": 3
  }
 ],
 [
  "uk-tank-bridge",
  1,
  "equipment",
  null,
  {}
 ],
 [
  "us-block",
  1,
  "equipment",
  null,
  {}
 ],
 [
  "de-block",
  1,
  "equipment",
  null,
  {}
 ],
 [
  "de-block-reichswald",
  1,
  "equipment",
  null,
  {}
 ],
 [
  "de-88mm",
  4,
  "gun",
  "de-75mm",
  {
   "caliber": 88
  }
 ],
 [
  "de-quad20",
  4,
  "gun",
  "de-20mm",
  {
   "caliber": 20
  }
 ],
 [
  "de-nebel",
  6,
  "gun",
  "de-120mm",
  {
   "caliber": 150
  }
 ],
 [
  "de-75how",
  4,
  "gun",
  "de-75mm",
  {
   "caliber": 75
  }
 ],
 [
  "de-75ig",
  4,
  "gun",
  "attacker-inf-gun",
  {
   "caliber": 75
  }
 ],
 [
  "de-150ig",
  4,
  "gun",
  "attacker-inf-gun",
  {
   "caliber": 150
  }
 ],
 [
  "de-105how",
  4,
  "gun",
  "de-75mm",
  {
   "caliber": 105
  }
 ],
 [
  "de-37mm",
  6,
  "gun",
  "de-20mm",
  {
   "caliber": 37
  }
 ],
 [
  "de-150how",
  4,
  "gun",
  "de-75mm",
  {
   "caliber": 150
  }
 ],
 [
  "de-170how",
  4,
  "gun",
  "de-75mm",
  {
   "caliber": 170
  }
 ],
 [
  "us-105mm",
  4,
  "gun",
  "de-75mm",
  {
   "caliber": 105
  }
 ],
 [
  "us-76mm",
  4,
  "gun",
  "us-57mm",
  {
   "caliber": 76
  }
 ],
 [
  "us-155mm",
  4,
  "gun",
  "us-57mm",
  {
   "caliber": 155
  }
 ],
 [
  "us-8inch",
  4,
  "gun",
  "us-57mm",
  {
   "caliber": 203
  }
 ],
 [
  "us-40mm",
  4,
  "gun",
  "de-20mm",
  {
   "caliber": 40
  }
 ],
 [
  "us-90mm",
  4,
  "gun",
  "us-57mm",
  {
   "caliber": 90
  }
 ],
 [
  "de-wespe",
  6,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 105,
   "af-front": 2,
   "movement-points": 12
  }
 ],
 [
  "de-maultier-goodwood",
  4,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 150,
   "af-front": 1,
   "movement-points": 12
  }
 ],
 [
  "de-hummel",
  6,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 150,
   "af-front": 2,
   "movement-points": 13
  }
 ],
 [
  "de-pziv-w",
  5,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 75,
   "af-front": 8,
   "movement-points": 12
  }
 ],
 [
  "de-pziv",
  5,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 75,
   "af-front": 8,
   "movement-points": 12
  }
 ],
 [
  "de-panther-w",
  5,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 75,
   "af-front": 18,
   "movement-points": 15
  }
 ],
 [
  "de-panther",
  5,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 75,
   "af-front": 18,
   "movement-points": 15
  }
 ],
 [
  "de-tiger",
  4,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 88,
   "af-front": 11,
   "movement-points": 11
  }
 ],
 [
  "de-kingtiger",
  4,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 88,
   "af-front": 26,
   "movement-points": 11
  }
 ],
 [
  "de-grille",
  6,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 150,
   "af-front": 2,
   "movement-points": 12
  }
 ],
 [
  "de-234-1",
  3,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 20,
   "af-front": 2,
   "movement-points": 30
  }
 ],
 [
  "de-234-2",
  3,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 50,
   "af-front": 3,
   "movement-points": 30
  }
 ],
 [
  "de-234-4",
  3,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 75,
   "af-front": 2,
   "movement-points": 30
  }
 ],
 [
  "de-lynx",
  3,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 20,
   "af-front": 3,
   "movement-points": 18
  }
 ],
 [
  "de-wirbelwind",
  4,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 20,
   "af-front": 8,
   "movement-points": 12
  }
 ],
 [
  "us-m7",
  6,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 105,
   "af-front": 4,
   "movement-points": 12
  }
 ],
 [
  "us-m8",
  3,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 37,
   "af-front": 1,
   "movement-points": 30
  }
 ],
 [
  "us-m16",
  4,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 0,
   "af-front": 1,
   "movement-points": 16
  }
 ],
 [
  "us-m10",
  4,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 76,
   "af-front": 4,
   "movement-points": 12
  }
 ],
 [
  "us-m18",
  4,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 76,
   "af-front": 2,
   "movement-points": 24
  }
 ],
 [
  "us-m36",
  4,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 90,
   "af-front": 4,
   "movement-points": 13
  }
 ],
 [
  "us-m24",
  5,
  "vehicle",
  "us-m4-75",
  {
   "caliber": 75,
   "af-front": 4,
   "movement-points": 18
  }
 ],
 [
  "de-pziii",
  5,
  "vehicle",
  "attacker-tank",
  {}
 ],
 [
  "de-smg",
  4,
  "squad",
  "attacker-elite-squad-5-4-8",
  {}
 ],
 [
  "de-security",
  4,
  "squad",
  "attacker-2nd-line-squad",
  {}
 ],
 [
  "de-mg",
  6,
  "team",
  "attacker-mmg",
  {}
 ],
 [
  "us-armored-rifle",
  3,
  "squad",
  "american-elite-squad",
  {}
 ],
 [
  "us-engineer",
  4,
  "squad",
  "american-elite-squad",
  {}
 ],
 [
  "us-scout",
  3,
  "squad",
  "us-rifle",
  {}
 ],
 [
  "us-m20",
  3,
  "vehicle",
  "us-halftrack",
  {
   "movement-points": 30,
   "movement-type": "armored-car"
  }
 ]
];
 const profiles={...SquadProfiles.types},definitions={...SquadAslDefinitions.types};
 for(const [id,count,kind,base,changes] of rows){
  profiles[id]={count,kind,label:kind==="squad"?"Squad":kind==="vehicle"?"Vehicle":kind==="equipment"?"Equipment":kind==="gun"?"Gun and crew":kind==="mortar"?"Mortar and crew":"Weapon team",radius:kind==="equipment"?0:4};
  const source=catalog[base]||definitions[base];
  definitions[id]=source?{...JSON.parse(JSON.stringify(source)),status:catalog[base]&&!Object.keys(changes).length?"catalog match":"provisional estimate",rationale:"Playable mapping to "+base+". Composition is a game default; no Panzer combat factors copied.",values:{...source.values,...changes}}:{status:"equipment",definitionId:id,values:{},rationale:"Retained as one deployment object; no combat factors."};
 }
 window.SquadProfiles=Object.freeze({...SquadProfiles,types:Object.freeze(profiles)});
 window.SquadAslDefinitions={...SquadAslDefinitions,libraryVersion:"imported-library-1",types:definitions};
})();
