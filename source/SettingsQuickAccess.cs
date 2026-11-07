using System;using System.Linq;using System.Diagnostics;using System.Windows;using System.Windows.Controls;using System.Windows.Media;using System.Windows.Threading;
namespace KianaPet {
 public sealed partial class SettingsWindow {
  TextBlock commonGPTStatus,commonMusicStatus,appearancePreviewLabel;
  Button commonConnectGPT,commonConnectMusic,appearancePreviewPlay;
  Border appearancePreviewSurface;Image appearancePreviewImage;
  ComboBox appearancePreviewSkin,appearancePreviewAction;
  readonly DispatcherTimer appearancePreviewTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(80)};
  readonly Stopwatch appearancePreviewClock=new Stopwatch();
  AnimationPreviewSource appearancePreviewSource;string appearancePreviewSourceSkin,appearancePreviewSourceStyle,lastActualPreviewSkin;
  bool appearancePreviewPaused,appearancePreviewClosed;
  string AppearancePreviewSkinId{get{var info=appearancePreviewSkin==null?null:appearancePreviewSkin.SelectedItem as PetInfo;return info==null?"":info.Id;}}

  void BuildCommonConnections(Panel page){
   TitleText(page,"快捷连接");var buttons=new WrapPanel();
   commonConnectMusic=Button("打开 / 连接网易云音乐",pet.LaunchMusic,true);
   commonConnectGPT=Button("打开 / 连接 ChatGPT",pet.LaunchChatGPT,true);
   buttons.Children.Add(commonConnectMusic);buttons.Children.Add(commonConnectGPT);page.Children.Add(buttons);
   commonMusicStatus=CompanionControls.Text("",12);commonGPTStatus=CompanionControls.Text("",12);
   page.Children.Add(commonMusicStatus);page.Children.Add(commonGPTStatus);
   Note(page,"按当前连接方式打开或连接应用。连接方式可在“音乐”和“ChatGPT 联动”页调整。");
  }

