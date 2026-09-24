# 琪亚娜独立桌宠

## 效果演示

![桌宠通知效果预览](docs/demo/effect-preview.png)

![任务通知列表](docs/demo/task-list.png)

![音乐卡片](docs/demo/music.png)

## 安装方法

适用 Windows 10/11 x64，需 .NET Framework 4.8。已安装 Git 的电脑可在 PowerShell 中运行：

```powershell
git clone https://github.com/FeiyangHong/Kiana-Desktop-Pet.git
cd Kiana-Desktop-Pet
.\build-release.ps1
.\dist\Kiana-Desktop-Pet-0.7.5-Windows\install.ps1 -InstallRoot 'E:\Kiana-Desktop-Pet\runtime\KianaDesktopPet'
```

`-InstallRoot` 可改为本机希望保存程序、设置与日志的目录；ChatGPT 平滑联动组件会装在其同级的 `KianaSmoothPet`。首次启动桌宠请使用安装器生成的“琪亚娜桌宠”快捷方式。另一台电脑拉取更新后重新构建、安装即可；本机设置会保留并在更新前备份。

## 功能说明

- 六套琪亚娜衣装、触摸反馈、自由走动、作息、跨屏拖动和效果预览。
- 托盘菜单优先显示，宠物、工具栏与音乐栏会让到外部菜单后方，包括 QQ 的 Chromium 托盘菜单。
- ChatGPT Mini 后台联动、任务通知和状态提示；连接状态与通知来源分别显示。AI 对话接口尚未启用。
- 网易云音乐卡片、歌词、通知中心、快捷键、场景和偏好导入导出。
- 原创代码采用 [MIT 许可证](LICENSE)；角色素材及第三方组件的使用范围见 [素材声明](ASSET_NOTICE.md) 与 [licenses/](licenses/)。历史版本记录见 [CHANGELOG.md](CHANGELOG.md)。