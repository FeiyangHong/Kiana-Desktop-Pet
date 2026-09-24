using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Microsoft.Win32;
namespace KianaPet {
 public sealed class MusicState {
  public bool Connected,Playing,CanToggle,CanPrevious,CanNext;
  public string Title="",Artist="",Id="",Cover="",Label="等待网易云音乐连接";
  public double Position,Duration;public DateTime At=DateTime.UtcNow;
 }
 public sealed class MusicBridge:IDisposable {
  public const int Port=9436;
  public volatile MusicState Current=new MusicState();
  readonly HttpClient http=new HttpClient(new HttpClientHandler{UseProxy=false,AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(2)};
  readonly CancellationTokenSource shutdown=new CancellationTokenSource();
  public static object Get(object o,string k){var d=o as Dictionary<string,object>;return d!=null&&d.ContainsKey(k)?d[k]:null;}
  public static string Str(object o,string k){return Convert.ToString(Get(o,k));}
  static object Parse(string text){return new JavaScriptSerializer{MaxJsonLength=1024*1024}.DeserializeObject(text);}
  public static string FindApp(){
   foreach(var root in new[]{Registry.CurrentUser,Registry.LocalMachine})foreach(string key in new[]{@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"}){
    using(var list=root.OpenSubKey(key)){if(list==null)continue;foreach(string name in list.GetSubKeyNames())using(var item=list.OpenSubKey(name)){if(item==null)continue;string display=Convert.ToString(item.GetValue("DisplayName"));if(display!="网易云音乐"&&!display.Equals("CloudMusic",StringComparison.OrdinalIgnoreCase))continue;
     string icon=Convert.ToString(item.GetValue("DisplayIcon")).Trim('"');int end=icon.IndexOf(".exe",StringComparison.OrdinalIgnoreCase);if(end>=0)icon=icon.Substring(0,end+4);if(File.Exists(icon)&&Path.GetFileName(icon).Equals("cloudmusic.exe",StringComparison.OrdinalIgnoreCase))return icon;
    }}
   }return "";
  }
  public async Task Poll(bool enabled){var next=new MusicState();if(!enabled){next.Label="音乐栏已关闭";Current=next;return;}try{
   var d=Parse(await Evaluate(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","music-read.js")),false).ConfigureAwait(false));
   next.Connected=Convert.ToBoolean(Get(d,"connected"));next.Title=Str(d,"title");next.Artist=Str(d,"artist");next.Id=Str(d,"id");next.Cover=Str(d,"cover");next.Playing=Convert.ToBoolean(Get(d,"playing"));next.CanToggle=Convert.ToBoolean(Get(d,"toggle"));next.CanPrevious=Convert.ToBoolean(Get(d,"previous"));next.CanNext=Convert.ToBoolean(Get(d,"next"));next.Position=Convert.ToDouble(Get(d,"position"));next.Duration=Convert.ToDouble(Get(d,"duration"));next.At=DateTime.UtcNow;
   next.Label=next.Connected?"网易云音乐已连接 · "+(next.Playing?"正在播放":"已暂停"):"已连接客户端 · 请先选择歌曲";
  }catch(Exception){next.Label="请使用“网易云音乐 桌宠联动”入口打开客户端";}Current=next;}
  public async Task<bool> Command(string action){if(action!="toggle"&&action!="previous"&&action!="next")return false;if(!Current.Connected||DateTime.UtcNow-Current.At>TimeSpan.FromSeconds(5))return false;
   string code=File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","music-action.js"));try{return await Evaluate("("+code+")("+new JavaScriptSerializer().Serialize(action)+")",true).ConfigureAwait(false)=="clicked";}catch{return false;}
  }
  async Task<string> Evaluate(string expression,bool action){
   string app=FindApp();if(app.Length==0||!Native.TrustedMusicPort(Port,app))throw new Exception("Music endpoint unavailable");
   string origin="http://127.0.0.1:"+Port;string list=await http.GetStringAsync(origin+"/json/list").ConfigureAwait(false);if(list.Length>1024*1024)throw new Exception("Large target list");
   var targets=Parse(list) as object[];var target=targets==null?null:targets.FirstOrDefault(t=>Str(t,"type")=="page"&&Str(t,"url")=="orpheus://orpheus/pub/app.html");if(target==null)throw new Exception("Player not ready");
   Uri ws=new Uri(Str(target,"webSocketDebuggerUrl"));string id=Str(target,"id");if(!Bridge.ValidWs(ws,Port)||!System.Text.RegularExpressions.Regex.IsMatch(id,"^[A-Za-z0-9._-]{1,200}$")||ws.AbsolutePath!="/devtools/page/"+id||!Native.TrustedMusicPort(Port,app))throw new Exception("Invalid player endpoint");
   using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token)){timeout.CancelAfter(2000);using(var socket=new ClientWebSocket()){socket.Options.Proxy=null;await socket.ConnectAsync(ws,timeout.Token).ConfigureAwait(false);
    byte[] request=Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(new{id=1,method="Runtime.evaluate",@params=new{expression=expression,returnByValue=true,userGesture=action}}));await socket.SendAsync(new ArraySegment<byte>(request),WebSocketMessageType.Text,true,timeout.Token).ConfigureAwait(false);
    while(true){using(var message=new MemoryStream()){byte[] buffer=new byte[4096];WebSocketReceiveResult r;do{r=await socket.ReceiveAsync(new ArraySegment<byte>(buffer),timeout.Token).ConfigureAwait(false);if(r.MessageType==WebSocketMessageType.Close)throw new Exception("Closed");message.Write(buffer,0,r.Count);if(message.Length>65536)throw new Exception("Large response");}while(!r.EndOfMessage);
     var response=Parse(Encoding.UTF8.GetString(message.ToArray()));if(Convert.ToString(Get(response,"id"))!="1")continue;if(Get(response,"error")!=null||Get(Get(response,"result"),"exceptionDetails")!=null)throw new Exception("Player UI unavailable");string result=Str(Get(Get(response,"result"),"result"),"value");socket.Abort();return result;
    }}
   }}
  }
  public void Dispose(){shutdown.Cancel();http.Dispose();}
 }
}
