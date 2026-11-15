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

    public abstract Task<MpvMediaSource> GetSourceAsync();

    public abstract IMpvMediaSourceResolver Clone();

    internal void SetPreferredNodeHost(string? host)
        => PreferredNodeHost = string.IsNullOrEmpty(host) ? default : host;

    internal void ResetNodeState()
        => SetPreferredNodeHost(default);

    internal void UpdateNodeState(IReadOnlyList<string> candidateHosts, string? currentHost, IReadOnlyList<string>? candidateUrls = null)
    {
        // 手选的主机可能是测速合成出来的，不一定在 API 候选里；补进去，菜单的勾选状态才对得上。
        IReadOnlyList<string> hosts = candidateHosts;
        if (PreferredNodeHost is not null && !candidateHosts.Contains(PreferredNodeHost))
        {
            hosts = [.. candidateHosts, PreferredNodeHost];
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
