using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
namespace KianaPet {
 public sealed partial class PetWindow {
  readonly Border hoverBar=new Border(),notificationBadge=new Border();
  readonly TextBlock notificationNumber=new TextBlock();
  Border notificationSeparator;Button chatButton,voiceButton,notificationButton;double toolbarUntil;bool toolbarBusy;ContextMenu toolbarMenu;
  bool toolbarShown;int toolbarTransition;readonly ScaleTransform toolbarScale=new ScaleTransform(.92,.92);readonly TranslateTransform toolbarSlide=new TranslateTransform(0,-4);
  public bool ToolbarVisible{get{return hoverBar.Visibility==Visibility.Visible;}}
  bool ToolbarActive{get{return keyboardToolbarMode||MusicInteractionActive||(toolbarMenu!=null&&toolbarMenu.IsOpen)||Settings.HoverToolbar&&(hoverBar.IsMouseOver||clock.Elapsed.TotalSeconds<toolbarUntil);}}
  void BuildToolbar(Grid root){
   hoverBar.CornerRadius=new CornerRadius(16);hoverBar.Background=new SolidColorBrush(Color.FromArgb(245,255,255,255));hoverBar.BorderThickness=new Thickness(0);hoverBar.Padding=new Thickness(4,0,4,0);hoverBar.Margin=new Thickness(2,2,2,4);hoverBar.HorizontalAlignment=HorizontalAlignment.Center;hoverBar.VerticalAlignment=VerticalAlignment.Center;hoverBar.Visibility=Visibility.Hidden;hoverBar.Opacity=0;hoverBar.IsHitTestVisible=false;hoverBar.UseLayoutRounding=true;hoverBar.SnapsToDevicePixels=true;
   TransformGroup transform=new TransformGroup();transform.Children.Add(toolbarScale);transform.Children.Add(toolbarSlide);hoverBar.RenderTransform=transform;hoverBar.RenderTransformOrigin=new Point(.5,0);
   StackPanel buttons=new StackPanel{Orientation=Orientation.Horizontal};hoverBar.Child=buttons;Grid.SetRow(hoverBar,2);root.Children.Add(hoverBar);
   chatButton=ToolbarButton("chat","打开 ChatGPT Mini 新聊天",delegate{RunToolbarAction("chat");});buttons.Children.Add(chatButton);buttons.Children.Add(ToolbarSeparator());
   voiceButton=ToolbarButton("voice","开始 ChatGPT 语音聊天（使用 ChatGPT 的麦克风权限）",delegate{RunToolbarAction("voice");});buttons.Children.Add(voiceButton);
   notificationSeparator=ToolbarSeparator();buttons.Children.Add(notificationSeparator);
   notificationButton=ToolbarButton("bell","查看 ChatGPT 通知 / 联动状态",OpenToolbarNotifications);
   Grid badgeContainer=new Grid();var bell=(UIElement)notificationButton.Content;notificationButton.Content=null;badgeContainer.Children.Add(bell);
   notificationBadge.Background=new SolidColorBrush(Color.FromRgb(65,123,238));notificationBadge.CornerRadius=new CornerRadius(7);notificationBadge.MinWidth=13;notificationBadge.Height=13;notificationBadge.Padding=new Thickness(2,0,2,0);notificationBadge.HorizontalAlignment=HorizontalAlignment.Right;notificationBadge.VerticalAlignment=VerticalAlignment.Top;notificationBadge.Margin=new Thickness(0,-3,-4,0);notificationBadge.IsHitTestVisible=false;notificationBadge.Visibility=Visibility.Hidden;
   notificationNumber.FontSize=9;notificationNumber.Foreground=Brushes.White;notificationNumber.TextAlignment=TextAlignment.Center;notificationBadge.Child=notificationNumber;badgeContainer.Children.Add(notificationBadge);notificationButton.Content=badgeContainer;buttons.Children.Add(notificationButton);
   root.MouseEnter+=delegate{toolbarUntil=clock.Elapsed.TotalSeconds+.65;};root.MouseLeave+=delegate{toolbarUntil=clock.Elapsed.TotalSeconds+.65;};
   hoverBar.MouseEnter+=delegate{toolbarUntil=clock.Elapsed.TotalSeconds+.65;};
  }
  Button ToolbarButton(string geometry,string tip,Action action){
   Image icon=ToolbarIcons.Create(geometry);
   Button button=new Button{Content=icon,Width=32,Height=32,Padding=new Thickness(4),Background=new SolidColorBrush(Colors.Transparent),BorderThickness=new Thickness(0),Cursor=Cursors.Hand,ToolTip=tip,Focusable=false};
   System.Windows.Automation.AutomationProperties.SetName(button,tip);ToolTipService.SetShowOnDisabled(button,true);
   var border=new FrameworkElementFactory(typeof(Border));border.SetValue(Border.CornerRadiusProperty,new CornerRadius(8));border.SetBinding(Border.BackgroundProperty,new System.Windows.Data.Binding("Background"){RelativeSource=new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)});
   var presenter=new FrameworkElementFactory(typeof(ContentPresenter));presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty,HorizontalAlignment.Center);presenter.SetValue(ContentPresenter.VerticalAlignmentProperty,VerticalAlignment.Center);border.AppendChild(presenter);
   ControlTemplate template=new ControlTemplate(typeof(Button)){VisualTree=border};Trigger disabled=new Trigger{Property=IsEnabledProperty,Value=false};disabled.Setters.Add(new Setter(OpacityProperty,.35));template.Triggers.Add(disabled);button.Template=template;
   var pressScale=new ScaleTransform(1,1);button.RenderTransform=pressScale;button.RenderTransformOrigin=new Point(.5,.5);
   button.MouseEnter+=delegate{AnimateButton(button,pressScale,true,false);};button.MouseLeave+=delegate{AnimateButton(button,pressScale,false,false);};button.PreviewMouseLeftButtonDown+=delegate{AnimateButton(button,pressScale,true,true);};button.PreviewMouseLeftButtonUp+=delegate{AnimateButton(button,pressScale,button.IsMouseOver,false);};
   button.Click+=delegate{toolbarUntil=clock.Elapsed.TotalSeconds+1;action();};return button;
  }
  static Border ToolbarSeparator(){return new Border{Width=1,Height=18,Margin=new Thickness(3.5,0,3.5,0),Background=new SolidColorBrush(Color.FromArgb(14,26,28,31))};}
  static DoubleAnimation Motion(double target,int milliseconds){return new DoubleAnimation(target,TimeSpan.FromMilliseconds(MotionSettings.Enabled?milliseconds:0)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}};}
  static void AnimateButton(Button button,ScaleTransform scale,bool over,bool down){
   SolidColorBrush fill=button.Background as SolidColorBrush;if(fill!=null)fill.BeginAnimation(SolidColorBrush.ColorProperty,new ColorAnimation(Color.FromArgb((byte)(down?24:over?13:0),26,28,31),TimeSpan.FromMilliseconds(MotionSettings.Enabled?110:0)));
   scale.BeginAnimation(ScaleTransform.ScaleXProperty,Motion(down?.92:1,110));scale.BeginAnimation(ScaleTransform.ScaleYProperty,Motion(down?.92:1,110));
  }
  void SetToolbarVisible(bool show,bool immediate){
   if(immediate){if(!toolbarShown&&hoverBar.Visibility==Visibility.Hidden)return;toolbarShown=false;toolbarTransition++;hoverBar.BeginAnimation(OpacityProperty,null);toolbarScale.BeginAnimation(ScaleTransform.ScaleXProperty,null);toolbarScale.BeginAnimation(ScaleTransform.ScaleYProperty,null);toolbarSlide.BeginAnimation(TranslateTransform.YProperty,null);hoverBar.Opacity=0;toolbarScale.ScaleX=toolbarScale.ScaleY=.92;toolbarSlide.Y=-4;hoverBar.IsHitTestVisible=false;hoverBar.Visibility=Visibility.Hidden;return;}
   if(show==toolbarShown)return;toolbarShown=show;int transition=++toolbarTransition;
   int revealDelay=0;if(show){if(musicWindow!=null&&!Settings.MusicHoverOnly){musicWindow.Follow(Bounds());if(musicWindow.DockMoving&&MotionSettings.Enabled)revealDelay=180;}hoverBar.Visibility=Visibility.Visible;hoverBar.IsHitTestVisible=true;}
   int duration=show?PetVisuals.ShowMs:PetVisuals.HideMs;DoubleAnimation fade=Motion(show?1:0,duration);if(revealDelay>0){hoverBar.BeginAnimation(OpacityProperty,null);hoverBar.Opacity=0;fade.BeginTime=TimeSpan.FromMilliseconds(revealDelay);}
   if(!show)fade.Completed+=delegate{if(transition==toolbarTransition&&!toolbarShown){hoverBar.Visibility=Visibility.Hidden;hoverBar.IsHitTestVisible=false;}};
   hoverBar.BeginAnimation(OpacityProperty,fade);toolbarScale.BeginAnimation(ScaleTransform.ScaleXProperty,Motion(show?1:.96,duration));toolbarScale.BeginAnimation(ScaleTransform.ScaleYProperty,Motion(show?1:.96,duration));toolbarSlide.BeginAnimation(TranslateTransform.YProperty,Motion(show?0:-3,duration));
  }
  void UpdateToolbar(double now){
   if(menuLayerYielding&&IsVisible&&!manualHidden&&!fullscreenHidden)return;
   if(hover||hoverBar.IsMouseOver)toolbarUntil=now+.65;
   bool show=Settings.HoverToolbar&&IsVisible&&!manualHidden&&!fullscreenHidden&&!pressed&&!dragging&&(Settings.ToolbarPinned||keyboardToolbarMode||(toolbarMenu!=null&&toolbarMenu.IsOpen)||hover||hoverBar.IsMouseOver||now<toolbarUntil||(Settings.MusicHoverOnly&&MusicInteractionActive));
   SetToolbarVisible(show,!Settings.HoverToolbar||!IsVisible||manualHidden||fullscreenHidden||pressed||dragging);
   bool live=Settings.LinkChatGPT&&Link.Current.Connected&&DateTime.UtcNow-Link.Current.At<TimeSpan.FromSeconds(10);
   voiceButton.IsEnabled=!toolbarBusy&&live&&Link.Current.CanVoice;chatButton.IsEnabled=!toolbarBusy;
   voiceButton.ToolTip=live&&Link.Current.CanVoice?"开始 ChatGPT 语音聊天（使用 ChatGPT 的麦克风权限）":"请通过平滑启动器打开 ChatGPT 并显示 Mini；语音需在 ChatGPT 内可用";
   bool nativeNotifications=NotificationsAvailable&&Link.Current.CanNotifications&&Link.Current.NotificationCount>0;int localCount=LocalNoticeCount;bool hasNotifications=nativeNotifications||localCount>0;
   notificationButton.Visibility=hasNotifications?Visibility.Visible:Visibility.Collapsed;notificationSeparator.Visibility=notificationButton.Visibility;
   if(hasNotifications){Color color=Color.FromRgb(128,130,135);if(System.Text.RegularExpressions.Regex.IsMatch(Link.Current.NotificationColor??"","^#[0-9a-fA-F]{6}$"))color=(Color)ColorConverter.ConvertFromString(Link.Current.NotificationColor);
    if(!nativeNotifications&&localCount>0){var notices=VisibleNotices.Where(n=>n.Unread);color=notices.Any(n=>n.Status=="failed"||n.Status=="waiting")?Color.FromRgb(245,149,40):notices.Any(n=>n.Status=="running")?Color.FromRgb(58,131,247):Color.FromRgb(48,200,90);}
    SolidColorBrush brush=notificationBadge.Background as SolidColorBrush;if(brush==null||brush.Color!=color)notificationBadge.Background=new SolidColorBrush(color);
    notificationNumber.Text=System.Text.RegularExpressions.Regex.IsMatch(Link.Current.NotificationText??"","^\\d{1,3}\\+?$")?Link.Current.NotificationText:Link.Current.NotificationCount.ToString();
    if(localCount>0){int total=(nativeNotifications?Link.Current.NotificationCount:0)+localCount;notificationNumber.Text=total>99?"99+":total.ToString();}notificationButton.ToolTip=localCount>0?"本地任务 "+localCount+" 项 · ChatGPT "+(nativeNotifications?Link.Current.NotificationText:"0")+" 项":"ChatGPT · "+Bridge.Name(Link.Current.State)+" · "+notificationNumber.Text+" 项";
   }
   if(nativeNotifications&&Link.Current.Stale){notificationButton.ToolTip=Link.Current.Hint;}notificationBadge.Visibility=hasNotifications?Visibility.Visible:Visibility.Hidden;
   if(!IsVisible&&toolbarMenu!=null)toolbarMenu.IsOpen=false;
  }
  async void RunToolbarAction(string action){
   if(toolbarBusy)return;
   if(!Settings.LinkChatGPT||!Link.Current.Connected){Say("请在设置中连接 ChatGPT Mini。",5);OpenSettings();return;}
   toolbarBusy=true;try{bool opened=await Link.InvokeToolbar(action);if(opened&&action=="notifications")await Link.Poll(Settings.LinkChatGPT,Settings.BackgroundMini);if(!opened){Say("ChatGPT 暂未提供这个入口，请在原 Mini 中操作。",5);if(action!="notifications")OpenSettings();}}
   catch(Exception e){Store.Log("Toolbar: "+e.Message);Say("联动暂不可用，请检查 ChatGPT Mini。",5);OpenSettings();}finally{toolbarBusy=false;}
  }
  void OpenMainNotificationMenu(){
   toolbarMenu=PetMenus.Create();toolbarMenu.PlacementTarget=hoverBar;toolbarMenu.Placement=PlacementMode.Top;
   foreach(var notice in Link.Current.Notices){var selected=notice;string title=string.IsNullOrWhiteSpace(notice.Title)?"ChatGPT 任务":notice.Title;Add(toolbarMenu,Bridge.Name(notice.State)+" · "+title,async delegate{try{if(!await Link.OpenMainNotification(selected.Id))Say("当前无法打开任务，请切换到 ChatGPT 查看。",4);}catch{Say("联动暂不可用，请稍后重试。",4);}});}
   ShowPetMenu(toolbarMenu);
  }
  void OpenToolbarNotifications(){if(LocalNoticeCount>0||NotificationsAvailable&&Link.Current.NotificationCount>0){OpenNoticeCenter();return;}
   if(Settings.LinkChatGPT&&Link.Current.Connected&&Link.Current.CanNotifications){if(Link.Current.NotificationSource=="main")OpenMainNotificationMenu();else RunToolbarAction("notifications");return;}
   toolbarMenu=PetMenus.Create();toolbarMenu.PlacementTarget=hoverBar;toolbarMenu.Placement=PlacementMode.Top;
   toolbarMenu.Items.Add(new MenuItem{Header=StatusText,IsEnabled=false});toolbarMenu.Items.Add(new MenuItem{Header="当前没有可打开的 Mini 通知入口",IsEnabled=false});
   Add(toolbarMenu,"打开联动设置",OpenSettings);ShowPetMenu(toolbarMenu);
  }
 }
}
