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
 public static class SkinChangeActions {
  public static readonly string[] Allowed={"waving","jumping","groom","pat","stretch"};
  public static string Label(string id){return AnimationGallery.Catalog.First(a=>a.Id==id).Name;}
  public static void Validate(Config c){c.SkinShuffleActions=(c.SkinShuffleActions??Allowed).Where(Allowed.Contains).Distinct(StringComparer.Ordinal).ToArray();}
 }
 public sealed partial class PetWindow {
  readonly SkinChangeMessages skinChangeMessages=new SkinChangeMessages(new Random());
  readonly Random skinActionRandom=new Random();
  string lastSkinAction="",skinShowcaseExpectedState="",skinShowcaseAction="";
  bool skinShowcaseRunning,skinShowcaseSeated;
  double skinShowcaseAt,skinShowcaseRiseMs,skinShowcaseActionMs,skinShowcaseSitMs;
  AnimationPreviewSource skinShowcaseSource;AnimationChoice skinShowcaseChoice;
  string SkinChangeText(){return Settings.Bubbles&&!quietActive&&!sessionLocked?skinChangeMessages.Next():"换好衣服啦！";}
  bool SkinFeedbackBlocked(){return !IsVisible||manualHidden||fullscreenHidden||quietHidden||sessionLocked||quietActive||sleeping||pressed||dragging||interaction!=null||menuLayerYielding||displayPending||NoticePreviewOpen||(toolbarMenu!=null&&toolbarMenu.IsOpen)||noticeCenter!=null||storageWindow!=null||(focusClock.Session.Running&&focusClock.Session.Phase=="focus")||SkinShuffleSafety.GPTBlockReason(Settings,Link.Current,DateTime.UtcNow).Length>0;}
  void PlaySkinShowcase(){
   StopSkinShowcase();var choices=Settings.SkinShuffleActions.Where(SkinChangeActions.Allowed.Contains).Distinct().ToArray();
   if(choices.Length==0||!MotionSettings.Enabled||SkinFeedbackBlocked())return;
   var withoutLast=choices.Where(a=>a!=lastSkinAction).ToArray();if(withoutLast.Length>0)choices=withoutLast;
   skinShowcaseAction=choices[skinActionRandom.Next(choices.Length)];lastSkinAction=skinShowcaseAction;
   skinShowcaseChoice=AnimationGallery.Catalog.First(a=>a.Id==skinShowcaseAction);
   skinShowcaseSource=new AnimationPreviewSource(Settings.Skin,Settings.MusicSeatedStyle);skinShowcaseExpectedState=state;
   skinShowcaseSeated=state=="music-quiet"&&MusicSprites.Supported(Settings.Skin);
   skinShowcaseRiseMs=skinShowcaseSeated?720/AnimationTiming.Speed(Settings,"music-rise"):0;
   skinShowcaseSitMs=skinShowcaseSeated?720/AnimationTiming.Speed(Settings,"music-sit"):0;
   skinShowcaseActionMs=skinShowcaseChoice.Duration/AnimationTiming.Speed(Settings,skinShowcaseAction);
   skinShowcaseAt=clock.Elapsed.TotalSeconds;skinShowcaseRunning=true;DrawSkinShowcase();
  }
  string SkinShowcaseVisibleAction(double milliseconds){if(milliseconds>=skinShowcaseRiseMs+skinShowcaseActionMs+skinShowcaseSitMs)return state;return milliseconds<skinShowcaseRiseMs?"music-rise":milliseconds<skinShowcaseRiseMs+skinShowcaseActionMs?skinShowcaseAction:"music-sit";}
  bool DrawSkinShowcase(){
   if(!skinShowcaseRunning)return false;
   double ms=Math.Max(0,(clock.Elapsed.TotalSeconds-skinShowcaseAt)*1000);
   if(ms>=skinShowcaseRiseMs+skinShowcaseActionMs+skinShowcaseSitMs){StopSkinShowcase();return false;}
   try{
    string action=SkinShowcaseVisibleAction(ms);AnimationChoice choice=action==skinShowcaseAction?skinShowcaseChoice:AnimationGallery.Catalog.First(a=>a.Id==action);
    double elapsed=action=="music-rise"?ms*720/skinShowcaseRiseMs:action=="music-sit"?(ms-skinShowcaseRiseMs-skinShowcaseActionMs)*720/skinShowcaseSitMs:(ms-skinShowcaseRiseMs)*skinShowcaseChoice.Duration/skinShowcaseActionMs;
    var pose=skinShowcaseSource.Frame(choice,elapsed);if(pose==null){StopSkinShowcase();return false;}
    sprite.Source=pose.Source;activeAlignment=pose.Alignment;ApplySpriteAlignment();lastFrame=-1;return true;
   }catch(Exception e){Store.Log("Outfit action: "+e.Message);StopSkinShowcase();return false;}
  }
  public void StopOutfitAction(){StopSkinShowcase();}
  void StopSkinShowcase(){
   bool restore=skinShowcaseRunning;skinShowcaseRunning=false;skinShowcaseExpectedState="";skinShowcaseAction="";skinShowcaseSource=null;skinShowcaseChoice=null;
   if(restore){lastFrame=-1;if(!quitting)DrawAnimation((clock.Elapsed.TotalSeconds-stateAt)*1000);}
  }
 }
}
