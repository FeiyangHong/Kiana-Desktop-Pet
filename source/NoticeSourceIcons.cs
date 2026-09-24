using System;using System.IO;using System.Collections.Generic;using System.Windows;using System.Windows.Media;using System.Windows.Media.Imaging;
namespace KianaPet {
 public static class NoticeSourceIcons {
  public static readonly string[] Styles={"icon-name","icon-only","leading-icon","name-only"};
  static readonly Dictionary<string,ImageSource> registered=new Dictionary<string,ImageSource>(StringComparer.OrdinalIgnoreCase);
  static ImageSource lightChat,darkChat,lightFallback,darkFallback;static DateTime retryAt;
  // Future in-process adapters may supply their own frozen source icon. No paths or URLs
  // from notification text are executed or downloaded.
  public static void Register(string source,ImageSource icon){if(string.IsNullOrWhiteSpace(source)||source.Length>60||icon==null)throw new ArgumentException("Invalid source icon");var frozen=icon.Clone();if(!frozen.CanFreeze)throw new ArgumentException("Source icon must be freezable");frozen.Freeze();lock(registered){if(registered.Count>=64&&!registered.ContainsKey(source))throw new InvalidOperationException("Too many source icons");registered[source]=frozen;}}
  public static ImageSource Resolve(string source,bool chatGPT,bool dark){lock(registered){ImageSource supplied;if(!chatGPT&&source!=null&&registered.TryGetValue(source,out supplied))return supplied;}
   if(chatGPT){if(DateTime.UtcNow>=retryAt){retryAt=DateTime.UtcNow.AddMinutes(1);LoadChatIcons();}var icon=dark?darkChat:lightChat;if(icon!=null)return icon;}
   if(dark){if(darkFallback==null)darkFallback=Fallback(true);return darkFallback;}if(lightFallback==null)lightFallback=Fallback(false);return lightFallback;
  }
  static void LoadChatIcons(){try{string session=Path.Combine(CompanionPaths.SmoothRoot(),"session.json");if(!File.Exists(session)||new FileInfo(session).Length>65536)return;var saved=Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(session));object value;if(saved==null||!saved.TryGetValue("app",out value))return;string app=Path.GetFullPath(Convert.ToString(value));string prefix=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),"WindowsApps","OpenAI.Codex_");if(!app.StartsWith(prefix,StringComparison.OrdinalIgnoreCase)||!app.EndsWith(@"\app\ChatGPT.exe",StringComparison.OrdinalIgnoreCase))return;string assets=Path.Combine(Directory.GetParent(Path.GetDirectoryName(app)).FullName,"assets");lightChat=Load(Path.Combine(assets,"Square44x44Logo.targetsize-96_altform-lightunplated.png"));darkChat=Load(Path.Combine(assets,"Square44x44Logo.targetsize-96_altform-unplated.png"));}catch{lightChat=darkChat=null;}}
  static ImageSource Load(string file){if(!File.Exists(file)||new FileInfo(file).Length>1024*1024)return null;using(var stream=File.OpenRead(file)){var image=new BitmapImage();image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.DecodePixelWidth=96;image.StreamSource=stream;image.EndInit();image.Freeze();return image;}}
  static ImageSource Fallback(bool dark){var group=new DrawingGroup();var ink=PetPalette.Brush(PetPalette.Hex(dark?"#CEC8D8":"#777080"));group.Children.Add(new GeometryDrawing(Brushes.Transparent,null,new RectangleGeometry(new Rect(0,0,24,24))));group.Children.Add(new GeometryDrawing(null,new Pen(ink,1.5){LineJoin=PenLineJoin.Round,StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},Geometry.Parse("M5,3 L19,3 Q21,3 21,5 L21,16 Q21,18 19,18 L10,18 L5,22 L5,18 Q3,18 3,16 L3,5 Q3,3 5,3 M7,8 L17,8 M7,12 L14,12")));group.Freeze();var image=new DrawingImage(group);image.Freeze();return image;}
 }
}
