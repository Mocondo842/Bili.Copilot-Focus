# 去推荐化补丁锚点自检（PowerShell 版，供 Windows 上用；与 fork_check.sh 等价）
#
# 每次同步上游之后运行；缺失任一锚点即以退出码 1 结束。
# 不绑定文件路径：上游搬过文件（VideoPlayerPageViewModel → VideoConnectorViewModel），
# 所以在 src/ 全树里找锚点文本，只回答「补丁还在不在」。
# 文本级检查，不替代编译器。
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

$fail = 0
function Check([string]$pattern, [int]$expect, [string]$what) {
    $n = (Get-ChildItem -Path src -Recurse -Filter *.cs |
          Select-String -SimpleMatch -Pattern $pattern).Count
    if ($n -lt $expect) {
        Write-Host "MISSING ANCHOR ($n<$expect): $what"
        $script:fail = 1
    }
}

if (-not (Test-Path 'src/Desktop/BiliCopilot.UI/Toolkits/DeRecommendToolkit.cs')) {
    Write-Host 'MISSING FILE: src/Desktop/BiliCopilot.UI/Toolkits/DeRecommendToolkit.cs'
    $fail = 1
}

foreach ($file in @(
    'src/Desktop/BiliCopilot.UI/Toolkits/PlayerShortcutToolkit.cs',
    'src/Desktop/BiliCopilot.UI/Controls/Settings/ShortcutSettingControl.xaml',
    'src/Desktop/BiliCopilot.UI/Controls/Components/DescriptionButton.xaml',
    'src/Desktop/BiliCopilot.UI/Toolkits/FailedNodeRegistry.cs',
    'src/Desktop/BiliCopilot.UI/Toolkits/PlaybackHealthMonitor.cs')) {
    if (-not (Test-Path $file)) {
        Write-Host "MISSING FILE: $file"
        $fail = 1
    }
}

