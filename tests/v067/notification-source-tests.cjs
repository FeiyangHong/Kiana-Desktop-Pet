const fs=require('fs'),vm=require('vm'),assert=require('assert'),path=require('path');
const root=path.resolve(__dirname,'../..'),script=fs.readFileSync(path.join(root,'assets/main-notification-state.js'),'utf8');
let rows=[],status='success';const query={queryKey:['recent-conversations-meta'],get state(){return {status,data:rows}}};
const client={getQueryCache:()=>({getAll:()=>[query,query]})};
const fiber={memoizedProps:{client},return:null};const element={__reactFiber$test:fiber};
const run=()=>JSON.parse(vm.runInNewContext(script,{document:{querySelectorAll:()=>[element,element]}}));
const item=(id,runtime,unread=false)=>({id,hostId:'local',title:'测试任务',threadRuntimeStatus:runtime,hasUnreadTurn:unread,updatedAt:1});
rows=[item('task-1',{type:'active',activeFlags:[]})];let v=run();assert(v.ready);assert.equal(v.notices.length,1);assert.equal(v.notices[0].state,'running');
rows=[item('task-1',{type:'idle'},true)];assert.equal(run().notices[0].state,'review');
rows=[item('task-1',{type:'idle'},false)];assert.equal(run().notices.length,0);
for(const flag of ['waitingOnApproval','waitingOnUserInput']){rows=[item('task-1',{type:'active',activeFlags:[flag]})];assert.equal(run().notices[0].state,'waiting');}
rows=[item('task-1',{type:'systemError'})];assert.equal(run().notices[0].state,'failed');
rows=[item('bad-id\"',{type:'active'}),{...item('remote',{type:'active'}),hostId:'other'}];assert.equal(run().notices.length,0);
rows=[item('task-1',{type:'active'})];Object.defineProperty(rows[0],'turns',{get:()=>[]});Object.defineProperty(rows[0],'prompt',{get:()=>{throw Error('Prompt must not be read');}});assert.equal(run().notices.length,1);
status='error';assert.equal(run().ready,false);assert.equal(run().notices.length,0);
const lifecycle=fs.readFileSync(path.join(root,'assets/mini-open-state.js'),'utf8');
async function health(value){let listener,timer,cleaned=0,requests=0;const result=vm.runInNewContext(lifecycle,{setTimeout:f=>(timer=f,1),clearTimeout:()=>{},window:{addEventListener:(_,f)=>listener=f,removeEventListener:()=>cleaned++,electronBridge:{sendMessageFromView:m=>{assert.equal(m.type,'avatar-overlay-open-state-request');requests++;}}}});if(value===null)timer();else listener({data:{type:'avatar-overlay-open-state-changed',isOpen:value}});assert.equal(await result,value===null?'unknown':value?'open':'closed');assert.equal(cleaned,1);assert.equal(requests,1);}
(async()=>{await health(true);await health(false);await health(null);console.log('PASS: running, unread completion, read clear, waiting, failure, deduplication, metadata boundaries, missing source, native open/closed/timeout and listener cleanup');})().catch(e=>{console.error(e);process.exitCode=1});
