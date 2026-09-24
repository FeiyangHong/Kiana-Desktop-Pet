using System;using System.Linq;using System.Collections.Generic;using System.Windows;using System.Windows.Controls;using System.Windows.Controls.Primitives;using System.Windows.Input;using System.Windows.Media;using System.Windows.Media.Animation;using System.Windows.Media.Effects;using System.Windows.Threading;
namespace KianaPet {
 public static class NoticePreviewRows {
  public static NoticeRow[] Read(PetWindow pet){
   var rows=new List<NoticeRow>();var bridge=pet.Link.Current;
   if(pet.NotificationsAvailable)foreach(var n in bridge.Notices)rows.Add(new NoticeRow{Key="gpt:"+n.Id,Source="ChatGPT",Id=n.Id,Title=string.IsNullOrWhiteSpace(n.Title)?"ChatGPT 任务":n.Title,State=n.State=="review"?"succeeded":n.State,UpdatedAt=n.UpdatedAt,Unread=true,ChatGPT=true,Stale=bridge.Stale,CanOpen=bridge.Connected&&!bridge.Stale});
   foreach(var n in pet.VisibleNotices.Where(n=>n.Unread||n.Status=="running"||n.Status=="failed"||n.Status=="waiting"))rows.Add(new NoticeRow{Key="local:"+n.Source.Length+":"+n.Source+n.TaskId,Source=n.Source,Id=n.TaskId,Title=n.Title,State=n.Status,UpdatedAt=n.UpdatedAt,Unread=n.Unread,CanOpen=pet.TaskNotices.CanOpen(n),Local=n});
   return rows.GroupBy(n=>n.Key).Select(g=>g.First()).OrderBy(n=>n.Group).ThenByDescending(n=>n.UpdatedAt).ThenBy(n=>n.Key,StringComparer.Ordinal).ToArray();
  }
 }
 public sealed class QuickNoticeCard:Border {
  public NoticeRow Row;public readonly Button Open,Read;readonly TextBlock title,state;readonly PetWindow pet;readonly Action refresh;
  public QuickNoticeCard(PetWindow owner,Action changed,Action close){
   pet=owner;refresh=changed;Padding=new Thickness(0,4,0,4);Margin=new Thickness(0,2,0,2);Background=Brushes.Transparent;
   var layout=new Grid();layout.ColumnDefinitions.Add(new ColumnDefinition());layout.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});var text=new StackPanel{VerticalAlignment=VerticalAlignment.Center};state=CompanionControls.Text("",11);state.Margin=new Thickness(0,0,0,4);state.TextWrapping=TextWrapping.NoWrap;state.TextTrimming=TextTrimming.CharacterEllipsis;title=CompanionControls.Text("",13);title.Margin=new Thickness(0);title.TextWrapping=TextWrapping.NoWrap;title.TextTrimming=TextTrimming.CharacterEllipsis;text.Children.Add(state);text.Children.Add(title);layout.Children.Add(text);
   var actions=new StackPanel{Margin=new Thickness(10,0,0,0),VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(actions,1);layout.Children.Add(actions);
   Open=CompanionControls.Button("打开",delegate{if(Row==null||!Open.IsEnabled)return;if(Row.ChatGPT){string id=Row.Id;close();pet.OpenChatGPTTask(id);}else try{pet.TaskNotices.Open(Row.Local);refresh();}catch(Exception e){pet.Say("打开任务失败："+e.Message,4);}});
   Read=CompanionControls.Button("已读",delegate{pet.TaskNotices.Read(Row.Source,Row.Id);refresh();});foreach(var b in new[]{Open,Read}){b.MinHeight=24;b.FontSize=11;b.Padding=new Thickness(8,3,8,3);b.Margin=new Thickness(0,2,0,2);actions.Children.Add(b);}Child=layout;
  }
  public void Update(NoticeRow row,bool test){Row=row;title.Text=row.Title;title.ToolTip=row.Title;title.Foreground=PetPalette.Brush(PetPalette.Ink);state.Text="● "+row.Source+" · "+TaskNoticeHub.StateName(row.State)+(row.Stale?" · 待同步":"");state.Foreground=PetPalette.Brush(PetPalette.Hex(row.Stale?"#8b8790":row.State=="running"?"#3a83f7":row.State=="succeeded"?"#30a65a":row.State=="failed"?"#d95151":"#bd781c"));state.ToolTip=row.Stale?"连接暂断，保留上次状态；恢复后可跳转":state.Text;Open.IsEnabled=!test&&row.CanOpen;Open.ToolTip=test?"测试示例，不执行真实任务":row.Stale?"恢复连接后可打开":row.CanOpen?"打开对应任务":"来源尚未提供可用跳转";Read.Visibility=row.ChatGPT||test?Visibility.Collapsed:Visibility.Visible;Read.IsEnabled=row.Unread;foreach(var b in new[]{Open,Read}){b.Background=Brushes.Transparent;b.Foreground=title.Foreground;b.BorderBrush=PetPalette.Brush(PetPalette.Line);}}
 }
 public sealed class NoticePreview:Popup,IDisposable {
  readonly PetWindow pet;readonly FrameworkElement anchor;readonly Border surface,shadow;readonly StackPanel items=new StackPanel();readonly TextBlock summary;readonly Button native,center,closeButton;readonly DispatcherTimer timer=new DispatcherTimer();readonly Dictionary<string,QuickNoticeCard> cards=new Dictionary<string,QuickNoticeCard>();readonly MusicSurfaceAppearance appearance=new MusicSurfaceAppearance();
  MenuOutsideClick outside;DateTime leaveAt;bool pinned;string testState;bool? iconDark;public bool Pinned{get{return pinned;}}public bool IsTest{get{return testState!=null;}}public int VisibleTaskCount{get{return cards.Count;}}
  public NoticePreview(PetWindow owner,FrameworkElement target){
   pet=owner;anchor=target;PlacementTarget=target;Placement=PlacementMode.Top;VerticalOffset=-2;AllowsTransparency=true;StaysOpen=true;PopupAnimation=PopupAnimation.None;
   var root=new Grid{Margin=new Thickness(8),Width=pet.Settings.NoticePreviewWidth};PetVisuals.Apply(root,true);PetVisuals.InstallTooltips(root);RenderOptions.SetClearTypeHint(root,ClearTypeHint.Enabled);
   root.Resources[typeof(ScrollBar)]=(Style)System.Windows.Markup.XamlReader.Parse(@"<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='{x:Type ScrollBar}'>
<Setter Property='Width' Value='8'/><Setter Property='Margin' Value='3,2,0,2'/><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type ScrollBar}'>
<Track x:Name='PART_Track' Orientation='Vertical' IsDirectionReversed='True' Minimum='{TemplateBinding Minimum}' Maximum='{TemplateBinding Maximum}' Value='{TemplateBinding Value}' ViewportSize='{TemplateBinding ViewportSize}'>
<Track.DecreaseRepeatButton><RepeatButton Command='ScrollBar.PageUpCommand' Opacity='0' Focusable='False'/></Track.DecreaseRepeatButton>
<Track.Thumb><Thumb MinHeight='24' Background='{DynamicResource NoticeScrollThumb}'><Thumb.Template><ControlTemplate TargetType='{x:Type Thumb}'><Border Background='{TemplateBinding Background}' CornerRadius='3' Margin='1,0'/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb>
<Track.IncreaseRepeatButton><RepeatButton Command='ScrollBar.PageDownCommand' Opacity='0' Focusable='False'/></Track.IncreaseRepeatButton>
</Track></ControlTemplate></Setter.Value></Setter></Style>");
   shadow=new Border{CornerRadius=new CornerRadius(12),Effect=new DropShadowEffect{BlurRadius=12,ShadowDepth=2,Opacity=.13},IsHitTestVisible=false};root.Children.Add(shadow);
   surface=new Border{CornerRadius=new CornerRadius(12),BorderThickness=new Thickness(1),Padding=new Thickness(12,8,8,8)};root.Children.Add(surface);var layout=new Grid();layout.ColumnDefinitions.Add(new ColumnDefinition());layout.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});surface.Child=layout;
   var content=new StackPanel{VerticalAlignment=VerticalAlignment.Center};layout.Children.Add(content);content.Children.Add(new ScrollViewer{Content=items,MaxHeight=256,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});summary=CompanionControls.Text("",11);content.Children.Add(summary);
   native=CompanionControls.Button("原生通知",delegate{Dismiss();pet.OpenOriginalNotifications();});native.FontSize=11;native.Padding=new Thickness(8,3,8,3);native.MinHeight=24;native.HorizontalAlignment=HorizontalAlignment.Left;content.Children.Add(native);
   var rail=new StackPanel{Margin=new Thickness(8,0,0,0),VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(rail,1);layout.Children.Add(rail);
   center=IconButton("list","进入完整通知中心",delegate{if(!IsTest){Dismiss();pet.OpenNoticeCenter();}});closeButton=IconButton("close","关闭通知预览",Dismiss);rail.Children.Add(center);rail.Children.Add(closeButton);Child=root;
   root.PreviewMouseDown+=delegate{Pin();};root.PreviewKeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Escape){Dismiss();e.Handled=true;}};KeyboardNavigation.SetTabNavigation(root,KeyboardNavigationMode.Cycle);
   timer.Interval=TimeSpan.FromMilliseconds(250);timer.Tick+=delegate{if(!pet.CanShowNoticePreview){Dismiss();return;}Refresh();if(pinned)return;if(ContainsPointer())leaveAt=DateTime.UtcNow;else if(DateTime.UtcNow-leaveAt>TimeSpan.FromMilliseconds(550))Dismiss();};
   Opened+=delegate{leaveAt=DateTime.UtcNow;outside=new MenuOutsideClick(Dispatcher,ContainsPoint,Dismiss);timer.Start();root.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(MotionSettings.Enabled?PetVisuals.ShowMs:0)));};
   Closed+=delegate{if(!IsOpen)Cleanup();};
  }
  static Image Icon(string name){var drawing=new DrawingGroup();drawing.Children.Add(new GeometryDrawing(Brushes.Transparent,null,new RectangleGeometry(new Rect(0,0,18,18))));var pen=new Pen(PetPalette.Brush(PetPalette.Muted),1.1){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};drawing.Children.Add(new GeometryDrawing(null,pen,Geometry.Parse(name=="close"?"M5,5 L13,13 M13,5 L5,13":"M3,4 L4,4 M7,4 L15,4 M3,9 L4,9 M7,9 L15,9 M3,14 L4,14 M7,14 L15,14")));drawing.Freeze();return new Image{Source=new DrawingImage(drawing),Width=14,Height=14};}
  static Button IconButton(string name,string tip,Action action){var b=CompanionControls.Button("",action);b.Content=Icon(name);b.Width=24;b.Height=24;b.MinHeight=24;b.Padding=new Thickness(4);b.Margin=new Thickness(0);b.BorderThickness=new Thickness(0);b.Background=Brushes.Transparent;b.ToolTip=tip;System.Windows.Automation.AutomationProperties.SetName(b,tip);b.MouseEnter+=delegate{b.Background=PetPalette.Brush(Color.FromArgb(20,128,128,128));};b.MouseLeave+=delegate{b.Background=Brushes.Transparent;};return b;}
  bool ContainsPoint(Point p){return MenuOutsideClick.ContainsElement(Child as FrameworkElement,p)||MenuOutsideClick.ContainsElement(anchor,p);}
  bool ContainsPointer(){Native.Point p;Native.GetCursorPos(out p);return ContainsPoint(new Point(p.X,p.Y));}
  public void ShowPreview(bool pin){testState=null;Show(pin);}
  public void ShowTest(string state){testState=new[]{"running","succeeded","waiting","failed","stale","multiple"}.Contains(state)?state:"running";Show(true);}
  void Show(bool pin){Refresh();if(!IsOpen)IsOpen=true;if(pin)Pin();}
  public void Pin(){if(pinned)return;pinned=true;pet.ActivateNoticePreview();closeButton.Focus();Refresh();}
  void Cleanup(){timer.Stop();if(outside!=null){outside.Dispose();outside=null;}pet.NoticePreviewClosed(pinned);pinned=false;}
  public void Dismiss(){if(!IsOpen)return;Cleanup();IsOpen=false;testState=null;}
  public static NoticeRow[] TestRows(string state){var states=state=="multiple"?new[]{"running","succeeded","waiting","failed"}:new[]{state=="stale"?"running":state};return states.Select((s,i)=>new NoticeRow{Key="test:"+i,Id="test-"+i,Source="ChatGPT · 测试",Title=state=="multiple"?"示例任务 "+(i+1)+" · 通知横幅外观测试":"这是一条通知横幅测试",State=s,ChatGPT=true,Unread=true,Stale=state=="stale",CanOpen=false}).ToArray();}
  public void Refresh(){
   var all=IsTest?TestRows(testState):NoticePreviewRows.Read(pet);var bridge=pet.Link.Current;int limit=Math.Max(1,Math.Min(4,pet.Settings.NoticePreviewCount));var wanted=all.Take(limit).ToArray();
   bool fresh=pet.Music.Current.Connected&&DateTime.UtcNow-pet.Music.Current.At<=TimeSpan.FromSeconds(6);appearance.Apply(surface,pet.MusicCoverForAppearance,pet.Settings,pet.Settings.NoticePreviewFollowMusic&&pet.Settings.MusicEnabled&&fresh);shadow.Background=surface.Background;surface.BorderBrush=PetPalette.Brush(PetPalette.Line);summary.Foreground=PetPalette.Brush(PetPalette.Muted);
   double width=pet.Settings.NoticePreviewWidth;try{var point=anchor.PointToScreen(new Point());var screen=System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)point.X,(int)point.Y));var source=PresentationSource.FromVisual(anchor);if(source!=null)width=Math.Min(width,Math.Max(240,screen.WorkingArea.Width/source.CompositionTarget.TransformToDevice.M11-24));}catch(InvalidOperationException){}((FrameworkElement)Child).Width=width;
   ((FrameworkElement)Child).Resources["NoticeScrollThumb"]=PetPalette.Brush(PetPalette.Dark?PetPalette.Hex("#615C69"):PetPalette.Hex("#D3CFDC"));if(iconDark!=PetPalette.Dark){iconDark=PetPalette.Dark;center.Content=Icon("list");closeButton.Content=Icon("close");}
   int remaining=!IsTest&&pet.NotificationsAvailable?Math.Max(0,bridge.NotificationCount-bridge.Notices.Length):0;center.ToolTip=IsTest?"测试示例：不打开真实通知中心":"进入完整通知中心 · 共 "+(all.Length+remaining)+" 条"+(all.Length>limit?"，还有 "+(all.Length-limit)+" 条未展开":"");center.IsEnabled=!IsTest;
   foreach(string key in cards.Keys.Where(k=>!wanted.Any(n=>n.Key==k)).ToArray()){items.Children.Remove(cards[key]);cards.Remove(key);}
   for(int i=0;i<wanted.Length;i++){var row=wanted[i];QuickNoticeCard card;if(!cards.TryGetValue(row.Key,out card)){card=new QuickNoticeCard(pet,Refresh,Dismiss);cards.Add(row.Key,card);}card.Update(row,IsTest);if(items.Children.IndexOf(card)!=i){items.Children.Remove(card);items.Children.Insert(i,card);}}
   summary.Text=all.Length==0&&remaining==0?"目前没有待处理通知。":remaining>0?"另有 "+remaining+" 项 ChatGPT 活动（"+Bridge.Name(bridge.State)+"），来源暂未提供标题。":"";summary.Visibility=summary.Text.Length==0?Visibility.Collapsed:Visibility.Visible;
   native.Visibility=remaining>0?Visibility.Visible:Visibility.Collapsed;native.IsEnabled=!IsTest&&pet.NotificationsAvailable&&bridge.Connected&&!bridge.Stale&&bridge.NotificationSource=="mini"&&bridge.CanNotifications;native.Background=Brushes.Transparent;native.Foreground=PetPalette.Brush(PetPalette.Ink);native.BorderBrush=surface.BorderBrush;
  }
  public void Dispose(){Dismiss();timer.Stop();if(outside!=null){outside.Dispose();outside=null;}}
 }
 public sealed partial class PetWindow {
  NoticePreview noticePreview;DispatcherTimer noticeHoverTimer;DateTime noticePreviewClosedAt;
  public bool NoticePreviewOpen{get{return noticePreview!=null&&noticePreview.IsOpen;}}
  public bool CanShowNoticePreview{get{return !quitting&&IsVisible&&Settings.HoverToolbar&&!manualHidden&&!fullscreenHidden&&!quietHidden&&!sessionLocked&&!menuLayerYielding&&!pressed&&!dragging;}}
  void SetupNoticePreview(){noticeHoverTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(350)};noticeHoverTimer.Tick+=delegate{noticeHoverTimer.Stop();if(Settings.NoticePreviewHover&&notificationButton.IsMouseOver&&CanShowNoticePreview&&DateTime.UtcNow-noticePreviewClosedAt>TimeSpan.FromMilliseconds(600))ShowNoticePreview(false);};notificationButton.MouseEnter+=delegate{if(Settings.NoticePreviewHover)noticeHoverTimer.Start();};notificationButton.MouseLeave+=delegate{noticeHoverTimer.Stop();};}
  public void ShowNoticePreview(bool pin){if(!CanShowNoticePreview)return;ClosePetMenu();if(noticePreview==null)noticePreview=new NoticePreview(this,notificationButton);noticePreview.ShowPreview(pin);toolbarUntil=clock.Elapsed.TotalSeconds+1;}
  public void ShowNoticeTest(string state){if(!CanShowNoticePreview){Say("请先显示宠物并开启底部工具栏，再测试横幅。",4);return;}ClosePetMenu();if(noticePreview==null)noticePreview=new NoticePreview(this,notificationButton);noticePreview.ShowTest(state);toolbarUntil=clock.Elapsed.TotalSeconds+1;}
  public void ActivateNoticePreview(){Native.SetMenuActivation(handle,true);Activate();Native.FocusPetForMenu(handle);}
  public void NoticePreviewClosed(bool wasPinned){noticePreviewClosedAt=DateTime.UtcNow;if(wasPinned&&!keyboardToolbarMode)Native.SetMenuActivation(handle,false);toolbarUntil=clock.Elapsed.TotalSeconds+.3;}
  void CloseNoticePreview(){if(noticeHoverTimer!=null)noticeHoverTimer.Stop();if(noticePreview!=null)noticePreview.Dismiss();}
  void StopNoticePreview(){CloseNoticePreview();if(noticePreview!=null){noticePreview.Dispose();noticePreview=null;}}
 }
}
