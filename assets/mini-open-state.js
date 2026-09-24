// Ask native lifecycle state: a retained DOM can outlive a closed Mini window.
new Promise(resolve=>{
 let done=false;const finish=value=>{if(done)return;done=true;clearTimeout(timer);window.removeEventListener('message',receive);resolve(value);};
 const receive=event=>{const data=event.data;if(data?.type==='avatar-overlay-open-state-changed'&&typeof data.isOpen==='boolean')finish(data.isOpen?'open':'closed');};
 const timer=setTimeout(()=>finish('unknown'),700);
 window.addEventListener('message',receive);
 try{const result=window.electronBridge?.sendMessageFromView?.({type:'avatar-overlay-open-state-request'});if(result?.catch)result.catch(()=>finish('unknown'));}catch{finish('unknown');}
})
