using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;using System.Threading.Tasks;using System.Windows;using System.Windows.Controls;using System.Windows.Media;using KianaPet;
class LyricMotionTests {
 static readonly List<string> checks=new List<string>();
 static T Field<T>(object owner,string name){return (T)owner.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);}
 static void Check(bool value,string name){if(!value)throw new Exception(name);checks.Add(name);}
 [STAThread]static int Main(string[] args){Store.Root=args[0];Store.Save(new Config{Walking=false,Gravity=false,LinkChatGPT=false,MusicEnabled=false,MusicLyrics=true,SleepSchedule=false,IdleSleep=false,HideFullscreen=false});var app=new Application{ShutdownMode=ShutdownMode.OnLastWindowClose};var pet=new PetWindow(true);bool passed=false;
  pet.Loaded+=async delegate{MusicWindow music=null;try{await Task.Delay(750);MotionSettings.Reduced=false;music=new MusicWindow(pet);var playing=new MusicState{Connected=true,Title="示例歌曲",Artist="测试",Playing=true};string longLine="这是一句很长很长的同步歌词，用来确认音乐栏会显示完整内容并且在两端来回滚动。";
   music.Refresh(playing,longLine,null,false);music.ShowSoft();await Task.Delay(1750);var row=Field<Grid>(music,"lyricRow");var offset=Field<TranslateTransform>(music,"lyricOffset");var text=Field<TextBlock>(music,"lyric");Check(row.ActualWidth>100&&offset.X < -10,"播放长歌词时向左滚动");music.Capture(Path.Combine(Store.Root,"lyric-scrolling.png"));
   music.Refresh(playing,"短句",null,false);await Task.Delay(80);Check(offset.X>0&&text.TextTrimming==TextTrimming.None,"短句居中且不滚动");
   music.Refresh(playing,longLine,null,false);await Task.Delay(100);Check(offset.X>=-2,"歌词换行后从开头重新播放");
   playing.Playing=false;music.Refresh(playing,longLine,null,false);await Task.Delay(80);Check(Math.Abs(offset.X)<1&&text.TextTrimming==TextTrimming.CharacterEllipsis,"暂停时停止滚动并省略超出部分");
   playing.Playing=true;MotionSettings.Reduced=true;music.Refresh(playing,longLine,null,false);await Task.Delay(80);Check(Math.Abs(offset.X)<1&&text.TextTrimming==TextTrimming.CharacterEllipsis,"减少动态效果时保持静态省略");
   MotionSettings.Reduced=false;music.Close();passed=true;
  }catch(Exception e){Store.Atomic("lyric-motion-error.json",new{error=e.ToString()});}finally{Store.Atomic("lyric-motion-tests.json",new{passed,checks});if(music!=null&&music.IsVisible)music.Close();foreach(var w in app.Windows.Cast<Window>().Where(w=>w!=pet).ToArray())w.Close();pet.Close();}};app.Run(pet);return passed?0:1;
 }
}
