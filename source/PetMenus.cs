using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
namespace KianaPet {
 static class PetMenus {
  static ResourceDictionary theme;static bool? dark;
  public static ContextMenu Create(){
   if(theme==null||dark!=PetPalette.Dark){dark=PetPalette.Dark;string x=Theme;if(PetPalette.Dark)x=x.Replace("#FCFCFD","#27262D").Replace("#DEDDE4","#48434F").Replace("#EEEBF5","#45404F").Replace("#EAE7F0","#48434F").Replace("#8066AE","#CDB5F2").Replace("#8D8798","#C0BACB");theme=(ResourceDictionary)XamlReader.Parse(x);}
   ContextMenu menu=new ContextMenu{StaysOpen=false,FontFamily=new FontFamily("Microsoft YaHei UI"),FontSize=13,Foreground=PetPalette.Brush(PetPalette.Ink),MinWidth=238,UseLayoutRounding=true,SnapsToDevicePixels=true};
   TextOptions.SetTextFormattingMode(menu,TextFormattingMode.Display);TextOptions.SetTextRenderingMode(menu,TextRenderingMode.ClearType);TextOptions.SetTextHintingMode(menu,TextHintingMode.Fixed);
   menu.Resources.MergedDictionaries.Add(theme);menu.Style=(Style)theme[typeof(ContextMenu)];return menu;
  }
  const string Theme=@"<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<Style TargetType='{x:Type ContextMenu}'>
 <Setter Property='OverridesDefaultStyle' Value='True'/><Setter Property='SnapsToDevicePixels' Value='True'/><Setter Property='UseLayoutRounding' Value='True'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type ContextMenu}'>
  <Grid Margin='7' UseLayoutRounding='True' SnapsToDevicePixels='True'>
   <Border Background='#FCFCFD' CornerRadius='12' IsHitTestVisible='False'><Border.Effect><DropShadowEffect BlurRadius='10' ShadowDepth='2' Opacity='0.12'/></Border.Effect></Border>
   <Border Background='#FCFCFD' BorderBrush='#DEDDE4' BorderThickness='1' CornerRadius='12' Padding='6' RenderOptions.ClearTypeHint='Enabled'>
    <ItemsPresenter KeyboardNavigation.DirectionalNavigation='Cycle' RenderOptions.ClearTypeHint='Enabled'/>
   </Border>
  </Grid>
 </ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='{x:Type MenuItem}'>
 <Setter Property='FontFamily' Value='Microsoft YaHei UI'/><Setter Property='FontSize' Value='13'/><Setter Property='FontWeight' Value='Normal'/>
 <Setter Property='OverridesDefaultStyle' Value='True'/><Setter Property='UseLayoutRounding' Value='True'/><Setter Property='SnapsToDevicePixels' Value='True'/><Setter Property='TextOptions.TextFormattingMode' Value='Display'/><Setter Property='TextOptions.TextRenderingMode' Value='ClearType'/><Setter Property='TextOptions.TextHintingMode' Value='Fixed'/><Setter Property='MinHeight' Value='33'/><Setter Property='Padding' Value='8,6'/><Setter Property='Background' Value='Transparent'/><Setter Property='HorizontalContentAlignment' Value='Stretch'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type MenuItem}'>
  <Grid>
   <Border x:Name='Row' Background='{TemplateBinding Background}' CornerRadius='7' Padding='{TemplateBinding Padding}'>
    <Grid><Grid.ColumnDefinitions><ColumnDefinition Width='21'/><ColumnDefinition Width='*'/><ColumnDefinition Width='17'/></Grid.ColumnDefinitions>
     <TextBlock x:Name='Check' Text='✓' Foreground='#8066AE' FontSize='13' Visibility='Collapsed' VerticalAlignment='Center'/>
     <ContentPresenter Grid.Column='1' ContentSource='Header' RecognizesAccessKey='True' VerticalAlignment='Center' Margin='0,0,10,0'/>
     <Path x:Name='Arrow' Grid.Column='2' Data='M 0,0 L 4,4 L 0,8' Stroke='#8D8798' StrokeThickness='1.2' HorizontalAlignment='Right' VerticalAlignment='Center' Visibility='Collapsed'/>
    </Grid>
   </Border>
   <Popup x:Name='PART_Popup' AllowsTransparency='True' PopupAnimation='None' Placement='Right' HorizontalOffset='-3' VerticalOffset='-7' PlacementTarget='{Binding RelativeSource={RelativeSource TemplatedParent}}' IsOpen='{Binding IsSubmenuOpen, RelativeSource={RelativeSource TemplatedParent}}' Focusable='False'>
    <Grid Margin='7' MinWidth='232' UseLayoutRounding='True' SnapsToDevicePixels='True' TextOptions.TextFormattingMode='Display' TextOptions.TextRenderingMode='ClearType' TextOptions.TextHintingMode='Fixed'>
     <Border Background='#FCFCFD' CornerRadius='12' IsHitTestVisible='False'><Border.Effect><DropShadowEffect BlurRadius='10' ShadowDepth='2' Opacity='0.12'/></Border.Effect></Border>
     <Border Background='#FCFCFD' BorderBrush='#DEDDE4' BorderThickness='1' CornerRadius='12' Padding='6' RenderOptions.ClearTypeHint='Enabled'>
      <ItemsPresenter KeyboardNavigation.DirectionalNavigation='Cycle' RenderOptions.ClearTypeHint='Enabled'/>
     </Border>
    </Grid>
   </Popup>
  </Grid>
  <ControlTemplate.Triggers>
   <Trigger Property='IsHighlighted' Value='True'><Setter TargetName='Row' Property='Background' Value='#EEEBF5'/></Trigger>
   <Trigger Property='IsChecked' Value='True'><Setter TargetName='Check' Property='Visibility' Value='Visible'/></Trigger>
   <Trigger Property='HasItems' Value='True'><Setter TargetName='Arrow' Property='Visibility' Value='Visible'/></Trigger>
   <Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.5'/></Trigger>
  </ControlTemplate.Triggers>
 </ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='{x:Type Separator}'><Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type Separator}'><Border Height='1' Background='#EAE7F0' Margin='10,5'/></ControlTemplate></Setter.Value></Setter></Style>
