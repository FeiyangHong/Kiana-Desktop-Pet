using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Windows.Media;using System.Windows.Media.Imaging;
namespace KianaPet {
 public sealed class FrameAlignment {public double Scale=1,OffsetX,OffsetY;}
 public sealed class SkinAlignment {public string Skin;public double ReferenceFaceWidth,ReferenceFoot;public FrameAlignment[] Extra,Ambient,Transition;}
 public static class SpriteGeometry {
  public static Matrix Matrix(FrameAlignment item,double width){double unit=width/384d;return item==null?System.Windows.Media.Matrix.Identity:new Matrix(item.Scale,0,0,item.Scale,item.OffsetX*unit,item.OffsetY*unit);}
  public static bool Valid(FrameAlignment a){return a!=null&&!double.IsNaN(a.Scale)&&a.Scale>=.5&&a.Scale<=1.3&&!double.IsNaN(a.OffsetX)&&Math.Abs(a.OffsetX)<200&&!double.IsNaN(a.OffsetY)&&Math.Abs(a.OffsetY)<200;}
 }
 public sealed partial class PetWindow {
  Dictionary<string,SkinAlignment> spriteAlignment;FrameAlignment activeAlignment;
  void ShowSprite(BitmapSource source,string group,int index){if(spriteAlignment==null){spriteAlignment=new Dictionary<string,SkinAlignment>();try{string file=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","sprite-alignment.json");foreach(var skin in Store.Json.Deserialize<SkinAlignment[]>(File.ReadAllText(file))){if(skin.Extra.Length!=6||skin.Ambient.Length!=4||skin.Transition.Length!=4||!skin.Extra.Concat(skin.Ambient).Concat(skin.Transition).All(SpriteGeometry.Valid))continue;spriteAlignment[skin.Skin]=skin;}}catch(Exception e){Store.Log("Sprite alignment: "+e.Message);}}
   activeAlignment=null;SkinAlignment match;if(group!="base"&&group!="music"&&spriteAlignment.TryGetValue(Settings.Skin,out match)){var list=group=="extra"?match.Extra:group=="ambient"?match.Ambient:match.Transition;if(index>=0&&index<list.Length)activeAlignment=list[index];}sprite.Source=source;ApplySpriteAlignment();
  }
  void ApplySpriteAlignment(){sprite.RenderTransform=new MatrixTransform(SpriteGeometry.Matrix(activeAlignment,Settings.Size));}
 }
}
