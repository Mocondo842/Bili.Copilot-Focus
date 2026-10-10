# Bili.Copilot-Focus（哔哩助理 · 去推荐化分支）

上游：<https://github.com/Richasy/Bili.Copilot>（GPL-3.0）
本仓库：<https://github.com/Mocondo842/Bili.Copilot-Focus>

目标：**除主动关注与主动检索外，不呈现官方推流**——去掉「流行（推荐视频/热门/全区排行榜）」「视频分区」「直播」「番剧」「影视」「专栏」这些由官方挑选编排的面，只留下「动态」（你关注的人）、搜索（你自己发起）、以及收藏/历史/稍后再看这些你自己的数据。

做法上刻意压到最小：**不删任何页面、ViewModel、Service、端点、i18n 与设置项**，只改「是否把某一块塞进界面」。这样上游更新时冲突面几乎为零。

## 分支策略

| 分支 | 用途 | 纪律 |
|---|---|---|
| `master` | 上游镜像，只用于同步 | **不在 master 上提交**；用 GitHub 的「Sync fork」把它对齐上游 |
| `focus/derec` | 去推荐化补丁分支（本分支） | 上面的补丁只在这里提交；每次同步后 `master` 合进本分支 |

## 补丁清单（相对上游 master：`src/` 35 files changed, 1237 insertions(+), 159 deletions(-)；含本 fork 自带的构建/安装脚本与文档共 44 files changed, 2263 insertions(+), 222 deletions(-)）

| 编号 | 文件 | 改动 |
|---|---|---|
| G4 | `src/Desktop/BiliCopilot.UI/Toolkits/DeRecommendToolkit.cs`（**新增**，31 行） | 开关 `Disabled`（`=> true`，改回 `false` 即可整体还原）+ `IsEmptyMoment()` 判据。放在 `BiliCopilot.UI.Toolkits` 命名空间，因为下面 4 个文件本来就 `using` 了它——**不新增任何 using 行** |
| G1 | `.../ViewModels/Core/VideoConnectorViewModel/VideoConnectorViewModel.Methods.cs` | 播放页不再把「推荐」区块加进 sections（1 行） |
| G2 | `.../ViewModels/Components/SearchBoxViewModel/SearchBoxViewModel.cs` | 热搜请求在发起前就被拦掉（1 行） |
| G3 | `.../ViewModels/Items/MomentUperSectionViewModel/MomentUperSectionViewModel.cs`（3 处）与 `.../VideoMomentSectionDetailViewModel/VideoMomentSectionDetailViewModel.cs`（1 处） | 动态流丢弃「无内容」注入条目（4 处 `.Where`） |
| G5 | `.../ViewModels/Core/NavigationViewModel.cs` | 被屏蔽的 6 个推流页**强制不可见**：`GetItem` 不再读 `Is{Page}Visible` 设置（1 行） |
| G6 | `.../Pages/SettingsPage.xaml` | 移除「侧边导航栏设置」「搜索推荐」两个设置卡片（2 行改成注释），控件与设置项本身都保留 |
| G7 | `.../ViewModels/Components/SearchBoxViewModel/SearchBoxViewModel.cs` | 搜索推荐词在去推荐化下**一律不请求**，与设置值无关（1 行） |
| G8 | `.../ViewModels/Core/NavigationViewModel.cs` + `.../ViewModels/View/SettingsPageViewModel/SettingsPageViewModel.cs` | 推流页可见性的**写入口**也拦一道：`SetNavItemVisibility` / `WriteNavVisibleSetting` 遇到被屏蔽的页面一律按 `false` 处理 |

`DeRecommendToolkit.IsEmptyMoment()` 的三条判据必须同时成立才丢：`MomentType is null or Unsupported`、`Data is null`、`Description is null`。这样纯文本动态（带 Description）不会被误伤——只按 `Data is null` 过滤会连纯文本一起丢。

## 去推荐化是强制的，不需要手动关设置

早期版本要求你进设置里手动关掉 6 个推流页；现在**改设置也放不回来**：

