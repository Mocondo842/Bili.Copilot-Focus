# PM-BiliFocus-优化R1-v1（哔哩助理·去推荐化分支 · plan.md 六项优化）

- **项目**：[Bili.Copilot-Focus](https://github.com/Mocondo842/Bili.Copilot-Focus)（上游 Richasy/Bili.Copilot 的去推荐化 fork，分支 `focus/derec`）
- **日期**：2026-10-25
- **输入**：`plan.md`（6 项具体事项）+ `log-2026-10-11.txt` ~ `log-2026-10-25.txt`（13 天运行日志，共约 2.7MB）
- **方法论**：按 `PM.md` 六字段骨架编写；所有结论均附日志/代码出处，未写处一律进「待确认留白」

---

## 1️⃣ 问题与目标（为什么做）

> 编号 PB1–PB6 对应 `plan.md` 具体事项 1–6。事实与数字全部来自 13 天日志的聚类统计与源码定位。

| # | 问题（谁遇到什么麻烦，代价是什么） | 日志/代码证据 | 本轮目标（可判定） |
|---|---|---|---|
| **PB1** | 高清晰度（竖屏 1080p / 横屏 4K）偶发起播慢或打不开，用户只能降清晰度 | 4 次 `Failed to open` **全部指向 `mcdn.bilivideo.cn:8082`（PCDN 节点）**（10-21:43、10-23:31、10-24:996）；10-21/10-23 出现完整重试链「SSL 失败 → 1ms 内重选同参数 → 再失败 → 降清晰度 → Packet corrupt」；`Failure when receiving data from the peer` ×23 | 用户能在播放中右键**换节点重播**；候选全为 PCDN 时日志有明确告警行 |
| **PB2** | 评论列表图片经常部分无法加载预览，也无法点击查看；但点开大图窗口能看到 | 1601 条图片失败日志：**98.7% 是 TaskCanceledException（取消，非网络失败）**，90.8% 集中在同一秒批量出现（回收导致），真网络失败仅 18 条 404；根因：`EmoteTextBlock.OnControlUnloaded()` 清空九宫格数据源后，重新入树时 `Text` 未变化、`OnTextChanged` 不再触发 → 图片永久空白 | 列表回收后重新滚回的评论图自动恢复显示，无需进出页面 |
| **PB3** | 评论图片正常加载后，点「查看回复」回来，主楼图片又消失 | 与 PB2 同根因：查看回复时主列表整体折叠（`CommentMainPanel.xaml:59-80`），返回后同 VM 重新可见但数据源已被清空 | 同 PB2（同一处修复覆盖） |
| **PB4** | 没有「更换播放节点」的设置，播放中也无法换节点 | 选流代码 `VideoMediaSourceResolver.GetPlayUrls()` 直接取 `BaseUrl`，BackupUrls 仅在「不使用P2P」开启时用于避让 PCDN；无任何节点选择 UI | 播放器右键菜单出现「播放节点」子菜单：列出当前视频候选节点、标出当前节点、点击即按记忆进度重播 |
| **PB5** | 播放视频 A 时点开视频 B，A 窗口继续播放（两个视频同时出声） | `PlayerViewModel` 注册为 **Transient**（`GlobalDependencies.cs:128`），`AppViewModel.OpenPlayerAsync()` 每次新建 `PlayerWindow`，**全程无停止旧播放的逻辑**；旧 VM 仅在窗口被手动关闭时释放 | 打开新视频时，已存在的播放器连窗口一起自动退出 |
| **PB6** | 点设置偶发卡顿几十秒（页面冻结、覆盖显示） | 日志中与 SettingsPage 相关的卡顿**仅 2 条、约 1 秒、均为冷启动**（10-13:369、10-15:6）；「几十秒级」数值经反推**全部是看门狗启动误报**（13 天 19 条，8 位数毫秒 ≈ 开机以来时长）——`UiStallWatchdog.cs:77` 把基准初始化成了绝对时间戳而非延迟量，污染了日志 | 消除启动误报，让后续日志可信；「覆盖显示几十秒」本身因无日志证据，列入待确认（见 §8-Q1） |

**一句话目标**：把「偶发、无法自救」的播放与图片问题，变成「用户 3 秒内可自救（换节点）+ 系统自动恢复（图片）+ 日志可信（看门狗）」。

---

## 2️⃣ 范围与非范围（做到哪里停）

**Must（本期必须）**

| 编号 | 内容 | 对应问题 |
|---|---|---|
| M1 | `EmoteTextBlock` 入树时按当前 `Text` 恢复九宫格数据源 | PB2/PB3 |
| M2 | `AppViewModel.OpenPlayerAsync` 先退出已存在的播放器再开新的 | PB5 |
| M3 | 播放节点：候选收集（两个 resolver）+ 右键「播放节点」子菜单 + 手选重播 | PB4/PB1 |
| M4 | 全 PCDN 候选时写 WARN 日志（可观测性） | PB1 |
| M5 | 看门狗基准初始化修复（首 tick 不误报） | PB6 |
| M6 | 版本号抬升 + FORK.md 补丁清单登记 + 锚点守卫新增项全过 | 工程纪律 |

**Not now（本期明确不做）**

| 编号 | 内容 | 理由 |
|---|---|---|
| N1 | 节点偏好的跨视频记忆 / 持久化设置项 | 语义未定（按视频？按主机族？），见 §8-Q2，先不猜 |
| N2 | mpv 层的自动重试 / 自动换源 | 在 NuGet 包 `Richasy.MpvKernel.Player` 内，仓库改不到；本轮给用户手动自救入口 |
| N3 | 「设置页卡顿几十秒」的根因修复 | 无日志证据支撑定位（见 §8-Q1），先修观测再谈修复 |
| N4 | 弹幕分片 gRPC 失败（10-17 单日 2043 条，集中在网络中断窗口）与未处理 `ArgumentException`（10-17 两条） | 记录在案，另行立项 |
| N5 | 大图窗口（GalleryWindow）的加载观测埋点 | 与 PB2 修复解耦，验证后视需要再加 |

---

## 3️⃣ 用户故事（谁，在什么场景下，要什么）

- **BUS1**：作为一个在弱网/PCDN 节点下看 4K 的用户，我希望播放中右键就能换一个节点，以便不用退出重进或被迫降清晰度。
- **BUS2**：作为一个刷评论的用户，我希望评论里的图片在列表滚走再滚回来后仍然显示，以便不用每张图都点开大图看。
- **BUS3**：作为一个连续看视频的用户，我希望点开新视频时上一个播放窗口自动退出，以便不让两个视频同时出声。

---

## 4️⃣ 业务规则（条件、动作、边界值）

| 编号 | 规则（如果…那么…） |
|---|---|
| **BR1** | 节点候选 = 当前视频、当前清晰度、选中编码段的 `[BaseUrl] + BackupUrls`，按 URL 主机名去重、BaseUrl 在前；候选为空时右键「播放节点」子菜单禁用 |
| **BR2** | 选择优先级：**已手选节点 > 「不使用P2P」过滤 > BaseUrl**。即：已手选 → 用该主机的第一个候选；未手选且设置开 → 第一个非 PCDN 候选，全 PCDN 则回退 BaseUrl 并写 WARN；未手选且设置关 → BaseUrl（与现行为一致） |
| **BR3** | 手选节点在**当前视频内保留**（切清晰度、重播仍生效）；换视频（resolver `Initialize`）时重置为默认 |
| **BR4** | 选节点 = 以当前播放进度重播（`ReplayAsync(position)`），与切换清晰度走同一通路；音频轨仍按「不使用P2P」规则独立避让 |
| **BR5** | 打开新视频前，对 `Players` 中每个已存在的播放器执行完整 `DisposeAsync`（停播放 → 关窗口 → 移出集合）；集合为空时行为与之前完全一致 |
| **BR6** | `EmoteTextBlock` 在 `Loaded`（重新入树）时，若当前 `Text.Pictures` 非空且 Gallery 已激活，则重设 `ItemsSource` 并显示；`Text` 为空或无图时不动作 |
| **BR7** | 看门狗 `_ackLatencyTicks` 初始值为 0：首 tick 不报告也不进微卡顿统计；真实延迟由首 tick 投递的回调写入、次 tick 读出 |
| **BR8** | 以上改动不得破坏既有锚点：`fork_check.ps1` 全部锚点（含 `SettingNames.PlayWithoutP2P, true` ×2、`AppToolkit.IsCodecMatch(` ×2）必须原样命中 |

---

## 5️⃣ 验收标准（怎么算"完成了"）

| 编号 | Given-When-Then |
|---|---|
| **BS1** | **Given** 播放中的视频有 ≥2 个候选节点，**When** 右键 → 播放节点 → 选另一个节点，**Then** 播放从当前进度重新起播，日志出现节点选中记录，菜单勾选随新节点移动 |
| **BS2** | **Given** 未手选节点且「不使用P2P」为开，**When** 起播，**Then** 选流行为与本期之前一致（优先非 PCDN）；为关时直接用 BaseUrl |
| **BS3** | **Given** 一条含 3 图的评论曾被滚出可视区（回收），**When** 滚回该评论，**Then** 九宫格 3 图重新显示，无需进出页面 |
| **BS4** | **Given** 独立播放器窗口正在播放视频 A，**When** 在主窗口点开视频 B，**Then** A 的窗口关闭且播放停止，B 正常起播 |
| **BS5** | **Given** 应用启动后看门狗运行满 1 分钟，**When** 查看日志，**Then** 不再出现 8 位数毫秒的「UI 线程卡顿」误报与配套恢复行；后续卡顿记录均为真实值 |
| **BS6** | **Given** 代码改动完成，**When** 运行 `scripts/fork_check.ps1`，**Then** 输出 `fork_check: OK`（新增锚点全过） |

---

## 6️⃣ 异常与边界清单

| 编号 | 场景 | 预期行为 |
|---|---|---|
| E1 | 手选的主机在切换清晰度后的候选里不存在 | 忽略手选、按 BR2 默认规则选；菜单按新 CurrentHost 勾选（手选保留，下个清晰度若出现该主机仍生效） |
| E2 | 候选 URL 全部是 PCDN | 用 BaseUrl，写 WARN「节点：清晰度 X 的 N 个候选全部是 PCDN，无法避开」 |
| E3 | 候选 URL 格式非法（Host 解析失败） | `GetHost` 吞 `UriFormatException` 返回 null；该候选不进菜单 |
| E4 | 播放器未起播（`Player` 为 null）时点节点菜单项 | 命令直接返回，不重播 |
| E5 | 旧播放器 `DisposeAsync` 抛异常 | 与用户手动关窗走同一条释放路径，不额外兜底（保持与 `OnWindowDestroying` 一致） |
| E6 | `EmoteTextBlock.Text` 为 null | Loaded 恢复逻辑直接跳过（不 NRE） |
| E7 | Gallery 因 `x:Load="False"` 尚未激活（从未显示过图片） | `Gallery` 为 null，跳过恢复 |
| E8 | 断网时手选节点 | mpv 打不开 → 走现有错误提示路径；本轮不改 mpv 层行为（N2） |
| E9 | 同一 URL 被多个列表项同时持有，重试预算各自独立 | 维持 U19 现状（预算 2 次/实例），不在本轮调整 |

---

## 7️⃣ 轮次与状态

| 轮次 | 内容 | 状态 |
|---|---|---|
| **R1** | 本文档 + M1–M6 代码实施（版本 2.2511.2.917） | 代码就绪，**未经编译验证**（本机无 VS/Windows App SDK，构建依赖 GitHub Actions） |
| **R2** | 真机安装 2.2511.2.917，按 BS1–BS6 验收，回收 1–2 天日志复核（重点：图片失败条数、看门狗误报清零、节点菜单实际可用性） | 待 R1 交付 |

---

## 8️⃣ 待确认留白（素材未覆盖，不自行补充）

| # | 待确认 |
|---|---|
| Q1 | 「设置页与原页面覆盖显示几十秒」在 13 天日志中无对应记录——需要下次复现时记下操作时刻并保留当天 log 文件 |
| Q2 | 节点偏好是否要跨视频记忆（做成设置项）？还是保持「每个视频默认回到自动」？ |
| Q3 | 「自动退出上个视频」要不要留一个「允许多窗口」的开关？ |
| Q4 | 大图窗口用原图 URL、列表用裁剪 URL，两条加载链路是否需要统一观测？ |
| Q5 | 10-17 两条未处理 `ArgumentException: 参数错误。` 的触发操作是什么？ |
| Q6 | 弹幕 gRPC 分片失败（10-17 单日 2043 条）是否需要应用层重试策略？ |

---

### 附：本轮代码改动清单（对应 M1–M6）

| M | 文件 | 改动 |
|---|---|---|
| M1 | `Controls/Components/EmoteTextBlock.xaml.cs` | 构造器挂 `Loaded`：入树时按当前 `Text` 恢复九宫格 |
| M2 | `ViewModels/Core/AppViewModel/AppViewModel.cs` | `OpenPlayerAsync` 先 `DisposeAsync` 已存在的播放器 |
| M3 | `Resolvers/MediaSourceResolverBase.cs` + 两个 resolver + `PlayerViewModel.Commands.cs` + `PlayerInteractivePanel.xaml(.cs)` + resw | 节点候选/手选状态进基类；右键「播放节点」子菜单在菜单打开时动态填充；手选 → 重播 |
| M4 | 两个 resolver 的 `GetPlayUrls` | 全 PCDN 候选写 WARN |
| M5 | `Toolkits/UiStallWatchdog.cs` | `_ackLatencyTicks` 初始值改为 0 |
| M6 | `.github/workflows/fork-build-win-x64.yml`、`FORK.md`、`scripts/fork_check.ps1` | 版本 2.2511.2.917；补丁清单 P 系列；新增锚点 |
