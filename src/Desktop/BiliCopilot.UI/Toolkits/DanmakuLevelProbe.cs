// Copyright (c) Bili Copilot. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Text;
using BiliCopilot.UI;
using BiliCopilot.UI.Extensions;
using Richasy.BiliKernel.Authenticator;
using Richasy.BiliKernel.Bili;
using Richasy.BiliKernel.Http;
using Richasy.BiliKernel.Models;

namespace BiliCopilot.UI.Toolkits;

/// <summary>
/// VOD 弹幕等级/权重来源的只读探针：只记录字段 1（ID）与字段 9（weight），不参与渲染或过滤。
/// </summary>
internal static class DanmakuLevelProbe
{
    private const int ProbeVersion = 1;
    private const int MaxSampleCount = 5;

    /// <summary>
    /// 用当前注册的 BiliKernel 认证器签名并读取一个 VOD 分段的原始 REST 响应。
    /// </summary>
    public static async Task<string> InspectAsync(
        string aid,
        string cid,
        int segmentIndex,
        IReadOnlyList<string> serviceIds,
        CancellationToken cancellationToken)
    {
        var report = new ProbeReport
        {
            Aid = aid,
            Cid = cid,
            SegmentIndex = segmentIndex,
            Source = "signed-rest",
        };

        HttpResponseMessage? response = default;
        HttpRequestMessage? request = default;
        try
        {
            var authenticator = GlobalDependencies.Kernel.GetRequiredService<BiliAuthenticator>();
            var httpClient = GlobalDependencies.Kernel.GetRequiredService<BiliHttpClient>();
            await authenticator.InitializeWbiAsync(cancellationToken).ConfigureAwait(false);
            var parameters = new Dictionary<string, string>
            {
                ["type"] = "1",
                ["oid"] = cid,
                ["pid"] = aid,
                ["segment_index"] = segmentIndex.ToString(CultureInfo.InvariantCulture),
            };
            request = BiliHttpClient.CreateRequest(HttpMethod.Get, new Uri(BiliApis.Video.WebSegmentDanmaku));
            request.Headers.TryAddWithoutValidation("Referer", InternalHttpExtensions.VideoReferer);
            authenticator.AuthorizeRestRequest(request, parameters, new BiliAuthorizeExecutionSettings
            {
                RequireCookie = true,
                ApiType = BiliApiType.Web,
            });
            response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            report.HttpStatus = (int)response.StatusCode;
            report.ContentType = response.Content.Headers.ContentType?.ToString();
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            report.Bytes = bytes.Length;
            if (!response.IsSuccessStatusCode || bytes.Length == 0)
            {
                report.FailureReason = "HTTP 状态非成功或响应为空";
                return report.ToJson();
            }

            Parse(bytes, report);
            report.IdCorrespondence = BuildIdCorrespondence(report.Ids, serviceIds);
            return report.ToJson();
        }
        catch (Exception ex)
        {
            report.FailureReason = ex.GetType().Name;
            return report.ToJson();
        }
        finally
        {
            request?.Dispose();
            response?.Dispose();
        }
    }

    private static void Parse(byte[] bytes, ProbeReport report)
    {
        try
        {
            var position = 0;
            while (position < bytes.Length)
            {
                var tag = ReadVarint(bytes, ref position);
                var field = (int)(tag >> 3);
                var wireType = (int)(tag & 7);
                if (field == 1 && wireType == 2)
                {
                    var length = (int)ReadVarint(bytes, ref position);
                    if (length < 0 || length > bytes.Length - position)
                    {
                        throw new InvalidDataException("弹幕元素长度越界");
                    }

                    var end = position + length;
                    var elementBytes = bytes[position..end];
                    var element = ParseElement(elementBytes, report);
                    position = end;
                    report.TotalElements++;
                    if (element.HasId)
                    {
                        report.Ids.Add(element.Id.ToString(CultureInfo.InvariantCulture));
                    }

                    if (element.HasWeight)
                    {
                        report.PresentWeightCount++;
                        var bucket = element.Weight >= 0 && element.Weight <= 10
                            ? element.Weight.ToString(CultureInfo.InvariantCulture)
                            : "outOfRange";
                        report.WeightHistogram[bucket] = report.WeightHistogram.TryGetValue(bucket, out var count) ? count + 1 : 1;
                    }
                    else
                    {
                        report.AbsentWeightCount++;
                    }

                    if (report.Samples.Count < MaxSampleCount)
                    {
                        report.Samples.Add(new ProbeSample(element.HasId ? element.Id.ToString(CultureInfo.InvariantCulture) : null, element.HasWeight ? element.Weight : null));
                    }
                }
                else
                {
                    report.RecordUnknown(field, wireType);
                    SkipField(bytes, ref position, wireType);
                }
            }

            if (position != bytes.Length)
            {
                throw new InvalidDataException("响应边界不匹配");
            }
        }
        catch (Exception ex)
        {
            report.ParseError = ex.GetType().Name;
        }
    }

