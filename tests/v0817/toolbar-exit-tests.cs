using System;using System.IO;using System.Linq;using System.Reflection;using System.Diagnostics;using System.Collections.Generic;using System.Threading.Tasks;using System.Windows;using System.Windows.Controls;using System.Windows.Media;using System.Windows.Media.Imaging;using System.Windows.Threading;using KianaPet;
class ToolbarExitTests {
 static int checks;static readonly List<object> samples=new List<object>();
 static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;}
 static object F(object obj,string name){return obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(obj);}
 static void Set(object obj,string name,object value){obj.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);}
 static object Call(object obj,string name,params object[] args){return obj.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(obj,args);}
 static string Palette(Border bar){var solid=bar.Background as SolidColorBrush;var gradient=bar.Background as GradientBrush;return solid!=null?"solid:"+solid.Color:gradient!=null?"gradient:"+string.Join("/",gradient.GradientStops.Select(s=>s.Color.ToString())):"other";}
 static BitmapSource Cover(){byte[] data=new byte[16*16*4];for(int i=0;i<256;i++){data[i*4]=(byte)(i%16<8?200:40);data[i*4+1]=70;data[i*4+2]=(byte)(i%16<8?40:200);data[i*4+3]=255;}var b=BitmapSource.Create(16,16,96,96,PixelFormats.Bgra32,null,data,64);b.Freeze();return b;}
 static async Task<MusicWindow> Show(PetWindow pet,Border bar,string style,string mode){
  Call(pet,"SetToolbarVisible",false,true);var old=F(pet,"musicWindow") as MusicWindow;if(old!=null)old.HideSoft(true);
  pet.Settings.ToolbarStyle=style;pet.Settings.ToolbarFollowMusic=true;pet.Settings.ToolbarPinned=false;pet.Settings.MusicHoverOnly=true;pet.Settings.MusicEnabled=true;pet.Settings.MusicCoverTint=true;pet.Settings.MusicTintStrength=100;pet.Settings.MusicTintMode=mode;pet.Settings.ToolbarColorSource="manual";pet.Settings.ToolbarColor1="#00FF00";pet.Settings.ToolbarColor2="#FF00FF";pet.Settings.ToolbarColor3="#FFFF00";
  pet.Music.Current=new MusicState{Connected=true,Playing=true,Title="Fade-out fixture",NativeCover=Cover(),At=DateTime.UtcNow};Set(pet,"hover",true);Call(pet,"SetToolbarVisible",true,false);Call(pet,"UpdateMusic");await Task.Delay(220);
  Check(pet.ToolbarFollowingMusic&&bar.Visibility==Visibility.Visible,"music palette prepared before fade: "+style+"/"+mode);return (MusicWindow)F(pet,"musicWindow");
 }
 static async Task ObserveExit(PetWindow pet,Border bar,string expected,string label){
  var clock=Stopwatch.StartNew();int visible=0;
  while(clock.ElapsedMilliseconds<240){Call(pet,"UpdateToolbarTheme",0d);if(bar.Visibility==Visibility.Visible&&bar.Opacity>.005){string palette=Palette(bar);samples.Add(new{label,milliseconds=clock.ElapsedMilliseconds,opacity=bar.Opacity,palette});Check(palette==expected,"fade-out palette changed before toolbar was hidden: "+label);visible++;}await Task.Delay(10);}
  Check(visible>0,"observed real visible fade frames: "+label);Check(bar.Visibility==Visibility.Hidden,"fade completion hides toolbar: "+label);
 }
 [STAThread]static int Main(string[] args){Store.Root=args[0];Store.Save(new Config{LinkChatGPT=false,MusicEnabled=false,Walking=false,Gravity=false,SleepSchedule=false,IdleSleep=false,HideFullscreen=false,NoticeSound=false});var app=new Application{ShutdownMode=ShutdownMode.OnLastWindowClose};var pet=new PetWindow(true);bool passed=false;
  pet.Loaded+=async delegate{try{await Task.Delay(1100);((DispatcherTimer)F(pet,"animation")).Stop();foreach(var w in app.Windows.OfType<SettingsWindow>().ToArray())w.Close();var bar=(Border)F(pet,"hoverBar");
   foreach(string style in new[]{"tricolor","legacy","solid","glass"})foreach(string mode in new[]{"cover-gradient","solid","background-gradient"}){
    var card=await Show(pet,bar,style,mode);string expected=Palette(bar);Set(pet,"hover",false);Set(pet,"musicUntil",0d);Set(pet,"toolbarUntil",0d);Call(pet,"SetToolbarVisible",false,false);card.HideSoft(false);Call(pet,"UpdateToolbarTheme",0d);await ObserveExit(pet,bar,expected,style+"/"+mode);
    Check(!pet.ToolbarFollowingMusic,"independent theme restored after hidden: "+style+"/"+mode);
   }
   // Polling may hide the music card just before the next toolbar visibility tick.
   var asyncCard=await Show(pet,bar,"tricolor","cover-gradient");string beforePoll=Palette(bar);Set(pet,"hover",false);Set(pet,"musicUntil",0d);Set(pet,"toolbarUntil",0d);asyncCard.HideSoft(false);Call(pet,"UpdateToolbarTheme",0d);Check(Palette(bar)==beforePoll,"poll-before-visibility order keeps the outgoing palette");Call(pet,"SetToolbarVisible",false,false);await ObserveExit(pet,bar,beforePoll,"poll-first");
   var reenter=await Show(pet,bar,"tricolor","cover-gradient");string beforeReenter=Palette(bar);Call(pet,"SetToolbarVisible",false,false);reenter.HideSoft(false);await Task.Delay(35);Set(pet,"hover",true);Call(pet,"SetToolbarVisible",true,false);Call(pet,"UpdateMusic");Check(Palette(bar)==beforeReenter,"rapid return reveals the music palette immediately");await Task.Delay(200);Check(bar.Visibility==Visibility.Visible&&Palette(bar)==beforeReenter,"cancelled hide callback cannot reset a reappearing toolbar");
   var disconnected=await Show(pet,bar,"legacy","solid");string beforeDisconnect=Palette(bar);Set(pet,"hover",false);Set(pet,"musicUntil",0d);Set(pet,"toolbarUntil",0d);Call(pet,"SetToolbarVisible",false,false);pet.Music.Current.Connected=false;Call(pet,"UpdateMusic");await ObserveExit(pet,bar,beforeDisconnect,"disconnect-during-hide");Check(!pet.ToolbarFollowingMusic,"disconnected hidden toolbar releases music source");
   var pinned=await Show(pet,bar,"tricolor","cover-gradient");pet.Settings.ToolbarPinned=true;pinned.HideSoft(false);Call(pet,"UpdateToolbarTheme",0d);Check(((GradientBrush)bar.Background).GradientStops.Count==3,"toolbar that remains pinned restores its independent theme normally");
   var immediate=await Show(pet,bar,"tricolor","cover-gradient");immediate.HideSoft(true);Call(pet,"SetToolbarVisible",false,true);Call(pet,"UpdateToolbarTheme",0d);Check(bar.Visibility==Visibility.Hidden&&!pet.ToolbarFollowingMusic,"immediate hide does not retain a stale music source");
   MotionSettings.Reduced=true;Set(pet,"hover",true);Call(pet,"SetToolbarVisible",true,false);Set(pet,"hover",false);Set(pet,"musicUntil",0d);Set(pet,"toolbarUntil",0d);Call(pet,"SetToolbarVisible",false,false);await Task.Delay(60);Check(bar.Visibility==Visibility.Hidden,"reduced-motion hide still completes");passed=true;
  }catch(Exception e){Store.Atomic("error.json",new{error=e.ToString()});}finally{Store.Atomic("result.json",new{passed,checks,samples});foreach(var w in app.Windows.Cast<Window>().Where(w=>w!=pet).ToArray())w.Close();pet.Close();}};app.Run(pet);return passed?0:1;
 }
}
