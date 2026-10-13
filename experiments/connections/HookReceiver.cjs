'use strict';
// Experimental metadata-only receiver. No production settings or app control.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const statuses = Object.freeze({SessionStart:'session-started',UserPromptSubmit:'running',PermissionRequest:'permission-requested',Stop:'turn-ended',Interrupt:'interrupted',SessionEnd:'session-ended'});
const index = process.argv.indexOf('--output-dir');
const output = index >= 0 && process.argv[index + 1];
let input = '', exceeded = false;
process.stdin.setEncoding('utf8');
process.stdin.on('data', chunk => {if(input.length + chunk.length > 65536){exceeded = true;input = '';}else if(!exceeded)input += chunk;});
process.stdin.on('error', () => {});
process.stdin.on('end', () => {
 try {
  if(!output || !path.isAbsolute(output) || exceeded)return;
  fs.mkdirSync(output,{recursive:true});
  // Delivery marker contains no payload and distinguishes absent callbacks from filtering.
  fs.writeFileSync(path.join(output,crypto.randomUUID()+'.delivery'),new Date().toISOString()+'\n',{flag:'wx'});
  const event = JSON.parse(input);
  const validId = value => typeof value === 'string' && /^[A-Za-z0-9._:-]{1,160}$/.test(value);
  if(!Object.hasOwn(statuses,event.hook_event_name) || !validId(event.session_id))return;
  // A Stop hook means the turn ended; it does not establish success or failure.
  const record = {schema:1,source:'codex-hooks-probe',receivedAt:new Date().toISOString(),sessionId:event.session_id,turnId:validId(event.turn_id)?event.turn_id:null,event:event.hook_event_name,status:statuses[event.hook_event_name]};
  fs.mkdirSync(output,{recursive:true});
  // Unique atomic records tolerate concurrent callbacks. Receipt time is not causal order.
  const base = path.join(output,`${Date.now()}-${crypto.randomUUID()}`);
  fs.writeFileSync(base+'.tmp',JSON.stringify(record)+'\n',{flag:'wx'});
  fs.renameSync(base+'.tmp',base+'.json');
 }catch{ /* Never block an application task because the pet receiver failed. */ }
});