Check '!DeRecommendToolkit.Disabled && _view.Recommends is not null' 1 'G1 播放页推荐区块守卫'
Check 'DeRecommendToolkit.Disabled || HotSearchItems.Count > 0' 1 'G2 热搜请求守卫'
Check '!DeRecommendToolkit.IsEmptyMoment(p)' 4 'G3 动态空条目过滤（4 处）'
Check '!DeRecommendToolkit.IsHiddenPage(typeof(TPage))' 1 'G5 导航项强制隐藏（不读设置）'
Check 'DeRecommendToolkit.Disabled || !isRecommendEnabled' 1 'G7 搜索推荐词强制不请求'
Check 'DeRecommendToolkit.IsHiddenPage(pageType)' 2 'G8 推流页可见性写入口拦截（导航 + 设置页）'
Check 'Players.Any(p => p.Window is not null)' 1 'F1 关窗规则（仅独立播放器窗口时收托盘）'
Check 'SettingNames.PlayWithoutP2P, true' 2 'F4 默认避开 PCDN 节点（两个 resolver）'
Check 'AppWindow.Show();' 2 'F3 单实例/托盘唤回窗口'
Check 'PlayerShortcutToolkit.TryMatch(args.VirtualKey, out var action)' 1 'S1 播放器按键改走快捷键表'
Check 'PlayerShortcutToolkit.Get(PlayerShortcutAction.SkipForward)' 1 'S2 长按三倍速跟随「快进」的绑定'
Check 'PlayerShortcutToolkit.Set(action, shortcut)' 1 'S3 设置页写入新绑定'
Check 'AppToolkit.IsCodecMatch(' 2 'S5 编码偏好按编码族匹配（两个 resolver）'
Check 'IsSubtitleEnabled = !IsSubtitleEnabled;' 1 'S6 字幕开关快捷键只翻转开关'
Check 'ApplySubtitleEnabledAsync(value)' 1 'S7 字幕显隐走单独的应用路径'
Check 'PlayerShortcutToolkit.GetDescription(action)' 1 'S8 每个动作一条次级菜单（带描述）'
Check 'ToggleVideoLikeAsync(Data.Identifier.Id, state)' 1 'C1 视频卡片点赞按钮走真实点赞接口'
Check 'showCommentAction: _showCommentAction' 2 'C2 搜索卡片带评论动作（首屏 + 翻页）'
Check 'newSection.SetShowCommentAction(ShowComment)' 1 'C3 搜索页把评论面板接到视频分区'
Check 'showCommentAction: ShowVideoComment' 1 'C4 用户空间视频搜索卡片带评论动作'
Check 'Richasy.BiliKernel.Models.CommentTargetType.Video' 2 'C5 评论面板按视频初始化（搜索页 + 用户空间）'
Check 'GetVideoPageDetailAsync(new MediaIdentifier(Data.Identifier.Id' 1 'C6 简介按需取回（与播放页同一接口）'
Check '_descriptionTask ??= LoadDescriptionAsync();' 1 'C7 简介取回任务单飞（并发点击只发一次）'
Check '_likeButton.Command = ViewModel?.ToggleLikeCommand;' 1 'C8 点赞命令在 code-behind 赋值'
Check '_commentButton.Command = ViewModel?.ShowCommentCommand;' 1 'C9 评论命令在 code-behind 赋值'
Check 'ReferenceEquals(video, ViewModel)' 1 'C10 取回后校验按钮是否已被列表回收'
Check 'public VideoItemViewModel? InnerVideo' 1 'C11 用户空间视频卡片复用内层视频取简介'
Check 'Interlocked.Exchange(ref _stallStartTicks, now - latencyTicks);' 1 'U1 卡顿起点=被延迟的那次回调的投递时刻'
Check 'request.RequestUri?.Scheme == Uri.UriSchemeHttp' 1 'U14 图片地址统一升级到 https'
Check 'SearchByKeyword(data.Name)' 1 'U15 点标签不再把字符串喂给强类型命令'
Check 'internal static void Mark(string activity)' 1 'U16 卡顿面包屑入口'
Check '卡顿前最近动作' 1 'U17 卡顿日志带最近动作'
Check 'Interlocked.Exchange(ref _reported, 1) == 1' 1 'U18 看门狗去重标志改原子'
Check 'static.hdslb.com' 1 'U19 static 域名去掉尺寸后缀'
Check 'noface.gif' 1 'U19b 默认头像换成可解码的 JPEG'
Check 'MinorThresholdMs = 300' 1 'U25 微卡顿分档（300ms 起，按分钟汇总）'
Check 'SettingNames.PlayerVolume, -1d' 1 'U27 初始化时显式恢复上次音量'
Check 'Stopwatch.GetTimestamp() - postedTicks' 1 'U29 卡顿口径=UI 回调延迟，不含 tick 周期'
Check 'ImageFailed += OnImageFailed' 1 'U30 图片失败有预算重试（回收取消不再留空白）'
Check 'UI 线程恢复响应，本次卡顿累计约' 1 'U2b 恢复后补报总时长'
Check 'UI 看门狗已启动' 1 'U2 看门狗启动有日志'
Check 'UiStallWatchdog.Start();' 1 'U3 启动时拉起看门狗'
Check 'UiStallWatchdog.Stop();' 1 'U4 退出时停看门狗'
Check 'WriteTo.Async(a => a.File(' 1 'U5 日志异步落盘'
Check 'IsCardAnimationEnabled' 2 'U6 卡片动画开关接入启动路径'
Check 'AppToolkit.GetCulture(primaryLan)' 2 'U7 区域性对象按语言缓存'
Check 'EmojiRegex' 2 'U8 表情正则提为静态'
Check '_ackLatencyTicks = 0;' 1 'P1 看门狗基准初始化为 0（不再把开机毫秒数当卡顿）'
Check 'foreach (var player in Players.ToList())' 1 'P2 打开新视频前先退出已有播放器'
Check 'Text?.Pictures?.Count > 0 && Gallery is not null' 1 'P3 评论九宫格入树恢复'
Check 'CollectNodeCandidates(' 5 'P4 节点候选收集（基类定义 + 视频/音频 × 两个 resolver）'
Check 'UpdateNodeState(' 3 'P5 候选交给基类（基类定义 + 两个 resolver）'
Check 'ResetNodeState();' 2 'P6 换视频重置手选节点（两个 resolver）'
Check '无法避开' 2 'P7 全 PCDN 候选告警（两个 resolver）'
Check 'GetVideoNodes()' 1 'P8 播放器暴露节点候选'
Check 'SelectNodeAsync(string? host)' 1 'P9 手选节点命令（按进度重播）'
Check 'FillNodeMenu();' 1 'P10 右键菜单打开时填充播放节点'
Check 'RunSpeedTestAsync(' 3 'P14 测速入口（基类定义 + 自动择优 + 手选命令）'
Check 'ShouldSwitchTo(' 2 'P15 切换阈值判定（定义 + 调用）'
Check 'NodeSpeedTestResults' 4 'P16 测速结果缓存（基类读写 + 菜单快照）'
Check 'BuildSpeedTestTargets(' 3 'P17 待测线路组装（定义 + 两处调用）'
Check 'SettingNames.AutoSelectFastestNode' 2 'P18 自动择优开关读写'
Check 'StringNames.NodeAllMirrors' 1 'P19 右键菜单「全部 CDN 线路」'
Check 'FailedNodeRegistry.MarkFailed(' 2 'P23 打不开即登记（自动切换回退 + 换线回滚）'
Check 'FailedNodeRegistry.IsFailed(' 2 'P24 择优前过滤失败主机（自动 + 手测速）'
Check 'FailedNodeRegistry.Clear(' 1 'P25 手选即解除登记'
Check 'https://github.com/Mocondo842/Bili.Copilot-Focus/releases' 1 'P26 更新横幅指向本 fork 发布页'
Check 'BuildSpeedTestSet(' 2 'P27 统一测速集（定义 + 解析器入口）'
Check '节点：统一测速集' 1 'P28 测速集来源与当前入集诊断日志'
Check 'NodeRecoveryPolicy.IsNodeOpenError(' 1 'P29 错误类型闸（自动回退 + 错误换线共用一次归因）'
Check 'NodeRecoveryPolicy.CanRecover(' 1 'P30 错误换线冷却/次数/手选限流'
Check 'RunErrorNodeRecoveryAsync(' 2 'P31 受控错误换线（定义 + 唯一调用）'
Check '_errorNodeRecoveryConsumed = false;' 1 'P32 换视频重置错误换线配额'
Check 'public static string? DecodeHtml(string? text)' 1 'F8 实体解码 helper'
Check 'AppToolkit.DecodeHtml(' 16 'F8 展示文本统一解码（16 处）'
Check 'private bool _succeeded;' 1 'F9 图片成功标志（DrawImage 置位）'
Check 'ReloadIfPending(' 3 'F9 延迟重载入口（定义 + 两条调度路径）'
Check 'BaseRetryDelayMs << ' 1 'F9 重试退避延迟（跳出批量取消窗口）'
Check 'DanmakuLevelProbe.InspectAsync(' 1 'P33 只读弹幕字段探针（唯一调用）'
Check '弹幕等级探针：{Report}' 1 'P34 探针日志只输出结构化证据'
Check 'Interlocked.CompareExchange(ref _danmakuLevelProbeDone' 1 'P35 每个视频只探测一次'
Check 'class ProbeReport' 1 'P36 探针使用固定报告模型（无反射序列化）'
Check '解码：请求 NVDEC（cuda），GPU API Vulkan' 1 'P37 NVDEC 走 Vulkan 后端（cuda 硬解只允许 opengl/vulkan）'
Check '选流：音频节点 {Host}（{Reason}）' 2 'P38 音频按同一判定顺序选路并记录主机（两个 resolver）'
Check '音频候选全是 PCDN' 2 'P39 音频回退到 PCDN 时告警（不合规复用「无法避开」）'
Check 'ReplayAndVerifyAsync(' 5 'P40 换线统一走校验/回滚通路（定义 + 4 处调用）'
Check '节点：换线回滚到 ' 1 'P41 校验失败/健康窗口故障时回滚原线路'
Check '节点：播放中只记偏好' 1 'P42 播放中不重播（避免分段 MP4 seek 破坏解码）'
Check '节点：已有重播在进行，本次只记偏好' 1 'P43 换线重播单飞（并发换线不再叠加）'
Check '播放健康：' 1 'P44 每段播放收尾写一条健康汇总'


