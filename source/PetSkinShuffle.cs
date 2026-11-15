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
   if(!IsVisible||manualHidden||fullscreenHidden||quietHidden||sessionLocked)return "桌宠隐藏或锁屏";
   if(sleeping)return "桌宠正在休息";
   if(pressed||dragging)return "正在拖动或按住桌宠";
   if(quietActive)return "免打扰中";
   if(focusClock.Session.Running&&focusClock.Session.Phase=="focus")return "正在专注计时";
   if(Settings.LinkChatGPT&&Link.Current.Connected&&!Link.Current.Stale&&DateTime.UtcNow-Link.Current.At<TimeSpan.FromSeconds(10)&&Link.Current.State!="idle")return "GPT 任务优先";
   if(interaction!=null)return "互动动作尚未结束";
   if(menuLayerYielding||displayPending||NoticePreviewOpen||(toolbarMenu!=null&&toolbarMenu.IsOpen)||noticeCenter!=null||storageWindow!=null||settingsWindow!=null)return "菜单、通知或设置正在使用";
   if(hover||hoverBar.IsMouseOver||MusicHovered)return "鼠标正在操作桌宠或音乐栏";
   if(state!="idle"&&state!="music-quiet")return "等待待机或平稳坐姿";
   if(state=="music-quiet"&&!MusicSprites.Supported(Settings.Skin))return "等待普通待机";
   if(state=="music-quiet"&&Music.Current.Connected&&Music.Current.HasPosition&&DateTime.UtcNow-Music.Current.At<=TimeSpan.FromSeconds(6)&&MusicReactions.InChorus(MusicReactions.Position(Music.Current,DateTime.UtcNow),MusicReactions.Ranges(Settings,Music.Current,Settings.MusicChorusEnabled?chorusLibrary.For(Music.Current.Id):new ChorusRange[0]),Music.Current.Duration))return "已知副歌期间暂缓换装";
   if(state=="music-quiet"&&Settings.SkinShufflePool.Any(id=>!MusicSprites.Supported(id)))return "混合普通版候选时等待日常待机，保留音乐动作";
   return "";
  }
  void TrySkinShuffle(double now){
   if(!skinShuffleReady)return;
   string blocked=SkinShuffleBlockReason();
   double elapsed=Math.Max(0,(now-stateAt)*1000);
   double timeline=state=="music-quiet"?AnimationTiming.MusicElapsed(Settings,state,elapsed,false)-MusicReactions.SitTransitionMs:AnimationTiming.Elapsed(Settings,state,elapsed);
   bool boundary=skinBoundary.Observe(state,timeline,state=="music-quiet"?MusicSeatedFrames.CycleMs:6600,blocked.Length==0,Settings.ReduceMotion,now);
   if(preview||!skinShuffle.Due(Settings,DateTime.Now)||!boundary)return;
   string id=skinShuffle.Next(Settings);if(id==null)return;
   ApplyAutomaticSkin(id,now);
  }
  bool ApplyAutomaticSkin(string id,double now){
   if(!Pets.Any(p=>p.Id==id)||id==Settings.Skin)return false;
   string old=Settings.Skin;
   try{
    Settings.Skin=id;LoadSkinCore(false);
    // Redraw the current phase before composition; do not restart a music
    // session, resize windows, show an idle frame, wave or reset polling.
    DrawAnimation((now-stateAt)*1000);
    skinShuffle.Applied(Settings);skinShuffleError="";
    if(lastPreferences!=null)lastPreferences.Skin=id;
    Save();SaveSkinShuffle();return true;
   }catch(Exception e){Settings.Skin=old;LoadSkinCore(false);DrawAnimation((now-stateAt)*1000);skinShuffle.Reset(Settings);skinShuffleError="换装未完成："+e.Message;Store.Log(skinShuffleError);SaveSkinShuffle();return false;}
  }
  void ManualSkinShuffleReset(){if(!skinShuffleReady)return;skinShuffle.Configure(Settings);skinShuffle.Manual(Settings);skinBoundary.Reset();skinShuffleError="";SaveSkinShuffle();}
  public void UpdateSkinShuffleSettings(){Settings.Validate();skinShuffle.Configure(Settings);skinBoundary.Reset();skinShuffleError="";Save();SaveSkinShuffle();}
  public void HoldSkinToday(){skinShuffle.HoldToday(DateTime.Now);skinBoundary.Reset();SaveSkinShuffle();}
  public void ResumeSkinShuffle(){skinShuffle.Release(Settings);skinBoundary.Reset();skinShuffleError="";SaveSkinShuffle();}
  public string SkinShuffleStatus{get{
   if(!skinShuffleReady)return "随机换装准备中";
   if(skinShuffle.Held(DateTime.Now))return "今天固定当前衣装；明天恢复完整间隔计时。";
   if(!Settings.SkinShuffleEnabled&&!Settings.SkinShuffleOnStart)return "随机换装已关闭。";
   if(Settings.SkinShufflePool.Length==0)return "未选择参与的皮肤，暂不更换。";
   if(!Settings.SkinShufflePool.Any(id=>id!=Settings.Skin))return "当前就是唯一候选，不会重复换装。";
   if(skinShuffleError.Length>0)return skinShuffleError;
   if(!Settings.SkinShuffleEnabled)return "仅在启动时随机；下次启动生效。";
   if(skinShuffle.Due(Settings,DateTime.Now)){string reason=SkinShuffleBlockReason();return "已到更换时间，"+(reason.Length>0?reason+"；稍后再换。":"等待动作循环衔接点。");}
   return "距离下次随机约 "+SkinShuffleRules.WaitText(skinShuffle.Session.Remaining)+"（仅累计可见、清醒的陪伴时间）";
  }}
 }
}
