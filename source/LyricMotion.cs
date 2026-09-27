using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
namespace KianaPet {
 public sealed class LyricCue {
  public string Text="",Key="";public double Start,End,Position;public DateTime SampleAt;
  public bool Timed{get{return MusicReactions.Finite(Start)&&MusicReactions.Finite(End)&&MusicReactions.Finite(Position)&&End>Start+.1;}}
 }
 public static class LyricMotionRules {
  public static readonly string[] Modes={"bounce","once","progress","static"};
  public static void Validate(Config c){if(Array.IndexOf(Modes,c.MusicLyricScroll)<0)c.MusicLyricScroll="bounce";c.MusicLyricSpeed=Math.Max(15,Math.Min(120,c.MusicLyricSpeed));c.MusicLyricStartHold=Math.Max(0,Math.Min(10000,c.MusicLyricStartHold));c.MusicLyricEndHold=Math.Max(0,Math.Min(10000,c.MusicLyricEndHold));}
  public static double Fraction(LyricCue cue){return cue==null||!cue.Timed?0:Math.Max(0,Math.Min(1,(cue.Position-cue.Start)/(cue.End-cue.Start)));}
  public static DoubleAnimationUsingKeyFrames Animation(double distance,Config c){
   double travel=Math.Max(1.4,Math.Abs(distance)/c.MusicLyricSpeed),start=c.MusicLyricStartHold/1000d,end=c.MusicLyricEndHold/1000d;
   var a=new DoubleAnimationUsingKeyFrames{FillBehavior=FillBehavior.HoldEnd,RepeatBehavior=c.MusicLyricScroll=="once"?new RepeatBehavior(1):RepeatBehavior.Forever};
   a.KeyFrames.Add(new LinearDoubleKeyFrame(0,KeyTime.FromTimeSpan(TimeSpan.Zero)));
   a.KeyFrames.Add(new LinearDoubleKeyFrame(0,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(start))));
   a.KeyFrames.Add(new LinearDoubleKeyFrame(distance,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(start+travel))));
   if(c.MusicLyricScroll!="once"){
    a.KeyFrames.Add(new LinearDoubleKeyFrame(distance,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(start+travel+end))));
    a.KeyFrames.Add(new LinearDoubleKeyFrame(0,KeyTime.FromTimeSpan(TimeSpan.FromSeconds(start+travel+end+travel))));
   }return a;
  }
 }
 // Shared by the actual music card and the isolated settings sample.
 public sealed class LyricMotionPresenter {
  readonly FrameworkElement viewport;readonly TextBlock text;readonly TranslateTransform offset;string key="";
  public LyricMotionPresenter(FrameworkElement viewport,TextBlock text,TranslateTransform offset){this.viewport=viewport;this.text=text;this.offset=offset;}
  public void Stop(){offset.BeginAnimation(TranslateTransform.XProperty,null);key="";}
  public void Update(Config c,bool playing,LyricCue cue,bool motionEnabled){
   double width=viewport.ActualWidth;if(width<=0)return;bool visible=viewport.IsVisible;
   bool progress=c.MusicLyricScroll=="progress"&&cue!=null&&cue.Timed;
   string next=text.Text+"|"+(cue==null?"":cue.Key)+"|"+width+"|"+playing+"|"+visible+"|"+motionEnabled+"|"+c.MusicLyricScroll+"|"+c.MusicLyricSpeed+"|"+c.MusicLyricStartHold+"|"+c.MusicLyricEndHold+(progress?"|"+cue.SampleAt.Ticks+"|"+cue.Start+"|"+cue.End:"");
   if(key==next)return;key=next;offset.BeginAnimation(TranslateTransform.XProperty,null);offset.X=0;text.Width=double.NaN;text.TextTrimming=TextTrimming.None;text.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));double content=text.DesiredSize.Width;
   if(content<=width+1){offset.X=Math.Max(0,(width-content)/2);return;}
   if(!visible||!playing||!motionEnabled||c.MusicLyricScroll=="static"){text.Width=width;text.TextTrimming=TextTrimming.CharacterEllipsis;return;}
   double distance=width-content;
   if(progress){double start=LyricMotionRules.Fraction(cue),remaining=Math.Max(0,1.2-Math.Max(0,(DateTime.UtcNow-cue.SampleAt).TotalSeconds)),end=Math.Max(start,Math.Min(1,(cue.Position+remaining-cue.Start)/(cue.End-cue.Start)));offset.X=distance*start;if(remaining>.01)offset.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(distance*start,distance*end,TimeSpan.FromSeconds(remaining)){FillBehavior=FillBehavior.HoldEnd});}
   else offset.BeginAnimation(TranslateTransform.XProperty,LyricMotionRules.Animation(distance,c));
  }
 }
}
