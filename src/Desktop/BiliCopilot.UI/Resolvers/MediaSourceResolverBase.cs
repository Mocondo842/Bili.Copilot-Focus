// Copyright (c) Bili Copilot. All rights reserved.

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
    /// 最近一次实际选中的视频节点主机名.
    /// </summary>
    public string? CurrentNodeHost { get; private set; }

    public abstract Task<MpvMediaSource> GetSourceAsync();

    public abstract IMpvMediaSourceResolver Clone();

    internal void SetPreferredNodeHost(string? host)
        => PreferredNodeHost = string.IsNullOrEmpty(host) ? default : host;

    internal void ResetNodeState()
        => SetPreferredNodeHost(default);

    internal void UpdateNodeState(IReadOnlyList<string> candidateHosts, string? currentHost)
    {
        VideoNodeHosts = candidateHosts;
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
