// Copyright (c) Bili Copilot. All rights reserved.

using Microsoft.Extensions.Logging;

namespace BiliCopilot.UI.Toolkits;

/// <summary>
/// 播放健康监测：把 mpv 日志里的故障行归类计数，回答「这次播放有没有真的坏、坏在哪一层」.
/// 换线后的回滚判据与真机验收的判据都取自这里（见 FORK.md F13）.
/// </summary>
internal sealed class PlaybackHealthMonitor
{
    // 时间戳被重置：音频轨换了时间基准，接下来画面会被强制 seek。
    private static readonly string[] PtsResetMarkers = ["Invalid audio PTS", "audio timestamp reset"];

    // 音画不同步：mpv 自己判定已经漂移。
    private static readonly string[] DesyncMarkers = ["Audio/Video desynchronisation"];

    // 解码侧故障：参考帧缺失、包损坏、找不到接续点——一旦出现说明喂给解码器的码流不连续。
    private static readonly string[] DecodeMarkers =
    [
        "Error constructing the frame RPS",
        "Could not find ref with POC",
        "Skipping invalid undecodable",
        "Packet corrupt",
        "no join point",
    ];

    // 打开侧故障：线路打不开。这条走既有的「受控错误换线」，不算「已播内容损坏」。
    private static readonly string[] OpenFailureMarkers = ["SSL connect error", "Can not open external file", "Failed to open"];

    private int _decodeErrors;
    private int _ptsResets;
    private int _desyncs;
    private int _openFailures;
    private int _fatalFaults;

    /// <summary>
    /// 是否出现过「已在播的这一段不可信」的故障；换线后的健康窗口据此决定是否回滚.
    /// </summary>
    public bool HasFatalFault => Volatile.Read(ref _fatalFaults) > 0;

    /// <summary>
    /// 累计致命故障数；换线前后各取一次快照做差，比 Reset 更安全——不会把整段播放的累计抹掉.
    /// </summary>
    public int FatalFaultCount => Volatile.Read(ref _fatalFaults);

    /// <summary>
    /// 是否记录到任何故障；用于避免没有故障时也刷一条汇总.
    /// </summary>
    public bool HasAnyFault => HasFatalFault || Volatile.Read(ref _openFailures) > 0;

    /// <summary>
    /// 归类一行 mpv 日志；按「时间戳 → 不同步 → 解码 → 打开」顺序只记一次，非故障行忽略。
    /// </summary>
    /// <param name="message">mpv 日志的渲染文本.</param>
    public void Inspect(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        if (Matches(message, PtsResetMarkers))
        {
            _ = Interlocked.Increment(ref _ptsResets);
            _ = Interlocked.Increment(ref _fatalFaults);
            return;
        }

        if (Matches(message, DesyncMarkers))
        {
            _ = Interlocked.Increment(ref _desyncs);
            _ = Interlocked.Increment(ref _fatalFaults);
            return;
        }

        if (Matches(message, DecodeMarkers))
        {
            _ = Interlocked.Increment(ref _decodeErrors);
            _ = Interlocked.Increment(ref _fatalFaults);
            return;
        }

        if (Matches(message, OpenFailureMarkers))
        {
            _ = Interlocked.Increment(ref _openFailures);
        }
    }

    /// <summary>
    /// 清零计数；每开始一个新视频调用一次.
    /// </summary>
    public void Reset()
    {
        _ = Interlocked.Exchange(ref _decodeErrors, 0);
        _ = Interlocked.Exchange(ref _ptsResets, 0);
        _ = Interlocked.Exchange(ref _desyncs, 0);
        _ = Interlocked.Exchange(ref _openFailures, 0);
        _ = Interlocked.Exchange(ref _fatalFaults, 0);
    }

    /// <summary>
    /// 一行汇总，供日志与真机验收判据使用.
    /// </summary>
    /// <returns>形如「解码错误 3 / PTS 重置 1 / 不同步 0 / 打开失败 2」.</returns>
    public string Summarize()
        => $"解码错误 {Volatile.Read(ref _decodeErrors)} / PTS 重置 {Volatile.Read(ref _ptsResets)} / 不同步 {Volatile.Read(ref _desyncs)} / 打开失败 {Volatile.Read(ref _openFailures)}";

    private static bool Matches(string message, string[] markers)
    {
        foreach (var marker in markers)
        {
            if (message.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// mpv 客户端日志的旁路观察者：原样转发给内层 logger，顺手把故障行交给 <see cref="PlaybackHealthMonitor"/> 归类.
/// 只包在 <c>MpvClient.CreateAsync</c> 的 logger 实参上，不改变 mpv 的日志等级与落盘行为.
/// </summary>
/// <param name="inner">真正的日志实现.</param>
/// <param name="monitor">故障归类的接收方.</param>
internal sealed class MpvLogTap(ILogger inner, PlaybackHealthMonitor monitor) : ILogger
{
    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => inner.BeginScope(state);

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // 只读归类：Inspect 只做字符串匹配与计数，不会抛；这里不套 try，
        // 因为空 catch 会被 RCS1075 判错，而观察者本身绝不能影响 mpv 原生日志的转发。
        monitor.Inspect(formatter(state, exception));

        inner.Log(logLevel, eventId, state, exception, formatter);
    }
}
