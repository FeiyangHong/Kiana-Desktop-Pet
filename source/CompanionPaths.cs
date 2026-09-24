using System;
using System.IO;

namespace KianaPet {
 public static class CompanionPaths {
  public static string SmoothRoot(){
   string sibling=Path.Combine(Path.GetDirectoryName(Path.GetFullPath(Store.Root).TrimEnd(Path.DirectorySeparatorChar)),"KianaSmoothPet");
   if(File.Exists(Path.Combine(sibling,"app","scripts","launch.cmd")))return sibling;
   return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"Codex","PetTools","KianaSmoothPet");
  }
 }
}
