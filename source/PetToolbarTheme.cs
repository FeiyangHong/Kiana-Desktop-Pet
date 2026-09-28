using System;using System.Linq;using System.Windows;using System.Windows.Controls;using System.Windows.Media;using System.Windows.Media.Animation;
namespace KianaPet {
 public sealed partial class PetWindow {
  readonly ToolbarGlass toolbarGlass=new ToolbarGlass();Color[] toolbarBackdrop;double nextBackdrop;string toolbarThemeKey="";System.Windows.Media.Imaging.BitmapSource toolbarCover;Color? toolbarInk;Color[] toolbarColors;
  // Use the card's presentation target so fades switch once, not on every opacity tick.
  public bool ToolbarFollowingMusic {get{return Settings.ToolbarFollowMusic&&Settings.MusicEnabled&&musicWindow!=null&&musicWindow.PresentationActive&&Music.Current.Connected&&DateTime.UtcNow-Music.Current.At<=TimeSpan.FromSeconds(6);}}
  bool toolbarWasFollowing;
  public Color[] ToolbarBackgroundColors{get{return toolbarBackdrop;}}
  public string ToolbarMaterialStatus{get{return Settings.ToolbarStyle!="glass"?"":toolbarGlass.Available?"系统毛玻璃已启用；雾度控制底板遮盖，模糊半径由 Windows 管理。":"毛玻璃在工具栏显示时启用；系统不支持时使用半透明柔光底板。";}}
  void UpdateToolbarTheme(double now){
   bool following=ToolbarFollowingMusic;
   if(following!=toolbarWasFollowing){toolbarWasFollowing=following;toolbarThemeKey="";toolbarMusicAppearance.Invalidate();}
   bool shown=ToolbarVisible&&IsVisible&&!manualHidden&&!fullscreenHidden&&!sessionLocked&&!pressed&&!dragging;
   if(!following&&Settings.ToolbarColorSource=="background"&&Settings.ToolbarStyle!="legacy"&&shown&&!menuLayerYielding&&now>=nextBackdrop){nextBackdrop=now+1;var read=ToolbarBackgroundSample.Read(Bounds());if(read!=null){if(toolbarBackdrop==null)toolbarBackdrop=read;else for(int i=0;i<3;i++)toolbarBackdrop[i]=PetPalette.Mix(toolbarBackdrop[i],read[i],.3);}}
   if(following||Settings.ToolbarStyle=="legacy"){toolbarGlass.Hide();toolbarThemeKey="";hoverBar.BorderThickness=new Thickness(0);if(toolbarInk.HasValue){toolbarInk=null;appliedDark=null;ApplyAppearance();toolbarMusicAppearance.Invalidate();}toolbarMusicAppearance.Apply(hoverBar,MusicCoverForAppearance,Settings,following);return;}
   var art=Settings.MusicEnabled&&Music.Current.Connected?MusicCoverForAppearance:null;string key=Settings.ToolbarStyle+"|"+Settings.ToolbarColorSource+"|"+Settings.ToolbarPreset+"|"+Settings.ToolbarTint+"|"+Settings.ToolbarOpacity+"|"+Settings.ToolbarColor1+Settings.ToolbarColor2+Settings.ToolbarColor3+Settings.ToolbarIconColor+Settings.ToolbarLineColor+Settings.ToolbarAutoContrast+Settings.ToolbarDirection+PetPalette.Dark;
   if(Settings.ToolbarColorSource=="background"&&toolbarBackdrop!=null)key+=string.Join("",toolbarBackdrop.Select(c=>c.ToString()));
   if(key!=toolbarThemeKey||!ReferenceEquals(art,toolbarCover)){
    toolbarThemeKey=key;toolbarCover=art;var target=ToolbarTheme.Tinted(Settings,ToolbarTheme.ColorsFor(Settings,art,toolbarBackdrop),PetPalette.Dark);var brush=ToolbarTheme.MakeBrush(Settings,target,now);var old=hoverBar.Background as GradientBrush;var next=brush as GradientBrush;
    if(old!=null&&next!=null&&old.GradientStops.Count==next.GradientStops.Count&&MotionSettings.Enabled)for(int i=0;i<next.GradientStops.Count;i++)next.GradientStops[i].BeginAnimation(GradientStop.ColorProperty,new ColorAnimation(old.GradientStops[i].Color,next.GradientStops[i].Color,TimeSpan.FromMilliseconds(450)));
    hoverBar.Background=brush;hoverBar.BorderBrush=new SolidColorBrush(PetPalette.Hex(Settings.ToolbarLineColor)){Opacity=.38};hoverBar.BorderThickness=new Thickness(.65);toolbarColors=target;
   }
   if(shown&&Settings.ToolbarFlow&&!Settings.ReduceMotion&&Settings.ToolbarStyle!="solid"){
    var temp=ToolbarTheme.MakeBrush(Settings,toolbarColors,now);var linear=hoverBar.Background as LinearGradientBrush;var target=temp as LinearGradientBrush;if(linear!=null&&target!=null){linear.StartPoint=target.StartPoint;linear.EndPoint=target.EndPoint;}var radial=hoverBar.Background as RadialGradientBrush;var r=temp as RadialGradientBrush;if(radial!=null&&r!=null){radial.Center=r.Center;radial.GradientOrigin=r.GradientOrigin;}
   }
   Color inkColor=ToolbarTheme.Ink(Settings,toolbarColors,PetPalette.Dark);if(toolbarInk!=inkColor){toolbarInk=inkColor;chatButton.Content=ToolbarIcons.Create("chat",inkColor);voiceButton.Content=ToolbarIcons.Create("voice",inkColor);var badge=(Grid)notificationButton.Content;badge.Children.RemoveAt(0);badge.Children.Insert(0,ToolbarIcons.Create("bell",inkColor));foreach(var separator in ((Panel)hoverBar.Child).Children.OfType<Border>())separator.Background=new SolidColorBrush(inkColor){Opacity=.15};}
   if(Settings.ToolbarStyle=="glass"&&shown&&hoverBar.Opacity>.05&&hoverBar.ActualWidth>1){try{Point a=hoverBar.PointToScreen(new Point(0,0)),b=hoverBar.PointToScreen(new Point(hoverBar.ActualWidth,hoverBar.ActualHeight));toolbarGlass.Show(handle,new Native.Rect{Left=(int)Math.Floor(a.X),Top=(int)Math.Floor(a.Y),Right=(int)Math.Ceiling(b.X),Bottom=(int)Math.Ceiling(b.Y)},DpiScale,Settings.ToolbarGlassFog);}catch{toolbarGlass.Hide();}}else toolbarGlass.Hide();
  }
 }
}
