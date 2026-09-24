using System;using System.Collections.Generic;using System.Linq;
namespace KianaPet {
 // Reserved in-process interface: no ports, file watchers or external integrations.
 public interface ICompanionTaskSink {void Publish(TaskNotice notice);}
 public sealed class TaskNotice {public string Source,TaskId,Title,Status,ActionId;public DateTime UpdatedAt;public bool Unread;}
 public sealed class TaskNoticeHub:ICompanionTaskSink {
  readonly object gate=new object();readonly List<TaskNotice> items=new List<TaskNotice>();
  readonly HashSet<string> muted=new HashSet<string>();readonly Dictionary<string,Action<TaskNotice>> actions=new Dictionary<string,Action<TaskNotice>>();
  static string Key(string source,string id){return source.Length+":"+source+id;}
  public event Action<TaskNotice> Changed;
  static TaskNotice Copy(TaskNotice n){return new TaskNotice{Source=n.Source,TaskId=n.TaskId,Title=n.Title,Status=n.Status,ActionId=n.ActionId,UpdatedAt=n.UpdatedAt,Unread=n.Unread};}
  public void Publish(TaskNotice n){
   if(n==null||string.IsNullOrWhiteSpace(n.Source)||string.IsNullOrWhiteSpace(n.TaskId)||string.IsNullOrWhiteSpace(n.Title)||n.Source.Length>60||n.TaskId.Length>128||n.Title.Length>160||!new[]{"running","succeeded","failed","waiting"}.Contains(n.Status))throw new ArgumentException("Invalid task notification");
   if(n.Source.Any(char.IsControl)||n.TaskId.Any(char.IsControl)||(n.ActionId!=null&&(string.IsNullOrWhiteSpace(n.ActionId)||n.ActionId.Length>80||n.ActionId.Any(char.IsControl))))throw new ArgumentException("Invalid notification identity");
   TaskNotice changed;lock(gate){var prior=items.FirstOrDefault(i=>i.Source==n.Source&&i.TaskId==n.TaskId);if(prior!=null&&prior.Status==n.Status&&prior.Title==n.Title&&prior.ActionId==n.ActionId)return;if(prior!=null)items.Remove(prior);changed=Copy(n);changed.Title=new string(changed.Title.Where(c=>!char.IsControl(c)).ToArray());changed.UpdatedAt=DateTime.UtcNow;changed.Unread=true;items.Insert(0,changed);if(items.Count>50)items.RemoveRange(50,items.Count-50);muted.RemoveWhere(k=>!items.Any(i=>Key(i.Source,i.TaskId)==k));}
   var handler=Changed;if(handler!=null)handler(Copy(changed));
  }
  public TaskNotice[] Snapshot(){lock(gate){items.RemoveAll(n=>DateTime.UtcNow-n.UpdatedAt>TimeSpan.FromHours(24));return items.Select(Copy).ToArray();}}
  public int Unread{get{return Snapshot().Count(n=>n.Unread);}}
  public void Read(string source,string taskId){lock(gate){var n=items.FirstOrDefault(i=>i.Source==source&&i.TaskId==taskId);if(n!=null)n.Unread=false;}}
  public void ReadAll(){lock(gate){foreach(var n in items)n.Unread=false;}}
  public bool IsMuted(TaskNotice notice){lock(gate)return muted.Contains(Key(notice.Source,notice.TaskId));}
  public void Mute(string source,string taskId,bool value){lock(gate){string key=Key(source,taskId);if(value){if(items.Any(i=>i.Source==source&&i.TaskId==taskId))muted.Add(key);}else muted.Remove(key);}}
  public void RegisterAction(string source,string actionId,Action<TaskNotice> action){if(string.IsNullOrWhiteSpace(source)||source.Length>60||source.Any(char.IsControl)||string.IsNullOrWhiteSpace(actionId)||actionId.Length>80||actionId.Any(char.IsControl)||action==null)throw new ArgumentException("Invalid task action");lock(gate){string key=Key(source,actionId);if(actions.Count>=100&&!actions.ContainsKey(key))throw new InvalidOperationException("Too many actions");actions[key]=action;}}
  public void UnregisterAction(string source,string actionId){lock(gate)actions.Remove(Key(source,actionId));}
  public bool CanOpen(TaskNotice notice){lock(gate)return !string.IsNullOrEmpty(notice.ActionId)&&actions.ContainsKey(Key(notice.Source,notice.ActionId));}
  public bool Open(TaskNotice notice){Action<TaskNotice> action;TaskNotice target;lock(gate){target=items.FirstOrDefault(i=>i.Source==notice.Source&&i.TaskId==notice.TaskId);if(target==null||string.IsNullOrEmpty(target.ActionId)||!actions.TryGetValue(Key(target.Source,target.ActionId),out action))return false;target=Copy(target);}action(target);lock(gate){var current=items.FirstOrDefault(i=>i.Source==target.Source&&i.TaskId==target.TaskId);if(current!=null&&current.UpdatedAt==target.UpdatedAt&&current.Status==target.Status&&current.Title==target.Title)current.Unread=false;}return true;}
  public static string StateName(string status){return status=="running"?"进行中":status=="succeeded"?"已完成":status=="failed"?"失败":"等待处理";}
 }
}
