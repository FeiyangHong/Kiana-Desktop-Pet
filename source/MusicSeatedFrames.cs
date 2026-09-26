using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KianaPet {
 // Each source cell is a complete painted character. No separated limbs,
 // bone transforms, image warping, or opacity interpolation at playback.
 public static class MusicSeatedFrames {
  public const int FrameCount=8;
  public const double CycleMs=2400;
  public static readonly int[] Sequence={0,1,2,3,4,5,6,7,6,5,4,3,2,1};
  static string cachedSkin;
  static BitmapSource[] cached;
  public static int FrameIndex(double elapsed) {
   if(!MusicReactions.Finite(elapsed))return 0;
   return Sequence[(int)(Math.Max(0,elapsed)%CycleMs/CycleMs*Sequence.Length)%Sequence.Length];
  }
  public static BitmapSource[] Load(string skin) {
   if(cachedSkin==skin&&cached!=null)return cached;
   if(!MusicSprites.Supported(skin))return null;
   var list=Store.Json.Deserialize<MusicSpriteCalibration[]>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","music-seated-calibration.json")));
   var c=Array.Find(list,r=>r.Skin==skin);
   if(c==null||c.Frames==null||c.Frames.Length!=FrameCount||!MusicReactions.Finite(c.ReferenceHeadHeight)||c.ReferenceHeadHeight<120||c.ReferenceHeadHeight>300||!MusicReactions.Finite(c.ReferenceCenter)||!MusicReactions.Finite(c.ReferenceFoot))throw new InvalidDataException("坐姿完整帧校准无效");
   var atlas=new BitmapImage();atlas.BeginInit();atlas.CacheOption=BitmapCacheOption.OnLoad;atlas.UriSource=new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets",skin+"-music-seated.png"));atlas.EndInit();atlas.Freeze();
   if(atlas.PixelWidth<768||atlas.PixelWidth>4096||atlas.PixelHeight>4096)throw new InvalidDataException("坐姿完整图集尺寸无效");
   var frames=new BitmapSource[FrameCount];
   for(int i=0;i<FrameCount;i++) {
    var f=c.Frames[i];
    if(f==null||f.X<0||f.Y<0||f.Width<1||f.Height<1||f.X+f.Width>atlas.PixelWidth||f.Y+f.Height>atlas.PixelHeight||!MusicReactions.Finite(f.Center)||!MusicReactions.Finite(f.Foot)||!MusicReactions.Finite(f.HeadHeight)||f.HeadHeight<=0)throw new InvalidDataException("坐姿完整帧裁切无效");
    double scale=c.ReferenceHeadHeight/f.HeadHeight;
    if(!MusicReactions.Finite(scale)||scale<.3||scale>2.5)throw new InvalidDataException("坐姿完整帧比例无效");
    var raw=MusicSpriteCrop.Extract(atlas,new Int32Rect(f.X,f.Y,f.Width,f.Height));
    var visual=new DrawingVisual();RenderOptions.SetBitmapScalingMode(visual,BitmapScalingMode.HighQuality);
    using(var dc=visual.RenderOpen())dc.DrawImage(raw,new Rect(c.ReferenceCenter-f.Center*scale,c.ReferenceFoot-f.Foot*scale,f.Width*scale,f.Height*scale));
    var frame=new RenderTargetBitmap(768,832,192,192,PixelFormats.Pbgra32);frame.Render(visual);frame.Freeze();frames[i]=frame;
   }
   cachedSkin=skin;cached=frames;return frames;
  }
 }
}
