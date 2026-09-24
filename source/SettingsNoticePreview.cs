using System.Windows.Controls;
namespace KianaPet {
 public sealed partial class SettingsWindow {
  void BuildNoticePreviewSettings(Panel page){
   TitleText(page,"通知浮窗外观");
   Note(page,"应用标识样式");var sourceStyle=new ComboBox{ItemsSource=new[]{"图标＋应用名（标题行）","只显示图标，隐藏应用名","左侧大图标＋右侧通知内容","纯文字应用名"},SelectedIndex=System.Array.IndexOf(NoticeSourceIcons.Styles,pet.Settings.NoticeSourceStyle),Padding=new System.Windows.Thickness(8)};System.Windows.Automation.AutomationProperties.SetName(sourceStyle,"通知应用标识样式");sourceStyle.SelectionChanged+=delegate{if(sourceStyle.SelectedIndex<0)return;pet.Settings.NoticeSourceStyle=NoticeSourceIcons.Styles[sourceStyle.SelectedIndex];pet.ApplySettings();};page.Children.Add(sourceStyle);Note(page,"隐藏应用名后仍显示任务状态，悬停图标可查看来源。ChatGPT 使用本机应用图标；其他来源可由适配器注册图标，未提供时用通用通知标识。下面的通知测试会使用所选样式，测试标记始终保留。");
   Check(page,"通知横幅跟随音乐栏配色",pet.Settings.NoticePreviewFollowMusic,delegate(bool v){pet.Settings.NoticePreviewFollowMusic=v;pet.ApplySettings();});
   AddSlider(page,"通知浮窗宽度（逻辑像素）",320,560,pet.Settings.NoticePreviewWidth,10,delegate(int v){pet.Settings.NoticePreviewWidth=v;pet.ApplySettings();});
   AddSlider(page,"速览最多展示任务数",1,4,pet.Settings.NoticePreviewCount,1,delegate(int v){pet.Settings.NoticePreviewCount=v;pet.ApplySettings();});
   Check(page,"悬停铃铛时自动预览（关闭后仅点击展开）",pet.Settings.NoticePreviewHover,delegate(bool v){pet.Settings.NoticePreviewHover=v;pet.ApplySettings();});
   page.Children.Add(Button("预览当前通知浮窗",delegate{pet.ShowNoticePreview(true);},false));
   Note(page,"只显示任务状态、标题和快捷操作；右侧列表图标进入完整通知中心，悬停图标可查看剩余条数。默认 420 宽、单条横幅，长标题省略并可悬停查看全文。跟随音乐配色时沿用音乐页的纯色 / 渐变及浓淡，状态色保持不变；无封面、音乐关闭或断线时回到主题色。");
   TitleText(page,"通知测试");var example=new ComboBox{ItemsSource=new[]{"进行中（蓝色）","已完成（绿色）","等待处理（橙色）","失败（红色）","短暂断线（灰色）","多任务"},SelectedIndex=0,Padding=new System.Windows.Thickness(8)};page.Children.Add(example);page.Children.Add(Button("显示测试通知横幅",delegate{pet.ShowNoticeTest(new[]{"running","succeeded","waiting","failed","stale","multiple"}[System.Math.Max(0,example.SelectedIndex)]);},true));Note(page,"测试直接使用当前横幅尺寸、展示条数、主题与音乐配色，调整设置后可再次测试。示例不会写入真实任务、改变已读或播放提示音；“打开”和通知中心跳转在测试中禁用。点空白处、× 或 Esc 结束测试，之后铃铛仍显示真实通知。");
  }
 }
}
