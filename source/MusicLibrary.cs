using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Media.Imaging;
namespace KianaPet {
 public sealed class LyricLine{public double Time;public string Text;}
 public sealed class MusicLibrary:IDisposable {
  readonly HttpClient http=new HttpClient(new HttpClientHandler{UseProxy=false,AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(7)};
  readonly CancellationTokenSource shutdown=new CancellationTokenSource();
  string id="",coverUrl="",lyricId="",lyricStatus="";int generation,lyricGeneration;List<LyricLine> lines=new List<LyricLine>();public BitmapSource Cover{get;private set;}
  public static string Identity(MusicState state){return string.IsNullOrEmpty(state.Id)?"meta:"+state.Title+"\n"+state.Artist:"id:"+state.Id;}
  public BitmapSource CoverFor(MusicState state){return state.Connected&&id==Identity(state)&&coverUrl==state.Cover?Cover:null;}
  public static List<LyricLine> ParseLrc(string text){var result=new List<LyricLine>();double offset=0;var off=Regex.Match(text??"",@"\[offset:([+-]?\d+)\]",RegexOptions.IgnoreCase);if(off.Success)offset=double.Parse(off.Groups[1].Value,System.Globalization.CultureInfo.InvariantCulture)/1000;
   foreach(var row in (text??"").Split('\n')){var stamps=Regex.Matches(row,@"\[(\d{1,3}):(\d{2})(?:[.:](\d{1,3}))?\]");if(stamps.Count==0)continue;var last=stamps[stamps.Count-1];string words=row.Substring(last.Index+last.Length).Trim();foreach(Match stamp in stamps){double seconds=int.Parse(stamp.Groups[1].Value)*60+int.Parse(stamp.Groups[2].Value);string fraction=stamp.Groups[3].Value;if(fraction.Length>0)seconds+=int.Parse(fraction)/Math.Pow(10,fraction.Length);result.Add(new LyricLine{Time=seconds-offset,Text=words});}}
   return result.OrderBy(l=>l.Time).ToList();
  }
  public void Update(MusicState state,bool lyrics){if(!state.Connected){id="";lyricId="";lines.Clear();Cover=null;generation++;lyricGeneration++;return;}if(id!=Identity(state)||coverUrl!=state.Cover){id=Identity(state);coverUrl=state.Cover;Cover=null;generation++;LoadCover(state.Cover,generation);}
   if(!lyrics){lyricId="";lyricGeneration++;lines.Clear();lyricStatus="";return;}if(lyricId!=state.Id){lyricId=state.Id;lines.Clear();lyricStatus="正在载入歌词…";LoadLyrics(state.Id,++lyricGeneration);}
  }
  async void LoadLyrics(string song,int version){try{if(!Regex.IsMatch(song??"",@"^\d{1,20}$"))throw new Exception();string text=await http.GetStringAsync("https://music.163.com/api/song/lyric?id="+song+"&lv=-1&kv=-1&tv=-1");if(text.Length>512000)throw new Exception();var data=new JavaScriptSerializer{MaxJsonLength=512000}.DeserializeObject(text);string lrc=MusicBridge.Str(MusicBridge.Get(data,"lrc"),"lyric");if(version!=lyricGeneration||shutdown.IsCancellationRequested)return;lines=ParseLrc(lrc);lyricStatus=lines.Count>0?"♪":"这首歌暂无可用歌词";}catch{if(version==lyricGeneration&&!shutdown.IsCancellationRequested)lyricStatus="歌词暂不可用";}}
  async void LoadCover(string url,int version){try{Uri uri;if(!Uri.TryCreate(url,UriKind.Absolute,out uri)||!(uri.Scheme=="https"||uri.Scheme=="http")||!uri.Host.EndsWith(".music.126.net",StringComparison.OrdinalIgnoreCase))return;var secure=new UriBuilder(uri){Scheme="https",Port=-1,Query="param=128y128"};byte[] data;using(var response=await http.GetAsync(secure.Uri,HttpCompletionOption.ResponseHeadersRead,shutdown.Token)){response.EnsureSuccessStatusCode();if(response.Content.Headers.ContentLength>1024*1024)return;using(var source=await response.Content.ReadAsStreamAsync())using(var dest=new MemoryStream()){byte[] buffer=new byte[8192];int read;while((read=await source.ReadAsync(buffer,0,buffer.Length,shutdown.Token))>0){dest.Write(buffer,0,read);if(dest.Length>1024*1024)return;}data=dest.ToArray();}}
    if(version!=generation||shutdown.IsCancellationRequested)return;var image=new BitmapImage();using(var stream=new MemoryStream(data)){image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.DecodePixelWidth=128;image.StreamSource=stream;image.EndInit();image.Freeze();}Cover=image;
   }catch{}}
  public string Line(MusicState state){if(lyricId!=state.Id)return "";double position=state.Position+(state.Playing?Math.Min(1.2,Math.Max(0,(DateTime.UtcNow-state.At).TotalSeconds)):0);var line=lines.LastOrDefault(l=>l.Time<=position);return line==null?lyricStatus:line.Text;}
  public void Dispose(){shutdown.Cancel();generation++;lyricGeneration++;http.Dispose();}
 }
}
