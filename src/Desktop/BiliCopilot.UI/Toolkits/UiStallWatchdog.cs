// Copyright (c) Bili Copilot. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Richasy.WinUIKernel.Share.Toolkits;
using System.Diagnostics;

namespace BiliCopilot.UI.Toolkits;

/// <summary>
/// UI 线程卡顿看门狗.
/// 计时与判定都在线程池线程上做：UI 线程被长时间占住（含死锁）时，仍能由线程池线程写出日志，
/// 这样"界面卡住但日志里什么都没有"就能变成"日志里有一行卡顿时长"。
/// </summary>
internal static class UiStallWatchdog
{
    /// <summary>关闭看门狗的本地设置键（默认开启）.</summary>
    internal const string EnabledSettingName = "UiWatchdogEnabled";

    private const int ThresholdMs = 800;
    private const int PeriodMs = 500;

    private static readonly object SyncRoot = new();

    private static Timer? _timer;
    private static DispatcherQueue? _queue;
    private static ILogger? _logger;
    private static long _ackTicks;
    private static long _stallStartTicks;
    private static int _reported;
    private static readonly Queue<string> Marks = new();
    private static readonly object MarkLock = new();

    /// <summary>
    /// 记一个 UI 侧动作. 卡顿时会把最近若干条一起写进日志，用来把卡顿归因到具体操作.
    /// </summary>
    /// <param name="activity">动作描述.</param>
    internal static void Mark(string activity)
    {
        lock (MarkLock)
        {
            Marks.Enqueue($"{DateTimeOffset.Now:HH:mm:ss.fff} {activity}");
            while (Marks.Count > 8)
            {
                _ = Marks.Dequeue();
            }
        }
    }

    /// <summary>
    /// 启动看门狗；已在运行或设置里关掉时什么都不做.
    /// </summary>
    internal static void Start()
    {
        lock (SyncRoot)
        {
            if (_timer is not null)
            {
                return;
            }

            var settings = GlobalDependencies.Kernel.GetRequiredService<ISettingsToolkit>();
            if (!settings.ReadLocalSetting(EnabledSettingName, true))
            {
                return;
            }

            _queue = GlobalDependencies.Kernel.GetRequiredService<DispatcherQueue>();
            _logger = GlobalDependencies.Kernel.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(UiStallWatchdog));
            _ackTicks = Stopwatch.GetTimestamp();
            _timer = new Timer(OnTick, null, PeriodMs, PeriodMs);
            _logger.LogInformation("UI 看门狗已启动（阈值 {Threshold}ms，周期 {Period}ms）", ThresholdMs, PeriodMs);
        }
    }

    /// <summary>
    /// 停止看门狗.
    /// </summary>
    internal static void Stop()
    {
        lock (SyncRoot)
        {
            _timer?.Dispose();
            _timer = null;
            _queue = null;
        }
    }

    private static string DescribeMarks()
    {
        lock (MarkLock)
        {
            return Marks.Count == 0 ? "（无记录）" : string.Join(" ← ", Marks.Reverse());
        }
    }

    private static void OnTick(object? state)
    {
        var queue = _queue;
        if (queue is null)
        {
            return;
        }

        var now = Stopwatch.GetTimestamp();
        _ = queue.TryEnqueue(() =>
        {
            Interlocked.Exchange(ref _ackTicks, Stopwatch.GetTimestamp());
        });

        var ackTicks = Volatile.Read(ref _ackTicks);
        var elapsedMs = (long)((now - ackTicks) * 1000.0 / Stopwatch.Frequency);
        if (elapsedMs >= ThresholdMs)
        {
            if (Interlocked.Exchange(ref _reported, 1) == 1)
            {
                return;
            }

            // 卡顿起点取"最近一次 UI 回调的时刻"，而不是检出时刻，避免总时长被少算一个周期。
            Interlocked.Exchange(ref _stallStartTicks, ackTicks);
            _logger?.LogWarning("UI 线程卡顿 {ElapsedMs}ms（阈值 {Threshold}ms）", elapsedMs, ThresholdMs);
            _logger?.LogWarning("卡顿前最近动作：{Marks}", DescribeMarks());
        }
        else if (Interlocked.Exchange(ref _reported, 0) == 1)
        {
            var totalMs = (long)((now - _stallStartTicks) * 1000.0 / Stopwatch.Frequency);
            _logger?.LogWarning("UI 线程恢复响应，本次卡顿累计约 {TotalMs}ms", totalMs);
        }
    }
}
