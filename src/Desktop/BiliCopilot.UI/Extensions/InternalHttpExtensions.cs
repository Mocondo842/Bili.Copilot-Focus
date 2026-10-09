// Copyright (c) Bili Copilot. All rights reserved.

using System.Net;

namespace BiliCopilot.UI.Extensions;

internal static class InternalHttpExtensions
{
    /// <summary>
    /// 视频用户代理.
    /// </summary>
    public const string VideoUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Safari/537.36 Edg/116.0.1938.69";

    /// <summary>
    /// 直播用户代理.
    /// </summary>
    public const string LiveUserAgent = "Mozilla/5.0 BiliDroid/1.12.0 (bbcallen@gmail.com)";

    /// <summary>
    /// 视频来源.
    /// </summary>
    public const string VideoReferer = "https://www.bilibili.com";

    /// <summary>
    /// 直播来源.
    /// </summary>
    public const string LiveReferer = "https://live.bilibili.com";

    public static readonly HttpClient ImageClient = GetImageClient();

    /// <summary>
    /// 获取图片下载客户端.
    /// </summary>
    /// <returns></returns>
    private static HttpClient GetImageClient()
    {
        var handler = new CustomHttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 3,
            PreAuthenticate = true,
            ClientCertificateOptions = ClientCertificateOption.Automatic,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.Brotli,
            UseCookies = false, // Emby API does not use cookies for authentication
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true, // Bypass SSL validation for self-signed certificates (not recommended for production)
        };

        var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add("User-Agent", VideoUserAgent);
        return client;
    }

    private sealed class CustomHttpClientHandler : HttpClientHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 接口给的图片地址可能是 http://（实测同一地址 https 直接 200），统一升到 https 再发请求，
            // 否则 WinUI 侧加载会失败、还会白重试一次。
            if (request.RequestUri?.Scheme == Uri.UriSchemeHttp)
            {
                request.RequestUri = new UriBuilder(request.RequestUri) { Scheme = Uri.UriSchemeHttps, Port = -1 }.Uri;
            }

            // static.hdslb.com 不提供图片尺寸变换：带上 @Ww_Hh_1c.jpg 后缀会 404（实测 noface.gif 去掉后缀即 200），
            // 去掉后缀再请求。
            var requestUri = request.RequestUri;
            if (requestUri is not null && requestUri.Host.Equals("static.hdslb.com", StringComparison.OrdinalIgnoreCase))
            {
                var url = requestUri.AbsoluteUri;
                var suffixIndex = url.IndexOf('@', StringComparison.Ordinal);
                if (suffixIndex > 0)
                {
                    request.RequestUri = new Uri(url[..suffixIndex]);
                }
            }

            request.Version = HttpVersion.Version20;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