# 卡片与评论改造的 XAML 锚点（XAML 不在 Check 的搜索范围内，单独查）
foreach ($item in @(
    @('src/Desktop/BiliCopilot.UI/Controls/Components/VideoCardControl/VideoCardControl.xaml', 'x:Name="LikeButton"', 'C12 搜索卡片点赞按钮（供 code-behind 取）'),
    @('src/Desktop/BiliCopilot.UI/Controls/Components/VideoCardControl/VideoCardControl.xaml', 'x:Name="CommentButton"', 'C13 搜索卡片评论按钮（供 code-behind 取）'),
    @('src/Desktop/BiliCopilot.UI/Controls/Components/VideoCardControl/VideoCardControl.xaml', '<local:DescriptionButton', 'C14 搜索卡片简介按钮'),
    @('src/Desktop/BiliCopilot.UI/Controls/Components/MomentCardControl/PersonalVideoMomentPresenter.xaml', 'ViewModel.InnerVideo', 'C15 用户空间卡片简介按钮'),
    @('src/Desktop/BiliCopilot.UI/Controls/Comment/CommentItemControl.xaml', 'MaxLines="0"', 'C16 评论不截断'),
    @('src/Directory.Packages.props', 'Version="1.8.260921001"', 'U9 WindowsAppSDK 升到 1.8 同线最新'),
    @('src/Directory.Packages.props', 'Serilog.Sinks.Async', 'U10 异步日志包已声明'),
    @('src/Desktop/BiliCopilot.UI/BiliCopilot.UI.csproj', 'Serilog.Sinks.Async', 'U11 异步日志包已被引用'),
    @('src/Desktop/BiliCopilot.UI/App.xaml.cs', 'Log.CloseAndFlush();', 'U12 退出前刷日志队列'),
    @('src/Desktop/BiliCopilot.UI/Toolkits/UiStallWatchdog.cs', 'UI 看门狗', 'U13 看门狗文件就位'),
    @('src/Desktop/BiliCopilot.UI/Controls/Danmaku/VideoDanmakuPanel.xaml.cs', '弹幕：渲染器', 'U26 日志记录弹幕渲染器与开关状态'),
    @('src/Desktop/BiliCopilot.UI/ViewModels/Core/PlayerViewModel/PlayerViewModel.cs', 'OnCurrentVolumeChanged', 'U28 音量变化即持久化'),
    @('src/Desktop/BiliCopilot.UI/ViewModels/Items/SearchSuggestItemViewModel.cs', '[GeneratedBindableCustomProperty]', 'U24 联想 VM 支持运行时绑定'),
    @('src/Desktop/BiliCopilot.UI/Controls/Components/AppSearchBox.xaml', 'TextMemberPath="SearchContent"', 'U21 联想选中后写回输入框'),
    @('src/Desktop/BiliCopilot.UI/Controls/Components/AppSearchBox.xaml', 'UpdateTextOnSelect="True"', 'U22 联想可选中'),
    @('src/Desktop/BiliCopilot.UI/Toolkits/UiStallWatchdog.cs', '其中 GC 暂停', 'U23 卡顿日志带 GC 暂停增量'),
    @('src/Desktop/BiliCopilot.UI/Pages/Overlay/SearchPage.xaml', '<comment:CommentOverlayPanel', 'C17 搜索页评论浮层'),
    @('src/Desktop/BiliCopilot.UI/Controls/Core/PlayerInteractivePanel.xaml', 'x:Name="PlayerNodeItem"', 'P11 播放节点子菜单就位'),
    @('src/Desktop/BiliCopilot.UI/Resources/zh-Hans-CN/Resources.resw', '<data name="PlayerNode"', 'P12 播放节点文案'),
    @('src/Desktop/BiliCopilot.UI/Resources/zh-Hans-CN/Resources.resw', '<data name="NodeDefault"', 'P13 播放节点默认项文案'),
    @('src/Desktop/BiliCopilot.UI/Resources/zh-Hans-CN/Resources.resw', '<data name="NodeAllMirrors"', 'P20 「全部 CDN 线路」文案'),
    @('src/Desktop/BiliCopilot.UI/Resources/zh-Hans-CN/Resources.resw', '<data name="NodeAutoSelection"', 'P21 自动择优设置文案'),
    @('src/Desktop/BiliCopilot.UI/Controls/Settings/PlayerBehaviorSettingControl.xaml', 'x:Bind ViewModel.AutoSelectFastestNode, Mode=TwoWay', 'P22 自动择优开关挂在播放器行为设置里'))) {
    if (-not (Select-String -Path $item[0] -SimpleMatch -Pattern $item[1])) {
        Write-Host "MISSING ANCHOR: $($item[2])"
        $fail = 1
    }
}