- 6 个整页级推流面在 `NavigationViewModel.GetItem` 里被强制隐藏（不读 `Is{Page}Visible`）；
- 可见性的**写入口**（`SetNavItemVisibility`、`WriteNavVisibleSetting`）同样拦截：设置页初始化时会按本地设置
  （全新安装默认可见）回调一次，只挡住 `GetItem` 的话，进一次设置页这 6 个页面就会被重新打开——
  而设置卡片已经没了，等于**再也关不掉**。现在这两处遇到被屏蔽的页面一律按 `false` 处理；
- 「侧边导航栏设置」与「搜索推荐」两张设置卡片已从设置界面移除（`Pages/SettingsPage.xaml`），控件与设置项本身都还在仓库里，只是没入口；
- 搜索推荐词、热搜榜都被代码拦在请求之前；
- 「播放完自动播放推荐视频」即使被打开也无效果——推荐区块已不再加入播放页。

## 另外修掉的问题（都不属于上游行为，是本 fork 的增量）

| 编号 | 问题 | 改动 |
|---|---|---|
| F1 | 关掉主窗口后进程留在后台（托盘），再次启动因单实例被重定向而「点了没反应」，只能去任务管理器强杀 | `MainWindow.OnClosed`：只有「还有独立播放器窗口在播放」时才收进托盘，其余情况关窗就是真退出 |
| F2 | 托盘菜单「退出」不保证进程结束 | `App.ExitApp()` 结尾补 `Environment.Exit(0)` |
| F3 | 托盘图标点一下 / 二次启动唤不回藏在托盘里的窗口 | `App.xaml.cs` 两处改成 `AppWindow.Show()` + `Activate()` |
| F4 | 视频偶发「没声音，退出重进才好」：日志里是可播放地址打不开（`mcdn.bilivideo.cn` 这类 PCDN 节点） | 「不使用 P2P」默认改为开（两个 resolver + 设置项默认值），流地址优先选非 PCDN 节点；想改回来在播放器设置里关掉即可 |
| F5 | 日志里每次起播都有一条 `warn - No key binding found for key 'ESC'` | 播放器以 `UseConfig=false` 启动 mpv（不带内置按键表），原来每次起播都往 mpv 发一次 `ESC` 想关统计覆盖层——mpv 不认识这个键，只留下这条 warn。改为只在界面认为覆盖层开着时调用播放器自己的 `ToggleStatsOverlayAsync()` |
| S1 | 快捷键只有方向键和空格，且写死在代码里 | 新增 `Toolkits/PlayerShortcutToolkit.cs`：19 个动作（播放/快进/快退/音量±/倍速±/上一个下一个/静音/字幕/置顶/截图/全屏/紧凑窗口/章节±/统计/退出默认模式）的默认键 + 本地设置读写 + 冲突对调；`PlayerViewModel` 的按键处理改成查表分发，设置页新增「播放器快捷键」卡片，点框→按新键即可改绑 |
| S2 | 快捷键卡片把 19 行直接铺在设置页上 | 收进 `SettingsExpander.Items`：默认折叠；展开后**每个动作一张 `SettingsCard`**（Header=动作名，Description=一句中立描述，Content=按键框），与「播放器控制」同构；末条是「恢复默认」。条目在代码里生成（`Items` 是普通 `List<object>`，模板只在 `OnApplyTemplate` 读一次，所以必须在套用模板前填好） |
| F6 | 编码偏好选 HEVC，实际仍是 H.264 | 选流只按子串匹配（`Codecs.Contains("hev")`）且失败时静默退回第一段；现在按**编码族**匹配（hev1/hvc1/h265、av01/av1、avc1/h264），并把「偏好编码 / 该清晰度的候选编码 / 最终选中」写进日志——能直接看出是服务端没给 HEVC，还是匹配没命中 |
| F7 | 快捷键 ESC、V 按了没反应 | ESC 原先只在「连接中/加载中」才收 WinUI 全屏与画中画，而 F11、Ctrl+M 改的是**播放器自己的**全屏/紧凑窗口状态 → 窗口化播放时按 ESC 什么都不发生；现在无条件先收 WinUI 侧，再关扩展面板、统计覆盖层，最后收播放器全屏/紧凑窗口。V 原先在「字幕已开」的分支里执行的是条目的**选中**命令（重复选中＝空操作）；现在改成翻转开关 + 单独的显隐应用路径 |
| C1 | 搜索结果卡片上的点赞 / 评论按钮点了没反应 | 命令原先写在模板里用经典 `{Binding RelativeSource=TemplatedParent, Path=ViewModel.ToggleLikeCommand}` 取。本仓所有能工作的命令路径都不是经典绑定（UserControl 里用 `x:Bind`、模板里在 code-behind 赋值），且同一模板里普通数据绑定（计数）正常、命令无反应——按「经典绑定取不到 `[RelayCommand]` 生成的命令属性」处理（**可疑根因，本机无法编译验证**）。修法：给按钮加 `x:Name`，在 `VideoCardControl.OnApplyTemplate` / `OnViewModelChanged` 里按既有的 `_rootCard.Command = ...` 同款写法赋值。守卫脚本加反向检查，模板里再出现经典绑定到命令就报错 |
| C2 | 搜索结果、用户空间的视频卡片没有「显示视频简介」按钮 | 按钮原先只在 `ViewModel.Description` 非空时显示，而这两处数据本来就没有简介：① 搜索结果的视频分支不写 `VideoExtensionDataId.Description`（该键只有 Media 服务的 `GetVideoPageDetailAsync` 会写，播放页简介就来自这里；搜索服务 `#US` 字面量里那个 `Description` 无法归因到视频分支——同名常量在 `Live/Season/ArticleExtensionDataId` 里都有）；② 用户空间的视频动态来自 gRPC `dynamic.v2`，Moments 服务的 `#US` 字面量只有 `Aid Cid IsPreview MediaType SeasonId Subtitle`，**根本没有 Description 键**；动态页那个按钮显示的是动态正文，不是视频简介。修法：新增 `Controls/Components/DescriptionButton.xaml`，按钮常驻（`ViewModel` 由 `TemplateBinding`／`x:Bind` 传入，为空则不显示），点击时先开浮层显示等待文案、再按需向 `IPlayerService.GetVideoPageDetailAsync()` 取一次简介并回填；取回任务单飞（并发点击只发一次），**成功（含空结果）才缓存**、失败允许重试；await 后校验按钮是否已被列表回收改绑（虚拟化），避免浮层挂错卡片。用户空间卡片绑 `MomentItemViewModel.InnerVideo`，PGC／专栏等非视频卡片自然不显示 |
| U1 | 依赖长期停在旧版（上游停更，无人在上游升） | 同线升级：WindowsAppSDK → 1.8.260921001、SDK.BuildTools → 10.0.26100.9169、System.Text.Json → 9.0.20、Serilog `4.3.1-dev-02385` → **4.3.1 稳定版**、Serilog.Extensions.Logging `9.0.3-dev` → 9.0.2（**刻意接受 SemVer 降级**，生产环境不留 dev 依赖）、Microsoft.Extensions.* → 9.0.20、CommunityToolkit.WinUI.* → 8.2.251219、CommunityToolkit.Mvvm → 8.4.2（代码里早已在用 8.4 的 partial 属性语法，这次把声明对齐现实）、WinUIEx → 2.9.3、WebView2 → 1.0.4258.31；**H.NotifyIcon.WinUI 保持 2.3.2**（2.4.x 只支持 net10，net9.0-windows 会 NU1202）。**没动**：FluentIcons（1.x→2.x 图标名大改）、WindowsAppSDK 2.x（侧载交付链是围绕 1.8 搭的）、System.Text.Json 10.x（另一条 TFM 线） |
| U2 | 界面冻结时**日志里什么都没有**，无法定位 | 新增 `Toolkits/UiStallWatchdog.cs`：500ms 周期的线程池计时器，每次 tick 往 UI 线程投递一个回调并记录"最近一次回调完成的时刻"；**计时与判定、以及写日志都在线程池线程**上做，所以 UI 线程被占住（甚至死锁）时仍然能写出「UI 线程卡顿 xxx ms」，恢复后补报累计总时长。启动写一条「UI 看门狗已启动（阈值 800ms，周期 500ms）」，所以"没有卡顿行"也能被解释。开关：本地设置 `UiWatchdogEnabled`（默认开） |
| U3 | 日志是**同步写文件**：谁打日志谁做磁盘 I/O（UI 线程打日志就是 UI 线程写盘） | 改成 `WriteTo.Async(a => a.File(...), bufferSize: 10000, blockWhenFull: false)`（新增 `Serilog.Sinks.Async` 2.1.0），并在 `App.ExitApp()` 里 `UiStallWatchdog.Stop()` → `Log.CloseAndFlush()` → `Exit()`。强杀进程只会丢异步队列里尚未写出的那几条（毫秒级窗口） |
| U4 | 每个列表条目都 new 一个区域性对象 / 每个文本都 new 一个正则 | `AppToolkit.GetCulture(language)` 用 `ConcurrentDictionary` 按语言缓存（视频、文章两条目路径改用它）；`EmoteTextBlock` 的表情正则提为 `static readonly`（保留捕获组语义，`Split` 行为不变） |
| U5 | 长列表卡片动画无法关（逐卡注册合成动画） | 启动时按本地设置 `IsCardAnimationEnabled`（默认 true，行为与上游一致）写 `WinUIKernelShareExtensions.IsCardAnimationEnabled`；是否默认关留给拿到看门狗数据后再定 |

