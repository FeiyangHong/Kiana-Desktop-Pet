using System;using System.Drawing;using System.Linq;using System.Windows.Interop;using System.Windows.Threading;using Forms=System.Windows.Forms;
namespace KianaPet {
 public sealed partial class PetWindow {
  double nextDisplayProbe;int displayRechecks;bool recoveryActive;
  void ScheduleDisplayRecovery(bool resume){if(quitting||handle==IntPtr.Zero)return;
   if(!recoveryActive&&displayMemory!=null&&Settings.RememberLayouts&&!string.IsNullOrEmpty(displayKey)){displayMemory.Remember(displayKey,Settings);displayMemory.Save();}
   recoveryActive=true;walkUntil=0;velocity=0;walkPlanner.Reset();pressed=dragging=false;if(sprite.IsMouseCaptured)sprite.ReleaseMouseCapture();if(musicWindow!=null)musicWindow.CancelDisplayMotion();
   displayPending=true;displayDue=clock.Elapsed.TotalSeconds+.7;displayRechecks=resume?3:2;nextWalk=displayDue+5;
   if(resume){lastTick=clock.Elapsed.TotalSeconds;lastSlow=-10;wasStackVisible=false;linkFailures=musicFailures=0;Link.RequestReconnect();poll.Interval=TimeSpan.FromSeconds(2);musicTimer.Interval=TimeSpan.FromMilliseconds(850);Poll(null,EventArgs.Empty);PollMusic(null,EventArgs.Empty);}
  }
  void RecoverMusicCoordinates(){if(Settings.MusicPosition!="free"||!Settings.MusicHasPosition)return;int w=(int)Math.Ceiling(Settings.MusicWidth*Settings.MusicScalePercent/100d*DpiScale),h=(int)Math.Ceiling(120*Settings.MusicScalePercent/100d*DpiScale);if(musicWindow!=null){Native.Rect bounds;Native.GetWindowRect(new WindowInteropHelper(musicWindow).Handle,out bounds);if(bounds.Width>0){w=bounds.Width;h=bounds.Height;}}
   var fit=ScreenPlacement.Fit(new Rectangle((int)Math.Round(Settings.MusicX),(int)Math.Round(Settings.MusicY),w,h),Forms.Screen.AllScreens.Select(s=>s.WorkingArea).ToArray());Settings.MusicX=fit.X;Settings.MusicY=fit.Y;
  }
  public void NotifyMusicDisplayChange(){ScheduleDisplayRecovery(false);}
 }
 public sealed partial class MusicWindow {
  HwndSource displaySource;
  void InstallDisplayHook(){displaySource=HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);if(displaySource!=null)displaySource.AddHook(DisplayHook);Closed+=delegate{if(displaySource!=null)displaySource.RemoveHook(DisplayHook);};}
  IntPtr DisplayHook(IntPtr window,int msg,IntPtr a,IntPtr b,ref bool handled){if(msg==0x02e0||msg==0x007e)pet.NotifyMusicDisplayChange();return IntPtr.Zero;}
  public void CancelDisplayMotion(){StopDock();dockInitialized=false;pointerPressed=false;IsDragging=false;if(dragSurface!=null&&dragSurface.IsMouseCaptured)dragSurface.ReleaseMouseCapture();}
 }
}