  void BuildAppearancePreview(Panel page){
   TitleText(page,"外观与动作预览");
   appearancePreviewSkin=new ComboBox{ItemsSource=pet.Pets,DisplayMemberPath="Name",SelectedValuePath="Id",SelectedValue=pet.Settings.Skin,Padding=new Thickness(6),Margin=new Thickness(0,0,0,8)};
   appearancePreviewAction=new ComboBox{SelectedValuePath="Tag",Padding=new Thickness(6),Margin=new Thickness(0,0,0,8)};
   System.Windows.Automation.AutomationProperties.SetName(appearancePreviewSkin,"预览衣装");
   System.Windows.Automation.AutomationProperties.SetName(appearancePreviewAction,"预览动作");
   appearancePreviewImage=new Image{Width=144,Height=156,Stretch=Stretch.Fill,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
   RenderOptions.SetBitmapScalingMode(appearancePreviewImage,BitmapScalingMode.HighQuality);
   var grid=new Grid();grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(164)});grid.ColumnDefinitions.Add(new ColumnDefinition());
   grid.Children.Add(appearancePreviewImage);var controls=new StackPanel{VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(controls,1);grid.Children.Add(controls);
   controls.Children.Add(appearancePreviewSkin);controls.Children.Add(appearancePreviewAction);
   var actions=new WrapPanel();appearancePreviewPlay=Button("暂停预览",delegate{appearancePreviewPaused=!appearancePreviewPaused;UpdateAppearancePreviewPlayback();},false);
   actions.Children.Add(appearancePreviewPlay);actions.Children.Add(Button("应用这套衣装",delegate{var id=Convert.ToString(appearancePreviewSkin.SelectedValue);if(pet.Pets.Any(p=>p.Id==id)){pet.SelectSkin(id);UpdateAppearancePreviewStatus();}},true));controls.Children.Add(actions);
   appearancePreviewLabel=CompanionControls.Text("",12);controls.Children.Add(appearancePreviewLabel);
   appearancePreviewSurface=new Border{Child=grid,Padding=new Thickness(12),Margin=new Thickness(0,0,0,6),MinHeight=190,CornerRadius=new CornerRadius(PetVisuals.SurfaceRadius),BorderThickness=new Thickness(1)};page.Children.Add(appearancePreviewSurface);
   var more=new WrapPanel();more.Children.Add(Button("打开完整动作图鉴",delegate{NavigateCategory("效果预览","");},false));page.Children.Add(more);
   Note(page,"切换衣装和动作只影响预览；点击“应用这套衣装”才保存。听歌动作仅三套 Q 版提供。");
   appearancePreviewSkin.SelectionChanged+=delegate{RefreshAppearancePreviewActions();ResetAppearancePreview();};
   appearancePreviewAction.SelectionChanged+=delegate{ResetAppearancePreview();};
   appearancePreviewTimer.Tick+=delegate{DrawAppearancePreview();};
   appearancePreviewSurface.IsVisibleChanged+=delegate{UpdateAppearancePreviewPlayback();};
   Closed+=delegate{appearancePreviewClosed=true;appearancePreviewTimer.Stop();appearancePreviewClock.Stop();appearancePreviewSource=null;appearancePreviewImage.Source=null;};
   lastActualPreviewSkin=pet.Settings.Skin;RefreshAppearancePreviewActions();UpdateAppearancePreviewStatus();
  }
  void RefreshAppearancePreviewActions(){
   string selected=Convert.ToString(appearancePreviewAction.SelectedValue),skin=AppearancePreviewSkinId;
   appearancePreviewAction.ItemsSource=AnimationGallery.Catalog.Where(c=>c.Category!="音乐"||MusicSprites.Supported(skin)).Select(c=>new ComboBoxItem{Content=c.Name,Tag=c.Id}).ToArray();
   appearancePreviewAction.SelectedValue=AnimationGallery.Catalog.Any(c=>c.Id==selected&&(c.Category!="音乐"||MusicSprites.Supported(skin)))?selected:"idle";
  }
  void ResetAppearancePreview(){appearancePreviewClock.Reset();UpdateAppearancePreviewStatus();}
  void UpdateAppearancePreviewPlayback(){
   bool play=!appearancePreviewClosed&&appearancePreviewSurface!=null&&appearancePreviewSurface.IsVisible&&!appearancePreviewPaused&&MotionSettings.Enabled;
   if(play){appearancePreviewClock.Start();appearancePreviewTimer.Start();}else{appearancePreviewClock.Stop();appearancePreviewTimer.Stop();}
   if(appearancePreviewPlay!=null){appearancePreviewPlay.Content=appearancePreviewPaused?"播放预览":"暂停预览";appearancePreviewPlay.IsEnabled=MotionSettings.Enabled;}
  }
  void UpdateAppearancePreviewStatus(){
   if(appearancePreviewSurface==null||appearancePreviewClosed)return;
   if(lastActualPreviewSkin!=pet.Settings.Skin){lastActualPreviewSkin=pet.Settings.Skin;appearancePreviewSkin.SelectedValue=pet.Settings.Skin;}
   appearancePreviewSurface.Background=PetPalette.Brush(PetPalette.Background);appearancePreviewSurface.BorderBrush=PetPalette.Brush(PetPalette.Line);
   appearancePreviewLabel.Foreground=muted;appearancePreviewLabel.Text=AppearancePreviewSkinId==pet.Settings.Skin?"这套衣装正在使用":"仅预览，尚未应用";
   UpdateAppearancePreviewPlayback();DrawAppearancePreview();
  }
  void DrawAppearancePreview(){
   if(appearancePreviewClosed||appearancePreviewSurface==null||!appearancePreviewSurface.IsVisible)return;
   var choice=AnimationGallery.Catalog.FirstOrDefault(c=>c.Id==Convert.ToString(appearancePreviewAction.SelectedValue));string skin=AppearancePreviewSkinId;if(choice==null||string.IsNullOrEmpty(skin))return;
   try{if(appearancePreviewSource==null||appearancePreviewSourceSkin!=skin||appearancePreviewSourceStyle!=pet.Settings.MusicSeatedStyle){appearancePreviewSource=new AnimationPreviewSource(skin,pet.Settings.MusicSeatedStyle);appearancePreviewSourceSkin=skin;appearancePreviewSourceStyle=pet.Settings.MusicSeatedStyle;}
    double elapsed=MotionSettings.Enabled?appearancePreviewClock.Elapsed.TotalMilliseconds*AnimationTiming.Speed(pet.Settings,choice.Id):0;
    var pose=appearancePreviewSource.Frame(choice,elapsed);if(pose==null)return;appearancePreviewImage.Source=pose.Source;
    var transforms=new TransformGroup();transforms.Children.Add(new MatrixTransform(SpriteGeometry.Matrix(pose.Alignment,appearancePreviewImage.Width)));if(pose.Angle!=0)transforms.Children.Add(new RotateTransform(pose.Angle,appearancePreviewImage.Width*.46,appearancePreviewImage.Width*1.03));appearancePreviewImage.RenderTransform=transforms;
   }catch(Exception){appearancePreviewTimer.Stop();appearancePreviewLabel.Text="此动作暂无法预览，请尝试其他动作。";}
  }
  void UpdateQuickAccess(){
   if(commonGPTStatus!=null){commonGPTStatus.Text="ChatGPT："+pet.StatusText;commonGPTStatus.Foreground=muted;commonConnectGPT.ToolTip=pet.Link.Current.Hint;}
   if(commonMusicStatus!=null){commonMusicStatus.Text="网易云："+pet.MusicStatus;commonMusicStatus.Foreground=muted;commonConnectMusic.ToolTip=pet.MusicStatus;}
   UpdateAppearancePreviewStatus();
  }
 }
}
