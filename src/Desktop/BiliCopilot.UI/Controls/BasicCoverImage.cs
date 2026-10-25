// Copyright (c) Bili Copilot. All rights reserved.

using BiliCopilot.UI.Extensions;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Richasy.WinUIKernel.Share.Base;

namespace BiliCopilot.UI.Controls;

public sealed partial class BasicCoverImage : ImageExBase
{
    // U19：列表回收 / 离开视野时，基类会取消在途下载，此后没有新的 Source 变更就不会再加载。
    // F9：13 天日志证实失败 98.7% 是「同一秒内批量取消」（回收/页面切换），原来**立即**重试 2 次
    // 全落在同一个取消窗口里，预算耗尽后这张图就永久空白（一级列表残留：图空白但点开大图有内容）；
    // 「查看回复」折叠/展开面板不触发 Loaded，也没有重载入口（二级界面残留：主楼部分图消失）。
    // 改成：重试带退避延迟（跳出取消窗口）、Loaded 时若从未成功就强制重赋 Source
    //（x:Bind 对同值 Uri 不触发变更，只重置预算不重赋的话回收复用的容器还是空白）、
    // 终身上限 8 次，防止真 404 的图随滚动无限重试。
    private const int MaxRetryCount = 4;
    private const int MaxTotalFailures = 8;
    private const int BaseRetryDelayMs = 600;

    private readonly static BasicScaleEffect _scaleEffect = new();
    private int _retryCount;
    private int _totalFailures;
    private bool _succeeded;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicCoverImage"/> class.
    /// </summary>
    public BasicCoverImage()
    {
        Loaded += (_, _) =>
        {
            _retryCount = 0;
            ScheduleReload(BaseRetryDelayMs);
        };
        ImageFailed += OnImageFailed;
    }

    private void OnImageFailed(object? sender, EventArgs e)
    {
        _totalFailures++;
        if (_totalFailures >= MaxTotalFailures || !IsLoaded || Source is not Uri uri)
        {
            return;
        }

        if (_retryCount >= MaxRetryCount)
        {
            // 本轮预算用完：等下一次 Loaded（滚回可视区 / 面板重新展开）时重置再试。
            return;
        }

        _retryCount++;
        var delay = BaseRetryDelayMs << Math.Min(_retryCount - 1, 3);
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, async () =>
        {
            try
            {
                await Task.Delay(delay);
                ReloadIfPending(uri);
            }
            catch
            {
                // 延迟重试是尽力而为：等待期间控件可能已被回收。
            }
        });
    }

    /// <summary>
    /// 从未成功加载过时，延迟后强制重赋 Source 触发基类重新下载.
    /// </summary>
    private void ScheduleReload(int delayMs)
    {
        if (_succeeded || Source is not Uri uri)
        {
            return;
        }

        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, async () =>
        {
            try
            {
                await Task.Delay(delayMs);
                ReloadIfPending(uri);
            }
            catch
            {
            }
        });
    }

    private void ReloadIfPending(Uri uri)
    {
        if (_succeeded || _totalFailures >= MaxTotalFailures || !IsLoaded || Source is not Uri current || current != uri)
        {
            return;
        }

        Source = null;
        Source = uri;
    }

    protected override string GetCacheSubFolder()
        => "ImageCache";

    protected override HttpClient? GetCustomHttpClient()
        => InternalHttpExtensions.ImageClient;

    /// <inheritdoc/>
    protected override void DrawImage(CanvasBitmap canvasBitmap)
    {
        _succeeded = true;
        var width = canvasBitmap.Size.Width;
        var height = canvasBitmap.Size.Height;
        var aspectRatio = width / height;
        var actualHeight = DecodeWidth / aspectRatio;
        if (Math.Abs(DecodeHeight - actualHeight) > 1)
        {
            DecodeHeight = Math.Round(actualHeight);
            CanvasImageSource = new Microsoft.Graphics.Canvas.UI.Xaml.CanvasImageSource(
                resourceCreator: CanvasDevice.GetSharedDevice(),
                width: (float)DecodeWidth,
                height: (float)DecodeHeight,
                dpi: 96,
                CanvasAlphaMode.Premultiplied);
        }

        var destRect = new Rect(0, 0, DecodeWidth, DecodeHeight);
        var sourceRect = new Rect(0, 0, width, height);
        DrawBlurImage(canvasBitmap, destRect, sourceRect);
    }

    private void DrawBlurImage(
        CanvasBitmap canvasBitmap,
        Rect destinationRect,
        Rect sourceRect)
    {
        if (destinationRect.Width <= 0 || destinationRect.Height <= 0)
        {
            return;
        }

        _scaleEffect.Source = canvasBitmap;
        _scaleEffect.SourceRectangle = sourceRect;
        _scaleEffect.DestinationRectangle = destinationRect;
        using var ds = CanvasImageSource!.CreateDrawingSession(Colors.Transparent);
        ds.DrawImage(_scaleEffect, destinationRect, destinationRect);
    }
}
