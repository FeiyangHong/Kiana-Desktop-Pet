using System;
using System.Runtime.InteropServices;
namespace KianaPet {
 public sealed class PetHotkey:IDisposable {
  public const uint Alt=1,Control=2,Shift=4;
  readonly IntPtr window;int activeId;uint activeModifiers,activeKey;
  public string Error {get;private set;}
  public bool Active {get{return activeId!=0;}}
  public string Status {get{return Active?"已启用："+Format(activeModifiers,activeKey)+" → 找回宠物":"已关闭，不占用任何全局快捷键";}}
  public PetHotkey(IntPtr handle){window=handle;Error="";}
  public static bool Valid(uint modifiers,uint key){return (modifiers&~7u)==0&&(modifiers&(Control|Alt))!=0&&((key>=0x41&&key<=0x5a)||(key>=0x30&&key<=0x39)||(key>=0x70&&key<=0x7a));}
  public static string KeyName(uint key){if(key>=0x70&&key<=0x7a)return "F"+(key-0x6f);return ((char)key).ToString();}
  public static string Format(uint modifiers,uint key){return ((modifiers&Control)!=0?"Ctrl + ":"")+((modifiers&Alt)!=0?"Alt + ":"")+((modifiers&Shift)!=0?"Shift + ":"")+KeyName(key);}
  public bool Configure(bool enabled,uint modifiers,uint key){
   Error="";if(!enabled){Release();return true;}
   if(!Valid(modifiers,key)){Error="请选择 Ctrl 或 Alt，搭配字母、数字或 F1–F11；Shift 可叠加。";return false;}
   if(Active&&modifiers==activeModifiers&&key==activeKey)return true;
   int nextId=activeId==1729?1730:1729;
   if(!Native.RegisterHotKey(window,nextId,modifiers|0x4000,key)){
    int code=Marshal.GetLastWin32Error();Error=code==1409?"这组快捷键已被其他程序占用，请换一组。":"无法注册这组快捷键（系统错误 "+code+"），请换一组。";return false;
   }
   Release();activeId=nextId;activeModifiers=modifiers;activeKey=key;return true;
  }
  public bool Matches(IntPtr id,IntPtr data){long value=data.ToInt64();return Active&&id.ToInt32()==activeId&&(value&0xffff)==activeModifiers&&((value>>16)&0xffff)==activeKey;}
  void Release(){if(Active)Native.UnregisterHotKey(window,activeId);activeId=0;activeModifiers=activeKey=0;}
  public void Dispose(){Release();}
 }
 public sealed partial class PetWindow {
  PetHotkey hotkey;
  public string HotkeyStatus {get{return hotkey==null?"快捷键尚未初始化":!hotkey.Active&&Settings.RecoverHotkeyEnabled&&!string.IsNullOrEmpty(hotkey.Error)?"未启用："+hotkey.Error:hotkey.Status;}}
  void InitializeHotkey(){hotkey=new PetHotkey(handle);if(!preview&&!hotkey.Configure(Settings.RecoverHotkeyEnabled,Settings.RecoverHotkeyModifiers,Settings.RecoverHotkeyKey))Store.Log("Hotkey: "+hotkey.Error);}
  public bool SetHotkey(bool enabled,uint modifiers,uint key,out string error){
   if(hotkey==null||preview){error="预览模式不注册全局快捷键。";return false;}
   if(!hotkey.Configure(enabled,modifiers,key)){error=hotkey.Error;return false;}
   Settings.RecoverHotkeyEnabled=enabled;Settings.RecoverHotkeyModifiers=modifiers;Settings.RecoverHotkeyKey=key;Save();error="";return true;
  }
 }
}
