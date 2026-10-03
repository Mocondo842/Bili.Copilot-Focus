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

check "!DeRecommendToolkit.Disabled && _view.Recommends is not null" 1 "G1 播放页推荐区块守卫"
check "DeRecommendToolkit.Disabled || HotSearchItems.Count > 0"      1 "G2 热搜请求守卫"
check "!DeRecommendToolkit.IsEmptyMoment(p)"                         4 "G3 动态空条目过滤（4 处）"
check "!DeRecommendToolkit.IsHiddenPage(typeof(TPage))"              1 "G5 导航项强制隐藏（不读设置）"
check "DeRecommendToolkit.Disabled || !isRecommendEnabled"           1 "G7 搜索推荐词强制不请求"
check "Players.Any(p => p.Window is not null)"                       1 "F1 关窗规则（仅独立播放器窗口时收托盘）"
check "SettingNames.PlayWithoutP2P, true"                            2 "F4 默认避开 PCDN 节点（两个 resolver）"
check "AppWindow.Show();"                                            2 "F3 单实例/托盘唤回窗口"

if [ "$fail" = 0 ]; then
  echo "fork_check: OK (all anchors present)"
  echo "提醒：去推荐化已在代码里强制（改设置也放不回来），详见 FORK.md。"
else
  echo "fork_check: FAILED —— 补丁可能在同步/合并中丢失，请按 FORK.md 的锚点重放 recipe 处理。"
fi
exit "$fail"
