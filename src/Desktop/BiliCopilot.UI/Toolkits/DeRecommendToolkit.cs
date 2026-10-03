// Copyright (c) Bili Copilot. All rights reserved.
// 本文件由 fork 新增，不属于上游；见仓库根目录 FORK.md。

using Richasy.BiliKernel.Models;
using Richasy.BiliKernel.Models.Moment;

namespace BiliCopilot.UI.Toolkits;

/// <summary>
/// 去推荐化开关：fork 的目标是「除主动关注与主动检索外，不呈现官方推流」。
/// </summary>
internal static class DeRecommendToolkit
{
    /// <summary>
    /// 是否屏蔽官方推流面。
    /// </summary>
    internal static bool Disabled => true;

    /// <summary>
    /// 判断一条动态是否为「没有任何可展示内容」的条目。
    /// 上游内核把未知动态类型映射为 <see cref="MomentItemType.Unsupported"/> 且内容为 null，
    /// 这类条目在界面上只会留下一个「不受支持的内容」占位块；纯文本动态带 Description，不受影响。
    /// </summary>
    /// <param name="moment">动态数据.</param>
    /// <returns>是否应丢弃.</returns>
    internal static bool IsEmptyMoment(MomentInformation? moment)
        => moment is not null
            && moment.Data is null
            && moment.Description is null
            && moment.MomentType is null or MomentItemType.Unsupported;
}