| U6 | 点播放页的标签会抛未处理异常（日志实测 4 秒内 5 次 `System.ArgumentException: ... requires an argument of type SearchSuggestItemViewModel`） | `VideoDescriptorControl.OnTagButtonClick` 把标签名（`string`）直接喂给强类型命令；改为 `SearchBoxViewModel.SearchByKeyword(string)`（内部自建 VM 再走原路径） |
| U7 | 80 张图片加载失败（92 条报错里 76 条是 `http://` 地址，且每条白重试一次） | 图片 HttpClient 的 handler 里把 `http://` 请求统一升到 `https://`（实测同一地址 https 直接 200）；一处改动覆盖全部图片控件 |
| U8 | 卡顿只能知道"什么时候"，不知道"在做什么" | 看门狗加面包屑：`UiStallWatchdog.Mark(...)` 记最近 8 条 UI 侧动作（导航、开播放器…），卡顿时连同最近动作一起写日志 |

| U9 | 搜索联想能弹出但**选不中**：点了「翼王」仍按输入框里的 `yiwang` 搜 | `AutoSuggestBox` 上 `UpdateTextOnSelect="False"` 且没有 `TextMemberPath`：选中项既写不回输入框（看起来"没选中"），也没有可显示的字段。改成 `TextMemberPath="SearchContent"` + `UpdateTextOnSelect="True"`，选中即回填、搜索走选中项 |
| U10 | 默认头像类图片 42 次 404（`static.hdslb.com/.../noface.gif@96w_96h_1c.jpg`） | `static.hdslb.com` 不提供图片尺寸变换，带 `@Ww_Hh_1c.jpg` 后缀必 404（实测去掉后缀 200）；图片请求里对 `static.hdslb.com` 去掉 `@` 之后的后缀 |
| U11 | 卡顿只知道"卡多久"，不知道是 GC 还是业务代码 | 看门狗在卡顿前后各读一次 `GC.GetTotalPauseDuration()`，恢复行写「其中 GC 暂停 xxxms」——两者接近就是 GC，接近 0 就是 UI 线程上的业务代码 |

