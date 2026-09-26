using System;using System.Linq;using System.Windows;using System.Windows.Controls;
namespace KianaPet {
 public sealed partial class SettingsWindow {
  TextBlock musicReactionStatus,musicRuleSummary;ComboBox songNormalAction,songChorusAction;Button chorusStart,chorusEnd,chorusClear;bool syncingMusicReactions;
  static readonly string[] normalActions={"mixed","quiet","gentle","lively","off"},chorusActions={"same","quiet","gentle","lively","off"};
  ComboBox MusicActionChoice(Panel page,string label,string[] values,string selected,Action<string> save){Note(page,label);var box=new ComboBox{ItemsSource=values.Select(MusicReactions.Label).ToArray(),SelectedIndex=Array.IndexOf(values,selected),Padding=new Thickness(8),Margin=new Thickness(0,0,0,8)};System.Windows.Automation.AutomationProperties.SetName(box,label);box.SelectionChanged+=delegate{if(!syncingMusicReactions&&box.SelectedIndex>=0){save(values[box.SelectedIndex]);UpdateMusicReactionSettings();}};page.Children.Add(box);return box;}
  void BuildMusicReactions(Panel page){
   TitleText(page,"音乐动作：平时与副歌分别设置");Note(page,"坐着晃身听歌：坐在小圆凳上轻轻摇头晃身、摆腿，双手自然放松。轻微挥棒：胸前小幅摆动应援棒。活力挥棒：抬手举棒与小幅踮脚。三种动作都保留耳机，三套 Q 版支持。");
   MusicActionChoice(page,"平时播放时",normalActions,MusicReactions.GlobalAction(pet.Settings,false),delegate(string v){pet.Settings.MusicNormalAction=v;pet.Save();});
   MusicActionChoice(page,"进入副歌时",chorusActions,MusicReactions.GlobalAction(pet.Settings,true),delegate(string v){pet.Settings.MusicChorusAction=v;pet.Save();});
   musicRuleSummary=CompanionControls.Text("",12);page.Children.Add(musicRuleSummary);Note(page,"「坐着听歌＋偶尔轻微应援」以坐着听歌为主（约 45 秒），偶尔起身轻微挥棒（约 15 秒），再坐下继续听；只有播放稳定且没有其他互动时出现。也可以固定一种动作。例如：平时选择「轻微挥棒」，副歌选择「活力挥棒」，就会平时轻轻应援、高潮时举棒。选择「保持平时动作」则整首歌保持同一种动作。旧版设置会保留原来的出现规则。");
   Check(page,"自动查询副歌时间（仅发送歌曲 ID）",pet.Settings.MusicChorusEnabled,delegate(bool v){pet.Settings.MusicChorusEnabled=v;pet.Save();});Note(page,"没有副歌数据时继续平时动作，不按音量猜测高潮。关闭自动查询后，手动标记的副歌仍有效。连续播放约 8 秒后进入音乐动作；短暂停顿收棒，暂停超过 3 秒退出。拖动、睡眠、免打扰和 ChatGPT 任务优先；减少动态效果时不自动播放音乐动作。");
   TitleText(page,"只为当前歌曲覆盖设置");musicReactionStatus=CompanionControls.Text("",12);page.Children.Add(musicReactionStatus);
   songNormalAction=MusicActionChoice(page,"这首歌 · 平时播放时",new[]{"inherit"}.Concat(normalActions).ToArray(),"inherit",delegate(string v){pet.SetSongMusicAction(false,v);});
   songChorusAction=MusicActionChoice(page,"这首歌 · 进入副歌时",new[]{"inherit"}.Concat(chorusActions).ToArray(),"inherit",delegate(string v){pet.SetSongMusicAction(true,v);});
   chorusStart=Button("标记副歌起点（当前位置）",delegate{pet.MarkMusicChorus(false);UpdateMusicReactionSettings();},false);chorusEnd=Button("标记副歌终点（当前位置）",delegate{pet.MarkMusicChorus(true);UpdateMusicReactionSettings();},false);chorusClear=Button("清除本曲手动副歌",delegate{pet.ClearMusicChorus();UpdateMusicReactionSettings();},false);page.Children.Add(chorusStart);page.Children.Add(chorusEnd);page.Children.Add(chorusClear);Note(page,"先标记起点，再在终点标记；手动区间优先于接口数据。音乐栏右键也可分别设置两个阶段。最多记住 100 首歌，随通用偏好导出、导入。");
   TitleText(page,"音乐动作预览");foreach(string value in new[]{"quiet","gentle","lively"}){string captured=value;page.Children.Add(Button("预览 · "+MusicReactions.Label(value),delegate{pet.PreviewMusicAnimation(captured);},false));}UpdateMusicReactionSettings();
  }
  void UpdateMusicReactionSettings(){if(musicRuleSummary!=null)musicRuleSummary.Text="整体规则："+MusicReactions.RuleText(pet.Settings,"");if(musicReactionStatus==null||songNormalAction==null||songChorusAction==null||chorusStart==null)return;musicReactionStatus.Text=pet.MusicReactionStatus;musicReactionStatus.Foreground=muted;bool available=pet.Music.Current.Connected&&MusicReactions.SongId(pet.Music.Current.Id);songNormalAction.IsEnabled=songChorusAction.IsEnabled=chorusStart.IsEnabled=chorusEnd.IsEnabled=chorusClear.IsEnabled=available;var p=MusicReactions.Preference(pet.Settings,pet.Music.Current.Id);syncingMusicReactions=true;try{songNormalAction.SelectedIndex=Array.IndexOf(new[]{"inherit"}.Concat(normalActions).ToArray(),MusicReactions.SongActionSetting(p,false));songChorusAction.SelectedIndex=Array.IndexOf(new[]{"inherit"}.Concat(chorusActions).ToArray(),MusicReactions.SongActionSetting(p,true));}finally{syncingMusicReactions=false;}}
 }
}
