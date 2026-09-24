using System;
namespace KianaPet {
 public sealed class LinkHealthSnapshot {
  public bool Chat,Voice,Notifications,Degraded;public string ChatText,VoiceText,NotificationText,Summary;
 }
 public static class LinkHealth {
  public static LinkHealthSnapshot Read(BridgeState s,bool enabled,DateTime now){
   bool fresh=enabled&&s.AppOpen&&s.Connected&&!s.Stale&&now-s.At<TimeSpan.FromSeconds(10);
   bool notifications=fresh&&(s.MainReady||s.MiniStatus=="ready");
   string unavailable=!enabled?"联动已关闭":!s.AppOpen?"应用未启动":s.Stale?"重连中":!fresh?"状态待确认":"入口暂不可用";
   var h=new LinkHealthSnapshot{Chat=fresh&&s.CanChat,Voice=fresh&&s.CanVoice,Notifications=notifications};
   h.ChatText=h.Chat?"入口可用":unavailable;h.VoiceText=h.Voice?"入口可用（实际通话仍需麦克风权限）":unavailable;
   h.NotificationText=notifications?(s.NotificationSource=="main"?"备用来源可用 · 主窗口已加载任务":"原生来源可用"):(s.Stale?"仅保留上次状态，尚未同步":unavailable);
   h.Degraded=enabled&&s.AppOpen&&s.Code!="connecting"&&(!h.Chat||!h.Voice||!h.Notifications||s.NotificationSource=="main");
   h.Summary="聊天："+h.ChatText+"\n语音："+h.VoiceText+"\n通知："+h.NotificationText+"\n最近有效同步："+Time(s.LastSuccessAt)+"\n聊天入口最后确认："+Time(s.LastChatAt)+"\n语音入口最后确认："+Time(s.LastVoiceAt)+"\n通知最后同步："+Time(s.LastNotificationsAt);
   return h;
  }
  public static string Time(DateTime? at){return at.HasValue?at.Value.ToLocalTime().ToString("MM-dd HH:mm:ss"):"尚无有效记录";}
 }
 public sealed class LinkIncidentTracker {
  DateTime? badSince,goodSince;bool notified;
  public bool Update(BridgeState state,bool enabled,DateTime now,bool canNotify){
   if(!enabled||!state.AppOpen){badSince=goodSince=null;notified=false;return false;}
   bool bad=LinkHealth.Read(state,enabled,now).Degraded;
   if(!bad){if(!goodSince.HasValue)goodSince=now;if(now-goodSince.Value>=TimeSpan.FromSeconds(20)){badSince=null;notified=false;}return false;}
   goodSince=null;if(!badSince.HasValue)badSince=now;
   if(notified||now-badSince.Value<TimeSpan.FromSeconds(30))return false;
   notified=true;return canNotify;
  }
 }
 public sealed partial class Bridge {
  DateTime? lastChatAt,lastVoiceAt,lastNotificationsAt;
  void TrackCapabilities(BridgeState next){if(next.Connected&&!next.Stale){if(next.CanChat)lastChatAt=next.At;if(next.CanVoice)lastVoiceAt=next.At;if(next.MainReady||next.MiniStatus=="ready")lastNotificationsAt=next.At;}
   next.LastChatAt=lastChatAt;next.LastVoiceAt=lastVoiceAt;next.LastNotificationsAt=lastNotificationsAt;
  }
 }
 public sealed partial class PetWindow {
  readonly LinkIncidentTracker linkIncidents=new LinkIncidentTracker();
  void CheckLinkHealth(){bool allow=!Settings.NoticeQuiet&&!quietActive&&!manualHidden&&!fullscreenHidden&&!sessionLocked&&!menuLayerYielding&&Settings.Bubbles;
   if(linkIncidents.Update(Link.Current,Settings.LinkChatGPT,DateTime.UtcNow,allow))Say(Link.Current.NotificationSource=="main"&&Link.Current.Connected?"通知正在使用备用来源，可在设置中查看联动详情。":"联动持续异常，可在设置中检查并修复。",6);
  }
 }
}
