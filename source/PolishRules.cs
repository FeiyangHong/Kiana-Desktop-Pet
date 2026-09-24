using System;using System.Linq;using System.Collections.Generic;
namespace KianaPet {
 public sealed class ScenePreset {
  public string Name="自定义";public bool Walking,MusicEnabled=true,MusicHoverOnly=true,MusicLyrics,Bubbles=true,HideFullscreen=true,QuietAppsEnabled,QuietHoursEnabled,TaskNoticesEnabled=true,NoticeSilentSuccess=true;
  public string WalkProfile="occasional",QuietMode="silent";public int WalkSpeed=38,WalkDistanceMin=65,WalkDistanceMax=235,WalkPauseMin=12,WalkPauseMax=32;
  public static ScenePreset Capture(string name,Config c){return new ScenePreset{Name=name,Walking=c.Walking,MusicEnabled=c.MusicEnabled,MusicHoverOnly=c.MusicHoverOnly,MusicLyrics=c.MusicLyrics,Bubbles=c.Bubbles,HideFullscreen=c.HideFullscreen,QuietAppsEnabled=c.QuietAppsEnabled,QuietHoursEnabled=c.QuietHoursEnabled,TaskNoticesEnabled=c.TaskNoticesEnabled,NoticeSilentSuccess=c.NoticeSilentSuccess,QuietMode=c.QuietMode,WalkProfile=c.WalkProfile,WalkSpeed=c.WalkSpeed,WalkDistanceMin=c.WalkDistanceMin,WalkDistanceMax=c.WalkDistanceMax,WalkPauseMin=c.WalkPauseMin,WalkPauseMax=c.WalkPauseMax};}
  public void Apply(Config c){c.Walking=Walking;c.MusicEnabled=MusicEnabled;c.MusicHoverOnly=MusicHoverOnly;c.MusicLyrics=MusicLyrics;c.Bubbles=Bubbles;c.HideFullscreen=HideFullscreen;c.QuietAppsEnabled=QuietAppsEnabled;c.QuietHoursEnabled=QuietHoursEnabled;c.TaskNoticesEnabled=TaskNoticesEnabled;c.NoticeSilentSuccess=NoticeSilentSuccess;c.QuietMode=QuietMode;c.WalkProfile=WalkProfile;c.WalkSpeed=WalkSpeed;c.WalkDistanceMin=WalkDistanceMin;c.WalkDistanceMax=WalkDistanceMax;c.WalkPauseMin=WalkPauseMin;c.WalkPauseMax=WalkPauseMax;c.Validate();}
  public static ScenePreset[] Defaults(){return new[]{new ScenePreset{Name="工作",Walking=false},new ScenePreset{Name="休闲",Walking=true,MusicHoverOnly=false},new ScenePreset{Name="游戏",Walking=false,MusicEnabled=false,Bubbles=false,HideFullscreen=true}};}
 }
 public static class PolishRules {
  public static void Validate(Config c){c.MusicScalePercent=Math.Max(80,Math.Min(150,c.MusicScalePercent));c.MusicWidth=Math.Max(200,Math.Min(420,c.MusicWidth));if(!MusicGradient.Modes.Contains(c.MusicTintMode))c.MusicTintMode="solid";if(!new[]{"light","dark","system"}.Contains(c.Theme))c.Theme="light";c.MusicTintStrength=Math.Max(0,Math.Min(100,c.MusicTintStrength));c.ToolbarPercent=Math.Max(75,Math.Min(150,c.ToolbarPercent));c.NoticeBatchSeconds=Math.Max(2,Math.Min(30,c.NoticeBatchSeconds));if(c.Scenes==null||c.Scenes.Length==0)c.Scenes=ScenePreset.Defaults();c.Scenes=c.Scenes.Where(s=>s!=null&&!string.IsNullOrWhiteSpace(s.Name)).Take(12).ToArray();foreach(var s in c.Scenes)s.Name=s.Name.Trim().Substring(0,Math.Min(24,s.Name.Trim().Length));if(c.Scenes.Length==0)c.Scenes=ScenePreset.Defaults();}
  public static double RetrySeconds(int failures,double normal){return failures<=0?normal:Math.Min(30,normal*Math.Pow(2,Math.Min(5,failures)));}
  public static int FrameInterval(bool save,bool hidden,bool sleep,bool moving,bool hover){return !save?33:hidden?500:moving||hover?33:sleep?180:100;}
 }
 public sealed class NoticeBatcher {
  readonly Dictionary<string,TaskNotice> pending=new Dictionary<string,TaskNotice>();DateTime due;bool held;
  public void Add(TaskNotice n,DateTime now,int seconds){if(pending.Count==0)due=now.AddSeconds(seconds);pending[n.Source+"\n"+n.TaskId]=n;if(pending.Count>50)pending.Remove(pending.Keys.First());}
  public void RemoveWhere(Func<TaskNotice,bool> remove){foreach(var key in pending.Where(p=>remove(p.Value)).Select(p=>p.Key).ToArray())pending.Remove(key);if(pending.Count==0)held=false;}
  public string Flush(DateTime now,bool blocked,bool enabled,bool silentSuccess){if(!enabled){pending.Clear();held=false;return null;}if(pending.Count==0)return null;if(blocked){held=true;return null;}if(now<due)return null;var all=pending.Values.ToArray();pending.Clear();bool summary=held;held=false;var important=all.Where(n=>n.Status=="failed"||n.Status=="waiting").ToArray();if(important.Length==0&&silentSuccess)return null;if(all.Length==1&&!summary)return all[0].Source+"："+all[0].Title+"（"+TaskNoticeHub.StateName(all[0].Status)+"）";return (summary?"免打扰期间":"刚才")+"有 "+all.Length+" 项任务更新"+(important.Length>0?"，其中 "+important.Length+" 项需要处理":"，可点铃铛查看")+"。";}
 }
 public sealed class AmbientPlanner {
  double next=35,lastTouch=-10;string last="";public bool Touch(double now){if(now-lastTouch<.7)return false;lastTouch=now;next=now+35;return true;}
  public string Next(double now,bool available,Random r){if(!available){next=Math.Max(next,now+8);return null;}if(now<next)return null;string[] choices={"groom","stretch","blink"};var list=choices.Where(s=>s!=last).ToArray();last=list[r.Next(list.Length)];next=now+35+r.NextDouble()*45;return last;}
 }
}
