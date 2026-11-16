// Copyright (c) Bili Copilot. All rights reserved.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Headers;
using BiliCopilot.UI.Extensions;

namespace BiliCopilot.UI.Toolkits;

/// <summary>
/// 播放节点测速：把一条媒体地址按 CDN 主机展开成多条候选，并测量单条线路的下载吞吐。
/// 设计来源见 <c>research/60-piliplus-reuse.md</c>（PiliPlus 的 CDNService 主机替换 + 8 MiB/15 s 采样）。
/// </summary>
internal static class NodeSpeedTestToolkit
{
    /// <summary>
    /// 单条线路的默认采样上限：512 KiB（可由设置项改写）。
    /// 18 条线路全扫最坏 ≈ 9 MiB，够小到可以「每次起播都在后台跑一次」。
    /// </summary>
    public const int DefaultSampleKiB = 512;

    /// <summary>
    /// 单条线路的默认时间上限：2 秒（每条线路各自计时，不是整批的墙钟上限）。
    /// 并发是分批的，若按整批计时，后几批会拿不到任何字节，等于把「没测」当成「很慢」。
    /// </summary>
    public const int DefaultBudgetMs = 2000;

    /// <summary>
    /// 默认并发线路数。
    /// </summary>
    public const int DefaultConcurrency = 6;

    /// <summary>
    /// 并发上限：再高就会互相抢带宽，把排名测反。
    /// </summary>
    public const int MaxConcurrencyLimit = 8;

    /// <summary>
    /// 当前线路不在测速集里时，切换所需的绝对速率门槛（十进制 MB/s）。
    /// </summary>
    public const double UnknownCurrentThreshold = 3.0;

    /// <summary>
    /// 切换所需的相对提升倍数。
    /// </summary>
    public const double SwitchRatio = 1.25;

    /// <summary>
    /// 切换所需的绝对提升（十进制 MB/s）：避免「1.0 → 1.3」这种无感切换。
    /// </summary>
    public const double SwitchMargin = 0.5;

    /// <summary>
    /// 可参与主机替换的镜像主机。
    /// 刻意不含 Akamai（upos-hz-mirrorakam.akamaized.net）：它自带 hdnts 签名，改主机必然 403。
    /// </summary>
    public static readonly string[] MirrorHosts =
    [
        "upos-sz-mirrorali.bilivideo.com",
        "upos-sz-mirroralib.bilivideo.com",
        "upos-sz-mirroralio1.bilivideo.com",
        "upos-sz-mirrorcos.bilivideo.com",
        "upos-sz-mirrorcosb.bilivideo.com",
        "upos-sz-mirrorcoso1.bilivideo.com",
        "upos-sz-mirrorhw.bilivideo.com",
        "upos-sz-mirrorhwb.bilivideo.com",
        "upos-sz-mirrorhwo1.bilivideo.com",
        "upos-sz-mirror08c.bilivideo.com",
        "upos-sz-mirror08h.bilivideo.com",
        "upos-sz-mirror08ct.bilivideo.com",
        "upos-tf-all-hw.bilivideo.com",
        "upos-tf-all-tx.bilivideo.com",
        "upos-sz-mirroraliov.bilivideo.com",
        "upos-sz-mirrorcosov.bilivideo.com",
        "upos-sz-mirrorhwov.bilivideo.com",
        "cn-hk-eq-bcache-01.bilivideo.com",
    ];

    private static readonly HttpClient Client = CreateClient();

    /// <summary>
    /// 该地址是否可用于主机替换（只有 upos 镜像的 upgcxcode 路径才行）。
    /// </summary>
    /// <param name="url">媒体地址.</param>
    /// <returns>可用于替换时返回 <see langword="true"/>.</returns>
    public static bool CanExpand(string? url)
    {
        var host = GetHost(url);
        if (host is null)
        {
            return false;
        }

        // 302 是重定向主机、mcdn 是 PCDN、akamaized 自带签名：三类都不能改主机。
        if (host.Contains("302", StringComparison.Ordinal)
            || host.Contains("mcdn", StringComparison.Ordinal)
            || host.EndsWith(".akamaized.net", StringComparison.Ordinal))
        {
            return false;
        }

        // PiliPlus 的 _mirrorRegex 还要求路径是 /upgcxcode；不满足的地址合成出来也没有意义。
        if (url is null || !url.Contains("/upgcxcode/", StringComparison.Ordinal))
        {
            return false;
        }

        return host.Contains("upos-", StringComparison.Ordinal) || host.Contains("-tf-", StringComparison.Ordinal);
    }

