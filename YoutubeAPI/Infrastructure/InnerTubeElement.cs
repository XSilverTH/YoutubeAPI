using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YoutubeAPI.Models.Common;
using YoutubeAPI.Models.Videos;

namespace YoutubeAPI.Infrastructure;

internal static partial class InnerTubeElement
{
    [GeneratedRegex(@"([\d\.,]+)\s*([KMBkmb])?")]
    private static partial Regex CountRegex();

    [GeneratedRegex(@"(\d+)\s+(second|minute|hour|day|week|month|year)s?\s+ago", RegexOptions.IgnoreCase)]
    private static partial Regex RelativeDateRegex();

    private static readonly string[] ThumbnailWrapperProperties =
    [
        "thumbnail",
        "thumbnailViewModel",
        "channelThumbnailWithLinkRenderer",
        "decoratedAvatarViewModel",
        "avatar",
        "avatarViewModel",
        "image"
    ];

    private static readonly string[] TextWrapperProperties =
    [
        "dynamicTextViewModel",
        "pageHeaderTitleViewModel",
        "textViewModel",
        "text"
    ];

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
            foreach (var textPropertyName in TextWrapperProperties)
                if (element.TryGetProperty(textPropertyName, out var nested))
                {
                    var text = nested.GetText(string.Empty);
                    if (!string.IsNullOrEmpty(text))
                        return text;
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
                    target = thumbProp;
                else if (element.TryGetProperty("thumbnailViewModel", out var vmProp))
                    target = vmProp;
                else if (element.TryGetProperty("image", out var imageProp))
                    target = imageProp;
                else if (element.TryGetProperty("avatarViewModel", out var avatarProp))
                    target = avatarProp;
                else if (element.TryGetProperty("decoratedAvatarViewModel", out var decoratedAvatarProp))
                    target = decoratedAvatarProp;
            }

