using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KianaPet {
 public sealed class MusicRigPart {
  public int X,Y,Width,Height;
  public double DrawX,DrawY,DrawWidth,DrawHeight,PivotX,PivotY;
 }
 public sealed class MusicRigCalibration {
  public string Skin;
  public MusicRigPart Body,Stool,FarLeg,NearLeg;
  public double HipX,HipY;
 }
 // A single painted limb per side: opposite phases guarantee that the leg in
 // front really changes. No mirroring, image morphing, or frame-opacity blending.
 public static class MusicSeatedRig {
  public const int FrameCount=48;
  public const double CycleMs=2400;
  static string cachedSkin;
  static BitmapSource[] cached;
  public static double LegAngle(bool near,double phase) {
   // Positive rotation moves a hanging foot screen-left. Both shoes keep the
   // same side view; the range is small enough never to expose a camera-facing sole.
   return 10+24*Math.Sin(phase+(near?Math.PI:0));
  }
  public static int FrameIndex(double elapsed) {
   if(!MusicReactions.Finite(elapsed))return 0;
   return (int)(Math.Max(0,elapsed)%CycleMs/CycleMs*FrameCount)%FrameCount;
  }
  public static Point Ankle(MusicRigPart part,bool near,double phase) {
   var point=new Point(part.PivotX,part.DrawY+part.DrawHeight*.80);
   return new RotateTransform(LegAngle(near,phase),part.PivotX,part.PivotY).Transform(point);
  }
  public static MusicRigCalibration Calibration(string skin) {
   var list=Store.Json.Deserialize<MusicRigCalibration[]>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","music-seated-rig.json")));
   var c=Array.Find(list,r=>r.Skin==skin);
   if(c==null||!MusicReactions.Finite(c.HipX)||!MusicReactions.Finite(c.HipY))throw new InvalidDataException("坐姿骨骼配置无效");
   return c;
  }
  static BitmapSource Part(BitmapSource atlas,MusicRigPart p) {
   if(p==null||p.X<0||p.Y<0||p.Width<1||p.Height<1||p.X+p.Width>atlas.PixelWidth||p.Y+p.Height>atlas.PixelHeight||!MusicReactions.Finite(p.DrawX)||!MusicReactions.Finite(p.DrawY)||!MusicReactions.Finite(p.DrawWidth)||!MusicReactions.Finite(p.DrawHeight)||p.DrawWidth<=0||p.DrawHeight<=0||!MusicReactions.Finite(p.PivotX)||!MusicReactions.Finite(p.PivotY))throw new InvalidDataException("坐姿图层配置无效");
   return MusicSpriteCrop.Extract(atlas,new Int32Rect(p.X,p.Y,p.Width,p.Height));
  }
  static void Draw(DrawingContext dc,BitmapSource image,MusicRigPart p,double angle) {
   dc.PushTransform(new RotateTransform(angle,p.PivotX,p.PivotY));
   dc.DrawImage(image,new Rect(p.DrawX,p.DrawY,p.DrawWidth,p.DrawHeight));dc.Pop();
  }
  public static BitmapSource[] Load(string skin) {
   if(cachedSkin==skin&&cached!=null)return cached;
   var c=Calibration(skin);var atlas=new BitmapImage();atlas.BeginInit();atlas.CacheOption=BitmapCacheOption.OnLoad;atlas.UriSource=new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets",skin+"-music-seated.png"));atlas.EndInit();atlas.Freeze();
   var body=Part(atlas,c.Body);var stool=Part(atlas,c.Stool);var far=Part(atlas,c.FarLeg);var near=Part(atlas,c.NearLeg);
   var frames=new BitmapSource[FrameCount];
   for(int i=0;i<FrameCount;i++) {
    double phase=i*Math.PI*2/FrameCount;
    var visual=new DrawingVisual();RenderOptions.SetBitmapScalingMode(visual,BitmapScalingMode.HighQuality);
    using(var dc=visual.RenderOpen()) {
     Draw(dc,stool,c.Stool,0);
     // Knees, hips and both legs share the small upper-body sway, so sockets
     // cannot separate. The stool remains level and stationary.
     dc.PushTransform(new RotateTransform(1.2*Math.Sin(phase),c.HipX,c.HipY));
     Draw(dc,far,c.FarLeg,LegAngle(false,phase));
     Draw(dc,near,c.NearLeg,LegAngle(true,phase));
     Draw(dc,body,c.Body,0);dc.Pop();
    }
    var frame=new RenderTargetBitmap(768,832,192,192,PixelFormats.Pbgra32);frame.Render(visual);frame.Freeze();frames[i]=frame;
   }
   cachedSkin=skin;cached=frames;return frames;
  }
  public static void Clear(){cachedSkin=null;cached=null;}
 }
}