# 快捷键设置控件必须挂在设置页上（XAML 不在 Check 的搜索范围内，单独查）
if (-not (Select-String -Path 'src/Desktop/BiliCopilot.UI/Pages/SettingsPage.xaml' -SimpleMatch -Pattern '<settings:ShortcutSettingControl />')) {
    Write-Host 'MISSING ANCHOR: S4 快捷键设置控件没挂在设置页上'
    $fail = 1
}

# 反向检查：不该再向 mpv 发送没有绑定的 ESC（只看代码行，注释里提到不算）
$staleEsc = (Get-ChildItem -Path src -Recurse -Filter *.cs |
             Select-String -Pattern '^\s*(await )?Client!?\.SendKeyPressAsync\("ESC"\)').Count
if ($staleEsc -gt 0) {
    Write-Host 'UNEXPECTED: 仍在向 mpv 发送没有绑定的 ESC（见 FORK.md F5）'
    $fail = 1
}

# 反向检查：ESC 不该再被「只在连接中/加载中才收全屏」的条件挡住（见 FORK.md F7）
$staleEscGuard = (Get-ChildItem -Path src -Recurse -Filter *.cs |
                  Select-String -SimpleMatch -Pattern '(IsConnecting || Player.IsLoading) && Window is not null').Count
if ($staleEscGuard -gt 0) {
    Write-Host 'UNEXPECTED: ESC 又被「连接中/加载中」的条件限制住了（见 FORK.md F7）'
    $fail = 1
}

