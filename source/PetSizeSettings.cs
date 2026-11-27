using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Forms = System.Windows.Forms;

namespace KianaPet {
 public static class PetSizeSettings {
  public const int Minimum=96, Maximum=320;
  public static bool Valid(int value){return value>=Minimum && value<=Maximum;}

  public static int Effective(Config config,string skin){
   int value;
   if(skin!=null && config.SkinSizes!=null && config.SkinSizes.TryGetValue(skin,out value) && Valid(value))return value;
   return Math.Max(Minimum,Math.Min(Maximum,config.Size));
  }

  public static void Validate(Config config){
   config.Size=Math.Max(Minimum,Math.Min(Maximum,config.Size));
   if(config.SkinSizes==null){config.SkinSizes=new Dictionary<string,int>();return;}
   if(config.SkinSizes.Count==0)return;
   var installed=new HashSet<string>(PetCatalog.Load().Select(p=>p.Id),StringComparer.Ordinal);
   config.SkinSizes=config.SkinSizes.Where(p=>installed.Contains(p.Key) && Valid(p.Value))
     .ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal);
  }
 }

 public sealed partial class PetWindow {
  public void SetSkinSize(string skin,int? size){
   if(!Pets.Any(p=>p.Id==skin))throw new ArgumentException("未知皮肤。","skin");
   if(size.HasValue&&!PetSizeSettings.Valid(size.Value))throw new ArgumentOutOfRangeException("size");
   int before=PetSizeSettings.Effective(Settings,Settings.Skin);
   if(size.HasValue)Settings.SkinSizes[skin]=size.Value;
   else Settings.SkinSizes.Remove(skin);
   FinishSizeEdit(before);
  }

  public void SetSharedSize(int size){
   if(!PetSizeSettings.Valid(size))throw new ArgumentOutOfRangeException("size");
   int before=PetSizeSettings.Effective(Settings,Settings.Skin);
   Settings.Size=size;
   FinishSizeEdit(before);
  }

  void FinishSizeEdit(int before){
   if(before!=PetSizeSettings.Effective(Settings,Settings.Skin))ResizePet();
   Save();
  }

  double PetWindowWidth(int size){return Math.Max((Settings.LargeTouchTargets?210:176)*Settings.ToolbarPercent/100d,size);}
  double PetWindowHeight(int size){return size*208d/192+58+(Settings.LargeTouchTargets?48:40)*Settings.ToolbarPercent/100d;}
  double DisplayedPetSize{get{return double.IsNaN(sprite.Width)?PetSizeSettings.Effective(Settings,Settings.Skin):sprite.Width;}}

  static Rectangle ResizeAtFeet(Rectangle old,int width,int height,Rectangle workArea){
   // Integer midpoints keep repeated odd/even width changes from drifting.
   var intended=new Rectangle(old.Left+old.Width/2-width/2,old.Bottom-height,width,height);
   return ScreenPlacement.Fit(intended,new[]{workArea});
  }

  void ApplyPetSize(string skin){
   int size=PetSizeSettings.Effective(Settings,skin);
   double width=PetWindowWidth(size),height=PetWindowHeight(size);
   if(sprite.Width==size && Width==width && Height==height)return;
   bool anchored=IsLoaded && handle!=IntPtr.Zero && !double.IsNaN(sprite.Width);
   var old=Bounds();
   Rectangle area=anchored?Forms.Screen.FromHandle(handle).WorkingArea:Rectangle.Empty;
   Width=width;Height=height;sprite.Width=size;sprite.Height=size*208d/192;
   ApplySpriteAlignment();
   if(!anchored)return;
   UpdateLayout();
   var resized=Bounds();
   var target=ResizeAtFeet(new Rectangle(old.Left,old.Top,old.Width,old.Height),resized.Width,resized.Height,area);
   MoveTo(target.X,target.Y);
   if(Settings.MusicPosition!="free")PositionMusic();
  }

  // A standing-up transition can still display the previous skin when the new preference is saved.
  Rectangle SavedPetBounds(){
   var old=Bounds();var rect=new Rectangle(old.Left,old.Top,old.Width,old.Height);
   int size=PetSizeSettings.Effective(Settings,Settings.Skin);
   if(DisplayedPetSize==size)return rect;
   int w=(int)Math.Ceiling(PetWindowWidth(size)*DpiScale),h=(int)Math.Ceiling(PetWindowHeight(size)*DpiScale);
   return ResizeAtFeet(rect,w,h,Forms.Screen.FromHandle(handle).WorkingArea);
  }
 }
}
