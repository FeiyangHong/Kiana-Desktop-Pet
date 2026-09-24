using System;using System.IO;using System.Diagnostics;using System.Windows;using System.Windows.Controls;
namespace KianaPet {
 public sealed class RepositoryBinding {public string RepositoryRoot,InstallRoot;}
 public static class RepositoryUpdate {
  public static bool Valid(string root){try{return !string.IsNullOrWhiteSpace(root)&&(Directory.Exists(Path.Combine(root,".git"))||File.Exists(Path.Combine(root,".git")))&&File.Exists(Path.Combine(root,"scripts","Repository-Bootstrap.ps1"))&&File.Exists(Path.Combine(root,"source","Program.cs"));}catch{return false;}}
  public static string Find(string stateRoot){
   try{string file=Path.Combine(stateRoot,"repository.json");if(File.Exists(file)){var binding=Store.Json.Deserialize<RepositoryBinding>(File.ReadAllText(file));if(binding!=null&&Valid(binding.RepositoryRoot))return Path.GetFullPath(binding.RepositoryRoot);}}catch{}
   for(var dir=new DirectoryInfo(Path.GetFullPath(stateRoot));dir!=null;dir=dir.Parent)if(Valid(dir.FullName))return dir.FullName;
   return null;
  }
  public static void Bind(string root,string stateRoot){if(!Valid(root))throw new Exception("请选择完整的 Kiana-Desktop-Pet Git 仓库，并先拉取包含更新脚本的版本。");Maintenance.Write(Path.Combine(stateRoot,"repository.json"),new RepositoryBinding{RepositoryRoot=Path.GetFullPath(root),InstallRoot=Path.GetFullPath(stateRoot)});}
  public static ProcessStartInfo Command(string root,string stateRoot,bool pull){
   if(!Valid(root))throw new Exception("仓库未找到，请先选择本机仓库目录。");
   return new ProcessStartInfo{FileName=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),@"WindowsPowerShell\v1.0\powershell.exe"),Arguments="-NoLogo -NoProfile -ExecutionPolicy Bypass -File \""+Path.Combine(root,"scripts","Repository-Bootstrap.ps1")+"\" -Mode "+(pull?"update":"build")+" -InstallRoot \""+Path.GetFullPath(stateRoot).TrimEnd('\\')+"\" -KeepOpen",WorkingDirectory=root,UseShellExecute=true,WindowStyle=ProcessWindowStyle.Normal};
  }
 }
 public sealed partial class SettingsWindow {
  TextBlock repositoryLocation;Button repositoryPull,repositoryBuild;
  void BuildRepositorySettings(Panel page){
   TitleText(page,"仓库更新");repositoryLocation=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,8)};page.Children.Add(repositoryLocation);
   repositoryPull=Button("拉取 GitHub 更新并应用",delegate{RunRepositoryUpdate(true);},true);repositoryBuild=Button("构建并运行本机修改",delegate{RunRepositoryUpdate(false);},false);page.Children.Add(repositoryPull);page.Children.Add(repositoryBuild);
   page.Children.Add(Button("选择本机仓库目录…",delegate{using(var picker=new System.Windows.Forms.FolderBrowserDialog{Description="选择 Kiana-Desktop-Pet Git 仓库",SelectedPath=RepositoryUpdate.Find(Store.Root)??"",ShowNewFolderButton=false}){if(picker.ShowDialog()!=System.Windows.Forms.DialogResult.OK)return;try{RepositoryUpdate.Bind(picker.SelectedPath,Store.Root);RefreshRepositorySettings();}catch(Exception e){MessageBox.Show(this,e.Message,"仓库目录无效");}}},false));
   Note(page,"拉取更新：仅快进拉取，有未提交修改或历史分叉时停止。构建本机修改：不拉取，直接构建当前源码。两者都会先构建和校验，再保存偏好、备份并重启桌宠；不会提交或推送代码。进度窗口会保留结果，重复运行会被拦截。仓库位置仅保存在本机。");RefreshRepositorySettings();
  }
  void RefreshRepositorySettings(){string root=RepositoryUpdate.Find(Store.Root);repositoryLocation.Text=(root==null?"尚未选择本机 Git 仓库。":"源码仓库："+root)+"\n运行目录："+Store.Root;repositoryPull.IsEnabled=repositoryBuild.IsEnabled=root!=null;}
  void RunRepositoryUpdate(bool pull){try{string root=RepositoryUpdate.Find(Store.Root);RepositoryUpdate.Bind(root,Store.Root);Process.Start(RepositoryUpdate.Command(root,Store.Root,pull));}catch(Exception e){MessageBox.Show(this,e.Message,"更新未开始");}}
 }
}
