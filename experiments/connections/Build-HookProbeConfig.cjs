'use strict';
const fs = require('node:fs');
const path = require('node:path');
const repo = path.resolve(__dirname,'../..');
const root = path.join(repo,'.local','connection-probes','hook-candidate');
const output = path.join(repo,'.local','connection-probes','hook-events');
const receiver = path.join(__dirname,'HookReceiver.cjs');
const quote = value => {if(/["%\r\n]/.test(value))throw new Error('Unsupported command path');return '"'+value+'"';};
// An unquoted executable name works in both cmd and PowerShell on Windows;
// a quoted first token alone is only a string expression in PowerShell.
const executable = process.platform==='win32'?'node.exe':quote(process.execPath);
const command = executable+' '+quote(receiver)+' --output-dir '+quote(output);
const hooks = {};
for(const event of ['SessionStart','UserPromptSubmit','PermissionRequest','Stop','Interrupt','SessionEnd']){
 const handler = {type:'command',command,commandWindows:command,timeout:event==='SessionEnd'||event==='Interrupt'?1:2};
 if(event!=='SessionEnd')handler.async=true;
 hooks[event]=[{hooks:[handler]}];
}
fs.mkdirSync(root,{recursive:true});
const config = path.join(root,'hooks.json');
fs.writeFileSync(config,JSON.stringify({description:'Experimental Kiana metadata-only connection; does not replace notify or control applications.',hooks},null,2)+'\n');
console.log(JSON.stringify({config,output,installed:false,requiresNativeHookTrust:true}));
