"use strict";
// Portable operation files contain the complete world, including embedded deployment saves.
window.CampaignFile=(()=>{
 const format='campaign-atlas-save',version=1;
 function check(file,resolve){
  if(!file||file.format!==format||file.version!==version)throw Error('Not a supported Campaign Atlas save file.');
  const config=resolve(file.campaign),w=file.world;
  if(!w||typeof w.id!=='string'||!/^[a-zA-Z0-9-]+$/.test(w.id))throw Error('Invalid campaign identity.');
  config.validate(w);
  for(const field of ['situationPlans','squadPlans'])if(w[field]!==undefined&&(!w[field]||Array.isArray(w[field])||typeof w[field]!=='object'))throw Error('Invalid '+field+'.');
  return config;
 }
 function capture(storage,campaign,resolve){
  const config=resolve(campaign),id=storage.getItem(config.prefix+'active');
  if(!id)throw Error('Start or open a campaign before saving it to a file.');
  const file={format,version,savedAt:new Date().toISOString(),campaign,world:JSON.parse(storage.getItem(config.prefix+id))};
  check(file,resolve);return file;
 }
 function install(storage,file,resolve){
  const config=check(file,resolve),key=config.prefix+file.world.id,active=config.prefix+'active',old=storage.getItem(key),previous=storage.getItem(active);
  const backup=key+':before-file-load:'+Date.now();
  // Keep the previous same-ID save and roll back if either write fails.
  if(old!==null)storage.setItem(backup,old);
  try{storage.setItem(key,JSON.stringify(file.world));storage.setItem(active,file.world.id);}
  catch(e){if(old===null)storage.removeItem(key);else storage.setItem(key,old);if(previous===null)storage.removeItem(active);else storage.setItem(active,previous);throw e;}
  return config.url;
 }
 return{check,capture,install};
})();
