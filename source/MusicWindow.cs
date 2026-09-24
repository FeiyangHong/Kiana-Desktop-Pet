using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
namespace KianaPet {
 public sealed partial class MusicWindow:Window {
  readonly PetWindow pet;readonly TextBlock title=new TextBlock(),artist=new TextBlock(),lyric=new TextBlock();
  readonly Image cover=new Image();readonly Button previous,toggle,next,lyrics;readonly StackPanel lyricRow=new StackPanel();
  Grid outer;Border surface,shadowSurface;bool pointerPressed,shown;int transition;Native.Point dragStart;Native.Rect dragBounds;UIElement dragSurface;
  public bool IsDragging{get;private set;}public bool KeyboardMode{get;private set;}
  public void FocusControls(){KeyboardMode=true;Native.SetMenuActivation(new WindowInteropHelper(this).Handle,true);Activate();Native.FocusPetForMenu(new WindowInteropHelper(this).Handle);toggle.Focus();}
  public MusicWindow(PetWindow owner){pet=owner;InitializeDocking();Owner=owner;Title="琪亚娜 · 音乐";Width=238;Height=88;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;ShowInTaskbar=false;ShowActivated=false;Topmost=true;FontFamily=new FontFamily("Microsoft YaHei UI");UseLayoutRounding=true;SnapsToDevicePixels=true;
   TextOptions.SetTextFormattingMode(this,TextFormattingMode.Display);TextOptions.SetTextRenderingMode(this,TextRenderingMode.ClearType);
   PetVisuals.Apply(this,false);PetVisuals.InstallTooltips(this);outer=new Grid{Margin=new Thickness(6)};
   shadowSurface=new Border{Background=Brushes.White,CornerRadius=new CornerRadius(12),Effect=new DropShadowEffect{BlurRadius=10,ShadowDepth=2,Opacity=.1,Color=Colors.Black}};outer.Children.Add(shadowSurface);
   Border panel=new Border{Background=Brushes.White,BorderBrush=Brush("#E9E7EC"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(12),Padding=new Thickness(10,8,10,6)};surface=panel;RenderOptions.SetClearTypeHint(panel,ClearTypeHint.Enabled);outer.Children.Add(panel);
   StackPanel stack=new StackPanel();panel.Child=stack;Grid head=new Grid();head.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(34)});head.ColumnDefinitions.Add(new ColumnDefinition());
   Grid art=new Grid{Width=30,Height=30};art.Children.Add(new Border{Background=Brush("#F1EEF7"),CornerRadius=new CornerRadius(7)});art.Children.Add(new TextBlock{Text="♫",FontSize=20,Foreground=Brush("#9A88B4"),HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center});cover.Stretch=Stretch.UniformToFill;cover.Clip=new RectangleGeometry(new Rect(0,0,30,30),7,7);RenderOptions.SetBitmapScalingMode(cover,BitmapScalingMode.HighQuality);art.Children.Add(cover);head.Children.Add(art);
   StackPanel info=new StackPanel{Margin=new Thickness(5,0,0,0)};Grid.SetColumn(info,1);title.FontSize=12;title.FontWeight=FontWeights.SemiBold;title.Foreground=Brush("#29272D");title.TextTrimming=TextTrimming.CharacterEllipsis;artist.FontSize=11;artist.Foreground=Brush("#8B8790");artist.Margin=new Thickness(0,1,0,0);artist.TextTrimming=TextTrimming.CharacterEllipsis;info.Children.Add(title);info.Children.Add(artist);head.Children.Add(info);stack.Children.Add(head);
   Grid controls=new Grid{Margin=new Thickness(0,2,0,0)};controls.ColumnDefinitions.Add(new ColumnDefinition());controls.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});StackPanel buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};previous=Button("previous","上一首",delegate{pet.MusicCommand("previous");});toggle=Button("play","播放",delegate{pet.MusicCommand("toggle");});next=Button("next","下一首",delegate{pet.MusicCommand("next");});buttons.Children.Add(previous);buttons.Children.Add(toggle);buttons.Children.Add(next);controls.Children.Add(buttons);
   lyrics=Button("lyrics","显示／隐藏歌词",delegate{pet.Settings.MusicLyrics=!pet.Settings.MusicLyrics;pet.ApplySettings();});lyrics.HorizontalAlignment=HorizontalAlignment.Right;Grid.SetColumn(lyrics,1);controls.Children.Add(lyrics);stack.Children.Add(controls);
   lyricRow.Margin=new Thickness(0,3,0,0);lyric.FontSize=11;lyric.Foreground=Brush("#71647E");lyric.TextAlignment=TextAlignment.Center;lyric.TextTrimming=TextTrimming.CharacterEllipsis;lyricRow.Children.Add(lyric);stack.Children.Add(lyricRow);Content=outer;
   dragSurface=head;head.Background=Brushes.Transparent;head.Cursor=Cursors.SizeAll;head.ToolTip="按住封面或歌名拖动；松手后固定位置并避开宠物工具栏";
   head.MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){if(pet.Settings.LockMusicPosition)return;StopDock();Native.GetCursorPos(out dragStart);Native.GetWindowRect(new WindowInteropHelper(this).Handle,out dragBounds);pointerPressed=true;IsDragging=false;head.CaptureMouse();e.Handled=true;};
   head.MouseMove+=delegate(object sender,MouseEventArgs e){if(!pointerPressed)return;Native.Point p;Native.GetCursorPos(out p);int dx=p.X-dragStart.X,dy=p.Y-dragStart.Y;var source=PresentationSource.FromVisual(this);double dpi=source==null?1:source.CompositionTarget.TransformToDevice.M11;if(Math.Abs(dx)+Math.Abs(dy)>(e.StylusDevice==null?4:12)*dpi)IsDragging=true;if(IsDragging){Native.SetWindowPos(new WindowInteropHelper(this).Handle,IntPtr.Zero,dragBounds.Left+dx,dragBounds.Top+dy,0,0,0x0001|0x0004|0x0010);e.Handled=true;}};
   head.MouseLeftButtonUp+=delegate(object sender,MouseButtonEventArgs e){CompleteDrag();e.Handled=true;};head.LostMouseCapture+=delegate{if(pointerPressed)CompleteDrag();};
   KeyboardNavigation.SetTabNavigation(this,KeyboardNavigationMode.Cycle);PreviewKeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Escape&&KeyboardMode){KeyboardMode=false;Native.SetMenuActivation(new WindowInteropHelper(this).Handle,false);pet.FocusToolbar();e.Handled=true;}};Deactivated+=delegate{if(KeyboardMode){KeyboardMode=false;Native.SetMenuActivation(new WindowInteropHelper(this).Handle,false);}};
   SourceInitialized+=delegate{Native.StylePet(new WindowInteropHelper(this).Handle,false);InstallDisplayHook();};
  }
  static SolidColorBrush Brush(string hex){return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));}
  static Image MusicIcon(string name){string data=name=="previous"?"M5 4 L5 20 M19 5 L8 12 L19 19 Z":name=="next"?"M19 4 L19 20 M5 5 L16 12 L5 19 Z":name=="pause"?"M8 5 L8 19 M16 5 L16 19":name=="lyrics"?"M4 6 L20 6 M4 12 L20 12 M4 18 L14 18":"M8 4 L20 12 L8 20 Z";
   var g=new DrawingGroup();g.Children.Add(new GeometryDrawing(Brushes.Transparent,null,new RectangleGeometry(new Rect(0,0,24,24))));g.Children.Add(new GeometryDrawing(null,new Pen(PetPalette.Brush(PetPalette.Ink),name=="pause"?2.5:1.6){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round},Geometry.Parse(data)));g.Freeze();return new Image{Source=new DrawingImage(g),Width=16,Height=16};
  }
  static Button Button(string icon,string tip,Action click){var b=new Button{Content=MusicIcon(icon),Width=28,Height=24,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Focusable=true,Cursor=Cursors.Hand,ToolTip=tip};
   var border=new FrameworkElementFactory(typeof(Border));border.SetValue(Border.CornerRadiusProperty,new CornerRadius(8));border.SetBinding(Border.BackgroundProperty,new System.Windows.Data.Binding("Background"){RelativeSource=new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)});var content=new FrameworkElementFactory(typeof(ContentPresenter));content.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);content.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);border.AppendChild(content);var template=new ControlTemplate(typeof(Button)){VisualTree=border};var disabled=new Trigger{Property=IsEnabledProperty,Value=false};disabled.Setters.Add(new Setter(OpacityProperty,.3));template.Triggers.Add(disabled);b.Template=template;
   b.MouseEnter+=delegate{b.Background=PetPalette.Brush(PetPalette.Dark?PetPalette.Hex("#45404F"):PetPalette.Hex("#F4F1F7"));};b.MouseLeave+=delegate{b.Background=Brushes.Transparent;};b.Click+=delegate{click();};System.Windows.Automation.AutomationProperties.SetName(b,tip);return b;
  }
  static ToolTip FullText(string text){return new ToolTip{Content=new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,MaxWidth=320,FontSize=12},Padding=new Thickness(10,7,10,7)};}
  bool lastPlaying;string lastTitle="",lastLine="";
  public void Refresh(MusicState state,string line,BitmapSource art,bool busy){((FrameworkElement)dragSurface).Cursor=pet.Settings.LockMusicPosition?Cursors.Arrow:Cursors.SizeAll;((FrameworkElement)dragSurface).ToolTip=pet.Settings.LockMusicPosition?"音乐栏拖动已锁定；仍按设置跟随与避让":"按住封面或歌名拖动音乐栏";RefreshPalette(art);title.Text=state.Title;title.ToolTip=FullText(state.Title);artist.Text=(state.Playing?"":"已暂停 · ")+state.Artist;artist.ToolTip=FullText(state.Artist);cover.Source=art;
   if(state.Title!=lastTitle||state.Playing!=lastPlaying||paletteChanged){toggle.Content=MusicIcon(state.Playing?"pause":"play");toggle.ToolTip=state.Playing?"暂停":"继续播放";System.Windows.Automation.AutomationProperties.SetName(toggle,(string)toggle.ToolTip);lastPlaying=state.Playing;lastTitle=state.Title;}
   previous.IsEnabled=!busy&&state.CanPrevious;toggle.IsEnabled=!busy&&state.CanToggle;next.IsEnabled=!busy&&state.CanNext;
   bool showLyrics=pet.Settings.MusicLyrics;lyricRow.Visibility=showLyrics?Visibility.Visible:Visibility.Collapsed;ApplyCardSize(showLyrics);foreach(var b in new[]{previous,toggle,next,lyrics}){b.Width=pet.Settings.LargeTouchTargets?38:28;b.Height=pet.Settings.LargeTouchTargets?34:24;}lyrics.Opacity=showLyrics?1:.55;
   if(lastLine!=line){lastLine=line;lyric.Text=line;lyric.ToolTip=line;lyric.BeginAnimation(OpacityProperty,new DoubleAnimation(.25,1,TimeSpan.FromMilliseconds(MotionSettings.Enabled?PetVisuals.ShowMs:0)));}
  }
  void ApplyCardSize(bool showLyrics){double scale=pet.Settings.MusicScalePercent/100d;double width=pet.Settings.MusicWidth*scale,height=((showLyrics?110:88)+(pet.Settings.LargeTouchTargets?10:0))*scale;var transform=outer.LayoutTransform as ScaleTransform;if(transform!=null&&transform.ScaleX==scale&&Width==width&&Height==height)return;if(transform==null||transform.ScaleX!=scale)outer.LayoutTransform=new ScaleTransform(scale,scale);outer.Margin=new Thickness(6*scale);Width=width;Height=height;UpdateLayout();}
  public void ShowSoft(){if(shown&&IsVisible)return;shown=true;transition++;if(!IsVisible){BeginAnimation(OpacityProperty,null);Opacity=0;Show();}BeginAnimation(OpacityProperty,new DoubleAnimation(1,TimeSpan.FromMilliseconds(MotionSettings.Enabled?PetVisuals.ShowMs:0)));}
  public void HideSoft(bool immediate){StopDock();dockInitialized=false;if(!IsVisible)return;if(!shown&&!immediate)return;shown=false;int token=++transition;if(immediate){CompleteDrag();BeginAnimation(OpacityProperty,null);Opacity=0;Hide();return;}var fade=new DoubleAnimation(0,TimeSpan.FromMilliseconds(MotionSettings.Enabled?PetVisuals.HideMs:0));fade.Completed+=delegate{if(token==transition&&!shown)Hide();};BeginAnimation(OpacityProperty,fade);}
  public void SavePosition(){var h=new WindowInteropHelper(this).Handle;if(h==IntPtr.Zero)return;Native.Rect b;Native.GetWindowRect(h,out b);pet.Settings.MusicX=b.Left;pet.Settings.MusicY=b.Top;pet.Settings.MusicHasPosition=true;}
  void CompleteDrag(){if(!pointerPressed)return;bool moved=IsDragging;pointerPressed=false;IsDragging=false;if(dragSurface!=null)dragSurface.ReleaseMouseCapture();if(moved){pet.Settings.MusicPosition="free";SavePosition();pet.PositionMusic();SavePosition();pet.Save();}}
  public void Follow(Native.Rect petBounds,bool immediate=false){if(pointerPressed)return;Native.Rect full=petBounds;petBounds=pet.MusicFootprint(petBounds);var h=new WindowInteropHelper(this).Handle;if(h==IntPtr.Zero)return;Native.Rect b;Native.GetWindowRect(h,out b);
   var area=pet.Settings.MusicPosition=="free"&&pet.Settings.MusicHasPosition?System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)pet.Settings.MusicX+b.Width/2,(int)pet.Settings.MusicY+b.Height/2)).WorkingArea:System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(pet).Handle).WorkingArea;
   var source=PresentationSource.FromVisual(this);int gap=(int)Math.Ceiling(6*(source==null?1:source.CompositionTarget.TransformToDevice.M11));
   var target=MusicPlacement.Place(new System.Drawing.Rectangle(petBounds.Left,petBounds.Top,petBounds.Width,petBounds.Height),area,b.Width,b.Height,gap,pet.Settings.MusicPosition,pet.Settings.MusicHasPosition,pet.Settings.MusicX,pet.Settings.MusicY);
   if(target.IsEmpty){HideSoft(true);return;}PlaceCard(target,b,full,immediate);
  }
  public void Capture(string path){UpdateLayout();var source=PresentationSource.FromVisual(this);double dpi=source==null?1:source.CompositionTarget.TransformToDevice.M11;var bitmap=new RenderTargetBitmap((int)Math.Ceiling(ActualWidth*dpi),(int)Math.Ceiling(ActualHeight*dpi),96*dpi,96*dpi,PixelFormats.Pbgra32);bitmap.Render(this);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(path))encoder.Save(stream);}
 }
}