# 反向检查：视频卡片模板里不应再出现「经典绑定到命令」（取不到 [RelayCommand] 生成的属性）
$staleCmd = (Get-ChildItem -Path src -Recurse -Filter *.xaml |
             Select-String -SimpleMatch -Pattern 'Path=ViewModel.ToggleLikeCommand', 'Path=ViewModel.ShowCommentCommand').Count
if ($staleCmd -gt 0) {
    Write-Host 'UNEXPECTED: 卡片模板里又出现了经典绑定到命令（见 FORK.md C1）'
    $fail = 1
}

# 反向检查：当前 VOD 弹幕契约没有用户等级，禁止静默加入阈值设置/过滤入口
$dmFilterStale = (Get-ChildItem -Path src -Recurse -Filter *.cs |
                 Select-String -Pattern 'SettingNames\.DanmakuMinimumUserLevel|Func<DanmakuInformation, int\?>\? userLevelSelector').Count
if ($dmFilterStale -gt 0) {
    Write-Host 'UNEXPECTED: 未经产品决策就把 weight 当成用户等级过滤（见 docs/danmaku.md）'
    $fail = 1
}
# 反向检查：换线必须走 ReplayAndVerifyAsync。全树只剩「切清晰度」那一条合法的带进度重播，
# 多出一条就说明某条换线路径绕过了校验/回滚（见 FORK.md F12）。
$replayPos = (Get-ChildItem -Path src -Recurse -Filter *.cs |
              Select-String -SimpleMatch -Pattern 'await Player.ReplayAsync(Player.Position);').Count
if ($replayPos -ne 1) {
    Write-Host "UNEXPECTED: 带进度重播出现 $replayPos 处（应只有切清晰度 1 处），换线路径绕过了 ReplayAndVerifyAsync（见 FORK.md F12）"
    $fail = 1
}

# 反向检查：NVDEC 分支不得再配 d3d11 后端（cuda 硬解只允许 opengl/vulkan，见 FORK.md F10）。
# 分支横跨多行，而 Select-String 是逐行匹配的（多行正则永远不命中），所以对整块文本用 .NET 正则。
$nvdecPattern = 'PreferDecodeType\.NVDEC\)\s*\{(?:[^{}]|\{[^{}]*\})*?GpuApiType\.D3D11'
$nvdecD3d11 = @(Get-ChildItem -Path src -Recurse -Filter *.cs | Where-Object {
    [regex]::IsMatch((Get-Content $_.FullName -Raw), $nvdecPattern)
}).Count
if ($nvdecD3d11 -gt 0) {
    Write-Host 'UNEXPECTED: NVDEC 分支又配了 d3d11 后端（见 FORK.md F10）'
    $fail = 1
}

if ($fail -eq 0) {
    Write-Host 'fork_check: OK (all anchors present)'
    Write-Host '提醒：去推荐化已在代码里强制（改设置也放不回来），详见 FORK.md。'
}
else {
    Write-Host 'fork_check: FAILED —— 补丁可能在同步/合并中丢失，请按 FORK.md 的锚点重放 recipe 处理。'
}
exit $fail