    private static ProbeElement ParseElement(byte[] bytes, ProbeReport report)
    {
        var position = 0;
        var end = bytes.Length;
        var result = new ProbeElement();
        while (position < end)
        {
            var tag = ReadVarint(bytes, ref position);
            var field = (int)(tag >> 3);
            var wireType = (int)(tag & 7);
            switch (field, wireType)
            {
                case (1, 0):
                    result.Id = unchecked((long)ReadVarint(bytes, ref position));
                    result.HasId = true;
                    break;
                case (9, 0):
                    result.Weight = unchecked((int)ReadVarint(bytes, ref position));
                    result.HasWeight = true;
                    break;
                case (9, _):
                    report.WireTypeMismatch = true;
                    SkipField(bytes, ref position, wireType);
                    break;
                default:
                    report.RecordUnknown(field, wireType);
                    SkipField(bytes, ref position, wireType);
                    break;
            }
        }

        if (position != end)
        {
            throw new InvalidDataException("弹幕元素边界不匹配");
        }

        return result;
    }

    private static IdCorrespondence BuildIdCorrespondence(IReadOnlyList<string> probeIds, IReadOnlyList<string> serviceIds)
    {
        var probeSet = new HashSet<string>(probeIds, StringComparer.Ordinal);
        var serviceSet = new HashSet<string>(serviceIds, StringComparer.Ordinal);
        return new IdCorrespondence(
            probeIds.Count,
            serviceIds.Count,
            probeIds.Count(p => serviceSet.Contains(p)),
            serviceIds.Count(p => !probeSet.Contains(p)),
            probeIds.Count - probeSet.Count,
            probeIds.Count(p => !serviceSet.Contains(p)));
    }

    private static ulong ReadVarint(byte[] bytes, ref int position)
    {
        ulong value = 0;
        var shift = 0;
        while (shift < 70)
        {
            if (position >= bytes.Length)
            {
                throw new InvalidDataException("varint 被截断");
            }

            var current = bytes[position++];
            value |= (ulong)(current & 0x7f) << shift;
            if ((current & 0x80) == 0)
            {
                return value;
            }

            shift += 7;
        }

        throw new InvalidDataException("varint 过长");
    }

    private static void SkipField(byte[] bytes, ref int position, int wireType)
    {
        switch (wireType)
        {
            case 0:
                _ = ReadVarint(bytes, ref position);
                break;
            case 1:
                Advance(bytes, ref position, 8);
                break;
            case 2:
                var length = (int)ReadVarint(bytes, ref position);
                Advance(bytes, ref position, length);
                break;
            case 5:
                Advance(bytes, ref position, 4);
                break;
            default:
                throw new InvalidDataException("不支持的 wire type " + wireType.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void Advance(byte[] bytes, ref int position, int length)
    {
        if (length < 0 || length > bytes.Length - position)
        {
            throw new InvalidDataException("字段长度越界");
        }

        position += length;
    }

    private static void AppendJsonString(StringBuilder builder, string? value)
    {
        builder.Append('"');
        if (value is not null)
        {
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(c);
                        }

                        break;
                }
            }
        }

        builder.Append('"');
    }

