using System;using System.Linq;
namespace KianaPet {
 public static class SkinShuffleSafety {
  public static bool Fresh(Config c,BridgeState link,DateTime utc){return c.LinkChatGPT&&link!=null&&link.Connected&&!link.Stale&&utc-link.At<TimeSpan.FromSeconds(10);}
  public static string GPTBlockReason(Config c,BridgeState link,DateTime utc){
   if(!Fresh(c,link,utc))return "";
   // A completed aggregate must not hide another genuinely active task.
   var states=(link.Notices??new BridgeNotice[0]).Where(n=>n!=null).Select(n=>n.State).Concat(new[]{link.State}).ToArray();
   if(states.Contains("running"))return "GPT 正在处理任务";
   if(states.Contains("waiting"))return "GPT 有任务等待你的处理";
   if(states.Contains("failed"))return "GPT 有失败状态待处理";
   if(states.Contains("unknown"))return "GPT 任务状态待确认";
   return link.State=="idle"||link.State=="review"?"":"GPT 当前状态尚未空闲";
  }
  public static bool ReviewPoseAllowed(Config c,BridgeState link,DateTime utc){return Fresh(c,link,utc)&&link.State=="review"&&GPTBlockReason(c,link,utc).Length==0;}
 }
}
