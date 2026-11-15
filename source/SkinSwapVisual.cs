using System;using System.Windows;using System.Windows.Controls;using System.Windows.Media;using System.Windows.Media.Animation;using System.Windows.Media.Imaging;
namespace KianaPet {
 public sealed partial class PetWindow {
  readonly Grid skinVisual=new Grid(),skinLiveVisual=new Grid();
  readonly Image skinOldVisual=new Image{IsHitTestVisible=false,Visibility=Visibility.Hidden,Stretch=Stretch.Fill};
  int skinVisualToken;bool skinVisualRunning,skinVisualAutomatic;string skinVisualExpectedState="";
  void BuildSkinVisual(Grid root){
   skinLiveVisual.Children.Add(sprite);skinVisual.Children.Add(skinLiveVisual);skinVisual.Children.Add(skinOldVisual);Grid.SetRow(skinVisual,1);root.Children.Add(skinVisual);
   RenderOptions.SetBitmapScalingMode(skinOldVisual,BitmapScalingMode.HighQuality);
   skinVisual.IsVisibleChanged+=delegate{if(!skinVisual.IsVisible)StopSkinVisual();};
  }
  BitmapSource CaptureSkinVisual(){
   if(!Settings.SmoothTransitions||!MotionSettings.Enabled||sprite.Source==null||!IsVisible||manualHidden||fullscreenHidden||sessionLocked)return null;
   double width=skinVisual.ActualWidth,height=skinVisual.ActualHeight,dpi=DpiScale;
   if(width<1||height<1)return null;
   // Capture the current composite, including an interrupted previous fade.
   // The ghost owns only this small frozen bitmap, never a second live pet.
   try{var drawing=new DrawingVisual();using(var dc=drawing.RenderOpen()){
    // VisualBrush coordinates include the visual's parent offset (the 58-DIP bubble row).
    var rect=new Rect(0,0,width,height);var offset=VisualTreeHelper.GetOffset(skinVisual);var brush=new VisualBrush(skinVisual){ViewboxUnits=BrushMappingMode.Absolute,Viewbox=new Rect(offset.X,offset.Y,width,height),ViewportUnits=BrushMappingMode.Absolute,Viewport=rect,Stretch=Stretch.Fill};dc.DrawRectangle(brush,null,rect);
   }var bitmap=new RenderTargetBitmap(Math.Max(1,(int)Math.Ceiling(width*dpi)),Math.Max(1,(int)Math.Ceiling(height*dpi)),96*dpi,96*dpi,PixelFormats.Pbgra32);bitmap.Render(drawing);bitmap.Freeze();return bitmap;}catch(Exception e){Store.Log("Skin visual capture: "+e.Message);return null;}
  }
  void PlaySkinVisual(BitmapSource old,string expectedState,bool automatic){StartSkinFade(old,expectedState,automatic,false);}
  void StartSkinFade(BitmapSource old,string expectedState,bool automatic,bool keepShowcase){
   if(keepShowcase)ClearSkinFade();else StopSkinVisual();if(old==null||!Settings.SmoothTransitions||!MotionSettings.Enabled||!IsVisible){if(automatic)ShowOutfitFeedback(!keepShowcase);return;}
   int token=skinVisualToken;skinVisualRunning=true;skinVisualAutomatic=automatic;skinVisualExpectedState=expectedState;
   skinOldVisual.Source=old;skinOldVisual.Visibility=Visibility.Visible;skinOldVisual.Opacity=1;skinLiveVisual.Opacity=0;
   double duration=keepShowcase?Math.Min(300,skinShowcaseActionMs*.5):300;
   var outgoing=new DoubleAnimation(1,0,TimeSpan.FromMilliseconds(duration*.4)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseIn}};
   outgoing.Completed+=delegate{if(token!=skinVisualToken)return;skinOldVisual.BeginAnimation(OpacityProperty,null);skinOldVisual.Opacity=0;skinOldVisual.Visibility=Visibility.Hidden;skinOldVisual.Source=null;};
   var incoming=new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(duration*.6)){BeginTime=TimeSpan.FromMilliseconds(duration*.4),EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut}};
   incoming.Completed+=delegate{if(token!=skinVisualToken)return;if(keepShowcase)ClearSkinFade();else StopSkinVisual();if(automatic)ShowOutfitFeedback(!keepShowcase);};
   skinOldVisual.BeginAnimation(OpacityProperty,outgoing);skinLiveVisual.BeginAnimation(OpacityProperty,incoming);
  }
  void StopSkinVisual(){StopSkinShowcase();ClearSkinFade();}
  void ClearSkinFade(){
   skinVisualToken++;skinVisualRunning=false;skinVisualExpectedState="";
   skinOldVisual.BeginAnimation(OpacityProperty,null);skinLiveVisual.BeginAnimation(OpacityProperty,null);
   skinOldVisual.Source=null;skinOldVisual.Visibility=Visibility.Hidden;skinOldVisual.Opacity=0;skinLiveVisual.Opacity=1;
  }
  void SkinVisualActionChanged(string next){if((skinVisualRunning&&next!=skinVisualExpectedState)||(skinShowcaseRunning&&next!=skinShowcaseExpectedState))StopSkinVisual();}
  void MaintainSkinVisual(){
   if(!skinVisualRunning&&!skinShowcaseRunning)return;
   if((skinVisualRunning&&!Settings.SmoothTransitions)||!MotionSettings.Enabled||!IsVisible||manualHidden||fullscreenHidden||sessionLocked||pressed||dragging||menuLayerYielding||(skinVisualAutomatic&&!SkinChangeActions.TaskPose(skinVisualExpectedState)&&SkinShuffleSafety.GPTBlockReason(Settings,Link.Current,DateTime.UtcNow).Length>0)||(skinShowcaseRunning&&SkinFeedbackBlocked()))StopSkinVisual();
  }
 }
}
