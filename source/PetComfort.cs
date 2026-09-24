using System;using System.Diagnostics;using System.Linq;using System.Windows;using System.Windows.Threading;using Microsoft.Win32;using Forms=System.Windows.Forms;
namespace KianaPet {
 public sealed partial class PetWindow {
  readonly SmoothWalk walkMotion=new SmoothWalk();public readonly TaskNoticeHub TaskNotices=new TaskNoticeHub();
  bool manualQuiet,quietActive,quietHidden,sessionLocked;double lastQuietScan=-10;
  public string QuietStatus{get{return quietActive?"免打扰中 · "+(Settings.QuietMode=="hide"?"隐藏桌宠":"停止走动和气泡"):"正常陪伴";}}
  public bool ManualQuiet{get{return manualQuiet;}}
  public int LocalNoticeCount{get{return Settings.TaskNoticesEnabled?VisibleNotices.Count(n=>n.Unread):0;}}
  double DpiScale{get{var s=PresentationSource.FromVisual(this);return s==null||s.CompositionTarget==null?1:s.CompositionTarget.TransformToDevice.M11;}}
  void StartComfort(){
   TaskNotices.Changed+=NoticeChanged;
   if(!preview)Native.WTSRegisterSessionNotification(handle,0);
  }
  void StopComfort(){TaskNotices.Changed-=NoticeChanged;if(!preview)Native.WTSUnRegisterSessionNotification(handle);}
  void NoticeChanged(TaskNotice notice){if(quitting||Dispatcher.HasShutdownStarted)return;Dispatcher.BeginInvoke(new Action(delegate{if(!quitting&&Settings.TaskNoticesEnabled&&!NoticeMuted(notice))noticeBatcher.Add(notice,DateTime.UtcNow,Settings.NoticeBatchSeconds);}));}
  public void TestTaskNotice(){TaskNotices.Publish(new TaskNotice{Source="桌宠示例",TaskId=Guid.NewGuid().ToString("N"),Title="任务完成通知测试",Status="succeeded"});}
  public void ToggleQuiet(){manualQuiet=!manualQuiet;lastQuietScan=-10;UpdateQuiet(clock.Elapsed.TotalSeconds);}
  void UpdateQuiet(double now){if(now-lastQuietScan<2)return;lastQuietScan=now;string foreground="";string[] running=new string[0];
   if(Settings.QuietAppsEnabled&&Settings.QuietApps.Length>0){try{if(Settings.QuietForegroundOnly){uint pid;Native.GetWindowThreadProcessId(Native.GetForegroundWindow(),out pid);using(var p=Process.GetProcessById((int)pid))foreground=p.ProcessName;}else{var processes=Process.GetProcesses();try{running=processes.Select(p=>{try{return p.ProcessName;}catch{return "";}}).ToArray();}finally{foreach(var p in processes)p.Dispose();}}}catch{}}
   quietActive=ComfortRules.Quiet(Settings,manualQuiet,DateTime.Now.TimeOfDay,foreground,running);quietHidden=quietActive&&Settings.QuietMode=="hide";
   if(quietActive){walkUntil=0;bubble.Visibility=Visibility.Hidden;}
  }
  void RepairDisplay(){ScheduleDisplayRecovery(false);}
  public async void ReconnectChatGPT(){if(busyPoll||quitting)return;busyPoll=true;try{linkFailures=0;poll.Interval=TimeSpan.FromSeconds(2);Link.RequestReconnect();Link.StaleGraceSeconds=Settings.NotificationGraceSeconds;await Link.Poll(Settings.LinkChatGPT,Settings.BackgroundMini);ObserveBridgeNotifications();}finally{busyPoll=false;if(settingsWindow!=null)settingsWindow.UpdateStatus();}}
  public void ExportPreferences(bool positions){var dialog=new SaveFileDialog{Title="导出桌宠偏好",Filter="桌宠设置 (*.json)|*.json",FileName="琪亚娜桌宠设置.json",AddExtension=true};if(dialog.ShowDialog(settingsWindow)!=true)return;try{Save();System.IO.File.WriteAllText(dialog.FileName,PreferenceTransfer.Export(Settings,positions),new System.Text.UTF8Encoding(false));MessageBox.Show(settingsWindow,"设置已导出。默认通用偏好不含启动、快捷键、联动或位置；完整备份可包含这些本机配置。程序路径、连接会话和运行日志始终不包含。","导出完成");}catch(Exception e){MessageBox.Show(settingsWindow,e.Message,"导出失败");}}
  public void ImportPreferences(bool positions){var dialog=new OpenFileDialog{Title="导入桌宠偏好",Filter="桌宠设置 (*.json)|*.json",CheckFileExists=true};if(dialog.ShowDialog(settingsWindow)!=true)return;try{ReviewPreferenceFile(dialog.FileName,positions);}catch(Exception e){MessageBox.Show(settingsWindow,e.Message,"导入失败");}}
  public void ResetPreferences(){if(MessageBox.Show(settingsWindow,"恢复默认设置？当前偏好会先自动备份，宠物位置保留。","恢复默认",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;try{ReplacePreferences(new Config(),false);RefreshSettingsWindow();}catch(Exception e){MessageBox.Show(settingsWindow,e.Message,"恢复失败");}}
  public string ReplacePreferences(Config next,bool positions){
   next.Validate();if(!Pets.Any(p=>p.Id==next.Skin))throw new Exception("设置中的服装不在当前六套素材中。");
   Save();PreferenceTransfer.Backup();if(!positions){next.X=Settings.X;next.Y=Settings.Y;next.HasPosition=Settings.HasPosition;next.MusicPosition=Settings.MusicPosition;next.MusicX=Settings.MusicX;next.MusicY=Settings.MusicY;next.MusicHasPosition=Settings.MusicHasPosition;}
   string warning="";if(hotkey!=null&&!preview&&!hotkey.Configure(next.RecoverHotkeyEnabled,next.RecoverHotkeyModifiers,next.RecoverHotkeyKey)){warning="新快捷键未启用："+hotkey.Error;hotkey.Configure(false,0,0);next.RecoverHotkeyEnabled=false;}
   Settings=next;manualQuiet=false;lastQuietScan=-10;LoadSkin();ResizePet();UpdateLayout();if(next.HasPosition)MoveTo(next.X,next.Y);ClampPosition();ApplySettings();return warning;
  }
  void RefreshSettingsWindow(){string category=settingsWindow==null?"常用":settingsWindow.ActiveCategory,query=settingsWindow==null?"":settingsWindow.SearchQuery;if(settingsWindow!=null)settingsWindow.Close();OpenSettings();settingsWindow.NavigateCategory(category,query);}
 }
}
