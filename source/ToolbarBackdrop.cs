using System;using System.Runtime.InteropServices;using System.Windows;using System.Windows.Interop;using System.Windows.Media;
namespace KianaPet {
 // Small native backing surface. It never owns focus, input or pet state.
 public sealed class ToolbarGlass:IDisposable {
  HwndSource source;bool unavailable;int lastFog=-1;bool? lastDark;Native.Rect previous;bool shown;
  [StructLayout(LayoutKind.Sequential)]struct Margins{public int Left,Right,Top,Bottom;}
  [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
  [DllImport("dwmapi.dll")]static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd,ref Margins margins);
  [DllImport("gdi32.dll")]static extern IntPtr CreateRoundRectRgn(int left,int top,int right,int bottom,int width,int height);
  [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr obj);
  [DllImport("user32.dll")]static extern int SetWindowRgn(IntPtr hwnd,IntPtr region,bool redraw);
  [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
  [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr hwnd,int command);
  public IntPtr Handle{get{return source==null?IntPtr.Zero:source.Handle;}}
  public bool Available{get{return source!=null&&!unavailable;}}
  public bool Show(IntPtr pet,Native.Rect bounds,double dpi,int fog){
   if(unavailable)return false;
   try{if(source==null){var p=new HwndSourceParameters("Kiana toolbar glass"){WindowStyle=unchecked((int)0x80000000),ExtendedWindowStyle=0x080000A0,Width=1,Height=1,UsesPerPixelOpacity=false};source=new HwndSource(p);source.CompositionTarget.BackgroundColor=Colors.Transparent;source.RootVisual=new System.Windows.Media.DrawingVisual();source.AddHook(Hook);var m=new Margins{Left=-1,Right=-1,Top=-1,Bottom=-1};DwmExtendFrameIntoClientArea(source.Handle,ref m);int corner=2;DwmSetWindowAttribute(source.Handle,33,ref corner,4);int border=unchecked((int)0xfffffffe);DwmSetWindowAttribute(source.Handle,34,ref border,4);int type=3;unavailable=DwmSetWindowAttribute(source.Handle,38,ref type,4)!=0;if(unavailable){Dispose();return false;}}
    int width=Math.Max(1,bounds.Width),height=Math.Max(1,bounds.Height);bool resize=previous.Width!=width||previous.Height!=height,moved=resize||previous.Left!=bounds.Left||previous.Top!=bounds.Top;
    if(lastFog!=fog||lastDark!=PetPalette.Dark){int dark=PetPalette.Dark?1:0;DwmSetWindowAttribute(source.Handle,20,ref dark,4);lastDark=PetPalette.Dark;var v=(DrawingVisual)source.RootVisual;using(var dc=v.RenderOpen())dc.DrawRectangle(new SolidColorBrush(Color.FromArgb((byte)(fog*.7),PetPalette.Background.R,PetPalette.Background.G,PetPalette.Background.B)),null,new Rect(0,0,2000,300));lastFog=fog;}
    if(moved||!shown||Native.IsAbove(source.Handle,pet)||Native.IsTopmost(source.Handle)!=Native.IsTopmost(pet))SetWindowPos(source.Handle,pet,bounds.Left,bounds.Top,width,height,0x10|0x200|(shown?0u:0x40u));if(resize){IntPtr region=CreateRoundRectRgn(0,0,width,height,(int)(30*dpi),(int)(30*dpi));if(SetWindowRgn(source.Handle,region,true)==0)DeleteObject(region);}previous=bounds;shown=true;return true;
   }catch{unavailable=true;Dispose();return false;}
  }
  static IntPtr Hook(IntPtr hwnd,int msg,IntPtr w,IntPtr l,ref bool handled){if(msg==0x84){handled=true;return new IntPtr(-1);}if(msg==0x21){handled=true;return new IntPtr(3);}return IntPtr.Zero;}
  public void Hide(){if(source!=null&&shown)ShowWindow(source.Handle,0);shown=false;}
  public void Dispose(){if(source!=null){source.Dispose();source=null;}shown=false;}
 }
 public static class ToolbarBackgroundSample {
  [DllImport("user32.dll")]static extern IntPtr GetDC(IntPtr hwnd);
  [DllImport("user32.dll")]static extern int ReleaseDC(IntPtr hwnd,IntPtr dc);
  [DllImport("gdi32.dll")]static extern uint GetPixel(IntPtr dc,int x,int y);
  // Read a handful of nearby pixels outside the pet window. No image is retained or sent.
  public static Color[] Read(Native.Rect pet){IntPtr dc=GetDC(IntPtr.Zero);if(dc==IntPtr.Zero)return null;try{double[] r=new double[3],g=new double[3],b=new double[3],n=new double[3];var desktop=System.Windows.Forms.SystemInformation.VirtualScreen;for(int group=0;group<3;group++)for(int i=0;i<8;i++){int x=group==0?pet.Left-12:group==1?pet.Right+12:pet.Left+pet.Width*i/7;int y=group==2?pet.Bottom+12:pet.Bottom-64+i*8;if(!desktop.Contains(x,y))continue;uint p=GetPixel(dc,x,y);if(p==0xffffffff)continue;r[group]+=p&255;g[group]+=(p>>8)&255;b[group]+=(p>>16)&255;n[group]++;}var colors=new Color[3];bool any=false;for(int i=0;i<3;i++){colors[i]=n[i]>0?Color.FromRgb((byte)(r[i]/n[i]),(byte)(g[i]/n[i]),(byte)(b[i]/n[i])):PetPalette.Background;any|=n[i]>0;}return any?colors:null;}finally{ReleaseDC(IntPtr.Zero,dc);}}
 }
}
