using System;using System.IO;using System.Linq;using System.Reflection;using System.Diagnostics;using System.Threading.Tasks;using System.Windows;using System.Windows.Controls;using System.Windows.Media;using System.Windows.Media.Imaging;using System.Windows.Threading;using KianaPet;
class SkinFeedbackTests {
 static int checks;
 static object F(object o,string key){return o.GetType().GetField(key,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);}
 static void Set(object o,string key,object value){o.GetType().GetField(key,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);}
 static object Call(object o,string key,params object[] args){return o.GetType().GetMethod(key,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);}
 static void Check(bool value,string message){if(!value)throw new Exception(message);checks++;}
 static bool Bubble(PetWindow pet){return ((Border)F(pet,"bubble")).Visibility==Visibility.Visible;}
 static string Text(PetWindow pet){return ((TextBlock)F(pet,"bubbleText")).Text;}
 static void Clear(PetWindow pet){((Border)F(pet,"bubble")).Visibility=Visibility.Hidden;Set(pet,"bubbleUntil",0d);}
 static bool Swap(PetWindow pet){return (bool)Call(pet,"ApplyAutomaticSkin",SkinShuffleRules.Defaults.First(id=>id!=pet.Settings.Skin),((Stopwatch)F(pet,"clock")).Elapsed.TotalSeconds);}
 static void Capture(PetWindow pet){pet.UpdateLayout();var content=(FrameworkElement)pet.Content;var visual=new DrawingVisual();using(var dc=visual.RenderOpen()){dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(248,246,252)),null,new Rect(0,0,pet.Width,pet.Height));dc.DrawRectangle(new VisualBrush(content){Stretch=Stretch.Fill},null,new Rect(0,0,pet.Width,pet.Height));}var bitmap=new RenderTargetBitmap((int)Math.Ceiling(pet.Width*2),(int)Math.Ceiling(pet.Height*2),192,192,PixelFormats.Pbgra32);bitmap.Render(visual);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(Store.Root,"feedback.png")))encoder.Save(file);}
 [STAThread]static int Main(string[] args){Store.Root=args[0];Store.Save(new Config{LinkChatGPT=false,MusicEnabled=false,Walking=false,Gravity=false,AmbientActions=false,SleepSchedule=false,IdleSleep=false,HideFullscreen=false,LaunchChatGPTOnStart=false,LaunchMusicOnStart=false,NoticeSound=false,SkinShuffleEnabled=false,SkinShuffleOnStart=false,SmoothTransitions=false,ReduceMotion=false,Bubbles=true});var app=new Application{ShutdownMode=ShutdownMode.OnLastWindowClose};var pet=new PetWindow(true);bool passed=false;
  pet.Loaded+=async delegate{try{
   await Task.Delay(1100);((DispatcherTimer)F(pet,"animation")).Stop();foreach(var w in app.Windows.OfType<SettingsWindow>().ToArray())w.Close();Set(pet,"hover",false);Set(pet,"interaction",null);Set(pet,"menuLayerYielding",false);Set(pet,"sleeping",false);Set(pet,"state","idle");Clear(pet);
   double phase=(double)F(pet,"stateAt"),width=pet.Width,height=pet.Height;var polling=((DispatcherTimer)F(pet,"poll")).Interval;
   Check(Swap(pet)&&Bubble(pet)&&SkinChangeMessages.Contains(Text(pet)),"direct successful swap has completion feedback");
   Check(F(pet,"interaction")==null&&(double)F(pet,"stateAt")==phase&&pet.Width==width&&pet.Height==height&&((DispatcherTimer)F(pet,"poll")).Interval==polling,"feedback preserves action timeline, bounds and polling");
   pet.Say("任务已完成",5);double taskUntil=(double)F(pet,"bubbleUntil");Swap(pet);Check(Text(pet)=="任务已完成"&&(double)F(pet,"bubbleUntil")==taskUntil,"existing task bubble keeps its text and lifetime");
   Clear(pet);pet.Settings.Bubbles=false;Swap(pet);Check(!Bubble(pet),"bubble preference is respected");pet.Settings.Bubbles=true;
   foreach(string flag in new[]{"quietActive","sessionLocked","manualHidden","fullscreenHidden","quietHidden","sleeping","pressed","dragging","menuLayerYielding"}){Set(pet,flag,true);Clear(pet);Call(pet,"ShowSkinShuffleFeedback");Check(!Bubble(pet),"suppressed in "+flag);Set(pet,flag,false);}
   Set(pet,"interaction","waving");Call(pet,"ShowSkinShuffleFeedback");Check(!Bubble(pet),"active interaction is not interrupted");Set(pet,"interaction",null);
   pet.Settings.LinkChatGPT=true;pet.Link.Current.Connected=true;pet.Link.Current.State="running";pet.Link.Current.At=DateTime.UtcNow;Call(pet,"ShowSkinShuffleFeedback");Check(!Bubble(pet),"active GPT task has priority");pet.Settings.LinkChatGPT=false;
   Clear(pet);Check(!(bool)Call(pet,"ApplyAutomaticSkin","missing",0d)&&!Bubble(pet),"invalid selection has no success bubble");Check(!(bool)Call(pet,"ApplyAutomaticSkin",pet.Settings.Skin,0d)&&!Bubble(pet),"same outfit has no success bubble");
   pet.Settings.ReduceMotion=true;Call(pet,"ApplyInputOptions");Swap(pet);Check(Bubble(pet),"reduced motion keeps static completion feedback");pet.Settings.ReduceMotion=false;pet.Settings.SmoothTransitions=true;Call(pet,"ApplyInputOptions");Clear(pet);
   if(MotionSettings.Enabled){Swap(pet);Check(!Bubble(pet),"animated swap does not announce completion before the handoff");await Task.Delay(370);Check(Bubble(pet)&&!(bool)F(pet,"skinVisualRunning"),"bubble follows the completed fade");Capture(pet);
    Clear(pet);Swap(pet);Call(pet,"StopSkinVisual");await Task.Delay(370);Check(!Bubble(pet),"interrupted transition cannot emit delayed feedback");
    Swap(pet);await Task.Delay(40);Swap(pet);await Task.Delay(180);Check(!Bubble(pet),"older rapid selection cannot announce a newer swap early");await Task.Delay(200);Check(Bubble(pet),"latest rapid selection completes normally");
    Clear(pet);Swap(pet);pet.Say("新任务需要处理",5);await Task.Delay(370);Check(Text(pet)=="新任务需要处理","task bubble arriving during fade keeps priority");
    Clear(pet);Swap(pet);pet.Settings.LinkChatGPT=true;pet.Link.Current.State="running";pet.Link.Current.At=DateTime.UtcNow;await Task.Delay(370);Check(!Bubble(pet),"new GPT task suppresses pending completion feedback");pet.Settings.LinkChatGPT=false;
   }
   Clear(pet);pet.SelectSkin(SkinShuffleRules.Defaults.First(id=>id!=pet.Settings.Skin));Check(!Bubble(pet)&&F(pet,"interaction")==null,"manual selection waits for the shared handoff without a forced wave");await Task.Delay(370);Check(Bubble(pet)&&SkinChangeMessages.Contains(Text(pet)),"manual selection uses the shared completion bubble");
   passed=true;
  }catch(Exception e){Store.Atomic("error.json",new{error=e.ToString(),checks});}finally{foreach(var w in app.Windows.Cast<Window>().Where(w=>w!=pet).ToArray())w.Close();pet.Close();}};app.Run(pet);Store.Atomic("result.json",new{passed,checks});return passed?0:1;
 }
}