| U12 | `TextMemberPath` 触发 `System.NotSupportedException: ICustomProperty support ... SearchSuggestItemViewModel (property 'SearchContent')`，3 次 | 这是 U9 修复引入的回归：`TextMemberPath` 走运行时 XAML 属性查找，而该 VM 没有 `[GeneratedBindableCustomProperty]`。补上该特性，并加 `ToString()` 兜底 |
| U13 | 默认头像仍 42 次加载失败（`noface.gif` 改成去后缀后仍失败） | 去后缀只是让它 200：`noface.gif` 是 **GIF**，而图片解码端只吃 JPEG。改为把默认头像重写到 `i0.hdslb.com/bfs/face/member/noface.jpg`（支持尺寸后缀，实测 200 image/jpeg） |
| U14 | 卡顿是 GC 还是业务代码（`其中 GC 暂停 0ms` × 5） | 已定论：**不是 GC**，全部为 UI 线程上的业务代码。下一轮用"关弹幕 A/B"区分弹幕渲染与列表渲染 |

| U15 | 800ms 阈值抓不到"体感卡但没记录"的微卡顿 | 看门狗加第二档：300–800ms 记数并按分钟汇总成一条（`近 60 秒微卡顿 N 次，最长 Mms`，无微卡顿不输出），严重档保持逐条 |
| U16 | 日志看不出当时弹幕是开是关，A/B 无法自证 | 弹幕面板初始化时写一条 `弹幕：渲染器 X，滚动/顶部/底部 开否`，日志自己说明测试条件 |

