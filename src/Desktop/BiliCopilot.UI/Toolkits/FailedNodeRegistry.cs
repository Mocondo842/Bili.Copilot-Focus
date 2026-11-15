// Copyright (c) Bili Copilot. All rights reserved.

using System.Collections.Concurrent;

namespace BiliCopilot.UI.Toolkits;

/// <summary>
/// 打不开的节点主机登记处：测速通过、但 mpv 根本起不播的主机记在这里，后续择优与测速跳过它.
/// 只在进程内有效——不落盘，重启清零；登记超过 <see cref="Ttl"/> 后自动失效，避免一次网络抖动长期误杀一条好线路.
/// 判据与调用位置见 <c>research/80-phase2-acceptance.md</c> §9.
/// </summary>
internal static class FailedNodeRegistry
{
    private static readonly ConcurrentDictionary<string, DateTimeOffset> Failed = new(StringComparer.Ordinal);

    /// <summary>
    /// 登记有效期：30 分钟。
    /// </summary>
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);

    /// <summary>
    /// 把主机记入失败名单；主机名为空时无操作.
    /// </summary>
    /// <param name="host">节点主机名.</param>
    public static void MarkFailed(string? host)
    {
        if (string.IsNullOrEmpty(host))
        {
            return;
        }

        Failed[host] = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 判断主机此刻是否仍在失败名单里；登记已过期就顺手移除，并当作可用.
    /// </summary>
    /// <param name="host">节点主机名.</param>
    /// <returns>仍在有效期内返回 <see langword="true"/>.</returns>
    public static bool IsFailed(string? host)
    {
        if (string.IsNullOrEmpty(host) || !Failed.TryGetValue(host, out var markedAt))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow - markedAt < Ttl)
        {
            return true;
        }

        _ = Failed.TryRemove(host, out _);
        return false;
    }

    /// <summary>
    /// 把主机移出失败名单；用户手动选中它时调用，视为一次重试.
    /// </summary>
    /// <param name="host">节点主机名.</param>
    public static void Clear(string? host)
    {
        if (string.IsNullOrEmpty(host))
        {
            return;
        }

        _ = Failed.TryRemove(host, out _);
    }
}