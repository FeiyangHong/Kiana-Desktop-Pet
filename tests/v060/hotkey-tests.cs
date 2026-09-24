using System;using System.IO;using System.Collections.Generic;using System.Windows.Interop;using KianaPet;
class HotkeyTests {
 static List<string> checks=new List<string>();
 static void Check(bool value,string name){if(!value)throw new Exception(name);checks.Add(name);Console.WriteLine("PASS "+name);}
 [STAThread]static int Main(string[] args){Store.Root=args[0];try{
  Config old=Store.Json.Deserialize<Config>("{\"Size\":192,\"Skin\":\"time-runner-kiana-round\"}");old.Validate();Check(!old.RecoverHotkeyEnabled&&old.Size==192,"旧版配置升级默认关闭快捷键，保留原有偏好");Check(old.RecoverHotkeyModifiers==3&&PetHotkey.Format(3,80)=="Ctrl + Alt + P","Ctrl/Alt/Shift 使用正确 Windows 标志，不再错绑 Ctrl+Shift+P");
  Check(!PetHotkey.Valid(4,80)&&!PetHotkey.Valid(0,80)&&!PetHotkey.Valid(3,0x7b)&&!PetHotkey.Valid(8,80),"拒绝纯 Shift、无修饰键、保留 F12 和未知修饰键");Check(PetHotkey.Valid(7,0x7a)&&PetHotkey.Valid(1,0x30),"支持叠加修饰键、数字和 F1–F11");
  old.RecoverHotkeyEnabled=true;old.RecoverHotkeyModifiers=4;old.Validate();Check(!old.RecoverHotkeyEnabled&&old.RecoverHotkeyModifiers==3,"无效配置安全降级为关闭");old.RecoverHotkeyModifiers=7;old.RecoverHotkeyKey=0x78;Store.Save(old);var loaded=Store.Load();Check(!loaded.RecoverHotkeyEnabled&&loaded.RecoverHotkeyKey==0x78&&loaded.RecoverHotkeyModifiers==7,"关闭时也能持久保存自定义组合键");
  using(var first=new HwndSource(new HwndSourceParameters("Kiana hotkey test A"){Width=1,Height=1,WindowStyle=0}))using(var second=new HwndSource(new HwndSourceParameters("Kiana hotkey test B"){Width=1,Height=1,WindowStyle=0}))using(var binding=new PetHotkey(first.Handle)){
   var available=new List<uint>();for(uint k=0x70;k<=0x7a&&available.Count<2;k++)if(Native.RegisterHotKey(second.Handle,2900,7|0x4000,k)){available.Add(k);Native.UnregisterHotKey(second.Handle,2900);}if(available.Count<2)throw new Exception("Not enough free test keys");uint a=available[0],b=available[1];
   Check(binding.Configure(false,3,80)&&!binding.Active,"关闭状态不注册全局快捷键");Check(binding.Configure(true,7,a)&&binding.Active,"真实 Windows 窗口注册成功");Check(!Native.RegisterHotKey(second.Handle,2900,7|0x4000,a),"Windows 确认组合键已由桌宠注册");
   Check(binding.Matches(new IntPtr(1729),new IntPtr((a<<16)|7))&&!binding.Matches(new IntPtr(1729),new IntPtr((b<<16)|7)),"仅接受当前键位的 WM_HOTKEY 消息");
   Check(binding.Configure(true,7,a),"重复保存同一组合键不误报冲突");
   if(!Native.RegisterHotKey(second.Handle,2901,7|0x4000,b))throw new Exception("Failed to reserve collision key");try{Check(!binding.Configure(true,7,b)&&binding.Active&&binding.Error.Contains("占用"),"检测真实全局冲突并保留旧绑定");Check(binding.Matches(new IntPtr(1729),new IntPtr((a<<16)|7)),"冲突后原快捷键仍有效");}finally{Native.UnregisterHotKey(second.Handle,2901);}
   Check(binding.Configure(true,7,b)&&binding.Matches(new IntPtr(1730),new IntPtr((b<<16)|7)),"更换键位立即生效且消息切换到新绑定");Check(Native.RegisterHotKey(second.Handle,2900,7|0x4000,a),"更换后立即释放旧组合键");Native.UnregisterHotKey(second.Handle,2900);
   Check(binding.Configure(false,0,0)&&!binding.Active,"即使草稿不完整也能立即关闭");Check(Native.RegisterHotKey(second.Handle,2900,7|0x4000,b),"关闭后原组合键已释放");Native.UnregisterHotKey(second.Handle,2900);Check(!binding.Matches(new IntPtr(1730),new IntPtr((b<<16)|7)),"关闭后不处理残留快捷键消息");
   binding.Configure(true,7,a);binding.Dispose();Check(Native.RegisterHotKey(second.Handle,2900,7|0x4000,a),"退出时注销快捷键");Native.UnregisterHotKey(second.Handle,2900);
  }
  Store.Atomic("hotkey-tests.json",new{version="0.3.5",passed=true,checks=checks});return 0;
 }catch(Exception e){Store.Atomic("hotkey-tests.json",new{passed=false,error=e.ToString(),checks=checks});Console.WriteLine(e);return 1;}}
}
