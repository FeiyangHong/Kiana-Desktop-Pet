using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
namespace KianaPet {
 public static class Program {
  [STAThread]public static int Main(string[] args){
   if(Array.IndexOf(args,"--hook-event")>=0){int at=Array.IndexOf(args,"--hook-root");return at>=0&&at+1<args.Length?HookJournal.Receive(Path.GetFullPath(args[at+1])):0;}
   if(Array.IndexOf(args,"--music-launch")>=0)return MusicLauncher.Run();
   bool preview=Array.IndexOf(args,"--preview")>=0;string testRoot=null;for(int i=0;i<args.Length-1;i++)if(args[i]=="--state-dir")testRoot=args[i+1];if(testRoot!=null)Store.Root=Path.GetFullPath(testRoot);
   if(Array.IndexOf(args,"--install-hooks")>=0){try{HookSetup.Install();return 0;}catch(Exception e){Store.Atomic("hook-setup-error.json",new{error=e.Message});return 2;}}
   if(Array.IndexOf(args,"--connection-check")>=0){try{using(var bridge=new Bridge())using(var music=new MusicBridge()){bridge.Poll(true,false,"hooks").GetAwaiter().GetResult();music.Poll(true,"system").GetAwaiter().GetResult();var s=music.Current;Store.Atomic("connection-check.json",new{at=DateTime.UtcNow,chatgpt=bridge.Current,notices=bridge.Current.Notices,music=new{s.ConnectionSource,s.Connected,s.Playing,s.Title,s.Artist,s.Album,s.Id,s.Duration,s.HasPosition,s.HasDuration,s.MetadataSource,s.CanToggle,s.CanPrevious,s.CanNext,s.Label,hasCover=s.NativeCover!=null},error=music.NativeError});return s.Connected?0:2;}}catch(Exception e){Store.Atomic("connection-check.json",new{error=e.ToString()});return 2;}}
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
