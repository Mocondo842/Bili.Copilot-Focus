// Copyright (c) Bili Copilot. All rights reserved.

using BiliCopilot.UI.Models;
using BiliCopilot.UI.Models.Constants;
using BiliCopilot.UI.Pages.Overlay;
using BiliCopilot.UI.Toolkits;
using BiliCopilot.UI.ViewModels.Core;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Microsoft.Extensions.Logging;
using Richasy.BiliKernel.Bili.Media;
using Richasy.BiliKernel.Bili.User;
using Richasy.BiliKernel.Models.Media;
using Richasy.WinUIKernel.Share.Base;
using Richasy.WinUIKernel.Share.ViewModels;
using System.Globalization;
using Windows.ApplicationModel.DataTransfer;
using Windows.Globalization;
using WinRT;

namespace BiliCopilot.UI.ViewModels.Items;

/// <summary>
/// 视频项视图模型.
/// </summary>
[GeneratedBindableCustomProperty]
public sealed partial class VideoItemViewModel : ViewModelBase<VideoInformation>
{
    private static readonly CultureInfo ChineseCulture = new("zh-CN");

    private readonly Action<VideoItemViewModel>? _removeAction;
    private readonly VideoFavoriteFolder? _favFolder;
    private readonly Action<VideoItemViewModel>? _playAction;
    private readonly Action<VideoItemViewModel>? _showCommentAction;
    private bool _descriptionLoaded;
    private Task<string?>? _descriptionTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoItemViewModel"/> class.
    /// </summary>
    public VideoItemViewModel(
        VideoInformation info,
        VideoCardStyle style,
        Action<VideoItemViewModel> removeAction = default,
        VideoFavoriteFolder? favFolder = default,
        Action<VideoItemViewModel>? playAction = default,
        Action<VideoItemViewModel>? showCommentAction = default)
        : base(info)
    {
        _removeAction = removeAction;
        _favFolder = favFolder;
        _playAction = playAction;
        _showCommentAction = showCommentAction;
        var primaryLan = ApplicationLanguages.Languages[0];
        Style = style;
        Title = info.Identifier.Title;
        Cover = info.Identifier.Cover?.Uri;
        Author = info.Publisher?.User?.Name;
        Avatar = info.Publisher?.User?.Avatar?.Uri;
        Duration = AppToolkit.FormatDuration(TimeSpan.FromSeconds(info.Duration ?? 0));
        PublishRelativeTime = info.PublishTime?.Humanize(culture: AppToolkit.GetCulture(primaryLan));
        PlayCount = info.CommunityInformation?.PlayCount;
        DanmakuCount = info.CommunityInformation?.DanmakuCount;
        LikeCount = info.CommunityInformation?.LikeCount;
        CommentCount = info.CommunityInformation?.CommentCount;
        Description = info.GetExtensionIfNotNull<string?>(VideoExtensionDataId.Description);
        TagName = info.GetExtensionIfNotNull<string?>(VideoExtensionDataId.TagName);
        RecommendReason = info.GetExtensionIfNotNull<string?>(VideoExtensionDataId.RecommendReason);
        Subtitle = info.GetExtensionIfNotNull<string?>(VideoExtensionDataId.Subtitle);
        CollectTime = info.GetExtensionIfNotNull<DateTimeOffset>(VideoExtensionDataId.CollectTime).Humanize(default, ChineseCulture);
        IsUserValid = info.Publisher?.User is not null;
        var progress = info.GetExtensionIfNotNull<int?>(VideoExtensionDataId.Progress);
        if (progress is not null)
        {
            ProgressText = AppToolkit.FormatDuration(TimeSpan.FromSeconds(progress.Value));
        }
    }

    [RelayCommand]
    private void Play()
    {
        if (_playAction is not null)
        {
            _playAction(this);
            return;
        }

        this.Get<AppViewModel>().OpenPlayerCommand.Execute(new MediaSnapshot(Data));
    }

    [RelayCommand]
    private void PlayInPrivate()
    {
        if (_playAction is not null)
        {
            _playAction(this);
            return;
        }

        this.Get<AppViewModel>().OpenPlayerCommand.Execute(new MediaSnapshot(Data, true));
    }

    [RelayCommand]
    private async Task ToggleLikeAsync()
    {
        var state = !IsLiked;
        try
        {
            await this.Get<IPlayerService>().ToggleVideoLikeAsync(Data.Identifier.Id, state);
            IsLiked = state;
            LikeCount = Math.Max(0, (LikeCount ?? 0) + (state ? 1 : -1));
        }
        catch (Exception ex)
        {
            this.Get<ILogger<VideoItemViewModel>>().LogError(ex, "切换视频点赞状态失败");
        }
    }

    [RelayCommand]
    private void ShowComment()
        => _showCommentAction?.Invoke(this);

