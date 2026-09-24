using System;using System.Net.Http;
namespace KianaPet {
 public static class BridgeDiagnostics {
  public static string ChannelName(string code){switch(code){case "ready":return "正常";case "closed":return "已关闭";case "preparing":return "正在准备";case "unsynced":return "未同步通知";case "no-response":return "未响应";case "unavailable":return "暂不可用";default:return "待检测";}}
  public static void Set(BridgeState state,string code,string label,string hint){state.Code=code;state.Label=label;state.Hint=hint;}
  public static void Failure(BridgeState state,Exception error){
   state.Connected=state.CanChat=state.CanVoice=state.CanNotifications=false;state.NotificationCount=0;state.NotificationText=state.NotificationColor="";state.State="idle";string message=error.Message??"";state.MiniStatus=message.StartsWith("Mini 后台")?"closed":message=="Mini 状态查询超时"?"no-response":"unavailable";
   if(!state.AppOpen){Set(state,"app-closed","ChatGPT 未运行 · 独立陪伴","需要联动时，通过 ChatGPT 平滑桌宠启动器打开应用。");return;}
   if(error is OperationCanceledException||message=="Mini 状态查询超时"){Set(state,"timeout","连接暂时超时 · 将自动重试","无需退出桌宠；可以点击“检查并修复联动”立即重试。");return;}
   if(message.StartsWith("请先使用")){Set(state,"launcher-missing","缺少平滑启动器会话","点击“检查并修复联动”或“打开 ChatGPT”；按启动窗口提示操作。");return;}
   if(message=="Browser changed"){Set(state,"session-stale","ChatGPT 会话已变化","通过平滑启动器重新建立会话后会自动恢复；桌宠不会强制退出 ChatGPT。");return;}
   if(message.StartsWith("Endpoint unavailable")||error is HttpRequestException){Set(state,"endpoint-unavailable","ChatGPT 已打开 · 本机联动端口不可用","可能是普通方式启动，或启动器会话已失效。使用平滑启动器重新连接。");return;}
   if(message.StartsWith("Mini 后台")||message.StartsWith("请显示")){Set(state,"mini-missing","Mini 后台尚未准备好","在 ChatGPT 设置中显示 Mini 一次，再重新检测。后台模式会自动收起原生外观。");return;}
   if(message=="No states"||message=="Mini 接口执行失败"){Set(state,"interface-changed","Mini 界面暂时无法识别","稍后重新检测；若持续出现，可能需要适配新的 ChatGPT 版本。");return;}
   if(message=="Untrusted page"){Set(state,"endpoint-rejected","联动页面身份校验失败","请通过平滑启动器重新连接。不会使用未经校验的页面。");return;}
   Set(state,"temporary-error","联动暂时不可用 · 将自动重试","可先继续独立陪伴，或点击“检查并修复联动”。详细错误已记入本机日志。");
  }
 }
}
