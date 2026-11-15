// Copyright (c) Bili Copilot. All rights reserved.

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
    /// 单条线路的默认采样上限：8 MiB.
    /// </summary>
    public const long DefaultByteCap = 8 * 1024 * 1024;

    /// <summary>
    /// 单条线路的默认时间上限：15 秒.
    /// </summary>
    public static readonly TimeSpan DefaultBudget = TimeSpan.FromSeconds(15);

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

            // 只换 authority 前缀，查询串必须逐字节不动：upsig 签名就在查询串里，重新序列化会 403。
            var authority = uri.GetLeftPart(UriPartial.Authority);
            result.Add(string.Concat("https://", host, url[authority.Length..]));
        }

        return [.. result];
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
    /// 下载吞吐（十进制 MB/s），与 PiliPlus 的 Byte/µs 口径一致.
    /// </summary>
    public double MegabytesPerSecond
        => Elapsed.TotalSeconds <= 0 ? 0 : BytesRead / 1_000_000d / Elapsed.TotalSeconds;
}
