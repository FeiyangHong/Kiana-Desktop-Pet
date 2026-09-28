using System;using System.Linq;using System.Windows;using System.Windows.Media;using System.Windows.Media.Imaging;
namespace KianaPet {
 public static class ToolbarTheme {
  public static readonly string[] Styles={"legacy","solid","linear","tricolor","prism","aurora","halo","silk","glass"};
  public static readonly string[] StyleNames={"原有简洁","纯色半透明","双色渐变","三色渐变","棱镜炫彩","极光柔光","中心光晕","丝绸渐变","轻透毛玻璃"};
  public static readonly string[] Sources={"preset","manual","music","outfit","background"};
  public static readonly string[] SourceNames={"渐变预设","手动三色","音乐封面","当前服装","附近背景色"};
  public static readonly string[] PresetNames={"冰蓝新愿","逐光淡紫","炽愿暖红","樱花奶霜","海盐薄荷","暮色霞光","极光森林","星河夜色","蜜桃汽水","琥珀香槟","虹彩糖果","雾白银灰"};
  public static readonly string[][] Presets={new[]{"#A4E1FF","#C7CAFF","#F4FAFF"},new[]{"#BEB0FA","#88C9F4","#E8C6FA"},new[]{"#EA8894","#F3BF8D","#FFE5CA"},new[]{"#F7B7D3","#E9D3FA","#FFF0E7"},new[]{"#8EDFD2","#B7E5F5","#E6F7C5"},new[]{"#E9ACCC","#B1B9EC","#F1CD9C"},new[]{"#72D2B4","#78C8E2","#A7A2E6"},new[]{"#7986D0","#A092D9","#7AABD6"},new[]{"#FFAEB0","#F9D29B","#EDC1F0"},new[]{"#E2B788","#F2DCA8","#EED7CB"},new[]{"#F4A6D3","#A7D7EE","#D3E5A0"},new[]{"#D5DCE7","#EEF0F5","#C6CFDE"}};
  public static bool IsHex(string value){return System.Text.RegularExpressions.Regex.IsMatch(value??"","^#[0-9a-fA-F]{6}$");}
  public static void Validate(Config c){if(!Styles.Contains(c.ToolbarStyle))c.ToolbarStyle="legacy";if(!Sources.Contains(c.ToolbarColorSource))c.ToolbarColorSource="preset";c.ToolbarPreset=Math.Max(0,Math.Min(Presets.Length-1,c.ToolbarPreset));c.ToolbarTint=Math.Max(0,Math.Min(100,c.ToolbarTint));c.ToolbarOpacity=Math.Max(15,Math.Min(100,c.ToolbarOpacity));c.ToolbarFlowSeconds=Math.Max(5,Math.Min(60,c.ToolbarFlowSeconds));c.ToolbarDirection=Math.Max(0,Math.Min(3,c.ToolbarDirection));c.ToolbarGlassFog=Math.Max(0,Math.Min(100,c.ToolbarGlassFog));if(!IsHex(c.ToolbarColor1))c.ToolbarColor1="#A4E1FF";if(!IsHex(c.ToolbarColor2))c.ToolbarColor2="#C7CAFF";if(!IsHex(c.ToolbarColor3))c.ToolbarColor3="#F4FAFF";if(!IsHex(c.ToolbarIconColor))c.ToolbarIconColor="#29272D";if(!IsHex(c.ToolbarLineColor))c.ToolbarLineColor="#C3C4DA";}
  public static Color[] ColorsFor(Config c,BitmapSource cover,Color[] backdrop){
   if(c.ToolbarColorSource=="manual")return new[]{PetPalette.Hex(c.ToolbarColor1),PetPalette.Hex(c.ToolbarColor2),PetPalette.Hex(c.ToolbarColor3)};
   if(c.ToolbarColorSource=="background"&&backdrop!=null)return backdrop;
   if(c.ToolbarColorSource=="music"&&cover!=null){var p=MusicGradient.SamplePair(cover);return new[]{p[0],p[1],PetPalette.Mix(p[0],p[1],.5)};}
   int index=c.ToolbarPreset;if(c.ToolbarColorSource=="outfit")index=c.Skin.Contains("winter")?0:c.Skin.Contains("time-runner")?1:2;
   return Presets[index].Select(PetPalette.Hex).ToArray();
  }
  public static Color[] Tinted(Config c,Color[] palette,bool dark){return palette.Select(x=>PetPalette.Mix(PetPalette.Hex(dark?"#27262D":"#FFFFFF"),x,c.ToolbarTint/100d)).ToArray();}
  public static Color Ink(Config c,Color[] colors,bool dark){if(!c.ToolbarAutoContrast)return PetPalette.Hex(c.ToolbarIconColor);Color light=PetPalette.Hex("#FAFAFF"),ink=PetPalette.Hex("#222531"),background=PetPalette.Hex(dark?"#27262D":"#FFFFFF");var visible=colors.Select(x=>PetPalette.Mix(background,x,c.ToolbarOpacity/100d)).ToArray();return visible.Min(x=>Contrast(x,ink))>=visible.Min(x=>Contrast(x,light))?ink:light;}
  static double Linear(byte c){double v=c/255d;return v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4);}
  static double Luminance(Color c){return .2126*Linear(c.R)+.7152*Linear(c.G)+.0722*Linear(c.B);}
  public static double Contrast(Color a,Color b){double x=Luminance(a),y=Luminance(b);return (Math.Max(x,y)+.05)/(Math.Min(x,y)+.05);}
  public static Brush MakeBrush(Config c,Color[] colors,double seconds,bool animate=true){
   double shift=animate&&c.ToolbarFlow&&!c.ReduceMotion?Math.Sin(seconds*Math.PI*2/c.ToolbarFlowSeconds)*.14:0;
   if(c.ToolbarStyle=="solid")return new SolidColorBrush(colors[0]){Opacity=c.ToolbarOpacity/100d};
   GradientBrush brush;if(c.ToolbarStyle=="halo"||c.ToolbarStyle=="aurora")brush=new RadialGradientBrush{Center=new Point(.5+shift,.45),GradientOrigin=new Point(c.ToolbarStyle=="aurora"?.2+shift:.5+shift,.15),RadiusX=1,RadiusY=1.2};
   else {Point[] a={new Point(0,.5),new Point(.5,0),new Point(0,0),new Point(1,0)},b={new Point(1,.5),new Point(.5,1),new Point(1,1),new Point(0,1)};brush=new LinearGradientBrush{StartPoint=new Point(a[c.ToolbarDirection].X-shift,a[c.ToolbarDirection].Y),EndPoint=new Point(b[c.ToolbarDirection].X+shift,b[c.ToolbarDirection].Y)};}
   Color[] stops=c.ToolbarStyle=="linear"?new[]{colors[0],colors[1]}:c.ToolbarStyle=="prism"?new[]{colors[0],PetPalette.Mix(colors[0],colors[2],.6),colors[1],PetPalette.Mix(colors[1],colors[2],.7),colors[2]}:c.ToolbarStyle=="silk"?new[]{colors[0],colors[2],colors[1],colors[2],colors[0]}:colors;
   for(int i=0;i<stops.Length;i++)brush.GradientStops.Add(new GradientStop(stops[i],i/(double)(stops.Length-1)));brush.Opacity=c.ToolbarOpacity/100d;return brush;
  }
 }
}
