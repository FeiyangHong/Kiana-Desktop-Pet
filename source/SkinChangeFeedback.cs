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
  public static bool TaskPose(string state){return state=="running"||state=="waiting"||state=="review"||state=="failed";}
  public static void Validate(Config c){if(c.SkinShuffleMusicMode!="seated"&&c.SkinShuffleMusicMode!="stand-first")c.SkinShuffleMusicMode="seated";c.SkinShuffleActions=(c.SkinShuffleActions??Allowed).Where(Allowed.Contains).Distinct(StringComparer.Ordinal).ToArray();}
 }
 public sealed partial class PetWindow {
  readonly SkinChangeMessages skinChangeMessages=new SkinChangeMessages(new Random());
  readonly Random skinActionRandom=new Random();
  string lastSkinAction="",skinShowcaseExpectedState="",skinShowcaseAction="";
  bool skinShowcaseRunning,skinShowcaseSeated,skinShowcaseSwapStarted,skinFeedbackBubbleOwned;
  double skinShowcaseAt,skinShowcaseRiseMs,skinShowcaseActionMs,skinShowcaseSitMs;
  AnimationPreviewSource skinShowcaseSource;AnimationChoice skinShowcaseChoice;OutfitActionSequence skinShowcaseSequence;
  bool SkinFeedbackBlocked(){return !IsVisible||manualHidden||fullscreenHidden||quietHidden||sessionLocked||quietActive||sleeping||pressed||dragging||interaction!=null||menuLayerYielding||displayPending||NoticePreviewOpen||(toolbarMenu!=null&&toolbarMenu.IsOpen)||noticeCenter!=null||storageWindow!=null||(focusClock.Session.Running&&focusClock.Session.Phase=="focus")||SkinShuffleSafety.GPTBlockReason(Settings,Link.Current,DateTime.UtcNow).Length>0;}
  void PlaySkinShowcase(){
   StopSkinShowcase();var choices=Settings.SkinShuffleActions.Where(SkinChangeActions.Allowed.Contains).Distinct().ToArray();
   if(choices.Length==0||!MotionSettings.Enabled||SkinFeedbackBlocked()||SkinChangeActions.TaskPose(state)||state.StartsWith("music-",StringComparison.Ordinal))return;
   var withoutLast=choices.Where(a=>a!=lastSkinAction).ToArray();if(withoutLast.Length>0)choices=withoutLast;
   skinShowcaseAction=choices[skinActionRandom.Next(choices.Length)];lastSkinAction=skinShowcaseAction;
   skinShowcaseExpectedState=state;skinShowcaseSeated=state=="music-quiet"&&MusicSprites.Supported(Settings.Skin);
   skinShowcaseSequence=new OutfitActionSequence(Settings,skinShowcaseAction,skinShowcaseSeated);
   skinShowcaseChoice=skinShowcaseSequence.Choice;skinShowcaseSource=skinShowcaseSequence.Source;
   skinShowcaseRiseMs=skinShowcaseSequence.RiseMs;skinShowcaseSitMs=skinShowcaseSequence.SitMs;skinShowcaseActionMs=skinShowcaseSequence.ActionMs;
   skinShowcaseAt=clock.Elapsed.TotalSeconds;skinShowcaseRunning=true;DrawSkinShowcase();
  }
  bool CanStandForOutfit(string oldSkin){return state=="music-quiet"&&Settings.SkinShuffleMusicMode=="stand-first"&&MotionSettings.Enabled&&Settings.SkinShuffleActions.Any()&&MusicSprites.Supported(oldSkin)&&MusicSprites.Supported(Settings.Skin)&&!SkinFeedbackBlocked();}
  void BeginMusicOutfitChange(string oldSkin,OutfitActionSequence continuation,double started,bool swapped,System.Windows.Media.Imaging.BitmapSource visual){
   double continuedMs=continuation!=null?Math.Max(0,(clock.Elapsed.TotalSeconds-started)*1000):0;string action;
   if(continuation!=null&&Settings.SkinShuffleActions.Contains(continuation.Choice.Id))action=continuation.Choice.Id;
   else{continuation=null;var choices=Settings.SkinShuffleActions.Where(a=>a!=lastSkinAction).ToArray();if(choices.Length==0)choices=Settings.SkinShuffleActions;action=choices[skinActionRandom.Next(choices.Length)];lastSkinAction=action;}
   var oldSource=continuation!=null?continuation.OldSource:new AnimationPreviewSource(oldSkin,Settings.MusicSeatedStyle);
   skinShowcaseSequence=new OutfitActionSequence(Settings,action,true,null,oldSource,continuation);skinShowcaseChoice=skinShowcaseSequence.Choice;skinShowcaseAction=action;skinShowcaseExpectedState=state;skinShowcaseSeated=true;
   skinShowcaseRiseMs=skinShowcaseSequence.RiseMs;skinShowcaseSitMs=skinShowcaseSequence.SitMs;skinShowcaseActionMs=skinShowcaseSequence.ActionMs;
   skinShowcaseAt=clock.Elapsed.TotalSeconds-(continuation!=null?continuedMs/1000:0);skinShowcaseSwapStarted=continuation!=null&&swapped;skinShowcaseRunning=true;
   DrawSkinShowcase();if(continuation!=null&&swapped&&skinShowcaseRunning)StartSkinFade(visual,state,true,true);
  }
  void ApplyOutfitPose(AnimationPose pose){sprite.Source=pose.Source;activeAlignment=pose.Alignment;ApplySpriteAlignment();lastFrame=-1;}
  string SkinShowcaseVisibleAction(double milliseconds){return skinShowcaseSequence!=null&&milliseconds<skinShowcaseSequence.DurationMs?skinShowcaseSequence.VisibleAction(milliseconds):state;}
  bool DrawSkinShowcase(){
   if(!skinShowcaseRunning)return false;
   double ms=Math.Max(0,(clock.Elapsed.TotalSeconds-skinShowcaseAt)*1000);
   if(ms>=skinShowcaseRiseMs+skinShowcaseActionMs+skinShowcaseSitMs){StopSkinShowcase();return false;}
   try{
    if(skinShowcaseSeated&&!skinShowcaseSwapStarted&&ms>=skinShowcaseRiseMs){
     ApplyOutfitPose(skinShowcaseSequence.OldSource.Frame(AnimationGallery.Catalog.First(a=>a.Id=="idle"),0));
     var old=CaptureSkinVisual();skinShowcaseSwapStarted=true;skinShowcaseAt=clock.Elapsed.TotalSeconds-skinShowcaseRiseMs/1000;ms=skinShowcaseRiseMs;
     skinShowcaseSource=skinShowcaseSequence.Source;ApplyOutfitPose(skinShowcaseSequence.Frame(ms));StartSkinFade(old,state,true,true);return true;
    }
    skinShowcaseSource=ms<skinShowcaseRiseMs?skinShowcaseSequence.OldSource:skinShowcaseSequence.Source;
    var pose=skinShowcaseSequence.Frame(ms);if(pose==null){StopSkinShowcase();return false;}
    ApplyOutfitPose(pose);return true;
   }catch(Exception e){Store.Log("Outfit action: "+e.Message);StopSkinShowcase();return false;}
  }
  public void StopOutfitAction(){StopSkinVisual();}
  void StopSkinShowcase(){
   bool restore=skinShowcaseRunning;skinShowcaseRunning=false;skinShowcaseSwapStarted=false;skinShowcaseExpectedState="";skinShowcaseAction="";skinShowcaseSource=null;skinShowcaseChoice=null;skinShowcaseSequence=null;
   if(restore){lastFrame=-1;if(!quitting)DrawAnimation((clock.Elapsed.TotalSeconds-stateAt)*1000);}
  }
 }
}
