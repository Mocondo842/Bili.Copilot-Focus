// Copyright (c) Bili Copilot. All rights reserved.

using Richasy.MpvKernel;

namespace BiliCopilot.UI.Toolkits;

/// <summary>
/// 受控错误换线的归因与限流：只放行打开/网络类 mpv 错误，每个视频最多换线一次，30 秒内不重复测速。
/// </summary>
internal static class NodeRecoveryPolicy
{
    /// <summary>
    /// 两次错误换线之间的最短间隔；同一次网络抖动里连续报错不再重复测速。
    /// </summary>
    public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 判断该错误是否可归因到线路打不开。解码、音/视频输出、格式与参数错误一律不走换线。
    /// </summary>
    public static bool IsNodeOpenError(MpvError error, double duration)
        => error switch
        {
            MpvError.LoadingFailed
                or MpvError.TlsError
                or MpvError.Http403
                or MpvError.Http404
                or MpvError.Http500 => duration < 1,
            MpvError.Generic => duration < 1,
            _ => false,
        };

    /// <summary>
    /// 当前是否满足一次错误换线；通过后由调用方立即消费次数配额。
    /// </summary>
    public static bool CanRecover(bool consumed, DateTimeOffset lastAttempt, DateTimeOffset now, bool hasManualNode, bool isTesting)
        => !consumed && !hasManualNode && !isTesting && (lastAttempt == DateTimeOffset.MinValue || now - lastAttempt >= Cooldown);
}
