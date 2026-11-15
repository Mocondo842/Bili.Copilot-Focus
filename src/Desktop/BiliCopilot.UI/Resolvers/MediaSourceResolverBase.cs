// Copyright (c) Bili Copilot. All rights reserved.

using BiliCopilot.UI.Toolkits;
using Richasy.MpvKernel.Player;
using Richasy.MpvKernel.Player.Models;

namespace BiliCopilot.UI.Resolvers;

internal abstract class MediaSourceResolverBase : IMpvMediaSourceResolver
{
    public IntPtr WindowHandle { get; set; }

    /// <summary>
    /// 用户在播放器右键菜单里手动选择的视频节点主机名；null 表示跟随默认规则（「不使用P2P」设置）.
    /// 只在当前视频内保留，换视频时由 <see cref="ResetNodeState"/> 重置.
    /// </summary>
    public string? PreferredNodeHost { get; private set; }

    /// <summary>
    /// 最近一次选流得到的视频节点主机名候选（BaseUrl 在前，已按主机去重）；供右键菜单展示.
    /// </summary>
    public IReadOnlyList<string>? VideoNodeHosts { get; private set; }

    /// <summary>
    /// 最近一次选流得到的视频节点候选地址（未按主机去重，且保持 API 给的原始顺序）；供测速展开用.
    /// </summary>
    public IReadOnlyList<string>? VideoNodeUrls { get; private set; }

    /// <summary>
    /// 最近一次实际选中的视频节点主机名.
    /// </summary>
    public string? CurrentNodeHost { get; private set; }

    /// <summary>
    /// 最近一次线路测速的结果（未测速时为 null）；供右键菜单展示速率。
    /// </summary>
    public IReadOnlyList<NodeSpeedTestResult>? NodeSpeedTestResults { get; private set; }

    /// <summary>
    /// 是否正在测速（防重入：起播后台测速与手选命令可能同时触发）。
    /// </summary>
    public bool IsNodeSpeedTesting { get; private set; }

    public abstract Task<MpvMediaSource> GetSourceAsync();

    public abstract IMpvMediaSourceResolver Clone();

    internal void SetPreferredNodeHost(string? host)
        => PreferredNodeHost = string.IsNullOrEmpty(host) ? default : host;

    internal void ResetNodeState()
    {
        SetPreferredNodeHost(default);
        NodeSpeedTestResults = default;
    }

    /// <summary>
    /// 组装待测线路：优先把第一条可展开的候选按镜像主机展开（18 条），否则退回 API 候选。
    /// </summary>
    /// <returns>待测地址，无候选时为空数组.</returns>
    internal string[] BuildSpeedTestTargets()
    {
        var urls = VideoNodeUrls;
        if (urls is null || urls.Count == 0)
        {
            return [];
        }

        var expandable = urls.FirstOrDefault(NodeSpeedTestToolkit.CanExpand);
        return expandable is null
            ? [.. urls]
            : NodeSpeedTestToolkit.ExpandMirrors(expandable);
    }

    /// <summary>
    /// 对当前视频的候选线路测速，结果缓存在 <see cref="NodeSpeedTestResults"/> 上（供菜单展示速率）。
    /// </summary>
    /// <param name="byteCap">单条采样字节上限.</param>
    /// <param name="budget">单条时间上限.</param>
    /// <param name="concurrency">并发线路数.</param>
    /// <param name="cancellationToken">取消令牌.</param>
    /// <returns>测速结果；重入或取消时返回已有结果.</returns>
    internal async Task<IReadOnlyList<NodeSpeedTestResult>> RunSpeedTestAsync(
        long byteCap,
        TimeSpan budget,
        int concurrency,
        CancellationToken cancellationToken)
    {
        if (IsNodeSpeedTesting)
        {
            return NodeSpeedTestResults ?? Array.Empty<NodeSpeedTestResult>();
        }

        var targets = BuildSpeedTestTargets();
        if (targets.Length == 0)
        {
            return Array.Empty<NodeSpeedTestResult>();
        }

        IsNodeSpeedTesting = true;
        try
        {
            var results = await NodeSpeedTestToolkit
                .MeasureManyAsync(targets, byteCap, budget, concurrency, cancellationToken);
            NodeSpeedTestResults = results;
            return results;
        }
        catch (OperationCanceledException)
        {
            return NodeSpeedTestResults ?? Array.Empty<NodeSpeedTestResult>();
        }
        finally
        {
            IsNodeSpeedTesting = false;
        }
    }
    internal void UpdateNodeState(IReadOnlyList<string> candidateHosts, string? currentHost, IReadOnlyList<string>? candidateUrls = null)
    {
        // 手选的主机可能是测速合成出来的，不一定在 API 候选里；补进去，菜单的勾选状态才对得上。
        IReadOnlyList<string> hosts = candidateHosts;
        if (PreferredNodeHost is not null && !candidateHosts.Contains(PreferredNodeHost))
        {
            // 不能写成集合表达式：AOT/裁剪下「集合表达式 → 非可变接口」会被 CsWinRT1032 拒绝。
            hosts = new List<string>(candidateHosts) { PreferredNodeHost };
        }

        VideoNodeHosts = hosts;
        VideoNodeUrls = candidateUrls;
        CurrentNodeHost = currentHost;
    }

    /// <summary>
    /// 汇总一个段（视频或音频）的节点候选：BaseUrl 在前，拼接 BackupUrls，去掉空项并去重.
    /// </summary>
    internal static string[] CollectNodeCandidates(string? baseUrl, IEnumerable<string>? backupUrls)
    {
        if (string.IsNullOrEmpty(baseUrl))
        {
            return [];
        }

        var result = new List<string> { baseUrl };
        if (backupUrls is not null)
        {
            result.AddRange(backupUrls.Where(p => !string.IsNullOrEmpty(p)));
        }

        return [.. result.Distinct()];
    }

    /// <summary>
    /// 在候选中找出手选主机对应的地址：先按原样匹配，找不到再把候选按镜像主机展开后匹配.
    /// 展开是必须的——测速挑出来的主机往往不在 API 给的那几条候选里。
    /// </summary>
    /// <param name="candidates">API 给的候选地址.</param>
    /// <param name="host">手选的主机名.</param>
    /// <returns>命中的地址，未命中返回 <see langword="null"/>.</returns>
    internal static string? FindPreferredUrl(IReadOnlyList<string> candidates, string host)
    {
        var direct = candidates.FirstOrDefault(p => GetHost(p) == host);
        if (direct is not null)
        {
            return direct;
        }

        return candidates
            .SelectMany(p => NodeSpeedTestToolkit.ExpandMirrors(p))
            .FirstOrDefault(p => GetHost(p) == host);
    }

    internal static string? GetHost(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return default;
        }

        try
        {
            return new Uri(url).Host;
        }
        catch (UriFormatException)
        {
            return default;
        }
    }
}
