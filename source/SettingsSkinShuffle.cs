using System;using System.Linq;using System.Collections.Generic;using System.Windows;using System.Windows.Controls;
namespace KianaPet {
 public sealed partial class SettingsWindow {
  TextBlock skinShuffleStatus;TextBox skinShuffleMinutes;Expander skinShuffleSection;
  CheckBox skinShuffleTimed,skinShuffleStartup,skinShuffleJitter;
  Button skinShuffleNow,skinShuffleCancel;
  readonly Dictionary<string,CheckBox> skinShuffleChoices=new Dictionary<string,CheckBox>();
  readonly Dictionary<string,CheckBox> skinActionChoices=new Dictionary<string,CheckBox>();
  bool syncingSkinShuffle;int displayedShuffleMinutes;
  void BuildSkinShuffleSettings(StackPanel appearance){
   var body=Fold(appearance,"随机换装",false);
   skinShuffleSection=(Expander)appearance.Children[appearance.Children.Count-1];appearance.Children.Remove(skinShuffleSection);
   appearance.Children.Insert(0,skinShuffleSection);
   // Keep the primary action and live status outside the optional details.
   var quick=new StackPanel();appearance.Children.Insert(0,quick);TitleText(quick,"随机换装");
   skinShuffleStatus=CompanionControls.Text("",13);skinShuffleStatus.Foreground=ink;quick.Children.Add(skinShuffleStatus);
   var requestActions=new WrapPanel();skinShuffleNow=Button("立即随机换装",delegate{pet.RequestSkinShuffle();UpdateSkinShuffleStatus();},true);skinShuffleCancel=Button("取消本次请求",delegate{pet.CancelSkinShuffleRequest();UpdateSkinShuffleStatus();},false);requestActions.Children.Add(skinShuffleNow);requestActions.Children.Add(skinShuffleCancel);quick.Children.Add(requestActions);
   Note(body,"立即按钮只排队一次，不更改自动开关或今天固定。设置打开时也可执行，仍等待安全的动作衔接点；GPT 已结束但待查看时会保留原动作并在循环衔接点换装；连续点击不会累积多次换装。手动选衣装、修改随机设置或点击今天固定会取消待执行请求。");
   TitleText(body,"换装后的动作（手动与随机共用，可多选）");var actionChoices=new WrapPanel();
   foreach(string action in SkinChangeActions.Allowed){string id=action;var box=new CheckBox{Content=SkinChangeActions.Label(id),Foreground=ink,Width=150,Margin=new Thickness(0,6,8,6),IsChecked=pet.Settings.SkinShuffleActions.Contains(id)};skinActionChoices[id]=box;RoutedEventHandler update=delegate{if(syncingSkinShuffle)return;pet.Settings.SkinShuffleActions=SkinChangeActions.Allowed.Where(a=>skinActionChoices.ContainsKey(a)&&skinActionChoices[a].IsChecked==true).ToArray();pet.CancelSkinShuffleRequest();pet.StopOutfitAction();pet.Save();UpdateSkinShuffleStatus();};box.Checked+=update;box.Unchecked+=update;actionChoices.Children.Add(box);}body.Children.Add(actionChoices);
   Note(body,"默认勾选招手、开心回应、整理头发、摸头和伸懒腰；多选后随机播放一个，尽量不连续重复。全部取消只保留随机气泡。听歌坐姿使用已有的起身 → 所选动作 → 坐下过渡，随后回到当前听歌相位；播放进度与副歌判断继续运行，任务、副歌动作或拖动可立即打断。手动与随机换装共用此动作池及听歌坐姿衔接。减少动态效果时不播放动作。普通版沿用动作预览中的已有回退姿态。");
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
   Note(body,"默认每 60 分钟左右更换，浮动开启时约 54–66 分钟。只累计桌宠可见、清醒的陪伴时间，隐藏或锁屏不补计；到时等待待机或平稳坐姿的循环衔接点。拖动、互动、应援、已知副歌、GPT 活动任务、专注及睡眠期间暂缓；设置窗口只阻止未主动请求的定时换装。候选打乱后按轮使用，避免紧接着重复当前皮肤；手动换装或调整随机设置后重新计时。\n自动换装按所选动作展示并随机显示气泡，不移动窗口或重置音乐会话、轮询；包含普通版候选时听歌期间等待日常待机，避免丢失音乐动作。启动随机在首次显示前完成。今天固定会保留到当天结束，跨日后恢复完整间隔；计时、剩余候选与当天固定只存本机，开关、间隔和候选可随通用偏好迁移。");
  }
  void UpdateSkinShuffleStatus(){
   if(skinShuffleStatus==null)return;skinShuffleStatus.Text=pet.SkinShuffleStatus;skinShuffleNow.IsEnabled=pet.CanRequestSkinShuffle;skinShuffleCancel.IsEnabled=pet.SkinShuffleRequested;skinShuffleSection.Header="随机换装详细设置 · "+(pet.Settings.SkinShuffleEnabled?"按陪伴时间":pet.Settings.SkinShuffleOnStart?"仅启动时":"已关闭");
   syncingSkinShuffle=true;try{skinShuffleTimed.IsChecked=pet.Settings.SkinShuffleEnabled;skinShuffleStartup.IsChecked=pet.Settings.SkinShuffleOnStart;skinShuffleJitter.IsChecked=pet.Settings.SkinShuffleJitter;foreach(var choice in skinShuffleChoices)choice.Value.IsChecked=pet.Settings.SkinShufflePool.Contains(choice.Key);foreach(var choice in skinActionChoices)choice.Value.IsChecked=pet.Settings.SkinShuffleActions.Contains(choice.Key);if(displayedShuffleMinutes!=pet.Settings.SkinShuffleMinutes&&!skinShuffleMinutes.IsKeyboardFocusWithin){skinShuffleMinutes.Text=pet.Settings.SkinShuffleMinutes.ToString();displayedShuffleMinutes=pet.Settings.SkinShuffleMinutes;}}finally{syncingSkinShuffle=false;}
  }
 }
}
