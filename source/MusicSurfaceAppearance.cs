using System;using System.Windows;using System.Windows.Controls;using System.Windows.Media;using System.Windows.Media.Animation;using System.Windows.Media.Imaging;
namespace KianaPet {
 // Shared by both cards so the same cover, theme and strength produce identical colors.
 public sealed class MusicSurfaceAppearance {
  BitmapSource sampledCover;Color accent;Color[] coverColors;string paletteKey="";
  public void Invalidate(){paletteKey="";}
  static void AnimateColor(Animatable target,DependencyProperty property,Color color){Color from=(Color)target.GetValue(property);target.BeginAnimation(property,null);target.SetValue(property,color);if(MotionSettings.Enabled)target.BeginAnimation(property,new ColorAnimation(from,color,TimeSpan.FromMilliseconds(350)){FillBehavior=FillBehavior.Stop});}
  public bool Apply(Border surface,BitmapSource art,Config config,bool enabled){
   if(coverColors==null||!object.ReferenceEquals(art,sampledCover)){sampledCover=art;accent=PetPalette.Sample(art);coverColors=MusicGradient.SamplePair(art);Invalidate();}
   bool tint=enabled&&config.MusicCoverTint&&art!=null;string mode=enabled?config.MusicTintMode:"solid";
   string key=PetPalette.Dark+":"+tint+":"+config.MusicTintStrength+":"+mode+":"+MotionSettings.Enabled;
   if(key==paletteKey)return false;paletteKey=key;
   var solid=surface.Background as SolidColorBrush;var gradient=surface.Background as LinearGradientBrush;
   if(mode=="solid"){
    Color target=PetPalette.MusicSurface(accent,PetPalette.Dark,tint,config.MusicTintStrength);
    if(solid==null||solid.IsFrozen){Color from=gradient!=null?PetPalette.Mix(gradient.GradientStops[0].Color,gradient.GradientStops[1].Color,.5):target;solid=PetPalette.Brush(from);surface.Background=solid;}
    AnimateColor(solid,SolidColorBrush.ColorProperty,target);
   }else{
    var colors=MusicGradient.Stops(coverColors,art!=null,PetPalette.Dark,tint,config.MusicTintStrength,mode);
    if(gradient==null||gradient.IsFrozen){Color from=solid!=null?solid.Color:colors[0];gradient=MusicGradient.Brush(new[]{from,from});surface.Background=gradient;}
    AnimateColor(gradient.GradientStops[0],GradientStop.ColorProperty,colors[0]);AnimateColor(gradient.GradientStops[1],GradientStop.ColorProperty,colors[1]);
   }return true;
  }
 }
}
