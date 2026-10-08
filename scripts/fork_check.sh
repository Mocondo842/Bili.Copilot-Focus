#!/usr/bin/env bash
# 去推荐化补丁锚点自检（同步上游之后运行；缺失任一锚点即退出 1）
#
# 设计要点：**不绑定文件路径**。上游确实搬过文件（v2.2505.2.0→v2.2509.0.0 之间
# VideoPlayerPageViewModel 变成 VideoConnectorViewModel），所以这里在 src/ 全树里
# 找锚点文本，只回答「补丁还在不在」。
#
# 本脚本是文本级检查，不是编译器：类型层是否仍然成立，由 Windows 上的构建负责。
set -u
cd "$(dirname "$0")/.." || exit 1

fail=0
check() { # $1=锚点文本  $2=期望出现次数  $3=说明
  n=$(grep -rF --include=*.cs -- "$1" src 2>/dev/null | wc -l)
  if [ "$n" -lt "$2" ]; then
    echo "MISSING ANCHOR ($n<$2): $3"
    fail=1
  fi
}

# G4 新增文件（其余 4 处改动都依赖它）
[ -f src/Desktop/BiliCopilot.UI/Toolkits/DeRecommendToolkit.cs ] \
  || { echo "MISSING FILE: src/Desktop/BiliCopilot.UI/Toolkits/DeRecommendToolkit.cs"; fail=1; }

# fork 新增的快捷键文件
[ -f src/Desktop/BiliCopilot.UI/Toolkits/PlayerShortcutToolkit.cs ] \
  || { echo "MISSING FILE: src/Desktop/BiliCopilot.UI/Toolkits/PlayerShortcutToolkit.cs"; fail=1; }
[ -f src/Desktop/BiliCopilot.UI/Controls/Settings/ShortcutSettingControl.xaml ] \
  || { echo "MISSING FILE: src/Desktop/BiliCopilot.UI/Controls/Settings/ShortcutSettingControl.xaml"; fail=1; }
[ -f src/Desktop/BiliCopilot.UI/Controls/Components/DescriptionButton.xaml ] \
  || { echo "MISSING FILE: src/Desktop/BiliCopilot.UI/Controls/Components/DescriptionButton.xaml"; fail=1; }

check "!DeRecommendToolkit.Disabled && _view.Recommends is not null" 1 "G1 播放页推荐区块守卫"
check "DeRecommendToolkit.Disabled || HotSearchItems.Count > 0"      1 "G2 热搜请求守卫"
check "!DeRecommendToolkit.IsEmptyMoment(p)"                         4 "G3 动态空条目过滤（4 处）"
check "!DeRecommendToolkit.IsHiddenPage(typeof(TPage))"              1 "G5 导航项强制隐藏（不读设置）"
check "DeRecommendToolkit.Disabled || !isRecommendEnabled"           1 "G7 搜索推荐词强制不请求"
check "DeRecommendToolkit.IsHiddenPage(pageType)"                    2 "G8 推流页可见性写入口拦截（导航 + 设置页）"
check "Players.Any(p => p.Window is not null)"                       1 "F1 关窗规则（仅独立播放器窗口时收托盘）"
check "SettingNames.PlayWithoutP2P, true"                            2 "F4 默认避开 PCDN 节点（两个 resolver）"
check "AppWindow.Show();"                                            2 "F3 单实例/托盘唤回窗口"
check "PlayerShortcutToolkit.TryMatch(args.VirtualKey, out var action)" 1 "S1 播放器按键改走快捷键表"
check "PlayerShortcutToolkit.Get(PlayerShortcutAction.SkipForward)"   1 "S2 长按三倍速跟随「快进」的绑定"
check "PlayerShortcutToolkit.Set(action, shortcut)"                   1 "S3 设置页写入新绑定"
check "AppToolkit.IsCodecMatch("                                      2 "S5 编码偏好按编码族匹配（两个 resolver）"
check "IsSubtitleEnabled = !IsSubtitleEnabled;"                       1 "S6 字幕开关快捷键只翻转开关"
check "ApplySubtitleEnabledAsync(value)"                              1 "S7 字幕显隐走单独的应用路径"
check "PlayerShortcutToolkit.GetDescription(action)"                  1 "S8 每个动作一条次级菜单（带描述）"
check "ToggleVideoLikeAsync(Data.Identifier.Id, state)"               1 "C1 视频卡片点赞按钮走真实点赞接口"
check "showCommentAction: _showCommentAction"                         2 "C2 搜索卡片带评论动作（首屏 + 翻页）"
check "newSection.SetShowCommentAction(ShowComment)"                  1 "C3 搜索页把评论面板接到视频分区"
check "showCommentAction: ShowVideoComment"                           1 "C4 用户空间视频搜索卡片带评论动作"
check "Richasy.BiliKernel.Models.CommentTargetType.Video"             2 "C5 评论面板按视频初始化（搜索页 + 用户空间）"
check "GetVideoPageDetailAsync(new MediaIdentifier(Data.Identifier.Id" 1 "C6 简介按需取回（与播放页同一接口）"
check "_descriptionTask ??= LoadDescriptionAsync();"                  1 "C7 简介取回任务单飞（并发点击只发一次）"
check "_likeButton.Command = ViewModel?.ToggleLikeCommand;"           1 "C8 点赞命令在 code-behind 赋值"
check "_commentButton.Command = ViewModel?.ShowCommentCommand;"       1 "C9 评论命令在 code-behind 赋值"
check "ReferenceEquals(video, ViewModel)"                             1 "C10 取回后校验按钮是否已被列表回收"
check "public VideoItemViewModel? InnerVideo"                         1 "C11 用户空间视频卡片复用内层视频取简介"
check "Interlocked.Exchange(ref _stallStartTicks, ackTicks);"          1 "U1 卡顿起点取最近一次 UI 回调时刻"
check "UI 线程恢复响应，本次卡顿累计约"                                  1 "U2b 恢复后补报总时长"
check "UI 看门狗已启动"                                                  1 "U2 看门狗启动有日志（没有卡顿行也可解释）"
check "UiStallWatchdog.Start();"                                      1 "U3 启动时拉起看门狗"
check "UiStallWatchdog.Stop();"                                       1 "U4 退出时停看门狗"
check "WriteTo.Async(a => a.File("                                     1 "U5 日志异步落盘（不在调用线程写盘）"
check "IsCardAnimationEnabled"                                        2 "U6 卡片动画开关接入启动路径"
check "AppToolkit.GetCulture(primaryLan)"                              2 "U7 区域性对象按语言缓存（两条目路径）"
check "EmojiRegex"                                                    2 "U8 表情正则提为静态"