| U17 | 每次视频重新加载，音量都被还原（回到系统/mpv 默认） | 音量只在 `MpvPlayOptions.InitialVolume` 里恢复，而那是"新建播放源"才走的路（且只有视频/番剧两个 resolver 传了它）。现在播放器初始化时读 `SettingNames.PlayerVolume` 显式 `SetVolumeAsync` 复位，并在 `CurrentVolume` 变化时立即持久化（不再依赖 `Player.Volume` 事件是否触发） |
| U18 | 看门狗把 tick 周期算进了卡顿：微卡顿每分钟 120 次、最长 512ms 全是假象 | ack 回调改记"自身延迟"（投递→执行时刻之差），不再记执行时刻——否则 `now - ackTicks` 里必然含一个 500ms 周期，等于把 500ms 当基线。卡顿起点同步改成 `now - latencyTicks` |

| U19 | 评论区用户上传的图片会随机不显示，进出页面有时又好了 | 图片控件（`BasicCoverImage`）在列表回收/离开视野时会被基类取消在途下载，此后没有新的 Source 变更就再也不加载。在子类里挂 `ImageFailed`，对失败做**有预算的重试**（每次入树重置，最多 2 次；先置空再赋回同一 Uri 以触发重新加载）——覆盖评论图、封面、头像等所有用该控件的图片 |
| U20 | 用户上传的评论图与表情图一个是 `BitmapImage` 直连、一个走 `ImageExBase`，失败一个静默一个可查 | 评论图用的是 `BasicCoverImage`（同一条可查路径），表情图是 `BitmapImage` 直连——后者失败不写日志，需要时再单独加观测 |

> 阶段 B/C 未做（如 `Richasy.WinUIKernel.Share` preview4、FluentIcons 2.x、AgentKernel preview6、按实测卡顿点做的针对性优化）：理由与验收口径见设计文档 `.engine/cross-review-upgrade/draft.md`。

| C3 | 评论超过 4 行被截断成 `...`，必须点开才能看全 | `EmoteTextBlock` 的 `MaxLines` 默认 4，超出行数就出现 `...` 按钮。评论处传 `MaxLines="0"`（`RichTextBlock.MaxLines` 的 0 = 自动/不限行数），`IsTextTrimmed` 恒为 false，`...` 按钮自然不再出现；控件与浮层本身保留（动态简介等仍在用） |

> `AUDCLNT_E_DEVICE_INVALIDATED` 是 WASAPI 音频端点被系统回收（切设备/蓝牙断开/休眠）时报的错，音频输出重建由 libmpv 的 wasapi AO 负责，不在应用层；这块没动。F4 针对的是同一条日志里那串音频流地址打开失败的报错。

快捷键（S1）的三条既有行为照旧保留，改动只把「按键 → 动作」这段变成可配置：

- 快进 / 快退 / 音量只在**播放控制栏隐藏**时生效（控制栏显示时方向键要留给界面焦点）；
- 统计信息覆盖层打开时，数字键 `1`-`5`、上下键交给播放器本身（`stats.lua` 的强制绑定）；
- 长按右方向键三倍速只在「快进」仍绑在右方向键上时才触发。

## 安装器（fork 自带，替代上游 Install.ps1）

上游 `scripts/Install.ps1` 在**非管理员**上下文里会 `Start-Process -Verb RunAs` 重启自身后立刻 `exit`，
用户双击（右键 →「使用 PowerShell 运行」）只看到窗口闪一下；UAC 弹窗若被忽略/拒绝，就什么都不会发生。
本 fork 另写了 `scripts/Install-Focus.ps1`（**新文件，不动上游那个**），交付 zip 里只放它：

