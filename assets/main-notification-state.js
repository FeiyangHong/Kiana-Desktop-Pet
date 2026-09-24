// Read only notification metadata already held by the main app.
// No message text, tokens, prompts, tool arguments, clicks, or read acknowledgments.
(()=>{
 const clients=new Set(),rows=new Map();let ready=false;
 for(const element of [...document.querySelectorAll('button')].slice(0,8)){
  let fiber=element[Object.keys(element).find(k=>k.startsWith('__reactFiber$'))];
  for(let depth=0;fiber&&depth<100;depth++,fiber=fiber.return){
   const props=fiber.memoizedProps;
   for(const name of ['value','client']){
    const client=props?.[name];if(typeof client?.getQueryCache!=='function'||clients.has(client))continue;
    clients.add(client);
    for(const query of client.getQueryCache().getAll()){
     if(!['recent-conversations-meta','recent-conversations'].includes(query.queryKey?.[0])||query.state?.status!=='success'||!Array.isArray(query.state.data))continue;
     ready=true;
     for(const item of query.state.data){
      if(!item||typeof item.id!=='string'||! /^[a-zA-Z0-9_-]{1,160}$/.test(item.id))continue;
      // Navigation below matches the native local-task notification action.
      if(item.hostId&&item.hostId!=='local')continue;
      const runtime=item.threadRuntimeStatus,flags=runtime?.activeFlags||[],requests=Array.isArray(item.requests)?item.requests:[];
      const turns=Array.isArray(item.turns)?item.turns:[];const turn=turns.length?turns[turns.length-1]:null;
      const waiting=flags.includes('waitingOnApproval')||flags.includes('waitingOnUserInput')||requests.some(r=>['item/tool/requestUserInput','item/tool/requestOptionPicker','item/commandExecution/requestApproval','item/fileChange/requestApproval','item/permissions/requestApproval','item/tool/requestMcpServerElicitation'].includes(r?.method));
      const running=runtime?.type==='active'||item.resumeState==='resuming'||turn?.status==='inProgress';
      const failed=runtime?.type==='systemError'||turn?.status==='failed';
      const state=waiting?'waiting':failed?'failed':running?'running':item.hasUnreadTurn===true?'review':'idle';
      const stamp=Number(item.updatedAt)||0,previous=rows.get(item.id);
      if(!previous||stamp>=previous.updatedAt)rows.set(item.id,{id:item.id,state,title:typeof item.title==='string'?item.title.slice(0,100):'',updatedAt:stamp});
     }
    }
   }
  }
 }
 const order={waiting:0,failed:1,running:2,review:3};
 const notices=[...rows.values()].filter(r=>r.state!=='idle').sort((a,b)=>order[a.state]-order[b.state]||b.updatedAt-a.updatedAt).slice(0,100);
 return JSON.stringify({ready,notices});
})()
