# 音乐互动与三套 Q 版动画

## 使用

右键桌宠 → 设置 → 音乐 → 听歌互动与副歌应援。整体模式默认 **安静听歌**，也可选择轻柔应援、活力应援或关闭音乐动作。

- 安静：戴耳机轻轻倾听，副歌也不突然举棒。
- 轻柔：普通段落戴耳机，已知副歌期间低位缓慢挥棒。
- 活力：普通段落戴耳机，已知副歌期间举双棒、小幅弹跳，耳机保留。
- 连续播放约 8 秒后进入音乐状态；短暂停顿收棒，暂停超过 3 秒退出。切歌重新等待，拖动进度按新位置判断。
- 摸头、拖动、ChatGPT 活动状态、菜单及通知操作、睡眠与免打扰优先。“减少动态效果”开启时不自动播放音乐动作。

**音乐栏右键 → 这首歌的互动方式** 可覆盖整体模式，也可选择“跟随整体设置”。歌曲偏好按网易云歌曲 ID 记录，最多 100 首，随通用偏好导出导入；普通三套保持原动作，三套 Q 版使用新绘制素材。

同一菜单和设置页面提供 **标记副歌起点 / 终点（当前位置）**。先在起点标记，再播放或拖动到终点标记；完成后的区间优先于自动数据。清除手动区间后恢复接口数据。当前支持每首歌一个手动区间。

设置页面底部可独立预览三种动作，不修改音乐播放、通知或偏好。

## 副歌数据

请求 `https://music.163.com/api/song/chorus?ids=[歌曲ID]`，实现依据为 [开源适配源码](https://github.com/NeteaseCloudMusicApiEnhanced/api-enhanced/blob/main/module/song_chorus.js)。这是非官方接入，不保证所有歌曲都有数据或包含每一段副歌；不根据音量猜测高潮，不录制或分析音频。

“自动查询副歌时间”可关闭联网。请求仅包含歌曲 ID，不发送歌词、音频、账户凭证或桌宠其他状态。接口起止时间从毫秒转换为秒，并校验歌曲 ID 及区间边界。成功结果缓存 30 天，无结果缓存 1 天；失败至少间隔 5 分钟重试，最多缓存 300 首，保存于运行目录 `chorus-cache.json`。查询失败可使用已有缓存，没有数据时保持耳机倾听。手动标记不依赖联网开关。

副歌开始前约 1 秒进入应援姿态，结束后返回耳机倾听；挥棒使用绘制的循环节奏，尚不跟随真实歌曲 BPM。

## 素材及生成记录

使用内置 **image_gen**，没有调用 CLI/API 后备模式。三次调用分别以对应的 `*-round-ambient.png` 为角色及画风参考，新增：

- `assets/kiana-winter-wish-round-music.png`
- `assets/kiana-fiery-wishing-star-round-music.png`
- `assets/time-runner-kiana-round-music.png`

每套 6 张关键姿态：耳机倾听 2 张、轻柔挥棒 2 张、活力应援 2 张。原图 1024×1536 RGBA，保留生成的透明通道；透明像素隐藏的 RGB 可能有颜色，必须按 alpha 合成显示。

运行时按 `assets/music-sprite-calibration.json` 分行裁切，避开相邻皇冠与发饰；按原版脸宽及脚底基线校准，在 384×416 逻辑画布中生成 768×832 帧缓存，用高质量采样显示。原动作图集不覆盖。

![每行从左到右：原待机、耳机、轻柔、活力](demo/music-actions.png)

### 最终生成提示词组

公共规格分别配合各套服装说明，三个角色独立生成：

```text
Use case: identity-preserve. NEW production transparent RGBA animation sprite
sheet for the EXACT chibi Kiana costume in the reference. Reference is identity
and style input. Preserve hair ornaments, huge silver-white hair, large head,
tiny body, precise violet contours and crisp intricate costume shading.
Bright purple/blue-to-cyan eyes with white star highlights, cheerful friendly
expressions, never dull or vacant.
Exactly TWO columns THREE rows, SIX separate full-body keyframes, preferably
1536x2304 or largest portrait resolution. Consistent face size, head/body ratio,
character center and feet baseline inside each cell; entire hair and props fit.
Actual transparent alpha, no floor, backdrop, shadow, halo, text, grid lines or
painted checkerboard. Slim over-ear headphones remain on in ALL SIX poses.
Row1 quiet listening without rods: left open sparkling eyes, tiny smile, hand
at earcup; right peaceful closed-eye smile, slight other head tilt, hands near chest.
Row2 gentle support: two slim lightsticks at chest/shoulder level, alternate
small left/right sway, warm smile and open sparkling eyes, feet planted, no jump.
Row3 lively cheering: headphones remain, two rods diagonally raised, cheerful
open smile and bright eyes; alternate arm swing with small bent-knee bounce.
Never enlarge the head. Soft glow confined to rods, no dark aura around character.
```

各套服装说明：

```text
Winter Wish: blue crystal crown, ice-blue/white detailed dress, icy-blue
headphones and cyan rods. Preserve crown and all ice ornaments.
Fiery Wishing Star original: red bow and high silver-white ponytail,
red/black/white/gold costume and black gloves, red/black headphones and
warm rose-pink rods. Preserve visible bow and costume details.
Time Runner: violet-white-blue costume, violet crystal crown, long silver-white
high side ponytail, asymmetrical dark sleeve; lavender/purple headphones and
lavender-cyan rods. Preserve crown, sleeve and hair length.
```

素材适用 `ASSET_NOTICE.md`，不属于软件代码的 MIT 授权。
