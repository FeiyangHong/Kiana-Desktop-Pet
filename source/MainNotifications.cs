using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace KianaPet {
 public sealed class BridgeNotice {public string Id,State,Title;public DateTime UpdatedAt;}
 public sealed partial class Bridge {
  static DateTime NoticeTime(object value){double ms;return double.TryParse(Convert.ToString(value),out ms)&&ms>=0&&ms<253402300799000?new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddMilliseconds(ms):DateTime.MinValue;}
  static string AssetScript(string name){return File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets",name));}
  static bool MainTarget(object target){return Str(target,"type")=="page"&&Str(target,"url").StartsWith("app://",StringComparison.Ordinal)&&new Uri(Str(target,"url")).AbsolutePath=="/index.html"&&!MiniTarget(target);}
  static readonly string OpenMini="(async()=>{if(!window.electronBridge?.sendMessageFromView)return 'unavailable';await window.electronBridge.sendMessageFromView({type:'avatar-overlay-open'});return 'requested';})()";
  public static void MergeMainNotifications(BridgeState next,string json){
   object data=Parse(json);object[] rows=Get(data,"notices") as object[];
   if((Get(data,"ready") as bool?)!=true||rows==null){next.MainStatus="unavailable";return;}next.MainReady=true;next.MainStatus="ready";
   var notices=new List<BridgeNotice>();
   foreach(object row in rows.Take(100)){string id=Str(row,"id"),state=Str(row,"state");if(!Regex.IsMatch(id,"^[a-zA-Z0-9_-]{1,160}$")||!new[]{"running","waiting","failed","review"}.Contains(state))continue;notices.Add(new BridgeNotice{Id=id,State=state,Title=Str(row,"title"),UpdatedAt=NoticeTime(Get(row,"updatedAt"))});}
   next.Notices=notices.ToArray();if(notices.Count==0){if(!next.Connected){next.NotificationSource="main";next.State="idle";next.Label="备用状态 · 暂无待处理任务";next.Hint="主窗口状态可用，Mini 暂未就绪。可继续查看任务，并按需检查联动。";}return;}if(next.CanNotifications&&next.NotificationCount>0)return;
   next.Notices=notices.ToArray();next.NotificationSource="main";next.CanNotifications=true;next.NotificationCount=notices.Count;next.NotificationText=notices.Count>99?"99+":notices.Count.ToString();
   next.State=new[]{"waiting","failed","running","review"}.First(s=>notices.Any(n=>n.State==s));next.NotificationColor=next.State=="waiting"?"#ff9500":next.State=="failed"?"#ff453a":next.State=="running"?"#3a83f7":"#30c85a";
   next.Label="备用状态 · "+Name(next.State);next.Hint=next.MiniStatus=="ready"?"Mini 已连接；当前活动未出现在 Mini 通知中，暂用主窗口已加载的任务状态。":"Mini 没有同步到通知，当前使用主窗口已加载的任务状态；聊天和语音入口仍单独检测。";
  }
  public static string OpenMainNotificationExpression(string id){if(id==null||!Regex.IsMatch(id,"^[a-zA-Z0-9_-]{1,160}$"))throw new ArgumentException("Invalid notification task");return "(async()=>{if(!window.electronBridge?.sendMessageFromView)return 'unavailable';await window.electronBridge.sendMessageFromView({type:'open-in-main-window',path:'/local/"+id+"'});return 'opened';})()";}
  public Task<bool> OpenMainNotification(string id){return InvokeMainNotification(OpenMainNotificationExpression(id));}
  async Task<bool> InvokeMainNotification(string expression){return await Evaluate(expression,false,true,true).ConfigureAwait(false)=="opened";}
 }
}
