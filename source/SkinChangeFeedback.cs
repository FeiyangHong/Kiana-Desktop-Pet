using System;using System.Linq;using System.Windows;using System.Windows.Media;using System.Windows.Media.Animation;
namespace KianaPet {
 public sealed class SkinChangeMessages {
  static readonly string[] lines={"锵锵，换好新衣服啦！","今天这套，喜欢吗？","换个心情，继续陪你～","新造型，准备就绪！","这套也很适合我吧？","悄悄换装，被你发现啦！","衣服换好，陪伴继续！","嘿嘿，今天也要元气满满！"};
  readonly Random random;int[] order;int cursor,last=-1;
  public SkinChangeMessages(Random random){this.random=random;}
  public static bool Contains(string text){return lines.Contains(text);}
  public string Next(){
   if(order==null||cursor==order.Length){order=Enumerable.Range(0,lines.Length).ToArray();for(int i=order.Length-1;i>0;i--){int j=random.Next(i+1),value=order[i];order[i]=order[j];order[j]=value;}if(order[0]==last){int j=1+random.Next(order.Length-1),value=order[0];order[0]=order[j];order[j]=value;}cursor=0;}
   last=order[cursor++];return lines[last];
  }
 }
 public sealed partial class PetWindow {
  readonly SkinChangeMessages skinChangeMessages=new SkinChangeMessages(new Random());
  readonly RotateTransform skinShowcaseTurn=new RotateTransform();
  readonly TranslateTransform skinShowcaseLift=new TranslateTransform();
  bool skinShowcaseRunning;int skinShowcaseToken;string skinShowcaseExpectedState="";
  void BuildSkinShowcase(){var transforms=new TransformGroup();transforms.Children.Add(skinShowcaseTurn);transforms.Children.Add(skinShowcaseLift);skinLiveVisual.RenderTransform=transforms;skinLiveVisual.RenderTransformOrigin=new Point(.5,.9);}
  string SkinChangeText(){return Settings.Bubbles&&!quietActive&&!sessionLocked?skinChangeMessages.Next():"换好衣服啦！";}
  bool SkinFeedbackBlocked(){return !IsVisible||manualHidden||fullscreenHidden||quietHidden||sessionLocked||quietActive||sleeping||pressed||dragging||interaction!=null||menuLayerYielding||displayPending||NoticePreviewOpen||(toolbarMenu!=null&&toolbarMenu.IsOpen)||noticeCenter!=null||storageWindow!=null||(focusClock.Session.Running&&focusClock.Session.Phase=="focus")||SkinShuffleSafety.GPTBlockReason(Settings,Link.Current,DateTime.UtcNow).Length>0;}
  static DoubleAnimationUsingKeyFrames ShowcaseFrames(double first,double second,double third){var a=new DoubleAnimationUsingKeyFrames{Duration=TimeSpan.FromMilliseconds(900)};a.KeyFrames.Add(new SplineDoubleKeyFrame(0,KeyTime.FromTimeSpan(TimeSpan.Zero)));a.KeyFrames.Add(new SplineDoubleKeyFrame(first,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(210)),new KeySpline(.2,0,.3,1)));a.KeyFrames.Add(new SplineDoubleKeyFrame(second,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(470)),new KeySpline(.3,0,.3,1)));a.KeyFrames.Add(new SplineDoubleKeyFrame(third,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(690)),new KeySpline(.3,0,.3,1)));a.KeyFrames.Add(new SplineDoubleKeyFrame(0,KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(900)),new KeySpline(.3,0,.3,1)));return a;}
  void PlaySkinShowcase(){
   StopSkinShowcase();if(!Settings.SmoothTransitions||!MotionSettings.Enabled||SkinFeedbackBlocked())return;
   skinShowcaseRunning=true;skinShowcaseExpectedState=state;int token=skinShowcaseToken;bool seated=state.StartsWith("music-",StringComparison.Ordinal);double size=Settings.Size/176d;
   // Animate a separate parent layer: the live sprite's music/alignment
   // transform and state clock remain owned by their original renderers.
   var turn=ShowcaseFrames(seated?-3.5:-6,seated?3:5,seated?-1:-2);var lift=ShowcaseFrames(seated?-1.5*size:-6*size,seated?-.5*size:-2*size,0);
   turn.Completed+=delegate{if(token==skinShowcaseToken)StopSkinShowcase();};skinShowcaseTurn.BeginAnimation(RotateTransform.AngleProperty,turn);skinShowcaseLift.BeginAnimation(TranslateTransform.YProperty,lift);
  }
  void StopSkinShowcase(){skinShowcaseToken++;skinShowcaseRunning=false;skinShowcaseExpectedState="";skinShowcaseTurn.BeginAnimation(RotateTransform.AngleProperty,null);skinShowcaseLift.BeginAnimation(TranslateTransform.YProperty,null);skinShowcaseTurn.Angle=0;skinShowcaseLift.Y=0;}
 }
}
