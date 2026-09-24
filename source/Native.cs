using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;
using Forms=System.Windows.Forms;
namespace KianaPet {
 public static partial class Native {
  [StructLayout(LayoutKind.Sequential)]public struct Rect {public int Left,Top,Right,Bottom;public int Width{get{return Right-Left;}}public int Height{get{return Bottom-Top;}}}
  [StructLayout(LayoutKind.Sequential)]public struct Point {public int X,Y;}
  [StructLayout(LayoutKind.Sequential)]struct LastInput {public uint Size,Time;}
  [DllImport("user32.dll")]public static extern bool GetCursorPos(out Point point);
  [DllImport("user32.dll")]public static extern bool GetWindowRect(IntPtr window,out Rect rect);
  [DllImport("user32.dll")]public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll",EntryPoint="SetForegroundWindow")]public static extern bool FocusPetForMenu(IntPtr window);
  public static void SetMenuActivation(IntPtr window,bool active){if(window==IntPtr.Zero)return;long style=GetStyle(window,-20).ToInt64();if(active)style&=~0x08000000L;else style|=0x08000000L;SetStyle(window,-20,new IntPtr(style));}
  [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr window,StringBuilder text,int count);
  [DllImport("user32.dll")]static extern bool GetLastInputInfo(ref LastInput input);
  [DllImport("user32.dll")]public static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int cx,int cy,uint flags);
  [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]static extern IntPtr GetStyle(IntPtr window,int index);
  [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")]static extern IntPtr SetStyle(IntPtr window,int index,IntPtr value);
  [DllImport("user32.dll")]public static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
  [DllImport("iphlpapi.dll",SetLastError=true)]static extern uint GetExtendedTcpTable(IntPtr table,ref int size,bool order,int family,int tableClass,uint reserved);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr OpenProcess(uint access,bool inherit,uint process);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern bool QueryFullProcessImageName(IntPtr process,uint flags,StringBuilder name,ref int size);
  [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr CreateFile(string path,uint access,uint share,IntPtr security,uint disposition,uint flags,IntPtr template);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern uint GetFinalPathNameByHandle(IntPtr file,StringBuilder path,uint count,uint flags);
  static string Canonical(string path){IntPtr file=CreateFile(path,0,7,IntPtr.Zero,3,0,IntPtr.Zero);if(file==new IntPtr(-1))return "";try{StringBuilder result=new StringBuilder(4096);uint n=GetFinalPathNameByHandle(file,result,4096,0);return n>0&&n<4096?result.ToString():"";}finally{CloseHandle(file);}}
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]public static extern uint GetCurrentPackageFullName(ref uint length,StringBuilder name);
  [DllImport("user32.dll",SetLastError=true)]public static extern bool RegisterHotKey(IntPtr window,int id,uint modifiers,uint key);
  [DllImport("user32.dll")]public static extern bool UnregisterHotKey(IntPtr window,int id);
  [DllImport("wtsapi32.dll")]public static extern bool WTSRegisterSessionNotification(IntPtr window,uint flags);
  [DllImport("wtsapi32.dll")]public static extern bool WTSUnRegisterSessionNotification(IntPtr window);
  public static double IdleSeconds(){LastInput v=new LastInput{Size=(uint)Marshal.SizeOf(typeof(LastInput))};if(!GetLastInputInfo(ref v))return 0;return unchecked((uint)Environment.TickCount-v.Time)/1000.0;}
  public static void StylePet(IntPtr window,bool clickThrough){long style=GetStyle(window,-20).ToInt64();style|=0x80L|0x08000000L;style&=~0x40000L;if(clickThrough)style|=0x20;else style&=~0x20L;SetStyle(window,-20,new IntPtr(style));}
  public static bool Covers(Rect r,Rectangle screen){return r.Left<=screen.Left+2&&r.Top<=screen.Top+2&&r.Right>=screen.Right-2&&r.Bottom>=screen.Bottom-2;}
  public static string PortCheck="";
  static bool PortFail(string reason){PortCheck=reason;return false;}
  public static bool TrustedPort(int port,string expectedPath){return TrustedPortCore(port,expectedPath,true);}
  public static bool TrustedMusicPort(int port,string expectedPath){return System.IO.Path.GetFileName(expectedPath).Equals("cloudmusic.exe",StringComparison.OrdinalIgnoreCase)&&TrustedPortCore(port,expectedPath,false);}
  static bool TrustedPortCore(int port,string expectedPath,bool chatGPT){PortCheck="start";int size=0;GetExtendedTcpTable(IntPtr.Zero,ref size,false,2,3,0);if(size<4||size>1024*1024)return PortFail("table-size");IntPtr table=Marshal.AllocHGlobal(size);try{if(GetExtendedTcpTable(table,ref size,false,2,3,0)!=0)return false;int count=Marshal.ReadInt32(table);bool found=false;for(int i=0;i<count;i++){int off=4+i*24;int p=(Marshal.ReadByte(table,off+8)<<8)|Marshal.ReadByte(table,off+9);if(p!=port)continue;if(Marshal.ReadInt32(table,off+4)!=0x0100007f)return PortFail("not-loopback");uint pid=(uint)Marshal.ReadInt32(table,off+20);IntPtr process=OpenProcess(0x1000,false,pid);if(process==IntPtr.Zero)return PortFail("open-process");try{int n=2048;StringBuilder path=new StringBuilder(n);if(!QueryFullProcessImageName(process,0,path,ref n)||!string.Equals(Canonical(path.ToString()),Canonical(expectedPath),StringComparison.OrdinalIgnoreCase)||Canonical(expectedPath).Length==0)return PortFail("process-path: "+path.ToString());string prefix=System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"WindowsApps","OpenAI.Codex_");if(chatGPT&&(!expectedPath.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)||!path.ToString().EndsWith("\\app\\ChatGPT.exe",StringComparison.OrdinalIgnoreCase)))return PortFail("package-path: "+path.ToString());found=true;}finally{CloseHandle(process);}}PortCheck=found?"ok":"no-listener";return found;}finally{Marshal.FreeHGlobal(table);}}
 }
}
