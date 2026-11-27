using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KianaPet {
 public sealed class AnimationTrack {
  public string File;
  public int Columns, FrameWidth, FrameHeight, Duration;
  public int[] Order, Start;
  public double Stride;
  public double[] Travel;
  internal string AtlasPath;
  double[] speeds;

  internal void Validate(string manifest){
   if(string.IsNullOrEmpty(File)||Path.GetFileName(File)!=File||Columns<=0||FrameWidth<=0||FrameHeight<=0||
      Duration<=0||Order==null||Order.Length==0||Order.Any(i=>i<0)||Start==null||Start.Length!=Order.Length||
      Start[0]!=0||Start.Last()>=Duration||Start.Skip(1).Where((v,i)=>v<=Start[i]).Any())
    throw new InvalidDataException("动画时间表或图集布局无效：" + manifest);
   AtlasPath=Path.Combine(Path.GetDirectoryName(manifest),File);
   if(Travel==null)return;
   if(Travel.Length!=Start.Length+1||Travel[0]!=0||Stride<=0||Travel.Last()!=Stride||
      Travel.Skip(1).Where((v,i)=>!MusicReactions.Finite(v)||v<Travel[i]).Any())
    throw new InvalidDataException("行走距离曲线无效：" + manifest);
   var intervals=Start.Select((v,i)=>(double)(i+1<Start.Length?Start[i+1]:Duration)-v).ToArray();
   var rates=intervals.Select((v,i)=>(Travel[i+1]-Travel[i])/v).ToArray();
   speeds=new double[Start.Length];
   for(int i=0;i<Start.Length;i++){
    int prev=(i+Start.Length-1)%Start.Length;
    if(rates[prev]>0&&rates[i]>0)speeds[i]=3*(intervals[prev]+intervals[i])/
        ((2*intervals[i]+intervals[prev])/rates[prev]+(intervals[i]+2*intervals[prev])/rates[i]);
   }
  }
  public int Slot(double elapsed){
   int found=Array.BinarySearch(Start,(int)(Math.Max(0,elapsed)%Duration));
   return found>=0?found:~found-1;
  }
  // The approved walk uses a cyclic foot-contact curve, in 352 × 416 artwork units.
  public double Distance(double elapsed){
   if(Travel==null)return 0;
   elapsed=Math.Max(0,elapsed);
   double phase=elapsed%Duration;int i=Slot(phase),next=(i+1)%Start.Length;
   double interval=(next==0?Duration:Start[next])-Start[i],t=(phase-Start[i])/interval;
   return Math.Floor(elapsed/Duration)*Stride+Travel[i]+(Travel[i+1]-Travel[i])*t*t*(3-2*t)
          +interval*t*(1-t)*((1-t)*speeds[i]-t*speeds[next]);
  }
  public double ElapsedAtDistance(double distance){
   if(Travel==null)return 0;
   distance=Math.Max(0,distance);
   double cycles=Math.Floor(distance/Stride),within=distance-cycles*Stride,low=0,high=Duration;
   if(within==0)return cycles*Duration;
   for(int i=0;i<16;i++){
    double mid=(low+high)*.5;
    if(Distance(mid)<within)low=mid;else high=mid;
   }
   return cycles*Duration+(low+high)*.5;
  }
 }

 public static class AnimationAssets {
  // New character tracks retain the approved per-cel registration. Assets without
  // a manifest continue to use the original Kiana atlases and timing tables.
  static readonly Lazy<Dictionary<string,string>> manifests=new Lazy<Dictionary<string,string>>(
   ()=>PetCatalog.Load().Where(p=>!string.IsNullOrEmpty(p.AnimationManifest)).ToDictionary(p=>p.Id,
    p=>Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets",p.AnimationManifest),StringComparer.Ordinal));
  static readonly Dictionary<string,Dictionary<string,AnimationTrack>> tracks=new Dictionary<string,Dictionary<string,AnimationTrack>>();
  sealed class Sheet {
   public string Path;
   public BitmapSource[] Cells;
  }
  // Limit full-resolution decoded images retained across live playback and the galleries.
  static readonly LinkedList<Sheet> recentSheets=new LinkedList<Sheet>();
  public static bool HasTracks(string skin){return !string.IsNullOrEmpty(skin)&&manifests.Value.ContainsKey(skin);}
  public static AnimationTrack Track(string skin,string action,string style="classic"){
   if(!HasTracks(skin))return null;
   Dictionary<string,AnimationTrack> map;
   if(!tracks.TryGetValue(skin,out map)){
    string path=manifests.Value[skin];
    map=Store.Json.Deserialize<Dictionary<string,AnimationTrack>>(File.ReadAllText(path));
    if(map==null||!map.ContainsKey("idle"))throw new InvalidDataException("缺少待机动画："+path);
    foreach(var item in map.Values)item.Validate(path);
    tracks.Add(skin,map);
   }
   if(action=="music-quiet"){
    style=MusicSeatedFrames.NormalizeStyle(style);
    if(style!="classic")action+="-"+style;
   }
   AnimationTrack track;return map.TryGetValue(action,out track)?track:null;
  }
  public static bool Supports(string skin,string action){
   if(HasTracks(skin))return Track(skin,action)!=null;
   return !string.IsNullOrEmpty(skin)&&action!="drink"&&(!action.StartsWith("music-",StringComparison.Ordinal)||MusicSprites.LegacySupported(skin))
          &&AnimationGallery.Catalog.Any(c=>c.Id==action);
  }
  public static double Duration(string skin,string action,string style="classic"){
   var track=Track(skin,action,style);
   return track!=null?track.Duration:AnimationGallery.Catalog.First(c=>c.Id==action).Duration;
  }
  static BitmapSource[] Cells(AnimationTrack track){
   for(var node=recentSheets.First;node!=null;node=node.Next){
    if(node.Value.Path!=track.AtlasPath)continue;
    recentSheets.Remove(node);recentSheets.AddFirst(node);
    return node.Value.Cells;
   }
   BitmapSource[] cells;
   using(var stream=System.IO.File.OpenRead(track.AtlasPath)){
    var atlas=new PngBitmapDecoder(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames[0];
    if(atlas.PixelWidth!=track.Columns*track.FrameWidth||atlas.PixelHeight%track.FrameHeight!=0)
     throw new InvalidDataException("图集尺寸与动作清单不符："+track.File);
    cells=new BitmapSource[track.Columns*(atlas.PixelHeight/track.FrameHeight)];
    var source=new FormatConvertedBitmap(atlas,PixelFormats.Bgra32,null,0);
    // Add transparent sides without resampling. The WPF pet and previews use
    // a 192:208 viewport; stretching a 352:416 cel into it widens the face.
    int paddedWidth=track.FrameHeight*192/208;
    if(paddedWidth<track.FrameWidth||paddedWidth*208!=track.FrameHeight*192)
     throw new InvalidDataException("动画帧与桌宠画布比例不兼容："+track.File);
    int stride=paddedWidth*4,left=(paddedWidth-track.FrameWidth)/2;
    var data=new byte[stride*track.FrameHeight];
    for(int i=0;i<cells.Length;i++){
     var rect=new Int32Rect(i%track.Columns*track.FrameWidth,i/track.Columns*track.FrameHeight,track.FrameWidth,track.FrameHeight);
     source.CopyPixels(rect,data,stride,left*4);
     cells[i]=BitmapSource.Create(paddedWidth,track.FrameHeight,96,96,PixelFormats.Bgra32,null,data,stride);
     cells[i].Freeze();
    }
   }
   recentSheets.AddFirst(new Sheet{Path=track.AtlasPath,Cells=cells});
   while(recentSheets.Count>3)recentSheets.RemoveLast();
   return cells;
  }
  public static AnimationPose Frame(string skin,string action,double elapsed,string style="classic"){
   var track=Track(skin,action,style);
   if(track==null)return null;
   int cel=track.Order[track.Slot(elapsed)];
   var cells=Cells(track);
   if(cel>=cells.Length)throw new InvalidDataException("动画帧超出图集："+track.File);
   return new AnimationPose{Source=cells[cel],Key=track.File+":"+cel,SourceLabel=action=="music-quiet"?
      "完整人物帧 · "+MusicSeatedFrames.StyleLabel(style):"完整人物帧"};
  }
 }
}
