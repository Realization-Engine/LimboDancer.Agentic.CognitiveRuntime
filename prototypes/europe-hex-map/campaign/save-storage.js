"use strict";
// Reversible compression for large campaign saves. Legacy JSON remains readable.
window.CampaignSaveStorage=(()=>{
 const prefix='~campaign-lzw1~',owned=k=>['campaign-atlas:world:v1:','campaign-atlas:normandy-stlo:v1:','campaign-atlas:source-operation:','campaign-atlas:continental:'].some(p=>k.startsWith(p));
 function encode(text){const bytes=new TextEncoder().encode(text),dict=new Map();let next=256,w='',out=[];
  for(const byte of bytes){const c=String.fromCharCode(byte),wc=w+c;if(wc.length===1||dict.has(wc)){w=wc;continue;}out.push(w.length===1?w.charCodeAt(0):dict.get(w));if(next<65535)dict.set(wc,next++);w=c;}if(w)out.push(w.length===1?w.charCodeAt(0):dict.get(w));return prefix+out.map(n=>String.fromCharCode(n)).join('');
 }
 function decode(text){if(!text?.startsWith(prefix))return text;const codes=Array.from(text.slice(prefix.length).split(''),c=>c.charCodeAt(0));if(!codes.length)return '';const dict=[],out=[];for(let i=0;i<256;i++)dict[i]=String.fromCharCode(i);let w=dict[codes[0]],next=256;out.push(w);for(const code of codes.slice(1)){const value=dict[code]??(code===next?w+w[0]:null);if(value===null)throw Error('Invalid compressed campaign save');out.push(value);if(next<65535)dict[next++]=w+value[0];w=value;}const raw=out.join('');return new TextDecoder().decode(Uint8Array.from(raw,c=>c.charCodeAt(0)));}
 function migrate(storage){for(let i=0;i<storage.length;i++){const k=storage.key(i),value=storage.getItem(k);if(owned(k)&&value?.startsWith('{')){const encoded=encode(value);if(encoded.length<value.length)storage.setItem(k,encoded);}}}
 function wrap(storage){return new Proxy(storage,{get(target,key){if(key==='getItem')return k=>decode(target.getItem(k));if(key==='setItem')return(k,value)=>{const encoded=owned(k)&&value.startsWith('{')?encode(value):value;try{target.setItem(k,encoded);}catch(e){if(e.name!=='QuotaExceededError')throw e;migrate(target);target.setItem(k,encoded);}};const value=Reflect.get(target,key,target);return typeof value==='function'?value.bind(target):value;}});}
 return{encode,decode,wrap,migrate};
})();
