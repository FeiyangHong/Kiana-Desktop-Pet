using System;using System.Linq;using System.Collections.Generic;using System.Windows;using System.Windows.Controls;
namespace KianaPet {
 public sealed partial class SettingsWindow {
  TextBlock skinShuffleStatus;TextBox skinShuffleMinutes;Expander skinShuffleSection;
  CheckBox skinShuffleTimed,skinShuffleStartup,skinShuffleJitter;
  Button skinShuffleNow,skinShuffleCancel;
  readonly Dictionary<string,CheckBox> skinShuffleChoices=new Dictionary<string,CheckBox>();
  bool syncingSkinShuffle;int displayedShuffleMinutes;
  void BuildSkinShuffleSettings(StackPanel appearance){
   var body=Fold(appearance,"随机换装",false);
   skinShuffleSection=(Expander)appearance.Children[appearance.Children.Count-1];appearance.Children.Remove(skinShuffleSection);
   int index=appearance.Children.IndexOf(appearance.Children.OfType<TextBlock>().Single(t=>t.Text=="今天穿哪一套？"));appearance.Children.Insert(index,skinShuffleSection);
   skinShuffleStatus=CompanionControls.Text("",13);skinShuffleStatus.Foreground=ink;body.Children.Add(skinShuffleStatus);
   var requestActions=new WrapPanel();skinShuffleNow=Button("立即随机换装",delegate{pet.RequestSkinShuffle();UpdateSkinShuffleStatus();},true);skinShuffleCancel=Button("取消本次请求",delegate{pet.CancelSkinShuffleRequest();UpdateSkinShuffleStatus();},false);requestActions.Children.Add(skinShuffleNow);requestActions.Children.Add(skinShuffleCancel);body.Children.Add(requestActions);
   Note(body,"立即按钮只排队一次，不更改自动开关或今天固定。设置打开时也可执行，仍等待安全的动作衔接点；GPT 已结束但待查看时会保留原动作并在循环衔接点换装；连续点击不会累积多次换装。手动选衣装、修改随机设置或点击今天固定会取消待执行请求。");
   skinShuffleTimed=Check(body,"按陪伴时间随机更换皮肤",pet.Settings.SkinShuffleEnabled,v=>{if(syncingSkinShuffle)return;pet.Settings.SkinShuffleEnabled=v;pet.UpdateSkinShuffleSettings();UpdateSkinShuffleStatus();});
   skinShuffleMinutes=NumberRow(body,"间隔（分钟，1–1440）",pet.Settings.SkinShuffleMinutes);displayedShuffleMinutes=pet.Settings.SkinShuffleMinutes;
   body.Children.Add(Button("保存随机间隔",delegate{int value;if(!int.TryParse(skinShuffleMinutes.Text,out value)||value<1||value>1440){MessageBox.Show(this,"请输入 1–1440 分钟。","间隔尚未保存");return;}pet.Settings.SkinShuffleMinutes=value;displayedShuffleMinutes=value;pet.UpdateSkinShuffleSettings();UpdateSkinShuffleStatus();},false));
   skinShuffleJitter=Check(body,"间隔加入少量随机浮动（±10%）",pet.Settings.SkinShuffleJitter,v=>{if(syncingSkinShuffle)return;pet.Settings.SkinShuffleJitter=v;pet.UpdateSkinShuffleSettings();UpdateSkinShuffleStatus();});
   skinShuffleStartup=Check(body,"每次启动时随机一套（可单独使用）",pet.Settings.SkinShuffleOnStart,v=>{if(syncingSkinShuffle)return;pet.Settings.SkinShuffleOnStart=v;pet.UpdateSkinShuffleSettings();UpdateSkinShuffleStatus();});
   Note(body,"参与随机的皮肤（默认仅三套 Q 版）");var choices=new WrapPanel();
   foreach(var info in pet.Pets){string id=info.Id;var box=new CheckBox{Content=info.Name,Foreground=ink,Width=250,Margin=new Thickness(0,6,8,6),IsChecked=pet.Settings.SkinShufflePool.Contains(id)};skinShuffleChoices[id]=box;
    RoutedEventHandler update=delegate{if(syncingSkinShuffle)return;pet.Settings.SkinShufflePool=pet.Pets.Where(p=>skinShuffleChoices.ContainsKey(p.Id)&&skinShuffleChoices[p.Id].IsChecked==true).Select(p=>p.Id).ToArray();pet.UpdateSkinShuffleSettings();UpdateSkinShuffleStatus();};box.Checked+=update;box.Unchecked+=update;choices.Children.Add(box);
   }body.Children.Add(choices);
   var actions=new WrapPanel();actions.Children.Add(Button("只选三套 Q 版",delegate{pet.Settings.SkinShufflePool=SkinShuffleRules.Defaults.ToArray();pet.UpdateSkinShuffleSettings();UpdateSkinShuffleStatus();},false));actions.Children.Add(Button("今天固定这套",delegate{pet.HoldSkinToday();UpdateSkinShuffleStatus();},false));actions.Children.Add(Button("恢复随机并重新计时",delegate{pet.ResumeSkinShuffle();UpdateSkinShuffleStatus();},false));body.Children.Add(actions);
   Note(body,"默认每 120 分钟左右更换，浮动开启时约 108–132 分钟。只累计桌宠可见、清醒的陪伴时间，隐藏或锁屏不补计；到时等待待机或平稳坐姿的循环衔接点。拖动、互动、应援、已知副歌、GPT 任务、专注、睡眠及设置操作期间暂缓。候选打乱后按轮使用，避免紧接着重复当前皮肤；手动换装或调整随机设置后重新计时。\n自动换装不挥手、不弹气泡、不移动位置，也不重置音乐会话或轮询；包含普通版候选时听歌期间等待日常待机，避免丢失音乐动作。启动随机在首次显示前完成。今天固定会保留到当天结束，跨日后恢复完整间隔；计时、剩余候选与当天固定只存本机，开关、间隔和候选可随通用偏好迁移。");
  }
  void UpdateSkinShuffleStatus(){
   if(skinShuffleStatus==null)return;skinShuffleStatus.Text=pet.SkinShuffleStatus;skinShuffleNow.IsEnabled=pet.CanRequestSkinShuffle;skinShuffleCancel.IsEnabled=pet.SkinShuffleRequested;skinShuffleSection.Header="随机换装 · "+(pet.Settings.SkinShuffleEnabled?"按陪伴时间":pet.Settings.SkinShuffleOnStart?"仅启动时":"已关闭");
   syncingSkinShuffle=true;try{skinShuffleTimed.IsChecked=pet.Settings.SkinShuffleEnabled;skinShuffleStartup.IsChecked=pet.Settings.SkinShuffleOnStart;skinShuffleJitter.IsChecked=pet.Settings.SkinShuffleJitter;foreach(var choice in skinShuffleChoices)choice.Value.IsChecked=pet.Settings.SkinShufflePool.Contains(choice.Key);if(displayedShuffleMinutes!=pet.Settings.SkinShuffleMinutes&&!skinShuffleMinutes.IsKeyboardFocusWithin){skinShuffleMinutes.Text=pet.Settings.SkinShuffleMinutes.ToString();displayedShuffleMinutes=pet.Settings.SkinShuffleMinutes;}}finally{syncingSkinShuffle=false;}
  }
 }
}
