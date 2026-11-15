// Copyright (c) Bili Copilot. All rights reserved.

using BiliCopilot.UI.Models.Constants;
using BiliCopilot.UI.Toolkits;
using BiliCopilot.UI.ViewModels.Core;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace BiliCopilot.UI.Controls.Core;

public sealed partial class PlayerInteractivePanel : PlayerControlBase
{
    private readonly DispatcherQueueTimer _tapTimer;
    private readonly DispatcherQueueTimer _holdTimer;
    private readonly DispatcherQueueTimer _tempControlTimer;
    private Point _startPoint;
    private InteractiveArea _interactiveArea;
    private double _totalDeltaX;
    private int _tapCount;
    private bool _isManipulating;
    private bool _isTouch;
    private bool _isLeftButton;
    private bool _isRightButton;
    private bool _isHolding;

    public PlayerInteractivePanel()
    {
        InitializeComponent();
        _tapTimer = DispatcherQueue.CreateTimer();
        _tapTimer.Interval = TimeSpan.FromMilliseconds(200);
        _tapTimer.Tick += OnTapTimerTick;
        _holdTimer = DispatcherQueue.CreateTimer();
        _holdTimer.Interval = TimeSpan.FromMilliseconds(500);
        _holdTimer.Tick += OnHoldTimerTick;
        _tempControlTimer = DispatcherQueue.CreateTimer();
        _tempControlTimer.Interval = TimeSpan.FromMilliseconds(500);
        _tempControlTimer.Tick += OnTempControlTimerTick;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    internal static string GetPanscanText(bool usingPanscan)
        => usingPanscan ? ResourceToolkit.GetLocalizedString(Models.Constants.StringNames.CancelCutBlackArea) : ResourceToolkit.GetLocalizedString(Models.Constants.StringNames.CutBlackArea);

    protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
    {
        var distance = e.GetCurrentPoint(this).Properties.MouseWheelDelta;
        if (!AppToolkit.IsShiftPressed() && !AppToolkit.IsCtrlPressed() && !AppToolkit.IsAltPressed())
        {
            if (ViewModel?.Client is not null)
            {
                if (distance > 0)
                {
                    ViewModel.IncreaseVolumeCommand.Execute(default);
                }
                else if (distance < 0)
                {
                    ViewModel.DecreaseVolumeCommand.Execute(default);
                }
            }
        }
        else if (AppToolkit.IsOnlyCtrlPressed())
        {
            if (ViewModel?.Client is not null)
            {
                if (distance > 0)
                {
                    ViewModel.IncreaseSpeedCommand.Execute(default);
                }
                else if (distance < 0)
                {
                    ViewModel.DecreaseSpeedCommand.Execute(default);
                }
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        CheckPointerType(e);
        _isLeftButton = e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse &&
                        point.Properties.IsLeftButtonPressed;
        _isRightButton = e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse &&
                         point.Properties.IsRightButtonPressed;
        _startPoint = point.Position;
        var width = ActualWidth;
        var sideWidth = Math.Max(80, width / 5d);

        if (point.Position.X > width - sideWidth)
        {
            _interactiveArea = InteractiveArea.Aside;
        }
        else
        {
            _interactiveArea = InteractiveArea.Main;
        }

        _isHolding = false;
        if (!_isRightButton)
        {
            _holdTimer.Start();
        }

        CapturePointer(e.Pointer);
        e.Handled = true;
    }

    protected override void OnControlLoaded()
        => _tempControlTimer.Start();

    protected override void OnControlUnloaded()
        => CleanupInteractionState();

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        if (PointerCaptures?.Any(p => p.PointerId == e.Pointer.PointerId) != true)
        {
            return;
        }

        CheckPointerType(e);
        var currentPoint = e.GetCurrentPoint(this);
        var deltaX = currentPoint.Position.X - _startPoint.X;
        var deltaY = currentPoint.Position.Y - _startPoint.Y;

        if (!_isManipulating && (Math.Abs(deltaX) > 5 || Math.Abs(deltaY) > 5))
        {
            _isManipulating = true;
            _tapTimer.Stop();
            _holdTimer.Stop();
            _tapCount = 0;
        }

        if (_isManipulating)
        {
            _totalDeltaX += deltaX;
            HandleManipulationUpdate(deltaX, deltaY);
            _startPoint = currentPoint.Position;
        }

        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        if (PointerCaptures?.Any(p => p.PointerId == e.Pointer.PointerId) != true)
        {
            return;
        }

        CheckPointerType(e);
        var currentPoint = e.GetCurrentPoint(this);
        var deltaX = currentPoint.Position.X - _startPoint.X;
        var deltaY = currentPoint.Position.Y - _startPoint.Y;

        _holdTimer.Stop();

        if (_isHolding)
        {
            ViewModel?.RestoreSpeedCommand?.Execute(default);
            _isHolding = false;
        }
        else if (!_isManipulating)
        {
            _tapCount++;
            if (_tapCount == 1)
            {
                _tapTimer.Start();
            }
        }
        else
        {
            HandleManipulationCompleted();
        }

        if (_isRightButton)
        {
            _isRightButton = false;
            if (!ViewModel!.IsConnecting && ViewModel.Player?.IsPlaybackInitialized == true)
            {
                FlyoutBase.GetAttachedFlyout(this).ShowAt(this, new FlyoutShowOptions
                {
                    Position = e.GetCurrentPoint(this).Position,
                });
            }
        }

        ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    private void OnTempControlTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (ViewModel == null)
        {
            return;
        }

        var now = DateTimeOffset.Now;
        if (ViewModel.IsVolumeChanging && now - ViewModel.LastVolumeChangingTime > TimeSpan.FromSeconds(2))
        {
            ViewModel.IsVolumeChanging = false;
        }

        if (ViewModel.IsProgressChanging && now - ViewModel.LastProgressChangingTime > TimeSpan.FromSeconds(2))
        {
            ViewModel.IsProgressChanging = false;
        }

        if (ViewModel.IsSpeedChanging && now - ViewModel.LastSpeedChangingTime > TimeSpan.FromSeconds(2))
        {
            ViewModel.IsSpeedChanging = false;
        }
    }

    private void OnHoldTimerTick(object sender, object e)
    {
        _holdTimer.Stop();
        _isHolding = true;
        ViewModel.IsTouching = _isTouch;
        ViewModel?.TripleSpeedCommand?.Execute(default);
    }

    private async void OnTapTimerTick(object? sender, object? e)
    {
        _tapTimer.Stop();
        if (ViewModel.Client is null)
        {
            return;
        }

        if (_tapCount == 2)
        {
            if (_isTouch)
            {
                var state = ViewModel.Player.PlaybackState;
                if (state == Richasy.MpvKernel.Core.Enums.MpvPlayerState.Playing)
                {
                    await ViewModel.Client.PauseAsync();
                }
                else if (state == Richasy.MpvKernel.Core.Enums.MpvPlayerState.Paused)
                {
                    await ViewModel.Client.ResumeAsync();
                }
            }
            else if (_isLeftButton)
            {
                ViewModel.ToggleFullScreenCommand.Execute(default);
            }
        }
        else if (_tapCount == 1 && !ViewModel.IsEnd && (!ViewModel.IsControlsVisible || (_startPoint.Y >= 50 && ActualHeight - _startPoint.Y >= 150)))
        {
            if (_isTouch)
            {
                ViewModel.IsControlsVisible = !ViewModel.IsControlsVisible;
                ViewModel.IsTouchControlsVisible = !ViewModel.IsTouchControlsVisible;
            }
            else if (_isLeftButton)
            {
                ViewModel.PlayPauseCommand.Execute(default);
            }
        }

        _tapCount = 0;
    }

    private async void HandleManipulationUpdate(double deltaX, double deltaY)
    {
        switch (_interactiveArea)
        {
            case InteractiveArea.Main:
                {
                    if (Math.Abs(deltaX) > 2 && Math.Abs(deltaX) > Math.Abs(deltaY))
                    {
                        if (ViewModel.Player.PlaybackState == Richasy.MpvKernel.Core.Enums.MpvPlayerState.Playing)
                        {
                            await ViewModel.Client!.PauseAsync();
                        }

                        var newPos = GetNewPosition();
                        if (newPos != null)
                        {
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                ViewModel.IsPreviewProgressChanging = true;
                                ViewModel.IsControlsVisible = true;
                                ViewModel.PreviewPosition = newPos.Value;
                            });
                        }
                    }
                }

                break;
            case InteractiveArea.Aside:
                {
                    if (Math.Abs(deltaY) > 5 && Math.Abs(deltaY) > Math.Abs(deltaX))
                    {
                        var volumeChange = -deltaY / 10;
                        DispatcherQueue.TryEnqueue(async () =>
                        {
                            var currentVolume = ViewModel.Player.Volume;
                            var newVolume = Math.Max(0, Math.Min(ViewModel.MaxVolume, currentVolume + volumeChange));
                            ViewModel.LastVolumeChangingTime = DateTimeOffset.Now;
                            ViewModel.IsVolumeChanging = true;
                            await ViewModel.Client!.SetVolumeAsync(newVolume);
                        });
                    }
                }

                break;
            default:
                break;
        }
    }

