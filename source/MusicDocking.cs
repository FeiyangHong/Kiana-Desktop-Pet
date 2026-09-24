using System;using System.Drawing;using System.Windows;using System.Windows.Threading;
namespace KianaPet {
 public static class DockingRules {
  public static Rectangle Footprint(Rectangle full,int footer,bool hoverOnly,bool toolbarReserved){return hoverOnly||toolbarReserved?full:new Rectangle(full.Left,full.Top,full.Width,Math.Max(1,full.Height-footer));}
  public static double Ease(double progress){double p=Math.Max(0,Math.Min(1,progress));return 1-Math.Pow(1-p,3);}
 }
 public sealed partial class PetWindow {
  public Native.Rect MusicFootprint(Native.Rect full){int footer=(int)Math.Round((Settings.LargeTouchTargets?48:40)*Settings.ToolbarPercent/100d*DpiScale);var r=DockingRules.Footprint(new Rectangle(full.Left,full.Top,full.Width,full.Height),footer,Settings.MusicHoverOnly,toolbarShown||ToolbarVisible);return new Native.Rect{Left=r.Left,Top=r.Top,Right=r.Right,Bottom=r.Bottom};}
  public void SetMusicPinned(bool pinned){Settings.MusicHoverOnly=!pinned;ApplySettings();UpdateMusic();}
  public void SetToolbarPinned(bool pinned){Settings.ToolbarPinned=pinned;if(pinned)Settings.HoverToolbar=true;ApplySettings();UpdateToolbar(clock.Elapsed.TotalSeconds);UpdateMusic();}
 }
 public sealed partial class MusicWindow {
  readonly DispatcherTimer dockTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(16)};readonly System.Diagnostics.Stopwatch dockClock=new System.Diagnostics.Stopwatch();
  Rectangle dockTarget;Native.Rect dockFrom,lastPetBounds;bool dockInitialized;int dockLayout;
  void InitializeDocking(){dockTimer.Tick+=delegate{if(!IsVisible||pointerPressed){StopDock();return;}double t=MotionSettings.Enabled?DockingRules.Ease(dockClock.Elapsed.TotalMilliseconds/180d):1;MoveCard((int)Math.Round(dockFrom.Left+(dockTarget.Left-dockFrom.Left)*t),(int)Math.Round(dockFrom.Top+(dockTarget.Top-dockFrom.Top)*t));if(t>=1)StopDock();};Closed+=delegate{StopDock();};}
  void StopDock(){dockTimer.Stop();dockClock.Stop();}
  void MoveCard(int x,int y){Native.SetWindowPos(new System.Windows.Interop.WindowInteropHelper(this).Handle,IntPtr.Zero,x,y,0,0,0x0001|0x0004|0x0010);}
  public bool DockMoving{get{return dockTimer.IsEnabled;}}
  void PlaceCard(Rectangle target,Native.Rect current,Native.Rect full,bool immediate){int layout=full.Height-pet.MusicFootprint(full).Height;bool footprintChanged=dockInitialized&&layout!=dockLayout;bool bodyMoved=dockInitialized&&(full.Left!=lastPetBounds.Left||full.Top!=lastPetBounds.Top);dockLayout=layout;lastPetBounds=full;
   if(dockInitialized&&target==dockTarget&&!immediate){if(dockTimer.IsEnabled&&!MotionSettings.Enabled){StopDock();MoveCard(target.Left,target.Top);}return;}
   bool animate=dockInitialized&&!immediate&&!bodyMoved&&!pet.Settings.MusicHoverOnly&&MotionSettings.Enabled&&(footprintChanged||dockTimer.IsEnabled);dockTarget=target;dockInitialized=true;StopDock();if(target.Left==current.Left&&target.Top==current.Top)return;if(animate){dockFrom=current;dockClock.Restart();dockTimer.Start();}else MoveCard(target.Left,target.Top);
  }
 }
}
