using System;using System.Linq;using System.Collections.Generic;
namespace KianaPet {
 public sealed class MusicActivityReport {
  public string Action,Chorus,Reason,Rules,Capabilities;
  public override string ToString(){return "当前动作："+Action+"\n副歌状态："+Chorus+"\n切换说明："+Reason+"\n生效规则："+Rules+"\n连接能力："+Capabilities;}
 }
 public static class MusicActivity {
  public static string ActionName(string action){if(action=="music-rise"||action=="music-sit")return AnimationGallery.Catalog.First(a=>a.Id==action).Name;if((action??"").StartsWith("music-"))return MusicReactions.Label(action.Substring(6));var item=AnimationGallery.Catalog.FirstOrDefault(a=>a.Id==action);return item==null?(action=="sleep"?"休息":action=="carry"?"拖动中":"日常待机"):item.Name;}
  public static MusicActivityReport Read(Config c,MusicState s,ChorusRange[] remote,string query,string actual,string blocked,double wait,bool retaining,DateTime utc){
   bool fresh=s.Connected&&(utc-s.At).TotalSeconds<=6;var pref=MusicReactions.Preference(c,s.Id);bool manual=pref!=null&&MusicReactions.ValidRange(pref.Start,pref.End,s.Duration);
   var ranges=MusicReactions.Ranges(c,s,c.MusicChorusEnabled?remote:new ChorusRange[0]).Where(r=>r!=null&&MusicReactions.ValidRange(r.Start,r.End,s.Duration)).OrderBy(r=>r.Start).ToArray();
   double position=MusicReactions.Position(s,utc);bool chorus=fresh&&s.HasPosition&&MusicReactions.InChorus(position,ranges,s.Duration);
   string phase=!s.Connected?"未连接":!fresh?"信息已过期，等待新样本":!s.HasPosition?"无法判断（连接未提供有效进度）":ranges.Length==0?"暂无可用区间"+(c.MusicChorusEnabled?" · "+query:" · 自动查询已关闭"):chorus?"副歌中（"+(manual?"手动标记":"自动区间")+"）":"平时播放";
   if(fresh&&s.HasPosition&&ranges.Length>0&&!chorus){var next=ranges.FirstOrDefault(r=>r.Start>position);phase+=next==null?" · 本轮副歌已结束":" · 下段 "+ChorusDisplay.Time(next.Start)+"（还剩 "+ChorusDisplay.Time(next.Start-position)+"）";}
   string normal=MusicReactions.Action(c,s.Id,false),peak=MusicReactions.Action(c,s.Id,true);string reason;
   if(!string.IsNullOrEmpty(blocked))reason=blocked;
   else if(!c.MusicEnabled)reason="音乐栏已关闭，音乐动作停用。";
   else if(c.ReduceMotion)reason="已开启减少动态效果，自动音乐动作暂停。";
   else if(!MusicSprites.Supported(c.Skin))reason="当前普通版服装不支持音乐动作，请使用 Q 版。";
   else if(retaining)reason="短暂停顿或断连保留上次音乐动作，宽限结束后恢复日常活动。";
   else if(!fresh)reason="没有新鲜的播放信息，等待连接恢复。";
   else if(!s.Playing)reason="播放器已暂停，等待继续播放。";
   else if(wait>0)reason="等待稳定播放，还需约 "+Math.Ceiling(wait)+" 秒。";
   else if(!s.HasPosition)reason="缺少有效进度，只使用平时动作，不能定位歌词或副歌。";
   else if(ranges.Length==0)reason=manual?"手动区间无效。":!MusicReactions.SongId(s.Id)?"缺少歌曲 ID，无法查询副歌；继续平时动作。":!c.MusicChorusEnabled?"自动副歌查询已关闭，且没有完整手动区间。":"当前没有可用副歌区间，继续平时动作。";
   else if(chorus&&peak=="off")reason="副歌规则设为不做音乐动作，使用日常活动。";
   else if(normal==peak)reason="平时与副歌的生效动作相同，因此经过副歌边界也不会换动作。";
   else if(chorus)reason="已按副歌规则切换；起身等过渡仍会正常播放。";
   else reason=normal=="off"?"平时规则设为不做音乐动作，等待副歌。":"尚未进入副歌，正在使用平时规则。";
   return new MusicActivityReport{Action=ActionName(actual),Chorus=phase,Reason=reason,Rules="平时「"+MusicReactions.Label(normal)+"」／副歌「"+MusicReactions.Label(peak)+"」"+(pref==null?"（整体设置）":"（含本曲覆盖）"),Capabilities=(s.ConnectionSource=="system"?"系统媒体":"原有页面联动")+" · 歌曲 ID "+(MusicReactions.SongId(s.Id)?"可用":"缺失")+" · 进度 "+(fresh&&s.HasPosition?ChorusDisplay.Time(position):"不可用")+" · 歌词 "+(fresh&&s.HasPosition&&MusicReactions.SongId(s.Id)?c.MusicLyrics?"已开启":"可用，未开启":"同步条件不足")};
  }
 }
 public sealed partial class PetWindow {
  public MusicActivityReport MusicActivityStatus{get{
   string blocked=manualHidden||fullscreenHidden||quietHidden||sessionLocked||!IsVisible?"桌宠当前隐藏或锁屏，暂不更新可见动作。":pressed||dragging?"正在拖动／按住桌宠，手动互动优先。":sleeping?"桌宠正在休息，睡眠动作优先。":quietActive?"免打扰中，自动音乐动作暂停。":interaction!=null?"正在播放手动互动或日常互动，结束后恢复音乐规则。":Settings.LinkChatGPT&&Link.Current.Connected&&!Link.Current.Stale&&DateTime.UtcNow-Link.Current.At<TimeSpan.FromSeconds(10)&&Link.Current.State!="idle"?"GPT 任务状态优先，任务空闲后恢复音乐动作。":NoticePreviewOpen||(toolbarMenu!=null&&toolbarMenu.IsOpen)?"通知预览或菜单正在使用，暂缓音乐动作。":"";
   if(skinShowcaseRunning)blocked="正在播放换装展示动作，结束后恢复当前音乐姿态；播放进度与副歌判断继续。";
   return MusicActivity.Read(Settings,Music.Current,chorusLibrary.For(Music.Current.Id),chorusLibrary.StatusFor(Music.Current.Id),CurrentAction,blocked,musicReactionPlanner.RemainingDelay(clock.Elapsed.TotalSeconds,Settings),musicReactionPlanner.Retaining(Music.Current,clock.Elapsed.TotalSeconds,DateTime.UtcNow,Settings),DateTime.UtcNow);
  }}
  public void ShowMusicActivity(){OpenSettings();settingsWindow.NavigateCategory("音乐","");}
 }
}
