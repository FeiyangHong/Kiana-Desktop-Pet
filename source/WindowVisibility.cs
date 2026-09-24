using System;using System.Diagnostics;using System.Drawing;using System.Runtime.InteropServices;using System.Text;
namespace KianaPet {
 public static class FullscreenRules {
  public static bool IsFullscreen(Native.Rect frame,Native.Rect client,Rectangle screen,bool maximized,long style){
   // Normal maximization retains a caption/resize frame, even with custom Electron chrome.
   if(maximized&&(style&0x00C40000L)!=0)return false;
   return frame.Width>0&&frame.Height>0&&client.Width>0&&client.Height>0&&Native.Covers(frame,screen)&&Native.Covers(client,screen);
  }
 }
 public static partial class Native {
  [DllImport("user32.dll")]public static extern bool IsZoomed(IntPtr window);
  [DllImport("user32.dll")]public static extern bool IsIconic(IntPtr window);
  [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr window);
  [DllImport("user32.dll")]static extern bool GetClientRect(IntPtr window,out Rect rect);
  [DllImport("user32.dll")]static extern bool ClientToScreen(IntPtr window,ref Point point);
  [DllImport("user32.dll")]static extern IntPtr GetWindow(IntPtr window,uint command);
  [DllImport("dwmapi.dll")]static extern int DwmGetWindowAttribute(IntPtr window,int attribute,out Rect rect,int size);
  public static string FullscreenReason="not-checked";
  public static bool IsFullscreen(IntPtr pet){
   IntPtr foreground=GetForegroundWindow();FullscreenReason="no-foreground";
   if(foreground==IntPtr.Zero||foreground==pet||IsIconic(foreground)||!IsWindowVisible(foreground))return false;
   uint pid;GetWindowThreadProcessId(foreground,out pid);if(pid==(uint)Process.GetCurrentProcess().Id){FullscreenReason="own-window";return false;}
   StringBuilder cls=new StringBuilder(256);GetClassName(foreground,cls,256);if(cls.ToString()=="Progman"||cls.ToString()=="WorkerW"||cls.ToString().Contains("Shell_Tray")){FullscreenReason="shell";return false;}
   Rect frame,client;if(!GetWindowRect(foreground,out frame)||!GetClientRect(foreground,out client)){FullscreenReason="geometry-unavailable";return false;}
   Rect visible;if(DwmGetWindowAttribute(foreground,9,out visible,16)==0&&visible.Width>0&&visible.Height>0)frame=visible;
   Point origin=new Point();if(!ClientToScreen(foreground,ref origin)){FullscreenReason="geometry-unavailable";return false;}client.Left+=origin.X;client.Right+=origin.X;client.Top+=origin.Y;client.Bottom+=origin.Y;
   bool maximized=IsZoomed(foreground);long style=GetStyle(foreground,-16).ToInt64();
   bool full=FullscreenRules.IsFullscreen(frame,client,System.Windows.Forms.Screen.FromHandle(pet).Bounds,maximized,style);
   FullscreenReason=full?"fullscreen-on-pet-monitor":maximized&&(style&0x00C40000L)!=0?"normal-maximized":"windowed-or-other-monitor";return full;
  }
  public static bool IsTopmost(IntPtr window){return (GetStyle(window,-20).ToInt64()&8)!=0;}
  public static bool IsAbove(IntPtr window,IntPtr other){for(IntPtr h=GetWindow(window,3);h!=IntPtr.Zero;h=GetWindow(h,3))if(h==other)return false;return true;}
  public static bool IsOurWindow(IntPtr window){uint pid;GetWindowThreadProcessId(window,out pid);return pid==(uint)Process.GetCurrentProcess().Id;}
  public static void RaiseWithoutActivation(IntPtr window){SetWindowPos(window,new IntPtr(-1),0,0,0,0,0x0001|0x0002|0x0010);}
 }
 public sealed partial class PetWindow {
  IntPtr lastStackForeground;Native.Rect lastStackBounds;bool wasStackVisible,lastStackMaximized,stackRepairAttempted;
  void MaintainWindowStack(){
   IntPtr detected=!preview&&Settings.YieldToMenus?Native.ChromiumTrayPriority(handle):IntPtr.Zero;
   if(detected!=IntPtr.Zero)fallbackMenu=detected;
   else if(!Settings.YieldToMenus||preview||fallbackMenu!=Native.GetForegroundWindow())fallbackMenu=IntPtr.Zero;
   bool priority=(externalMenus!=null&&externalMenus.Active)||fallbackMenu!=IntPtr.Zero;
   if(menuLayerYielding!=priority)UpdateMenuPriority(priority);
   if(menuLayerYielding){ApplyMenuLayers();wasStackVisible=false;return;}if(preview)return;if(!IsVisible||lastHidden.Length>0){wasStackVisible=false;return;}
   IntPtr foreground=Native.GetForegroundWindow();Native.Rect bounds;Native.GetWindowRect(foreground,out bounds);bool maximized=Native.IsZoomed(foreground);
   bool changed=!wasStackVisible||foreground!=lastStackForeground||maximized!=lastStackMaximized||!bounds.Equals(lastStackBounds);
   wasStackVisible=true;lastStackForeground=foreground;lastStackBounds=bounds;lastStackMaximized=maximized;
   if(changed)stackRepairAttempted=false;
   if(foreground==IntPtr.Zero||Native.IsOurWindow(foreground))return;
   // Repair only when covered (or the topmost bit was lost). One repair per foreground/geometry
   // change avoids a repeated z-order fight with another app that insists on staying above us.
   if(!stackRepairAttempted&&(!Native.IsTopmost(handle)||(Native.IsTopmost(foreground)&&!Native.IsAbove(handle,foreground)))){Native.RaiseWithoutActivation(handle);stackRepairAttempted=true;}
  }
 }
}
