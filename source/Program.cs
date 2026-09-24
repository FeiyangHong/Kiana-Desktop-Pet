using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
namespace KianaPet {
 public static class Program {
  [STAThread]public static int Main(string[] args){
   if(Array.IndexOf(args,"--music-launch")>=0)return MusicLauncher.Run();
   bool preview=Array.IndexOf(args,"--preview")>=0;string testRoot=null;for(int i=0;i<args.Length-1;i++)if(args[i]=="--state-dir")testRoot=args[i+1];if(testRoot!=null)Store.Root=Path.GetFullPath(testRoot);
   if(Array.IndexOf(args,"--bridge-check")>=0){using(Bridge bridge=new Bridge()){bridge.Poll(true).GetAwaiter().GetResult();Store.Atomic("bridge-check.json",bridge.Current);return bridge.Current.Connected?0:2;}}
   bool created;string suffix=preview?"Preview":System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
   using(Mutex single=new Mutex(true,"Local.KianaDesktopPet."+suffix,out created)){
    if(!created){try{Directory.CreateDirectory(Store.Root);File.WriteAllText(Path.Combine(Store.Root,"show.request"),"show");}catch{}return 0;}
    try{Application app=new Application();app.ShutdownMode=ShutdownMode.OnExplicitShutdown;
     app.DispatcherUnhandledException+=delegate(object sender,DispatcherUnhandledExceptionEventArgs e){Store.Log(e.Exception.ToString());MessageBox.Show("桌宠出现错误，详情已保存到运行数据目录中的 desktop.log。\n"+e.Exception.Message,"琪亚娜桌宠");e.Handled=true;app.Shutdown(1);};
     PetWindow pet=new PetWindow(preview);app.Run(pet);return 0;
    }catch(Exception e){Store.Log(e.ToString());MessageBox.Show("桌宠启动失败：\n"+e.Message,"琪亚娜桌宠");return 1;}
  }
 }
}
}