            CollectThumbnails(target, list);
            return list;
        }

        private static void CollectThumbnails(JsonElement node, List<Thumbnail> thumbnails)
        {
            if (node.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in node.EnumerateArray())
                    CollectThumbnails(item, thumbnails);

                return;
            }

            if (node.ValueKind != JsonValueKind.Object)
                return;

            if (node.TryGetProperty("url", out var urlElement) &&
                urlElement.ValueKind == JsonValueKind.String)
            {
                var url = urlElement.GetString();
                if (!string.IsNullOrWhiteSpace(url))
                {
                    if (url.StartsWith("//", StringComparison.Ordinal)) url = "https:" + url;
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    {
                        var width = node.TryGetProperty("width", out var widthElement) &&
                                    widthElement.TryGetInt32(out var widthValue)
                            ? widthValue
                            : 0;
                        var height = node.TryGetProperty("height", out var heightElement) &&
                                     heightElement.TryGetInt32(out var heightValue)
                            ? heightValue
                            : 0;
                        thumbnails.Add(new Thumbnail(uri, width, height));
                    }
                }

                return;
            }

            if (node.TryGetProperty("thumbnails", out var nestedThumbnails))
            {
                CollectThumbnails(nestedThumbnails, thumbnails);
                return;
            }

            if (node.TryGetProperty("sources", out var sources))
            {
                CollectThumbnails(sources, thumbnails);
                return;
            }

            foreach (var propertyName in ThumbnailWrapperProperties)
                if (node.TryGetProperty(propertyName, out var nested))
                {
                    CollectThumbnails(nested, thumbnails);
                    return;
                }
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

    public static TimeSpan? ParseVideoDuration(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        var duration = ParseDuration(element.GetText("lengthText"));
        if (duration is not null)
            return duration;

        if (element.TryGetProperty("thumbnailOverlays", out var thumbnailOverlays))
        {
            duration = ParseClassicDurationOverlay(thumbnailOverlays);
            if (duration is not null)
                return duration;
        }

        var thumbnailViewModel = element.GetPropertyOrDefault("contentImage")
            .GetPropertyOrDefault("thumbnailViewModel");
        if (thumbnailViewModel.ValueKind == JsonValueKind.Object &&
            thumbnailViewModel.TryGetProperty("overlays", out var overlays))
            return ParseModernDurationOverlay(overlays);

        return null;
    }

    private static TimeSpan? ParseClassicDurationOverlay(JsonElement overlays)
    {
        if (overlays.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var overlay in overlays.EnumerateArray())
        {
            if (!overlay.TryGetProperty("thumbnailOverlayTimeStatusRenderer", out var timeStatus))
                continue;

            var duration = ParseDuration(timeStatus.GetText("text"));
            if (duration is not null)
                return duration;
        }

        return null;
    }

    private static TimeSpan? ParseModernDurationOverlay(JsonElement overlays)
    {
        if (overlays.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var overlay in overlays.EnumerateArray())
        {
            if (overlay.TryGetProperty("thumbnailOverlayTimeStatusRenderer", out var timeStatus))
            {
                var duration = ParseDuration(timeStatus.GetText("text"));
                if (duration is not null)
                    return duration;
            }

            if (overlay.TryGetProperty("thumbnailBottomOverlayViewModel", out var bottomOverlay))
            {
                var duration = ParseModernBadgeDuration(bottomOverlay.GetPropertyOrDefault("badges"));
                duration ??= ParseModernBadgeDuration(bottomOverlay.GetPropertyOrDefault("badge"));
                if (duration is not null)
                    return duration;
            }

            if (overlay.TryGetProperty("thumbnailOverlayBadgeViewModel", out var badgeOverlay))
            {
                var duration = ParseModernBadgeDuration(badgeOverlay.GetPropertyOrDefault("thumbnailBadges"));
                if (duration is not null)
                    return duration;
            }
        }

        return null;
    }

    private static TimeSpan? ParseModernBadgeDuration(JsonElement badges)
    {
        if (badges.ValueKind == JsonValueKind.Object)
        {
            var badgeViewModel = badges.TryGetProperty("thumbnailBadgeViewModel", out var viewModel)
                ? viewModel
                : badges;
            return ParseDuration(badgeViewModel.GetText("text"));
        }

        if (badges.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var badge in badges.EnumerateArray())
        {
            var badgeViewModel = badge.TryGetProperty("thumbnailBadgeViewModel", out var viewModel)
                ? viewModel
                : badge;
            var duration = ParseDuration(badgeViewModel.GetText("text"));
            if (duration is not null)
                return duration;
        }

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

    /// <summary>
    ///     Parses viewer-specific watch progress and saved resume state from any InnerTube response shape.
    /// </summary>
    public static VideoPlaybackProgress? ParsePlaybackProgress(JsonElement element)
    {
        var state = new PlaybackProgressState();
        CollectPlaybackProgress(element, state);

        if (!state.WatchedPercent.HasValue && !state.ResumePosition.HasValue && !state.IsCompleted)
            return null;

        var watchedFraction = state.WatchedPercent / 100;
        if (watchedFraction is < 0 or > 1)
            watchedFraction = null;

        return new VideoPlaybackProgress(watchedFraction, state.ResumePosition,
            state.IsCompleted || watchedFraction >= 1);
    }

    private static void CollectPlaybackProgress(JsonElement element, PlaybackProgressState state,
        bool allowWatchEndpointStartTime = false)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
                CollectPlaybackProgress(child, state, allowWatchEndpointStartTime);
            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
            return;

        foreach (var property in element.EnumerateObject())
        {
            var name = property.Name;
            var value = property.Value;
            if (value.ValueKind == JsonValueKind.String &&
                IsCompletionLabel(value.GetString()))
                state.IsCompleted = true;
            else if (name.Equals("percentDurationWatched", StringComparison.OrdinalIgnoreCase) ||
                     name.Equals("percentWatched", StringComparison.OrdinalIgnoreCase) ||
                     name.Equals("watchedPercent", StringComparison.OrdinalIgnoreCase))
                state.SetWatchedPercent(ParseFiniteNumber(value), 3);
            else if (name.Equals("startPercent", StringComparison.OrdinalIgnoreCase))
                state.SetWatchedPercent(ParseFiniteNumber(value), 2);
            else if (name.Equals("endPercent", StringComparison.OrdinalIgnoreCase))
                state.SetWatchedPercent(ParseFiniteNumber(value), 1);
            else if (name.Equals("isWatched", StringComparison.OrdinalIgnoreCase) ||
                     name.Equals("isCompleted", StringComparison.OrdinalIgnoreCase))
            {
                if (value.ValueKind is JsonValueKind.True or JsonValueKind.False && value.GetBoolean())
                    state.IsCompleted = true;
            }
            else if (name.Equals("watchState", StringComparison.OrdinalIgnoreCase) &&
                     value.ValueKind == JsonValueKind.String &&
                     value.GetString()?.Contains("COMPLETE", StringComparison.OrdinalIgnoreCase) == true)
                state.IsCompleted = true;
            else if (allowWatchEndpointStartTime && name.Equals("startTimeSeconds", StringComparison.OrdinalIgnoreCase))
                state.SetResumePosition(ParseResumePosition("resumePlaybackPositionSeconds", value));
            else if (IsResumePositionName(name))
                state.SetResumePosition(ParseResumePosition(name, value));

            var childAllowsStartTime = allowWatchEndpointStartTime &&
                                       name.Equals("watchEndpoint", StringComparison.OrdinalIgnoreCase);
            if (name.Equals("currentVideoEndpoint", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("navigationEndpoint", StringComparison.OrdinalIgnoreCase))
                childAllowsStartTime = true;
            CollectPlaybackProgress(value, state, childAllowsStartTime);
        }
    }

    private static bool IsCompletionLabel(string? value)
    {
        return string.Equals(value?.Trim(), "WATCHED", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value?.Trim(), "COMPLETED", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsResumePositionName(string name)
    {
        return name.Equals("resumePlaybackPositionMs", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("resumePlaybackPositionSeconds", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("playbackPositionMs", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("playbackPositionSeconds", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("savedPlaybackPositionMs", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("savedPlaybackPositionSeconds", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("resumePositionMs", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("resumePositionSeconds", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("playbackPosition", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("resumePosition", StringComparison.OrdinalIgnoreCase);
    }

    private static TimeSpan? ParseResumePosition(string name, JsonElement value)
    {
        var number = ParseFiniteNumber(value);
        if (!number.HasValue || number < 0)
            return null;

        var milliseconds = name.EndsWith("Ms", StringComparison.OrdinalIgnoreCase)
            ? number.Value
            : number.Value * 1000;
        if (milliseconds > TimeSpan.MaxValue.TotalMilliseconds)
            return null;

        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private static double? ParseFiniteNumber(JsonElement value)
    {
        double number;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out number) ||
            value.ValueKind == JsonValueKind.String &&
            double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
            return double.IsFinite(number) ? number : null;

        return null;
    }

    private sealed class PlaybackProgressState
    {
        public double? WatchedPercent { get; private set; }
        public int WatchedPercentPriority { get; private set; }
        public TimeSpan? ResumePosition { get; private set; }
        public bool IsCompleted { get; set; }

        public void SetWatchedPercent(double? value, int priority)
        {
            if (!value.HasValue || value is < 0 or > 100 ||
                WatchedPercentPriority > priority)
                return;

            WatchedPercent = value;
            WatchedPercentPriority = priority;
        }

        public void SetResumePosition(TimeSpan? value)
        {
            if (value.HasValue && !ResumePosition.HasValue)
                ResumePosition = value;
        }
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