</ResourceDictionary>";
 }
 // This listener exists only while a pet menu is open. It never blocks mouse events or records input.
 sealed class MenuOutsideClick:IDisposable {
  [StructLayout(LayoutKind.Sequential)]struct MouseData {public Native.Point Point;public uint Mouse,Flags,Time;public UIntPtr Extra;}
  delegate IntPtr Callback(int code,IntPtr message,IntPtr data);
  [DllImport("user32.dll")]static extern IntPtr SetWindowsHookEx(int id,Callback callback,IntPtr module,uint thread);
  [DllImport("user32.dll")]static extern bool UnhookWindowsHookEx(IntPtr hook);
  [DllImport("user32.dll")]static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr GetModuleHandle(string name);
  readonly ContextMenu menu;readonly Callback callback;IntPtr hook;bool disposed;
  public MenuOutsideClick(ContextMenu value){menu=value;callback=Observe;hook=SetWindowsHookEx(14,callback,GetModuleHandle(null),0);if(hook==IntPtr.Zero)Store.Log("Menu outside-click listener unavailable; WPF dismissal remains active.");}
  IntPtr Observe(int code,IntPtr message,IntPtr data){
   int msg=message.ToInt32();if(code>=0&&(msg==0x201||msg==0x204||msg==0x207||msg==0x20B)){
    MouseData info=(MouseData)Marshal.PtrToStructure(data,typeof(MouseData));Point point=new Point(info.Point.X,info.Point.Y);
    menu.Dispatcher.BeginInvoke(DispatcherPriority.Input,new Action(delegate{if(!disposed&&menu.IsOpen&&!ContainsMenu(menu,point))menu.IsOpen=false;}));
   }
   return CallNextHookEx(hook,code,message,data);
  }
  static bool ContainsElement(FrameworkElement element,Point point){if(element==null||!element.IsVisible)return false;try{return new Rect(element.PointToScreen(new Point(0,0)),element.PointToScreen(new Point(element.ActualWidth,element.ActualHeight))).Contains(point);}catch(InvalidOperationException){return false;}}
  internal static bool ContainsMenu(ItemsControl menu,Point point){
   if(ContainsElement(menu,point))return true;
   foreach(object value in menu.Items){MenuItem item=value as MenuItem;if(item==null||!item.IsSubmenuOpen)continue;Popup popup=item.Template.FindName("PART_Popup",item) as Popup;if(popup!=null&&ContainsElement(popup.Child as FrameworkElement,point))return true;if(ContainsMenu(item,point))return true;}
   return false;
  }
  public void Dispose(){if(disposed)return;disposed=true;if(hook!=IntPtr.Zero){UnhookWindowsHookEx(hook);hook=IntPtr.Zero;}GC.KeepAlive(callback);}
 }
 public sealed partial class PetWindow {
  MenuOutsideClick menuOutsideClick;
  void ShowPetMenu(ContextMenu menu){
   ClosePetMenu();toolbarMenu=menu;Native.SetMenuActivation(handle,true);Activate();Native.FocusPetForMenu(handle);
   MenuOutsideClick listener=null;bool cleaned=false;
   var descriptor=System.ComponentModel.DependencyPropertyDescriptor.FromProperty(ContextMenu.IsOpenProperty,typeof(ContextMenu));EventHandler changed=null;
   Action cleanup=delegate{if(cleaned)return;cleaned=true;descriptor.RemoveValueChanged(menu,changed);if(listener!=null)listener.Dispose();if(menuOutsideClick==listener)menuOutsideClick=null;if(toolbarMenu==menu){toolbarMenu=null;Native.SetMenuActivation(handle,false);toolbarUntil=clock.Elapsed.TotalSeconds+.3;}};
   changed=delegate{if(!menu.IsOpen)cleanup();};descriptor.AddValueChanged(menu,changed);
   menu.Opened+=delegate{if(cleaned||!menu.IsOpen)return;listener=new MenuOutsideClick(menu);menuOutsideClick=listener;menu.Focus();};
   menu.Closed+=delegate{cleanup();};
   menu.PreviewKeyDown+=delegate(object sender,KeyEventArgs e){if(e.Key==Key.Escape){menu.IsOpen=false;e.Handled=true;}};
   Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(menu,delegate{menu.IsOpen=false;});menu.IsOpen=true;
  }
  void ClosePetMenu(){if(toolbarMenu!=null)toolbarMenu.IsOpen=false;if(menuOutsideClick!=null){menuOutsideClick.Dispose();menuOutsideClick=null;}}
 }
}