    private async void HandleManipulationCompleted()
    {
        if (_interactiveArea == InteractiveArea.Main && Math.Abs(_totalDeltaX) > 10)
        {
            var newPos = GetNewPosition();
            if (newPos.HasValue)
            {
                await ViewModel.Client!.SetCurrentPositionAsync(newPos.Value);
                await ViewModel.Client.ResumeAsync();
            }
        }

        _startPoint = new(0, 0);
        _totalDeltaX = 0;
        _interactiveArea = InteractiveArea.None;
        _isManipulating = false;
    }

    private double? GetNewPosition()
    {
        var state = ViewModel.Player.PlaybackState;
        var isValidState = state is Richasy.MpvKernel.Core.Enums.MpvPlayerState.Playing or Richasy.MpvKernel.Core.Enums.MpvPlayerState.Paused;
        if (isValidState)
        {
            var duration = ViewModel.Player.Duration;
            var currentPosition = ViewModel.Player.Position;
            var newPosition = currentPosition + (_totalDeltaX / ActualWidth / 2 * duration);
            if (newPosition < 0)
            {
                newPosition = 0;
            }
            else if (newPosition > duration)
            {
                newPosition = duration - 0.1;
            }

            return newPosition;
        }

        return default;
    }

    /// <summary>
    /// 清理所有交互状态，防止状态残留
    /// </summary>
    private void CleanupInteractionState()
    {
        // 停止所有计时器
        _holdTimer.Stop();
        _tapTimer.Stop();

        // 如果正在倍速播放，恢复正常速度
        if (_isHolding)
        {
            ViewModel?.IsTouching = false;
            ViewModel?.RestoreSpeedCommand?.Execute(default);
            _isHolding = false;
        }

        // 如果正在预览进度，取消预览
        if (ViewModel is not null && ViewModel.IsPreviewProgressChanging)
        {
            ViewModel.IsPreviewProgressChanging = false;
        }

        // 重置所有状态变量
        _tapCount = 0;
        _isManipulating = false;
        _isLeftButton = false;
        _isRightButton = false;
        _isTouch = false;
        _totalDeltaX = 0;
        _startPoint = new(0, 0);
        _interactiveArea = InteractiveArea.None;
    }