    /// <summary>
    /// 把一条地址按目标主机展开成多条候选（原地址保留在首位，同一主机不重复）。
    /// </summary>
    /// <param name="url">媒体地址.</param>
    /// <param name="hosts">目标主机集合，默认用 <see cref="MirrorHosts"/>.</param>
    /// <returns>候选地址，首项恒为原地址.</returns>
    public static string[] ExpandMirrors(string? url, IEnumerable<string>? hosts = null)
    {
        if (string.IsNullOrEmpty(url))
        {
            return [];
        }

        if (!CanExpand(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return [url];
        }

        var result = new List<string> { url };
        foreach (var host in hosts ?? MirrorHosts)
        {
            if (string.Equals(host, uri.Host, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // 只换 authority，路径与查询串逐字节不动：upsig 签名就在查询串里，重新序列化会 403。
            result.Add(string.Concat("https://", host, uri.PathAndQuery));
        }

        return [.. result];
    }

    /// <summary>
    /// 把 API 候选、可合成镜像和当前线路汇总成统一测速集；当前线路排在首位。
    /// </summary>
    /// <param name="urls">API 给的视频候选地址.</param>
    /// <param name="currentHost">实际选中的视频主机.</param>
    /// <returns>按完整地址与主机去重后的待测地址；无候选时为空数组.</returns>
    public static string[] BuildSpeedTestSet(IEnumerable<string>? urls, string? currentHost)
    {
        var candidates = urls?.Where(p => !string.IsNullOrEmpty(p)).ToArray() ?? [];
        var currentUrl = FindCurrentUrl(candidates, currentHost);
        var ordered = currentUrl is null ? new List<string>() : new List<string> { currentUrl };
        ordered.AddRange(candidates);

        foreach (var expansion in ordered.ToArray().SelectMany(p => ExpandMirrors(p)))
        {
            ordered.Add(expansion);
        }

        var seenHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var url in ordered)
        {
            var host = GetHost(url);
            if (host is null || !seenHosts.Add(host))
            {
                continue;
            }

            result.Add(url);
        }

        return [.. result];
    }

    private static string? FindCurrentUrl(IReadOnlyList<string> candidates, string? currentHost)
    {
        if (string.IsNullOrEmpty(currentHost))
        {
            return default;
        }

        var direct = candidates.FirstOrDefault(p => string.Equals(GetHost(p), currentHost, StringComparison.OrdinalIgnoreCase));
        if (direct is not null)
        {
            return direct;
        }

        var mirrored = candidates
            .SelectMany(p => ExpandMirrors(p))
            .FirstOrDefault(p => string.Equals(GetHost(p), currentHost, StringComparison.OrdinalIgnoreCase));
        if (mirrored is not null)
        {
            return mirrored;
        }

        var template = candidates.FirstOrDefault(CanExpand);
        return template is not null
            ? string.Concat("https://", currentHost, new Uri(template).PathAndQuery)
            : default;
    }

    /// <summary>
    /// 测量单条线路的下载吞吐。
    /// </summary>
    /// <param name="url">待测地址.</param>
    /// <param name="byteCap">采样字节上限.</param>
    /// <param name="budget">时间上限.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>测量结果，失败时带错误描述，不抛异常.</returns>
    public static async Task<NodeSpeedTestResult> MeasureAsync(
        string url,
        long byteCap,
        TimeSpan budget,
        CancellationToken cancellationToken)
    {
        var host = GetHost(url) ?? "(未知)";
        var stopwatch = Stopwatch.StartNew();
        long totalBytes = 0;
        var firstByte = TimeSpan.Zero;

        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedSource.CancelAfter(budget);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Referrer = new Uri(InternalHttpExtensions.VideoReferer);
            request.Headers.Range = new RangeHeaderValue(0, byteCap - 1);
            using var response = await Client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linkedSource.Token)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                // 4xx 基本都是「这条线路不认这个地址」，换条线路就行。
                var status = (int)response.StatusCode;
                var reason = status is >= 400 and < 500 ? "此线路不认该地址" : $"HTTP {status}";
                stopwatch.Stop();
                return new NodeSpeedTestResult(host, url, totalBytes, stopwatch.Elapsed, firstByte, false, reason);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(linkedSource.Token).ConfigureAwait(false);
            var buffer = new byte[81920];
            while (totalBytes < byteCap)
            {
                var read = await stream.ReadAsync(buffer, linkedSource.Token).ConfigureAwait(false);
                if (read <= 0)
                {
                    break;
                }

                if (totalBytes == 0)
                {
                    firstByte = stopwatch.Elapsed;
                }

                totalBytes += read;
            }

            stopwatch.Stop();
            return new NodeSpeedTestResult(host, url, totalBytes, stopwatch.Elapsed, firstByte, true, default);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // 预算耗尽：已读到的字节仍然有效（这正是「慢但能通」的线路）。
            stopwatch.Stop();
            return new NodeSpeedTestResult(
                host,
                url,
                totalBytes,
                stopwatch.Elapsed,
                firstByte,
                totalBytes > 0,
                totalBytes > 0 ? default : "测速超时");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new NodeSpeedTestResult(host, url, totalBytes, stopwatch.Elapsed, firstByte, false, ex.Message);
        }
    }

    /// <summary>
    /// 从结果集中挑出最快的一条（同速率保持列表序，失败项不参与）.
    /// </summary>
    /// <param name="results">测量结果.</param>
    /// <returns>最快的一条，全部失败时返回 <see langword="null"/>.</returns>
    public static NodeSpeedTestResult? SelectFastest(IEnumerable<NodeSpeedTestResult> results)
        => results.Where(p => p.Succeeded).OrderByDescending(p => p.MegabytesPerSecond).FirstOrDefault();

