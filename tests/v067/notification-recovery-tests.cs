using System;using System.IO;using System.Linq;using System.Reflection;using System.Collections.Generic;using System.Threading.Tasks;using System.Windows;using System.Windows.Controls;using System.Windows.Media;using KianaPet;
class NotificationRecoveryTests {
 static List<string> checks=new List<string>();static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks.Add(name);}
 static object Field(object o,string n){return o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(o);}static object Call(object o,string n,params object[] args){return o.GetType().GetMethod(n,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);}
 static BridgeState State(string state){var next=new BridgeState{Connected=true};Bridge.MergeMainNotifications(next,"{\"ready\":true,\"notices\":[{\"id\":\"task-1\",\"state\":\""+state+"\",\"title\":\"测试任务\"}]}");return next;}
 [STAThread]static int Main(string[] args){Store.Root=args[0];Store.Save(new Config{Walking=false,Gravity=false,LinkChatGPT=false,MusicEnabled=false,SleepSchedule=false,IdleSleep=false,HideFullscreen=false});var app=new Application{ShutdownMode=ShutdownMode.OnLastWindowClose};var pet=new PetWindow(true);bool passed=false;
 pet.Loaded+=async delegate{try{await Task.Delay(400);foreach(var w in app.Windows.OfType<SettingsWindow>().ToArray())w.Close();pet.Settings.LinkChatGPT=true;
 foreach(string state in new[]{"running","review","waiting","failed"}){pet.Link.Current=State(state);Call(pet,"UpdateToolbar",10000d);Check(((Button)Field(pet,"notificationButton")).Visibility==Visibility.Visible,"角标显示："+state);Check(((SolidColorBrush)((Border)Field(pet,"notificationBadge")).Background).Color==(Color)ColorConverter.ConvertFromString(pet.Link.Current.NotificationColor),"颜色同步："+state);}
 pet.Link.Current=State("running");Call(pet,"OpenToolbarNotifications");var menu=(ContextMenu)Field(pet,"toolbarMenu");Check(menu.IsOpen&&menu.Items.Count==1&&((MenuItem)menu.Items[0]).Header.ToString().Contains("测试任务"),"备用来源铃铛打开任务列表，不误点空的原生 Mini");menu.IsOpen=false;
 pet.Link.Current=new BridgeState{Connected=true};Call(pet,"UpdateToolbar",10001d);Check(((Button)Field(pet,"notificationButton")).Visibility==Visibility.Collapsed,"已读清空后隐藏铃铛，不伪造完成");
 var native=new BridgeState{CanNotifications=true,NotificationCount=2,NotificationColor="#30c85a"};Bridge.MergeMainNotifications(native,"{\"ready\":true,\"notices\":[{\"id\":\"task\",\"state\":\"running\"}]}");Check(native.NotificationSource=="mini"&&native.NotificationCount==2&&native.NotificationColor=="#30c85a","原生真实角标优先，避免双计数");
 Check(!Store.Json.Serialize(State("running")).Contains("测试任务"),"诊断状态文件不保存任务标题");bool rejected=false;try{Bridge.OpenMainNotificationExpression("';send()");}catch(ArgumentException){rejected=true;}Check(rejected,"跳转参数校验");
 var failed=new BridgeState{AppOpen=true};BridgeDiagnostics.Failure(failed,new Exception("Mini 状态查询超时"));Check(failed.Code=="timeout"&&!failed.Connected,"Mini 无回复不冒充已连接");passed=true;
 }catch(Exception e){Store.Atomic("error.json",new{error=e.ToString()});}finally{Store.Atomic("notification-recovery-tests.json",new{passed,checks});pet.Close();}};app.Run(pet);return passed?0:1;
 }
}
