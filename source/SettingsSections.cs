using System;using System.Linq;using System.Collections.Generic;using System.Windows;using System.Windows.Controls;using System.Windows.Media;
namespace KianaPet {
 public sealed partial class SettingsWindow {
  readonly Dictionary<Expander,bool> sectionSearchState=new Dictionary<Expander,bool>();
  StackPanel Fold(Panel parent,string title,bool expanded){var body=new StackPanel{Margin=new Thickness(6,8,6,10)};var fold=new Expander{Header=title,Content=body,IsExpanded=expanded,Padding=new Thickness(12,8,12,8),Margin=new Thickness(0,5,0,6),HorizontalContentAlignment=HorizontalAlignment.Stretch};System.Windows.Automation.AutomationProperties.SetName(fold,title);parent.Children.Add(fold);return body;}
  public static IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject {foreach(object obj in LogicalTreeHelper.GetChildren(root)){var child=obj as DependencyObject;if(child==null)continue;var match=child as T;if(match!=null)yield return match;foreach(var nested in Descendants<T>(child))yield return nested;}}
  void FoldSections(StackPanel page,Func<string,bool> shouldFold){var groups=Sections(page);foreach(var group in groups){var title=group.FirstOrDefault() as TextBlock;if(title==null||title.FontSize<16||!shouldFold(title.Text))continue;int index=page.Children.IndexOf(title);string label=title.Text;var body=new StackPanel{Margin=new Thickness(6,8,6,8)};foreach(var item in group){page.Children.Remove(item);if(item!=title)body.Children.Add(item);}var fold=new Expander{Header=label,Content=body,IsExpanded=false,Padding=new Thickness(12,8,12,8),Margin=new Thickness(0,5,0,6),HorizontalContentAlignment=HorizontalAlignment.Stretch};page.Children.Insert(index,fold);}}
  void ArrangeSettings(TabControl tabs){
   var music=(StackPanel)((ScrollViewer)tabs.Items.Cast<TabItem>().Single(t=>(string)t.Header=="音乐").Content).Content;
   var launch=music.Children.OfType<Button>().Single(b=>(string)b.Content=="连接网易云音乐");music.Children.Remove(launch);music.Children.Remove(musicStatus);var connect=new StackPanel();TitleText(connect,"网易云音乐连接");launch.Content="打开 / 连接网易云音乐";System.Windows.Automation.AutomationProperties.SetName(launch,"打开 / 连接网易云音乐");connect.Children.Add(launch);connect.Children.Add(musicStatus);Note(connect,"播放控制、歌曲信息与歌词共用此连接。");music.Children.Insert(0,connect);
   var help=music.Children.OfType<TextBlock>().FirstOrDefault(x=>x.Text.StartsWith("点击上方“连接网易云音乐”"));if(help!=null){music.Children.Remove(help);help.Text=help.Text.Replace("点击上方“连接网易云音乐”","点击上方“打开 / 连接网易云音乐”");Fold(connect,"连接方式与歌词说明",false).Children.Add(help);}
   var groups=Sections(music);var basic=groups.First(g=>g.FirstOrDefault() is TextBlock&&((TextBlock)g[0]).Text=="网易云音乐小卡片");int at=1;foreach(var item in basic){music.Children.Remove(item);music.Children.Insert(at++,item);}var toggles=basic.OfType<CheckBox>().ToArray();if(toggles.Length>0){int index=music.Children.IndexOf(toggles[0]);var compact=new System.Windows.Controls.Primitives.UniformGrid{Columns=2};compact.SizeChanged+=delegate{compact.Columns=compact.ActualWidth>=550?2:1;};foreach(var toggle in toggles){music.Children.Remove(toggle);compact.Children.Add(toggle);}music.Children.Insert(index,compact);}FoldSections(music,label=>label!="网易云音乐小卡片");
   var link=(StackPanel)((ScrollViewer)tabs.Items.Cast<TabItem>().Single(t=>(string)t.Header=="ChatGPT 联动").Content).Content;var open=link.Children.OfType<Button>().Single(b=>(string)b.Content=="打开 ChatGPT");var repair=link.Children.OfType<Button>().Single(b=>(string)b.Content=="检查并修复联动");link.Children.Remove(open);link.Children.Remove(repair);var actions=new WrapPanel();actions.Children.Add(open);actions.Children.Add(repair);link.Children.Insert(1,actions);link.Children.Remove(status);link.Children.Insert(2,status);
   var details=new List<UIElement>();foreach(var item in link.Children.Cast<UIElement>().ToArray()){var note=item as TextBlock;var button=item as Button;if(item==connectionHint||note!=null&&note.Text.StartsWith("点击下方")||button!=null&&(string)button.Content=="打开运行记录目录")details.Add(item);}foreach(var item in details)link.Children.Remove(item);var body=Fold(link,"连接详情与后台 Mini 说明",false);foreach(var item in details){var note=item as TextBlock;if(note!=null)note.Text=note.Text.Replace("点击下方“打开 ChatGPT”","点击上方“打开 ChatGPT”");body.Children.Add(item);}FoldSections(link,label=>label=="为未来预留");
   var appearance=(StackPanel)((ScrollViewer)tabs.Items.Cast<TabItem>().Single(t=>(string)t.Header=="外观与互动").Content).Content;appearance.Children.Insert(0,Button("工具栏配色、渐变与毛玻璃",()=>NavigateCategory("工具栏样式",""),false));
  }
  StackPanel SettingsPage(TabControl tabs,string title){return (StackPanel)((ScrollViewer)tabs.Items.Cast<TabItem>().Single(t=>(string)t.Header==title).Content).Content;}
  void MoveSection(StackPanel from,StackPanel to,string title,int index){var group=Sections(from).First(g=>g[0] is TextBlock&&((TextBlock)g[0]).Text==title);foreach(var item in group)from.Children.Remove(item);foreach(var item in group)to.Children.Insert(index++,item);}
  void PromoteButton(StackPanel page,string title,int index){var button=page.Children.OfType<Button>().Single(x=>(string)x.Content==title);page.Children.Remove(button);page.Children.Insert(index,button);}
  List<List<UIElement>> SectionsForPanel(Panel page){var stack=page as StackPanel;return stack!=null?Sections(stack):new List<List<UIElement>>{page.Children.Cast<UIElement>().ToList()};}
  void FoldLongHelp(Panel page){
   // Only static explanatory notes are marked; live status/error text must stay visible.
   foreach(var group in SectionsForPanel(page)){var notes=group.OfType<TextBlock>().Where(x=>Equals(x.Tag,"settings-help")&&(x.Text.Length>=100||x.Text.Count(c=>c=='\n')>=3)).ToArray();
   if(notes.Length>0){int index=page.Children.IndexOf(notes[0]);var body=Fold(page,"详细说明",false);var fold=(Expander)page.Children[page.Children.Count-1];page.Children.Remove(fold);page.Children.Insert(index,fold);foreach(var note in notes){page.Children.Remove(note);body.Children.Add(note);}}}
   foreach(var child in page.Children.Cast<UIElement>().ToArray()){var fold=child as Expander;if(fold!=null){if((string)fold.Header!="详细说明"&&fold.Content is Panel)FoldLongHelp((Panel)fold.Content);}else if(child is Panel)FoldLongHelp((Panel)child);}
  }
  void ArrangeAllSettings(TabControl tabs){
   var scenes=SettingsPage(tabs,"场景与维护");var about=SettingsPage(tabs,"使用说明");var notices=SettingsPage(tabs,"通知中心");
   MoveSection(scenes,scenes,"仓库更新",0);var repositoryDetails=scenes.Children.OfType<Expander>().Single(x=>(string)x.Header=="仓库目录与更新说明");scenes.Children.Remove(repositoryDetails);scenes.Children.Insert(3,repositoryDetails);
   // Transfer and local notification controls live with the related task, not in About.
   MoveSection(about,scenes,"双设备偏好与备份",scenes.Children.Count);
   MoveSection(about,notices,"任务完成通知接口（预留）",notices.Children.Count);
   var duplicate=scenes.Children.OfType<CheckBox>().Single(x=>(string)x.Content=="普通任务完成静默，仅保留铃铛提示");scenes.Children.Remove(duplicate);
   MoveSection(scenes,notices,"通知节奏",notices.Children.Count);
   FoldSections(scenes,label=>label!="仓库更新");
   PromoteButton(notices,"打开任务通知中心",1);
   MoveSection(notices,notices,"通知测试",2);
   FoldSections(notices,label=>label!="任务与提醒"&&label!="提醒方式"&&label!="通知测试");
   var startup=SettingsPage(tabs,"启动与联动");MoveSection(startup,startup,"按需打开",0);
   var accessibility=SettingsPage(tabs,"无障碍");var keys=new WrapPanel();foreach(var button in accessibility.Children.OfType<Button>().ToArray()){accessibility.Children.Remove(button);keys.Children.Add(button);}accessibility.Children.Add(keys);
   var hotkeys=SettingsPage(tabs,"快捷键");hotkeys.Children.Remove(hotkeyStatus);hotkeys.Children.Insert(1,hotkeyStatus);
   var quiet=SettingsPage(tabs,"免打扰");FoldSections(quiet,label=>label!="暂时安静一下");
   foreach(var fold in quiet.Children.OfType<Expander>()){bool active=(string)fold.Header=="按应用自动免打扰"?pet.Settings.QuietAppsEnabled:pet.Settings.QuietHoursEnabled;fold.IsExpanded=active;}
   var appearance=SettingsPage(tabs,"外观与互动");FoldSections(appearance,label=>label=="界面与互动");
   var common=SettingsPage(tabs,"常用");common.Children.Add(Button("拉取更新 / 构建本机修改",()=>NavigateCategory("场景与维护",""),false));
   foreach(TabItem tab in tabs.Items){string category=(string)tab.Header;if(category=="效果预览"||category=="最近调整"||category=="工具栏样式")continue;FoldLongHelp(SettingsPage(tabs,category));}
  }
  void SearchExpand(DependencyObject root,string query){foreach(var fold in Descendants<Expander>(root)){if(query.Length==0){bool old;if(sectionSearchState.TryGetValue(fold,out old)){fold.IsExpanded=old;sectionSearchState.Remove(fold);}}else if(Matches(SearchText(fold),query)){if(!sectionSearchState.ContainsKey(fold))sectionSearchState[fold]=fold.IsExpanded;fold.IsExpanded=true;}}}
 }
}