    private void CheckPointerType(PointerRoutedEventArgs e)
        => _isTouch = e.Pointer.PointerDeviceType is Microsoft.UI.Input.PointerDeviceType.Touch or Microsoft.UI.Input.PointerDeviceType.Pen;

    /// <summary>
    /// Interactive area types.
    /// </summary>
    internal enum InteractiveArea
    {
        /// <summary>
        /// None.
        /// </summary>
        None,

        /// <summary>
        /// Main area.
        /// </summary>
        Main,

        /// <summary>
        /// Side area.
        /// </summary>
        Aside,
    }

    private void OnFlyoutOpened(object sender, object e)
    {
        ViewModel.IsPopupVisible = true;
        FillNodeMenu();
        foreach (var item in Anime4KItem.Items.OfType<RadioMenuFlyoutItem>())
        {
            var tag = item.Tag.ToString();
            if (Enum.TryParse<Anime4KMode>(tag, out var mode))
            {
                item.IsChecked = ViewModel.Anime4KMode == mode;
            }
        }

        foreach (var item in ArtCNNItem.Items.OfType<RadioMenuFlyoutItem>())
        {
            var tag = item.Tag.ToString();
            if (Enum.TryParse<ArtCNNMode>(tag, out var mode))
            {
                item.IsChecked = ViewModel.ArtCNNMode == mode;
            }
        }

        foreach (var item in NNEDI3Item.Items.OfType<RadioMenuFlyoutItem>())
        {
            var tag = item.Tag.ToString();
            if (Enum.TryParse<Nnedi3Mode>(tag, out var mode))
            {
                item.IsChecked = ViewModel.Nnedi3Mode == mode;
            }
        }

        foreach (var item in RavuItem.Items.OfType<RadioMenuFlyoutItem>())
        {
            var tag = item.Tag.ToString();
            if (Enum.TryParse<RavuMode>(tag, out var mode))
            {
                item.IsChecked = ViewModel.RavuMode == mode;
            }
        }

        foreach (var item in NvidiaVsrItem.Items.OfType<RadioMenuFlyoutItem>())
        {
            var tag = item.Tag.ToString();
            if (Enum.TryParse<VsrScale>(tag, out var mode))
            {
                item.IsChecked = ViewModel.VsrScale == mode;
            }
        }
    }

