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

## 补丁清单（5 文件 / +36 −6）

| 编号 | 文件 | 改动 |
|---|---|---|
| G4 | `src/Desktop/BiliCopilot.UI/Toolkits/DeRecommendToolkit.cs`（**新增**，30 行） | 编译期开关 `Disabled` + `IsEmptyMoment()` 判据。放在 `BiliCopilot.UI.Toolkits` 命名空间，因为下面 4 个文件本来就 `using` 了它——**不新增任何 using 行** |
| G1 | `.../ViewModels/Core/VideoConnectorViewModel/VideoConnectorViewModel.Methods.cs` | 播放页不再把「推荐」区块加进 sections（1 行） |
| G2 | `.../ViewModels/Components/SearchBoxViewModel/SearchBoxViewModel.cs` | 热搜请求在发起前就被拦掉（1 行） |
| G3 | `.../ViewModels/Items/MomentUperSectionViewModel/MomentUperSectionViewModel.cs`（3 处）与 `.../VideoMomentSectionDetailViewModel/VideoMomentSectionDetailViewModel.cs`（1 处） | 动态流丢弃「无内容」注入条目（4 处 `.Where`） |

`DeRecommendToolkit.IsEmptyMoment()` 的三条判据必须同时成立才丢：`MomentType is null or Unsupported`、`Data is null`、`Description is null`。这样纯文本动态（带 Description）不会被误伤——只按 `Data is null` 过滤会连纯文本一起丢。

## 运行时设置配方（零代码的那一半，必须在应用里手动做一次）

11 个官方推流面里，**6 个整页级推流面根本不需要改代码**——上游自带开关：

1. 设置 →「侧边导航栏设置」，把 **流行 / 视频 / 直播 / 番剧 / 影视 / 专栏** 全部关掉，**只留「动态」**；
2. 顺手把「搜索推荐」（`ShowSearchRecommend`）也关掉，搜索框空输入时就不会再列推荐词。

关掉导航项之后那些页面不会被构造，对应的 ViewModel 也就不会被解析，接口自然不发请求（落地页会自动回落到第一个可见项＝动态）。

> 这些开关存在应用的 LocalSettings 里，**卸载重装会丢**，重装后照上面再关一次即可（约 30 秒）。

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
