using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KianaPet {
 public static class PetCatalog {
  static readonly Lazy<List<PetInfo>> installed = new Lazy<List<PetInfo>>(Read);

  static List<PetInfo> Read() {
   string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "pets.json");
   // Settings transfer and the standalone hook receiver can run without the artwork directory.
   using (var stream = File.Exists(path) ? File.OpenRead(path) :
        typeof(PetCatalog).Assembly.GetManifestResourceStream("KianaPet.pets.json")) {
    if (stream == null) throw new FileNotFoundException("找不到桌宠皮肤清单。", path);
    using (var reader = new StreamReader(stream)) return Parse(reader.ReadToEnd());
   }
  }

  static List<PetInfo> Parse(string json) {
   var skins = Store.Json.Deserialize<List<PetInfo>>(json);
   var ids = new HashSet<string>(StringComparer.Ordinal);
   if (skins == null || skins.Count == 0 || skins.Any(s => s == null ||
       string.IsNullOrWhiteSpace(s.Id) || string.IsNullOrWhiteSpace(s.Name) || !ids.Add(s.Id))) {
    throw new InvalidDataException("皮肤清单缺少名称或包含重复编号。请重新安装完整的桌宠包。");
   }
   return skins;
  }

  public static List<PetInfo> Load() { return new List<PetInfo>(installed.Value); }

  public static string Name(string id) {
   var skin = installed.Value.FirstOrDefault(s => s.Id == id);
   return skin == null ? id : skin.Name;
  }
 }
}
