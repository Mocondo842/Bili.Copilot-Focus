// Copyright (c) Bili Copilot. All rights reserved.

using BiliCopilot.UI.Models.Constants;
using Microsoft.Extensions.Logging;
using Richasy.WinUIKernel.Share.Toolkits;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Vortice.DXGI;
using Windows.Storage;

namespace BiliCopilot.UI.Toolkits;

/// <summary>
/// 应用工具组.
/// </summary>
internal sealed partial class AppToolkit : SharedAppToolkit
{
    private static readonly ConcurrentDictionary<string, CultureInfo> CultureCache = new();

    /// <summary>
    /// 取区域性对象（按语言缓存，避免每个列表条目都 new 一个）.
    /// </summary>
    /// <param name="languageName">语言名.</param>
    /// <returns>区域性对象.</returns>
    public static CultureInfo GetCulture(string languageName)
        => CultureCache.GetOrAdd(languageName, static name => new CultureInfo(name));

    private const int VK_LCONTROL = 0xA2; // 左Ctrl键
    private const int VK_RCONTROL = 0xA3; // 右Ctrl键
    private const int VK_SHIFT = 0x10; // Shift键
    private const int VK_MENU = 0x12; // Alt键

    private static readonly List<string> _gpuNames = [];

    public AppToolkit(ISettingsToolkit settings) : base(settings)
    {
    }

    /// <summary>
    /// 格式化时长.
    /// </summary>
    /// <returns>时长文本，如 01:33.</returns>
    public static string FormatDuration(TimeSpan duration)
        => duration.TotalHours >= 1 ? duration.ToString(@"hh\:mm\:ss") : duration.ToString(@"mm\:ss");

    /// <summary>
    /// 格式化数量.
    /// </summary>
    /// <returns>数量文本，如 233万.</returns>
    public static string FormatCount(long count)
    {
        if (count < 0)
        {
            return string.Empty;
        }

        if (count >= 100_000_000)
        {
            var unit = ResourceToolkit.GetLocalizedString(StringNames.Billion);
            return Math.Round(count / 100_000_000d, 2) + unit;
        }
        else if (count >= 10_000)
        {
            var unit = ResourceToolkit.GetLocalizedString(StringNames.TenThousands);
            return Math.Round(count / 10_000d, 2) + unit;
        }

        return count.ToString();
    }

    /// <summary>
    /// 将颜色代码转换为Windows.UI.Color对象.
    /// </summary>
    /// <param name="hexCode">颜色代码.</param>
    /// <returns><see cref="Windows.UI.Color"/>.</returns>
    public static Windows.UI.Color HexToColor(string hexCode)
    {
        // 去除可能包含的 # 符号
        if (hexCode.StartsWith("#"))
        {
            hexCode = hexCode[1..];
        }

        if (int.TryParse(hexCode, out var intValue))
        {
            hexCode = intValue.ToString("X2");
        }

        var color = default(Windows.UI.Color);
        if (hexCode.Length == 4)
        {
            hexCode = "00" + hexCode;
        }

        if (hexCode.Length == 6)
        {
            color.R = byte.Parse(hexCode[..2], NumberStyles.HexNumber);
            color.G = byte.Parse(hexCode.Substring(2, 2), NumberStyles.HexNumber);
            color.B = byte.Parse(hexCode.Substring(4, 2), NumberStyles.HexNumber);
            color.A = 255;
        }

        if (hexCode.Length == 8)
        {
            color.R = byte.Parse(hexCode.Substring(2, 2), NumberStyles.HexNumber);
            color.G = byte.Parse(hexCode.Substring(4, 2), NumberStyles.HexNumber);
            color.B = byte.Parse(hexCode.Substring(6, 2), NumberStyles.HexNumber);
            color.A = byte.Parse(hexCode[..2], NumberStyles.HexNumber);
        }

        return color;
    }

    /// <summary>
    /// 获取偏好解码模式.
    /// </summary>
    /// <returns>解码标识符.</returns>
    public static string GetPreferCodecId()
    {
        var preferCodec = SettingsToolkit.ReadLocalSetting(SettingNames.PreferCodec, PreferCodecType.H264);
        return preferCodec switch
        {
            PreferCodecType.H265 => "hev",
            PreferCodecType.Av1 => "av01",
            _ => "avc",
        };
    }

    /// <summary>
    /// 判断某段流的编码描述是否属于偏好编码族。
    /// 服务端给的写法不固定（hev1 / hvc1 / h265、av01 / av1、avc1 / h264），只按首选项的子串匹配会漏。
    /// </summary>
    /// <param name="codecs">流的编码描述，如 <c>hev1.1.6.L150.90</c>.</param>
    /// <param name="preferCodecId">偏好编码标识，见 <see cref="GetPreferCodecId"/>.</param>
    /// <returns>是否属于偏好编码族.</returns>
    public static bool IsCodecMatch(string? codecs, string preferCodecId)
    {
        if (string.IsNullOrEmpty(codecs))
        {
            return false;
        }

        return preferCodecId switch
        {
            "hev" => codecs.Contains("hev", StringComparison.OrdinalIgnoreCase)
                || codecs.Contains("hvc", StringComparison.OrdinalIgnoreCase)
                || codecs.Contains("h265", StringComparison.OrdinalIgnoreCase),
            "av01" => codecs.Contains("av01", StringComparison.OrdinalIgnoreCase)
                || codecs.Contains("av1", StringComparison.OrdinalIgnoreCase),
            _ => codecs.Contains("avc", StringComparison.OrdinalIgnoreCase)
                || codecs.Contains("h264", StringComparison.OrdinalIgnoreCase),
        };
    }