    /// <summary>
    /// 确保能拿到视频简介：本地没有时按需向服务端取一次.
    /// 并发点击只发一次请求；取回成功（无论内容是否为空）才缓存，失败则允许下次重试.
    /// </summary>
    /// <returns>视频简介，可能为空.</returns>
    public Task<string?> EnsureDescriptionAsync()
    {
        if (!string.IsNullOrEmpty(Description) || _descriptionLoaded)
        {
            return Task.FromResult(Description);
        }

        return _descriptionTask ??= LoadDescriptionAsync();
    }

    private async Task<string?> LoadDescriptionAsync()
    {
        try
        {
            var view = await this.Get<IPlayerService>().GetVideoPageDetailAsync(new MediaIdentifier(Data.Identifier.Id, Data.Identifier.Title, Data.Identifier.Cover));
            Description = view.Information.GetExtensionIfNotNull<string>(VideoExtensionDataId.Description);
            _descriptionLoaded = true;
        }
        catch (Exception ex)
        {
            _descriptionTask = default;
            this.Get<ILogger<VideoItemViewModel>>().LogError(ex, "获取视频简介失败");
        }

        return Description;
    }

    [RelayCommand]
    private void ShowUserSpace()
    {
        if (Data.Publisher?.User is not null)
        {
            this.Get<NavigationViewModel>().NavigateToOver(typeof(UserSpacePage), Data.Publisher.User);
        }
    }

    [RelayCommand]
    private async Task AddToViewLaterAsync()
    {
        try
        {
            await this.Get<IViewLaterService>().AddAsync(Data.Identifier.Id);
            this.Get<AppViewModel>().ShowTipCommand.Execute((ResourceToolkit.GetLocalizedString(StringNames.AddViewLaterSucceed), InfoType.Success));
        }
        catch (Exception ex)
        {
            this.Get<ILogger<VideoItemViewModel>>().LogError(ex, "添加稍后再看失败");
            this.Get<AppViewModel>().ShowTipCommand.Execute((ResourceToolkit.GetLocalizedString(StringNames.AddViewLaterFailed), InfoType.Error));
        }
    }

    [RelayCommand]
    private async Task RemoveViewLaterAsync()
    {
        try
        {
            await this.Get<IViewLaterService>().RemoveAsync([Data.Identifier.Id]);
            _removeAction?.Invoke(this);
        }
        catch (Exception ex)
        {
            this.Get<ILogger<VideoItemViewModel>>().LogError(ex, "移除稍后再看视频失败");
            this.Get<AppViewModel>().ShowTipCommand.Execute((ResourceToolkit.GetLocalizedString(StringNames.FailedToRemoveVideoFromViewLater), InfoType.Error));
        }
    }

    [RelayCommand]
    private async Task RemoveHistoryAsync()
    {
        try
        {
            await this.Get<IViewHistoryService>().RemoveVideoHistoryItemAsync(Data);
            _removeAction?.Invoke(this);
        }
        catch (Exception ex)
        {
            this.Get<ILogger<VideoItemViewModel>>().LogError(ex, "移除历史记录视频失败");
            this.Get<AppViewModel>().ShowTipCommand.Execute((ResourceToolkit.GetLocalizedString(StringNames.FailedToRemoveVideoFromHistory), InfoType.Error));
        }
    }

    [RelayCommand]
    private async Task RemoveFavoriteAsync()
    {
        try
        {
            await this.Get<IFavoriteService>().RemoveVideoAsync(_favFolder, Data.Identifier);
            _removeAction?.Invoke(this);
        }
        catch (Exception ex)
        {
            this.Get<ILogger<VideoItemViewModel>>().LogError(ex, "移除收藏视频失败");
            this.Get<AppViewModel>().ShowTipCommand.Execute((ResourceToolkit.GetLocalizedString(StringNames.FailedToRemoveVideoFromFavorite), InfoType.Error));
        }
    }

    [RelayCommand]
    private async Task OpenInBroswerAsync()
        => await Windows.System.Launcher.LaunchUriAsync(GetWebUri()).AsTask();

    [RelayCommand]
    private void CopyUri()
    {
        var dp = new DataPackage();
        dp.SetText(GetWebUri().ToString());
        dp.SetWebLink(GetWebUri());
        Clipboard.SetContent(dp);
        this.Get<AppViewModel>().ShowTipCommand.Execute((ResourceToolkit.GetLocalizedString(StringNames.Copied), InfoType.Success));
    }

    [RelayCommand]
    private void Pin()
    {
        var pinItem = new PinItem(Data.Identifier.Id, Data.Identifier.Title, Data.Identifier.Cover.Uri.ToString(), PinContentType.Video);
        this.Get<PinnerViewModel>().AddItemCommand.Execute(pinItem);
    }

    private Uri GetWebUri()
    {
        var shortLink = Data.GetExtensionIfNotNull<string>(VideoExtensionDataId.ShortLink);
        return string.IsNullOrEmpty(shortLink) ? new Uri($"https://www.bilibili.com/video/av{Data.Identifier.Id}") : new Uri(shortLink);
    }
}
