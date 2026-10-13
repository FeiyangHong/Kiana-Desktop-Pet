'use strict';
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const assert = require('node:assert/strict');
const {spawn,spawnSync} = require('node:child_process');
const root = path.resolve(__dirname,'../../.local/connection-probes','hook-tests-'+crypto.randomUUID());
const receiver = path.join(__dirname,'HookReceiver.cjs');
fs.mkdirSync(root,{recursive:true});
let checks=0;
function invoke(input,folder=root){const p=spawnSync(process.execPath,[receiver,'--output-dir',folder],{input,encoding:'utf8',windowsHide:true,timeout:4000});assert.equal(p.status,0);assert.equal(p.stdout,'');assert.equal(p.stderr,'');return p;}
const privateText='PRIVATE_PROMPT_MUST_NOT_BE_RECORDED';
for(const event of ['SessionStart','UserPromptSubmit','PermissionRequest','Stop','Interrupt','SessionEnd'])invoke(JSON.stringify({session_id:'fixture-session',turn_id:'fixture-turn',hook_event_name:event,prompt:privateText,tool_input:{secret:privateText},transcript_path:privateText}));
let records=fs.readdirSync(root).filter(f=>f.endsWith('.json')).map(f=>JSON.parse(fs.readFileSync(path.join(root,f),'utf8')));
assert.equal(records.length,6);checks++;
assert(records.every(r=>r.sessionId==='fixture-session'&&r.turnId==='fixture-turn'));checks++;
assert.equal(records.find(r=>r.event==='Stop').status,'turn-ended');checks++;
assert(!JSON.stringify(records).includes(privateText));checks++;
for(const input of ['{bad json',JSON.stringify({session_id:'../../escape',hook_event_name:'Stop'}),JSON.stringify({session_id:'fixture',hook_event_name:'Unknown'}),'x'.repeat(70000)])invoke(input);
assert.equal(fs.readdirSync(root).filter(f=>f.endsWith('.json')).length,6);checks++;
// A broken destination must also leave the originating task unaffected.
const blocked=path.join(root,'file-not-directory');fs.writeFileSync(blocked,'fixture');invoke(JSON.stringify({session_id:'fixture',hook_event_name:'Stop'}),blocked);checks++;
Promise.all(Array.from({length:8},(_,i)=>new Promise((resolve,reject)=>{const p=spawn(process.execPath,[receiver,'--output-dir',root],{windowsHide:true,stdio:['pipe','pipe','pipe']});let stdout='',stderr='';p.stdout.on('data',x=>stdout+=x);p.stderr.on('data',x=>stderr+=x);p.on('error',reject);p.on('exit',code=>{try{assert.equal(code,0);assert.equal(stdout+stderr,'');resolve();}catch(e){reject(e);}});p.stdin.end(JSON.stringify({session_id:'parallel-'+i,hook_event_name:'UserPromptSubmit'}));}))).then(()=>{
 assert.equal(fs.readdirSync(root).filter(f=>f.endsWith('.json')).length,14);assert(!fs.readdirSync(root).some(f=>f.endsWith('.tmp')));checks++;
 const report={passed:true,checks,realHookDeliveryVerified:false,output:root};fs.writeFileSync(path.join(root,'report.json'),JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify(report));
}).catch(e=>{console.error(e);process.exitCode=1;});
