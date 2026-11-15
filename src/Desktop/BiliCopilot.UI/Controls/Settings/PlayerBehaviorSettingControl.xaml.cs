// Copyright (c) Bili Copilot. All rights reserved.

namespace BiliCopilot.UI.Controls.Settings;

/// <summary>
/// 播放器行为设置控件.
/// </summary>
public sealed partial class PlayerBehaviorSettingControl : SettingsPageControlBase
{
    private bool _isLoaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerBehaviorSettingControl"/> class.
    /// </summary>
    public PlayerBehaviorSettingControl() => InitializeComponent();

    protected override void OnControlLoaded()
    {
        // 先赋值再放行，避免初始化赋值被当成用户输入写回设置。
        NodeSampleBox.Value = ViewModel.NodeSpeedTestSampleKiB;
        NodeBudgetBox.Value = ViewModel.NodeSpeedTestBudgetMs;
        NodeConcurrencyBox.Value = ViewModel.NodeSpeedTestConcurrency;
        _isLoaded = true;
    }

    private void OnNodeSampleChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!_isLoaded || double.IsNaN(args.NewValue))
        {
            return;
        }

        ViewModel.NodeSpeedTestSampleKiB = Convert.ToInt32(args.NewValue);
    }

    private void OnNodeBudgetChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!_isLoaded || double.IsNaN(args.NewValue))
        {
            return;
        }

        ViewModel.NodeSpeedTestBudgetMs = Convert.ToInt32(args.NewValue);
    }

    private void OnNodeConcurrencyChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (!_isLoaded || double.IsNaN(args.NewValue))
        {
            return;
        }

        ViewModel.NodeSpeedTestConcurrency = Convert.ToInt32(args.NewValue);
    }
}
