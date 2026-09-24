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
  public NoticeRow Row;public readonly Button Open,Read;readonly TextBlock title,state,time;readonly PetWindow pet;readonly Action refresh;readonly Grid layout=new Grid();readonly StackPanel stack=new StackPanel();readonly WrapPanel actions=new WrapPanel();bool? compact;
  public QuickNoticeCard(PetWindow owner,Action changed,Action close){
   pet=owner;refresh=changed;Padding=new Thickness(10,8,10,8);Margin=new Thickness(0,3,0,3);CornerRadius=new CornerRadius(8);BorderThickness=new Thickness(1);
   state=CompanionControls.Text("",11);title=CompanionControls.Text("",13);title.TextTrimming=TextTrimming.CharacterEllipsis;title.Margin=new Thickness(0,2,0,2);time=CompanionControls.Text("",10);time.Margin=new Thickness(0,2,0,2);stack.Children.Add(state);stack.Children.Add(title);stack.Children.Add(time);
   Open=CompanionControls.Button("打开任务",delegate{if(Row==null||!Row.CanOpen)return;if(Row.ChatGPT){string id=Row.Id;close();pet.OpenChatGPTTask(id);}else try{pet.TaskNotices.Open(Row.Local);refresh();}catch(Exception e){pet.Say("打开任务失败："+e.Message,4);}});
   Read=CompanionControls.Button("已读",delegate{pet.TaskNotices.Read(Row.Source,Row.Id);refresh();});foreach(var b in new[]{Open,Read}){b.MinHeight=26;b.FontSize=11;b.Padding=new Thickness(8,3,8,3);b.Margin=new Thickness(0,4,6,0);actions.Children.Add(b);}layout.Children.Add(stack);layout.Children.Add(actions);Child=layout;
  }
  void ArrangeCard(){bool slim=pet.Settings.NoticePreviewCompact;if(compact==slim)return;compact=slim;layout.ColumnDefinitions.Clear();layout.RowDefinitions.Clear();layout.ColumnDefinitions.Add(new ColumnDefinition());layout.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});layout.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});Grid.SetColumnSpan(stack,slim?1:2);Grid.SetColumn(actions,slim?1:0);Grid.SetRow(actions,slim?0:1);Grid.SetColumnSpan(actions,slim?1:2);actions.Orientation=slim?Orientation.Vertical:Orientation.Horizontal;actions.VerticalAlignment=VerticalAlignment.Center;actions.Margin=slim?new Thickness(10,0,0,0):new Thickness(0);title.TextWrapping=slim?TextWrapping.NoWrap:TextWrapping.Wrap;title.MaxHeight=slim?23:40;state.TextWrapping=slim?TextWrapping.NoWrap:TextWrapping.Wrap;state.TextTrimming=TextTrimming.CharacterEllipsis;state.Margin=new Thickness(0,2,0,2);Open.Content=slim?"打开":"打开任务";Padding=slim?new Thickness(0,5,0,5):new Thickness(10,8,10,8);BorderThickness=slim?new Thickness(0):new Thickness(1);}
  public void Update(NoticeRow row){ArrangeCard();Row=row;Background=PetPalette.Brush(PetPalette.Background);BorderBrush=PetPalette.Brush(PetPalette.Line);title.Text=row.Title;title.ToolTip=row.Title;title.Foreground=PetPalette.Brush(PetPalette.Ink);state.Text="● "+row.Source+" · "+TaskNoticeHub.StateName(row.State)+(row.Stale?" · 待同步":"");state.Foreground=PetPalette.Brush(PetPalette.Hex(row.Stale?"#8b8790":row.State=="running"?"#3a83f7":row.State=="succeeded"?"#30a65a":row.State=="failed"?"#d95151":"#bd781c"));time.Text=row.UpdatedAt==DateTime.MinValue?"更新时间：来源未提供":"更新于 "+row.UpdatedAt.ToLocalTime().ToString("HH:mm:ss");time.Foreground=PetPalette.Brush(PetPalette.Muted);Open.IsEnabled=row.CanOpen;Open.ToolTip=row.Stale?"恢复连接后可打开":row.CanOpen?"打开对应任务":"来源尚未提供可用跳转";Read.Visibility=row.ChatGPT?Visibility.Collapsed:Visibility.Visible;Read.IsEnabled=row.Unread;foreach(var b in new[]{Open,Read}){b.Background=Background;b.Foreground=title.Foreground;b.BorderBrush=BorderBrush;}}
 }
 public sealed class NoticePreview:Popup,IDisposable {
  readonly PetWindow pet;readonly FrameworkElement anchor;readonly Border surface,shadow;readonly StackPanel items=new StackPanel();readonly TextBlock heading,hint,summary;readonly Button readAll,native,center,closeButton;readonly DispatcherTimer timer=new DispatcherTimer();readonly Dictionary<string,QuickNoticeCard> cards=new Dictionary<string,QuickNoticeCard>();
  MenuOutsideClick outside;DateTime leaveAt;bool pinned;public bool Pinned{get{return pinned;}}public int VisibleTaskCount{get{return cards.Count;}}
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
   surface=new Border{CornerRadius=new CornerRadius(12),BorderThickness=new Thickness(1),Padding=new Thickness(12)};root.Children.Add(surface);var content=new StackPanel();surface.Child=content;
   var header=new DockPanel();closeButton=CompanionControls.Button("×",Dismiss);closeButton.Width=26;closeButton.MinHeight=24;closeButton.Padding=new Thickness(0);closeButton.Margin=new Thickness(4,0,0,0);System.Windows.Automation.AutomationProperties.SetName(closeButton,"关闭通知预览");DockPanel.SetDock(closeButton,Dock.Right);header.Children.Add(closeButton);heading=CompanionControls.Text("通知速览",14);heading.FontWeight=FontWeights.SemiBold;header.Children.Add(heading);content.Children.Add(header);
   hint=CompanionControls.Text("",11);content.Children.Add(hint);content.Children.Add(new ScrollViewer{Content=items,MaxHeight=280,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});summary=CompanionControls.Text("",11);content.Children.Add(summary);
   var footer=new DockPanel();var actions=new WrapPanel();readAll=CompanionControls.Button("本地全部已读",delegate{pet.TaskNotices.ReadAll();Refresh();});native=CompanionControls.Button("原生通知",delegate{Dismiss();pet.OpenOriginalNotifications();});foreach(var b in new[]{readAll,native}){b.FontSize=11;b.Padding=new Thickness(8,4,8,4);b.MinHeight=28;actions.Children.Add(b);}
   center=CompanionControls.Button("通知中心 →",delegate{Dismiss();pet.OpenNoticeCenter();});center.ToolTip="进入完整通知中心，查看全部任务和筛选";center.Padding=new Thickness(8,4,8,4);center.FontSize=11;center.MinHeight=28;center.HorizontalAlignment=HorizontalAlignment.Right;center.Margin=new Thickness(0);DockPanel.SetDock(center,Dock.Right);footer.Children.Add(center);footer.Children.Add(actions);content.Children.Add(footer);Child=root;
   root.PreviewMouseDown+=delegate{Pin();};root.PreviewKeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Escape){Dismiss();e.Handled=true;}};KeyboardNavigation.SetTabNavigation(root,KeyboardNavigationMode.Cycle);
   timer.Interval=TimeSpan.FromMilliseconds(250);timer.Tick+=delegate{if(!pet.CanShowNoticePreview){Dismiss();return;}Refresh();if(pinned)return;if(ContainsPointer())leaveAt=DateTime.UtcNow;else if(DateTime.UtcNow-leaveAt>TimeSpan.FromMilliseconds(550))Dismiss();};
   Opened+=delegate{leaveAt=DateTime.UtcNow;outside=new MenuOutsideClick(Dispatcher,ContainsPoint,Dismiss);timer.Start();root.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(MotionSettings.Enabled?PetVisuals.ShowMs:0)));};
   Closed+=delegate{if(!IsOpen)Cleanup();};
  }
  bool ContainsPoint(Point p){return MenuOutsideClick.ContainsElement(Child as FrameworkElement,p)||MenuOutsideClick.ContainsElement(anchor,p);}
  bool ContainsPointer(){Native.Point p;Native.GetCursorPos(out p);return ContainsPoint(new Point(p.X,p.Y));}
  public void ShowPreview(bool pin){Refresh();if(!IsOpen)IsOpen=true;if(pin)Pin();}
  public void Pin(){if(pinned)return;pinned=true;pet.ActivateNoticePreview();closeButton.Focus();Refresh();}
  void Cleanup(){timer.Stop();if(outside!=null){outside.Dispose();outside=null;}pet.NoticePreviewClosed(pinned);pinned=false;}
  public void Dismiss(){if(!IsOpen)return;Cleanup();IsOpen=false;}
  public void Refresh(){
   var all=NoticePreviewRows.Read(pet);var bridge=pet.Link.Current;int limit=Math.Max(1,Math.Min(4,pet.Settings.NoticePreviewCount));bool slim=pet.Settings.NoticePreviewCompact;var wanted=all.Take(limit).ToArray();surface.Background=shadow.Background=PetPalette.Brush(PetPalette.Background);surface.BorderBrush=PetPalette.Brush(PetPalette.Line);heading.Foreground=PetPalette.Brush(PetPalette.Ink);hint.Foreground=summary.Foreground=PetPalette.Brush(PetPalette.Muted);
   double width=pet.Settings.NoticePreviewWidth;try{var point=anchor.PointToScreen(new Point());var screen=System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)point.X,(int)point.Y));var source=PresentationSource.FromVisual(anchor);if(source!=null)width=Math.Min(width,Math.Max(240,screen.WorkingArea.Width/source.CompositionTarget.TransformToDevice.M11-24));}catch(InvalidOperationException){}((FrameworkElement)Child).Width=width;surface.Padding=slim?new Thickness(12,8,12,8):new Thickness(12);heading.FontSize=slim?12:14;hint.Visibility=slim&&!bridge.Stale?Visibility.Collapsed:Visibility.Visible;
   ((FrameworkElement)Child).Resources["NoticeScrollThumb"]=PetPalette.Brush(PetPalette.Dark?PetPalette.Hex("#615C69"):PetPalette.Hex("#D3CFDC"));
   int remaining=pet.NotificationsAvailable?Math.Max(0,bridge.NotificationCount-bridge.Notices.Length):0;heading.Text="通知 · "+(all.Length+remaining)+(slim&&all.Length>limit?"  /  另有 "+(all.Length-limit)+" 条":"");heading.ToolTip=pinned?"点击任务可打开 · 点空白处或 Esc 收起":"悬停预览 · 点击铃铛固定展开";
   hint.Text=bridge.Stale&&pet.NotificationsAvailable?"连接暂断 · 保留上次状态，恢复后可跳转":pinned?"点击任务可打开 · 点空白处或 Esc 收起":"悬停预览 · 点击铃铛固定展开";
   foreach(string key in cards.Keys.Where(k=>!wanted.Any(n=>n.Key==k)).ToArray()){items.Children.Remove(cards[key]);cards.Remove(key);}
   for(int i=0;i<wanted.Length;i++){var row=wanted[i];QuickNoticeCard card;if(!cards.TryGetValue(row.Key,out card)){card=new QuickNoticeCard(pet,Refresh,Dismiss);cards.Add(row.Key,card);}card.Update(row);if(items.Children.IndexOf(card)!=i){items.Children.Remove(card);items.Children.Insert(i,card);}}
   summary.Text=all.Length==0&&remaining==0?"目前没有待处理通知。":(!slim&&all.Length>limit?"另有 "+(all.Length-limit)+" 项，可进入通知中心查看。":"")+(remaining>0?"另有 "+remaining+" 项 ChatGPT 活动（"+Bridge.Name(bridge.State)+"），来源暂未提供标题。":"");summary.Visibility=summary.Text.Length==0?Visibility.Collapsed:Visibility.Visible;
   readAll.Visibility=pet.VisibleNotices.Any(n=>n.Unread)?Visibility.Visible:Visibility.Collapsed;native.Visibility=remaining>0?Visibility.Visible:Visibility.Collapsed;native.IsEnabled=pet.NotificationsAvailable&&bridge.Connected&&!bridge.Stale&&bridge.NotificationSource=="mini"&&bridge.CanNotifications;
   foreach(var b in new[]{closeButton,readAll,native,center}){b.Background=surface.Background;b.Foreground=heading.Foreground;b.BorderBrush=surface.BorderBrush;}
  }
  public void Dispose(){Dismiss();timer.Stop();if(outside!=null){outside.Dispose();outside=null;}}
 }
 public sealed partial class PetWindow {
  NoticePreview noticePreview;DispatcherTimer noticeHoverTimer;DateTime noticePreviewClosedAt;
  public bool NoticePreviewOpen{get{return noticePreview!=null&&noticePreview.IsOpen;}}
  public bool CanShowNoticePreview{get{return !quitting&&IsVisible&&Settings.HoverToolbar&&!manualHidden&&!fullscreenHidden&&!quietHidden&&!sessionLocked&&!menuLayerYielding&&!pressed&&!dragging;}}
  void SetupNoticePreview(){noticeHoverTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(350)};noticeHoverTimer.Tick+=delegate{noticeHoverTimer.Stop();if(Settings.NoticePreviewHover&&notificationButton.IsMouseOver&&CanShowNoticePreview&&DateTime.UtcNow-noticePreviewClosedAt>TimeSpan.FromMilliseconds(600))ShowNoticePreview(false);};notificationButton.MouseEnter+=delegate{if(Settings.NoticePreviewHover)noticeHoverTimer.Start();};notificationButton.MouseLeave+=delegate{noticeHoverTimer.Stop();};}
  public void ShowNoticePreview(bool pin){if(!CanShowNoticePreview)return;ClosePetMenu();if(noticePreview==null)noticePreview=new NoticePreview(this,notificationButton);noticePreview.ShowPreview(pin);toolbarUntil=clock.Elapsed.TotalSeconds+1;}
  public void ActivateNoticePreview(){Native.SetMenuActivation(handle,true);Activate();Native.FocusPetForMenu(handle);}
  public void NoticePreviewClosed(bool wasPinned){noticePreviewClosedAt=DateTime.UtcNow;if(wasPinned&&!keyboardToolbarMode)Native.SetMenuActivation(handle,false);toolbarUntil=clock.Elapsed.TotalSeconds+.3;}
  void CloseNoticePreview(){if(noticeHoverTimer!=null)noticeHoverTimer.Stop();if(noticePreview!=null)noticePreview.Dismiss();}
  void StopNoticePreview(){CloseNoticePreview();if(noticePreview!=null){noticePreview.Dispose();noticePreview=null;}}
 }
}