    /// <summary>
    /// 是否为P2P地址.
    /// </summary>
    /// <returns>检查结果.</returns>
    public static bool IsP2PUrl(string url)
    {
        var uri = new Uri(url);
        return P2PRegex().IsMatch(uri.Host);
    }

    public static bool IsCtrlPressed()
    {
        var leftCtrlState = PInvoke.GetAsyncKeyState(VK_LCONTROL);
        var rightCtrlState = PInvoke.GetAsyncKeyState(VK_RCONTROL);
        return (leftCtrlState & 0x8000) != 0 || (rightCtrlState & 0x8000) != 0;
    }

    public static bool IsShiftPressed()
    {
        var shiftState = PInvoke.GetAsyncKeyState(VK_SHIFT);
        return (shiftState & 0x8000) != 0;
    }

    public static bool IsAltPressed()
    {
        var altState = PInvoke.GetAsyncKeyState(VK_MENU);
        return (altState & 0x8000) != 0;
    }

    public static bool IsOnlyCtrlPressed()
        => IsCtrlPressed() && !IsShiftPressed() && !IsAltPressed();

    public static bool IsOnlyShiftPressed()
        => !IsCtrlPressed() && IsShiftPressed() && !IsAltPressed();

    public static bool IsOnlyAltPressed()
        => !IsCtrlPressed() && !IsShiftPressed() && IsAltPressed();

    public static bool NotModifierKeyPressed()
        => !IsCtrlPressed() && !IsShiftPressed() && !IsAltPressed();

    public static async Task<string> EnsureMpvConfigExistAsync()
    {
        var localFolder = Microsoft.Windows.Storage.ApplicationData.GetDefault().LocalFolder;
        var destPath = Path.Combine(localFolder.Path, "mpv.conf");
        if (!File.Exists(destPath))
        {
            var defaultConfig = await StorageFile.GetFileFromApplicationUriAsync(new("ms-appx:///Assets/basic-mpv.conf"));
            await defaultConfig.CopyAsync(localFolder, "mpv.conf", NameCollisionOption.ReplaceExisting).AsTask();
        }

        return destPath;
    }

    /// <summary>
    /// 获取WebDav服务器地址.
    /// </summary>
    /// <returns>地址.</returns>
    public static string GetWebDavServer(string server, string path)
        => $"{server.TrimEnd('/')}/{path.TrimStart('/')}";

    [GeneratedRegex(@"(mcdn.bilivideo.(cn|com)|szbdyd.com)")]
    private static partial Regex P2PRegex();

    public static async Task OpenAndSelectFileInExplorerAsync(string path, bool isDirectory)
    {
        if ((isDirectory && !Directory.Exists(path)) || (!isDirectory && !File.Exists(path)))
        {
            return;
        }

        var arguments = $"/select,\"{path}\"";
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = arguments,
            UseShellExecute = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(processStartInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
        }
    }

    public static List<string> GetGpuNames()
    {
        if (_gpuNames.Count > 0)
        {
            return _gpuNames;
        }

        try
        {
            // Use DXGI API to enumerate adapters, which is AOT-compatible
            var result = DXGI.CreateDXGIFactory1<IDXGIFactory1>(out var factory);
            if (!result.Success)
            {
                return _gpuNames;
            }

            uint adapterIndex = 0;
            while (true)
            {
                var enumResult = factory!.EnumAdapters1(adapterIndex, out var adapter);
                if (enumResult.Failure)
                {
                    break;
                }
                var desc = adapter.Description1;
                var name = desc.Description;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    // Filter out software/virtual adapters
                    if (name.Contains("Microsoft Basic", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Remote", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Virtual", StringComparison.OrdinalIgnoreCase))
                    {
                        adapter.Dispose();
                        adapterIndex++;
                        continue;
                    }
                    // Skip software adapters (WARP, etc.)
                    if ((desc.Flags & AdapterFlags.Software) != 0)
                    {
                        adapter.Dispose();
                        adapterIndex++;
                        continue;
                    }
                    name = name.Trim();
                    if (!_gpuNames.Contains(name))
                    {
                        _gpuNames.Add(name);
                    }
                }
                adapter.Dispose();
                adapterIndex++;
            }
        }
        catch (Exception ex)
        {
            GlobalDependencies.Kernel.GetRequiredService<ILogger<App>>().LogError(ex, "Failed to enumerate GPU adapters.");
        }

        return _gpuNames;
    }
}
