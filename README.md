<p align="center">
<img src="assets/StoreLogo.png" width="48px"/>
</p>

<div align="center">

# 哔哩助理 · Focus（去推荐化分支）

本仓库是 [Richasy/Bili.Copilot](https://github.com/Richasy/Bili.Copilot)（哔哩助理，GPL-3.0）的个人分支，全部改动在 **`focus/derec`** 分支上维护（`master` 保持上游镜像，用于同步）。

</div>

## 概述

本分支的目标：**除主动关注与主动检索外，不呈现官方推流**——去掉「流行（推荐视频/热门/全区排行榜）」「视频分区」「直播」「番剧」「影视」「专栏」这些由官方挑选编排的面，只留下「动态」（你关注的人）、搜索（你自己发起的）、以及收藏/历史/稍后再看这些你自己的数据。

去推荐化在代码里**强制生效**（改设置也放不回来），且做法刻意压到最小：不删任何页面、ViewModel、Service、端点、i18n 与设置项，只改「是否把某一块塞进界面」——上游同步的冲突面几乎为零。

在去推荐化之外，本分支还陆续修掉了一批上游问题（播放器快捷键可配置、避开 PCDN 节点、UI 卡顿看门狗、图片失败预算重试、搜索标题 HTML 实体解码、播放节点右键菜单、打开新视频自动退出旧播放器等）。**完整的补丁清单、设计依据与验证状态见 [FORK.md](FORK.md)**——它是本分支的总账，README 只讲怎么装、怎么下、怎么构建。

## 安装（侧载）

> ⚠️ **不要安装 Microsoft Store 版本**：侧载版与商店版共用 package identity，装上商店版会把本分支覆盖回官方版。

- 系统：Windows 11 x64（x64 侧载包；Windows 10 未验证）
- 安装器会自动请求管理员权限，并用 `certutil` 静默导入随包的自签名证书（`CN=Richasy`，十年有效期）

1. 按下节「下载」取得 `BiliCopilot-Focus_<版本>_x64.zip` 并解压，内含：
   `BiliCopilot.UI_<版本>_x64.msix`、`focus-sideload.cer`、`Install-Focus.cmd`、`Install-Focus.ps1`、`安装说明.txt`
2. 双击 **`Install-Focus.cmd`**（或右键 `Install-Focus.ps1` →「使用 PowerShell 运行」）：
   自动提权 → 导入证书 → 预检并安装依赖（Windows App Runtime）→ 安装主程序；失败时窗口会停住并显示原因
3. 证书信任一次后，后续新版本直接覆盖安装即可

## 下载

**发布策略（Draft-first）**：每次构建的产物进入**草稿发布**——不进公开 Releases 页面、不触发通知；同时自动清理 `focus-*` 历史（含草稿），只保留最近 3 个。公开页面上可见的旧预发布（2.2511.2.917 及更早）会随构建逐步清掉。

**协作者（本机 `gh` 已登录，2.103+ 实测可用）**，一行命令按 tag 下载草稿：

```powershell
gh release download focus-2.2511.2.919 --repo Mocondo842/Bili.Copilot-Focus --pattern "*.zip" --clobber
```

- 当前最新版本号以构建报告为准：[`ci/logs` 分支的 `ci/last-build.md`](https://github.com/Mocondo842/Bili.Copilot-Focus/blob/ci/logs/ci/last-build.md)（含版本、产物清单、sha256 与诊断）
- 备选：仓库 **Actions** 页面对应 run 的 **Artifacts**（需登录后手动下载）
- 非 gh 登录用户（非协作者）：草稿对外完全不可见，无法下载——这是 Draft-first 的设计行为

## 构建

### 自动构建（GitHub Actions，推荐）

推送到 `focus/derec`（`src/**`、`scripts/**` 或 workflow 本身有改动时）自动触发 [Fork Build (Win x64 sideload)](.github/workflows/fork-build-win-x64.yml)，在 `windows-latest` 上完成：

锚点守卫（`fork_check`，补丁丢失即失败）→ 还原依赖 → `msbuild` x64 SideloadOnly 签名打包 → 签名校验（`AppxSignature.p7x` 硬门禁）→ **草稿发布** + 清理旧发布 → 草稿资产 API 回拉 sha256 验收 → 构建报告推送到 `ci/logs` 分支（无论成败）。

也可在 Actions 页面手动触发（Run workflow），可手填版本号（留空用 workflow 里的 `DEFAULT_VERSION`；**每次改动都要上抬**，否则无法覆盖安装旧版本）。

### 本机构建（Windows 11 + Visual Studio）

1. `git clone` 本仓库并切换到 `focus/derec`
2. 下载两个播放组件（上游约定，构建必需）：

   | 文件 | 放置目录 | 来源 |
   | --- | --- | --- |
   | `libmpv-2.dll` | `src\Desktop\BiliCopilot.UI\Assets\libmpv\x64\` | [mpv-winbuild](https://github.com/zhongfly/mpv-winbuild) 最新 dev 构建（x86_64） |
   | `ffmpeg.exe` | `src\Desktop\BiliCopilot.UI\Assets\ffmpeg\` | 同上，ffmpeg x64 构建 |

3. 构建前跑锚点守卫，**必须输出 `fork_check: OK`**：
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\fork_check.ps1
   ```
4. 用 Visual Studio 打开 `src\Bili.Copilot.sln` 构建 x64 Release；命令行 `msbuild` 参数照抄 workflow 的 Build 步骤即可（含 `SideloadOnly`、签名、`APPX0105/APPX0107` 升级为错误等开关）

## 使用

与上游一致（以下仅与本分支差异相关的要点）：

- **登录**：优先扫码登录
- **播放**：MPV 内置播放器；快捷键已可在设置页自定义；播放中右键菜单含**播放节点**切换与着色器等
- **去推荐化行为**：流行/分区/直播/番剧/影视/专栏六个入口不可见也不可改回；搜索推荐词与热搜在请求前被拦截；播放页无推荐区块
- **视频下载**：内置 BBDown 与 ffmpeg，播放页下载按钮即可

上游的完整使用文档（[bili.richasy.net](https://bili.richasy.net)）仍然适用，本分支改动覆盖的部分以 [FORK.md](FORK.md) 为准。

## 文档与问题

- [FORK.md](FORK.md)：补丁总账（每个编号 = 一个问题 + 改动 + 验证状态）、同步上游的 recipe、发布策略
- [plan.md](plan.md)：当前待解决/待复现的问题清单
- 构建状态：[ci/logs 分支 `ci/last-build.md`](https://github.com/Mocondo842/Bili.Copilot-Focus/blob/ci/logs/ci/last-build.md)

## 鸣谢

- [Windows App SDK](https://github.com/microsoft/windowsappsdk)
- [WinUI](https://github.com/microsoft/microsoft-ui-xaml)
- [BiliBili](https://www.bilibili.com/)
- [哔哩哔哩-API 收集整理](https://github.com/SocialSisterYi/bilibili-API-collect)
- [BBDown](https://github.com/nilaoda/BBDown)
- [BewlyBewly](https://github.com/BewlyBewly/BewlyBewly)
- [寒霜弹幕使](https://github.com/cotaku/DanmakuFrostMaster)
- [cnbluefire/WinUI3.Win2D](https://github.com/cnbluefire/WinUI3.Win2D)
- [Windows Community Toolkit](https://github.com/CommunityToolkit/Windows)
- [FluentIcons](https://github.com/davidxuang/FluentIcons)
- [ComputeSharp](https://github.com/Sergio0694/ComputeSharp)
- 以及上游 [Richasy/Bili.Copilot](https://github.com/Richasy/Bili.Copilot) 的作者与所有贡献者

本仓库沿用上游的 **GPL-3.0** 许可。
