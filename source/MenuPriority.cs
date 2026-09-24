using System;using System.Collections.Generic;using System.Linq;using System.Runtime.InteropServices;using System.Text;using System.Windows;using System.Windows.Threading;using Forms=System.Windows.Forms;
namespace KianaPet {
 public sealed class MenuWindowInfo {public IntPtr Handle;public string ClassName;public Native.Rect Bounds;public long Style;}
 public static class MenuPriorityRules {
  public static bool TrayPanel(string name){return name=="NotifyIconOverflowWindow"||name=="TopLevelWindowForOverflowXamlIsland";}
  public static bool StandardPopup(string name,long style){return name=="#32768"||(name.StartsWith("WindowsForms10.Window",StringComparison.Ordinal)&&(style&0x80000000L)!=0&&(style&0x00C40000L)==0);}
  public static bool TrayPopup(MenuWindowInfo window,Native.Point anchor,System.Drawing.Rectangle screen){
   var r=window.Bounds;if((window.Style&0x80000000L)==0||(window.Style&0x00C40000L)!=0||r.Width<40||r.Height<20||r.Width>screen.Width*.8||r.Height>screen.Height*.9)return false;
   string c=window.ClassName;if(c.IndexOf("tooltip",StringComparison.OrdinalIgnoreCase)>=0||c=="SysShadow"||c=="Shell_TrayWnd"||c=="Shell_SecondaryTrayWnd")return false;
   if(r.Right<=screen.Left||r.Left>=screen.Right||r.Bottom<=screen.Top||r.Top>=screen.Bottom)return false;
   int dx=Math.Max(0,Math.Max(r.Left-anchor.X,anchor.X-r.Right)),dy=Math.Max(0,Math.Max(r.Top-anchor.Y,anchor.Y-r.Bottom));return dx<=240&&dy<=240;
  }
  public static string HiddenReason(bool locked,bool manual,bool fullscreen,bool quiet,bool menus){return locked?"locked":manual?"manual":fullscreen?"fullscreen":quiet?"quiet":menus?"external-menu":"";}
 }
 public static partial class Native {
  delegate bool MenuEnumProc(IntPtr window,IntPtr param);
  [DllImport("user32.dll")]static extern bool EnumWindows(MenuEnumProc callback,IntPtr param);
  [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(Point point);
  [DllImport("user32.dll")]static extern IntPtr GetAncestor(IntPtr window,uint flags);
  [StructLayout(LayoutKind.Sequential)]struct GuiMenuInfo {public uint Size,Flags;public IntPtr Active,Focus,Capture,MenuOwner,MoveSize,Caret;public Rect CaretRect;}
  [DllImport("user32.dll")]static extern bool GetGUIThreadInfo(uint thread,ref GuiMenuInfo info);
  public static bool ForegroundHasExternalMenu(){var info=new GuiMenuInfo{Size=(uint)Marshal.SizeOf(typeof(GuiMenuInfo))};return GetGUIThreadInfo(0,ref info)&&(info.Flags&0x1c)!=0&&info.MenuOwner!=IntPtr.Zero&&!IsOurWindow(info.MenuOwner);}
  static string WindowClass(IntPtr window){var text=new StringBuilder(256);GetClassName(window,text,256);return text.ToString();}
  public static bool IsTrayPoint(Point point){string cls=WindowClass(GetAncestor(WindowFromPoint(point),2));return cls=="Shell_TrayWnd"||cls=="Shell_SecondaryTrayWnd"||cls=="NotifyIconOverflowWindow"||cls=="TopLevelWindowForOverflowXamlIsland";}
  public static List<MenuWindowInfo> ExternalMenuWindows(){var result=new List<MenuWindowInfo>();EnumWindows(delegate(IntPtr h,IntPtr unused){if(!IsPresented(h)||IsOurWindow(h))return true;Rect rect;if(!GetWindowRect(h,out rect)||rect.Width<=0||rect.Height<=0)return true;result.Add(new MenuWindowInfo{Handle=h,ClassName=WindowClass(h),Bounds=rect,Style=GetStyle(h,-16).ToInt64()});return true;},IntPtr.Zero);return result;}
 }
 // Event notifications give prompt response; periodic reconciliation handles missing menu-end
 // events, nested submenus and applications which destroy their menu process abruptly.
 public sealed class ExternalMenuMonitor:IDisposable {
  delegate void WinEventProc(IntPtr hook,uint type,IntPtr window,int obj,int child,uint thread,uint time);
  delegate IntPtr MouseProc(int code,IntPtr message,IntPtr data);
  [StructLayout(LayoutKind.Sequential)]struct MouseData {public Native.Point Point;public uint Mouse,Flags,Time;public UIntPtr Extra;}
  [DllImport("user32.dll")]static extern IntPtr SetWinEventHook(uint min,uint max,IntPtr module,WinEventProc callback,uint process,uint thread,uint flags);
  [DllImport("user32.dll")]static extern bool UnhookWinEvent(IntPtr hook);
  [DllImport("user32.dll")]static extern IntPtr SetWindowsHookEx(int id,MouseProc callback,IntPtr module,uint thread);
  [DllImport("user32.dll")]static extern bool UnhookWindowsHookEx(IntPtr hook);
  [DllImport("user32.dll")]static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr GetModuleHandle(string name);
  readonly Dispatcher dispatcher;readonly Action<bool> changed;readonly DispatcherTimer timer,releaseTimer;readonly WinEventProc events;readonly MouseProc mouse;
  IntPtr menuHook,windowHook,mouseHook;bool disposed,queued,active;DateTime armUntil;Native.Point trayAnchor;
  HashSet<IntPtr> visible=new HashSet<IntPtr>(),baseline=new HashSet<IntPtr>(),customMenus=new HashSet<IntPtr>();
  public IntPtr[] PriorityWindows{get;private set;}
  public bool Active{get{return active;}}public bool Disposed{get{return disposed;}}
  public ExternalMenuMonitor(Dispatcher ui,Action<bool> notify){dispatcher=ui;changed=notify;events=Event;mouse=Mouse;
   menuHook=SetWinEventHook(4,7,IntPtr.Zero,events,0,0,2);windowHook=SetWinEventHook(0x8001,0x8003,IntPtr.Zero,events,0,0,2);
   mouseHook=SetWindowsHookEx(14,mouse,GetModuleHandle(null),0);
   if(menuHook==IntPtr.Zero||windowHook==IntPtr.Zero)Store.Log("Menu event hook unavailable; window reconciliation remains active.");if(mouseHook==IntPtr.Zero)Store.Log("Tray right-click detection unavailable; standard menu detection remains active.");
   releaseTimer=new DispatcherTimer(DispatcherPriority.Input,dispatcher){Interval=TimeSpan.FromMilliseconds(24)};releaseTimer.Tick+=delegate{releaseTimer.Stop();Scan(true);};
   timer=new DispatcherTimer(DispatcherPriority.Background,dispatcher){Interval=TimeSpan.FromMilliseconds(200)};timer.Tick+=delegate{Scan();};timer.Start();Scan();
  }
  void Event(IntPtr hook,uint type,IntPtr window,int obj,int child,uint thread,uint time){if(type>=0x8000&&(obj!=0||child!=0))return;QueueScan();}
  IntPtr Mouse(int code,IntPtr message,IntPtr data){if(code>=0&&message.ToInt32()==0x204&&!disposed){var info=(MouseData)Marshal.PtrToStructure(data,typeof(MouseData));if(Native.IsTrayPoint(info.Point)){ArmTray(info.Point);}}return CallNextHookEx(mouseHook,code,message,data);}
  void ArmTray(Native.Point point){trayAnchor=point;baseline=new HashSet<IntPtr>(visible);armUntil=DateTime.UtcNow.AddSeconds(2);QueueScan();}
  void QueueScan(){if(disposed||queued||dispatcher.HasShutdownStarted)return;queued=true;dispatcher.BeginInvoke(DispatcherPriority.Input,new Action(delegate{queued=false;if(!disposed)Scan();}));}
  public void Scan(){Scan(false);}
  void Scan(bool release){if(disposed)return;var windows=Native.ExternalMenuWindows();visible=new HashSet<IntPtr>(windows.Select(w=>w.Handle));customMenus.IntersectWith(visible);var now=DateTime.UtcNow;
   if(now<armUntil){var screen=Forms.Screen.FromPoint(new System.Drawing.Point(trayAnchor.X,trayAnchor.Y)).Bounds;foreach(var w in windows)if(!baseline.Contains(w.Handle)&&MenuPriorityRules.TrayPopup(w,trayAnchor,screen))customMenus.Add(w.Handle);}
   PriorityWindows=windows.Where(w=>customMenus.Contains(w.Handle)||MenuPriorityRules.StandardPopup(w.ClassName,w.Style)||MenuPriorityRules.TrayPanel(w.ClassName)).Select(w=>w.Handle).ToArray();
   bool found=PriorityWindows.Length>0;
   if(found){releaseTimer.Stop();SetActive(true);}else if(active&&!release){if(!releaseTimer.IsEnabled)releaseTimer.Start();}else SetActive(false);
  }
  void SetActive(bool value){if(active==value){changed(value);return;}active=value;if(!value)armUntil=DateTime.MinValue;changed(value);}
  public void Dispose(){if(disposed)return;disposed=true;timer.Stop();releaseTimer.Stop();if(menuHook!=IntPtr.Zero)UnhookWinEvent(menuHook);if(windowHook!=IntPtr.Zero)UnhookWinEvent(windowHook);if(mouseHook!=IntPtr.Zero)UnhookWindowsHookEx(mouseHook);menuHook=windowHook=mouseHook=IntPtr.Zero;customMenus.Clear();GC.KeepAlive(events);GC.KeepAlive(mouse);}
 }
 public sealed partial class PetWindow {
  ExternalMenuMonitor externalMenus;bool menuLayerYielding;
  public bool MenuLayerYielding{get{return menuLayerYielding;}}
  void StartMenuPriority(){if(preview||externalMenus!=null)return;externalMenus=new ExternalMenuMonitor(Dispatcher,delegate(bool active){UpdateMenuPriority(active);});}
  void StopMenuPriority(){if(externalMenus!=null){externalMenus.Dispose();externalMenus=null;}}
  bool menuLayerApplying;
  void ApplyMenuLayers(){ApplyMenuLayers(false);}
  void ApplyMenuLayers(bool force){
   if(menuLayerApplying)return;menuLayerApplying=true;
   try{
    var windows=externalMenus==null?null:externalMenus.PriorityWindows;IntPtr below=windows==null||windows.Length==0?IntPtr.Zero:windows[windows.Length-1];
    if(menuLayerYielding&&below==IntPtr.Zero)return;
    var handles=new List<IntPtr>{handle};if(musicWindow!=null)handles.Add(new System.Windows.Interop.WindowInteropHelper(musicWindow).Handle);
    Native.SetPetLayer(handles.ToArray(),menuLayerYielding?below:IntPtr.Zero,force);
   }finally{menuLayerApplying=false;}
  }
  void UpdateMenuPriority(bool active){if(quitting)return;bool yield=Settings.YieldToMenus&&active;if(menuLayerYielding==yield){ApplyMenuLayers();return;}menuLayerYielding=yield;
   if(yield){walkUntil=0;}else{nextWalk=clock.Elapsed.TotalSeconds+2;}
   ApplyMenuLayers(true);
  }
  string RefreshPetVisibility(){string hidden=MenuPriorityRules.HiddenReason(sessionLocked,manualHidden,fullscreenHidden,quietHidden,false);
   if(hidden!=lastHidden){lastHidden=hidden;if(hidden.Length>0){bubble.Visibility=Visibility.Hidden;walkUntil=0;Hide();if(musicWindow!=null)musicWindow.HideSoft(true);}else Show();}return hidden;
  }
 }
}
