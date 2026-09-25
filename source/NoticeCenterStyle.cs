using System.Windows;using System.Windows.Controls;using System.Windows.Media;using System.Windows.Markup;
namespace KianaPet {
 // Scoped to the notification center; native keyboard and selection behavior stay on ComboBox.
 public static class NoticeCenterStyle {
  public static void Install(FrameworkElement root){
   var dictionary=(ResourceDictionary)XamlReader.Parse(@"<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
<Style TargetType='{x:Type ComboBoxItem}'>
 <Setter Property='Foreground' Value='{DynamicResource NoticeInk}'/><Setter Property='Padding' Value='10,7'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type ComboBoxItem}'>
  <Border x:Name='surface' CornerRadius='6' Background='Transparent' Padding='{TemplateBinding Padding}'><ContentPresenter/></Border>
  <ControlTemplate.Triggers><Trigger Property='IsHighlighted' Value='True'><Setter TargetName='surface' Property='Background' Value='{DynamicResource NoticeHover}'/></Trigger><Trigger Property='IsSelected' Value='True'><Setter Property='FontWeight' Value='SemiBold'/></Trigger></ControlTemplate.Triggers>
 </ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='{x:Type ComboBox}'>
 <Setter Property='Foreground' Value='{DynamicResource NoticeInk}'/><Setter Property='FontSize' Value='12'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type ComboBox}'>
  <Grid>
   <ToggleButton x:Name='toggle' Focusable='False' ClickMode='Press' IsChecked='{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}'>
    <ToggleButton.Template><ControlTemplate TargetType='{x:Type ToggleButton}'>
     <Border x:Name='outline' CornerRadius='8' Background='{DynamicResource NoticeSurface}' BorderBrush='{DynamicResource NoticeLine}' BorderThickness='1'>
      <Path Data='M0,0 L4,4 L8,0' Stroke='{DynamicResource NoticeMuted}' StrokeThickness='1.2' HorizontalAlignment='Right' VerticalAlignment='Center' Margin='0,0,12,0'/>
     </Border>
     <ControlTemplate.Triggers><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='outline' Property='Background' Value='{DynamicResource NoticeHover}'/></Trigger></ControlTemplate.Triggers>
    </ControlTemplate></ToggleButton.Template>
   </ToggleButton>
   <ContentPresenter IsHitTestVisible='False' Margin='10,7,30,7' VerticalAlignment='Center' Content='{TemplateBinding SelectionBoxItem}' ContentTemplate='{TemplateBinding SelectionBoxItemTemplate}'/>
   <Border x:Name='focus' IsHitTestVisible='False' CornerRadius='8' BorderThickness='1' BorderBrush='{DynamicResource NoticeMuted}' Visibility='Collapsed'/>
   <Popup x:Name='PART_Popup' Placement='Bottom' IsOpen='{TemplateBinding IsDropDownOpen}' AllowsTransparency='True' Focusable='False' PopupAnimation='None'>
    <Border Background='{DynamicResource NoticeSurface}' BorderBrush='{DynamicResource NoticeLine}' BorderThickness='1' CornerRadius='8' Padding='4' Margin='0,4,0,0' MinWidth='{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}'>
     <ScrollViewer MaxHeight='280' CanContentScroll='True' VerticalScrollBarVisibility='Auto'><ItemsPresenter KeyboardNavigation.DirectionalNavigation='Contained'/></ScrollViewer>
    </Border>
   </Popup>
  </Grid>
  <ControlTemplate.Triggers><Trigger Property='IsKeyboardFocusWithin' Value='True'><Setter TargetName='focus' Property='Visibility' Value='Visible'/></Trigger><Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='.45'/></Trigger></ControlTemplate.Triggers>
 </ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='{x:Type ScrollBar}'>
 <Setter Property='Width' Value='8'/><Setter Property='Margin' Value='4,2,0,2'/>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type ScrollBar}'>
  <Track x:Name='PART_Track' Orientation='Vertical' IsDirectionReversed='True' Minimum='{TemplateBinding Minimum}' Maximum='{TemplateBinding Maximum}' Value='{TemplateBinding Value}' ViewportSize='{TemplateBinding ViewportSize}'>
   <Track.DecreaseRepeatButton><RepeatButton Command='ScrollBar.PageUpCommand' Opacity='0' Focusable='False'/></Track.DecreaseRepeatButton>
   <Track.Thumb><Thumb MinHeight='24' Background='{DynamicResource NoticeThumb}'><Thumb.Template><ControlTemplate TargetType='{x:Type Thumb}'><Border Background='{TemplateBinding Background}' CornerRadius='3' Margin='1,0'/></ControlTemplate></Thumb.Template></Thumb></Track.Thumb>
   <Track.IncreaseRepeatButton><RepeatButton Command='ScrollBar.PageDownCommand' Opacity='0' Focusable='False'/></Track.IncreaseRepeatButton>
  </Track>
 </ControlTemplate></Setter.Value></Setter>
</Style>
<Style TargetType='{x:Type CheckBox}'>
 <Setter Property='Template'><Setter.Value><ControlTemplate TargetType='{x:Type CheckBox}'>
  <StackPanel Orientation='Horizontal'>
   <Border x:Name='box' Width='14' Height='14' CornerRadius='4' BorderBrush='{DynamicResource NoticeLine}' BorderThickness='1' Background='{DynamicResource NoticeSurface}' VerticalAlignment='Center'>
    <Path x:Name='mark' Visibility='Collapsed' Data='M0,3 L3,6 L8,0' Stroke='{DynamicResource NoticeInk}' StrokeThickness='1.4' HorizontalAlignment='Center' VerticalAlignment='Center'/>
   </Border><ContentPresenter Margin='7,0,0,0' VerticalAlignment='Center' RecognizesAccessKey='True'/>
  </StackPanel>
  <ControlTemplate.Triggers><Trigger Property='IsChecked' Value='True'><Setter TargetName='mark' Property='Visibility' Value='Visible'/></Trigger><Trigger Property='IsKeyboardFocused' Value='True'><Setter TargetName='box' Property='BorderBrush' Value='{DynamicResource NoticeMuted}'/></Trigger><Trigger Property='IsMouseOver' Value='True'><Setter TargetName='box' Property='BorderBrush' Value='{DynamicResource NoticeMuted}'/></Trigger></ControlTemplate.Triggers>
 </ControlTemplate></Setter.Value></Setter>
</Style>
</ResourceDictionary>");root.Resources.MergedDictionaries.Add(dictionary);Refresh(root);
  }
  public static void Refresh(FrameworkElement root){if(root.Resources.Contains("NoticeTheme")&&(bool)root.Resources["NoticeTheme"]==PetPalette.Dark)return;root.Resources["NoticeTheme"]=PetPalette.Dark;root.Resources["NoticeInk"]=PetPalette.Brush(PetPalette.Ink);root.Resources["NoticeMuted"]=PetPalette.Brush(PetPalette.Muted);root.Resources["NoticeSurface"]=PetPalette.Brush(PetPalette.Background);root.Resources["NoticeLine"]=PetPalette.Brush(PetPalette.Line);root.Resources["NoticeHover"]=PetPalette.Brush(PetPalette.Mix(PetPalette.Background,PetPalette.Muted,.09));root.Resources["NoticeThumb"]=PetPalette.Brush(PetPalette.Hex(PetPalette.Dark?"#615C69":"#D3CFDC"));}
 }
}
