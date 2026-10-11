"use strict";
// Nominal composition transcribed from Avalon Hill Panzer Leader, printed pp.19-22.
// Personnel/support inventories remain evidence, not extra combatants or free weapons.
window.SquadComposition = (()=>{
 const source="Panzer Leader Unit Function Table, printed pages 19-22 (PDF pages 20-23)";
 const rows={
 "us-rifle": {
  "count": null,
  "personnel": 36,
  "page": 19,
  "composition": "36 personnel; 6 automatic rifles, 4 machine guns, one 60 mm mortar.",
  "status": "source table"
 },
 "uk-rifle": {
  "count": null,
  "personnel": 36,
  "page": 19,
  "composition": "36 personnel; 6 automatic rifles, 4 machine guns, one 60 mm mortar.",
  "status": "source table"
 },
 "ca-rifle": {
  "count": null,
  "personnel": 36,
  "page": 19,
  "composition": "36 personnel; 6 automatic rifles, 4 machine guns, one 60 mm mortar.",
  "status": "source table"
 },
 "be-rifle": {
  "count": null,
  "personnel": 36,
  "page": 19,
  "composition": "36 personnel; 6 automatic rifles, 4 machine guns, one 60 mm mortar.",
  "status": "source table"
 },
 "us-armored-rifle": {
  "count": null,
  "personnel": 45,
  "page": 19,
  "composition": "45 personnel; 6 automatic rifles, 5 machine guns, 12 SMGs, one 60 mm mortar.",
  "status": "source table"
 },
 "us-engineer": {
  "count": null,
  "personnel": 49,
  "page": 19,
  "composition": "49 personnel; 2 machine guns; mine-clearing, demolition and construction equipment.",
  "status": "source table"
 },
 "uk-engineer": {
  "count": null,
  "personnel": 65,
  "page": 19,
  "composition": "65 personnel; 3 Bren guns; mine-clearing, demolition and construction equipment.",
  "status": "source table"
 },
 "ca-engineer": {
  "count": null,
  "personnel": 65,
  "page": 19,
  "composition": "65 personnel; 3 Bren guns; mine-clearing, demolition and construction equipment.",
  "status": "source table"
 },
 "be-engineer": {
  "count": null,
  "personnel": 65,
  "page": 19,
  "composition": "65 personnel; 3 Bren guns; mine-clearing, demolition and construction equipment.",
  "status": "source table"
 },
 "us-scout": {
  "count": null,
  "personnel": 24,
  "page": 19,
  "composition": "24 personnel; 4 automatic rifles, 2 machine guns.",
  "status": "source table"
 },
 "uk-scout": {
  "count": null,
  "personnel": 24,
  "page": 19,
  "composition": "24 personnel; 4 automatic rifles, 2 machine guns.",
  "status": "source table"
 },
 "ca-scout": {
  "count": null,
  "personnel": 24,
  "page": 19,
  "composition": "24 personnel; 4 automatic rifles, 2 machine guns.",
  "status": "source table"
 },
 "us-mg": {
  "count": 6,
  "personnel": 40,
  "page": 19,
  "composition": "6 .50-cal machine guns, 3 rocket launchers, 40 personnel. Canadian assignment uses the Allied MG row as a game proxy.",
  "status": "source table",
  "kind": "team"
 },
 "ca-mg": {
  "count": 6,
  "personnel": 40,
  "page": 19,
  "composition": "6 .50-cal machine guns, 3 rocket launchers, 40 personnel. Canadian assignment uses the Allied MG row as a game proxy.",
  "status": "source table",
  "kind": "team"
 },
 "us-57mm": {
  "count": 4,
  "personnel": 39,
  "page": 19,
  "composition": "4 x 57 mm anti-tank guns; 39 personnel.",
  "status": "source table"
 },
 "uk-6pdr": {
  "count": 4,
  "personnel": 39,
  "page": 19,
  "composition": "4 x 57 mm anti-tank guns; 39 personnel.",
  "status": "source table"
 },
 "ca-6pdr": {
  "count": 4,
  "personnel": 39,
  "page": 19,
  "composition": "4 x 57 mm anti-tank guns; 39 personnel.",
  "status": "source table"
 },
 "us-76mm": {
  "count": 4,
  "personnel": 47,
  "page": 19,
  "composition": "4 x 3-inch M5 anti-tank guns; 47 personnel.",
  "status": "source table"
 },
 "us-90mm": {
  "count": 4,
  "personnel": 96,
  "page": 19,
  "composition": "4 x 90 mm M2 anti-aircraft guns; 96 personnel.",
  "status": "source table"
 },
 "us-40mm": {
  "count": 6,
  "personnel": 42,
  "page": 19,
  "composition": "6 x 40 mm Bofors anti-aircraft guns; 42 personnel.",
  "status": "source table"
 },
 "uk-25pdr": {
  "count": 6,
  "personnel": 39,
  "page": 19,
  "composition": "6 x 25-pounder gun-howitzers; 39 personnel. Table prints 86.4 mm; tactical estimate uses 87.6 mm.",
  "status": "source table"
 },
 "ca-25pdr": {
  "count": 6,
  "personnel": 39,
  "page": 19,
  "composition": "6 x 25-pounder gun-howitzers; 39 personnel. Table prints 86.4 mm; tactical estimate uses 87.6 mm.",
  "status": "source table"
 },
 "be-25pdr": {
  "count": 6,
  "personnel": 39,
  "page": 19,
  "composition": "6 x 25-pounder gun-howitzers; 39 personnel. Table prints 86.4 mm; tactical estimate uses 87.6 mm.",
  "status": "source table"
 },
 "us-105mm": {
  "count": 6,
  "personnel": 117,
  "page": 19,
  "composition": "6 x 105 mm M2A2 howitzers; 117 personnel.",
  "status": "source table"
 },
 "us-155mm": {
  "count": 6,
  "personnel": 152,
  "page": 19,
  "composition": "6 x 155 mm M1 howitzers; 152 personnel.",
  "status": "source table"
 },
 "us-8inch": {
  "count": 6,
  "personnel": 136,
  "page": 19,
  "composition": "6 x 8-inch guns; 136 personnel.",
  "status": "source table"
 },
 "ca-76mortar": {
  "count": 4,
  "personnel": 29,
  "page": 19,
  "composition": "4 x 76 mm mortars; 29 personnel.",
  "status": "source table"
 },
 "be-76mortar": {
  "count": 4,
  "personnel": 29,
  "page": 19,
  "composition": "4 x 76 mm mortars; 29 personnel.",
  "status": "source table"
 },
 "us-81mm": {
  "count": 6,
  "personnel": 59,
  "page": 19,
  "composition": "6 x 81 mm mortars; 59 personnel.",
  "status": "source table"
 },
 "uk-81mm": {
  "count": 6,
  "personnel": 59,
  "page": 19,
  "composition": "6 x 81 mm mortars; 59 personnel.",
  "status": "source table"
 },
 "ca-107mortar": {
  "count": 4,
  "personnel": 52,
  "page": 19,
  "composition": "4 x 4.2-inch (107 mm) mortars; 52 personnel.",
  "status": "source table"
 },
 "us-truck": {
  "count": 5,
  "personnel": 5,
  "page": 19,
  "composition": "5 x 2.5-ton trucks; 5 drivers.",
  "status": "source table"
 },
 "uk-truck": {
  "count": 5,
  "personnel": 5,
  "page": 19,
  "composition": "5 x 2.5-ton trucks; 5 drivers.",
  "status": "source table"
 },
 "ca-truck": {
  "count": 5,
  "personnel": 5,
  "page": 19,
  "composition": "5 x 2.5-ton trucks; 5 drivers.",
  "status": "source table"
 },
 "be-truck": {
  "count": 5,
  "personnel": 5,
  "page": 19,
  "composition": "5 x 2.5-ton trucks; 5 drivers.",
  "status": "source table"
 },
 "uk-m3": {
  "count": 5,
  "personnel": 12,
  "page": 19,
  "composition": "One Daimler scout car and 4 M3 scout cars; 5 machine guns; 12 personnel.",
  "status": "source table"
 },
 "uk-bren": {
  "count": 6,
  "personnel": 12,
  "page": 19,
  "composition": "6 Bren carriers, 6 Bren guns; 12 personnel. MG-carrier label uses this carrier row.",
  "status": "source table"
 },
 "ca-bren": {
  "count": 6,
  "personnel": 12,
  "page": 19,
  "composition": "6 Bren carriers, 6 Bren guns; 12 personnel. MG-carrier label uses this carrier row.",
  "status": "source table"
 },
 "be-bren": {
  "count": 6,
  "personnel": 12,
  "page": 19,
  "composition": "6 Bren carriers, 6 Bren guns; 12 personnel. MG-carrier label uses this carrier row.",
  "status": "source table"
 },
 "uk-mgcarrier": {
  "count": 6,
  "personnel": 12,
  "page": 19,
  "composition": "6 Bren carriers, 6 Bren guns; 12 personnel. MG-carrier label uses this carrier row.",
  "status": "source table"
 },
 "us-halftrack": {
  "count": 5,
  "personnel": 10,
  "page": 19,
  "composition": "5 M3 halftracks, 5 machine guns; 10 personnel.",
  "status": "source table"
 },
 "us-m20": {
  "count": 6,
  "personnel": 24,
  "page": 19,
  "composition": "6 M20 armored cars, 6 machine guns; 24 personnel.",
  "status": "source table"
 },
 "us-m8": {
  "count": 6,
  "personnel": 24,
  "page": 19,
  "composition": "6 M8 armored cars; 37 mm guns, 12 machine guns; 24 personnel.",
  "status": "source table"
 },
 "uk-daimler": {
  "count": 6,
  "personnel": 24,
  "page": 19,
  "composition": "6 Daimler armored cars; table prints 37 mm, 6 machine guns, 24 personnel. Tactical estimate uses 40 mm.",
  "status": "source table"
 },
 "be-daimler": {
  "count": 6,
  "personnel": 24,
  "page": 19,
  "composition": "6 Daimler armored cars; table prints 37 mm, 6 machine guns, 24 personnel. Tactical estimate uses 40 mm.",
  "status": "source table"
 },
 "uk-sexton": {
  "count": 6,
  "personnel": 51,
  "page": 20,
  "composition": "6 Sexton 25-pounder self-propelled howitzers, 6 machine guns; 51 personnel.",
  "status": "source table"
 },
 "us-m7": {
  "count": 6,
  "personnel": 45,
  "page": 20,
  "composition": "6 M7 Priest 105 mm self-propelled howitzers, 6 machine guns; 45 personnel.",
  "status": "source table"
 },
 "us-m16": {
  "count": 6,
  "personnel": 39,
  "page": 20,
  "composition": "6 quad .50-cal mounts on M3 halftracks; 39 personnel.",
  "status": "source table"
 },
 "us-m4-105": {
  "count": 3,
  "personnel": 15,
  "page": 20,
  "composition": "3 M4A3/105 Sherman assault guns, 3 machine guns; 15 personnel.",
  "status": "source table"
 },
 "uk-achilles": {
  "count": 4,
  "personnel": 20,
  "page": 20,
  "composition": "4 Achilles tank destroyers; table prints 76 mm; 4 machine guns, 20 personnel.",
  "status": "source table"
 },
 "be-achilles": {
  "count": 4,
  "personnel": 20,
  "page": 20,
  "composition": "4 Achilles tank destroyers; table prints 76 mm; 4 machine guns, 20 personnel.",
  "status": "source table"
 },
 "us-m10": {
  "count": 4,
  "personnel": 20,
  "page": 20,
  "composition": "4 tank destroyers with 76 mm guns, 5 machine guns; 20 personnel.",
  "status": "source table"
 },
 "us-m18": {
  "count": 4,
  "personnel": 20,
  "page": 20,
  "composition": "4 tank destroyers with 76 mm guns, 5 machine guns; 20 personnel.",
  "status": "source table"
 },
 "us-m36": {
  "count": 4,
  "personnel": 20,
  "page": 20,
  "composition": "4 M36 tank destroyers with 90 mm guns, 5 machine guns; 20 personnel.",
  "status": "source table"
 },
 "us-m5": {
  "count": 5,
  "personnel": 20,
  "page": 20,
  "composition": "5 M5 Stuart tanks; 37 mm guns, 10 machine guns; 20 personnel.",
  "status": "source table"
 },
 "us-m24": {
  "count": 5,
  "personnel": 20,
  "page": 20,
  "composition": "5 M24 tanks; 75 mm guns, 10 machine guns; 20 personnel.",
  "status": "source table"
 },
 "uk-cromwell-goodwood": {
  "count": 4,
  "personnel": 20,
  "page": 20,
  "composition": "4 Cromwell tanks; 75 mm guns, 8 machine guns; 20 personnel.",
  "status": "source table"
 },
 "uk-sherman": {
  "count": 4,
  "personnel": 20,
  "page": 20,
  "composition": "3 M4/75 Shermans and one M4A4 Sherman Firefly (76.2 mm); 8 machine guns; 20 personnel.",
  "status": "source table"
 },
 "us-m4-75": {
  "count": 5,
  "personnel": 25,
  "page": 20,
  "composition": "5 Shermans with the designated main armament, 10 machine guns; 25 personnel.",
  "status": "source table"
 },
 "us-m4-76": {
  "count": 5,
  "personnel": 25,
  "page": 20,
  "composition": "5 Shermans with the designated main armament, 10 machine guns; 25 personnel.",
  "status": "source table"
 },
 "uk-valentine-bridge-carrier": {
  "count": 4,
  "personnel": null,
  "page": 20,
  "composition": "4 modified Valentine tanks with main armament removed; small box-girder bridges.",
  "status": "source table"
 },
 "uk-tank-bridge": {
  "count": 1,
  "personnel": null,
  "page": 22,
  "composition": "Retained as one deployment object. No combat strength or personnel inferred.",
  "status": "scenario equipment",
  "kind": "equipment"
 },
 "us-block": {
  "count": 1,
  "personnel": null,
  "page": 22,
  "composition": "Retained as one deployment object. No combat strength or personnel inferred.",
  "status": "scenario equipment",
  "kind": "equipment"
 },
 "de-block": {
  "count": 1,
  "personnel": null,
  "page": 22,
  "composition": "Retained as one deployment object. No combat strength or personnel inferred.",
  "status": "scenario equipment",
  "kind": "equipment"
 },
 "de-block-reichswald": {
  "count": 1,
  "personnel": null,
  "page": 22,
  "composition": "Retained as one deployment object. No combat strength or personnel inferred.",
  "status": "scenario equipment",
  "kind": "equipment"
 },
 "de-75mm": {
  "count": 4,
  "personnel": 45,
  "page": 21,
  "composition": "4 x 75 mm anti-tank guns; 45 personnel.",
  "status": "source table"
 },
 "de-88mm": {
  "count": 4,
  "personnel": 111,
  "page": 21,
  "composition": "4 x 88 mm anti-tank guns; 111 personnel.",
  "status": "source table"
 },
 "de-20mm": {
  "count": 6,
  "personnel": 40,
  "page": 21,
  "composition": "6 x 20 mm anti-aircraft guns; 40 personnel.",
  "status": "source table"
 },
 "de-quad20": {
  "count": 3,
  "personnel": 45,
  "page": 21,
  "composition": "3 quadruple 20 mm anti-aircraft mounts; 45 personnel.",
  "status": "source table"
 },
 "de-37mm": {
  "count": 6,
  "personnel": 42,
  "page": 21,
  "composition": "6 x 37 mm anti-aircraft guns; 42 personnel.",
  "status": "source table"
 },
 "de-75ig": {
  "count": 2,
  "personnel": 36,
  "page": 21,
  "composition": "2 x 75 mm infantry guns; 36 personnel.",
  "status": "source table"
 },
 "de-75how": {
  "count": 6,
  "personnel": 67,
  "page": 21,
  "composition": "6 x 75 mm leFK 18 howitzers; 67 personnel.",
  "status": "source table"
 },
 "de-105how": {
  "count": 6,
  "personnel": 112,
  "page": 21,
  "composition": "6 x 105 mm leFH 18 howitzers; 112 personnel.",
  "status": "source table"
 },
 "de-150ig": {
  "count": 2,
  "personnel": 39,
  "page": 21,
  "composition": "2 x 150 mm infantry guns; 39 personnel.",
  "status": "source table"
 },
 "de-150how": {
  "count": 6,
  "personnel": 120,
  "page": 21,
  "composition": "6 x 150 mm sFH 36 howitzers; 120 personnel.",
  "status": "source table"
 },
 "de-170how": {
  "count": 6,
  "personnel": 139,
  "page": 21,
  "composition": "6 x 170 mm K18 guns; 139 personnel.",
  "status": "source table"
 },
 "de-81mm": {
  "count": 6,
  "personnel": 66,
  "page": 21,
  "composition": "6 x 81 mm mortars; 66 personnel.",
  "status": "source table"
 },
 "de-120mm": {
  "count": 4,
  "personnel": 47,
  "page": 21,
  "composition": "4 x 120 mm mortars; 47 personnel.",
  "status": "source table"
 },
 "de-nebel": {
  "count": 6,
  "personnel": 86,
  "page": 21,
  "composition": "6 x 150 mm Nebelwerfer rocket launchers; 86 personnel.",
  "status": "source table",
  "kind": "gun"
 },
 "de-engineer": {
  "count": null,
  "personnel": 63,
  "page": 21,
  "composition": "63 personnel; 10 SMGs, 9 machine guns, 8 flamethrowers; demolition and mine-clearing equipment.",
  "status": "source table"
 },
 "de-security": {
  "count": null,
  "personnel": 46,
  "page": 21,
  "composition": "46 personnel; 12 SMGs, 4 machine guns.",
  "status": "source table"
 },
 "de-rifle": {
  "count": null,
  "personnel": 59,
  "page": 21,
  "composition": "59 personnel; 11 SMGs, 6 machine guns.",
  "status": "source table"
 },
 "de-smg": {
  "count": null,
  "personnel": 52,
  "page": 21,
  "composition": "52 personnel; 30 SMGs, 4 machine guns.",
  "status": "source table"
 },
 "de-mg": {
  "count": 6,
  "personnel": null,
  "page": 21,
  "composition": "No standalone German MG row in the original table. Six weapon teams remain a playable assumption; exact personnel unverified.",
  "status": "explicit game assumption",
  "kind": "team"
 },
 "de-wagon": {
  "count": 4,
  "personnel": 6,
  "page": 21,
  "composition": "4 horse-drawn vehicles; 6 personnel.",
  "status": "source table"
 },
 "de-truck": {
  "count": 5,
  "personnel": 5,
  "page": 21,
  "composition": "5 x 3-ton trucks; 5 drivers.",
  "status": "source table"
 },
 "de-halftrack": {
  "count": 5,
  "personnel": 10,
  "page": 21,
  "composition": "5 SdKfz 251 halftracks, 5 machine guns; 10 personnel.",
  "status": "source table"
 },
 "de-234-1": {
  "count": 7,
  "personnel": 38,
  "page": 21,
  "composition": "7 SdKfz 234 armored cars with 20 mm guns; 38 personnel.",
  "status": "source table"
 },
 "de-234-2": {
  "count": 7,
  "personnel": 38,
  "page": 21,
  "composition": "7 SdKfz 234 armored cars with 50 mm guns; 38 personnel.",
  "status": "source table"
 },
 "de-234-4": {
  "count": 7,
  "personnel": 38,
  "page": 21,
  "composition": "7 SdKfz 234 armored cars with 75 mm guns; 38 personnel.",
  "status": "source table"
 },
 "de-wespe": {
  "count": 6,
  "personnel": 125,
  "page": 22,
  "composition": "6 Wespe 105 mm self-propelled howitzers; 125 personnel.",
  "status": "source table"
 },
 "de-maultier-goodwood": {
  "count": 6,
  "personnel": 126,
  "page": 22,
  "composition": "6 Maultier 150 mm self-propelled rocket launchers; 126 personnel.",
  "status": "source table"
 },
 "de-hummel": {
  "count": 6,
  "personnel": 131,
  "page": 22,
  "composition": "6 Hummel 150 mm self-propelled howitzers; 131 personnel.",
  "status": "source table"
 },
 "de-grille": {
  "count": 2,
  "personnel": 26,
  "page": 22,
  "composition": "2 sIG 33/1 150 mm self-propelled guns; 26 personnel.",
  "status": "source table"
 },
 "de-wirbelwind": {
  "count": 3,
  "personnel": 40,
  "page": 22,
  "composition": "3 Flakpanzer IV tanks with quad 20 mm mounts; 40 personnel.",
  "status": "source table"
 },
 "de-hetzer": {
  "count": 6,
  "personnel": 31,
  "page": 22,
  "composition": "6 assault guns/tank destroyers with 75 mm guns; 31 personnel.",
  "status": "source table"
 },
 "de-stug": {
  "count": 6,
  "personnel": 31,
  "page": 22,
  "composition": "6 assault guns/tank destroyers with 75 mm guns; 31 personnel.",
  "status": "source table"
 },
 "de-lynx": {
  "count": 5,
  "personnel": 25,
  "page": 22,
  "composition": "5 PzKw II L tanks with 20 mm guns; 25 personnel.",
  "status": "source table"
 },
 "de-pziii": {
  "count": 4,
  "personnel": 20,
  "page": 22,
  "composition": "4 PzKw III J tanks with 50 mm guns; 20 personnel.",
  "status": "source table"
 },
 "de-pziv-w": {
  "count": 4,
  "personnel": 20,
  "page": 22,
  "composition": "4 PzKw IV H tanks with 75 mm guns; 20 personnel. Original counter designation: PzKw IV (W).",
  "status": "source table"
 },
 "de-pziv": {
  "count": 5,
  "personnel": 25,
  "page": 22,
  "composition": "5 PzKw IV H tanks with 75 mm guns; 25 personnel. Original counter designation: PzKw IV (SS).",
  "status": "source table"
 },
 "de-panther-w": {
  "count": 4,
  "personnel": 20,
  "page": 22,
  "composition": "4 Panthers with 75 mm guns; 20 personnel. Original counter designation: PzKw V (W).",
  "status": "source table"
 },
 "de-panther": {
  "count": 5,
  "personnel": 25,
  "page": 22,
  "composition": "5 Panthers with 75 mm guns; 25 personnel. Original counter designation: PzKw V (SS).",
  "status": "source table"
 },
 "de-tiger": {
  "count": 5,
  "personnel": 25,
  "page": 22,
  "composition": "5 Tiger tanks of the designated model with 88 mm guns; 25 personnel.",
  "status": "source table"
 },
 "de-kingtiger": {
  "count": 5,
  "personnel": 25,
  "page": 22,
  "composition": "5 Tiger tanks of the designated model with 88 mm guns; 25 personnel.",
  "status": "source table"
 }
};
 rows['uk-tank-bridge'].page=20;
 const profiles={...SquadProfiles.types};
 for(const [id,evidence] of Object.entries(rows)){
  if(!profiles[id])continue;
  const old=profiles[id];
  profiles[id]={...old,count:evidence.count??old.count,kind:evidence.kind||old.kind,evidence:{...evidence,source},
   compositionRule:evidence.count===null?"Squad count is a playable grouping, not a personnel census. Source personnel and supporting weapons remain attached to the parent; they are not generated again as extra units.":"One tactical object per principal vehicle or weapon in the source composition; operating crews are included."};
 }
 for(const id of ['de-pziv','de-panther'])profiles[id].sourceDesignation='SS';
 profiles['uk-sherman'].members=[...Array(3).fill({definitionId:'uk-sherman',label:'Sherman 75'}),{definitionId:'uk-firefly',label:'Sherman Firefly'}];
 profiles['uk-m3'].members=[{definitionId:'uk-daimler-scout',label:'Daimler scout car'},...Array(4).fill({definitionId:'uk-m3',label:'M3 scout car'})];
 window.SquadProfiles=Object.freeze({...SquadProfiles,version:'source-composition-2',types:Object.freeze(profiles)});
 return {source,rows};
})();
