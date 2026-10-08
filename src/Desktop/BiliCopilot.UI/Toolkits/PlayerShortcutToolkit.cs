// Copyright (c) Bili Copilot. All rights reserved.
// 本文件由 fork 新增，不属于上游；见仓库根目录 FORK.md。

using Richasy.WinUIKernel.Share.Toolkits;
using Windows.System;

namespace BiliCopilot.UI.Toolkits;

/// <summary>
/// 播放器里可绑定快捷键的动作。
/// </summary>
internal enum PlayerShortcutAction
{
    /// <summary>
    /// 播放 / 暂停.
    /// </summary>
    PlayPause,

    /// <summary>
    /// 快进.
    /// </summary>
    SkipForward,

    /// <summary>
    /// 快退.
    /// </summary>
    SkipBackward,

    /// <summary>
    /// 音量增加.
    /// </summary>
    VolumeUp,

    /// <summary>
    /// 音量减少.
    /// </summary>
    VolumeDown,

    /// <summary>
    /// 倍速增加.
    /// </summary>
    SpeedUp,

    /// <summary>
    /// 倍速减少.
    /// </summary>
    SpeedDown,

    /// <summary>
    /// 下一个视频.
    /// </summary>
    NextVideo,

    /// <summary>
    /// 上一个视频.
    /// </summary>
    PreviousVideo,

    /// <summary>
    /// 静音切换.
    /// </summary>
    ToggleMute,

    /// <summary>
    /// 字幕开关.
    /// </summary>
    ToggleSubtitle,

    /// <summary>
    /// 窗口置顶.
    /// </summary>
    ToggleTopMost,

    /// <summary>
    /// 截图.
    /// </summary>
    TakeScreenshot,

    /// <summary>
    /// 全屏切换.
    /// </summary>
    ToggleFullScreen,

    /// <summary>
    /// 紧凑窗口（画中画）.
    /// </summary>
    ToggleCompactOverlay,

    /// <summary>
    /// 上一章节.
    /// </summary>
    PreviousChapter,

    /// <summary>
    /// 下一章节.
    /// </summary>
    NextChapter,

    /// <summary>
    /// 统计信息.
    /// </summary>
    ToggleStats,

    /// <summary>
    /// 退出默认模式.
    /// </summary>
    ExitDefaultMode,
}

/// <summary>
/// 一条快捷键：主键 + 修饰键。
/// </summary>
/// <param name="Key">主键.</param>
/// <param name="Ctrl">是否按住 Ctrl.</param>
/// <param name="Shift">是否按住 Shift.</param>
/// <param name="Alt">是否按住 Alt.</param>
internal readonly record struct PlayerShortcut(VirtualKey Key, bool Ctrl, bool Shift, bool Alt);

/// <summary>
/// 播放器快捷键表：默认值、读写本地设置、匹配与显示。
/// 上游把按键写死在 <c>PlayerViewModel</c> 的 KeyUp 处理里，这里改成可配置，
/// 动作与命令的对应关系仍留在播放器视图模型里。
/// </summary>
internal static class PlayerShortcutToolkit
{
    private const string SettingPrefix = "PlayerShortcut.";

    private static readonly List<PlayerShortcutAction> ActionOrder =
    [
        PlayerShortcutAction.PlayPause,
        PlayerShortcutAction.SkipForward,
        PlayerShortcutAction.SkipBackward,
        PlayerShortcutAction.VolumeUp,
        PlayerShortcutAction.VolumeDown,
        PlayerShortcutAction.SpeedUp,
        PlayerShortcutAction.SpeedDown,
        PlayerShortcutAction.NextVideo,
        PlayerShortcutAction.PreviousVideo,
        PlayerShortcutAction.ToggleMute,
        PlayerShortcutAction.ToggleSubtitle,
        PlayerShortcutAction.ToggleTopMost,
        PlayerShortcutAction.TakeScreenshot,
        PlayerShortcutAction.ToggleFullScreen,
        PlayerShortcutAction.ToggleCompactOverlay,
        PlayerShortcutAction.PreviousChapter,
        PlayerShortcutAction.NextChapter,
        PlayerShortcutAction.ToggleStats,
        PlayerShortcutAction.ExitDefaultMode,
    ];

    /// <summary>
    /// 全部可配置动作，界面按此顺序展示。
    /// </summary>
    internal static IReadOnlyList<PlayerShortcutAction> AllActions => ActionOrder;

