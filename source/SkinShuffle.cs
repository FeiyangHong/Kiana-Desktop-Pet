using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace KianaPet {
 public sealed class SkinShuffleSession {
  public double Remaining=-1;
  public int Minutes=120;
  public bool Enabled=true,Jitter=true,OnStart=false,RoundStarted=false;
  public string[] Pool=new string[0],Bag=new string[0];
  public string HeldDate="",LastSkin="";
 }
 public static class SkinShuffleRules {
  public static readonly string[] Defaults={"kiana-winter-wish-round","kiana-fiery-wishing-star-round","time-runner-kiana-round"};
  public static readonly string[] Allowed=Defaults.Concat(new[]{"kiana-winter-wish","kiana-fiery-wishing-star","time-runner-kiana"}).ToArray();
  public static void Validate(Config c){
   c.SkinShuffleMinutes=Math.Max(1,Math.Min(1440,c.SkinShuffleMinutes));
   c.SkinShufflePool=(c.SkinShufflePool??Defaults).Where(Allowed.Contains).Distinct(StringComparer.Ordinal).Take(6).ToArray();
  }
  public static string Day(DateTime date){return date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);}
  public static string WaitText(double value){long seconds=(long)Math.Ceiling(MusicReactions.Finite(value)?Math.Max(0,value):0);return (seconds/3600).ToString("00")+":"+(seconds/60%60).ToString("00")+":"+(seconds%60).ToString("00");}
 }
 // All timing is accumulated from visible/awake ticks. Wall-clock dates only
 // govern the explicit today hold; elapsed time never advances while offline.
 public sealed class SkinShufflePlanner {
  readonly Random random;
  public SkinShuffleSession Session=new SkinShuffleSession();
  public bool Dirty{get;private set;}
  double last;bool hasTick,wasCounting;
  public SkinShufflePlanner(Random source){random=source;}
  double Period(Config c){return c.SkinShuffleMinutes*60d*(c.SkinShuffleJitter?.9+random.NextDouble()*.2:1);}
  public void Load(SkinShuffleSession saved,Config c){
   Session=saved??new SkinShuffleSession();
   Session.Pool=Session.Pool??new string[0];
   Session.Bag=(Session.Bag??new string[0]).Where(c.SkinShufflePool.Contains).Distinct(StringComparer.Ordinal).Take(6).ToArray();
   DateTime date;if(!DateTime.TryParseExact(Session.HeldDate,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out date))Session.HeldDate="";
   if(!MusicReactions.Finite(Session.Remaining)||Session.Remaining<0||Session.Remaining>95040)Session.Remaining=-1;
   Configure(c);hasTick=false;
  }
  public bool Configure(Config c){
   bool poolChanged=!Session.Pool.SequenceEqual(c.SkinShufflePool);
   bool changed=poolChanged||Session.Minutes!=c.SkinShuffleMinutes||Session.Enabled!=c.SkinShuffleEnabled||Session.Jitter!=c.SkinShuffleJitter||Session.OnStart!=c.SkinShuffleOnStart;
   if(poolChanged){Session.Bag=new string[0];Session.RoundStarted=false;}
   if(changed||Session.Remaining<0){Session.Pool=c.SkinShufflePool.ToArray();Session.Minutes=c.SkinShuffleMinutes;Session.Enabled=c.SkinShuffleEnabled;Session.Jitter=c.SkinShuffleJitter;Session.OnStart=c.SkinShuffleOnStart;Reset(c);}
   if(Session.LastSkin!=c.Skin){Manual(c);changed=true;}
   return changed;
  }
  public void Reset(Config c){Session.Remaining=Period(c);Dirty=true;hasTick=false;wasCounting=false;}
  public void Manual(Config c){Session.LastSkin=c.Skin;Session.Bag=Session.Bag.Where(id=>id!=c.Skin).ToArray();Reset(c);}
  public bool Held(DateTime date){return Session.HeldDate==SkinShuffleRules.Day(date);}
  public void HoldToday(DateTime date){Session.HeldDate=SkinShuffleRules.Day(date);Dirty=true;wasCounting=false;}
  public void Release(Config c){Session.HeldDate="";Reset(c);}
  public void Advance(Config c,double now,bool counting,DateTime date){
   Configure(c);
   if(Session.HeldDate.Length>0&&!Held(date)){Session.HeldDate="";Reset(c);}
   bool active=c.SkinShuffleEnabled&&c.SkinShufflePool.Length>0&&!Held(date)&&counting;
   if(hasTick&&active&&wasCounting){double delta=now-last;if(delta>0&&delta<=2){Session.Remaining=Math.Max(0,Session.Remaining-delta);Dirty=true;}}
   last=now;hasTick=true;wasCounting=active;
  }
  public bool Due(Config c,DateTime date){return c.SkinShuffleEnabled&&!Held(date)&&Session.Remaining<=0;}
  public string StartupChoice(Config c,DateTime date,bool preview){return !preview&&c.SkinShuffleOnStart&&!Held(date)?Next(c):null;}
  public string Next(Config c){
   if(!c.SkinShufflePool.Any(id=>id!=c.Skin))return null;
   var bag=Session.Bag.Where(id=>c.SkinShufflePool.Contains(id)).Distinct(StringComparer.Ordinal).ToList();
   if(bag.Count==0){bag.AddRange(c.SkinShufflePool);if(!Session.RoundStarted)bag.Remove(c.Skin);Session.RoundStarted=true;if(bag.Count==0)return null;Shuffle(bag);}
   // A manual change counts as using that outfit in the current round.
   int next=bag.FindIndex(id=>id!=c.Skin);
   if(next<0){bag=c.SkinShufflePool.ToList();Shuffle(bag);next=bag.FindIndex(id=>id!=c.Skin);}
   string result=bag[next];bag.RemoveAt(next);Session.Bag=bag.ToArray();Dirty=true;return result;
  }
  void Shuffle(List<string> bag){for(int i=bag.Count-1;i>0;i--){int j=random.Next(i+1);string value=bag[i];bag[i]=bag[j];bag[j]=value;}}
  public void Applied(Config c){Session.LastSkin=c.Skin;Reset(c);}
  public void Saved(){Dirty=false;}
 }
 public sealed class SkinShuffleBoundary {
  string observed="";long loop=-1;double previous=-1,stableAt,lastAt;
  public void Reset(){observed="";loop=-1;previous=-1;}
  public bool Observe(string state,double timeline,double cycle,bool safe,bool reduced,double now){
   if(!safe||timeline<0||cycle<=0){Reset();return false;}
   long current=(long)Math.Floor(timeline/cycle);
   if(observed!=state||now-lastAt>2||now<lastAt){observed=state;loop=current;previous=timeline;stableAt=lastAt=now;return false;}
   bool result=reduced?now-stableAt>=2:current>loop&&timeline>=previous&&now-stableAt>=.2;
   loop=current;previous=timeline;lastAt=now;return result;
  }
 }
}
