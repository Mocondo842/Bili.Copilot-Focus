// Copyright (c) Bili Copilot. All rights reserved.

using BiliCopilot.UI.Extensions;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Richasy.WinUIKernel.Share.Base;

namespace BiliCopilot.UI.Controls;

public sealed partial class BasicCoverImage : ImageExBase
{
    private const int MaxRetryCount = 2;

    private readonly static BasicScaleEffect _scaleEffect = new();
    private int _retryCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicCoverImage"/> class.
    /// </summary>
    public BasicCoverImage()
    {
        // 列表回收 / 离开视野时，基类会取消在途下载，此后没有新的 Source 变更就不会再加载，
        // 表现就是"这张图随机地一直是空白，进出页面有时又好了"。这里对失败做有预算的重试。
        Loaded += (_, _) => _retryCount = 0;
        ImageFailed += OnImageFailed;
    }

    private void OnImageFailed(object? sender, EventArgs e)
    {
        if (!IsLoaded || _retryCount >= MaxRetryCount || Source is not Uri uri)
        {
            return;
        }

        _retryCount++;
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            if (IsLoaded)
            {
                Source = null;
                Source = uri;
            }
        });
    }

    protected override string GetCacheSubFolder()
        => "ImageCache";

    protected override HttpClient? GetCustomHttpClient()
        => InternalHttpExtensions.ImageClient;

    /// <inheritdoc/>
    protected override void DrawImage(CanvasBitmap canvasBitmap)
    {
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
