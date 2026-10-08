// Copyright (c) Bili Copilot. All rights reserved.
// 本文件由 fork 新增，不属于上游；见仓库根目录 FORK.md。

using BiliCopilot.UI.Toolkits;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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
        BuildRows();
    }

    /// <inheritdoc/>
    protected override void OnControlLoaded()
    {
        foreach (var pair in _boxes)
        {
            if (pair.Value != _capturingBox)
            {
                pair.Value.Text = PlayerShortcutToolkit.Format(PlayerShortcutToolkit.Get(pair.Key));
            }
        }
    }

    private void BuildRows()
    {
        foreach (var action in PlayerShortcutToolkit.AllActions)
        {
            var row = new Grid { ColumnSpacing = 12 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var label = new TextBlock
            {
                Text = PlayerShortcutToolkit.GetDisplayName(action),
                VerticalAlignment = VerticalAlignment.Center,
            };

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

            Grid.SetColumn(box, 1);
            row.Children.Add(label);
            row.Children.Add(box);
            _boxes[action] = box;
            ShortcutPanel.Children.Add(row);
        }
    }

    private void OnResetAllClick(object sender, RoutedEventArgs e)
    {
        PlayerShortcutToolkit.ResetAll();
        _capturingBox = null;
        _capturingAction = null;
        OnControlLoaded();
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