    private void OnRowBarButtonClick(object sender, EventArgs e)
    {
        ContextMenu.Hide();
    }

    private void OnFlyoutClosed(object sender, object e)
    {
        ViewModel.IsPopupVisible = false;
    }

    /// <summary>
    /// 按当前视频的实际候选填充「播放节点」子菜单；每次菜单打开时重建，勾选状态不残留.
    /// </summary>
    private void FillNodeMenu()
    {
        PlayerNodeItem.Items.Clear();
        var nodes = ViewModel?.GetVideoNodes();
        if (nodes is null)
        {
            PlayerNodeItem.IsEnabled = false;
            return;
        }

        PlayerNodeItem.IsEnabled = true;
        var defaultItem = new RadioMenuFlyoutItem
        {
            Text = ResourceToolkit.GetLocalizedString(StringNames.NodeDefault),
            GroupName = "PlayerNodeGroup",
        };
        ApplyRadioStyle(defaultItem);
        PlayerNodeItem.Items.Add(defaultItem);
        foreach (var host in nodes.Hosts)
        {
            var item = new RadioMenuFlyoutItem
            {
                Text = host,
                Tag = host,
                GroupName = "PlayerNodeGroup",
            };
            ApplyRadioStyle(item);
            PlayerNodeItem.Items.Add(item);
            item.IsChecked = host == nodes.CurrentHost;
            item.Click += OnNodeItemClick;
        }

        defaultItem.IsChecked = string.IsNullOrEmpty(nodes.PreferredHost);
        defaultItem.Click += OnDefaultNodeItemClick;

        // fork：「测速能合成、但 API 没给」的镜像线路也摊开——否则自动择优挑了哪条都无从核对。
        PlayerNodeItem.Items.Add(new MenuFlyoutSeparator());
        var mirrorItem = new MenuFlyoutSubItem
        {
            Text = ResourceToolkit.GetLocalizedString(StringNames.NodeAllMirrors),
        };
        if (Resources.TryGetValue("ContextMenuFlyoutSubItemStyle", out var subItemStyle) && subItemStyle is Style subMenuStyle)
        {
            mirrorItem.Style = subMenuStyle;
        }

        FillMirrorItems(mirrorItem, nodes);
        PlayerNodeItem.Items.Add(mirrorItem);

        // 显式触发的那一条保留：关掉自动择优时，它是唯一的按需测速入口。
        PlayerNodeItem.Items.Add(new MenuFlyoutSeparator());
        var speedTestItem = new MenuFlyoutItem
        {
            Text = ResourceToolkit.GetLocalizedString(StringNames.NodeSpeedTest),
        };
        speedTestItem.Click += OnSpeedTestNodeItemClick;
        PlayerNodeItem.Items.Add(speedTestItem);
    }

