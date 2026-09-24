using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using System.Threading.Tasks;using System.Windows;using System.Windows.Input;using System.Windows.Interop;using System.Runtime.InteropServices;using KianaPet;using R=System.Drawing.Rectangle;
class MusicPositionTests {
 [DllImport("user32.dll")]static extern bool SetCursorPos(int x,int y);
 static List<string> passed=new List<string>();static void Check(bool ok,string name){if(!ok)throw new Exception(name);passed.Add(name);}
 static object Call(object o,string name,params object[] a){return o.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,a);}
 static void Set(object o,string name,object v){o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);}
 static T Field<T>(object o,string name){return (T)o.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);}
 static Native.Rect Bounds(Window w){Native.Rect r;Native.GetWindowRect(new WindowInteropHelper(w).Handle,out r);return r;}
 [STAThread]static void Main(string[] args){Store.Root=args[0];Directory.CreateDirectory(Store.Root);var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};PetWindow pet=null;Native.Point original;Native.GetCursorPos(out original);
 app.Startup+=async delegate{try{
  R area=new R(0,0,1920,1080),body=new R(800,350,264,430);var below=MusicPlacement.Place(body,area,357,132,9,"below",false,0,0);Check(below.Top==body.Bottom+9&&!below.IntersectsWith(body),"下方布局保留工具栏间距");
  var bottom=new R(800,650,264,430);var side=MusicPlacement.Place(bottom,area,357,132,9,"below",false,0,0);Check(area.Contains(side)&&!side.IntersectsWith(bottom),"贴底时移到侧边且不覆盖按钮栏");
  var edge=new R(0,650,264,430);var right=MusicPlacement.Place(edge,area,357,165,9,"left",false,0,0);Check(area.Contains(right)&&right.Left>=edge.Right+9,"左侧无空间自动向右避让，兼容展开歌词");
  var free=MusicPlacement.Place(body,area,357,132,9,"free",true,50,80);Check(free.Location==new System.Drawing.Point(50,80),"手动位置固定");
  var collision=MusicPlacement.Place(body,area,357,132,9,"free",true,850,600);Check(!collision.IsEmpty&&!collision.IntersectsWith(body),"手动位置覆盖宠物时自动避让");
  var other=new R(-1920,0,1920,1080);var negative=MusicPlacement.Place(body,other,357,132,9,"free",true,-1000,100);Check(negative.Left==-1000,"支持负坐标显示器");
  Check(area.Contains(MusicPlacement.Place(body,area,357,132,9,"free",true,-9000,9000)),"迁移后屏幕外坐标自动收回");
  Check(MusicPlacement.Place(body,new R(0,0,100,100),357,132,9,"below",false,0,0).IsEmpty,"屏幕无可用空间时不遮挡工具栏");
  double until=0;Check(!MusicPlacement.HoverVisible(true,false,false,1,ref until),"悬停模式默认隐藏");Check(MusicPlacement.HoverVisible(true,true,false,2,ref until),"移入宠物显示");Check(MusicPlacement.HoverVisible(true,false,false,2.5,ref until),"跨越间隙的保留时间");Check(!MusicPlacement.HoverVisible(true,false,false,3,ref until),"离开后自动隐藏");Check(MusicPlacement.HoverVisible(true,false,true,4,ref until),"拖动期间保持显示");Check(MusicPlacement.HoverVisible(false,false,false,20,ref until),"可切换为常显");
  Store.Save(new Config{MusicPosition="free",MusicHasPosition=true,MusicX=-900,MusicY=75,MusicHoverOnly=false});var saved=Store.Load();Check(saved.MusicX==-900&&saved.MusicHasPosition&&!saved.MusicHoverOnly,"保存与恢复位置和显示偏好");
  Store.Save(new Config{MusicEnabled=true,MusicHoverOnly=true,Walking=false,Gravity=false,HideFullscreen=false,SleepSchedule=false,IdleSleep=false});pet=new PetWindow(true);pet.Show();foreach(var w in app.Windows.OfType<SettingsWindow>().ToArray())w.Close();SetCursorPos(10,10);
  pet.Music.Current=new MusicState{Connected=true,Title="琪亚娜的音乐时间",Artist="位置与悬停预览",At=DateTime.UtcNow};Set(pet,"hover",false);Call(pet,"UpdateMusic");Check(Field<MusicWindow>(pet,"musicWindow")==null,"真实窗口在未悬停时不创建弹出栏");
  Set(pet,"hover",true);Call(pet,"UpdateMusic");await Task.Delay(250);var music=Field<MusicWindow>(pet,"musicWindow");Check(music!=null&&music.IsVisible,"真实音乐窗口在悬停时显示");
  var mb=Bounds(music);var pb=Bounds(pet);Check(!new R(mb.Left,mb.Top,mb.Width,mb.Height).IntersectsWith(new R(pb.Left,pb.Top,pb.Width,pb.Height)),"真实 DPI 窗口不覆盖宠物和工具栏");
  Set(pet,"hover",false);Set(pet,"musicUntil",0d);Call(pet,"UpdateMusic");for(int attempt=0;attempt<25&&music.IsVisible;attempt++){await Task.Delay(100);Call(pet,"UpdateMusic");}Check(!music.IsVisible,"真实淡出完成后隐藏窗口");
  pet.Settings.MusicHoverOnly=false;pet.Music.Current.At=DateTime.UtcNow;Call(pet,"UpdateMusic");await Task.Delay(200);Check(music.IsVisible,"切换常显立即恢复");
  var head=Field<UIElement>(music,"dragSurface");mb=Bounds(music);SetCursorPos(mb.Left+35,mb.Top+25);head.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonDownEvent});SetCursorPos(mb.Left+95,mb.Top-55);head.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice,Environment.TickCount){RoutedEvent=UIElement.MouseMoveEvent});Check(music.IsDragging,"封面拖动事件改变真实窗口位置");head.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=UIElement.MouseLeftButtonUpEvent});Check(!music.IsDragging&&pet.Settings.MusicPosition=="free"&&pet.Settings.MusicHasPosition,"松手切换手动固定并保存");saved=Store.Load();Check(saved.MusicPosition=="free"&&saved.MusicX==pet.Settings.MusicX,"拖动位置持久化");
  pet.ResetMusicPosition();Check(pet.Settings.MusicPosition=="below"&&!pet.Settings.MusicHasPosition,"一键恢复下方布局");pet.Music.Current.At=DateTime.UtcNow;Call(pet,"UpdateMusic");await Task.Delay(200);music.Capture(Path.Combine(Store.Root,"music-position.png"));
  Store.Atomic("music-position-tests.json",new{version="0.3.2",passed=true,checks=passed,dragTest="路由鼠标事件及真实窗口坐标验证；未触发音乐播放控制"});
 }catch(Exception e){Store.Atomic("music-position-tests.json",new{version="0.3.2",passed=false,checks=passed,error=e.ToString()});Environment.ExitCode=1;}finally{SetCursorPos(original.X,original.Y);if(pet!=null)pet.Close();else app.Shutdown();}};app.Run();}
}