- 非管理员时请求提权，但**原窗口停下来**并提示「UAC 可能藏在别的窗口后面 / 改为右键以管理员身份运行」；
- 提权后的窗口用 `-NoExit` 启动，装完或报错都停在那里（`ReadKey`，非交互会话退化为 `Start-Sleep`）；
- 证书用 `certutil -f -addstore Root` **静默**导入（不弹确认框），失败只提示不吞掉；
- 依赖按「文件名 `Microsoft*`」识别（沿用上游约定），主应用常规安装失败时自动 `-ForceUpdateFromAnyVersion` 重试；
- 脚本目录用 `$PSScriptRoot` + 两级回退，不依赖 `$MyInvocation` 的写法；
- 文件带 UTF-8 BOM（Windows PowerShell 5.1 才会正确显示中文）。

包内另有 `安装说明.txt`（源文件 `scripts/fork-install-guide.txt`），写清了脚本用法、手动安装三步、
以及「为什么必须信任这个证书，且信任一次之后升级不用再信任」。

真机反馈（2026-10-03 夜）与对应改法：

- 右键「使用 PowerShell 运行」时**只弹一个安全警告，点开后窗口一闪消失**——执行策略/安全提示
  在脚本拿到控制权之前就把它拦掉了，脚本内部再怎么写都没用。补了 **`Install-Focus.cmd`**：
  显式 `-ExecutionPolicy Bypass`，命令结束后无条件 `pause`，所以窗口一定留得住。
- 依赖包报「已安装更高版本 8000.879.2017.0」——系统里的 Windows App Runtime 比包里带的新。
  安装器现在会**先从 msix 里读出 Identity（msix 就是 zip，读 AppxManifest.xml）并和
  `Get-AppxPackage` 的已装版本比较**，已有更高/同版本就静默跳过；万一还是失败，也只提示
  「系统里已有更高版本，不影响后面的安装」。
- 提权交接用**退出码 2** 表示「已请求提权、安装在新窗口继续」，`Install-Focus.cmd` 据此给出
  正确提示（而不是把它当成失败）。
- 顶层加了 `try/catch`，任何意外都会打出来并停住窗口——**再也不会静默闪掉**。

> `Install-Focus.ps1` 与 `.cmd` 都过了 `pwsh` 语法解析检查（容器里的 PowerShell），
> 但**在真 Windows 上还没跑过**——待验证。

## 自动构建（GitHub Actions，x64 侧载包）

`.github/workflows/fork-build-win-x64.yml`（只在 focus/derec 存在）在 GitHub 托管的 `windows-latest` 上打包：

- 触发：推送到 focus/derec（`src/**`、`scripts/**` 或该 workflow 本身有改动时）；也支持手填版本号的手动触发。
- 产物：`BiliCopilot.UI_<版本>_x64.msix` + `focus-sideload.cer` + `Install.ps1` + `安装说明.txt`，打成 zip 后同时进 **Actions artifact** 和 **`focus-<版本>` 预发布**（同名 tag 会先删后建）。默认版本见 workflow 的 `DEFAULT_VERSION`（每次改动都要上抬，否则 `Add-AppxPackage` 不会覆盖安装已装的旧版本）。
- 自检：构建前跑 `scripts/fork_check.ps1` 锚点守卫；构建后跑 `signtool verify /pa`；`APPX0105/APPX0107`（签名相关告警）被升级为错误——**签名没成功就构建失败**，不会又产出一个装不上的包。
- 构建报告：无论成败都推到 `ci/logs` 分支的 `ci/last-build.md`（含 job 状态、产物清单、诊断与日志尾部），这是没有 API token 时也能读到 CI 日志的通道。

### 签名证书（为什么仓库里有一把私钥）

`src/Desktop/BiliCopilot.UI/focus-sideload.pfx` + `.cer`：自签名代码签名证书，`CN=Richasy`（必须与 manifest 的 Publisher 一致），有效期 2026-10-03 → 2036-09-30。

