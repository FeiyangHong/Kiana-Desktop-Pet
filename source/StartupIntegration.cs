using System;using System.Diagnostics;using System.IO;using System.Threading.Tasks;
namespace KianaPet {
 public static class StartupRules {
  public static bool ShouldLaunch(bool enabled,bool running,bool preview){return enabled&&!running&&!preview;}
  public static string RepairAction(string code){return new[]{"app-closed","launcher-missing","session-stale","endpoint-unavailable"}.ContainsCode(code)?"launcher":code=="disabled"?"disabled":"recheck";}
  static bool ContainsCode(this string[] values,string value){return Array.IndexOf(values,value)>=0;}
 }
 public static class CompanionLauncher {
  public static bool Running(string name){var list=Process.GetProcessesByName(name);try{return list.Length>0;}finally{foreach(var p in list)p.Dispose();}}
  public static ProcessStartInfo ChatGPT(bool interactive){string path=Path.Combine(CompanionPaths.SmoothRoot(),"app","scripts","launch.cmd");if(!File.Exists(path))throw new FileNotFoundException("请安装完整迁移包中的 ChatGPT 联动组件。",path);return new ProcessStartInfo{FileName=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"cmd.exe"),Arguments="/d /k \"\""+path+"\"\"",UseShellExecute=true,WindowStyle=interactive?ProcessWindowStyle.Normal:ProcessWindowStyle.Hidden};}
  public static void StartChatGPT(bool interactive){Process.Start(ChatGPT(interactive));}
  public static void StartMusicQuietly(){string path=MusicBridge.FindApp();if(string.IsNullOrEmpty(path))throw new FileNotFoundException("没有找到网易云音乐客户端。" );Process.Start(new ProcessStartInfo{FileName=path,Arguments="--remote-debugging-port="+MusicBridge.Port+" --remote-debugging-address=127.0.0.1",WorkingDirectory=Path.GetDirectoryName(path),UseShellExecute=true});}
 }
 public sealed partial class PetWindow {
  bool startupHandled;public string StartupStatus{get;private set;}
  public void LaunchChatGPT(){try{CompanionLauncher.StartChatGPT(true);}catch(Exception e){System.Windows.MessageBox.Show(e.Message,"打开 ChatGPT");}}
  async void StartCompanionApps(){if(startupHandled||preview)return;startupHandled=true;StartupStatus="启动选项已检查";await Task.Delay(700);if(quitting)return;
   try{bool running=CompanionLauncher.Running("ChatGPT");if(StartupRules.ShouldLaunch(Settings.LaunchChatGPTOnStart,running,preview)){CompanionLauncher.StartChatGPT(false);StartupStatus="正在启动 ChatGPT";}}catch(Exception e){StartupStatus="ChatGPT 未启动："+e.Message;Store.Log(StartupStatus);}
   try{bool running=CompanionLauncher.Running("cloudmusic");if(StartupRules.ShouldLaunch(Settings.LaunchMusicOnStart,running,preview)){CompanionLauncher.StartMusicQuietly();StartupStatus+=" · 正在启动网易云音乐";}}catch(Exception e){StartupStatus+=" · 音乐未启动："+e.Message;Store.Log(e.Message);}
  }
  public async void RepairChatGPT(){if(busyPoll||quitting)return;busyPoll=true;try{
   Link.StaleGraceSeconds=Settings.NotificationGraceSeconds;Link.RequestReconnect();await Link.Poll(Settings.LinkChatGPT,Settings.BackgroundMini);
   string action=StartupRules.RepairAction(Link.Current.Code);
   if(action=="launcher")LaunchChatGPT();else if(action=="disabled")Say("请先开启 ChatGPT 联动。",4);
   ObserveBridgeNotifications();
  }finally{busyPoll=false;linkFailures=0;poll.Interval=TimeSpan.FromSeconds(2);if(settingsWindow!=null)settingsWindow.UpdateStatus();}}
 }
}
