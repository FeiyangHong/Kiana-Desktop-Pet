using System;
using System.Drawing;
namespace KianaPet {
 public static class MusicPlacement {
  public static Rectangle Place(Rectangle pet,Rectangle area,int width,int height,int gap,string mode,bool hasPosition,double savedX,double savedY){
   if(width>area.Width||height>area.Height)return Rectangle.Empty;
   Func<int,int,Rectangle> clamp=(x,y)=>new Rectangle((int)Rules.Clamp(x,area.Left,area.Right-width),(int)Rules.Clamp(y,area.Top,area.Bottom-height),width,height);
   int center=pet.Left+(pet.Width-width)/2,sideY=pet.Bottom-height;
   Rectangle below=clamp(center,pet.Bottom+gap),left=clamp(pet.Left-width-gap,sideY),right=clamp(pet.Right+gap,sideY),above=clamp(center,pet.Top-height-gap);
   Rectangle desired=mode=="free"&&hasPosition?clamp((int)Math.Round(savedX),(int)Math.Round(savedY)):mode=="left"?left:mode=="right"?right:below;
   Rectangle avoid=pet;avoid.Inflate(gap,gap);
   if(!desired.IntersectsWith(avoid))return desired;
   Rectangle[] choices=mode=="right"?new[]{right,left,below,above}:mode=="below"?new[]{below,left,right,above}:new[]{left,right,below,above};
   Rectangle best=Rectangle.Empty;double distance=double.MaxValue;
   foreach(var item in choices){if(item.IntersectsWith(avoid))continue;if(mode!="free")return item;double dx=item.X-desired.X,dy=item.Y-desired.Y,d=dx*dx+dy*dy;if(d<distance){best=item;distance=d;}}
   // A screen too small for both windows: hide the card rather than cover the toolbar.
   return best;
  }
  public static bool HoverVisible(bool hoverOnly,bool pointerOver,bool dragging,double now,ref double until){if(pointerOver||dragging)until=now+.85;return !hoverOnly||pointerOver||dragging||now<until;}
 }
}
