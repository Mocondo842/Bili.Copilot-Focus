// Copyright (c) Bili Copilot. All rights reserved.
// 本文件由 fork 新增，不属于上游；见仓库根目录 FORK.md。

using BiliCopilot.UI.Toolkits;
using Microsoft.UI.Xaml.Input;
using Richasy.WinUIKernel.Share.Base;
using Windows.System;

namespace BiliCopilot.UI.Controls.Settings;

/// <summary>
/// 播放器快捷键设置控件.
/// </summary>
public sealed partial class ShortcutSettingControl : SettingsPageControlBase
{
    private readonly Dictionary<PlayerShortcutAction, TextBox> _boxes = [];

    private TextBox? _capturingBox;
    private PlayerShortcutAction? _capturingAction;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortcutSettingControl"/> class.
    /// </summary>
    public ShortcutSettingControl()
    {
        InitializeComponent();
        BuildItems();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded() => RefreshBoxes();

    private void BuildItems()
    {
        // 条目必须在展开器套用模板之前放进 Items：模板只在 OnApplyTemplate 里读一次这个列表，
        // 之后再改列表不会刷新界面（ItemsRepeater 的 ItemsSource 是普通 List）。
        foreach (var action in PlayerShortcutToolkit.AllActions)
        {
            var box = CreateShortcutBox(action);
            _boxes[action] = box;
            Exp.Items.Add(new SettingsCard
            {
                Header = PlayerShortcutToolkit.GetDisplayName(action),
                Description = PlayerShortcutToolkit.GetDescription(action),
                Content = box,
            });
        }

        Exp.Items.Add(new SettingsCard
        {
            Header = "恢复默认",
            Description = "把所有动作的快捷键恢复成出厂设置",
            Content = CreateResetButton(),
        });
    }

    private TextBox CreateShortcutBox(PlayerShortcutAction action)
    {
        var box = new TextBox
        {
            Text = PlayerShortcutToolkit.Format(PlayerShortcutToolkit.Get(action)),
            IsReadOnly = true,
            MinWidth = 140,
            TextAlignment = TextAlignment.Center,
            Tag = action,
        };
        box.GotFocus += OnShortcutBoxGotFocus;
        box.LostFocus += OnShortcutBoxLostFocus;
        box.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler(OnShortcutBoxKeyDown), true);
        return box;
    }

    private Button CreateResetButton()
    {
        var button = new Button
        {
            Content = "全部恢复默认",
            MinWidth = 140,
        };
        button.Click += OnResetAllClick;
        return button;
    }

    private void RefreshBoxes()
    {
        foreach (var pair in _boxes)
        {
            if (pair.Value != _capturingBox)
            {
                pair.Value.Text = PlayerShortcutToolkit.Format(PlayerShortcutToolkit.Get(pair.Key));
            }
        }
    }

    private void OnResetAllClick(object sender, RoutedEventArgs e)
    {
        PlayerShortcutToolkit.ResetAll();
        _capturingBox = null;
        _capturingAction = null;
        RefreshBoxes();
    }

    private void OnShortcutBoxGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox box || box.Tag is not PlayerShortcutAction action)
        {
            return;
        }

        _capturingBox = box;
        _capturingAction = action;
        box.Text = "按下新按键…";
    }

    private void OnShortcutBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox box || box.Tag is not PlayerShortcutAction action || _capturingBox != box)
        {
            return;
        }

        _capturingBox = null;
        _capturingAction = null;
        box.Text = PlayerShortcutToolkit.Format(PlayerShortcutToolkit.Get(action));
    }

    private void OnShortcutBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (sender is not TextBox box || _capturingBox != box || _capturingAction is not PlayerShortcutAction action)
        {
            return;
        }

        e.Handled = true;
        if (PlayerShortcutToolkit.IsModifierKey(e.Key) || e.Key == VirtualKey.None)
        {
            box.Text = "按下主键…";
            return;
        }

        var shortcut = new PlayerShortcut(e.Key, AppToolkit.IsCtrlPressed(), AppToolkit.IsShiftPressed(), AppToolkit.IsAltPressed());
        var swapped = PlayerShortcutToolkit.Set(action, shortcut);
        _capturingBox = null;
        _capturingAction = null;
        box.Text = PlayerShortcutToolkit.Format(PlayerShortcutToolkit.Get(action));
        if (swapped is PlayerShortcutAction other && _boxes.TryGetValue(other, out var otherBox))
        {
            otherBox.Text = PlayerShortcutToolkit.Format(PlayerShortcutToolkit.Get(other));
        }
    }
}
