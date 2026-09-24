using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Drawing;using System.Runtime.InteropServices;using Forms=System.Windows.Forms;
namespace KianaPet {
 public sealed class DisplayPosition {public string Key,MusicPosition;public double X,Y,MusicX,MusicY;public bool MusicHasPosition;public DateTime SavedAt;}
 public sealed class DisplayMemory {
  public List<DisplayPosition> Items=new List<DisplayPosition>();
  public static string Key(IEnumerable<string> monitors){return string.Join("|",monitors.OrderBy(x=>x,StringComparer.Ordinal));}
  public DisplayPosition Find(string key){return Items.FirstOrDefault(i=>i.Key==key);}
  public void Remember(string key,Config c){if(string.IsNullOrEmpty(key))return;Items.RemoveAll(i=>i.Key==key);Items.Insert(0,new DisplayPosition{Key=key,X=c.X,Y=c.Y,MusicX=c.MusicX,MusicY=c.MusicY,MusicPosition=c.MusicPosition,MusicHasPosition=c.MusicHasPosition,SavedAt=DateTime.UtcNow});if(Items.Count>20)Items.RemoveRange(20,Items.Count-20);}
  public static DisplayMemory Load(){try{var m=Store.Json.Deserialize<DisplayMemory>(File.ReadAllText(Path.Combine(Store.Root,"display-layouts.json")));m.Items=m.Items.Where(i=>i!=null&&i.Key!=null&&i.Key.Length<8192&&!double.IsNaN(i.X)&&!double.IsInfinity(i.X)&&!double.IsNaN(i.Y)&&!double.IsInfinity(i.Y)&&!double.IsNaN(i.MusicX)&&!double.IsInfinity(i.MusicX)&&!double.IsNaN(i.MusicY)&&!double.IsInfinity(i.MusicY)).Take(20).ToList();return m;}catch{return new DisplayMemory();}}
  public void Save(){Store.Atomic("display-layouts.json",this);}
  [DllImport("user32.dll")]static extern IntPtr MonitorFromPoint(Native.Point point,uint flags);
  [DllImport("shcore.dll")]static extern int GetDpiForMonitor(IntPtr monitor,int type,out uint x,out uint y);
  public static string CurrentKey(){return Key(Forms.Screen.AllScreens.Select(s=>{uint x=96,y=96;try{GetDpiForMonitor(MonitorFromPoint(new Native.Point{X=s.Bounds.Left+1,Y=s.Bounds.Top+1},2),0,out x,out y);}catch{}return s.DeviceName+":"+s.Bounds.ToString()+":"+s.WorkingArea.ToString()+":"+x+"x"+y;}));}
 }
 public sealed partial class PetWindow {
  DisplayMemory displayMemory;string displayKey;bool displayPending;double displayDue,lastPositionSave;
  void StartDisplayMemory(){displayMemory=DisplayMemory.Load();displayKey=DisplayMemory.CurrentKey();if(Settings.RememberLayouts)RestoreLayout(displayKey);}
  void RestoreLayout(string key){var p=displayMemory.Find(key);if(p==null){ClampPosition();RecoverMusicCoordinates();return;}Settings.MusicX=p.MusicX;Settings.MusicY=p.MusicY;Settings.MusicPosition=new[]{"below","left","right","free"}.Contains(p.MusicPosition)?p.MusicPosition:"below";Settings.MusicHasPosition=p.MusicHasPosition;MoveTo(p.X,p.Y);ClampPosition();RecoverMusicCoordinates();PositionMusic();}
  void RememberDisplay(){if(displayMemory==null||!Settings.RememberLayouts||displayPending||displayKey!=DisplayMemory.CurrentKey())return;displayMemory.Remember(displayKey,Settings);displayMemory.Save();}
  void CheckDisplay(double now){
   if(now>=nextDisplayProbe){nextDisplayProbe=now+5;if(!displayPending&&displayKey!=DisplayMemory.CurrentKey())RepairDisplay();}
   if(displayPending&&now>=displayDue&&!sessionLocked){string key=DisplayMemory.CurrentKey();bool changed=key!=displayKey;displayKey=key;
    if(changed&&Settings.RememberLayouts)RestoreLayout(key);else{ClampPosition();RecoverMusicCoordinates();}
    PositionMusic();ApplyMenuLayers(false);wasStackVisible=false;displayPending=false;
    if(--displayRechecks>0){displayPending=true;displayDue=now+1.5;}else{recoveryActive=false;Save();}
   }
   if(now-lastPositionSave>8&&!dragging&&!pressed&&!displayPending){lastPositionSave=now;var rect=Bounds();if(rect.Left!=Settings.X||rect.Top!=Settings.Y)Save();}
  }
  public void ForgetDisplayLayouts(){string snapshot=Store.Json.Serialize(displayMemory);Edits.AddAction("清除布局记忆","场景与维护",delegate{displayMemory=Store.Json.Deserialize<DisplayMemory>(snapshot);displayMemory.Save();},DateTime.UtcNow);Store.Atomic("recent-settings.json",Edits.Recent);displayMemory=new DisplayMemory();displayMemory.Save();}
 }
}
