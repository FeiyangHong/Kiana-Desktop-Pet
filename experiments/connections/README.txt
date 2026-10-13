独立连接方式验证工具（实验，尚未接管桌宠）

本目录不参与 source/build.ps1 编译，也没有加入 release-files.json。
现有 ChatGPT / 网易云 CDP 联动、运行程序、偏好及 notify 命令保持原样。
探针与 Hooks 的结果保存到被 Git 忽略的 .local/connection-probes/。

一、Windows 音乐媒体会话

用 Windows PowerShell 5.1 执行：
  powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\experiments\connections\NativeMedia-Probe.ps1 -DurationSeconds 30 -ReadThumbnail

只枚举 cloudmusic 的系统媒体会话；不执行播放控制、不打开或退出播放器。
ReadThumbnail 读取封面流的长度，不下载或保存图片。
canPlay/canPause/canNext 等只代表客户端声明的能力，未代表按钮调用已通过实测。
timelineValid=false 时，原始进度 0 不能当作歌曲刚开始播放。

2026-10-13 本机结果：
  普通启动且无 9436 端口，119/119 次采样成功，暂停/恢复/切歌/最小化后仍可读取。
  歌名、歌手、播放状态和非空封面流可用；时间轴无效，歌曲 ID 不属于系统标准字段。
  同步歌词与副歌计时尚不能单独由此通道提供。

二、Codex Hooks 元数据通道

准备候选配置（不会自动安装）：
  node .\experiments\connections\Build-HookProbeConfig.cjs
验证接收器隐私、异常输入和并发写入：
  node .\experiments\connections\Test-HookReceiver.cjs

Hooks 首次安装或定义修改后必须由用户在应用内审核信任；工具不会代为信任。
本机验证期间新建了用户 ~/.codex/hooks.json，未覆盖原有配置或 notify。
应用内人工信任在 config.toml 中追加了 hooks.state 记录；原有配置内容校验一致。
其他机器安装前须合并候选条目，不能覆盖已有 Hooks。
只记录 session/turn 编号、事件、接收时间与保守状态标签；不记录提示词、工具参数或聊天内容。
Stop 只记为 turn-ended，不能直接标记为成功。
接收器无 stdout/stderr 输出；损坏输入或不可写目录不会阻止原任务。

协议检查（候选配置只作用于临时 app-server 进程）：
  node .\experiments\connections\HookProtocol-Probe.cjs --codex <本机codex.exe完整路径>
读取用户已安装并信任的定义：
  node .\experiments\connections\HookProtocol-Probe.cjs --codex <路径> --installed
真实回合验证（一个独立临时会话，不触及现有会话）：
  node .\experiments\connections\HookProtocol-Probe.cjs --codex <路径> --installed --turn
只中断探针自己创建的临时回合：
  node .\experiments\connections\HookProtocol-Probe.cjs --codex <路径> --installed --interrupt
以上两条真实回合验证会发起模型请求；其余协议检查不发起模型请求。

HookCLI-Probe.py 提供临时 CLI 对照测试；保留模型/账号默认配置，
只在该测试进程关闭无关插件和 notify，进程结束即失效。

2026-10-13 本机结果：
  6 类定义被当前安装版识别、启用且信任；真实收到 SessionStart、
  UserPromptSubmit、Stop、Interrupt、SessionEnd。
  PermissionRequest 只做了定义识别与离线回放，没有主动制造审批来验证。
  当前桌面会话也收到真实 UserPromptSubmit，没有重启 GPT。
  异步事件可能乱序；临时 CLI 退出过快时 Stop 回调可能来不及写入。
  后续正式接入必须按 session/turn 聚合，处理终态优先、重复事件及缺失事件；
  不能用按接收时间排序的最后一条记录直接驱动桌宠。
  云端任务与其他聊天类型的覆盖、完整 Mini 操作能力仍需独立验证。

GPT 主进程本次原本带调试端口，按用户要求一直保持运行。
Hooks 验证本身不访问该端口；尚未在本机重演 GPT 完全普通启动后的全流程。

撤销实验注册时，只移除用户 Hooks 文件中指向本目录 HookReceiver.cjs 的条目，
保留其他条目和已有通知配置；不要清空共享的 Hooks 信任记录。
