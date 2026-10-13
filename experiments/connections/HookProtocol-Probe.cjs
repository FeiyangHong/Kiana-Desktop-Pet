'use strict';
// Checks candidate hooks using a separate CLI process. --start-session creates one
// ephemeral empty session; never resumes chats, trusts hooks, or sends model requests.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const {spawn}=require('node:child_process');const readline=require('node:readline');
const index=process.argv.indexOf('--codex'),codex=index>=0&&process.argv[index+1];
if(!codex||!path.isAbsolute(codex))throw new Error('--codex requires an absolute CLI path');
const repo=path.resolve(__dirname,'../..'),root=path.join(repo,'.local','connection-probes');
const config=JSON.parse(fs.readFileSync(path.join(root,'hook-candidate','hooks.json'),'utf8'));
const installed=process.argv.includes('--installed');
const interruptTurn=process.argv.includes('--interrupt');
const runTurn=process.argv.includes('--turn')||interruptTurn;
const startSession=process.argv.includes('--start-session')||runTurn;
const args=['app-server','--listen','stdio://'];
// Keep unrelated integrations out of this isolated protocol process.
const userToml=fs.readFileSync(path.join(process.env.USERPROFILE,'.codex','config.toml'),'utf8');
for(const match of userToml.matchAll(/^\[(plugins|mcp_servers)\."?([A-Za-z0-9_@-]+)"?\]\s*$/gm))args.push('-c',`${match[1]}.${match[2]}.enabled=false`);
args.push('-c','notify=[]');
for(const [event,groups] of Object.entries(installed?{}:config.hooks)){
 const h=groups[0].hooks[0];
 const fields=[`type="command"`,`command=${JSON.stringify(h.command)}`,`timeout=${h.timeout}`];
 if(h.async)fields.push('async=true');
 args.push('-c',`hooks.${event}=[{hooks=[{${fields.join(',')}}]}]`);
}
const userConfig=path.join(process.env.USERPROFILE,'.codex','config.toml');
const hash=()=>crypto.createHash('sha256').update(fs.readFileSync(userConfig)).digest('hex');
const originalHash=hash();
const child=spawn(codex,args,{cwd:repo,windowsHide:true,stdio:['pipe','pipe','pipe']});
const pending=new Map();let id=0,stderr='',closed=false;
const hookNotifications=[];
const completedTurns=[];
child.stderr.on('data',x=>{if(stderr.length<20000)stderr+=x;});
child.on('exit',()=>{closed=true;for(const p of pending.values())p.reject(new Error('Probe app-server exited'));pending.clear();});
const lines=readline.createInterface({input:child.stdout});
lines.on('line',line=>{try{const msg=JSON.parse(line),p=pending.get(msg.id);if(msg.method&&msg.method.startsWith('hook/'))hookNotifications.push(msg);if(msg.method==='turn/completed')completedTurns.push(msg.params);if(p){pending.delete(msg.id);clearTimeout(p.timer);if(msg.error)p.reject(new Error(JSON.stringify(msg.error)));else p.resolve(msg.result);}}catch{}});
function request(method,params){return new Promise((resolve,reject)=>{const key=++id;const timer=setTimeout(()=>{pending.delete(key);reject(new Error('RPC timed out: '+method));},15000);pending.set(key,{resolve,reject,timer});child.stdin.write(JSON.stringify({id:key,method,params})+'\n');});}
async function main(){let report;
 try{
  const init=await request('initialize',{clientInfo:{name:'kiana-connection-probe',title:'Kiana read-only hook configuration probe',version:'0.1.0'},capabilities:{experimentalApi:true}});
  child.stdin.write(JSON.stringify({method:'initialized'})+'\n');
  const result=await request('hooks/list',{cwds:[repo]});
  const hooks=result.data.flatMap(e=>e.hooks),events=hooks.filter(h=>h.command&&h.command.includes('HookReceiver.cjs'));
  let ephemeralThreadId=null;
  if(startSession){const thread=await request('thread/start',{cwd:repo,ephemeral:true,approvalPolicy:'never',sandbox:'read-only'});ephemeralThreadId=thread.thread.id;await new Promise(resolve=>setTimeout(resolve,2500));}
  if(runTurn){const turn=await request('turn/start',{threadId:ephemeralThreadId,input:[{type:'text',text:'Reply with exactly HOOK_PROBE_OK. This is an authorized local connection test. Do not use tools, read or change files, or control applications.'}]});if(interruptTurn){await request('turn/interrupt',{threadId:ephemeralThreadId,turnId:turn.turn.id});}const until=Date.now()+90000;while(!completedTurns.some(t=>t.threadId===ephemeralThreadId)&&Date.now()<until)await new Promise(resolve=>setTimeout(resolve,100));if(!completedTurns.some(t=>t.threadId===ephemeralThreadId))throw new Error('Test turn completion timed out');await new Promise(resolve=>setTimeout(resolve,3500));}
  report={schema:1,probe:'codex-hook-protocol',mode:installed?'installed-user-hooks':'candidate-only',server:init,parsedCandidateHooks:events.length,hooks:events.map(h=>({event:h.eventName,trust:h.trustStatus,enabled:h.enabled,source:h.source,async:h.async===true,timeoutSeconds:h.timeoutSec})),errors:result.data.flatMap(e=>e.errors),warnings:result.data.flatMap(e=>e.warnings),realHookDeliveryVerified:false,desktopConfigurationChanged:hash()!==originalHash,modelRequests:0,chatsStarted:0};
  if(events.length!==6||report.errors.length||report.desktopConfigurationChanged)process.exitCode=1;
  report.ephemeralThreadId=ephemeralThreadId;report.chatsStarted=startSession?1:0;report.hookNotifications=hookNotifications;
  report.modelRequests=runTurn?1:0;report.completedTurns=completedTurns.map(t=>({threadId:t.threadId,turnId:t.turn.id,status:t.turn.status}));
 }catch(e){report={probe:'codex-hook-protocol',error:e.message,stderr,realHookDeliveryVerified:false,desktopConfigurationChanged:hash()!==originalHash};process.exitCode=1;}
 finally{
  child.stdin.end();await Promise.race([new Promise(resolve=>{if(closed)resolve();else child.once('exit',resolve);}),new Promise(resolve=>setTimeout(resolve,2000))]);if(!closed)child.kill();lines.close();
  const eventDir=path.join(root,'hook-events');if(report.ephemeralThreadId&&fs.existsSync(eventDir)){report.deliveredEvents=fs.readdirSync(eventDir).filter(f=>f.endsWith('.json')).map(f=>JSON.parse(fs.readFileSync(path.join(eventDir,f),'utf8'))).filter(e=>e.sessionId===report.ephemeralThreadId);report.realHookDeliveryVerified=report.deliveredEvents.length>0;}
  if(runTurn&&report.completedTurns){const expected=interruptTurn?'Interrupt':'Stop';report.requiredEventsVerified=(report.deliveredEvents||[]).some(e=>e.event===expected)&&(report.deliveredEvents||[]).some(e=>e.event==='UserPromptSubmit');if(!report.requiredEventsVerified)process.exitCode=1;}
  fs.writeFileSync(path.join(root,interruptTurn?'hook-interrupt-result.json':runTurn?'hook-live-result.json':installed?'hook-installed-protocol-result.json':'hook-protocol-result.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
 }
}
main().catch(e=>{console.error(e);child.kill();process.exitCode=1;});
