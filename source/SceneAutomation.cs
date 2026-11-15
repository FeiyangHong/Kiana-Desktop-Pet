using System;using System.Linq;using System.Reflection;
namespace KianaPet {
 public sealed class AutoSceneRule {public string Trigger="music",Scene="休闲",Process="",Start="09:00",End="18:00";}
 public sealed class SceneAutomation {
  ScenePreset baseline,applied;string current="",candidate="";double since;public bool Paused;public string Active{get{return current;}}
  static readonly FieldInfo[] fields=typeof(ScenePreset).GetFields().Where(f=>f.Name!="Name").ToArray();
  static bool Equal(object a,object b){return Store.Json.Serialize(a)==Store.Json.Serialize(b);}
  static void Restore(Config c,ScenePreset before,ScenePreset overlay){foreach(var f in fields){var target=typeof(Config).GetField(f.Name);if(target!=null&&Equal(target.GetValue(c),f.GetValue(overlay)))target.SetValue(c,f.GetValue(before));}}
  public Config Persistent(Config c){var result=PreferenceTransfer.Copy(c);if(baseline!=null)Restore(result,baseline,applied);return result;}
  public void Clear(Config c){if(baseline!=null)Restore(c,baseline,applied);baseline=applied=null;current=candidate="";}
  public void Pause(Config c){Clear(c);Paused=true;}
  public void Resume(){Paused=false;candidate="";}
  public static string Match(Config c,string foreground,bool playing,bool battery,TimeSpan time){foreach(var r in c.AutoSceneRules){bool match=r.Trigger=="music"?playing:r.Trigger=="battery"?battery:r.Trigger=="foreground"?!string.IsNullOrWhiteSpace(r.Process)&&string.Equals(foreground,r.Process,StringComparison.OrdinalIgnoreCase):r.Trigger=="time"&&Rules.IsSleepTime(time,TimeSpan.Parse(r.Start),TimeSpan.Parse(r.End));if(match&&c.Scenes.Any(s=>s.Name==r.Scene&&(r.Trigger!="music"||s.MusicEnabled)))return r.Scene;}return "";}
  public bool Tick(Config c,string target,double now){if(!c.AutoScenesEnabled||Paused){if(baseline==null)return false;Clear(c);return true;}if(target!=candidate){candidate=target;since=now;return false;}if(now-since<2||target==current)return false;ClearKeepCandidate(c,target,now);if(target.Length>0){var preset=c.Scenes.FirstOrDefault(s=>s.Name==target);if(preset==null)return false;baseline=ScenePreset.Capture("自动前",c);preset.Apply(c);applied=ScenePreset.Capture(target,c);current=target;}return true;}
  void ClearKeepCandidate(Config c,string target,double now){Clear(c);candidate=target;since=now;}
 }
}
