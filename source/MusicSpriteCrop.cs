using System;using System.Windows;using System.Windows.Media;using System.Windows.Media.Imaging;
namespace KianaPet {
 public static class MusicSpriteCrop {
  // Atlas cells can have transparent RGB colour or a neighbouring pose in their
  // rectangular padding. Keep this pose's connected silhouette and its original
  // anti-aliased alpha fringe; never interpret transparent RGB as visible colour.
  public static BitmapSource Extract(BitmapSource atlas,Int32Rect rect){
   var crop=new CroppedBitmap(atlas,rect);var converted=new FormatConvertedBitmap(crop,PixelFormats.Pbgra32,null,0);int w=rect.Width,h=rect.Height,n=w*h;var pixels=new byte[n*4];converted.CopyPixels(pixels,w*4,0);var labels=new int[n];var queue=new int[n];int label=0,best=0,bestCount=0;
   for(int start=0;start<n;start++){if(labels[start]!=0||pixels[start*4+3]<96)continue;label++;int read=0,write=1;queue[0]=start;labels[start]=label;while(read<write){int p=queue[read++],x=p%w,y=p/w;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int xx=x+dx,yy=y+dy;if(xx<0||xx>=w||yy<0||yy>=h)continue;int q=yy*w+xx;if(labels[q]==0&&pixels[q*4+3]>=96){labels[q]=label;queue[write++]=q;}}}if(write>bestCount){bestCount=write;best=label;}}
   if(bestCount<n/20)throw new System.IO.InvalidDataException("音乐动作没有完整主体");
   // A stool may be separate from the standing pose during the sit-down sequence.
   var sizes=new int[label+1];for(int p=0;p<n;p++)if(labels[p]>0)sizes[labels[p]]++;
   var keep=new bool[n];for(int p=0;p<n;p++){if(labels[p]==0||sizes[labels[p]]<bestCount*.03)continue;int x=p%w,y=p/w;for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++){int xx=x+dx,yy=y+dy;if(xx>=0&&xx<w&&yy>=0&&yy<h)keep[yy*w+xx]=true;}}
   for(int p=0;p<n;p++)if(!keep[p]){int at=p*4;pixels[at]=pixels[at+1]=pixels[at+2]=pixels[at+3]=0;}
   var result=BitmapSource.Create(w,h,96,96,PixelFormats.Pbgra32,null,pixels,w*4);result.Freeze();return result;
  }
 }
}
