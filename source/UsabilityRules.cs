using System;using System.Linq;using System.Drawing;using System.Collections.Generic;using System.Reflection;
namespace KianaPet {
 public static class MotionSettings {public static bool Reduced;public static bool Enabled{get{return !Reduced&&System.Windows.SystemParameters.ClientAreaAnimation;}}}
 public static class CursorAvoidance {
  public static bool Near(Rectangle pet,Point cursor,double margin){return cursor.X>=pet.Left-margin&&cursor.X<=pet.Right+margin&&cursor.Y>=pet.Top-margin&&cursor.Y<=pet.Bottom+margin;}
  public static bool Crosses(Rectangle pet,Point cursor,double target,double margin){return cursor.Y>=pet.Top-margin&&cursor.Y<=pet.Bottom+margin&&cursor.X>=Math.Min(pet.Left,target)-margin&&cursor.X<=Math.Max(pet.Left,target)+pet.Width+margin;}
 }
 public sealed class PreferenceEdit {public Config Before;public string Label,Category,Key;public DateTime At;public Action Restore;}
 public sealed class RecentEdit {public string Label,Category;public DateTime At;}
 public sealed class EditHistory {
  public readonly List<PreferenceEdit> Items=new List<PreferenceEdit>();public readonly List<RecentEdit> Recent=new List<RecentEdit>();public int Revision;
  static readonly string[] Ignored={"X","Y","HasPosition","MusicX","MusicY","MusicHasPosition","Schema"};
  public static string[] Changed(Config a,Config b){return typeof(Config).GetFields().Where(f=>!Ignored.Contains(f.Name)&&Store.Json.Serialize(f.GetValue(a))!=Store.Json.Serialize(f.GetValue(b))).Select(f=>f.Name).ToArray();}
  public static string Category(string key){return key=="SettingsCategoryOrder"?"常用":key.StartsWith("Launch")?"启动与联动":key.StartsWith("Notice")||key=="NotificationGraceSeconds"||key=="CompletionNoticeSeconds"?"通知中心":key.StartsWith("Music")?"音乐":key.StartsWith("Quiet")?"免打扰":key.StartsWith("Recover")?"快捷键":key=="MutedNoticeSources"?"通知中心":key=="Skin"||key=="Size"||key=="Theme"||key=="ToolbarPercent"||key=="ToolbarPinned"||key=="ToolbarFollowMusic"||key=="YieldToMenus"||key=="HoverToolbar"||key=="AmbientActions"?"外观与互动":key=="ReduceMotion"||key=="LargeTouchTargets"||key=="AvoidCursor"?"无障碍":key.StartsWith("Walk")||key=="Gravity"||key=="SleepSchedule"?"活动与作息":"场景与维护";}
  public static string Label(string key){var names=new Dictionary<string,string>{{"SettingsCategoryOrder","设置分类顺序"},{"NoticeSourceStyle","通知应用标识样式"},{"NoticePreviewFollowMusic","通知横幅跟随音乐配色"},{"MusicDefaultPosition","音乐栏默认停靠位置"},{"NoticePreviewWidth","通知浮窗宽度"},{"NoticePreviewCount","通知速览条数"},{"NoticePreviewHover","悬停通知预览"},{"LaunchChatGPTOnStart","随桌宠启动 ChatGPT"},{"LaunchMusicOnStart","随桌宠启动网易云音乐"},{"NoticeQuiet","静默提醒"},{"NoticeSound","提示音"},{"NoticeSilentSuccess","普通完成静默"},{"CompletionNoticeSeconds","提醒停留时间"},{"NotificationGraceSeconds","断线保留时长"},{"Skin","换装"},{"Size","宠物大小"},{"Theme","界面主题"},{"ToolbarPercent","工具栏大小"},{"ToolbarFollowMusic","工具栏跟随音乐配色"},{"YieldToMenus","菜单优先显示"},{"MusicScalePercent","音乐栏整体大小"},{"MusicWidth","音乐栏宽度"},{"MusicTintStrength","音乐配色程度"},{"MusicCoverTint","音乐封面配色"},{"MusicTintMode","音乐渐变样式"},{"MusicPosition","音乐栏位置"},{"Walking","自由走动"},{"Scenes","场景方案"},{"ReduceMotion","减少动态效果"},{"LargeTouchTargets","大触控区域"},{"AvoidCursor","避让鼠标"},{"MusicHoverOnly","音乐栏常驻"},{"ToolbarPinned","工具栏常驻"}};string label;return names.TryGetValue(key,out label)?label:Category(key)+"设置";}
  public void Record(Config before,Config after,DateTime now){var changed=Changed(before,after);if(changed.Length==0)return;string key=string.Join("|",changed),label=changed.Length==1?Label(changed[0]):"一组偏好设置",category=changed.Length==1?Category(changed[0]):"场景与维护";if(Items.Count>0&&Items[0].Key==key&&Items[0].Restore==null&&now-Items[0].At<TimeSpan.FromSeconds(1.2)){Items[0].At=now;Recent.RemoveAll(x=>x.Label==label&&x.Category==category);}else Items.Insert(0,new PreferenceEdit{Before=PreferenceTransfer.Copy(before),Label=label,Category=category,Key=key,At=now});if(Items.Count>20)Items.RemoveRange(20,Items.Count-20);AddRecent(label,category,now);}
  public void AddAction(string label,string category,Action restore,DateTime now){Items.Insert(0,new PreferenceEdit{Label=label,Category=category,Restore=restore,At=now});if(Items.Count>20)Items.RemoveRange(20,Items.Count-20);AddRecent(label,category,now);}
  void AddRecent(string label,string category,DateTime now){Recent.RemoveAll(r=>r.Label==label&&r.Category==category);Recent.Insert(0,new RecentEdit{Label=label,Category=category,At=now});if(Recent.Count>12)Recent.RemoveRange(12,Recent.Count-12);Revision++;}
  public PreferenceEdit Peek(DateTime now){while(Items.Count>0&&now-Items[0].At>TimeSpan.FromSeconds(90))Items.RemoveAt(0);return Items.FirstOrDefault();}
  public PreferenceEdit Pop(DateTime now){var e=Peek(now);if(e!=null){Items.RemoveAt(0);Revision++;}return e;}
 }
 public static class TransitionSequence {
  // 0/1 = new inbetweens; 2/3 = existing action keyframes; -1 = relaxed original pose.
  public static int Frame(double milliseconds){int[] values={-1,0,1,2,3,2,1,0,-1};int[] times={100,160,170,250,620,240,190,190,180};double t=Math.Max(0,milliseconds);for(int i=0;i<times.Length;i++){if(t<times[i])return values[i];t-=times[i];}return -1;}
 }
}
