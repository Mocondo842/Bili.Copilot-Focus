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

Check '!DeRecommendToolkit.Disabled && _view.Recommends is not null' 1 'G1 播放页推荐区块守卫'
Check 'DeRecommendToolkit.Disabled || HotSearchItems.Count > 0' 1 'G2 热搜请求守卫'
Check '!DeRecommendToolkit.IsEmptyMoment(p)' 4 'G3 动态空条目过滤（4 处）'
Check '!DeRecommendToolkit.IsHiddenPage(typeof(TPage))' 1 'G5 导航项强制隐藏（不读设置）'
Check 'DeRecommendToolkit.Disabled || !isRecommendEnabled' 1 'G7 搜索推荐词强制不请求'
Check 'Players.Any(p => p.Window is not null)' 1 'F1 关窗规则（仅独立播放器窗口时收托盘）'
Check 'SettingNames.PlayWithoutP2P, true' 2 'F4 默认避开 PCDN 节点（两个 resolver）'
Check 'AppWindow.Show();' 2 'F3 单实例/托盘唤回窗口'

if ($fail -eq 0) {
    Write-Host 'fork_check: OK (all anchors present)'
    Write-Host '提醒：去推荐化已在代码里强制（改设置也放不回来），详见 FORK.md。'
}
else {
    Write-Host 'fork_check: FAILED —— 补丁可能在同步/合并中丢失，请按 FORK.md 的锚点重放 recipe 处理。'
}
exit $fail
