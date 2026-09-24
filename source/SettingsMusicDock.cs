using System;using System.Windows;using System.Windows.Controls;
namespace KianaPet {
 public sealed partial class SettingsWindow {
  void BuildDefaultMusicDock(Panel page){
   Note(page,"恢复音乐栏时使用的默认位置");var defaultDock=new ComboBox{ItemsSource=new[]{"宠物下方","宠物左侧","宠物右侧"},SelectedIndex=Array.IndexOf(new[]{"below","left","right"},pet.Settings.MusicDefaultPosition),Padding=new Thickness(8),Margin=new Thickness(0,0,0,8)};System.Windows.Automation.AutomationProperties.SetName(defaultDock,"音乐栏默认停靠位置");defaultDock.SelectionChanged+=delegate{if(defaultDock.SelectedIndex<0)return;pet.Settings.MusicDefaultPosition=new[]{"below","left","right"}[defaultDock.SelectedIndex];pet.ApplySettings();};page.Children.Add(defaultDock);page.Children.Add(Button("恢复音乐栏到默认位置",pet.ResetMusicPosition,false));Note(page,"初始默认为宠物下方。右键 → 音乐与工具栏可一键恢复默认位置，或直接放回下方 / 左侧 / 右侧；恢复后退出手动固定位置模式，继续避开工具栏和屏幕边缘。设置默认位置不会立即移动音乐栏。");
  }
 }
}
