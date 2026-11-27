using System;
using System.Collections.Generic;
using System.Linq;

namespace KianaPet {
 public static class AnimationTiming {
  public const double Minimum=.1, Maximum=4;
  public static readonly string[] Actions={"idle","running-left","running-right","waving","jumping","pat","carry","sleep","blink","groom","stretch","drink","running","review","waiting","failed","music-quiet","music-gentle","music-lively","music-sit","music-rise"};

  public static double Default(string action){return action=="music-quiet"?.25:1;}
  public static bool Valid(double value){return !double.IsNaN(value)&&!double.IsInfinity(value)&&value>=Minimum&&value<=Maximum;}

  static string SkinKey(string action,string style){
   if(action!="music-quiet")return action;
   return style=="nod"?"music-quiet:nod":style=="sway"?"music-quiet:sway":"music-quiet:classic";
  }

  static bool ValidKey(string key,bool perSkin){
   if(!perSkin)return Actions.Contains(key);
   if(key=="music-quiet:classic"||key=="music-quiet:sway"||key=="music-quiet:nod")return true;
   return key!="music-quiet"&&Actions.Contains(key);
  }

  static Dictionary<string,double> Clean(Dictionary<string,double> rates,bool perSkin){
   if(rates==null)return new Dictionary<string,double>();
   return rates.Where(p=>ValidKey(p.Key,perSkin)&&Valid(p.Value)).ToDictionary(p=>p.Key,p=>Math.Round(p.Value,2));
  }

  public static void Validate(Config c){
   c.AnimationSpeeds=Clean(c.AnimationSpeeds,false);
   var cleaned=new Dictionary<string,Dictionary<string,double>>();
   if(c.SkinAnimationSpeeds!=null){
    foreach(var entry in c.SkinAnimationSpeeds){
     if(!SkinShuffleRules.Allowed.Contains(entry.Key))continue;
     var rates=Clean(entry.Value,true);
     if(rates.Count>0)cleaned.Add(entry.Key,rates);
    }
   }
   c.SkinAnimationSpeeds=cleaned;
  }

  public static double SharedSpeed(Config c,string action){
   double value;
   return action!=null&&c!=null&&c.AnimationSpeeds!=null&&c.AnimationSpeeds.TryGetValue(action,out value)&&Valid(value)?value:Default(action);
  }

  public static bool TrySkinSpeed(Config c,string skin,string action,string style,out double value){
   value=0;
   Dictionary<string,double> rates;
   return c!=null&&c.SkinAnimationSpeeds!=null&&skin!=null&&action!=null&&c.SkinAnimationSpeeds.TryGetValue(skin,out rates)&&rates!=null&&rates.TryGetValue(SkinKey(action,style??c.MusicSeatedStyle),out value)&&Valid(value);
  }

  public static double Speed(Config c,string skin,string action,string style=null){
   double value;
   return TrySkinSpeed(c,skin,action,style,out value)?value:SharedSpeed(c,action);
  }
  public static double Speed(Config c,string action){return Speed(c,c==null?null:c.Skin,action);}
  public static double Elapsed(Config c,string action,double elapsed){return Math.Max(0,elapsed)*Speed(c,action);}

  // A null skin edits the shared defaults, a null value removes the saved rate,
  // and a null action resets only the selected scope.
  public static void Edit(Config c,string skin,string action,string style,double? value){
   if(skin!=null&&!SkinShuffleRules.Allowed.Contains(skin))throw new ArgumentException("未知皮肤。");
   if(action!=null&&!Actions.Contains(action))throw new ArgumentException("未知动作。");
   if(value.HasValue&&(action==null||!Valid(value.Value)))throw new ArgumentOutOfRangeException("value");
   Dictionary<string,double> rates;
   if(skin==null){
    rates=c.AnimationSpeeds;
   }else if(!c.SkinAnimationSpeeds.TryGetValue(skin,out rates)){
    if(!value.HasValue)return;
    c.SkinAnimationSpeeds[skin]=rates=new Dictionary<string,double>();
   }
   if(action==null)rates.Clear();
   else{
    string key=skin==null?action:SkinKey(action,style??c.MusicSeatedStyle);
    if(value.HasValue)rates[key]=Math.Round(value.Value,2);
    else rates.Remove(key);
   }
   if(skin!=null&&rates.Count==0)c.SkinAnimationSpeeds.Remove(skin);
  }

  // Wall time and sprite time cross the transition boundary at independent rates.
  static double ConvertMusicTime(Config c,string skin,string action,double elapsed,bool rising,bool toWallTime){
   elapsed=Math.Max(0,elapsed);
   double loopRate=Speed(c,skin,action);
   if(action!="music-quiet"&&!rising)return toWallTime?elapsed/loopRate:elapsed*loopRate;
   string transition=action=="music-quiet"?"music-sit":"music-rise";
   double transitionMs=AnimationAssets.Duration(skin,transition), transitionRate=Speed(c,skin,transition);
   double wallTransition=transitionMs/transitionRate;
   if(toWallTime)return elapsed<transitionMs?elapsed/transitionRate:wallTransition+(elapsed-transitionMs)/loopRate;
   return elapsed<wallTransition?elapsed*transitionRate:transitionMs+(elapsed-wallTransition)*loopRate;
  }
  public static double MusicElapsed(Config c,string skin,string action,double elapsed,bool rising){
   return ConvertMusicTime(c,skin,action,elapsed,rising,false);
  }
  public static double MusicElapsed(Config c,string action,double elapsed,bool rising){
   return MusicElapsed(c,c==null?null:c.Skin,action,elapsed,rising);
  }
  public static double MusicWallTime(Config c,string action,double elapsed,bool rising){
   return ConvertMusicTime(c,c.Skin,action,elapsed,rising,true);
  }
 }

 public sealed partial class PetWindow {
  public void EditAnimationSpeed(string skin,string action,string style,double? value){
   double now=clock.Elapsed.TotalSeconds,oldRate=AnimationTiming.Speed(Settings,state);
   double elapsed=Math.Max(0,(now-stateAt)*1000),oldInteractionRate=AnimationTiming.Speed(Settings,interaction);
   bool music=state!=null&&state.StartsWith("music-",StringComparison.Ordinal);
   double timeline=music?AnimationTiming.MusicElapsed(Settings,state,elapsed,musicRising):elapsed*oldRate;
   AnimationTiming.Edit(Settings,skin,action,style,value);
   double wallTime=music?AnimationTiming.MusicWallTime(Settings,state,timeline,musicRising):timeline/AnimationTiming.Speed(Settings,state);
   if(Math.Abs(wallTime-elapsed)>.001)stateAt=now-wallTime/1000;
   if(interaction!=null){
    double newRate=AnimationTiming.Speed(Settings,interaction);
    if(oldInteractionRate!=newRate)interactionUntil=now+Math.Max(0,interactionUntil-now)*oldInteractionRate/newRate;
   }
   Save();
  }
  public void SetAnimationSpeed(string action,double value){EditAnimationSpeed(null,action,null,value);}
  public void ResetAnimationSpeeds(){EditAnimationSpeed(null,null,null,null);}
 }
}
