using System;using System.Linq;using System.Collections.Generic;
namespace KianaPet {
 public static class CompanionOptions {
  public static string ProfileLabel(string value){return value=="quiet"?"安静":value=="balanced"?"均衡":value=="smooth"?"流畅":"原有策略";}
  public static readonly string[] Profiles={"legacy","quiet","balanced","smooth"};
  public static void Validate(Config c){
   if(!Profiles.Contains(c.PerformanceProfile))c.PerformanceProfile="legacy";
   c.FocusMinutes=Math.Max(1,Math.Min(180,c.FocusMinutes));c.BreakMinutes=Math.Max(1,Math.Min(60,c.BreakMinutes));
   if(!new[]{"badge","bubble","action"}.Contains(c.FocusReminder))c.FocusReminder="bubble";
   c.SharedPreferenceFile=c.SharedPreferenceFile??"";if(c.SharedPreferenceFile.Length>1024)c.SharedPreferenceFile="";
   c.AutoSceneRules=(c.AutoSceneRules??new AutoSceneRule[0]).Where(r=>r!=null&&new[]{"music","foreground","battery","time"}.Contains(r.Trigger)&&!string.IsNullOrWhiteSpace(r.Scene)).Take(20).ToArray();
   foreach(var r in c.AutoSceneRules){r.Scene=r.Scene.Substring(0,Math.Min(24,r.Scene.Length));r.Process=(r.Process??"").Trim();if(r.Process.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))r.Process=r.Process.Substring(0,r.Process.Length-4);if(r.Process.Length>80)r.Process="";TimeSpan a,b;if(!TimeSpan.TryParse(r.Start,out a)||a.TotalHours<0||a.TotalHours>=24)r.Start="09:00";if(!TimeSpan.TryParse(r.End,out b)||b.TotalHours<0||b.TotalHours>=24)r.End="18:00";}
  }
 }
 public sealed class FocusSession {
  public string Phase="idle";public bool Running;public double Remaining;public int Completed;
 }
 public sealed class FocusClock {
  public FocusSession Session=new FocusSession();double last;bool hasTick;
  public void Load(FocusSession value){Session=value??new FocusSession();if(!new[]{"idle","focus","break","break-ready","focus-ready"}.Contains(Session.Phase)||!MusicReactions.Finite(Session.Remaining)||Session.Remaining<0||Session.Remaining>10800)Session=new FocusSession();Session.Running=false;Session.Completed=Math.Max(0,Session.Completed);hasTick=false;}
  public void Begin(bool rest,Config c,double now){Session.Phase=rest?"break":"focus";Session.Remaining=(rest?c.BreakMinutes:c.FocusMinutes)*60;Session.Running=true;last=now;hasTick=true;}
  public bool Tick(double now,bool locked){if(!hasTick){last=now;hasTick=true;return false;}double delta=Math.Max(0,now-last);last=now;if(!Session.Running)return false;if(locked){Session.Running=false;return false;}Session.Remaining=Math.Max(0,Session.Remaining-delta);if(Session.Remaining>0)return false;Session.Running=false;if(Session.Phase=="focus"){Session.Completed++;Session.Phase="break-ready";}else Session.Phase="focus-ready";return true;}
  public void Pause(double now){Tick(now,false);Session.Running=false;}
  public void Resume(double now){if(Session.Phase!="focus"&&Session.Phase!="break")return;last=now;hasTick=true;Session.Running=Session.Remaining>0;}
  public void Reset(){int completed=Session.Completed;Session=new FocusSession{Completed=completed};hasTick=false;}
  public string Status{get{return Session.Phase=="idle"?"尚未开始":Session.Phase=="break-ready"?"专注完成，可以开始休息":Session.Phase=="focus-ready"?"休息结束，准备下一轮":(Session.Phase=="focus"?"专注":"休息")+" · "+ChorusDisplay.Time(Math.Ceiling(Session.Remaining))+(Session.Running?"":"（已暂停）");}}
 }
 public static class PerformanceRules {
  public static string Effective(Config c,bool battery){return c.BatterySaving&&battery?"quiet":c.PerformanceProfile;}
  public static int Frame(Config c,bool battery,bool hidden,bool locked,bool sleep,bool moving,bool hover){string profile=Effective(c,battery);if(profile=="legacy")return locked?500:PolishRules.FrameInterval(c.ResourceSaving,hidden,sleep,moving,hover);if(hidden||locked)return profile=="quiet"?1000:500;if(moving||hover)return profile=="smooth"?16:profile=="quiet"?50:33;return sleep?250:profile=="quiet"?200:profile=="smooth"?66:100;}
  public static double MusicPoll(Config c,bool battery,bool hidden,bool locked,int failures){string profile=Effective(c,battery);double normal=!c.MusicEnabled||locked?4:hidden?profile=="legacy"?4:2:.85;return c.ResourceSaving||profile!="legacy"?PolishRules.RetrySeconds(failures,normal):.85;}
 }
 public sealed class ProcessMeter {
  double at=-1,cpu;public double CpuPercent,MemoryMiB;public bool Ready;
  public void Sample(double seconds){if(at>=0&&seconds-at<2)return;using(var p=System.Diagnostics.Process.GetCurrentProcess()){double value=p.TotalProcessorTime.TotalSeconds;if(at>=0&&seconds>at){CpuPercent=Math.Max(0,(value-cpu)/(seconds-at)*100);Ready=true;}cpu=value;at=seconds;MemoryMiB=p.WorkingSet64/1048576d;}}
 }
}
