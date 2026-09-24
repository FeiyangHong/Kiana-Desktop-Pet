// Temporary presentation only. Keep native state and controls mounted.
// A renewable lease restores Mini if the independent pet stops responding.
(()=>{
 const key='__kianaBackgroundMiniV1',attr='data-kiana-background-mini',id='kiana-background-mini-v1';
 let bridge=window[key];
 if(!bridge){
  const style=document.createElement('style');style.id=id;
  style.textContent=`html[${attr}] [data-avatar-mascot],html[${attr}] [data-avatar-overlay-hit-region="mascot"]{visibility:hidden!important;pointer-events:none!important}
html[${attr}="parked"] body,html[${attr}="parked"] body *{visibility:hidden!important;pointer-events:none!important}`;
  (document.head||document.documentElement).appendChild(style);
  let timer,blurTimer;
  const park=()=>{document.documentElement.setAttribute(attr,'parked');window.dispatchEvent(new Event('resize'));};
  const onKey=e=>{if(e.key==='Escape')park();};
  const onBlur=()=>{clearTimeout(blurTimer);blurTimer=setTimeout(()=>{if(!document.hasFocus())park();},800);};
  bridge={expires:0,reveal(){clearTimeout(blurTimer);document.documentElement.setAttribute(attr,'panel');window.dispatchEvent(new Event('resize'));},park,
   restore(){clearInterval(timer);clearTimeout(blurTimer);window.removeEventListener('keydown',onKey,true);window.removeEventListener('blur',onBlur);style.remove();document.documentElement.removeAttribute(attr);delete window[key];window.dispatchEvent(new Event('resize'));}};
  window[key]=bridge;window.addEventListener('keydown',onKey,true);window.addEventListener('blur',onBlur);
  timer=setInterval(()=>{if(Date.now()>bridge.expires)bridge.restore();},1000);park();
 }
 bridge.expires=Date.now()+10000;
})();
