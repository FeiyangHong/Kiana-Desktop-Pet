using System.Windows.Controls;
namespace KianaPet {
 public sealed partial class SettingsWindow {
  void BuildNoticePreviewSettings(Panel page){
   TitleText(page,"通知浮窗外观");
   Check(page,"使用手机通知式紧凑横幅",pet.Settings.NoticePreviewCompact,delegate(bool v){pet.Settings.NoticePreviewCompact=v;pet.ApplySettings();});
   AddSlider(page,"通知浮窗宽度（逻辑像素）",320,560,pet.Settings.NoticePreviewWidth,10,delegate(int v){pet.Settings.NoticePreviewWidth=v;pet.ApplySettings();});
   AddSlider(page,"速览最多展示任务数",1,4,pet.Settings.NoticePreviewCount,1,delegate(int v){pet.Settings.NoticePreviewCount=v;pet.ApplySettings();});
   Check(page,"悬停铃铛时自动预览（关闭后仅点击展开）",pet.Settings.NoticePreviewHover,delegate(bool v){pet.Settings.NoticePreviewHover=v;pet.ApplySettings();});
   page.Children.Add(Button("预览当前通知浮窗",delegate{pet.ShowNoticePreview(true);},false));
   Note(page,"默认 420 宽、单条紧凑横幅；标题过长显示省略号，悬停标题可查看全文。更多任务从“通知中心”进入。关闭紧凑模式可用较舒展的卡片布局；最多展示四条，长列表可滚动。宽度独立于宠物大小，并按当前屏幕可用宽度自动限制。设置自动保存，可随通用偏好迁移。");
  }
 }
}
