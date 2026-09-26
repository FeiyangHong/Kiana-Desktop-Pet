using System;using System.Linq;using System.Collections.Generic;using System.Text.RegularExpressions;
namespace KianaPet {
 public sealed class SongMusicPreference {public string Id="",Title="",Mode="inherit",NormalAction="",ChorusAction="";public double Start=-1,End=-1;}
 public sealed class ChorusRange {public double Start,End;}
 public static class MusicReactions {
  public static readonly string[] Modes={"quiet","gentle","lively","off"};
  public static readonly string[] Labels={"坐着晃身听歌","轻微挥棒","活力挥棒","不做音乐动作"};
  public static bool SongId(string id){return Regex.IsMatch(id??"",@"^\d{1,20}$");}
  public static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
  public static bool ValidRange(double start,double end,double duration){return Finite(start)&&Finite(end)&&start>=0&&end>start&&end-start<=600&&end<=86400&&(duration<=0||end<=duration+1);}
  public static SongMusicPreference Preference(Config config,string id){return (config.MusicSongPreferences??new SongMusicPreference[0]).LastOrDefault(p=>p!=null&&p.Id==id);}
  public static string Mode(Config c,string id){var p=Preference(c,id);return p!=null&&Modes.Contains(p.Mode)?p.Mode:c.MusicReactionMode;}
  public static string Label(string mode){int i=Array.IndexOf(Modes,mode);return i<0?(mode=="mixed"?"坐着听歌＋偶尔轻微应援":mode=="same"?"保持平时动作":"跟随整体设置"):Labels[i];}
  public static string SongActionSetting(SongMusicPreference p,bool chorus){if(p==null)return "inherit";string value=chorus?p.ChorusAction:p.NormalAction;if(Modes.Contains(value)||(!chorus&&value=="mixed")||value=="inherit"||(chorus&&value=="same"))return value;return p.Mode=="inherit"?"inherit":chorus?p.Mode:p.Mode=="off"?"off":"quiet";}
  public static string GlobalAction(Config c,bool chorus){string value=chorus?c.MusicChorusAction:c.MusicNormalAction;return Modes.Contains(value)||(!chorus&&value=="mixed")||(chorus&&value=="same")?value:chorus?c.MusicReactionMode:c.MusicReactionMode=="off"?"off":"quiet";}
  public static string Action(Config c,string id,bool chorus){string value=SongActionSetting(Preference(c,id),chorus);if(value=="inherit")value=GlobalAction(c,chorus);return value=="same"?Action(c,id,false):value;}
  public static string RuleText(Config c,string id){return "平时播放："+Label(Action(c,id,false))+"；进入副歌："+Label(Action(c,id,true))+"。没有副歌时间时使用平时动作。";}
  public static string ChooseAction(Config c,string id,bool chorus,double elapsed=0){string action=Action(c,id,chorus);if(action=="mixed")action=(Math.Max(0,elapsed)%60)<45?"quiet":"gentle";return action=="off"?null:"music-"+action;}
  public static void Validate(Config c){if(!Modes.Contains(c.MusicReactionMode))c.MusicReactionMode="quiet";c.MusicNormalAction=GlobalAction(c,false);c.MusicChorusAction=GlobalAction(c,true);c.MusicSongPreferences=(c.MusicSongPreferences??new SongMusicPreference[0]).Where(p=>p!=null&&SongId(p.Id)).GroupBy(p=>p.Id).Select(g=>g.Last()).Reverse().Take(100).Reverse().ToArray();foreach(var p in c.MusicSongPreferences){if(!Modes.Contains(p.Mode))p.Mode="inherit";p.NormalAction=SongActionSetting(p,false);p.ChorusAction=SongActionSetting(p,true);p.Title=(p.Title??"").Substring(0,Math.Min(120,(p.Title??"").Length));if(!Finite(p.Start)||p.Start<0||p.Start>86400)p.Start=-1;if(!ValidRange(p.Start,p.End,0))p.End=-1;}}
  public static double Position(MusicState state,DateTime now){return Math.Max(0,state.Position)+(state.Playing?Math.Min(1.2,Math.Max(0,(now-state.At).TotalSeconds)):0);}
  public static bool InChorus(double position,ChorusRange[] ranges,double duration){return (ranges??new ChorusRange[0]).Any(r=>r!=null&&ValidRange(r.Start,r.End,duration)&&position>=Math.Max(0,r.Start-1)&&position<r.End);}
  public static ChorusRange[] Ranges(Config c,MusicState state,ChorusRange[] remote){var p=Preference(c,state.Id);return p!=null&&ValidRange(p.Start,p.End,state.Duration)?new[]{new ChorusRange{Start=p.Start,End=p.End}}:remote??new ChorusRange[0];}
  public static string Choose(string mode,bool chorus){if(mode=="off")return null;return chorus&&mode=="gentle"?"music-gentle":chorus&&mode=="lively"?"music-lively":"music-quiet";}
  // Drawn in-betweens, with short eyelid transitions and a longer resting pose.
  // Elapsed time, rather than tick count, keeps the cycle stable after delayed UI ticks.
  static readonly int[][] durations={new[]{450,450,420,150,420,450},new[]{280,180,280,180,280,180},new[]{180,120,180,120,180,120}};
  public const int FramesPerAction=6;
  public const int SitTransitionMs=720;
  public static int PlaybackFrame(string state,double elapsed,bool reduced,bool rising){if(reduced)return Frame(state,0,true);elapsed=Finite(elapsed)?Math.Max(0,elapsed):0;bool sitting=state=="music-quiet";if((sitting||rising)&&elapsed<SitTransitionMs){int step=Math.Min(5,(int)(elapsed/120));return 18+(sitting?step:5-step);}return Frame(state,elapsed-((sitting||rising)?SitTransitionMs:0),false);}
  public static int Frame(string state,double elapsed,bool reduced){int row=state=="music-gentle"?1:state=="music-lively"?2:0;if(reduced||!Finite(elapsed))return row*FramesPerAction;var times=durations[row];double position=Math.Max(0,elapsed)%times.Sum();for(int i=0;i<times.Length;i++){if(position<times[i])return row*FramesPerAction+i;position-=times[i];}return row*FramesPerAction;}
 }
 // Timing follows the player's position, so seeking and repeat playback need no separate clock.
 public sealed class MusicReactionPlanner {
  string identity="";double started,lastPlaying;bool active;
  public string Choose(MusicState state,Config config,ChorusRange[] ranges,double now,DateTime utc,bool allowed){
   string key=MusicLibrary.Identity(state);if(key!=identity){identity=key;started=now;active=false;lastPlaying=double.NegativeInfinity;}
   if(!state.Connected||utc-state.At>TimeSpan.FromSeconds(6)){active=false;started=now;return null;}
   if(state.Playing){if(!active){active=true;started=now;}lastPlaying=now;}else if(now-lastPlaying>3){active=false;started=now;return null;}
   if(!allowed||!config.MusicEnabled||config.ReduceMotion)return null;
   if(!active||now-started<8)return null;var resolved=MusicReactions.Ranges(config,state,ranges);bool chorus=state.Playing&&MusicReactions.InChorus(MusicReactions.Position(state,utc),resolved,state.Duration);
   return !state.Playing?(MusicReactions.Action(config,state.Id,false)=="off"?null:"music-quiet"):MusicReactions.ChooseAction(config,state.Id,chorus,now-started-8);
  }
 }
}