    /// <summary>
    /// 动作名（界面展示用）。
    /// </summary>
    /// <param name="action">动作.</param>
    /// <returns>中文名.</returns>
    internal static string GetDisplayName(PlayerShortcutAction action)
        => action switch
        {
            PlayerShortcutAction.PlayPause => "播放 / 暂停",
            PlayerShortcutAction.SkipForward => "快进",
            PlayerShortcutAction.SkipBackward => "快退",
            PlayerShortcutAction.VolumeUp => "音量增加",
            PlayerShortcutAction.VolumeDown => "音量减少",
            PlayerShortcutAction.SpeedUp => "倍速增加",
            PlayerShortcutAction.SpeedDown => "倍速减少",
            PlayerShortcutAction.NextVideo => "下一个视频",
            PlayerShortcutAction.PreviousVideo => "上一个视频",
            PlayerShortcutAction.ToggleMute => "静音切换",
            PlayerShortcutAction.ToggleSubtitle => "字幕开关",
            PlayerShortcutAction.ToggleTopMost => "窗口置顶",
            PlayerShortcutAction.TakeScreenshot => "截图",
            PlayerShortcutAction.ToggleFullScreen => "全屏切换",
            PlayerShortcutAction.ToggleCompactOverlay => "紧凑窗口",
            PlayerShortcutAction.PreviousChapter => "上一章节",
            PlayerShortcutAction.NextChapter => "下一章节",
            PlayerShortcutAction.ToggleStats => "统计信息",
            PlayerShortcutAction.ExitDefaultMode => "退出默认模式",
            _ => action.ToString(),
        };

    /// <summary>
    /// 动作的默认快捷键。
    /// </summary>
    /// <param name="action">动作.</param>
    /// <returns>默认快捷键.</returns>
    internal static PlayerShortcut GetDefault(PlayerShortcutAction action)
        => action switch
        {
            PlayerShortcutAction.PlayPause => new(VirtualKey.Space, false, false, false),
            PlayerShortcutAction.SkipForward => new(VirtualKey.Right, false, false, false),
            PlayerShortcutAction.SkipBackward => new(VirtualKey.Left, false, false, false),
            PlayerShortcutAction.VolumeUp => new(VirtualKey.Up, false, false, false),
            PlayerShortcutAction.VolumeDown => new(VirtualKey.Down, false, false, false),
            PlayerShortcutAction.SpeedUp => new(VirtualKey.Up, true, false, false),
            PlayerShortcutAction.SpeedDown => new(VirtualKey.Down, true, false, false),
            PlayerShortcutAction.NextVideo => new(VirtualKey.Right, false, true, false),
            PlayerShortcutAction.PreviousVideo => new(VirtualKey.Left, false, true, false),
            PlayerShortcutAction.ToggleMute => new(VirtualKey.M, false, false, false),
            PlayerShortcutAction.ToggleSubtitle => new(VirtualKey.V, false, false, false),
            PlayerShortcutAction.ToggleTopMost => new(VirtualKey.T, false, false, false),
            PlayerShortcutAction.TakeScreenshot => new(VirtualKey.S, false, false, false),
            PlayerShortcutAction.ToggleFullScreen => new(VirtualKey.F11, false, false, false),
            PlayerShortcutAction.ToggleCompactOverlay => new(VirtualKey.M, true, false, false),
            PlayerShortcutAction.PreviousChapter => new(VirtualKey.PageUp, false, false, false),
            PlayerShortcutAction.NextChapter => new(VirtualKey.PageDown, false, false, false),
            PlayerShortcutAction.ToggleStats => new(VirtualKey.I, false, false, false),
            PlayerShortcutAction.ExitDefaultMode => new(VirtualKey.Escape, false, false, false),
            _ => new(VirtualKey.None, false, false, false),
        };

    private static ISettingsToolkit Settings => GlobalDependencies.Kernel.GetRequiredService<ISettingsToolkit>();

    /// <summary>
    /// 读取动作当前的快捷键，读不到（没改过或值坏了）就用默认值。
    /// </summary>
    /// <param name="action">动作.</param>
    /// <returns>当前快捷键.</returns>
    internal static PlayerShortcut Get(PlayerShortcutAction action)
    {
        var text = Settings.ReadLocalSetting(SettingPrefix + action.ToString(), string.Empty);
        return TryParse(text, out var shortcut) ? shortcut : GetDefault(action);
    }

    /// <summary>
    /// 写入动作的快捷键；若该组合已被别的动作占用，则两者对调，避免出现两个动作抢同一个键。
    /// </summary>
    /// <param name="action">动作.</param>
    /// <param name="shortcut">新快捷键.</param>
    /// <returns>被对调的动作；没有冲突时为 <c>null</c>.</returns>
    internal static PlayerShortcutAction? Set(PlayerShortcutAction action, PlayerShortcut shortcut)
    {
        if (shortcut.Key == VirtualKey.None)
        {
            return null;
        }

        PlayerShortcutAction? swapped = null;
        foreach (var other in ActionOrder)
        {
            if (other == action)
            {
                continue;
            }

            if (Get(other) == shortcut)
            {
                Write(other, Get(action));
                swapped = other;
                break;
            }
        }

        Write(action, shortcut);
        return swapped;
    }

