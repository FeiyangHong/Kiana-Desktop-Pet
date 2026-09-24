using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms=System.Windows.Forms;

namespace KianaPet {
 public sealed partial class PetWindow:Window {
  public Config Settings;public List<PetInfo> Pets;public Bridge Link=new Bridge();
  readonly bool preview;bool initialized,closeScheduled;readonly Stopwatch clock=Stopwatch.StartNew();readonly Random random=new Random();
  readonly WalkPlanner walkPlanner=new WalkPlanner();
  readonly DispatcherTimer animation=new DispatcherTimer(),poll=new DispatcherTimer();
  readonly Image sprite=new Image();readonly TextBlock bubbleText=new TextBlock(),zzz=new TextBlock();readonly Border bubble=new Border();
  readonly Dictionary<string,BitmapSource[]> frames=new Dictionary<string,BitmapSource[]>();
  readonly Dictionary<string,BitmapSource[]> extras=new Dictionary<string,BitmapSource[]>();
  Forms.NotifyIcon tray;SettingsWindow settingsWindow;IntPtr handle;HwndSource source;
  string state="idle",interaction=null,dragState="running-right",lastRoutine="",lastHidden="";
  double stateAt=0,interactionUntil=0,bubbleUntil=0,nextWalk=15,walkUntil=0,targetX=0,velocity=0,lastTick=0,lastSlow=0;
  DateTime awakeUntil=DateTime.Now.AddSeconds(45);bool forceSleep=false,sleeping=false,dragging=false,pressed=false,hover=false,manualHidden=false,fullscreenHidden=false,quitting=false,busyPoll=false;
  Native.Point pressPoint,lastDrag;Native.Rect pressRect;System.Windows.Point touchPoint;
  int lastFrame=-1;public string StatusText{get{return !Settings.LinkChatGPT?"联动已关闭":Link.Current.Connected&&!Link.Current.Stale&&DateTime.UtcNow-Link.Current.At>=TimeSpan.FromSeconds(10)?"状态待确认 · 上次检测已过期":Link.Current.Label;}} public string CurrentAction{get{return state;}}
  public PetWindow(bool isPreview){preview=isPreview;Settings=Store.Load();string catalog=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","pets.json");Pets=Store.Json.Deserialize<List<PetInfo>>(File.ReadAllText(catalog));if(Pets==null||Pets.Count!=6)throw new Exception("六套宠物素材清单不完整，请完整解压迁移包。");if(!Pets.Any(p=>p.Id==Settings.Skin))Settings.Skin=Pets[0].Id;
   PetVisuals.Apply(this,false);PetVisuals.InstallTooltips(this);Title="琪亚娜桌宠";WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;Topmost=true;ShowInTaskbar=false;ShowActivated=false;
   Grid root=new Grid();root.RowDefinitions.Add(new RowDefinition{Height=new GridLength(58)});root.RowDefinitions.Add(new RowDefinition());root.RowDefinitions.Add(new RowDefinition{Height=new GridLength(40)});
   bubble.Background=new SolidColorBrush(Color.FromArgb(245,255,253,255));bubble.BorderBrush=new SolidColorBrush(Color.FromRgb(222,210,240));bubble.BorderThickness=new Thickness(1);bubble.CornerRadius=new CornerRadius(PetVisuals.SurfaceRadius);bubble.Padding=new Thickness(10,5,10,5);bubble.Margin=new Thickness(2,0,2,4);bubble.HorizontalAlignment=HorizontalAlignment.Center;bubble.VerticalAlignment=VerticalAlignment.Bottom;bubble.IsHitTestVisible=false;bubble.Visibility=Visibility.Hidden;
   bubbleText.FontFamily=new FontFamily("Microsoft YaHei UI");bubbleText.FontSize=12;bubbleText.MaxHeight=38;bubbleText.TextTrimming=TextTrimming.CharacterEllipsis;bubbleText.Foreground=new SolidColorBrush(Color.FromRgb(78,60,105));bubbleText.TextWrapping=TextWrapping.Wrap;bubbleText.TextAlignment=TextAlignment.Center;bubble.Child=bubbleText;root.Children.Add(bubble);
   sprite.Stretch=Stretch.Fill;RenderOptions.SetBitmapScalingMode(sprite,BitmapScalingMode.HighQuality);sprite.ToolTip="摸摸头、戳戳脸，或按住拖动\n右键打开菜单 · 双击打招呼";Grid.SetRow(sprite,1);root.Children.Add(sprite);
   zzz.Text="z Z z";zzz.FontFamily=new FontFamily("Segoe UI");zzz.FontWeight=FontWeights.Bold;zzz.FontSize=21;zzz.Foreground=new SolidColorBrush(Color.FromRgb(134,106,179));zzz.HorizontalAlignment=HorizontalAlignment.Right;zzz.VerticalAlignment=VerticalAlignment.Top;zzz.Margin=new Thickness(0,4,10,0);zzz.IsHitTestVisible=false;zzz.Visibility=Visibility.Hidden;Grid.SetRow(zzz,1);root.Children.Add(zzz);BuildToolbar(root);Content=root;
   sprite.MouseLeftButtonDown+=PointerDown;sprite.MouseMove+=PointerMove;sprite.MouseLeftButtonUp+=PointerUp;sprite.LostMouseCapture+=delegate{pressed=false;dragging=false;};sprite.MouseEnter+=delegate{hover=true;};sprite.MouseLeave+=delegate{hover=false;};sprite.MouseRightButtonUp+=delegate{OpenMenu();};
   Deactivated+=delegate{if(keyboardToolbarMode){keyboardToolbarMode=false;Native.SetMenuActivation(handle,false);}};PreviewKeyDown+=ToolbarKey;
   SourceInitialized+=delegate{handle=new WindowInteropHelper(this).Handle;Native.StylePet(handle,false);source=HwndSource.FromHwnd(handle);source.AddHook(Hook);InitializeHotkey();};
   Loaded+=delegate{if(initialized)return;initialized=true;LoadSkin();ResizePet();if(Settings.HasPosition){MoveTo(Settings.X,Settings.Y);ClampPosition();}else CenterPet();if(!preview){CreateTray();Say("我来陪你啦！右键看看我的小菜单。",7);Store.Log("Started "+Maintenance.Version);}animation.Interval=TimeSpan.FromMilliseconds(33);animation.Tick+=Tick;animation.Start();poll.Interval=TimeSpan.FromSeconds(2);poll.Tick+=Poll;poll.Start();Poll(null,EventArgs.Empty);StartMusic();StartMenuPriority();StartComfort();StartPolish();StartUsability();StartCompanionApps();if(preview){OpenSettings();var captureTimer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};captureTimer.Tick+=delegate{captureTimer.Stop();CapturePreview();};captureTimer.Start();}};
   Closing+=delegate(object sender,System.ComponentModel.CancelEventArgs e){if(quitting)return;if(!closeScheduled){e.Cancel=true;closeScheduled=true;ClosePetMenu();if(sprite.IsMouseCaptured)sprite.ReleaseMouseCapture();Dispatcher.BeginInvoke(new Action(Close),DispatcherPriority.ApplicationIdle);return;}quitting=true;ClosePetMenu();if(noticeCenter!=null)noticeCenter.Close();if(storageWindow!=null)storageWindow.Close();StopMenuPriority();StopComfort();StopMusic();animation.Stop();poll.Stop();Link.Dispose();if(source!=null)source.RemoveHook(Hook);if(hotkey!=null)hotkey.Dispose();if(tray!=null){tray.Visible=false;tray.Dispose();}Save();};
   // End the WPF loop even after a native popup/WinForms synchronization context.
   Closed+=delegate{if(quitting&&!Dispatcher.HasShutdownStarted)Dispatcher.BeginInvokeShutdown(DispatcherPriority.ApplicationIdle);};
  }
  IntPtr Hook(IntPtr w,int msg,IntPtr a,IntPtr b,ref bool handled){if(msg==0x007e||msg==0x02e0||msg==0x001a)RepairDisplay();if(msg==0x0218&&(a.ToInt32()==6||a.ToInt32()==7||a.ToInt32()==18)){ScheduleDisplayRecovery(true);}if(msg==0x02b1){if(a.ToInt32()==7)sessionLocked=true;if(a.ToInt32()==8){sessionLocked=false;ScheduleDisplayRecovery(true);}}if(msg==0x0312&&hotkey!=null&&hotkey.Matches(a,b)){Recover();handled=true;}return IntPtr.Zero;}
  void LoadSkin(){if(!frames.ContainsKey(Settings.Skin)){PetInfo info=Pets.First(p=>p.Id==Settings.Skin);string atlas=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets",info.Atlas);BitmapImage bitmap=new BitmapImage();bitmap.BeginInit();bitmap.CacheOption=BitmapCacheOption.OnLoad;bitmap.UriSource=new Uri(atlas);bitmap.EndInit();bitmap.Freeze();if(bitmap.PixelWidth!=1536||bitmap.PixelHeight!=2288)throw new Exception("图集尺寸错误："+info.Name);BitmapSource[] crop=new BitmapSource[88];for(int i=0;i<88;i++){CroppedBitmap frame=new CroppedBitmap(bitmap,new Int32Rect((i%8)*192,(i/8)*208,192,208));frame.Freeze();crop[i]=frame;}frames[Settings.Skin]=crop;}lastFrame=-1;SetFrame(0,0);}
  void SetFrame(int row,int col){int i=row*8+col;if(i==lastFrame)return;ShowSprite(frames[Settings.Skin][i],"base",i);lastFrame=i;}
  void EnsureExtras(){if(extras.ContainsKey(Settings.Skin))return;string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets",Settings.Skin+"-extra.png");if(!File.Exists(path))return;BitmapImage atlas=new BitmapImage();atlas.BeginInit();atlas.CacheOption=BitmapCacheOption.OnLoad;atlas.UriSource=new Uri(path);atlas.EndInit();atlas.Freeze();if(atlas.PixelWidth!=768||atlas.PixelHeight!=1248)throw new Exception("新增动画图集尺寸错误");BitmapSource[] list=new BitmapSource[6];for(int i=0;i<6;i++){CroppedBitmap frame=new CroppedBitmap(atlas,new Int32Rect((i%2)*384,(i/2)*416,384,416));frame.Freeze();list[i]=frame;}extras[Settings.Skin]=list;}
  void DrawAnimation(double elapsed){if(DrawTransition(elapsed))return;if(Settings.ReduceMotion)elapsed=state=="groom"||state=="stretch"?1000:0;if(DrawAmbient(elapsed))return;EnsureExtras();if(extras.ContainsKey(Settings.Skin)&&(state=="pat"||state=="carry"||state=="sleep")){int row=state=="pat"?0:state=="carry"?1:2;int col=(int)(elapsed/(state=="sleep"?1800:state=="carry"?280:650))%2;int i=row*2+col;if(lastFrame!=100+i){ShowSprite(extras[Settings.Skin][i],"extra",i);lastFrame=100+i;}return;}string fallback=state=="pat"?"jumping":state=="carry"?dragState:state;SetFrame(Rules.Row(fallback),Rules.Frame(fallback,elapsed));}
  public void SelectSkin(string id){if(!Pets.Any(p=>p.Id==id))return;Settings.Skin=id;LoadSkin();React("waving","换好衣服啦！",3);Save();}
  public void ResizePet(){Width=Math.Max((Settings.LargeTouchTargets?210:176)*Settings.ToolbarPercent/100d,Settings.Size);Height=Settings.Size*208.0/192+58+(Settings.LargeTouchTargets?48:40)*Settings.ToolbarPercent/100d;sprite.Width=Settings.Size;sprite.Height=Settings.Size*208.0/192;ApplySpriteAlignment();if(IsLoaded)Dispatcher.BeginInvoke(new Action(ClampPosition),DispatcherPriority.Loaded);}
  void MoveTo(double x,double y){if(handle!=IntPtr.Zero)Native.SetWindowPos(handle,IntPtr.Zero,(int)Math.Round(x),(int)Math.Round(y),0,0,0x0001|0x0004|0x0010);}
  Native.Rect Bounds(){Native.Rect r;Native.GetWindowRect(handle,out r);return r;}
  public void CenterPet(){if(handle==IntPtr.Zero)return;walkPlanner.Reset();var area=Forms.Screen.FromHandle(handle).WorkingArea;Native.Rect r=Bounds();MoveTo(area.Right-r.Width-70,area.Bottom-r.Height-8);velocity=0;walkUntil=0;Save();}
  void ClampPosition(){if(handle==IntPtr.Zero)return;Native.Rect r=Bounds();var fit=ScreenPlacement.Fit(new System.Drawing.Rectangle(r.Left,r.Top,r.Width,r.Height),Forms.Screen.AllScreens.Select(s=>s.WorkingArea).ToArray());MoveTo(fit.Left,fit.Top);}
  public void Recover(){manualHidden=false;Native.StylePet(handle,false);awakeUntil=DateTime.Now.AddMinutes(10);forceSleep=false;if(!IsVisible)Show();CenterPet();Say("在这里！",3);}
  public void Wake(){forceSleep=false;awakeUntil=DateTime.Now.AddMinutes(30);React("waving","好，接下来陪你一会儿。",4);}
  public void Rest(){forceSleep=true;interaction=null;interactionUntil=0;walkUntil=0;Say("我先眯一会儿，点我就醒。",4);}
  public void ApplySettings(){Settings.Validate();Link.StaleGraceSeconds=Settings.NotificationGraceSeconds;ApplyInputOptions();ApplyAppearance();UpdateMenuPriority(externalMenus!=null&&externalMenus.Active);poll.Interval=TimeSpan.FromSeconds(2);musicTimer.Interval=TimeSpan.FromMilliseconds(850);linkFailures=musicFailures=0;lastQuietScan=-10;UpdateQuiet(clock.Elapsed.TotalSeconds);ResizePet();nextWalk=clock.Elapsed.TotalSeconds+3;walkUntil=0;if(!Settings.SleepSchedule&&!Settings.IdleSleep)forceSleep=false;Save();}
  public void Say(string text,double seconds){if(!Settings.Bubbles||quietActive||sessionLocked)return;bubbleText.Text=text;bubble.Visibility=Visibility.Visible;bubbleUntil=clock.Elapsed.TotalSeconds+seconds;}
  public void React(string action,string text,double seconds){interaction=action;interactionUntil=clock.Elapsed.TotalSeconds+seconds;walkUntil=0;nextWalk=clock.Elapsed.TotalSeconds+12;Say(text,seconds);}
  void PointerDown(object sender,MouseButtonEventArgs e){if(e.ClickCount>=2){Wake();React("waving","琪亚娜，随时待命！",3);return;}forceSleep=false;awakeUntil=DateTime.Now.AddMinutes(15);Native.GetCursorPos(out pressPoint);lastDrag=pressPoint;pressRect=Bounds();touchPoint=e.GetPosition(sprite);pressed=true;dragging=false;velocity=0;walkUntil=0;sprite.CaptureMouse();e.Handled=true;}
  void PointerMove(object sender,MouseEventArgs e){if(!pressed||Settings.LockPetPosition)return;Native.Point p;Native.GetCursorPos(out p);int dx=p.X-pressPoint.X,dy=p.Y-pressPoint.Y;if(Math.Abs(dx)+Math.Abs(dy)>(e.StylusDevice==null?5:12)*DpiScale)dragging=true;if(dragging){MoveTo(pressRect.Left+dx,pressRect.Top+dy);if(p.X!=lastDrag.X)dragState=p.X<lastDrag.X?"running-left":"running-right";lastDrag=p;}e.Handled=true;}
  void PointerUp(object sender,MouseButtonEventArgs e){if(!pressed)return;bool moved=dragging;pressed=false;dragging=false;sprite.ReleaseMouseCapture();ClampPosition();if(moved){walkPlanner.Reset();React("waving","这里也不错！",2);}else if(ambientPlanner.Touch(clock.Elapsed.TotalSeconds)){double y=touchPoint.Y/sprite.ActualHeight;if(y<0.38)React("pat","嘿嘿，再摸一下！",3);else if(y<0.62)React("jumping","被你戳到啦！",2.5);else React("waving","我在呢～",3);}Save();e.Handled=true;}
  void Tick(object sender,EventArgs e){double now=clock.Elapsed.TotalSeconds,dt=Math.Min(.1,Math.Max(0,now-lastTick));lastTick=now;
   if(now-lastSlow>0.6){lastSlow=now;UpdateQuiet(now);fullscreenHidden=Settings.HideFullscreen&&Native.IsFullscreen(handle);string hidden=RefreshPetVisibility();
    MaintainWindowStack();
    if(!preview){CheckLinkHealth();string exitRequest=Path.Combine(Store.Root,"exit.request");if(File.Exists(exitRequest)){try{File.Delete(exitRequest);}catch{}Close();return;}string request=Path.Combine(Store.Root,"show.request");if(File.Exists(request)){try{File.Delete(request);}catch{}Recover();}
     if(now-lastStatusWrite>3){lastStatusWrite=now;try{Store.Atomic("status.json",new{version=Maintenance.Version,action=state,skin=Settings.Skin,hidden=hidden,menuYielding=menuLayerYielding,menuCandidates=externalMenus==null?0:externalMenus.PriorityWindows.Length,menuMonitorActive=externalMenus!=null&&externalMenus.Active,menuFallback=fallbackMenu!=IntPtr.Zero,fullscreenReason=Settings.HideFullscreen?Native.FullscreenReason:"disabled",chatgpt=Link.Current,aiEnabled=false});}catch{}}}
    if(settingsWindow!=null)settingsWindow.UpdateStatus();
   }
   TickPolish(now);UpdateToolbar(now);UpdateMusic();if(!IsVisible)return;if(menuLayerYielding||displayPending){DrawAnimation((now-stateAt)*1000);return;}
   bool scheduled=Settings.SleepSchedule&&Rules.IsSleepTime(DateTime.Now.TimeOfDay,TimeSpan.Parse(Settings.Bedtime),TimeSpan.Parse(Settings.WakeTime));
   sleeping=forceSleep||(DateTime.Now>=awakeUntil&&(scheduled||(Settings.IdleSleep&&Native.IdleSeconds()>=Settings.IdleMinutes*60)));
   if(sleeping)walkUntil=0;
   zzz.Visibility=sleeping?Visibility.Visible:Visibility.Hidden;
   if(bubble.Visibility==Visibility.Visible&&now>bubbleUntil)bubble.Visibility=Visibility.Hidden;
   if(now>=interactionUntil)interaction=null;
   string linked=Settings.LinkChatGPT&&Link.Current.Connected&&!Link.Current.Stale&&DateTime.UtcNow-Link.Current.At<TimeSpan.FromSeconds(10)?Link.Current.State:"idle";

   bool canWalk=Settings.Walking&&!Settings.ReduceMotion&&clock.Elapsed.TotalSeconds>=interactionHoldUntil&&noticeCenter==null&&storageWindow==null&&!Settings.LockPetPosition&&!quietActive&&!sleeping&&!pressed&&!hover&&!ToolbarActive&&settingsWindow==null&&interaction==null&&linked=="idle";
   Native.Rect rect=Bounds();var area=Forms.Screen.FromHandle(handle).WorkingArea;if(ToolbarActive)interactionHoldUntil=now+1.8;if(CursorBlocksWalk(rect,DpiScale)){canWalk=false;walkUntil=0;nextWalk=Math.Max(nextWalk,now+2);}
   if(!Settings.LockPetPosition&&!pressed&&!ToolbarActive&&Settings.Gravity&&rect.Bottom<area.Bottom-3){velocity=Math.Min(750,velocity+1000*dt);MoveTo(rect.Left,Settings.ReduceMotion?area.Bottom-rect.Height:Math.Min(area.Bottom-rect.Height,rect.Top+velocity*dt));}else velocity=0;
   double scale=DpiScale;if(canWalk&&now>=nextWalk){targetX=CursorSafeTarget(rect,area,scale);walkMotion.Begin(rect.Left,targetX);walkUntil=now+Math.Abs(targetX-rect.Left)/(Settings.WalkSpeed*scale)+2;nextWalk=walkUntil+Settings.WalkPauseMin+random.NextDouble()*(Settings.WalkPauseMax-Settings.WalkPauseMin);}
   bool walking=canWalk&&now<walkUntil&&Math.Abs(targetX-rect.Left)>3;
   string direction=targetX<rect.Left?"running-left":"running-right";
   if(walking){if(Math.Abs(walkMotion.Position-rect.Left)>2)walkMotion.Begin(rect.Left,targetX);double x=walkMotion.Step(dt,Settings.WalkSpeed*scale,140*scale);MoveTo(Rules.Clamp(x,area.Left,area.Right-rect.Width),rect.Top);}else walkMotion.Begin(rect.Left,targetX);
   AmbientTick(now,!Settings.ReduceMotion&&noticeCenter==null&&storageWindow==null&&!sleeping&&!walking&&!pressed&&!hover&&!ToolbarActive&&!quietActive&&settingsWindow==null&&interaction==null&&linked=="idle");
   string chosen=Rules.ChooseState(sleeping,dragging,dragState,interaction,linked,walking,direction);if(dragging)chosen="carry";if(chosen!=state){state=chosen;stateAt=now;lastFrame=-1;if(Settings.SmoothTransitions&&MotionSettings.Enabled&&!dragging)sprite.BeginAnimation(OpacityProperty,new System.Windows.Media.Animation.DoubleAnimation(.82,1,TimeSpan.FromMilliseconds(120)));}
   DrawAnimation((now-stateAt)*1000);
   string routine=sleeping?"sleep":DateTime.Now.Hour<12?"morning":DateTime.Now.Hour<18?"day":"evening";
   if(lastRoutine.Length==0)lastRoutine=routine;else if(routine!=lastRoutine){lastRoutine=routine;if(routine=="sleep")Say("晚安，休息一下。",4);else if(routine=="morning")Say("早上好，新的一天开始啦！",5);}
  }
  async void Poll(object sender,EventArgs e){if(busyPoll||preview)return;busyPoll=true;try{Link.StaleGraceSeconds=Settings.NotificationGraceSeconds;await Link.Poll(Settings.LinkChatGPT,Settings.BackgroundMini);ObserveBridgeNotifications();}finally{busyPoll=false;if(!quitting){linkFailures=Link.Current.Connected?0:linkFailures+1;poll.Interval=TimeSpan.FromSeconds(Settings.ResourceSaving?PolishRules.RetrySeconds(linkFailures,2):2);}}}
  public void Save(){try{if(handle!=IntPtr.Zero){Native.Rect r=Bounds();Settings.X=r.Left;Settings.Y=r.Top;Settings.HasPosition=true;}TrackPreferences();Store.Save(Settings);RememberDisplay();}catch(Exception e){Store.Log("Save: "+e.Message);}}
  public void OpenSettings(){if(settingsWindow!=null){settingsWindow.Activate();return;}settingsWindow=new SettingsWindow(this);settingsWindow.Closed+=delegate{settingsWindow=null;};settingsWindow.Show();}
  public void CapturePreview(){if(settingsWindow!=null)settingsWindow.Capture(Path.Combine(Store.Root,"settings-preview.png"));UpdateLayout();var bitmap=new RenderTargetBitmap((int)ActualWidth,(int)ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(this);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var stream=File.Create(Path.Combine(Store.Root,"pet-preview.png"))){encoder.Save(stream);}}
  void OpenMenu(){ContextMenu menu=PetMenus.Create();menu.PlacementTarget=sprite;menu.Placement=System.Windows.Controls.Primitives.PlacementMode.MousePoint;
   var skins=Submenu(menu,"换一套衣服");foreach(PetInfo pet in Pets){string id=pet.Id;var item=new MenuItem{Header=pet.Name,IsCheckable=true,IsChecked=id==Settings.Skin};item.Click+=delegate{SelectSkin(id);};skins.Items.Add(item);}
   var interact=Submenu(menu,"互动与休息");Add(interact,"摸摸头",delegate{Wake();React("pat","嘿嘿，今天也要开心！",3);});Add(interact,"整理头发",delegate{React("groom","整理好啦～",2.1);});Add(interact,"伸个懒腰",delegate{React("stretch","活动一下！",2.1);});Add(interact,"打个招呼",delegate{Wake();React("waving","琪亚娜，随时待命！",3);});Add(interact,sleeping?"醒来陪我（30 分钟）":"休息一会儿",delegate{if(sleeping)Wake();else Rest();});
   var scenes=Submenu(menu,"切换使用场景");for(int i=0;i<Settings.Scenes.Length;i++){int index=i;Add(scenes,Settings.Scenes[i].Name,delegate{ApplyScene(index);});}
   var bars=Submenu(menu,"音乐与工具栏");Add(bars,Settings.MusicEnabled?"收起音乐栏":"显示音乐栏",delegate{Settings.MusicEnabled=!Settings.MusicEnabled;ApplySettings();});Add(bars,Settings.MusicHoverOnly?"音乐栏设为常驻":"音乐栏改为悬停显示",delegate{SetMusicPinned(Settings.MusicHoverOnly);});Add(bars,Settings.ToolbarPinned?"工具栏改为悬停显示":"工具栏设为常驻",delegate{SetToolbarPinned(!Settings.ToolbarPinned);});Add(bars,"连接网易云音乐",LaunchMusic);
   var desktop=Submenu(menu,"桌面活动与位置");Add(desktop,Settings.Walking?"暂停自由走动":"开启自由走动",delegate{Settings.Walking=!Settings.Walking;ApplySettings();});Add(desktop,Settings.LockPetPosition?"解锁宠物位置":"锁定宠物位置",delegate{Settings.LockPetPosition=!Settings.LockPetPosition;ApplySettings();});Add(desktop,"回到屏幕右下角",CenterPet);Add(desktop,"鼠标穿透（从托盘恢复）",delegate{Native.StylePet(handle,true);Say("双击托盘图标可以恢复互动。",4);});Add(desktop,"暂时隐藏（从托盘恢复）",delegate{manualHidden=true;Hide();});
   menu.Items.Add(new Separator());Add(menu,manualQuiet?"关闭临时免打扰":"开启临时免打扰",ToggleQuiet);Add(menu,"通知中心",OpenNoticeCenter);Add(menu,"设置",OpenSettings);menu.Items.Add(new Separator());Add(menu,"退出独立桌宠",delegate{Close();});ShowPetMenu(menu);
  }
  static MenuItem Submenu(ItemsControl parent,string label){var item=new MenuItem{Header=label};parent.Items.Add(item);return item;}
  static void Add(ItemsControl menu,string label,Action callback){MenuItem item=new MenuItem{Header=label};item.Click+=delegate{callback();};menu.Items.Add(item);}
  void CreateTray(){tray=new Forms.NotifyIcon();string icon=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","pet.ico");tray.Icon=File.Exists(icon)?new System.Drawing.Icon(icon):System.Drawing.SystemIcons.Information;tray.Text="琪亚娜桌宠 · 双击找回";Forms.ContextMenuStrip menu=new Forms.ContextMenuStrip();menu.Items.Add("显示 / 找回宠物",null,delegate{Dispatcher.BeginInvoke(new Action(Recover));});menu.Items.Add("切换临时免打扰",null,delegate{Dispatcher.BeginInvoke(new Action(ToggleQuiet));});menu.Items.Add("设置",null,delegate{Dispatcher.BeginInvoke(new Action(OpenSettings));});menu.Items.Add("退出独立桌宠",null,delegate{Dispatcher.BeginInvoke(new Action(Close));});tray.ContextMenuStrip=menu;tray.DoubleClick+=delegate{Dispatcher.BeginInvoke(new Action(Recover));};tray.Visible=true;}
 }
}
