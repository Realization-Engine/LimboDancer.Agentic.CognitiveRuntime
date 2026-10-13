"use strict";
window.CampaignGeographicStore=(()=>{
 const KEY='campaign-atlas:continental:v1';
 function publish(w,storage,numbers=[]){storage=globalThis.window?.CampaignSaveStorage?.wrap(storage)||storage;if(!window.CampaignGeography)return;const routes=w.operation.supplyProfile?.routes||Object.fromEntries(numbers.map(n=>{const d=window.PANZER_SITUATION_LIBRARY?.find(d=>d.situation.number===n);return[n,{binding:d?CampaignSupplySpatial.build(d):null}];}));let saved={};try{saved=JSON.parse(storage.getItem(KEY)||'{}');}catch{}w.geographicRegistration={version:CampaignGeography.version,crs:'EPSG:3035',bindings:{}};
  for(const [number,r]of Object.entries(routes)){const b=r.binding;if(!b)continue;const registration=CampaignGeography.register(b);w.geographicRegistration.bindings[number]=registration;saved[number]={operationId:w.id,revision:w.revision,sourceNumber:Number(number),registration,binding:b,operation:w.operation};}
  storage.setItem(KEY,JSON.stringify(saved));
 }
 function read(storage,number){storage=globalThis.window?.CampaignSaveStorage?.wrap(storage)||storage;try{return JSON.parse(storage.getItem(KEY)||'{}')[number]||null;}catch{return null;}}
 return{KEY,publish,read};
})();
