using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
namespace KianaPet {
 public sealed class BridgeState {public bool Stale,MainReady;public string MiniStatus="unknown",MainStatus="unknown";public string NotificationSource="mini";[ScriptIgnore]public BridgeNotice[] Notices=new BridgeNotice[0];public string Code="connecting",Hint="正在检测连接。";public DateTime? LastSuccessAt,LastChatAt,LastVoiceAt,LastNotificationsAt;public bool AppOpen; public bool Connected;public string State="idle";public string Label="等待连接";public bool CanChat,CanVoice,CanNotifications;public int NotificationCount;public string NotificationText="",NotificationColor="";public DateTime At=DateTime.UtcNow;}
 public sealed partial class Bridge:IDisposable {
  readonly CancellationTokenSource shutdown=new CancellationTokenSource();
  readonly HttpClient http=new HttpClient(new HttpClientHandler{UseProxy=false,AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(2)};
  public volatile BridgeState Current=new BridgeState();
  string lastError="";DateTime? lastSuccessAt,noStateSince;
  public void RequestReconnect(){lastOpenAttempt=DateTime.MinValue;noStateSince=null;}
  bool backgroundMini,leaseActive;DateTime lastOpenAttempt=DateTime.MinValue;
  readonly SemaphoreSlim access=new SemaphoreSlim(1,1);
  static readonly string RestoreMini="window.__kianaBackgroundMiniV1?.restore();";
  static string BackgroundScript(){return File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","background-mini.js"));}
  static Dictionary<string,object> Obj(object o){return o as Dictionary<string,object>;}
  static object Get(object o,string k){Dictionary<string,object>d=Obj(o);return d!=null&&d.ContainsKey(k)?d[k]:null;}
  static string Str(object o,string k){return Convert.ToString(Get(o,k));}
  static object Parse(string s){return new JavaScriptSerializer{MaxJsonLength=1024*1024}.DeserializeObject(s);}
  async Task<object> Json(string url,CancellationToken cancel){using(HttpResponseMessage r=await http.GetAsync(url,cancel).ConfigureAwait(false)){r.EnsureSuccessStatusCode();if(r.Content.Headers.ContentLength>1024*1024)throw new Exception("Response too large");return Parse(await r.Content.ReadAsStringAsync().ConfigureAwait(false));}}
  public async Task Poll(bool enabled,bool background=false){if(shutdown.IsCancellationRequested)return;BridgeState next=new BridgeState{LastSuccessAt=lastSuccessAt};try{
   backgroundMini=enabled&&background;
   next.AppOpen=System.Diagnostics.Process.GetProcessesByName("ChatGPT").Length>0;
   if(!enabled){if(leaseActive){try{await Evaluate(RestoreMini+"'restored'").ConfigureAwait(false);}catch{}leaseActive=false;}BridgeDiagnostics.Set(next,"disabled","联动已关闭","开启读取 ChatGPT Mini 状态后自动连接。");PublishState(next);return;}
   if(!next.AppOpen){noStateSince=null;BridgeDiagnostics.Set(next,"app-closed","ChatGPT 未运行 · 独立陪伴","需要联动时，使用平滑启动器打开 ChatGPT。");PublishState(next);return;}
   string presentation=backgroundMini?BackgroundScript():leaseActive?RestoreMini:"";
   object data=Parse(await Evaluate(presentation+InspectExpression,backgroundMini).ConfigureAwait(false));leaseActive=backgroundMini;object[] states=Get(data,"states") as object[];if(states==null)throw new Exception("No states");
   string[] priority={"waiting","failed","review","running","waving","jumping","idle","running-left","running-right"};next.State=priority.FirstOrDefault(v=>states.Any(x=>Convert.ToString(x)==v))??"idle";next.Connected=states.Length>0;next.MiniStatus=next.Connected?"ready":"preparing";
   next.CanChat=Convert.ToBoolean(Get(data,"chat"));next.CanVoice=Convert.ToBoolean(Get(data,"voice"));next.CanNotifications=Convert.ToBoolean(Get(data,"notifications"));next.NotificationCount=Convert.ToInt32(Get(data,"count"));next.NotificationText=Str(data,"countText");next.NotificationColor=Str(data,"badgeColor");
   next.Label=next.Connected?(backgroundMini?"ChatGPT 已联动（Mini 后台） · ":"ChatGPT 已联动 · ")+Name(next.State):"接口已连接 · Mini 正在准备";lastError="";next.Code=next.Connected?"connected":"mini-preparing";next.Hint=next.Connected?"每两秒自动同步；断线后自动重试。":"Mini 正在加载；若持续出现，可手动显示 Mini 后重新检测。";if(next.Connected){noStateSince=null;lastSuccessAt=DateTime.UtcNow;next.LastSuccessAt=lastSuccessAt;}else{if(!noStateSince.HasValue)noStateSince=DateTime.UtcNow;if(DateTime.UtcNow-noStateSince.Value>TimeSpan.FromSeconds(15))BridgeDiagnostics.Set(next,"interface-changed","Mini 长时间未就绪","请重新检测；若仍无状态，可能需要适配新的 ChatGPT 版本。");}
  }catch(OperationCanceledException error){BridgeDiagnostics.Failure(next,error);}catch(Exception error){if(error.Message!=lastError){lastError=error.Message;Store.Log("Bridge: "+error.Message);}BridgeDiagnostics.Failure(next,error);}
  if(enabled&&next.AppOpen){
   try{MergeMainNotifications(next,await Evaluate(AssetScript("main-notification-state.js"),false,false,true).ConfigureAwait(false));
    if(next.NotificationSource=="main"){next.Connected=true;next.Code="fallback";if(next.MiniStatus=="preparing")next.MiniStatus="unsynced";lastSuccessAt=DateTime.UtcNow;next.LastSuccessAt=lastSuccessAt;}}
   catch(Exception){next.MainStatus="unavailable";/* Keep genuine Mini results. */}
  }
  PublishState(next);
  }
  // Only toolbar metadata and pet states are read. Actions run exclusively after an explicit toolbar click.
  public static readonly string InspectExpression=@"(()=>{const buttons=Array.from(document.querySelectorAll('button'));const usable=e=>e&&!e.disabled&&e.getAttribute('aria-disabled')!=='true';const chat=buttons.find(e=>['开始新聊天','Start new chat','New chat'].includes(e.getAttribute('aria-label')));const voice=buttons.find(e=>['开始语音聊天','Start voice chat'].includes(e.getAttribute('aria-label')));const notifications=buttons.find(e=>e.closest('[data-avatar-overlay-native-surface-id=""mascot-badge""]')||e.getAttribute('data-testid')==='avatar-overlay-notification-badge');
const countPattern=/^\d{1,3}\+?$/;let countText='',badgeColor='';
const rgb=e=>{const value=getComputedStyle(e).backgroundColor;const m=/^rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)(?:\s*,\s*([\d.]+))?\s*\)$/.exec(value);if(!m||(m[4]!==undefined&&Number(m[4])<.1))return '';return '#'+m.slice(1,4).map(v=>Math.min(255,Number(v)).toString(16).padStart(2,'0')).join('');};
if(notifications){const candidates=[...notifications.querySelectorAll('span'),notifications];for(const e of candidates){const value=(e.textContent||'').trim();if(!countPattern.test(value))continue;const color=rgb(e);if(!countText)countText=value;if(color){countText=value;badgeColor=color;break;}}}
 const nativeMini=!!document.querySelector('[data-avatar-overlay-native-surface-id=""composer""]')&&!!document.querySelector('[data-avatar-overlay-native-surface-id=""voice-controls""]');
 const activities=Array.from(document.querySelectorAll('[data-avatar-overlay-activity-stack-item]'));
 const states=Array.from(document.querySelectorAll('[data-codex-pet-state]'),e=>e.getAttribute('data-codex-pet-state'));
 if(!states.length&&nativeMini){for(const item of activities){const icon=item.querySelector('[data-avatar-overlay-status]');const kind=icon?.getAttribute('data-avatar-overlay-status');states.push(kind==='spinner'?'running':kind==='check-circle'?'review':kind==='warning'?(icon.classList.contains('text-danger')?'failed':'waiting'):'idle');}if(!states.length)states.push('idle');}
 let count=countText?parseInt(countText,10):0;
 if(!count&&nativeMini&&activities.length){count=activities.length;countText=count>99?'99+':String(count);}
 return JSON.stringify({states:states,chat:!!usable(chat)||!!document.querySelector(""[data-avatar-overlay-native-surface-id=\""composer\""] [contenteditable=true], [data-avatar-overlay-native-surface-id=\""composer\""] textarea""),voice:!!usable(voice)||(!!window.__kianaBackgroundMiniV1&&Array.from(document.querySelectorAll(""[data-avatar-overlay-native-surface-id=\""voice-controls\""] button"")).some(usable)),notifications:(!!usable(notifications)||nativeMini)&&count>0,count:count,countText:countText,badgeColor:badgeColor});})()";
   public static string ActionExpression(string action){if(action!="chat"&&action!="voice"&&action!="notifications")throw new ArgumentException("Unknown toolbar action");return @"(()=>{const buttons=Array.from(document.querySelectorAll('button'));const usable=e=>e&&!e.disabled&&e.getAttribute('aria-disabled')!=='true';const chat=buttons.find(e=>['开始新聊天','Start new chat','New chat'].includes(e.getAttribute('aria-label')));const voice=buttons.find(e=>['开始语音聊天','Start voice chat'].includes(e.getAttribute('aria-label')));const notifications=buttons.find(e=>e.closest('[data-avatar-overlay-native-surface-id=""mascot-badge""]')||e.getAttribute('data-testid')==='avatar-overlay-notification-badge');const actionName="""+action+@""";const selected="+action+@";if(actionName===""notifications""&&document.querySelector('[data-avatar-overlay-activity-stack-item]')){window.__kianaBackgroundMiniV1?.reveal();if(usable(selected)&&['显示活动','Show activity'].includes(selected.getAttribute('aria-label')))selected?.click();return ""opened"";}if(!usable(selected)){const editor=document.querySelector(""[data-avatar-overlay-native-surface-id=\""composer\""] [contenteditable=true], [data-avatar-overlay-native-surface-id=\""composer\""] textarea"");if(actionName===""chat""&&editor&&window.__kianaBackgroundMiniV1){window.__kianaBackgroundMiniV1.reveal();editor.focus();return ""opened"";}if(actionName===""voice""&&window.__kianaBackgroundMiniV1&&Array.from(document.querySelectorAll(""[data-avatar-overlay-native-surface-id=\""voice-controls\""] button"")).some(usable)){window.__kianaBackgroundMiniV1.reveal();return ""opened"";}return ""unavailable"";}selected.click();return ""opened"";})()";}
  public async Task<bool> InvokeToolbar(string action){string command=ActionExpression(action);if(backgroundMini)command=BackgroundScript()+command.Replace("selected.click();","window.__kianaBackgroundMiniV1?.reveal();selected.click();");string result=await Evaluate(command,backgroundMini,true).ConfigureAwait(false);return result=="opened";}
  async Task<string> Evaluate(string expression,bool ensureMini=false,bool userGesture=false,bool mainOnly=false){await access.WaitAsync(shutdown.Token).ConfigureAwait(false);try{return await EvaluateCore(expression,ensureMini,userGesture,mainOnly).ConfigureAwait(false);}finally{access.Release();}}
  static bool MiniTarget(object target){return Str(target,"type")=="page"&&Str(target,"url").StartsWith("app://",StringComparison.Ordinal)&&Uri.UnescapeDataString(Str(target,"url")).Contains("/avatar-overlay");}
  async Task<string> EvaluateCore(string expression,bool ensureMini,bool userGesture,bool mainOnly){
   string root=CompanionPaths.SmoothRoot();string file=Path.Combine(root,"session.json");
   if(!File.Exists(file))throw new Exception("请先使用 ChatGPT 平滑桌宠启动器。");
   object saved=Parse(File.ReadAllText(file));int port=Convert.ToInt32(Get(saved,"port"));string browser=Str(saved,"browserId"),app=Str(saved,"app");
   if(port<1024||port>65535||!System.Text.RegularExpressions.Regex.IsMatch(browser,"^[A-Za-z0-9._-]{1,200}$")||!Native.TrustedPort(port,app))throw new Exception("Endpoint unavailable: "+Native.PortCheck);
   string origin="http://127.0.0.1:"+port;object version=await Json(origin+"/json/version",shutdown.Token).ConfigureAwait(false);
   Uri browserUrl=new Uri(Str(version,"webSocketDebuggerUrl"));if(!ValidWs(browserUrl,port)||browserUrl.AbsolutePath!="/devtools/browser/"+browser)throw new Exception("Browser changed");
   object[] targets=await Json(origin+"/json/list",shutdown.Token).ConfigureAwait(false) as object[];
   if(targets==null)throw new Exception("No target list");object target=targets.FirstOrDefault(MiniTarget);
   if(mainOnly){object mainPage=targets.FirstOrDefault(MainTarget);if(mainPage==null)throw new Exception("Main page unavailable");return await EvaluateTarget(mainPage,port,app,expression,userGesture).ConfigureAwait(false);}
   if(target==null&&ensureMini&&DateTime.UtcNow-lastOpenAttempt>TimeSpan.FromSeconds(15)){
    object main=targets.FirstOrDefault(MainTarget);
    if(main!=null){lastOpenAttempt=DateTime.UtcNow;await EvaluateTarget(main,port,app,"(async()=>{if(!window.electronBridge?.sendMessageFromView)return 'unavailable';await window.electronBridge.sendMessageFromView({type:'avatar-overlay-open'});return 'requested';})()",false).ConfigureAwait(false);
     for(int i=0;i<4&&target==null;i++){await Task.Delay(250,shutdown.Token).ConfigureAwait(false);targets=await Json(origin+"/json/list",shutdown.Token).ConfigureAwait(false) as object[];if(targets!=null)target=targets.FirstOrDefault(MiniTarget);}
    }
   }
   if(target==null)throw new Exception(ensureMini?"Mini 后台未就绪 · 请在 ChatGPT 设置中显示 Mini 一次":"请显示 ChatGPT Mini 宠物，或开启 Mini 后台模式。");
   // Park before opening so an existing Mini does not add a second visible pet.
   if(ensureMini)await EvaluateTarget(target,port,app,BackgroundScript()+"'parked'",false).ConfigureAwait(false);
   string nativeState=await EvaluateTarget(target,port,app,AssetScript("mini-open-state.js"),false).ConfigureAwait(false);
   if(nativeState=="closed"&&ensureMini&&DateTime.UtcNow-lastOpenAttempt>TimeSpan.FromSeconds(15)){
    object mainPage=targets.FirstOrDefault(MainTarget);if(mainPage!=null){lastOpenAttempt=DateTime.UtcNow;await EvaluateTarget(mainPage,port,app,OpenMini,false).ConfigureAwait(false);await Task.Delay(250,shutdown.Token).ConfigureAwait(false);nativeState=await EvaluateTarget(target,port,app,AssetScript("mini-open-state.js"),false).ConfigureAwait(false);}
   }
   if(nativeState=="closed")throw new Exception("Mini 后台窗口已关闭");
   if(nativeState!="open")throw new Exception("Mini 状态查询超时");
   return await EvaluateTarget(target,port,app,expression,userGesture).ConfigureAwait(false);
  }
  async Task<string> EvaluateTarget(object target,int port,string app,string expression,bool userGesture){
   Uri ws=new Uri(Str(target,"webSocketDebuggerUrl"));string id=Str(target,"id");if(!ValidWs(ws,port)||!System.Text.RegularExpressions.Regex.IsMatch(id,"^[A-Za-z0-9._-]{1,200}$")||ws.AbsolutePath!="/devtools/page/"+id||!Native.TrustedPort(port,app))throw new Exception("Untrusted page");
   using(CancellationTokenSource timeout=CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token)){
    timeout.CancelAfter(2500);using(ClientWebSocket socket=new ClientWebSocket()){
     socket.Options.Proxy=null;await socket.ConnectAsync(ws,timeout.Token).ConfigureAwait(false);
     string request=new JavaScriptSerializer().Serialize(new {id=1,method="Runtime.evaluate",@params=new{expression=expression,returnByValue=true,awaitPromise=true,userGesture=userGesture}});
     byte[] bytes=Encoding.UTF8.GetBytes(request);await socket.SendAsync(new ArraySegment<byte>(bytes),WebSocketMessageType.Text,true,timeout.Token).ConfigureAwait(false);
     while(true){MemoryStream message=new MemoryStream();WebSocketReceiveResult result;byte[] buffer=new byte[4096];do{result=await socket.ReceiveAsync(new ArraySegment<byte>(buffer),timeout.Token).ConfigureAwait(false);if(result.MessageType==WebSocketMessageType.Close)throw new Exception("Closed");message.Write(buffer,0,result.Count);if(message.Length>65536)throw new Exception("Oversized message");}while(!result.EndOfMessage);
      object response=Parse(Encoding.UTF8.GetString(message.ToArray()));if(Convert.ToString(Get(response,"id"))!="1")continue;
      if(Get(response,"error")!=null||Get(Get(response,"result"),"exceptionDetails")!=null)throw new Exception("Mini 接口执行失败");
      string raw=Str(Get(Get(response,"result"),"result"),"value");socket.Abort();return raw;
     }
    }
   }
  }
  public static bool ValidWs(Uri uri,int port){return uri.Scheme=="ws"&&uri.Host=="127.0.0.1"&&uri.Port==port&&string.IsNullOrEmpty(uri.UserInfo)&&string.IsNullOrEmpty(uri.Query)&&string.IsNullOrEmpty(uri.Fragment);}
  public static string Name(string state){switch(state){case"running":return "思考中 / 处理中";case"waiting":return "等待你回应";case"review":return "任务完成";case"failed":return "任务出现错误";case"waving":return "打招呼";default:return "陪伴中";}}
  public void Dispose(){shutdown.Cancel();http.Dispose();}
 }
}
