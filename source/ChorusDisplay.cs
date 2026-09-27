using System;using System.Linq;using System.Collections.Generic;
namespace KianaPet {
 public static class ChorusDisplay {
  public static string Time(double value){value=MusicReactions.Finite(value)?Math.Max(0,value):0;long seconds=(long)Math.Floor(value);return (seconds/60).ToString("00")+":"+(seconds%60).ToString("00");}
  public static string Describe(Config config,MusicState state,ChorusRange[] remote,string status,DateTime? acquired){
   if(!state.Connected)return "连接并播放网易云歌曲后显示副歌时间。";
   if(!MusicReactions.SongId(state.Id))return "当前歌曲没有可查询的网易云 ID。";
   var lines=new List<string>();var pref=MusicReactions.Preference(config,state.Id);
   bool manual=pref!=null&&MusicReactions.ValidRange(pref.Start,pref.End,state.Duration);
   if(manual)lines.Add("当前生效：手动标记 "+Time(pref.Start)+"–"+Time(pref.End));
   else if(pref!=null&&pref.Start>=0&&pref.End<0)lines.Add("手动起点 "+Time(pref.Start)+"，等待标记终点；暂不覆盖自动区间。");
   var ranges=(remote??new ChorusRange[0]).Where(r=>r!=null&&MusicReactions.ValidRange(r.Start,r.End,state.Duration)).OrderBy(r=>r.Start).ToArray();
   if(ranges.Length>0){lines.Add("来源：网易云接口（已缓存）"+(manual?" · 手动标记优先":!config.MusicChorusEnabled?" · 自动查询已关闭，以下区间未启用":" · 自动区间"));for(int i=0;i<ranges.Length;i++)lines.Add("第 "+(i+1)+" 段  "+Time(ranges[i].Start)+"–"+Time(ranges[i].End));if(acquired.HasValue)lines.Add("获取时间："+acquired.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));}
   else if(!config.MusicChorusEnabled)lines.Add("自动查询已关闭；手动标记仍可使用。");
   else if(remote!=null&&remote.Length>0)lines.Add("已获取的区间与当前歌曲时长不符，暂不使用。");
   else lines.Add(string.IsNullOrWhiteSpace(status)?"当前歌曲暂无可用副歌时间。":status);
   return string.Join("\n",lines);
  }
 }
}
