using System;using System.Windows.Media;using System.Windows.Media.Animation;using System.Windows.Media.Imaging;
namespace KianaPet {
 public sealed partial class MusicWindow {
  readonly MusicSurfaceAppearance musicAppearance=new MusicSurfaceAppearance();bool paletteChanged;
  void RefreshPalette(BitmapSource art){paletteChanged=musicAppearance.Apply(surface,art,pet.Settings,true);if(!paletteChanged)return;
   shadowSurface.Background=surface.Background;surface.BorderBrush=PetPalette.Brush(PetPalette.Line);title.Foreground=PetPalette.Brush(PetPalette.Ink);Color secondary=pet.Settings.MusicTintMode=="solid"?PetPalette.Muted:PetPalette.Mix(PetPalette.Ink,PetPalette.Muted,.2);artist.Foreground=PetPalette.Brush(secondary);lyric.Foreground=PetPalette.Brush(secondary);previous.Content=MusicIcon("previous");next.Content=MusicIcon("next");lyrics.Content=MusicIcon("lyrics");
  }
 }
}
