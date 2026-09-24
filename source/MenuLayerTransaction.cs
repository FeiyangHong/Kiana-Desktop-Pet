using System;using System.Collections.Generic;using System.Runtime.InteropServices;
namespace KianaPet {
 public static partial class Native {
  [DllImport("user32.dll")]static extern IntPtr BeginDeferWindowPos(int count);
  [DllImport("user32.dll")]static extern IntPtr DeferWindowPos(IntPtr batch,IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
  [DllImport("user32.dll")]static extern bool EndDeferWindowPos(IntPtr batch);
  [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")]static extern int DwmGetWindowInt(IntPtr window,int attribute,out int value,int size);
  public static bool IsPresented(IntPtr window){int cloaked;return window!=IntPtr.Zero&&IsWindowVisible(window)&&!IsIconic(window)&&(DwmGetWindowInt(window,14,out cloaked,4)!=0||cloaked==0);}
  // Use the menu's own z-order band. Keeping a topmost pet behind a topmost menu avoids
  // dropping it beneath unrelated app windows. No visibility, opacity or focus changes.
  public static bool SetPetLayer(IntPtr[] windows,IntPtr below,bool force){
   if(below!=IntPtr.Zero&&!IsPresented(below))return false;
   bool topmost=below==IntPtr.Zero||IsTopmost(below);var pending=new List<IntPtr>();
   foreach(var h in windows)if(h!=IntPtr.Zero&&(force||IsTopmost(h)!=topmost||below!=IntPtr.Zero&&IsAbove(h,below)))pending.Add(h);
   if(pending.Count==0)return true;IntPtr after=below==IntPtr.Zero?new IntPtr(-1):below;const uint flags=0x1|0x2|0x10|0x200;
   IntPtr batch=BeginDeferWindowPos(pending.Count);if(batch==IntPtr.Zero)return false;
   foreach(var h in pending){batch=DeferWindowPos(batch,h,after,0,0,0,0,flags);if(batch==IntPtr.Zero)return false;}
   return EndDeferWindowPos(batch);
  }
 }
}
