using System;using System.IO;using System.Collections.Generic;using System.Windows;using System.Windows.Controls;using System.Windows.Media;using System.Windows.Media.Imaging;using System.Windows.Threading;
namespace KianaPet {
 public sealed class MusicSpriteCalibration {public string Skin;public double ReferenceHeadHeight,ReferenceFaceWidth,ReferenceCenter,ReferenceFoot;public MusicFrameCalibration[] Frames;}
 public sealed class MusicFrameCalibration {public string Sheet="";public int X,Y,Width,Height;public double HeadHeight,FaceWidth,Center,Foot,Lift,OffsetX;}
 public static class MusicSprites {
  static readonly Dictionary<string,BitmapSource[]> loaded=new Dictionary<string,BitmapSource[]>();
  public static bool Supported(string skin){return AnimationAssets.HasTracks(skin)?AnimationAssets.Supports(skin,"music-quiet"):LegacySupported(skin);}
  public static bool AddGentleAngle(string skin){return LegacySupported(skin)&&skin!="time-runner-kiana-round";}
  public static bool LegacySupported(string skin){return skin=="kiana-winter-wish-round"||skin=="kiana-fiery-wishing-star-round"||skin=="time-runner-kiana-round";}
  public static BitmapSource Playback(string skin,string state,double elapsed,bool reduced,bool rising,out int key,string seatedStyle="classic"){
   if(!Supported(skin)){key=-1;return null;}
   if(AnimationAssets.HasTracks(skin)){
    bool sitting=state=="music-quiet";
    string transition=sitting?"music-sit":"music-rise";
    double intro=sitting||rising?AnimationAssets.Duration(skin,transition):0;
    string action=!reduced&&elapsed<intro?transition:state;
    double t=reduced?0:action==transition?elapsed:elapsed-intro;
    var track=AnimationAssets.Track(skin,action,seatedStyle);
    int group=action=="music-sit"?0:action=="music-rise"?1:action=="music-gentle"?2:action=="music-lively"?3:MusicSeatedFrames.NormalizeStyle(seatedStyle)=="nod"?6:MusicSeatedFrames.NormalizeStyle(seatedStyle)=="sway"?5:4;
    key=group*1000+track.Order[track.Slot(t)];
    return AnimationAssets.Frame(skin,action,t,seatedStyle).Source;
   }
   if(state=="music-quiet"&&(reduced||elapsed>=MusicReactions.SitTransitionMs)){
    int index=reduced?0:MusicSeatedFrames.FrameIndex(elapsed-MusicReactions.SitTransitionMs);key=(MusicSeatedFrames.NormalizeStyle(seatedStyle)=="nod"?3000:MusicSeatedFrames.NormalizeStyle(seatedStyle)=="sway"?2000:1000)+index;return MusicSeatedFrames.Load(skin,seatedStyle)[index];
   }
   key=MusicReactions.PlaybackFrame(state,elapsed,reduced,rising);var frames=Load(skin);return frames==null?null:frames[key];
  }
  public static BitmapSource[] Load(string skin){BitmapSource[] result;if(loaded.TryGetValue(skin,out result))return result;if(!LegacySupported(skin))return null;
   try{var calibrations=Store.Json.Deserialize<MusicSpriteCalibration[]>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","music-sprite-calibration.json")));var calibration=Array.Find(calibrations,c=>c.Skin==skin);if(calibration==null||calibration.Frames==null||calibration.Frames.Length!=24||!MusicReactions.Finite(calibration.ReferenceFaceWidth)||calibration.ReferenceFaceWidth<80||calibration.ReferenceFaceWidth>180||!MusicReactions.Finite(calibration.ReferenceHeadHeight)||calibration.ReferenceHeadHeight<120||calibration.ReferenceHeadHeight>300||!MusicReactions.Finite(calibration.ReferenceCenter)||!MusicReactions.Finite(calibration.ReferenceFoot))throw new InvalidDataException("音乐动作比例配置错误");
    // All poses use the approved idle head height (crown to chin), including seated poses: never enlarge
    // a seated character to fill the standing height. Each atlas has six detailed poses.
    var sheets=new Dictionary<string,BitmapSource>();result=new BitmapSource[24];for(int i=0;i<result.Length;i++){var c=calibration.Frames[i];if(c==null||!(c.Sheet==""||c.Sheet=="-gentle"||c.Sheet=="-lively"||c.Sheet=="-sit"))throw new InvalidDataException("音乐动作图集配置错误");BitmapSource atlas;if(!sheets.TryGetValue(c.Sheet,out atlas)){var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.UriSource=new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets",skin+"-music"+c.Sheet+".png"));image.EndInit();image.Freeze();if(image.PixelWidth<768||image.PixelWidth>4096||image.PixelHeight>4096)throw new InvalidDataException("音乐动作图集尺寸错误");atlas=image;sheets[c.Sheet]=atlas;}
     if(c.X<0||c.Y<0||c.Width<1||c.Height<1||c.X+c.Width>atlas.PixelWidth||c.Y+c.Height>atlas.PixelHeight||!MusicReactions.Finite(c.Center)||!MusicReactions.Finite(c.Foot)||!MusicReactions.Finite(c.Lift)||Math.Abs(c.Lift)>12||!MusicReactions.Finite(c.OffsetX)||Math.Abs(c.OffsetX)>80)throw new InvalidDataException("音乐动作裁切配置错误");var raw=MusicSpriteCrop.Extract(atlas,new Int32Rect(c.X,c.Y,c.Width,c.Height));raw.Freeze();double scale=calibration.ReferenceHeadHeight/c.HeadHeight;if(scale<.3||scale>2.5||!MusicReactions.Finite(scale))throw new InvalidDataException("音乐动作缩放无效");var drawing=new DrawingVisual();RenderOptions.SetBitmapScalingMode(drawing,BitmapScalingMode.HighQuality);using(var dc=drawing.RenderOpen())dc.DrawImage(raw,new Rect(calibration.ReferenceCenter-c.Center*scale+c.OffsetX,calibration.ReferenceFoot-c.Foot*scale-c.Lift,c.Width*scale,c.Height*scale));var frame=new RenderTargetBitmap(768,832,192,192,PixelFormats.Pbgra32);frame.Render(drawing);frame.Freeze();result[i]=frame;}var seated=MusicSeatedFrames.Load(skin);for(int i=0;i<6;i++)result[i]=seated[i*MusicSeatedFrames.FrameCount/6];loaded.Clear();loaded[skin]=result;return result;
   }catch(Exception e){Store.Log("Music sprites: "+e.Message);loaded[skin]=null;return null;}
  }
 }
 public sealed class MusicAnimationPreview:Window {
  readonly DispatcherTimer timer=new DispatcherTimer();public readonly Image Sprite=new Image();readonly DateTime started=DateTime.UtcNow;
  public MusicAnimationPreview(string skin,string mode,Config config=null){Title="音乐动作预览 · "+MusicReactions.Label(mode);Width=370;Height=470;WindowStartupLocation=WindowStartupLocation.CenterScreen;PetVisuals.Apply(this,true);Background=PetPalette.Brush(PetPalette.Background);var root=new StackPanel{Margin=new Thickness(20)};Sprite.Width=270;Sprite.Height=292.5;Sprite.Stretch=Stretch.Fill;RenderOptions.SetBitmapScalingMode(Sprite,BitmapScalingMode.HighQuality);root.Children.Add(Sprite);root.Children.Add(CompanionControls.Text("独立预览，不修改播放、任务或歌曲偏好。",12));root.Children.Add(CompanionControls.Button("关闭预览",Close));Content=root;string state=mode=="gentle"?"music-gentle":mode=="lively"?"music-lively":"music-quiet";Action draw=delegate{if(MusicSprites.Supported(skin)){double ms=AnimationTiming.MusicElapsed(config,skin,state,(DateTime.UtcNow-started).TotalMilliseconds,false);int key;Sprite.Source=MusicSprites.Playback(skin,state,ms,MotionSettings.Reduced,false,out key,config==null?"classic":config.MusicSeatedStyle);Sprite.RenderTransform=new RotateTransform(state=="music-gentle"&&MusicSprites.AddGentleAngle(skin)&&!MotionSettings.Reduced?MusicReactions.GentleAngle(ms):0,Sprite.Width*.46,Sprite.Width*1.03);}};draw();timer.Interval=TimeSpan.FromMilliseconds(33);timer.Tick+=delegate{draw();};Loaded+=delegate{timer.Start();};Closed+=delegate{timer.Stop();};PreviewKeyDown+=delegate(object sender,System.Windows.Input.KeyEventArgs e){if(e.Key==System.Windows.Input.Key.Escape)Close();};}
 }
}
