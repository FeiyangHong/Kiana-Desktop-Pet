using System;
using System.Windows;
using System.Windows.Controls;
namespace KianaPet {
 public sealed partial class SettingsWindow {
  readonly TextBlock hotkeyStatus=new TextBlock(),hotkeyFeedback=new TextBlock();
  void BuildHotkeyPage(TabControl tabs){
   StackPanel page=Page(tabs,"快捷键");TitleText(page,"全局快捷键");
   Note(page,"默认关闭。启用后，即使正在使用其他应用，也可以用自定义组合键找回宠物。关闭后仍可双击托盘图标找回。");
   CheckBox enable=new CheckBox{Content="启用找回宠物快捷键",IsChecked=pet.Settings.RecoverHotkeyEnabled,Margin=new Thickness(0,8,0,16)};page.Children.Add(enable);
   StackPanel row=new StackPanel{Orientation=Orientation.Horizontal};
   CheckBox ctrl=new CheckBox{Content="Ctrl",IsChecked=(pet.Settings.RecoverHotkeyModifiers&PetHotkey.Control)!=0,Margin=new Thickness(0,8,18,8)};
   CheckBox alt=new CheckBox{Content="Alt",IsChecked=(pet.Settings.RecoverHotkeyModifiers&PetHotkey.Alt)!=0,Margin=new Thickness(0,8,18,8)};
   CheckBox shift=new CheckBox{Content="Shift",IsChecked=(pet.Settings.RecoverHotkeyModifiers&PetHotkey.Shift)!=0,Margin=new Thickness(0,8,18,8)};
   ComboBox key=new ComboBox{Width=90,Padding=new Thickness(8,4,8,4),Margin=new Thickness(0,0,0,8)};
   for(uint k=0x41;k<=0x5a;k++)key.Items.Add(new ComboBoxItem{Content=PetHotkey.KeyName(k),Tag=k});
   for(uint k=0x30;k<=0x39;k++)key.Items.Add(new ComboBoxItem{Content=PetHotkey.KeyName(k),Tag=k});
   for(uint k=0x70;k<=0x7a;k++)key.Items.Add(new ComboBoxItem{Content=PetHotkey.KeyName(k),Tag=k});
   foreach(ComboBoxItem item in key.Items)if((uint)item.Tag==pet.Settings.RecoverHotkeyKey)key.SelectedItem=item;
   row.Children.Add(ctrl);row.Children.Add(alt);row.Children.Add(shift);row.Children.Add(key);page.Children.Add(row);
   bool updating=false;
   Action<bool> save=delegate(bool enabled){
    uint modifiers=(ctrl.IsChecked==true?PetHotkey.Control:0)|(alt.IsChecked==true?PetHotkey.Alt:0)|(shift.IsChecked==true?PetHotkey.Shift:0);
    uint selected=key.SelectedItem==null?0:(uint)((ComboBoxItem)key.SelectedItem).Tag;
    string error;
    // Turning off must always work, even with an incomplete draft.
    if(!enabled){modifiers=pet.Settings.RecoverHotkeyModifiers;selected=pet.Settings.RecoverHotkeyKey;}
    if(pet.SetHotkey(enabled,modifiers,selected,out error)){hotkeyFeedback.Text=enabled?"已生效。":"已关闭，原快捷键已释放。";}
    else{hotkeyFeedback.Text=error+"\n未替换原有配置。";}
    updating=true;enable.IsChecked=pet.Settings.RecoverHotkeyEnabled;updating=false;hotkeyStatus.Text=pet.HotkeyStatus;
   };
   enable.Checked+=delegate{if(!updating)save(true);};enable.Unchecked+=delegate{if(!updating)save(false);};
   page.Children.Add(Button("保存组合键",delegate{
    if(enable.IsChecked==true){save(true);return;}
    uint modifiers=(ctrl.IsChecked==true?PetHotkey.Control:0)|(alt.IsChecked==true?PetHotkey.Alt:0)|(shift.IsChecked==true?PetHotkey.Shift:0);
    uint selected=key.SelectedItem==null?0:(uint)((ComboBoxItem)key.SelectedItem).Tag;
    if(!PetHotkey.Valid(modifiers,selected)){hotkeyFeedback.Text="请选择 Ctrl 或 Alt，搭配一个主键。";return;}
    string error;pet.SetHotkey(false,modifiers,selected,out error);hotkeyFeedback.Text="组合键已保存；保持关闭，不占用按键。";
   },true));
   hotkeyStatus.TextWrapping=TextWrapping.Wrap;hotkeyStatus.Foreground=purple;hotkeyStatus.Margin=new Thickness(0,14,0,8);page.Children.Add(hotkeyStatus);
   hotkeyFeedback.TextWrapping=TextWrapping.Wrap;hotkeyFeedback.Foreground=muted;page.Children.Add(hotkeyFeedback);
   Note(page,"支持 Ctrl / Alt / Shift 组合与字母、数字、F1–F11，至少包含 Ctrl 或 Alt。保存后立即生效，无需重启。注册失败会保留此前的快捷键。\n\n可检测其他程序注册的全局快捷键冲突；无法自动检测各应用内部的快捷键。请避开 Ctrl + Shift + P 等常用组合。\n\n这里管理独立桌宠的全局快捷键。ChatGPT、网易云音乐自己的快捷键仍在各自应用中设置。菜单或联动面板里的 Esc 是局部收起操作，不全局占用。");
  }
 }
}