    /// <summary>
    /// 填充「全部 CDN 线路」：已测速的带速率、按速率降序在前，未测速的只显主机名，当前线路打勾.
    /// </summary>
    /// <param name="parent">子菜单容器.</param>
    /// <param name="nodes">当前节点快照.</param>
    private void FillMirrorItems(MenuFlyoutSubItem parent, PlayerNodeSnapshot nodes)
    {
        var speeds = new Dictionary<string, double>(StringComparer.Ordinal);
        if (nodes.Results is not null)
        {
            foreach (var result in nodes.Results.Where(p => p.Succeeded))
            {
                speeds.TryAdd(result.Host, result.MegabytesPerSecond);
            }
        }

        if (speeds.Count == 0)
        {
            parent.Items.Add(new MenuFlyoutItem
            {
                Text = ResourceToolkit.GetLocalizedString(StringNames.NodeSpeedTestEmpty),
                IsEnabled = false,
            });
        }

        var ordered = nodes.Mirrors
            .OrderByDescending(p => speeds.ContainsKey(p))
            .ThenByDescending(p => speeds.TryGetValue(p, out var tested) ? tested : 0d)
            .ThenBy(p => p, StringComparer.Ordinal)
            .ToList();

        foreach (var host in ordered)
        {
            var text = speeds.TryGetValue(host, out var speed)
                ? $"{host} · {speed:F1} MB/s"
                : host;
            if (string.Equals(host, nodes.CurrentHost, StringComparison.Ordinal))
            {
                text = "✓ " + text;
            }

            var item = new MenuFlyoutItem
            {
                Text = text,
                Tag = host,
            };
            if (Resources.TryGetValue("ContextMenuFlyoutItemStyle", out var value) && value is Style itemStyle)
            {
                item.Style = itemStyle;
            }

            item.Click += OnNodeItemClick;
            parent.Items.Add(item);
        }
    }

    private void ApplyRadioStyle(RadioMenuFlyoutItem item)
    {
        if (Resources.TryGetValue("ContextMenuFlyoutRadioItemStyle", out var value) && value is Style style)
        {
            item.Style = style;
        }
    }

    private void OnDefaultNodeItemClick(object sender, RoutedEventArgs e)
        => ViewModel?.SelectNodeCommand.Execute(null);

    private void OnNodeItemClick(object sender, RoutedEventArgs e)
        => ViewModel?.SelectNodeCommand.Execute((sender as FrameworkElement)?.Tag?.ToString());

    private void OnSpeedTestNodeItemClick(object sender, RoutedEventArgs e)
        => ViewModel?.SpeedTestAndSelectNodeCommand.Execute(null);

    private void OnMenuButtonClick(object sender, RoutedEventArgs e)
    {
        var btn = sender as FrameworkElement;
        var point = btn!.TransformToVisual(this).TransformPoint(new Point(0, 0));
        FlyoutBase.GetAttachedFlyout(this).ShowAt(this, new FlyoutShowOptions
        {
            Position = point,
            Placement = FlyoutPlacementMode.Right,
        });
    }

    private void OnAnime4KItemClick(object sender, RoutedEventArgs e)
    {
        var tag = (sender as FrameworkElement)!.Tag.ToString();
        if (Enum.TryParse<Anime4KMode>(tag, out var mode))
        {
            ViewModel.ChangeAnime4KModeCommand.Execute(mode);
        }
    }

    private void OnArtCNNItemClick(object sender, RoutedEventArgs e)
    {
        var tag = (sender as FrameworkElement)!.Tag.ToString();
        if (Enum.TryParse<ArtCNNMode>(tag, out var mode))
        {
            ViewModel.ChangeArtCNNModeCommand.Execute(mode);
        }
    }

    private void OnNvidiaVsrItemClick(object sender, RoutedEventArgs e)
    {
        var tag = (sender as FrameworkElement)!.Tag.ToString();
        if (Enum.TryParse<VsrScale>(tag, out var mode))
        {
            ViewModel.ChangeNvidiaVsrModeCommand.Execute(mode);
        }
    }

    private void OnNNEDI3ItemClick(object sender, RoutedEventArgs e)
    {
        var tag = (sender as FrameworkElement)!.Tag.ToString();
        if (Enum.TryParse<Nnedi3Mode>(tag, out var mode))
        {
            ViewModel.ChangeNnedi3ModeCommand.Execute(mode);
        }
    }

    private void OnRavuItemClick(object sender, RoutedEventArgs e)
    {
        var tag = (sender as FrameworkElement)!.Tag.ToString();
        if (Enum.TryParse<RavuMode>(tag, out var mode))
        {
            ViewModel.ChangeRavuModeCommand.Execute(mode);
        }
    }
}