- **不是机密**：它是给侧载包签名用的临时证书，跟上游仓库里那把 `BiliCopilot.UI_TemporaryKey.pfx` 同性质；公钥（.cer）随安装包分发，安装时由 `Install.ps1` 导入到本机受信任根。
- 之所以另起一把：上游那把的证书 **2026-09-28 已过期**，继续用会得到 APPX0105/APPX0107 告警、产出未签名且无法安装的 msix（CI run #4 实测）。
- 有效期十年，所以证书不会每次构建都变——受信任后，后续版本可以直接覆盖安装。

## 同步上游（每次上游更新后）

先在 GitHub 仓库页面点一次「Sync fork」，让 fork 的 `master` 对齐上游（fork 无法直接用 SSH 拉上游：deploy key 只授权本仓库）。然后一条命令：

```bash
bash scripts/focus_sync.sh
```

它做四件事：`fetch origin` → 快进 `master`（`--ff-only`，所以 master 上永远不会出现本地提交）→ 把 `master` 合进 `focus/derec` → 跑锚点守卫，**只有守卫通过才推送**。任一步失败即中止。

手动等价步骤：

```bash
git fetch origin
git checkout master && git merge --ff-only origin/master
git checkout focus/derec && git merge --no-edit master
bash scripts/fork_check.sh        # 必须输出 fork_check: OK (5/5 anchors)；Windows 上用 .\scripts\fork_check.ps1
git push origin focus/derec
```

### 冲突兜底：按锚点重放，不要手工合并整段

补丁只有 4 个文件 5 行，真正会冲突的只有「同一行恰好被上游改过」这一种情况。锚点原文：

```csharp
// G1 锚点：                              if (_view.Recommends is not null)
//   改成：                               if (!DeRecommendToolkit.Disabled && _view.Recommends is not null)

// G2 锚点：                              if (HotSearchItems.Count > 0)
//   改成：                               if (DeRecommendToolkit.Disabled || HotSearchItems.Count > 0)

// G3 锚点：                              foreach (var item in momentView.Moments)
//   改成：                               foreach (var item in momentView.Moments.Where(p => !DeRecommendToolkit.IsEmptyMoment(p)))
//   （另有两处 `.Select(p => new MomentItemViewModel(...))`，在 `.Select` 之前插同样的 `.Where(...)`）
```

找不到锚点时**必须让守卫脚本失败退出**，不要跳过——「补丁被上游改没了」和「补丁还在」在编译上没有任何区别。若上游把文件搬了位置，路径无关的守卫脚本仍能找到锚点；照锚点把改动重放过去即可。

## 验证状态（如实记录）

- **已验证**（Linux 上可做）：补丁施加与锚点断言；对真实上游快照的三方合并实验——`v2.2510.0.0 → v2.2511.2.0`（154 文件 / +54849 −440）与 `v2.2509.0.0 → v2.2511.2.0`（178 文件 / +55052 −2347）均 **CLEAN、0 冲突**，合并后 5 个锚点全在。
- **未验证**：编译与运行。本补丁是在没有 Windows / Visual Studio / Windows App SDK 的机器上写的，**从未编译过**。请在本机用 Visual Studio 打开 `src/Bili.Copilot.sln` 构建 x64 后验证。
- **待实机确认**：① Windows 10（manifest 声明 `MinVersion=10.0.19041.0`，但上游 README 只写 Windows 11）；② 动态流里实际出现的注入条目类型；③ 关掉导航项后是否真的零请求（抓包）；④ 播放页的 `Related` 字段仍随 `/x/web-interface/view` 到达（本方案只在渲染层摘掉，要连字段都不接收必须改 NuGet 依赖 `bili-kernel`，不做）。

完整的调研与评审记录见上游工作目录 `cross-review/bilicopilot-derec/`（`draft.md` 方案、`report.md` 三轮交叉评审）。本分支的补丁与那份方案逐字一致。

## 注意

- **不要安装 Microsoft Store 版本**：它与侧载版共用 package identity，装上就把补丁版覆盖回官方版。侧载用仓库里的 `scripts/Install.ps1`。
- 应用内那个「有更新」提示不是更新器（只比较版本号并打开官方 release 页面），可以留着。
- 本仓库沿用上游的 **GPL-3.0** 许可。