# 卡片与评论改造的 XAML 锚点（XAML 不在 check 的搜索范围内，单独查）
xcheck() { # $1=文件  $2=锚点文本  $3=说明
  grep -qF -- "$2" "$1" || { echo "MISSING ANCHOR: $3"; fail=1; }
}
xcheck src/Desktop/BiliCopilot.UI/Controls/Components/VideoCardControl/VideoCardControl.xaml 'x:Name="LikeButton"' "C12 搜索卡片点赞按钮（供 code-behind 取）"
xcheck src/Desktop/BiliCopilot.UI/Controls/Components/VideoCardControl/VideoCardControl.xaml 'x:Name="CommentButton"' "C13 搜索卡片评论按钮（供 code-behind 取）"
xcheck src/Desktop/BiliCopilot.UI/Controls/Components/VideoCardControl/VideoCardControl.xaml '<local:DescriptionButton' "C14 搜索卡片简介按钮"
xcheck src/Desktop/BiliCopilot.UI/Controls/Components/MomentCardControl/PersonalVideoMomentPresenter.xaml 'ViewModel.InnerVideo' "C15 用户空间卡片简介按钮"
xcheck src/Desktop/BiliCopilot.UI/Controls/Comment/CommentItemControl.xaml 'MaxLines="0"' "C16 评论不截断"
xcheck src/Desktop/BiliCopilot.UI/Pages/Overlay/SearchPage.xaml '<comment:CommentOverlayPanel' "C17 搜索页评论浮层"
xcheck src/Directory.Packages.props 'Version="1.8.260921001"' "U9 WindowsAppSDK 升到 1.8 同线最新"
xcheck src/Directory.Packages.props 'Serilog.Sinks.Async' "U10 异步日志包已声明"
xcheck src/Desktop/BiliCopilot.UI/BiliCopilot.UI.csproj 'Serilog.Sinks.Async' "U11 异步日志包已被引用"
xcheck src/Desktop/BiliCopilot.UI/App.xaml.cs 'Log.CloseAndFlush();' "U12 退出前刷日志队列"
xcheck src/Desktop/BiliCopilot.UI/Toolkits/UiStallWatchdog.cs 'UI 看门狗' "U13 看门狗文件就位"

# 快捷键设置控件必须挂在设置页上（XAML 不在 check 的搜索范围内，单独查）
grep -qF '<settings:ShortcutSettingControl />' src/Desktop/BiliCopilot.UI/Pages/SettingsPage.xaml \
  || { echo "MISSING ANCHOR: S4 快捷键设置控件没挂在设置页上"; fail=1; }

# 反向检查：不该再向 mpv 发送没有绑定的 ESC（只看代码行，注释里提到不算）
if grep -rEn --include=*.cs '^[[:space:]]*(await )?Client!?\.SendKeyPressAsync\("ESC"\)' src >/dev/null 2>&1; then
  echo "UNEXPECTED: 仍在向 mpv 发送没有绑定的 ESC（见 FORK.md F5）"
  fail=1
fi

# 反向检查：ESC 不该再被「只在连接中/加载中才收全屏」的条件挡住（见 FORK.md F7）
if grep -rF --include=*.cs -- '(IsConnecting || Player.IsLoading) && Window is not null' src >/dev/null 2>&1; then
  echo "UNEXPECTED: ESC 又被「连接中/加载中」的条件限制住了（见 FORK.md F7）"
  fail=1
fi

# 反向检查：视频卡片模板里不应再出现「经典绑定到命令」（取不到 [RelayCommand] 生成的属性）
if grep -rF --include=*.xaml 'Path=ViewModel.ToggleLikeCommand' src >/dev/null 2>&1 \
   || grep -rF --include=*.xaml 'Path=ViewModel.ShowCommentCommand' src >/dev/null 2>&1; then
  echo "UNEXPECTED: 卡片模板里又出现了经典绑定到命令（见 FORK.md C1）"
  fail=1
fi

if [ "$fail" = 0 ]; then
  echo "fork_check: OK (all anchors present)"
  echo "提醒：去推荐化已在代码里强制（改设置也放不回来），详见 FORK.md。"
else
  echo "fork_check: FAILED —— 补丁可能在同步/合并中丢失，请按 FORK.md 的锚点重放 recipe 处理。"
fi
exit "$fail"
