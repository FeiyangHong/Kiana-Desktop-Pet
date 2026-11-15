using System;using System.Linq;using System.Diagnostics;using System.Windows;using System.Windows.Controls;using System.Windows.Media;using System.Windows.Threading;
namespace KianaPet {
 // Shared by the live pet and the isolated preview; constructing or drawing
 // this sequence never writes preferences or contacts either integration.
 public sealed class OutfitActionSequence {
  public readonly AnimationPreviewSource Source;public readonly AnimationChoice Choice;
  public readonly double RiseMs,ActionMs,SitMs;public double DurationMs{get{return RiseMs+ActionMs+SitMs;}}
  public OutfitActionSequence(Config c,string action,bool seated,AnimationPreviewSource source=null){if(!SkinChangeActions.Allowed.Contains(action))throw new ArgumentException("请选择现有换装动作。");Source=source??new AnimationPreviewSource(c.Skin,c.MusicSeatedStyle);Choice=AnimationGallery.Catalog.First(a=>a.Id==action);bool sit=seated&&MusicSprites.Supported(c.Skin);RiseMs=sit?720/AnimationTiming.Speed(c,"music-rise"):0;SitMs=sit?720/AnimationTiming.Speed(c,"music-sit"):0;ActionMs=Choice.Duration/AnimationTiming.Speed(c,action);}
  public string VisibleAction(double ms){ms=Math.Max(0,Math.Min(DurationMs-.001,ms));return ms<RiseMs?"music-rise":ms<RiseMs+ActionMs?Choice.Id:"music-sit";}
  public AnimationPose Frame(double ms){ms=Math.Max(0,Math.Min(DurationMs-.001,ms));string action=VisibleAction(ms);var choice=action==Choice.Id?Choice:AnimationGallery.Catalog.First(a=>a.Id==action);double elapsed=action=="music-rise"?ms*720/RiseMs:action=="music-sit"?(ms-RiseMs-ActionMs)*720/SitMs:(ms-RiseMs)*Choice.Duration/ActionMs;return Source.Frame(choice,elapsed);}
 }
 public sealed class OutfitActionPreview:Window {
  readonly Config config;readonly string[] actions;readonly AnimationPreviewSource previewSource;readonly Image sprite=new Image{Width=224,Height=224*208d/192,Stretch=Stretch.Fill};
  readonly ComboBox selection=new ComboBox();readonly CheckBox seated=new CheckBox{Content="模拟听歌坐姿衔接"},cycle=new CheckBox{Content="自动轮播勾选动作",IsChecked=true};
  readonly TextBlock status=new TextBlock{TextWrapping=TextWrapping.Wrap};readonly Stopwatch clock=Stopwatch.StartNew();readonly DispatcherTimer timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(33)};
  OutfitActionSequence sequence;bool disposed;
  public OutfitActionPreview(Config current,bool currentSeated){
   config=PreferenceTransfer.Copy(current);previewSource=new AnimationPreviewSource(config.Skin,config.MusicSeatedStyle);actions=config.SkinShuffleActions.Where(SkinChangeActions.Allowed.Contains).Distinct().ToArray();
   if(actions.Length==0)throw new ArgumentException("请先勾选至少一个换装动作。");
   Title="换装动作预览 · 不更换皮肤";Width=450;Height=680;MinWidth=390;MinHeight=540;WindowStartupLocation=WindowStartupLocation.CenterOwner;PetVisuals.Apply(this,true);Background=PetPalette.Brush(PetPalette.Background);Foreground=PetPalette.Brush(PetPalette.Ink);
   var root=new StackPanel{Margin=new Thickness(20)};root.Children.Add(new TextBlock{Text="预览勾选的换装动作",FontSize=20,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,10)});
   root.Children.Add(new TextBlock{Text="使用当前皮肤测试；不会更换桌宠皮肤、重置计时或操作音乐。",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)});
   selection.ItemsSource=actions.Select(SkinChangeActions.Label).ToArray();selection.Padding=new Thickness(8);root.Children.Add(selection);
   seated.IsEnabled=MusicSprites.Supported(config.Skin);seated.IsChecked=seated.IsEnabled&&currentSeated;seated.Margin=new Thickness(0,10,0,8);root.Children.Add(seated);cycle.Margin=new Thickness(0,0,0,10);root.Children.Add(cycle);
   var stage=new Border{Height=250,Background=Brushes.White,CornerRadius=new CornerRadius(12),Child=sprite};root.Children.Add(stage);status.Margin=new Thickness(0,10,0,10);root.Children.Add(status);
   var buttons=new WrapPanel();var replay=CompanionControls.Button("重播当前动作",ResetPlayback);var next=CompanionControls.Button("下一动作",delegate{selection.SelectedIndex=(selection.SelectedIndex+1)%actions.Length;if(actions.Length==1)ResetPlayback();});buttons.Children.Add(replay);buttons.Children.Add(next);root.Children.Add(buttons);
   root.Children.Add(new TextBlock{Text=seated.IsEnabled?"坐姿衔接：起身 → 所选动作 → 坐下。减少动态效果时只显示静态姿态。":"当前普通版没有听歌坐姿素材；展示已有动作及原有回退姿态。",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0),FontSize=12});Content=new ScrollViewer{Content=root,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
   selection.SelectionChanged+=delegate{ResetPlayback();};seated.Checked+=delegate{ResetPlayback();};seated.Unchecked+=delegate{ResetPlayback();};
   timer.Tick+=delegate{if(disposed||sequence==null)return;timer.Interval=TimeSpan.FromMilliseconds(MotionSettings.Enabled?33:250);if(MotionSettings.Enabled&&clock.Elapsed.TotalMilliseconds>=sequence.DurationMs){if(cycle.IsChecked==true){int nextIndex=(selection.SelectedIndex+1)%actions.Length;if(nextIndex==selection.SelectedIndex)ResetPlayback();else selection.SelectedIndex=nextIndex;}else{Draw();timer.Stop();clock.Stop();return;}}Draw();};cycle.Checked+=delegate{if(IsVisible&&!disposed){clock.Start();timer.Start();}};
   IsVisibleChanged+=delegate{if(IsVisible&&!disposed){clock.Start();timer.Start();}else{timer.Stop();clock.Stop();}};Closed+=delegate{disposed=true;timer.Stop();clock.Stop();sprite.Source=null;sequence=null;};selection.SelectedIndex=0;
  }
  void ResetPlayback(){if(selection.SelectedIndex<0||disposed)return;sequence=new OutfitActionSequence(config,actions[selection.SelectedIndex],seated.IsChecked==true,previewSource);clock.Restart();if(IsVisible)timer.Start();else clock.Stop();Draw();}
  void Draw(){if(sequence==null||disposed)return;double elapsed=MotionSettings.Enabled?Math.Min(clock.Elapsed.TotalMilliseconds,sequence.DurationMs-.001):sequence.RiseMs+sequence.ActionMs*.5;try{var pose=sequence.Frame(elapsed);sprite.Source=pose.Source;sprite.RenderTransform=new MatrixTransform(SpriteGeometry.Matrix(pose.Alignment,224));status.Text=(MotionSettings.Enabled&&clock.Elapsed.TotalMilliseconds>=sequence.DurationMs?"预览完成："+SkinChangeActions.Label(sequence.Choice.Id):"当前阶段："+MusicActivity.ActionName(sequence.VisibleAction(elapsed)))+(MotionSettings.Enabled?"":" · 减少动态效果：静态预览");}catch(Exception e){timer.Stop();status.Text="预览未完成："+e.Message;}}
 }
}
