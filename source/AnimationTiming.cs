using System;using System.Linq;using System.Collections.Generic;
namespace KianaPet {
 public static class AnimationTiming {
  public const double Minimum=.1,Maximum=4;
  public static readonly string[] Actions={"idle","running-left","running-right","waving","jumping","pat","carry","sleep","blink","groom","stretch","running","review","waiting","failed","music-quiet","music-gentle","music-lively","music-sit","music-rise"};
  public static double Default(string action){return action=="music-quiet"?.25:1;}
  public static bool Valid(double value){return !double.IsNaN(value)&&!double.IsInfinity(value)&&value>=Minimum&&value<=Maximum;}
  public static void Validate(Config c){c.AnimationSpeeds=(c.AnimationSpeeds??new Dictionary<string,double>()).Where(p=>Actions.Contains(p.Key)&&Valid(p.Value)).ToDictionary(p=>p.Key,p=>Math.Round(p.Value,2));}
  public static double Speed(Config c,string action){double value;return action!=null&&c!=null&&c.AnimationSpeeds!=null&&c.AnimationSpeeds.TryGetValue(action,out value)&&Valid(value)?value:Default(action);}
  public static double Elapsed(Config c,string action,double elapsed){return Math.Max(0,elapsed)*Speed(c,action);}
  // Convert wall time to the existing music timeline. Entry/exit rates are independent of loop rates.
  public static double MusicElapsed(Config c,string action,double elapsed,bool rising){
   elapsed=Math.Max(0,elapsed);bool transition=action=="music-quiet"||rising;
   if(!transition)return Elapsed(c,action,elapsed);
   double entrySpeed=Speed(c,action=="music-quiet"?"music-sit":"music-rise"),entry=MusicReactions.SitTransitionMs/entrySpeed;
   return elapsed<entry?elapsed*entrySpeed:MusicReactions.SitTransitionMs+(elapsed-entry)*Speed(c,action);
  }
 }
 public sealed partial class PetWindow {
  public void SetAnimationSpeed(string action,double value){if(!AnimationTiming.Actions.Contains(action)||!AnimationTiming.Valid(value))throw new ArgumentException("播放速度请输入 0.10–4.00 倍。");double old=AnimationTiming.Speed(Settings,action);Settings.AnimationSpeeds[action]=Math.Round(value,2);if(interaction==action){double now=clock.Elapsed.TotalSeconds;interactionUntil=now+Math.Max(0,interactionUntil-now)*old/value;}Save();}
  public void ResetAnimationSpeeds(){double old=AnimationTiming.Speed(Settings,interaction);Settings.AnimationSpeeds.Clear();if(interaction!=null){double now=clock.Elapsed.TotalSeconds;interactionUntil=now+Math.Max(0,interactionUntil-now)*old/AnimationTiming.Speed(Settings,interaction);}Save();}
 }
}
