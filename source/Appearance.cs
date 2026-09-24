using System;using System.Windows;using System.Windows.Media;using System.Windows.Media.Imaging;using Microsoft.Win32;
namespace KianaPet {
 public static class PetPalette {
  public static bool Dark;public static Color Background{get{return Hex(Dark?"#27262D":"#FFFFFF");}}public static Color Ink{get{return Hex(Dark?"#F1EFF5":"#29272D");}}public static Color Muted{get{return Hex(Dark?"#C0BACB":"#6B6572");}}public static Color Line{get{return Hex(Dark?"#48434F":"#E9E7EC");}}
  public static Color Hex(string s){return (Color)ColorConverter.ConvertFromString(s);}public static SolidColorBrush Brush(Color c){return new SolidColorBrush(c);}
  public static bool IsDark(string theme){if(theme!="system")return theme=="dark";try{using(var k=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))return Convert.ToInt32(k==null?1:k.GetValue("AppsUseLightTheme",1))==0;}catch{return false;}}
  public static Color Mix(Color a,Color b,double t){t=Math.Max(0,Math.Min(1,t));return Color.FromRgb((byte)Math.Round(a.R+(b.R-a.R)*t),(byte)Math.Round(a.G+(b.G-a.G)*t),(byte)Math.Round(a.B+(b.B-a.B)*t));}
  public static Color Sample(BitmapSource source){if(source==null)return Hex("#A28ABA");var scaled=new TransformedBitmap(source,new ScaleTransform(16d/source.PixelWidth,16d/source.PixelHeight));var format=new FormatConvertedBitmap(scaled,PixelFormats.Bgra32,null,0);byte[] bytes=new byte[format.PixelWidth*format.PixelHeight*4];format.CopyPixels(bytes,format.PixelWidth*4,0);double r=0,g=0,b=0,total=0;for(int i=0;i<bytes.Length;i+=4){double max=Math.Max(bytes[i],Math.Max(bytes[i+1],bytes[i+2])),min=Math.Min(bytes[i],Math.Min(bytes[i+1],bytes[i+2]));double weight=bytes[i+3]/255d*(.2+(max-min)/255d);if(max<25||min>240)weight*=.1;r+=bytes[i+2]*weight;g+=bytes[i+1]*weight;b+=bytes[i]*weight;total+=weight;}return total<=0?Hex("#A28ABA"):Color.FromRgb((byte)(r/total),(byte)(g/total),(byte)(b/total));}
  public static Color MusicSurface(Color accent,bool dark,bool enabled,int strength){return Mix(Hex(dark?"#27262D":"#FFFFFF"),accent,enabled?Math.Max(0,Math.Min(100,strength))/100d*(dark?.20:.16):0);}
 }
 public sealed partial class PetWindow {
  bool? appliedDark;
  void ApplyAppearance(){bool dark=PetPalette.IsDark(Settings.Theme);PetPalette.Dark=dark;var scale=hoverBar.LayoutTransform as ScaleTransform;if(scale==null||scale.ScaleX!=Settings.ToolbarPercent/100d)hoverBar.LayoutTransform=new ScaleTransform(Settings.ToolbarPercent/100d,Settings.ToolbarPercent/100d);var root=Content as System.Windows.Controls.Grid;if(root!=null)root.RowDefinitions[2].Height=new GridLength((Settings.LargeTouchTargets?48:40)*Settings.ToolbarPercent/100d);
   if(appliedDark==dark)return;appliedDark=dark;if(settingsWindow!=null)settingsWindow.RefreshTheme();toolbarMusicAppearance.Invalidate();bubble.Background=PetPalette.Brush(PetPalette.Background);bubble.BorderBrush=PetPalette.Brush(PetPalette.Line);bubbleText.Foreground=PetPalette.Brush(PetPalette.Ink);
   chatButton.Content=ToolbarIcons.Create("chat");voiceButton.Content=ToolbarIcons.Create("voice");var badges=notificationButton.Content as System.Windows.Controls.Grid;if(badges!=null){badges.Children.RemoveAt(0);badges.Children.Insert(0,ToolbarIcons.Create("bell"));}var buttons=hoverBar.Child as System.Windows.Controls.Panel;foreach(UIElement child in buttons.Children){var separator=child as System.Windows.Controls.Border;if(separator!=null)separator.Background=PetPalette.Brush(PetPalette.Line);}
  }
 }
}

