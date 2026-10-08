// Copyright (c) Bili Copilot. All rights reserved.

using BiliCopilot.UI.Models.Constants;
using BiliCopilot.UI.Toolkits;
using BiliCopilot.UI.ViewModels.Items;
using Richasy.WinUIKernel.Share.Base;

namespace BiliCopilot.UI.Controls.Components;

/// <summary>
/// 简介按钮. 点击后立刻打开浮出层（取回期间显示等待文案），再按需向服务端取一次视频简介并回填.
/// 弹层不依赖命令绑定——模板里的经典绑定取不到命令属性；浮层内容全部走代码.
/// </summary>
public sealed partial class DescriptionButton : DescriptionButtonBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptionButton"/> class.
    /// </summary>
    public DescriptionButton() => InitializeComponent();

    private async void OnDescriptionClickAsync(object sender, RoutedEventArgs e)
    {
        // 先捕获当前绑定的条目：列表虚拟化可能在本方法 await 期间把按钮回收给另一条数据.
        var video = ViewModel;
        if (video is null)
        {
            return;
        }

        // 浮层由 Button.Flyout 在点击时打开，这里先把文案换成等待提示.
        FlyoutTextBlock.Text = ResourceToolkit.GetLocalizedString(StringNames.LoadingAndWait);
        var description = await video.EnsureDescriptionAsync();
        if (!ReferenceEquals(video, ViewModel))
        {
            // 按钮已经改绑到别的条目：数据已写回原来那条，但不要用它去改这一张卡片的浮层.
            return;
        }

        FlyoutTextBlock.Text = string.IsNullOrEmpty(description)
            ? ResourceToolkit.GetLocalizedString(StringNames.NoSpecificData)
            : description;
    }
}

/// <summary>
/// 简介按钮基类.
/// </summary>
public abstract class DescriptionButtonBase : LayoutUserControlBase<VideoItemViewModel>
{
}
