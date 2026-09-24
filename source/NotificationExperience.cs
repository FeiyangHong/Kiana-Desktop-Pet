using System;using System.Collections.Generic;using System.Linq;using System.Media;
namespace KianaPet {
 public sealed class NotificationTransitions {
  readonly Dictionary<string,string> known=new Dictionary<string,string>();bool seeded;
  public BridgeNotice[] Update(BridgeState state){if(!state.Connected||state.Stale)return new BridgeNotice[0];var rows=state.Notices.Length>0?state.Notices:state.NotificationCount>0?new[]{new BridgeNotice{Id="native-state",State=state.State,Title="ChatGPT 任务",UpdatedAt=state.At}}:new BridgeNotice[0];var changed=new List<BridgeNotice>();
   foreach(var row in rows){string prior;bool existed=known.TryGetValue(row.Id,out prior);if(seeded&&(!existed||prior!=row.State)&&new[]{"review","failed","waiting"}.Contains(row.State))changed.Add(row);known[row.Id]=row.State;}
   foreach(string key in known.Keys.Where(k=>!rows.Any(r=>r.Id==k)).ToArray())known.Remove(key);seeded=true;return changed.ToArray();
  }
 }
 public sealed partial class PetWindow {
  readonly NotificationTransitions notificationTransitions=new NotificationTransitions();
  void ObserveBridgeNotifications(){if(!Settings.LinkChatGPT)return;foreach(var row in notificationTransitions.Update(Link.Current))noticeBatcher.Add(new TaskNotice{Source="ChatGPT",TaskId=row.Id,Title=string.IsNullOrWhiteSpace(row.Title)?"任务状态更新":row.Title,Status=row.State=="review"?"succeeded":row.State},DateTime.UtcNow,Settings.NoticeBatchSeconds);}
  void ShowNoticeSummary(string summary){if(Settings.NoticeQuiet)return;if(summary.Length>100)summary=summary.Substring(0,97)+"…";Say(summary,Settings.CompletionNoticeSeconds);if(Settings.NoticeSound&&!quietActive&&!sessionLocked&&!manualHidden&&!fullscreenHidden&&!menuLayerYielding)SystemSounds.Asterisk.Play();}
  public bool NotificationsAvailable{get{return Settings.LinkChatGPT&&(Link.Current.Stale?Link.Current.LastSuccessAt.HasValue&&DateTime.UtcNow-Link.Current.LastSuccessAt.Value<TimeSpan.FromSeconds(Settings.NotificationGraceSeconds):Link.Current.Connected&&DateTime.UtcNow-Link.Current.At<TimeSpan.FromSeconds(10));}}
  public async void OpenChatGPTTask(string id){if(!Link.Current.Connected||Link.Current.Stale){Say("连接恢复后再打开任务。",4);return;}try{await Link.OpenMainNotification(id);}catch{Say("任务暂时无法打开，请重新检测连接。",4);}}
 }
}
