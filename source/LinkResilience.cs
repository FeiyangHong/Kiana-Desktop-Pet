using System;using System.Linq;
namespace KianaPet {
 public static class LinkResilience {
  public static bool Transient(string code){return new[]{"timeout","temporary-error","mini-preparing","mini-missing","interface-changed","mini-unsynced","endpoint-unavailable","session-stale"}.Contains(code);}
  public static BridgeState KeepLast(BridgeState next,BridgeState last,DateTime now,int seconds){
   if(next.Connected||last==null||!next.AppOpen||!Transient(next.Code)||now-last.At>TimeSpan.FromSeconds(seconds)||last.NotificationCount<=0)return next;
   next.Stale=true;next.NotificationSource=last.NotificationSource;next.State=last.State;next.NotificationCount=last.NotificationCount;next.NotificationText=last.NotificationText;next.NotificationColor="#8b8790";next.Notices=last.Notices;next.LastSuccessAt=last.At;
   next.CanNotifications=true;next.CanChat=next.CanVoice=false;next.Label="正在重连 · 保留上次通知";next.Hint="当前状态暂未确认；上次同步 "+last.At.ToLocalTime().ToString("HH:mm:ss")+"。最多保留 "+seconds+" 秒，恢复后以实际状态为准。";return next;
  }
 }
 public sealed partial class Bridge {
  BridgeState lastGood;public int StaleGraceSeconds=30;
  void PublishState(BridgeState next){next.At=DateTime.UtcNow;TrackCapabilities(next);if(next.Connected&&!next.Stale)lastGood=next;else if(next.Code=="disabled"||next.Code=="app-closed"||next.Code=="endpoint-rejected"){lastGood=null;}
   Current=LinkResilience.KeepLast(next,lastGood,DateTime.UtcNow,StaleGraceSeconds);
  }
 }
}
