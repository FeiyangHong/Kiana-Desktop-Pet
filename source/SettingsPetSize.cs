using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace KianaPet {
 public sealed partial class SettingsWindow {
  Border appearanceImageSlot;
  Slider appearanceSkinSizeSlider,sharedSizeSlider;
  TextBlock appearanceSizeStatus,sharedSizeStatus;
  Button appearanceSaveSkinSize,appearanceInheritSize;
  string sizePreviewSkin;
  int savedPreviewSize;
  bool previewHasSizeOverride,syncingSizeSliders;

  static Slider PetSizeSlider(string name){
   var slider=new Slider{
    Minimum=PetSizeSettings.Minimum,Maximum=PetSizeSettings.Maximum,TickFrequency=1,SmallChange=1,LargeChange=8,
    IsSnapToTickEnabled=true,IsMoveToPointEnabled=true,Margin=new Thickness(0,8,5,8)
   };
   AutomationProperties.SetName(slider,name);
   return slider;
  }

  void BuildSkinSizeControl(Panel page){
   var controls=new StackPanel{Margin=new Thickness(0,2,0,6)};
   page.Children.Add(controls);
   TitleText(controls,"预览皮肤大小");
   appearanceSizeStatus=CompanionControls.Text("",12);
   controls.Children.Add(appearanceSizeStatus);
   appearanceSkinSizeSlider=PetSizeSlider("预览皮肤大小");
   controls.Children.Add(appearanceSkinSizeSlider);
   var actions=new WrapPanel();
   appearanceSaveSkinSize=Button("保存此皮肤大小",delegate{
    pet.SetSkinSize(AppearancePreviewSkinId,(int)appearanceSkinSizeSlider.Value);
    SyncAppearanceSize();
   },true);
   appearanceInheritSize=Button("恢复共用大小",delegate{
    if(previewHasSizeOverride)pet.SetSkinSize(AppearancePreviewSkinId,null);
    sizePreviewSkin=null;
    SyncAppearanceSize();
   },false);
   actions.Children.Add(appearanceSaveSkinSize);
   actions.Children.Add(appearanceInheritSize);
   controls.Children.Add(actions);
   appearanceSkinSizeSlider.ValueChanged+=delegate{
    if(syncingSizeSliders)return;
    UpdateSizeDraft();
    DrawAppearancePreview();
   };
  }

  void BuildSharedSizeControl(Panel page){
   TitleText(page,"所有皮肤的共用大小");
   sharedSizeStatus=CompanionControls.Text("",12);
   page.Children.Add(sharedSizeStatus);
   sharedSizeSlider=PetSizeSlider("所有皮肤的共用大小");
   page.Children.Add(sharedSizeSlider);
   sharedSizeSlider.ValueChanged+=delegate{
    if(syncingSizeSliders)return;
    pet.SetSharedSize((int)sharedSizeSlider.Value);
    SyncAppearanceSize();
   };
   SyncAppearanceSize();
  }

  void SyncAppearanceSize(){
   string skin=AppearancePreviewSkinId;
   if(appearanceSkinSizeSlider==null||string.IsNullOrEmpty(skin))return;
   int effective=PetSizeSettings.Effective(pet.Settings,skin);
   bool custom=pet.Settings.SkinSizes.ContainsKey(skin);
   syncingSizeSliders=true;
   try{
    if(sharedSizeSlider!=null){
     sharedSizeSlider.Value=pet.Settings.Size;
     sharedSizeStatus.Text=pet.Settings.Size+" 逻辑像素 · 调整后立即用于继承共用大小的皮肤";
    }
    // A routine status refresh must preserve the unsaved size being tried.
    if(skin!=sizePreviewSkin||savedPreviewSize!=effective||previewHasSizeOverride!=custom){
     sizePreviewSkin=skin;
     savedPreviewSize=effective;
     previewHasSizeOverride=custom;
     appearanceSkinSizeSlider.Value=effective;
    }
   }finally{syncingSizeSliders=false;}
   UpdateSizeDraft();
  }

  void UpdateSizeDraft(){
   int draft=(int)appearanceSkinSizeSlider.Value;
   bool changed=draft!=savedPreviewSize;
   string source=(previewHasSizeOverride?"专属大小 ":"继承共用大小 ")+savedPreviewSize;
   appearanceSizeStatus.Text=(changed?"试看 "+draft+" 逻辑像素 · ":"")+source+" 逻辑像素";
   appearanceSaveSkinSize.IsEnabled=changed||!previewHasSizeOverride;
   appearanceInheritSize.IsEnabled=changed||previewHasSizeOverride;
   appearancePreviewImage.Width=draft;
   appearancePreviewImage.Height=draft*208.0/192.0;
  }

  void ArrangeAppearanceImage(){
   if(appearancePreviewSurface==null||appearanceImageSlot==null)return;
   // The full size range gets the same drawing area. Dragging the slider cannot
   // move it under the pointer. On narrow windows the full-size image sits below it.
   bool stacked=appearancePreviewSurface.ActualWidth<PetSizeSettings.Maximum+266;
   DockPanel.SetDock(appearanceImageSlot,stacked?Dock.Bottom:Dock.Left);
   appearanceImageSlot.Margin=stacked?new Thickness(0,8,0,0):new Thickness(0,0,14,0);
  }
 }
}
