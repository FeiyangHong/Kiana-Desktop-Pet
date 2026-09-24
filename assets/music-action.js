(action)=>{
 const selectors={toggle:'[title*="Ctrl + P"]',previous:'[title^="上一首"]',next:'[title^="下一首"]'};
 if(!Object.prototype.hasOwnProperty.call(selectors,action))return 'unavailable';
 const bar=document.querySelector('.default-bar-wrapper')||document.querySelector('.vinyl-page-bar-wrapper');
 const button=bar?.querySelector(selectors[action])?.closest('button');
 if(!button||button.disabled||button.getAttribute('aria-disabled')==='true')return 'unavailable';
 button.click();return 'clicked';
}
