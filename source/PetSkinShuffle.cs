using System;
using System.IO;
using System.Linq;

namespace KianaPet {
 public sealed partial class PetWindow {
  readonly SkinShufflePlanner skinShuffle=new SkinShufflePlanner(new Random());
  readonly SkinShuffleBoundary skinBoundary=new SkinShuffleBoundary();
  bool skinShuffleReady;
  double lastSkinCheckpoint;
  string skinShuffleError="";
  bool skinShuffleRequested;
  string skinShuffleRequestNote="";
  public bool SkinShuffleRequested{get{return skinShuffleRequested;}}
  public bool CanRequestSkinShuffle{get{return skinShuffleReady&&!skinShuffleRequested&&Settings.SkinShufflePool.Any(id=>id!=Settings.Skin);}}
  public void RequestSkinShuffle(){
   if(!skinShuffleReady||skinShuffleRequested)return;
   skinShuffleRequestNote="";
   if(!Settings.SkinShufflePool.Any(id=>id!=Settings.Skin)){skinShuffleRequestNote=Settings.SkinShufflePool.Length==0?"未选择参与的皮肤，本次请求未排队。":"当前就是唯一候选，本次请求未排队。";return;}
   // A deliberate one-shot request bypasses the clock and today's automatic
   // hold, but never bypasses interaction or animation-boundary protections.
   skinShuffleRequested=true;skinShuffleError="";skinBoundary.Reset();
  }
  public void CancelSkinShuffleRequest(){skinShuffleRequested=false;skinShuffleRequestNote="";skinBoundary.Reset();}
  void PrepareSkinShuffle(){
   SkinShuffleSession saved=null;
   try{saved=Store.Json.Deserialize<SkinShuffleSession>(File.ReadAllText(Path.Combine(Store.Root,"skin-shuffle-session.json")));}catch{}
   skinShuffle.Load(saved,Settings);skinShuffleReady=true;
   {
    string id=skinShuffle.StartupChoice(Settings,DateTime.Now,preview);
    if(id!=null){string old=Settings.Skin;try{Settings.Skin=id;LoadSkinCore(false);skinShuffle.Applied(Settings);Store.Save(Settings);}catch(Exception e){Settings.Skin=old;skinShuffle.Reset(Settings);skinShuffleError=e.Message;}}
   }
   SaveSkinShuffle();
  }
  void SaveSkinShuffle(){if(!skinShuffleReady)return;try{Store.Atomic("skin-shuffle-session.json",skinShuffle.Session);skinShuffle.Saved();}catch(Exception e){Store.Log("Skin shuffle save: "+e.Message);}}
  void CountSkinShuffle(double now){
   if(!skinShuffleReady)return;
   bool count=IsVisible&&!manualHidden&&!fullscreenHidden&&!quietHidden&&!sessionLocked&&!sleeping;
   skinShuffle.Advance(Settings,now,count,DateTime.Now);
   if(!count)skinBoundary.Reset();
   if(skinShuffle.Dirty&&now-lastSkinCheckpoint>=30){lastSkinCheckpoint=now;SaveSkinShuffle();}
  }
  string SkinShuffleBlockReason(){
   if(sessionLocked)return "系统已锁屏";
   if(manualHidden)return "桌宠已手动隐藏";
   if(fullscreenHidden)return "全屏应用触发隐藏";
   if(quietHidden)return "免打扰正在隐藏桌宠";
   if(!IsVisible)return "桌宠窗口当前未显示";
   if(sleeping)return "桌宠正在休息";
   if(dragging)return "正在拖动桌宠";
   if(pressed)return "鼠标正在按住桌宠";
   if(quietActive)return "免打扰中";
   if(focusClock.Session.Running&&focusClock.Session.Phase=="focus")return "正在专注计时";
   string gpt=SkinShuffleSafety.GPTBlockReason(Settings,Link.Current,DateTime.UtcNow);if(gpt.Length>0)return gpt;
   if(interaction!=null)return "互动动作尚未结束";
   if(skinVisualRunning||skinShowcaseRunning)return "换装过渡或展示动作尚未结束";
   if(menuLayerYielding)return "外部菜单或托盘面板正在使用";
   if(displayPending)return "显示器布局正在恢复";
   if(NoticePreviewOpen)return "通知速览已展开";
   if(toolbarMenu!=null&&toolbarMenu.IsOpen)return "桌宠菜单已展开";
   if(noticeCenter!=null)return "通知中心窗口尚未关闭";
   if(storageWindow!=null)return "存储管理窗口尚未关闭";
   if(settingsWindow!=null&&!skinShuffleRequested)return "定时换装等待关闭设置窗口";
   if(hover)return "鼠标正悬停在桌宠上";
   if(hoverBar.IsMouseOver)return "鼠标正悬停在工具栏上";
   if(MusicHovered)return "音乐栏正在被悬停、拖动或键盘操作";
   bool review=state=="review"&&SkinShuffleSafety.ReviewPoseAllowed(Settings,Link.Current,DateTime.UtcNow);
   if(state!="idle"&&state!="music-quiet"&&!review)return "等待当前动作结束："+MusicActivity.ActionName(state);
   if(state=="music-quiet"&&!MusicSprites.Supported(Settings.Skin))return "等待普通待机";
   if(state=="music-quiet"&&Music.Current.Connected&&Music.Current.HasPosition&&DateTime.UtcNow-Music.Current.At<=TimeSpan.FromSeconds(6)&&MusicReactions.InChorus(MusicReactions.Position(Music.Current,DateTime.UtcNow),MusicReactions.Ranges(Settings,Music.Current,Settings.MusicChorusEnabled?chorusLibrary.For(Music.Current.Id):new ChorusRange[0]),Music.Current.Duration))return "已知副歌期间暂缓换装";
   if(state=="music-quiet"&&Settings.SkinShufflePool.Any(id=>!MusicSprites.Supported(id)))return "混合普通版候选时等待日常待机，保留音乐动作";
   return "";
  }
  void TrySkinShuffle(double now){
   if(!skinShuffleReady)return;
   bool boundary=SkinShuffleBoundaryReady(now);
   if(!preview)ExecuteSkinShuffle(now,boundary);
  }
  bool SkinShuffleBoundaryReady(double now){
   string blocked=SkinShuffleBlockReason();
   double elapsed=Math.Max(0,(now-stateAt)*1000);
   double timeline=state=="music-quiet"?AnimationTiming.MusicElapsed(Settings,state,elapsed,false)-MusicReactions.SitTransitionMs:AnimationTiming.Elapsed(Settings,state,elapsed);
   bool boundary=skinBoundary.Observe(state,timeline,state=="music-quiet"?MusicSeatedFrames.CycleMs:Rules.CycleMs(state),blocked.Length==0,Settings.ReduceMotion,now);
   // A delayed tick must still land on the first review frame, rather than
   // treating any point in the next loop as a visual handoff.
   return boundary&&(state!="review"||Settings.ReduceMotion||Rules.Frame(state,timeline)==0);
  }
  bool ExecuteSkinShuffle(double now,bool boundary){
   if(!skinShuffleReady||!boundary||(!skinShuffleRequested&&!skinShuffle.Due(Settings,DateTime.Now))||SkinShuffleBlockReason().Length>0)return false;
   bool requested=skinShuffleRequested;string id=skinShuffle.Next(Settings);skinShuffleRequested=false;
   if(id==null){if(requested)skinShuffleRequestNote="没有可更换的候选，本次请求已结束。";return false;}
   return ApplyAutomaticSkin(id,now);
  }
  int SkinShuffleFrameInterval(int normal){
   if(state!="review"||Settings.ReduceMotion||(!skinShuffleRequested&&!skinShuffle.Due(Settings,DateTime.Now))||SkinShuffleBlockReason().Length>0)return normal;
   // Sample the short entry frame even in quiet mode or at high action speeds.
   // This adjustment ends with the request and never changes provider polling.
   return Math.Min(normal,Math.Max(16,(int)(Rules.FirstFrameMs(state)/AnimationTiming.Speed(Settings,state)/2)));
  }
  bool ApplyAutomaticSkin(string id,double now){
   if(!Pets.Any(p=>p.Id==id)||id==Settings.Skin)return false;
   string old=Settings.Skin;var visual=CaptureSkinVisual();StopSkinVisual();
   try{
    Settings.Skin=id;LoadSkinCore(false);
    // Redraw the current phase before composition; do not restart a music
    // session, resize windows, force a greeting or reset polling.
    // The selected existing action and bubble follow the visual handoff.
    DrawAnimation((now-stateAt)*1000);
    skinShuffle.Applied(Settings);skinShuffleError="";
    if(lastPreferences!=null)lastPreferences.Skin=id;
    Save();SaveSkinShuffle();PlaySkinVisual(visual,state,true);return true;
   }catch(Exception e){StopSkinVisual();Settings.Skin=old;LoadSkinCore(false);DrawAnimation((now-stateAt)*1000);skinShuffle.Reset(Settings);skinShuffleError="换装未完成："+e.Message;Store.Log(skinShuffleError);SaveSkinShuffle();return false;}
  }
  void ShowSkinShuffleFeedback(){
   // Completion feedback must never take over task/interaction messaging.
   double now=clock.Elapsed.TotalSeconds;
   if(SkinFeedbackBlocked())return;
   PlaySkinShowcase();
   if(!Settings.Bubbles)return;
   if(bubble.Visibility==System.Windows.Visibility.Visible&&now<bubbleUntil)return;
   Say(skinChangeMessages.Next(),2.8);
  }
  void ManualSkinShuffleReset(){if(!skinShuffleReady)return;CancelSkinShuffleRequest();skinShuffle.Configure(Settings);skinShuffle.Manual(Settings);skinShuffleError="";SaveSkinShuffle();}
  public void UpdateSkinShuffleSettings(){CancelSkinShuffleRequest();Settings.Validate();skinShuffle.Configure(Settings);skinShuffleError="";Save();SaveSkinShuffle();}
  public void HoldSkinToday(){CancelSkinShuffleRequest();skinShuffleError="";skinShuffle.HoldToday(DateTime.Now);SaveSkinShuffle();}
  public void ResumeSkinShuffle(){CancelSkinShuffleRequest();skinShuffle.Release(Settings);skinShuffleError="";SaveSkinShuffle();}
  string SkinShuffleHandoffReason(){return state=="review"?"GPT 已结束，等待待查看动作的循环衔接点。":state=="music-quiet"?"等待坐姿音乐动作的循环衔接点。":"等待待机动作的循环衔接点。";}
  public string SkinShuffleStatus{get{
   if(!skinShuffleReady)return "随机换装准备中";
   if(skinShuffleRequested){string reason=SkinShuffleBlockReason();return "立即随机换装已排队，"+(reason.Length>0?reason+"；稍后更换。":SkinShuffleHandoffReason());}
   if(skinShuffleRequestNote.Length>0)return skinShuffleRequestNote;
   if(skinShuffleError.Length>0)return skinShuffleError;
   if(skinShuffle.Held(DateTime.Now))return "今天固定当前衣装；明天恢复完整间隔计时。";
   if(!Settings.SkinShuffleEnabled&&!Settings.SkinShuffleOnStart)return "随机换装已关闭。";
   if(Settings.SkinShufflePool.Length==0)return "未选择参与的皮肤，暂不更换。";
   if(!Settings.SkinShufflePool.Any(id=>id!=Settings.Skin))return "当前就是唯一候选，不会重复换装。";
   if(!Settings.SkinShuffleEnabled)return "仅在启动时随机；下次启动生效。";
   if(skinShuffle.Due(Settings,DateTime.Now)){string reason=SkinShuffleBlockReason();return "已到更换时间，"+(reason.Length>0?reason+"；稍后再换。":SkinShuffleHandoffReason());}
   return "距离下次随机约 "+SkinShuffleRules.WaitText(skinShuffle.Session.Remaining)+"（仅累计可见、清醒的陪伴时间）";
  }}
 }
}
