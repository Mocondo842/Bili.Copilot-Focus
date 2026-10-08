// Copyright (c) Bili Copilot. All rights reserved.

using Richasy.WinUIKernel.Share.Base;

namespace BiliCopilot.UI.Controls.Components;

/// <summary>
/// 简介按钮，点击后在浮出层中显示完整简介。
/// </summary>
public sealed partial class DescriptionButton : LayoutUserControlBase
{
    /// <summary>
    /// <see cref="Description"/> 的依赖属性.
    /// </summary>
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(DescriptionButton), new PropertyMetadata(default, new PropertyChangedCallback(OnDescriptionChanged)));

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptionButton"/> class.
    /// </summary>
    public DescriptionButton() => InitializeComponent();

    /// <summary>
    /// 简介文本，为空时不显示按钮。
    /// </summary>
    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var instance = d as DescriptionButton;
        var description = e.NewValue as string;
        if (instance?.DescriptionBtn is null)
        {
            return;
        }

        instance.DescriptionBtn.Visibility = string.IsNullOrEmpty(description) ? Visibility.Collapsed : Visibility.Visible;
        instance.FlyoutTextBlock.Text = description;
    }
}
