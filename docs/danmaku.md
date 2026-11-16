# 弹幕体验

## 当前 VOD 弹幕字段

`Richasy.BiliKernel.Abstractions 0.0.2-preview1` 的 VOD `DanmakuInformation` 仅包含 `Id`、`Content`、`Mode`、`StartPosition`、`Color`、`FontSize`；播放器主链路通过 `IDanmakuService.GetSegmentDanmakusWithGrpcAsync` 取得这些数据。该封装模型没有按用户等级字段，也没有 `weight`。

固定版本 BiliKernel 使用的 `DanmakuElem` protobuf 定义了 `weight = 9`（int32，wire type 0）；独立的公开协议备份把它注释为「用于智能屏蔽，AI 根据语义及长度得出的权重，范围 0–10，值越大权重越高」。它不是用户等级。`player_danmaku_ai_recommended_level` / `ai_level` 属于播放器配置阈值，也不是每条弹幕的等级。

## 只读探针

本 fork 每次播放的 `InitializeDanmakuAsync` 只对第一个 6 分钟分段额外读取一次 `/x/v2/dm/web/seg.so`。它复用应用注册的 `BiliAuthenticator` / `BiliHttpClient` 做 Web 签名（含 WBI/Cookie），解析外层字段 1 的弹幕元素，记录字段 1（ID）和字段 9（weight），输出 HTTP 状态、字节数、字段覆盖率、值分布、未知字段、与主链路 ID 的对应关系及失败原因。

探针只写日志，不修改弹幕列表、不缓存元数据、不设置阈值、不提供 UI、不参与两种渲染器；字段缺省按 proto3 默认值处理并记录为 `absentWeightCount`，不把 0 当作等级。Cookie、Token 和查询签名不写入日志。

## 功能状态与前提

**按用户等级屏蔽未实现**。当前 API 封装没有用户等级，且没有找到可验证的每条弹幕用户等级字段；`weight` 的语义是 AI 智能屏蔽权重。仅当未来数据源提供可验证等级、并另行确认阈值语义后，才考虑增加过滤。

探针的字段编号是固定版本源码与公开协议备份共同验证的；其运行时分布、登录/匿名差异以及 ID 对应关系仍需真机日志验证。验证记录放在工作区 `research/80-phase2-acceptance.md`，不把「字段存在」当成「等级可用」。