    /// <summary>
    /// 把动作恢复成默认快捷键。
    /// </summary>
    /// <param name="action">动作.</param>
    internal static void Reset(PlayerShortcutAction action)
        => Settings.WriteLocalSetting(SettingPrefix + action.ToString(), Serialize(GetDefault(action)));

    /// <summary>
    /// 把所有动作恢复成默认快捷键。
    /// </summary>
    internal static void ResetAll()
    {
        foreach (var action in ActionOrder)
        {
            Reset(action);
        }
    }

    /// <summary>
    /// 按当前修饰键状态匹配按键。
    /// </summary>
    /// <param name="key">按下的主键.</param>
    /// <param name="action">匹配到的动作.</param>
    /// <returns>是否命中.</returns>
    internal static bool TryMatch(VirtualKey key, out PlayerShortcutAction action)
    {
        var ctrl = AppToolkit.IsCtrlPressed();
        var shift = AppToolkit.IsShiftPressed();
        var alt = AppToolkit.IsAltPressed();

        foreach (var item in ActionOrder)
        {
            var shortcut = Get(item);
            if (shortcut.Key == key && shortcut.Ctrl == ctrl && shortcut.Shift == shift && shortcut.Alt == alt)
            {
                action = item;
                return true;
            }
        }

        action = default;
        return false;
    }

    /// <summary>
    /// 是否是修饰键本身（绑定时要等主键）。
    /// </summary>
    /// <param name="key">按键.</param>
    /// <returns>是否修饰键.</returns>
    internal static bool IsModifierKey(VirtualKey key)
        => key is VirtualKey.Control
            or VirtualKey.LeftControl
            or VirtualKey.RightControl
            or VirtualKey.Shift
            or VirtualKey.LeftShift
            or VirtualKey.RightShift
            or VirtualKey.Menu
            or VirtualKey.LeftMenu
            or VirtualKey.RightMenu
            or VirtualKey.LeftWindows
            or VirtualKey.RightWindows;

    /// <summary>
    /// 格式化成界面上的写法，如 <c>Ctrl+↑</c>。
    /// </summary>
    /// <param name="shortcut">快捷键.</param>
    /// <returns>展示文本.</returns>
    internal static string Format(PlayerShortcut shortcut)
    {
        var parts = new List<string>(4);
        if (shortcut.Ctrl)
        {
            parts.Add("Ctrl");
        }

        if (shortcut.Shift)
        {
            parts.Add("Shift");
        }

        if (shortcut.Alt)
        {
            parts.Add("Alt");
        }

        parts.Add(GetKeyName(shortcut.Key));
        return string.Join('+', parts);
    }

    private static void Write(PlayerShortcutAction action, PlayerShortcut shortcut)
        => Settings.WriteLocalSetting(SettingPrefix + action.ToString(), Serialize(shortcut));

    private static string Serialize(PlayerShortcut shortcut)
    {
        var parts = new List<string>(4);
        if (shortcut.Ctrl)
        {
            parts.Add("Ctrl");
        }

        if (shortcut.Shift)
        {
            parts.Add("Shift");
        }

        if (shortcut.Alt)
        {
            parts.Add("Alt");
        }

        parts.Add(shortcut.Key.ToString());
        return string.Join('+', parts);
    }

    private static bool TryParse(string text, out PlayerShortcut shortcut)
    {
        shortcut = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var tokens = text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            return false;
        }

        if (!Enum.TryParse(tokens[^1], true, out VirtualKey key) || key == VirtualKey.None)
        {
            return false;
        }

        var ctrl = false;
        var shift = false;
        var alt = false;
        for (var i = 0; i < tokens.Length - 1; i++)
        {
            if (tokens[i].Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
            {
                ctrl = true;
            }
            else if (tokens[i].Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                shift = true;
            }
            else if (tokens[i].Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                alt = true;
            }
        }

        shortcut = new PlayerShortcut(key, ctrl, shift, alt);
        return true;
    }

    private static string GetKeyName(VirtualKey key)
    {
        var name = key.ToString();
        return name switch
        {
            "Left" => "←",
            "Right" => "→",
            "Up" => "↑",
            "Down" => "↓",
            "Space" => "空格",
            "Escape" => "Esc",
            "PageUp" => "PgUp",
            "PageDown" => "PgDn",
            "Enter" => "回车",
            "Back" => "退格",
            _ => name.StartsWith("Number", StringComparison.Ordinal) && name.Length == 7 ? name[6..] : name,
        };
    }
}
