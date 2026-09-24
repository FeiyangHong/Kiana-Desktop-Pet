using System;using System.Drawing;using System.Linq;
namespace KianaPet {
 public static class ComfortRules {
  static int Range(int v,int min,int max){return Math.Max(min,Math.Min(max,v));}
  public static void Validate(Config c){
   c.WalkSpeed=Range(c.WalkSpeed,10,120);c.WalkDistanceMin=Range(c.WalkDistanceMin,20,500);c.WalkDistanceMax=Range(c.WalkDistanceMax,c.WalkDistanceMin,600);
   c.WalkPauseMin=Range(c.WalkPauseMin,5,600);c.WalkPauseMax=Range(c.WalkPauseMax,c.WalkPauseMin,900);
   if(!new[]{"quiet","occasional","lively","custom"}.Contains(c.WalkProfile))c.WalkProfile="custom";
   if(c.QuietMode!="hide")c.QuietMode="silent";
   c.QuietApps=NormalizeApps(c.QuietApps??new string[0]);
   TimeSpan a,b;if(!TimeSpan.TryParse(c.QuietStart,out a)||a.TotalHours<0||a.TotalHours>=24)c.QuietStart="09:00";if(!TimeSpan.TryParse(c.QuietEnd,out b)||b.TotalHours<0||b.TotalHours>=24)c.QuietEnd="18:00";
  }
  public static string[] NormalizeApps(string[] values){return values.SelectMany(v=>(v??"").Split(new[]{',',';','\r','\n','，','；'},StringSplitOptions.RemoveEmptyEntries)).Select(v=>v.Trim()).Where(v=>System.Text.RegularExpressions.Regex.IsMatch(v,@"^[\p{L}\p{N}_. -]{1,80}$")).Select(v=>v.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)?v.Substring(0,v.Length-4):v).Where(v=>v.Length>0).Distinct(StringComparer.OrdinalIgnoreCase).Take(40).ToArray();}
  public static void Preset(Config c,string name){c.WalkProfile=name;
   if(name=="quiet"){c.WalkSpeed=22;c.WalkDistanceMin=35;c.WalkDistanceMax=100;c.WalkPauseMin=90;c.WalkPauseMax=180;}
   else if(name=="lively"){c.WalkSpeed=65;c.WalkDistanceMin=100;c.WalkDistanceMax=320;c.WalkPauseMin=8;c.WalkPauseMax=20;}
   else{c.WalkProfile="occasional";c.WalkSpeed=38;c.WalkDistanceMin=65;c.WalkDistanceMax=235;c.WalkPauseMin=12;c.WalkPauseMax=32;}
  }
  public static bool Quiet(Config c,bool manual,TimeSpan time,string foreground,string[] running){
   if(manual||c.QuietHoursEnabled&&Rules.IsSleepTime(time,TimeSpan.Parse(c.QuietStart),TimeSpan.Parse(c.QuietEnd)))return true;
   if(!c.QuietAppsEnabled||c.QuietApps.Length==0)return false;
   return c.QuietForegroundOnly?c.QuietApps.Contains(foreground??"",StringComparer.OrdinalIgnoreCase):running.Any(p=>c.QuietApps.Contains(p,StringComparer.OrdinalIgnoreCase));
  }
 }
 public sealed class SmoothWalk {
  public double Position{get;private set;}public double Speed{get;private set;}double target;
  public void Begin(double start,double end){Position=start;target=end;Speed=0;}
  public double Step(double dt,double maxSpeed,double acceleration){
   dt=Math.Max(0,Math.Min(.1,dt));double distance=Math.Abs(target-Position);if(distance<.1){Position=target;Speed=0;return Position;}
   double wanted=Math.Min(maxSpeed,Math.Sqrt(2*acceleration*distance));Speed=Speed<wanted?Math.Min(wanted,Speed+acceleration*dt):Math.Max(wanted,Speed-acceleration*dt);
   Position+=Math.Sign(target-Position)*Math.Min(distance,Speed*dt);return Position;
  }
 }
 public static class ScreenPlacement {
  public static Rectangle Fit(Rectangle window,Rectangle[] areas){
   if(areas==null||areas.Length==0)return window;
   Rectangle best=areas[0];long overlap=-1;double nearest=double.MaxValue;
   foreach(var area in areas){Rectangle hit=Rectangle.Intersect(window,area);long size=(long)Math.Max(0,hit.Width)*Math.Max(0,hit.Height);double dx=window.Left+window.Width/2.0-Rules.Clamp(window.Left+window.Width/2.0,area.Left,area.Right),dy=window.Top+window.Height/2.0-Rules.Clamp(window.Top+window.Height/2.0,area.Top,area.Bottom);double d=dx*dx+dy*dy;if(size>overlap||size==overlap&&d<nearest){best=area;overlap=size;nearest=d;}}
   return new Rectangle((int)Rules.Clamp(window.Left,best.Left,best.Right-window.Width),(int)Rules.Clamp(window.Top,best.Top,best.Bottom-window.Height),window.Width,window.Height);
  }
 }
}
