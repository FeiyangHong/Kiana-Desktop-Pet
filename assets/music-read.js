(()=>{
 const bar=document.querySelector('.default-bar-wrapper')||document.querySelector('.vinyl-page-bar-wrapper');
 if(!bar)return JSON.stringify({connected:false});
 const el=document.querySelector('#root')?.firstElementChild;
 let fiber=el?.[Object.keys(el||{}).find(k=>/^__react(Fiber|InternalInstance)\$/.test(k))],p=null;
 for(let n=0;fiber&&n<60;n++,fiber=fiber.return){const store=fiber.memoizedProps?.store;if(store&&typeof store.getState==='function'){p=store.getState().playing;break;}}
 const usable=e=>{const b=e?.closest('button');return !!b&&!b.disabled&&b.getAttribute('aria-disabled')!=='true';};
 const play=bar.querySelector('[title*="Ctrl + P"]'),prev=bar.querySelector('[title^="上一首"]'),next=bar.querySelector('[title^="下一首"]');
 const slider=document.querySelector('[aria-label="播放进度调节"] input[type="range"]');
 const title=p?.resourceName||bar.querySelector('.main-title .title')?.textContent||'';
 return JSON.stringify({connected:!!title&&!!play,title:title,artist:p?.resourceArtists?.map(a=>a.name).join(' / ')||bar.querySelector('.author')?.textContent||'',id:String(p?.resourceTrackId||''),cover:p?.resourceCoverUrl||'',playing:!!play?.title.startsWith('暂停'),position:Number(slider?.value||0),duration:Number(slider?.max||p?.resourceDuration||0),toggle:usable(play),previous:usable(prev),next:usable(next)});
})()
