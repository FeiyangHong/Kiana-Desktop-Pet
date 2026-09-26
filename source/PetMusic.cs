using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
namespace KianaPet {
 public sealed partial class PetWindow {
  public readonly MusicBridge Music=new MusicBridge();readonly MusicLibrary musicLibrary=new MusicLibrary();
  MusicWindow musicWindow;readonly DispatcherTimer musicTimer=new DispatcherTimer();bool musicPolling,musicBusy;double musicUntil;
  public string MusicStatus{get{return Music.Current.Label;}}
  bool MusicHovered{get{return musicWindow!=null&&musicWindow.IsVisible&&(musicWindow.IsMouseOver||musicWindow.IsDragging||musicWindow.KeyboardMode);}}
  bool MusicInteractionActive{get{return Settings.MusicEnabled&&(MusicHovered||clock.Elapsed.TotalSeconds<musicUntil);}}
  void StartMusic(){musicTimer.Interval=TimeSpan.FromMilliseconds(850);musicTimer.Tick+=PollMusic;if(!preview){musicTimer.Start();PollMusic(null,EventArgs.Empty);}}
  async void PollMusic(object sender,EventArgs e){if(musicPolling||quitting)return;musicPolling=true;try{await Music.Poll(Settings.MusicEnabled);if(quitting)return;musicLibrary.Update(Music.Current,Settings.MusicEnabled&&Settings.MusicLyrics);RefreshMusicChorus();UpdateMusic();}finally{musicPolling=false;if(!quitting){musicFailures=Music.Current.Connected?0:musicFailures+1;double normal=!Settings.MusicEnabled||sessionLocked||!IsVisible?4:.85;musicTimer.Interval=TimeSpan.FromSeconds(Settings.ResourceSaving?PolishRules.RetrySeconds(musicFailures,normal):.85);}}}
  readonly MusicSurfaceAppearance toolbarMusicAppearance=new MusicSurfaceAppearance();
  void UpdateMusic(){bool fresh=Music.Current.Connected&&DateTime.UtcNow-Music.Current.At<=TimeSpan.FromSeconds(6);toolbarMusicAppearance.Apply(hoverBar,MusicCoverForAppearance,Settings,Settings.ToolbarFollowMusic&&Settings.MusicEnabled&&fresh);if(!Settings.MusicEnabled||!IsVisible||manualHidden||fullscreenHidden||pressed||dragging||!Music.Current.Connected||(!Settings.MusicPausedCard&&!Music.Current.Playing)||DateTime.UtcNow-Music.Current.At>TimeSpan.FromSeconds(6)){musicUntil=0;if(musicWindow!=null)musicWindow.HideSoft(true);return;}
   if(menuLayerYielding){if(musicWindow!=null){musicWindow.Refresh(Music.Current,musicLibrary.Line(Music.Current),MusicCoverForAppearance,musicBusy);ApplyMenuLayers();}return;}
   bool show=MusicPlacement.HoverVisible(Settings.MusicHoverOnly,hover||hoverBar.IsMouseOver||MusicHovered,musicWindow!=null&&musicWindow.IsDragging,clock.Elapsed.TotalSeconds,ref musicUntil);
   if(!show){if(musicWindow!=null)musicWindow.HideSoft(false);return;}
   if(musicWindow==null)musicWindow=new MusicWindow(this);var state=Music.Current;musicWindow.Refresh(state,musicLibrary.Line(state),musicLibrary.CoverFor(state),musicBusy);musicWindow.ShowSoft();musicWindow.Follow(Bounds());
  }
  public void FocusMusicControls(){if(!Settings.MusicEnabled||!Music.Current.Connected){Say("请先连接网易云音乐并开启音乐栏。",4);return;}musicUntil=clock.Elapsed.TotalSeconds+5;UpdateMusic();if(musicWindow!=null&&musicWindow.IsVisible)musicWindow.FocusControls();}
  public void SetMusicPosition(string position){Settings.MusicPosition=position;if(position=="free"&&!Settings.MusicHasPosition&&musicWindow!=null)musicWindow.SavePosition();ApplySettings();}
  public void ResetMusicPosition(){RestoreMusicDock(Settings.MusicDefaultPosition);}
  public void RestoreMusicDock(string position){if(position!="below"&&position!="left"&&position!="right")return;Settings.MusicPosition=position;Settings.MusicHasPosition=false;ApplySettings();PositionMusic();UpdateMusic();}
  public void PositionMusic(){if(musicWindow!=null)musicWindow.Follow(Bounds(),true);}
  public async void MusicCommand(string action){if(musicBusy)return;musicBusy=true;try{UpdateMusic();if(!await Music.Command(action))Say("音乐控制暂不可用，请重新连接网易云音乐。",4);await Music.Poll(Settings.MusicEnabled);musicLibrary.Update(Music.Current,Settings.MusicEnabled&&Settings.MusicLyrics);}finally{musicBusy=false;if(!quitting)UpdateMusic();}}
  async void RefreshMusicChorus(){await chorusLibrary.Update(Music.Current,Settings.MusicEnabled&&Settings.MusicChorusEnabled&&MusicReactions.Mode(Settings,Music.Current.Id)!="off");}
  void StopMusic(){chorusLibrary.Dispose();musicTimer.Stop();Music.Dispose();musicLibrary.Dispose();if(musicWindow!=null)musicWindow.Close();}
  public void LaunchMusic(){musicFailures=0;musicTimer.Interval=TimeSpan.FromMilliseconds(850);try{Process.Start(new ProcessStartInfo{FileName=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"KianaDesktopPet.exe"),Arguments="--music-launch",WorkingDirectory=AppDomain.CurrentDomain.BaseDirectory,UseShellExecute=true});}catch(Exception e){MessageBox.Show("暂时无法打开网易云音乐："+e.Message,"网易云音乐");}}
 }
}

