using System;using System.IO;using System.Linq;using System.Diagnostics;using System.Threading.Tasks;using System.Windows;
namespace KianaPet {
 public sealed partial class PetWindow {
  readonly SceneAutomation autoScenes=new SceneAutomation();readonly FocusClock focusClock=new FocusClock();readonly ProcessMeter processMeter=new ProcessMeter();
  ExternalTaskInbox taskInbox;bool inboxBusy,onBattery;double nextCompanionScan,nextSceneScan,lastFocusSave;string focusReminderPending="";
  public Config PersistentPreferences(){return autoScenes.Persistent(Settings);}
  public bool FocusQuietActive{get{return Settings.FocusQuiet&&focusClock.Session.Running&&focusClock.Session.Phase=="focus";}}
  public string FocusStatus{get{return focusClock.Status+" · 已完成 "+focusClock.Session.Completed+" 轮";}}
  public string AutoSceneStatus{get{return !Settings.AutoScenesEnabled?"自动场景未启用":autoScenes.Paused?"自动场景已暂停（手动选择优先）":autoScenes.Active.Length==0?"等待规则匹配，使用原偏好":"自动场景："+autoScenes.Active+"（临时覆盖，退出后恢复）";}}
  public string ExternalTaskStatus{get{return !Settings.ExternalNoticesEnabled?"外部任务接收未启用":taskInbox==null?"等待初始化":taskInbox.Status;}}
  public string PerformanceStatus{get{processMeter.Sample(clock.Elapsed.TotalSeconds);return "当前策略："+CompanionOptions.ProfileLabel(PerformanceRules.Effective(Settings,onBattery))+(onBattery?" · 电池供电":" · 外接电源")+"\n桌宠进程内存："+processMeter.MemoryMiB.ToString("0.0")+" MiB · CPU："+(processMeter.Ready?processMeter.CpuPercent.ToString("0.0")+"%（单核 = 100%，最近采样区间）":"采样中")+"\n动画定时器："+animation.Interval.TotalMilliseconds.ToString("0")+" ms · 音乐轮询："+musicTimer.Interval.TotalSeconds.ToString("0.##")+" s · GPT 轮询："+poll.Interval.TotalSeconds.ToString("0.##")+" s";}}
  void StartCompanionFeatures(){autoScenes.Paused=Settings.AutoScenesPaused;taskInbox=new ExternalTaskInbox(Store.Root,TaskNotices);try{focusClock.Load(Store.Json.Deserialize<FocusSession>(File.ReadAllText(Path.Combine(Store.Root,"focus-session.json"))));}catch{}TaskNotices.RegisterAction("专注计时","focus-session",delegate{OpenFocusSettings();});}
  void StopCompanionFeatures(){PauseFocusForSystem();}
  void SaveFocus(){try{Store.Atomic("focus-session.json",focusClock.Session);}catch(Exception e){Store.Log("Focus save: "+e.Message);}}
  public void StartFocus(bool rest){focusClock.Begin(rest,Settings,clock.Elapsed.TotalSeconds);focusReminderPending="";TaskNotices.Read("专注计时","session");SaveFocus();}
  public void PauseFocus(){bool done=focusClock.Tick(clock.Elapsed.TotalSeconds,false);focusClock.Pause(clock.Elapsed.TotalSeconds);if(done)FocusCompleted();SaveFocus();}
  public void ResumeFocus(){focusClock.Resume(clock.Elapsed.TotalSeconds);SaveFocus();}
  public void ResetFocus(){focusClock.Reset();focusReminderPending="";TaskNotices.Read("专注计时","session");SaveFocus();}
  void PauseFocusForSystem(){if(focusClock.Session.Running){focusClock.Session.Running=false;SaveFocus();}}
  void FocusCompleted(){focusReminderPending=focusClock.Session.Phase;TaskNotices.Publish(new TaskNotice{Source="专注计时",TaskId="session",Title=focusClock.Session.Phase=="break-ready"?"专注完成，起来休息一下吧":"休息结束，准备好再开始下一轮",Status="waiting",ActionId="focus-session"});SaveFocus();}
  public void OpenFocusSettings(){OpenSettings();settingsWindow.NavigateCategory("专注与休息","");}
  public void PauseAutoScenes(){autoScenes.Pause(Settings);Settings.AutoScenesPaused=true;lastQuietScan=-10;}
  public void ResumeAutoScenes(){autoScenes.Resume();Settings.AutoScenesPaused=false;nextSceneScan=0;Save();}
  void TickCompanionFeatures(double now){
   bool running=focusClock.Session.Running;if(focusClock.Tick(now,sessionLocked))FocusCompleted();if(running&&!focusClock.Session.Running)SaveFocus();if(focusClock.Session.Running&&now-lastFocusSave>10){lastFocusSave=now;SaveFocus();}
   if(focusReminderPending.Length>0&&!manualHidden&&!fullscreenHidden&&!sessionLocked&&!quietActive&&!Settings.NoticeQuiet&&!menuLayerYielding&&IsVisible){string phase=focusReminderPending;if(phase!=focusClock.Session.Phase)focusReminderPending="";else if(Settings.FocusReminder=="badge")focusReminderPending="";else if(!sleeping&&!pressed&&!dragging&&interaction==null&&(!Settings.LinkChatGPT||!Link.Current.Connected||Link.Current.Stale||DateTime.UtcNow-Link.Current.At>=TimeSpan.FromSeconds(10)||Link.Current.State=="idle")&&(toolbarMenu==null||!toolbarMenu.IsOpen)&&!NoticePreviewOpen){focusReminderPending="";string text=phase=="break-ready"?"辛苦啦，起来活动一下吧。":"休息好了吗？准备好再开始。";if(Settings.FocusReminder=="action"&&!Settings.ReduceMotion)React("waving",text,5);else Say(text,5);}}
   if(now>=nextSceneScan){nextSceneScan=now+2;onBattery=System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus==System.Windows.Forms.PowerLineStatus.Offline;if(!preview){string foreground="";if(Settings.AutoScenesEnabled&&Settings.AutoSceneRules.Any(r=>r.Trigger=="foreground")){try{uint id;Native.GetWindowThreadProcessId(Native.GetForegroundWindow(),out id);using(var p=Process.GetProcessById((int)id))foreground=p.ProcessName;}catch{}}bool music=Music.Current.Connected&&Music.Current.Playing&&DateTime.UtcNow-Music.Current.At<TimeSpan.FromSeconds(6);string target=sessionLocked?"":SceneAutomation.Match(Settings,foreground,music,onBattery,DateTime.Now.TimeOfDay);if(autoScenes.Tick(Settings,target,now)){lastQuietScan=-10;walkUntil=0;}}}
   if(now<nextCompanionScan)return;nextCompanionScan=now+2;
   if(!preview&&Settings.ExternalNoticesEnabled&&!inboxBusy&&taskInbox!=null){inboxBusy=true;Task.Run(()=>taskInbox.Drain()).ContinueWith(t=>{if(t.IsFaulted)taskInbox.Status="接收暂不可用："+t.Exception.GetBaseException().Message;if(!Dispatcher.HasShutdownStarted)Dispatcher.BeginInvoke(new Action(delegate{inboxBusy=false;}));});}
  }
 }
}
