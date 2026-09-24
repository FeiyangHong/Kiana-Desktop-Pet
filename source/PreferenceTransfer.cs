using System;using System.IO;using System.Text;using System.Collections.Generic;using System.Linq;
namespace KianaPet {
 public sealed class PreferencePackage {public int Schema=1;public string Product="KianaDesktopPet";public string Version=Maintenance.Version;public string Scope="all";public DateTime? ExportedAt;public string[] IncludedKeys;public Config Preferences;}
 public static class PreferenceTransfer {
  public static Config Copy(Config c){return Store.Json.Deserialize<Config>(Store.Json.Serialize(c));}
  public static void WithoutPosition(Config c){c.X=c.Y=-1;c.HasPosition=false;c.MusicX=c.MusicY=0;c.MusicHasPosition=false;if(c.MusicPosition=="free")c.MusicPosition="below";}
  public static string Export(Config config,bool positions){var c=Copy(config);var defaults=new Config();if(!positions){foreach(var f in typeof(Config).GetFields().Where(f=>PreferenceReview.Local(f.Name)))f.SetValue(c,f.GetValue(defaults));WithoutPosition(c);}return Store.Json.Serialize(new PreferencePackage{Preferences=c,Scope=positions?"all":"common",ExportedAt=DateTime.UtcNow,IncludedKeys=typeof(Config).GetFields().Where(f=>f.Name!="Schema"&&(positions||!PreferenceReview.Local(f.Name))).Select(f=>f.Name).ToArray()});}
  public static Config Import(string text){
   if(text==null||text.Length>65536)throw new Exception("设置文件过大或为空。");
   var root=Store.Json.DeserializeObject(text) as Dictionary<string,object>;if(root==null)throw new Exception("不是有效的设置文件。");Config config;
   if(root.ContainsKey("Product")){var p=Store.Json.Deserialize<PreferencePackage>(text);if(p.Product!="KianaDesktopPet"||p.Schema!=1||p.Preferences==null)throw new Exception("设置包类型或版本不支持。");config=p.Preferences;}
   else{if(!root.ContainsKey("Schema")||!root.ContainsKey("Skin")||!root.ContainsKey("Size"))throw new Exception("请选择桌宠导出的设置包或 settings.json。");config=Store.Json.Deserialize<Config>(text);}
   if(config.Schema!=1)throw new Exception("这份设置需要更新的桌宠版本。");config.Validate();return config;
  }
  public static string Backup(){string folder=Path.Combine(Store.Root,"preference-backups");Directory.CreateDirectory(folder);string path=Path.Combine(folder,"settings-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,6)+".json");File.WriteAllText(path,Export(Store.Load(),true),new UTF8Encoding(false));return path;}
 }
}
