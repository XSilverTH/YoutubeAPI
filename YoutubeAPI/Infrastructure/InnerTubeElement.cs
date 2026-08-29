using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YoutubeAPI.Models.Common;

namespace YoutubeAPI.Infrastructure;

internal static partial class InnerTubeElement
{
    [GeneratedRegex(@"([\d\.,]+)\s*([KMBkmb])?")]
    private static partial Regex CountRegex();

    [GeneratedRegex(@"(\d+)\s+(second|minute|hour|day|week|month|year)s?\s+ago", RegexOptions.IgnoreCase)]
    private static partial Regex RelativeDateRegex();

    extension(JsonElement element)
    {
        public JsonElement GetPropertyOrDefault(string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var prop))
                return prop;
            return default;
        }

        public string GetText(string propertyName = "text")
        {
            if (element.ValueKind == JsonValueKind.String) return element.GetString() ?? string.Empty;

            if (element.ValueKind != JsonValueKind.Object) return string.Empty;

            if (!string.IsNullOrEmpty(propertyName) && element.TryGetProperty(propertyName, out var target))
                return target.GetText(string.Empty);

            if (element.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String)
                return contentEl.GetString() ?? string.Empty;

            if (element.TryGetProperty("simpleText", out var simpleTextEl) &&
                simpleTextEl.ValueKind == JsonValueKind.String) return simpleTextEl.GetString() ?? string.Empty;

            if (element.TryGetProperty("runs", out var runsEl) && runsEl.ValueKind == JsonValueKind.Array)
            {
                var sb = new StringBuilder();
                foreach (var run in runsEl.EnumerateArray())
                    if (run.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                        sb.Append(textEl.GetString());
                    else if (run.TryGetProperty("content", out var runContentEl) &&
                             runContentEl.ValueKind == JsonValueKind.String) sb.Append(runContentEl.GetString());

                return sb.ToString();
            }

            if (element.TryGetProperty("accessibility", out var accessibility) &&
                accessibility.TryGetProperty("accessibilityData", out var accessibilityData) &&
                accessibilityData.TryGetProperty("label", out var label) &&
                label.ValueKind == JsonValueKind.String)
                return label.GetString() ?? string.Empty;

            return string.Empty;
        }

        public IReadOnlyList<Thumbnail> GetThumbnails(string propertyName = "thumbnails")
        {
            var list = new List<Thumbnail>();

            var target = element;
            if (!string.IsNullOrEmpty(propertyName) && element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty(propertyName, out var prop))
                    target = prop;
                else if (element.TryGetProperty("thumbnail", out var thumbProp))
                    target = thumbProp.TryGetProperty("thumbnails", out var subProp) ? subProp : thumbProp;
                else if (element.TryGetProperty("thumbnailViewModel", out var vmProp))
                    target = vmProp.TryGetProperty("image", out var img) && img.TryGetProperty("sources", out var src)
                        ? src
                        : vmProp;
                else if (element.TryGetProperty("image", out var imgProp) &&
                         imgProp.TryGetProperty("sources", out var sources))
                    target = sources;
                else if (element.TryGetProperty("avatarViewModel", out var avm) &&
                         avm.TryGetProperty("image", out var avmImg) &&
                         avmImg.TryGetProperty("sources", out var avmSrc)) target = avmSrc;
            }

            target = target.ValueKind switch
            {
                JsonValueKind.Object when target.TryGetProperty("thumbnails", out var innerThumbs) => innerThumbs,
                JsonValueKind.Object when target.TryGetProperty("sources", out var innerSources) => innerSources,
                _ => target
            };

            if (target.ValueKind != JsonValueKind.Array) return list;
            foreach (var item in target.EnumerateArray())
            {
                var urlStr = item.TryGetProperty("url", out var u) ? u.GetString() : null;
                if (string.IsNullOrWhiteSpace(urlStr))
                    continue;

                if (urlStr.StartsWith("//", StringComparison.Ordinal)) urlStr = "https:" + urlStr;

                if (!Uri.TryCreate(urlStr, UriKind.Absolute, out var uri)) continue;
                var width = item.TryGetProperty("width", out var w) && w.TryGetInt32(out var widthVal)
                    ? widthVal
                    : 0;
                var height = item.TryGetProperty("height", out var h) && h.TryGetInt32(out var heightVal)
                    ? heightVal
                    : 0;
                list.Add(new Thumbnail(uri, width, height));
            }

            return list;
        }

        public bool IsVerified()
        {
            if (element.ValueKind != JsonValueKind.Object)
                return false;

            if (element.TryGetProperty("isVerified", out var v) &&
                v.ValueKind is JsonValueKind.True or JsonValueKind.False)
                return v.GetBoolean();

            if (element.TryGetProperty("ownerBadges", out var ownerBadges) &&
                ownerBadges.ValueKind == JsonValueKind.Array)
                foreach (var badge in ownerBadges.EnumerateArray())
                    if (badge.TryGetProperty("metadataBadgeRenderer", out var mbr))
                    {
                        var style = mbr.TryGetProperty("style", out var s) ? s.GetString() ?? "" : "";
                        if (style.Contains("VERIFIED", StringComparison.OrdinalIgnoreCase))
                            return true;
                    }

            if (element.TryGetProperty("badges", out var badges) && badges.ValueKind == JsonValueKind.Array)
                foreach (var badge in badges.EnumerateArray())
                    if (badge.TryGetProperty("metadataBadgeRenderer", out var mbr))
                    {
                        var style = mbr.TryGetProperty("style", out var s) ? s.GetString() ?? "" : "";
                        if (style.Contains("VERIFIED", StringComparison.OrdinalIgnoreCase))
                            return true;
                    }

            var accessibility = element.GetText("accessibility");
            return accessibility.Contains("verified", StringComparison.OrdinalIgnoreCase);
        }

        public (string? Token, string? TrackingParams) ExtractContinuation()
        {
            if (element.ValueKind != JsonValueKind.Object)
                return (null, null);

            if (element.TryGetProperty("continuationItemRenderer", out var cir))
            {
                var tracking = cir.TryGetProperty("trackingParams", out var tp) ? tp.GetString() : null;
                if (cir.TryGetProperty("continuationEndpoint", out var ep) &&
                    ep.TryGetProperty("continuationCommand", out var cc) &&
                    cc.TryGetProperty("token", out var tok))
                    return (tok.GetString(), tracking);
            }

            if (element.TryGetProperty("continuationEndpoint", out var cep) &&
                cep.TryGetProperty("continuationCommand", out var ccmd) &&
                ccmd.TryGetProperty("token", out var tok2))
            {
                var tracking = element.TryGetProperty("trackingParams", out var tp) ? tp.GetString() : null;
                return (tok2.GetString(), tracking);
            }

            if (element.TryGetProperty("nextContinuationData", out var ncd) &&
                ncd.TryGetProperty("continuation", out var tok3))
            {
                var tracking = ncd.TryGetProperty("clickTrackingParams", out var tp) ? tp.GetString() : null;
                return (tok3.GetString(), tracking);
            }

            if (!element.TryGetProperty("reloadContinuationData", out var rcd) ||
                !rcd.TryGetProperty("continuation", out var tok4)) return (null, null);
            {
                var tracking = rcd.TryGetProperty("clickTrackingParams", out var tp) ? tp.GetString() : null;
                return (tok4.GetString(), tracking);
            }
        }
    }

    public static TimeSpan? ParseDuration(string? durationText)
    {
        if (string.IsNullOrWhiteSpace(durationText))
            return null;

        var clean = durationText.Trim();
        var parts = clean.Split(':');
        switch (parts.Length)
        {
            case 2 when int.TryParse(parts[0], out var minutes) && int.TryParse(parts[1], out var seconds):
                return new TimeSpan(0, minutes, seconds);
            case 3 when int.TryParse(parts[0], out var hours) && int.TryParse(parts[1], out var min) &&
                        int.TryParse(parts[2], out var sec):
                return new TimeSpan(hours, min, sec);
        }

        if (long.TryParse(clean, out var totalSeconds)) return TimeSpan.FromSeconds(totalSeconds);

        return null;
    }

    public static long? ParseCount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var trimmed = text.Trim();
        // Match numbers like 1,234,567 or 1.2M or 500K or 500
        var match = CountRegex().Match(trimmed);
        if (!match.Success)
            return null;

        var numStr = match.Groups[1].Value.Replace(",", "");
        if (!double.TryParse(numStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
            return null;

        var multiplier = match.Groups[2].Value.ToUpperInvariant();
        switch (multiplier)
        {
            case "K":
                num *= 1_000;
                break;
            case "M":
                num *= 1_000_000;
                break;
            case "B":
                num *= 1_000_000_000;
                break;
        }

        return (long)num;
    }

    public static DateTimeOffset? ParseRelativeDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var trimmed = text.Trim();
        if (DateTimeOffset.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
            return dto;

        var match = RelativeDateRegex().Match(trimmed);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var amount)) return null;
        var unit = match.Groups[2].Value.ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;
        return unit switch
        {
            "second" => now.AddSeconds(-amount),
            "minute" => now.AddMinutes(-amount),
            "hour" => now.AddHours(-amount),
            "day" => now.AddDays(-amount),
            "week" => now.AddDays(-amount * 7),
            "month" => now.AddMonths(-amount),
            "year" => now.AddYears(-amount),
            _ => null
        };
    }
}