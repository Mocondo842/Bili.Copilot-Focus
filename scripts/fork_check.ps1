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
    'src/Desktop/BiliCopilot.UI/Controls/Components/DescriptionButton.xaml')) {
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
Check 'VideoExtensionDataId.Description)' 1 'C6 视频卡片读简介扩展字段'

# 卡片与评论改造的 XAML 锚点（XAML 不在 Check 的搜索范围内，单独查）
foreach ($item in @(
    @('src/Desktop/BiliCopilot.UI/Controls/Components/VideoCardControl/VideoCardControl.xaml', 'ViewModel.ToggleLikeCommand', 'C7 搜索卡片点赞按钮'),
    @('src/Desktop/BiliCopilot.UI/Controls/Components/VideoCardControl/VideoCardControl.xaml', '<local:DescriptionButton', 'C8 搜索卡片简介按钮'),
    @('src/Desktop/BiliCopilot.UI/Controls/Components/MomentCardControl/PersonalVideoMomentPresenter.xaml', 'Symbol="Feed"', 'C9 用户空间卡片简介按钮'),
    @('src/Desktop/BiliCopilot.UI/Controls/Comment/CommentItemControl.xaml', 'MaxLines="0"', 'C10 评论不截断'),
    @('src/Desktop/BiliCopilot.UI/Pages/Overlay/SearchPage.xaml', '<comment:CommentOverlayPanel', 'C11 搜索页评论浮层'))) {
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

if ($fail -eq 0) {
    Write-Host 'fork_check: OK (all anchors present)'
    Write-Host '提醒：去推荐化已在代码里强制（改设置也放不回来），详见 FORK.md。'
}
else {
    Write-Host 'fork_check: FAILED —— 补丁可能在同步/合并中丢失，请按 FORK.md 的锚点重放 recipe 处理。'
}
exit $fail
