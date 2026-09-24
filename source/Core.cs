using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace KianaPet {
 public sealed class PetInfo {
  public string Id {get;set;} public string Name {get;set;} public string Atlas {get;set;}
 }
 public sealed class Config {
  public int Schema=1;
  public string Skin="kiana-fiery-wishing-star-round";
  public int Size=176;
  public bool RecoverHotkeyEnabled=false;
  public uint RecoverHotkeyModifiers=PetHotkey.Control|PetHotkey.Alt,RecoverHotkeyKey=0x50;
  public bool BackgroundMini=true;
  public bool NoticePreviewHover=true,NoticePreviewFollowMusic=false;
  public int NoticePreviewWidth=420,NoticePreviewCount=1;
  public bool LaunchChatGPTOnStart=false,LaunchMusicOnStart=false,NoticeSound=false,NoticeQuiet=false;public int CompletionNoticeSeconds=6,NotificationGraceSeconds=30;
  public bool HasPosition=false,LockPetPosition=false,LockMusicPosition=false,SmoothTransitions=true;
  public string WalkProfile="occasional";
  public int WalkSpeed=38,WalkDistanceMin=65,WalkDistanceMax=235,WalkPauseMin=12,WalkPauseMax=32;
  public bool QuietAppsEnabled=false,QuietForegroundOnly=true,QuietHoursEnabled=false;
  public string[] QuietApps=new string[0];
  public string QuietMode="silent",QuietStart="09:00",QuietEnd="18:00";
  public bool TaskNoticesEnabled=true;
  public string Theme="light";public string MusicTintMode="solid";
  public bool MusicCoverTint=true,RememberLayouts=true,ResourceSaving=true,AmbientActions=true,NoticeSilentSuccess=true;
  public int MusicTintStrength=20,ToolbarPercent=100,NoticeBatchSeconds=5;
  public ScenePreset[] Scenes=ScenePreset.Defaults();
  public bool AvoidCursor=true,ReduceMotion=false,LargeTouchTargets=false,MusicPausedCard=true;
  public int BackupKeepCount=3;
  public string[] MutedNoticeSources=new string[0];
  public bool MusicEnabled=true,MusicLyrics=false;
  public bool ToolbarPinned=false,ToolbarFollowMusic=false,YieldToMenus=true;
  public int MusicScalePercent=100,MusicWidth=238;
  public bool MusicHoverOnly=true,MusicHasPosition=false;
  public string MusicPosition="below";
  public string MusicDefaultPosition="below";
  public double MusicX=0,MusicY=0;
  public bool Walking=true, Gravity=true, SleepSchedule=true, IdleSleep=true, HideFullscreen=true, Bubbles=true, LinkChatGPT=true, HoverToolbar=true;
  public string Bedtime="02:00", WakeTime="09:00";
  public int IdleMinutes=10;
  public double X=-1, Y=-1;
  public void Validate() {
   NoticePreviewWidth=Math.Max(320,Math.Min(560,NoticePreviewWidth));NoticePreviewCount=Math.Max(1,Math.Min(4,NoticePreviewCount));
   ComfortRules.Validate(this);CompletionNoticeSeconds=Math.Max(2,Math.Min(30,CompletionNoticeSeconds));NotificationGraceSeconds=Math.Max(10,Math.Min(120,NotificationGraceSeconds));
   PolishRules.Validate(this);
   BackupKeepCount=Math.Max(1,Math.Min(10,BackupKeepCount));MutedNoticeSources=(MutedNoticeSources??new string[0]).Where(s=>!string.IsNullOrWhiteSpace(s)&&s.Length<=60).Distinct(StringComparer.Ordinal).Take(50).ToArray();
   if(!PetHotkey.Valid(RecoverHotkeyModifiers,RecoverHotkeyKey)){RecoverHotkeyEnabled=false;RecoverHotkeyModifiers=PetHotkey.Control|PetHotkey.Alt;RecoverHotkeyKey=0x50;}
   if(!new[]{"below","left","right","free"}.Contains(MusicPosition))MusicPosition="below";
   if(!new[]{"below","left","right"}.Contains(MusicDefaultPosition))MusicDefaultPosition="below";
   if(double.IsNaN(MusicX)||double.IsInfinity(MusicX)||double.IsNaN(MusicY)||double.IsInfinity(MusicY)){MusicX=MusicY=0;MusicHasPosition=false;}
   Size=Math.Max(96,Math.Min(320,Size)); IdleMinutes=Math.Max(1,Math.Min(120,IdleMinutes));
   TimeSpan a,b; if(!TimeSpan.TryParse(Bedtime,out a)||a.TotalHours<0||a.TotalHours>=24)Bedtime="02:00";
   if(!TimeSpan.TryParse(WakeTime,out b)||b.TotalHours<0||b.TotalHours>=24)WakeTime="09:00";
   if(double.IsNaN(X)||double.IsInfinity(X)||double.IsNaN(Y)||double.IsInfinity(Y)){X=Y=-1;HasPosition=false;}
   else if(X!=-1||Y!=-1)HasPosition=true;
  }
 }
 public static class Store {
  public static string Root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"Codex","PetTools","KianaDesktopPet");
  public static readonly JavaScriptSerializer Json=new JavaScriptSerializer {MaxJsonLength=1024*1024};
  public static Config Load(){Directory.CreateDirectory(Root);string p=Path.Combine(Root,"settings.json");try{Config c=Json.Deserialize<Config>(File.ReadAllText(p));if(c==null)throw new Exception();c.Validate();return c;}catch{if(File.Exists(p))File.Copy(p,p+".invalid-"+DateTime.Now.ToString("yyyyMMdd-HHmmss"),true);return new Config();}}
  public static void Atomic(string name,object data){Directory.CreateDirectory(Root);string p=Path.Combine(Root,name),tmp=p+".tmp";File.WriteAllText(tmp,new JavaScriptSerializer().Serialize(data),new System.Text.UTF8Encoding(false));if(File.Exists(p))File.Replace(tmp,p,null);else File.Move(tmp,p);}
  public static void Save(Config c){c.Validate();Atomic("settings.json",c);}
  public static void Log(string text){try{Directory.CreateDirectory(Root);string p=Path.Combine(Root,"desktop.log");if(File.Exists(p)&&new FileInfo(p).Length>512000)File.Move(p,p+"."+DateTime.Now.ToString("yyyyMMddHHmmss"));File.AppendAllText(p,DateTime.Now.ToString("o")+" "+text+Environment.NewLine);}catch{}}
 }
 public static class Rules {
  public static bool IsSleepTime(TimeSpan now,TimeSpan bed,TimeSpan wake){if(bed==wake)return false;return bed<wake?now>=bed&&now<wake:now>=bed||now<wake;}
  public static string ChooseState(bool sleeping,bool dragging,string dragState,string interaction,string linked,bool walking,string direction){
   if(dragging)return dragState;if(interaction!=null)return interaction;if(sleeping)return "sleep";
   if(linked=="waiting"||linked=="failed"||linked=="review"||linked=="running")return linked;
   return walking?direction:"idle";
  }
  public static readonly string[] States={"idle","running-right","running-left","waving","jumping","failed","waiting","running","review"};
  public static readonly int[] Counts={6,8,8,4,5,8,6,6,6};
  public static int Row(string state){int n=Array.IndexOf(States,state);return n<0?0:n;}
  public static int Frame(string state,double elapsed){if(state=="sleep")return 2;int row=Row(state);int[] durations;
   if(row==0)durations=new[]{1680,660,660,840,840,1920};
   else {int normal=(row==1||row==2||row==7)?120:(row==6||row==8?150:140);int last=(row==1||row==2||row==7)?220:row==5?240:row==6?260:280;durations=Enumerable.Repeat(normal,Counts[row]).ToArray();durations[durations.Length-1]=last;}
   double t=elapsed%durations.Sum();for(int i=0;i<durations.Length;i++){if(t<durations[i])return i;t-=durations[i];}return 0;
  }
  public static double Clamp(double value,double min,double max){return Math.Max(min,Math.Min(Math.Max(min,max),value));}
  // Reverse after two same-direction outings when the edge permits it.
  public static double WalkTarget(double current,double min,double max,Random random,int lastDirection=0,int streak=0,double distanceMin=65,double distanceMax=235,double edge=24){
   current=Clamp(current,min,max);double left=current-min,right=Math.Max(0,max-current);
   bool goLeft=random.Next(2)==0;
   if(streak>=2&&lastDirection!=0)goLeft=lastDirection>0;
   if(left<edge&&right>=edge)goLeft=false;else if(right<edge&&left>=edge)goLeft=true;
   double room=goLeft?left:right;if(room<4){goLeft=!goLeft;room=goLeft?left:right;}
   double distance=Math.Min(Math.Max(0,room),distanceMin+random.NextDouble()*Math.Max(0,distanceMax-distanceMin));
   return Clamp(current+(goLeft?-distance:distance),min,max);
  }
 }
 public sealed class WalkPlanner {
  int lastDirection,streak;
  public double Next(double current,double min,double max,Random random,double distanceMin=65,double distanceMax=235,double edge=24){
   return NextSafe(current,min,max,random,distanceMin,distanceMax,edge,null);
  }
  public double NextSafe(double current,double min,double max,Random random,double distanceMin,double distanceMax,double edge,Func<double,bool> allowed){
   double target=Rules.WalkTarget(current,min,max,random,lastDirection,streak,distanceMin,distanceMax,edge);
   if(allowed!=null&&!allowed(target)){target=Rules.Clamp(current-(target-current),min,max);if(!allowed(target))return current;}
   if(Math.Abs(target-current)>3){int direction=Math.Sign(target-current);streak=direction==lastDirection?Math.Min(2,streak+1):1;lastDirection=direction;}
   return target;
  }
  public void Reset(){lastDirection=streak=0;}
 }
 // Reserved extension contract. No provider, credentials, background requests or AI calls are enabled.
 public interface ICompanionChatProvider {
  System.Threading.Tasks.Task<string> ReplyAsync(string userText,System.Threading.CancellationToken cancellation);
 }
}
