using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
namespace KianaPet {
 public static class MusicLauncher {
  static bool Running(){bool result=false;foreach(var p in Process.GetProcessesByName("cloudmusic")){result=true;p.Dispose();}return result;}
  public static int Run(){string exe=MusicBridge.FindApp();if(exe.Length==0){MessageBox.Show("没有找到网易云音乐 Windows 客户端，请先安装客户端，再打开这个入口。","网易云音乐");return 2;}
   if(Native.TrustedMusicPort(MusicBridge.Port,exe)){Process.Start(new ProcessStartInfo{FileName=exe,UseShellExecute=true});return 0;}
   var app=new Application{ShutdownMode=ShutdownMode.OnMainWindowClose};var window=new Window{Title="网易云音乐",Width=440,Height=215,ResizeMode=ResizeMode.NoResize,WindowStartupLocation=WindowStartupLocation.CenterScreen,FontFamily=new FontFamily("Microsoft YaHei UI"),Background=Brushes.White};var stack=new StackPanel{Margin=new Thickness(24)};var title=new TextBlock{Text="准备连接网易云音乐",FontSize=19,FontWeight=FontWeights.SemiBold};var status=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,14,0,12),FontSize=13,Foreground=Brushes.DimGray};stack.Children.Add(title);stack.Children.Add(status);var cancel=new Button{Content="关闭",Width=75,Height=29,HorizontalAlignment=HorizontalAlignment.Right};cancel.Click+=delegate{window.Close();};stack.Children.Add(cancel);window.Content=stack;
   bool started=false;DateTime began=DateTime.UtcNow;var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};timer.Tick+=delegate{
    if(Native.TrustedMusicPort(MusicBridge.Port,exe)){window.Close();return;}
    if(!started&&!Running()){try{Process.Start(new ProcessStartInfo{FileName=exe,Arguments="--remote-debugging-port="+MusicBridge.Port+" --remote-debugging-address=127.0.0.1",WorkingDirectory=System.IO.Path.GetDirectoryName(exe),UseShellExecute=true});started=true;began=DateTime.UtcNow;}catch(Exception e){status.Text="启动失败："+e.Message;timer.Stop();}}
    if(started){status.Text=DateTime.UtcNow-began<TimeSpan.FromSeconds(20)?"正在启动客户端，连接成功后此窗口自动关闭。":"客户端尚未提供连接。请关闭此窗口，在网易云音乐完全退出后重试。";if(DateTime.UtcNow-began>TimeSpan.FromSeconds(25))timer.Stop();}
    else status.Text="网易云音乐正在普通模式运行。请从它的托盘菜单选择“退出”，此窗口会等待并自动重新启动。";
   };window.Closed+=delegate{timer.Stop();};timer.Start();app.Run(window);return 0;
  }
 }
}