    /// <summary>
    /// 是否应该从当前线路切到测速最快的那条。
    /// 切换会冲掉已缓冲的数据，所以只在「明显更快」时才切；当前线路不在测速集里时改用绝对门槛。
    /// </summary>
    /// <param name="fastest">测速最快的一条.</param>
    /// <param name="current">当前线路的测速结果；当前主机不在测速集里时为 <see langword="null"/>.</param>
    /// <param name="reason">判定理由（写日志用）.</param>
    /// <returns>应该切换时返回 <see langword="true"/>.</returns>
    public static bool ShouldSwitchTo(NodeSpeedTestResult fastest, NodeSpeedTestResult? current, out string reason)
    {
        if (!fastest.Succeeded)
        {
            reason = "最快的一条也不可用";
            return false;
        }

        if (current is null)
        {
            reason = "当前线路不在测速集里";
            return fastest.MegabytesPerSecond >= UnknownCurrentThreshold;
        }

        if (string.Equals(fastest.Host, current.Host, StringComparison.Ordinal))
        {
            reason = "最快的仍是当前线路";
            return false;
        }

        if (!current.Succeeded)
        {
            reason = "当前线路测速失败";
            return true;
        }

        var ratioReached = fastest.MegabytesPerSecond >= current.MegabytesPerSecond * SwitchRatio;
        var marginReached = fastest.MegabytesPerSecond - current.MegabytesPerSecond >= SwitchMargin;
        reason = $"{fastest.MegabytesPerSecond:F1} vs {current.MegabytesPerSecond:F1} MB/s";
        return ratioReached && marginReached;
    }

    /// <summary>
    /// 并发测量多条线路（去重后按并发上限跑完）.
    /// </summary>
    /// <param name="urls">待测地址.</param>
    /// <param name="byteCap">采样字节上限.</param>
    /// <param name="budget">单条时间上限（每条线路各自计时）.</param>
    /// <param name="concurrency">并发线路数，内部钳到 1..<see cref="MaxConcurrencyLimit"/>.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>按「成功优先、速率降序」排好的结果.</returns>
    public static async Task<IReadOnlyList<NodeSpeedTestResult>> MeasureManyAsync(
        IEnumerable<string> urls,
        long byteCap,
        TimeSpan budget,
        int concurrency,
        CancellationToken cancellationToken)
    {
        var targets = urls.Distinct(StringComparer.Ordinal).ToArray();
        var results = new ConcurrentBag<NodeSpeedTestResult>();
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Clamp(concurrency, 1, MaxConcurrencyLimit),
            CancellationToken = cancellationToken,
        };

        await Parallel.ForEachAsync(
            targets,
            options,
            async (url, token) => results.Add(await MeasureAsync(url, byteCap, budget, token).ConfigureAwait(false)))
            .ConfigureAwait(false);

        // 同上：返回 Interface 时必须给具体类型，集合表达式在 AOT 下会被 CsWinRT1032 拒绝。
        List<NodeSpeedTestResult> ordered = [.. results.OrderByDescending(p => p.Succeeded).ThenByDescending(p => p.MegabytesPerSecond)];
        return ordered;
    }

    private static HttpClient CreateClient()
    {
        // 时间上限由每次调用的 CTS 控制，这里必须关掉 HttpClient 自带的 100 秒超时。
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.Add("User-Agent", InternalHttpExtensions.VideoUserAgent);
        return client;
    }

    private static string? GetHost(string? url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host : default;
}

/// <summary>
/// 单条线路的测速结果.
/// </summary>
/// <param name="Host">主机名.</param>
/// <param name="Url">被测地址.</param>
/// <param name="BytesRead">实际读到的字节数.</param>
/// <param name="Elapsed">实际耗时.</param>
/// <param name="TimeToFirstByte">首字节耗时.</param>
/// <param name="Succeeded">是否可用.</param>
/// <param name="Error">失败原因.</param>
internal sealed record NodeSpeedTestResult(
    string Host,
    string Url,
    long BytesRead,
    TimeSpan Elapsed,
    TimeSpan TimeToFirstByte,
    bool Succeeded,
    string? Error)
{
    /// <summary>
    /// 下载吞吐（十进制 MB/s，与 PiliPlus 的 Byte/µs 口径一致）.
    /// 分母刻意用 <see cref="ThroughputSeconds"/> 而不是总耗时：把 DNS/TLS 握手与首包等待剔除掉，
    /// 否则快线路（2 MiB 只需几十毫秒）会被握手时间主导，排名由 RTT 决定而不是带宽.
    /// </summary>
    public double MegabytesPerSecond
        => ThroughputSeconds <= 0 ? 0 : BytesRead / 1_000_000d / ThroughputSeconds;

    /// <summary>
    /// 数据真正在流动的时长（总耗时减去首字节耗时），退化时回退到总耗时.
    /// </summary>
    public double ThroughputSeconds
    {
        get
        {
            var window = (Elapsed - TimeToFirstByte).TotalSeconds;
            return window > 0 ? window : Elapsed.TotalSeconds;
        }
    }
}