    private sealed class ProbeReport
    {
        public string? Aid { get; set; }
        public string? Cid { get; set; }
        public int SegmentIndex { get; set; }
        public string? Source { get; set; }
        public int? HttpStatus { get; set; }
        public string? ContentType { get; set; }
        public int? Bytes { get; set; }
        public int TotalElements { get; set; }
        public int PresentWeightCount { get; set; }
        public int AbsentWeightCount { get; set; }
        public bool WireTypeMismatch { get; set; }
        public string? ParseError { get; set; }
        public string? FailureReason { get; set; }
        public IdCorrespondence? IdCorrespondence { get; set; }
        public List<string> Ids { get; } = [];
        public Dictionary<string, int> WeightHistogram { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, string> UnknownFields { get; } = new(StringComparer.Ordinal);
        public List<ProbeSample> Samples { get; } = [];

        public void RecordUnknown(int field, int wireType)
            => UnknownFields.TryAdd(field.ToString(CultureInfo.InvariantCulture) + "/" + wireType.ToString(CultureInfo.InvariantCulture), "observed");

        public string ToJson()
        {
            var builder = new StringBuilder();
            builder.Append('{');
            AppendNumber(builder, "probeVersion", ProbeVersion);
            AppendString(builder, "source", Source);
            AppendString(builder, "endpoint", "/x/v2/dm/web/seg.so");
            AppendString(builder, "aid", Aid);
            AppendString(builder, "cid", Cid);
            AppendNumber(builder, "segmentIndex", SegmentIndex);
            AppendNumber(builder, "field", 9);
            AppendString(builder, "fieldName", "weight");
            AppendNumber(builder, "wireType", 0);
            AppendNumber(builder, "idField", 1);
            AppendString(builder, "idType", "int64");
            if (HttpStatus.HasValue) AppendNumber(builder, "httpStatus", HttpStatus.Value);
            AppendString(builder, "contentType", ContentType);
            if (Bytes.HasValue) AppendNumber(builder, "bytes", Bytes.Value);
            AppendNumber(builder, "totalElems", TotalElements);
            AppendNumber(builder, "presentWeightCount", PresentWeightCount);
            AppendNumber(builder, "absentWeightCount", AbsentWeightCount);
            AppendBool(builder, "wireTypeMismatch", WireTypeMismatch);
            AppendString(builder, "parseError", ParseError);
            AppendString(builder, "failureReason", FailureReason);
            AppendObject(builder, "valueHistogram", WeightHistogram);
            AppendObject(builder, "unknownFields", UnknownFields);
            AppendIdCorrespondence(builder, IdCorrespondence);
            AppendSamples(builder, Samples);
            builder.Append('}');
            return builder.ToString();

            static void AppendString(StringBuilder builder, string key, string? value)
            {
                builder.Append('"').Append(key).Append("\":");
                AppendJsonString(builder, value);
                builder.Append(',');
            }

            static void AppendNumber(StringBuilder builder, string key, int value)
                => builder.Append('"').Append(key).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture)).Append(',');

            static void AppendBool(StringBuilder builder, string key, bool value)
                => builder.Append('"').Append(key).Append("\":").Append(value ? "true" : "false").Append(',');

            static void AppendObject<TValue>(StringBuilder builder, string key, Dictionary<string, TValue> values)
            {
                builder.Append('"').Append(key).Append("\":{");
                var first = true;
                foreach (var pair in values)
                {
                    if (!first) builder.Append(',');
                    AppendJsonString(builder, pair.Key);
                    builder.Append(':');
                    if (pair.Value is int number) builder.Append(number.ToString(CultureInfo.InvariantCulture));
                    else AppendJsonString(builder, pair.Value is null ? null : pair.Value.ToString());
                    first = false;
                }

                builder.Append("},");
            }

            static void AppendIdCorrespondence(StringBuilder builder, IdCorrespondence? value)
            {
                builder.Append("\"idCorrespondence\":");
                if (value is null) builder.Append("null,");
                else builder.Append('{')
                    .Append("\"probeIds\":").Append(value.ProbeIds.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"serviceIds\":").Append(value.ServiceIds.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"matchedIds\":").Append(value.MatchedIds.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"missingInProbe\":").Append(value.MissingInProbe.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"duplicateIds\":").Append(value.DuplicateIds.ToString(CultureInfo.InvariantCulture))
                    .Append(",\"mismatchedIds\":").Append(value.MismatchedIds.ToString(CultureInfo.InvariantCulture))
                    .Append("},");
            }

            static void AppendSamples(StringBuilder builder, List<ProbeSample> samples)
            {
                builder.Append("\"samples\":[");
                for (var i = 0; i < samples.Count; i++)
                {
                    if (i > 0) builder.Append(',');
                    builder.Append('{');
                    AppendJsonString(builder, "id"); builder.Append(':'); AppendJsonString(builder, samples[i].Id);
                    var sample = samples[i];
                    builder.Append(",\"weight\":");
                    if (sample.Weight is int weight) builder.Append(weight.ToString(CultureInfo.InvariantCulture));
                    else builder.Append("null");
                    builder.Append(",\"present\":").Append(sample.Weight.HasValue ? "true" : "false").Append('}');
                }

                builder.Append(']');
            }
        }
    }

    private sealed record ProbeSample(string? Id, int? Weight);
    private sealed record IdCorrespondence(int ProbeIds, int ServiceIds, int MatchedIds, int MissingInProbe, int DuplicateIds, int MismatchedIds);

    private sealed class ProbeElement
    {
        public long Id { get; set; }
        public int Weight { get; set; }
        public bool HasId { get; set; }
        public bool HasWeight { get; set; }
    }
}