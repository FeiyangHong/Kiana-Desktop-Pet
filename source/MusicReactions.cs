using System;using System.Linq;using System.Collections.Generic;using System.Text.RegularExpressions;
namespace KianaPet {
 public sealed class SongMusicPreference {public string Id="",Title="",Mode="inherit";public double Start=-1,End=-1;}
 public sealed class ChorusRange {public double Start,End;}
 public static class MusicReactions {
  public static readonly string[] Modes={"quiet","gentle","lively","off"};
  public static readonly string[] Labels={"安静听歌","轻柔应援","活力应援","关闭音乐动作"};
  public static bool SongId(string id){return Regex.IsMatch(id??"",@"^\d{1,20}$");}
  public static bool Finite(double n){return !double.IsNaN(n)&&!double.IsInfinity(n);}
  public static bool ValidRange(double start,double end,double duration){return Finite(start)&&Finite(end)&&start>=0&&end>start&&end-start<=600&&end<=86400&&(duration<=0||end<=duration+1);}
  public static SongMusicPreference Preference(Config config,string id){return (config.MusicSongPreferences??new SongMusicPreference[0]).LastOrDefault(p=>p!=null&&p.Id==id);}
  public static string Mode(Config c,string id){var p=Preference(c,id);return p!=null&&Modes.Contains(p.Mode)?p.Mode:c.MusicReactionMode;}
  public static string Label(string mode){int i=Array.IndexOf(Modes,mode);return i<0?"跟随整体设置":Labels[i];}
  public static void Validate(Config c){if(!Modes.Contains(c.MusicReactionMode))c.MusicReactionMode="quiet";c.MusicSongPreferences=(c.MusicSongPreferences??new SongMusicPreference[0]).Where(p=>p!=null&&SongId(p.Id)).GroupBy(p=>p.Id).Select(g=>g.Last()).Reverse().Take(100).Reverse().ToArray();foreach(var p in c.MusicSongPreferences){if(!Modes.Contains(p.Mode))p.Mode="inherit";p.Title=(p.Title??"").Substring(0,Math.Min(120,(p.Title??"").Length));if(!Finite(p.Start)||p.Start<0||p.Start>86400)p.Start=-1;if(!ValidRange(p.Start,p.End,0))p.End=-1;}}
  public static double Position(MusicState state,DateTime now){return Math.Max(0,state.Position)+(state.Playing?Math.Min(1.2,Math.Max(0,(now-state.At).TotalSeconds)):0);}
  public static bool InChorus(double position,ChorusRange[] ranges,double duration){return (ranges??new ChorusRange[0]).Any(r=>r!=null&&ValidRange(r.Start,r.End,duration)&&position>=Math.Max(0,r.Start-1)&&position<r.End);}
  public static ChorusRange[] Ranges(Config c,MusicState state,ChorusRange[] remote){var p=Preference(c,state.Id);return p!=null&&ValidRange(p.Start,p.End,state.Duration)?new[]{new ChorusRange{Start=p.Start,End=p.End}}:remote??new ChorusRange[0];}
  public static string Choose(string mode,bool chorus){if(mode=="off")return null;return chorus&&mode=="gentle"?"music-gentle":chorus&&mode=="lively"?"music-lively":"music-quiet";}
  public static int Frame(string state,double elapsed,bool reduced){int row=state=="music-gentle"?1:state=="music-lively"?2:0;double step=row==2?420:row==1?1250:2500;return row*2+(reduced?0:(int)(Math.Max(0,elapsed)/step)%2);}
 }
 // Timing follows the player's position, so seeking and repeat playback need no separate clock.
 public sealed class MusicReactionPlanner {
  string identity="";double started,lastPlaying;bool active;
  public string Choose(MusicState state,Config config,ChorusRange[] ranges,double now,DateTime utc,bool allowed){
   string key=MusicLibrary.Identity(state);if(key!=identity){identity=key;started=now;active=false;lastPlaying=double.NegativeInfinity;}
   if(!state.Connected||utc-state.At>TimeSpan.FromSeconds(6)){active=false;started=now;return null;}
   if(state.Playing){if(!active){active=true;started=now;}lastPlaying=now;}else if(now-lastPlaying>3){active=false;started=now;return null;}
   if(!allowed||!config.MusicEnabled||config.ReduceMotion)return null;
   if(!active||now-started<8)return null;string mode=MusicReactions.Mode(config,state.Id);var resolved=MusicReactions.Ranges(config,state,ranges);bool chorus=state.Playing&&MusicReactions.InChorus(MusicReactions.Position(state,utc),resolved,state.Duration);
   return MusicReactions.Choose(mode,chorus);
  }
 }
}
