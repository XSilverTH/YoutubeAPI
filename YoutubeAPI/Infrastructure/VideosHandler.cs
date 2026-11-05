using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using YoutubeAPI.Exceptions;
using YoutubeAPI.Models.Channels;
using YoutubeAPI.Models.Common;
using YoutubeAPI.Models.Enums;
using YoutubeAPI.Models.ValueTypes;
using YoutubeAPI.Models.Videos;

namespace YoutubeAPI.Infrastructure;

internal sealed partial class VideosHandler(InnerTubeSession session) : IYouTubeVideosHandler
{
    [GeneratedRegex("\"captionTracks\"\\s*:\\s*(\\[[^\\]]+\\])")]
    private static partial Regex CaptionTracksRegex();
    public async Task<Video> GetAsync(VideoId videoId, CancellationToken cancellationToken)
    {
        try
        {
            var playerTask = FetchPlayerDataAsync(videoId, cancellationToken);
            var nextTask = FetchNextAsync(videoId, cancellationToken);

            await Task.WhenAll(playerTask, nextTask).ConfigureAwait(false);

            var playerData = await playerTask.ConfigureAwait(false);
            var nextData = await nextTask.ConfigureAwait(false);

            if (playerData == null && nextData == null)
                throw new ResourceNotFoundException($"Video '{videoId}' was not found or is unavailable.");

            var title = playerData?.Title ?? nextData?.Title;
            if (string.IsNullOrWhiteSpace(title))
            {
                if (playerData?.PlayabilityStatus is { } status &&
                    !string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(status, "LOGIN_REQUIRED", StringComparison.OrdinalIgnoreCase))
                    {
                        if (session.Options.Authentication != null)
                            throw new AuthenticationExpiredException(
                                playerData.PlayabilityReason ?? "This video requires login.");
                        throw new AuthenticationRequiredException(
                            playerData.PlayabilityReason ?? "This video requires login.");
                    }

                    throw new ResourceUnavailableException(
                        $"Video '{videoId}' is unavailable: {InnerTubeSession.Sanitize(playerData.PlayabilityReason ?? status)}");
                }

                throw new ResourceNotFoundException($"Video '{videoId}' was not found or is unavailable.");
            }

            var channelId = playerData?.ChannelId ?? nextData?.ChannelId ?? new ChannelId("UC0000000000000000000000");
            var channelTitle = playerData?.ChannelTitle ?? nextData?.ChannelTitle ?? "Unknown";
            var channelUrl = playerData?.ChannelUrl ??
                             (!string.IsNullOrWhiteSpace(channelId.Value)
                                 ? new Uri($"https://www.youtube.com/channel/{channelId}")
                                 : new Uri("https://www.youtube.com"));

            var channelSummary = new ChannelSummary(
                channelId,
                channelTitle,
                nextData?.ChannelHandle,
                channelUrl,
                nextData?.ChannelThumbnails ?? [],
                nextData?.IsVerified ?? false,
                nextData?.SubscriberCount);

            var thumbs = new List<Thumbnail>();
            if (playerData?.Thumbnails != null && playerData.Thumbnails.Count > 0)
                thumbs.AddRange(playerData.Thumbnails);
            if (thumbs.Count == 0 && nextData?.Thumbnails != null && nextData.Thumbnails.Count > 0)
                thumbs.AddRange(nextData.Thumbnails);
            if (thumbs.Count == 0)
                thumbs.Add(new Thumbnail(new Uri($"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg"), 480, 360));

            var stats = new VideoStatistics(
                playerData?.ViewCount ?? nextData?.ViewCount,
                playerData?.LikeCount ?? nextData?.LikeCount,
                nextData?.CommentCount);

            var duration = playerData?.Duration ?? nextData?.Duration;
            var keywords = playerData?.Keywords ?? nextData?.Keywords ?? [];

            var isShort = (nextData?.IsShort == true) ||
                          (duration is { TotalSeconds: > 0 and <= 60 } &&
                           (title.Contains("#shorts", StringComparison.OrdinalIgnoreCase) ||
                            title.Contains("#short", StringComparison.OrdinalIgnoreCase) ||
                            keywords.Any(k => k.Contains("shorts", StringComparison.OrdinalIgnoreCase))));

            var summary = new VideoSummary(
                videoId,
                title,
                channelSummary,
                duration,
                new Uri($"https://www.youtube.com/watch?v={videoId}"),
                thumbs,
                nextData?.PublishedText,
                playerData?.PublishedAt ?? nextData?.PublishedAt,
                isShort,
                stats);

            var description = playerData?.Description ?? nextData?.Description ?? string.Empty;
            var uploadDate = playerData?.UploadDate ?? nextData?.UploadDate;
            var liveState = playerData is { LiveState: not LiveBroadcastState.None }
                ? playerData.LiveState
                : nextData?.LiveState ?? LiveBroadcastState.None;

            return new Video(
                summary,
                description,
                keywords,
                uploadDate,
                liveState)
            {
                PlaybackProgress = nextData?.PlaybackProgress
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (YouTubeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new YouTubeRequestException(
                $"Failed to load video '{videoId}': {InnerTubeSession.Sanitize(ex.Message)}", "video.get", null, ex);
        }
    }

    public async Task<VideoPlaybackProgress?> GetPlaybackProgressAsync(VideoId videoId,
        CancellationToken cancellationToken)
    {
        try
        {
            var nextTask = FetchNextAsync(videoId, cancellationToken);
            var playerTask = FetchPlaybackProgressCoreAsync(videoId, cancellationToken);
            await Task.WhenAll(nextTask, playerTask).ConfigureAwait(false);

            return MergePlaybackProgress(
                await playerTask.ConfigureAwait(false),
                (await nextTask.ConfigureAwait(false))?.PlaybackProgress);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new YouTubeRequestException(
                $"Failed to load playback progress for '{videoId}': {InnerTubeSession.Sanitize(ex.Message)}",
                "playback.progress", null, ex);
        }
    }

    private static VideoPlaybackProgress? MergePlaybackProgress(
        VideoPlaybackProgress? primary,
        VideoPlaybackProgress? secondary)
    {
        if (primary == null)
            return secondary;
        if (secondary == null)
            return primary;

        return new VideoPlaybackProgress(
            primary.WatchedFraction ?? secondary.WatchedFraction,
            primary.ResumePosition ?? secondary.ResumePosition,
            primary.IsCompleted || secondary.IsCompleted);
    }

    private async Task<VideoPlaybackProgress?> FetchPlaybackProgressCoreAsync(VideoId videoId,
        CancellationToken cancellationToken)
    {
        using var playerDoc = await FetchPlayerDocAsync(videoId, cancellationToken).ConfigureAwait(false);
        if (playerDoc == null)
            return null;

        var overlays = InnerTubeElement.ParsePlaybackProgress(
            playerDoc.RootElement.GetPropertyOrDefault("playerOverlays"));
        var endpoint = InnerTubeElement.ParsePlaybackProgress(
            playerDoc.RootElement.GetPropertyOrDefault("currentVideoEndpoint"));
        return MergePlaybackProgress(overlays, endpoint);
    }

    public async Task<IReadOnlyList<TranscriptTrack>> GetTranscriptTracksAsync(VideoId videoId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var playerDoc = await FetchPlayerDocAsync(videoId, cancellationToken).ConfigureAwait(false);
            if (playerDoc != null)
            {
                var tracks = ParseCaptionTracks(playerDoc.RootElement);
                if (tracks.Count > 0)
                    return tracks;
            }

            using var watchDoc = await FetchWatchHtmlCaptionsDocAsync(videoId, cancellationToken).ConfigureAwait(false);
            if (watchDoc != null)
            {
                var tracks = ParseCaptionTracks(watchDoc.RootElement);
                if (tracks.Count > 0)
                    return tracks;
            }

            return [];
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (YouTubeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new YouTubeRequestException(
                $"Failed to load transcript tracks for '{videoId}': {InnerTubeSession.Sanitize(ex.Message)}",
                "transcripts.list", null, ex);
        }
    }

    public async Task<Transcript> GetTranscriptAsync(VideoId videoId, TranscriptTrackId trackId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var playerDoc = await FetchPlayerDocAsync(videoId, cancellationToken).ConfigureAwait(false);
            if (playerDoc != null)
            {
                var directTrack = FindCaptionTrackElement(playerDoc.RootElement, trackId.Value);
                if (directTrack.HasValue && TryGetCaptionTrackBaseUrl(directTrack.Value, out var baseUrl, out var track))
                {
                    var cues = await FetchCuesFromBaseUrlAsync(baseUrl, cancellationToken).ConfigureAwait(false);
                    return new Transcript(videoId, track, cues);
                }
            }

            using var watchDoc = await FetchWatchHtmlCaptionsDocAsync(videoId, cancellationToken).ConfigureAwait(false);
            if (watchDoc != null)
            {
                var watchTrack = FindCaptionTrackElement(watchDoc.RootElement, trackId.Value);
                if (watchTrack.HasValue && TryGetCaptionTrackBaseUrl(watchTrack.Value, out var baseUrl, out var track))
                {
                    var cues = await FetchCuesFromBaseUrlAsync(baseUrl, cancellationToken).ConfigureAwait(false);
                    return new Transcript(videoId, track, cues);
                }
            }

            throw new ResourceNotFoundException(
                $"Transcript track '{trackId}' was not found for video '{videoId}'.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (YouTubeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new YouTubeRequestException(
                $"Failed to load transcript for '{videoId}' track '{trackId}': {InnerTubeSession.Sanitize(ex.Message)}",
                "transcript.get", null, ex);
        }
    }

    private static bool TryGetCaptionTrackBaseUrl(JsonElement trackElement, out string baseUrl, out TranscriptTrack track)
    {
        baseUrl = trackElement.TryGetProperty("baseUrl", out var bu) ? bu.GetString() ?? "" : "";
        if (string.IsNullOrEmpty(baseUrl))
        {
            track = null!;
            return false;
        }

        var vssId = trackElement.TryGetProperty("vssId", out var vi) ? vi.GetString() : null;
        var langCode = trackElement.TryGetProperty("languageCode", out var lc) ? lc.GetString() ?? "" : "";
        var displayName = trackElement.GetText("name");
        var kind = trackElement.TryGetProperty("kind", out var k) ? k.GetString() ?? "" : "";
        var isAuto = kind.Equals("asr", StringComparison.OrdinalIgnoreCase) ||
                     vssId?.StartsWith("a.", StringComparison.OrdinalIgnoreCase) == true;

        var idValue = !string.IsNullOrEmpty(vssId) ? vssId : langCode;
        if (!TranscriptTrackId.TryParse(idValue, out var trackId))
            trackId = new TranscriptTrackId("default");

        track = new TranscriptTrack(trackId, langCode, displayName, isAuto);
        return true;
    }

    private async Task<PlayerVideoData?> FetchPlayerDataAsync(VideoId videoId, CancellationToken cancellationToken)
    {
        using var playerDoc = await FetchPlayerDocAsync(videoId, cancellationToken).ConfigureAwait(false);
        return playerDoc == null ? null : ParsePlayerResponse(playerDoc.RootElement);
    }

    private async Task<NextVideoData?> FetchNextAsync(VideoId videoId, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = await session.PostInnerTubeAsync(
                "next",
                writer => { writer.WriteString("videoId", videoId.Value); },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ParseNextResponse(doc.RootElement);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static NextVideoData ParseNextResponse(JsonElement root)
    {
        var progressFromOverlays = InnerTubeElement.ParsePlaybackProgress(
            root.GetPropertyOrDefault("playerOverlays"));
        var progressFromEndpoint = InnerTubeElement.ParsePlaybackProgress(
            root.GetPropertyOrDefault("currentVideoEndpoint"));
        var currentEndpoint = root.GetPropertyOrDefault("currentVideoEndpoint");
        var isShort = IsShortEndpoint(currentEndpoint) ||
                      root.TryGetProperty("reelWatchEndpoint", out _) ||
                      root.GetPropertyOrDefault("playerOverlays").TryGetProperty("reelPlayerOverlayRenderer", out _) ||
                      root.GetPropertyOrDefault("playerOverlays").TryGetProperty("reelPlayerHeaderRenderer", out _);
        var data = new NextVideoData
        {
            PlaybackProgress = MergePlaybackProgress(progressFromOverlays, progressFromEndpoint),
            IsShort = isShort
        };

        if (!root.TryGetProperty("contents", out var contents) ||
            !contents.TryGetProperty("twoColumnWatchNextResults", out var watchNext) ||
            !watchNext.TryGetProperty("results", out var results) ||
            !results.TryGetProperty("results", out var innerResults) ||
            !innerResults.TryGetProperty("contents", out var resultItems) ||
            resultItems.ValueKind != JsonValueKind.Array) return data;
        foreach (var item in resultItems.EnumerateArray())
            if (item.TryGetProperty("videoPrimaryInfoRenderer", out var primary))
            {
                data.Title = primary.GetText("title");
                var viewText = primary.GetText("viewCount");
                data.ViewCount = InnerTubeElement.ParseCount(viewText);
                var relDate = primary.GetText("dateText");
                data.PublishedText = relDate;
                data.PublishedAt = InnerTubeElement.ParseRelativeDate(relDate);
            }
            else if (item.TryGetProperty("videoSecondaryInfoRenderer", out var secondary))
            {
                if (secondary.TryGetProperty("owner", out var owner) &&
                    owner.TryGetProperty("videoOwnerRenderer", out var vor))
                {
                    data.ChannelTitle = vor.GetText("title");
                    if (string.IsNullOrWhiteSpace(data.ChannelTitle))
                        data.ChannelTitle = vor.GetText("attributedTitle");
                    var subText = vor.GetText("subscriberCountText");
                    data.SubscriberCount = InnerTubeElement.ParseCount(subText);
                    data.ChannelThumbnails = vor.GetThumbnails("thumbnail");
                    data.IsVerified = vor.IsVerified();

                    if (vor.TryGetProperty("navigationEndpoint", out var nav) &&
                        nav.TryGetProperty("browseEndpoint", out var be) &&
                        be.TryGetProperty("browseId", out var bid) &&
                        ChannelId.TryParse(bid.GetString(), out var cid))
                    {
                        data.ChannelId = cid;
                    }
                    else if (vor.FindBrowseId() is { } foundId && ChannelId.TryParse(foundId, out var parsedCid))
                    {
                        data.ChannelId = parsedCid;
                    }
                }
                var desc = secondary.GetText("description");
                if (string.IsNullOrEmpty(desc)) desc = secondary.GetText("attributedDescription");
                data.Description = desc;
            }

        return data;
    }

    private static bool IsShortEndpoint(JsonElement endpoint)
    {
        if (endpoint.ValueKind != JsonValueKind.Object)
            return false;

        if (endpoint.TryGetProperty("reelWatchEndpoint", out _))
            return true;

        if (endpoint.TryGetProperty("commandMetadata", out var cmdMeta) &&
            cmdMeta.TryGetProperty("webCommandMetadata", out var webCmd))
        {
            if (webCmd.TryGetProperty("url", out var urlEl) &&
                urlEl.GetString() is { } url &&
                url.Contains("/shorts/", StringComparison.OrdinalIgnoreCase))
                return true;

            if (webCmd.TryGetProperty("webPageType", out var wptEl) &&
                wptEl.GetString() is { } wpt &&
                wpt.Contains("SHORTS", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (endpoint.TryGetProperty("url", out var directUrl) &&
            directUrl.GetString() is { } dUrl &&
            dUrl.Contains("/shorts/", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private async Task<JsonDocument?> FetchPlayerDocAsync(VideoId videoId, CancellationToken cancellationToken)
    {
        try
        {
            return await session.PostInnerTubeAsync(
                "player",
                writer =>
                {
                    writer.WriteString("videoId", videoId.Value);
                    writer.WriteBoolean("contentCheckOk", true);
                    writer.WriteBoolean("racyCheckOk", true);
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ResourceNotFoundException)
        {
            return null;
        }
        catch (ResourceUnavailableException)
        {
            return null;
        }
    }

    private static List<TranscriptTrack> ParseCaptionTracks(JsonElement root)
    {
        var tracks = root;
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("captions", out var captions) &&
                captions.TryGetProperty("playerCaptionsTracklistRenderer", out var pctr) &&
                pctr.TryGetProperty("captionTracks", out var ct))
            {
                tracks = ct;
            }
            else if (root.TryGetProperty("captionTracks", out var directCt))
            {
                tracks = directCt;
            }
            else
            {
                return [];
            }
        }

        if (tracks.ValueKind != JsonValueKind.Array) return [];

        var list = new List<TranscriptTrack>();
        foreach (var track in tracks.EnumerateArray())
        {
            var vssId = track.TryGetProperty("vssId", out var vi) ? vi.GetString() : null;
            var langCode = track.TryGetProperty("languageCode", out var lc) ? lc.GetString() : null;
            var name = track.GetText("name");
            var kind = track.TryGetProperty("kind", out var k) ? k.GetString() ?? "" : "";
            var isAuto = kind.Equals("asr", StringComparison.OrdinalIgnoreCase) ||
                         vssId?.StartsWith("a.", StringComparison.OrdinalIgnoreCase) == true;

            var idValue = !string.IsNullOrEmpty(vssId) ? vssId : langCode ?? "default";
            if (TranscriptTrackId.TryParse(idValue, out var trackId))
                list.Add(new TranscriptTrack(trackId, langCode ?? "en", name, isAuto));
        }

        return list;
    }

    private static JsonElement? FindCaptionTrackElement(JsonElement root, string trackIdValue)
    {
        var tracks = root;
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("captions", out var captions) &&
                captions.TryGetProperty("playerCaptionsTracklistRenderer", out var pctr) &&
                pctr.TryGetProperty("captionTracks", out var ct))
            {
                tracks = ct;
            }
            else if (root.TryGetProperty("captionTracks", out var directCt))
            {
                tracks = directCt;
            }
            else
            {
                return null;
            }
        }

        if (tracks.ValueKind != JsonValueKind.Array) return null;

        foreach (var track in tracks.EnumerateArray())
        {
            var vssId = track.TryGetProperty("vssId", out var vi) ? vi.GetString() : null;
            var langCode = track.TryGetProperty("languageCode", out var lc) ? lc.GetString() : null;

            if (string.Equals(vssId, trackIdValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(langCode, trackIdValue, StringComparison.OrdinalIgnoreCase))
                return track;
        }

        foreach (var track in tracks.EnumerateArray())
        {
            var vssId = track.TryGetProperty("vssId", out var vi) ? vi.GetString() : null;
            var langCode = track.TryGetProperty("languageCode", out var lc) ? lc.GetString() : null;

            if (langCode?.StartsWith(trackIdValue, StringComparison.OrdinalIgnoreCase) == true ||
                vssId?.EndsWith(trackIdValue, StringComparison.OrdinalIgnoreCase) == true)
                return track;
        }

        return null;
    }

    private async Task<JsonDocument?> FetchWatchHtmlCaptionsDocAsync(VideoId videoId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://www.youtube.com/watch?v={videoId}&bpctr=9999999999&hl={session.Options.Language}");
            request.Headers.UserAgent.ParseAdd(InnerTubeSession.DefaultUserAgent);

            using var response = await session.HttpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return null;

            var html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var match = CaptionTracksRegex().Match(html);
            if (!match.Success)
                return null;

            return JsonDocument.Parse(match.Groups[1].Value);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<TranscriptCue>> FetchCuesFromBaseUrlAsync(string baseUrl,
        CancellationToken cancellationToken)
    {
        var list = new List<TranscriptCue>();
        Exception? lastException = null;

        // Try JSON3 format first.
        var jsonUrl = baseUrl.Contains('?') ? $"{baseUrl}&fmt=json3" : $"{baseUrl}?fmt=json3";
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, jsonUrl);
            req.Headers.UserAgent.ParseAdd(InnerTubeSession.DefaultUserAgent);
            using var res = await session.HttpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
            if (res.IsSuccessStatusCode)
            {
                using var jsonDoc = await JsonDocument
                    .ParseAsync(await res.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                if (jsonDoc.RootElement.TryGetProperty("events", out var events) &&
                    events.ValueKind == JsonValueKind.Array)
                {
                    foreach (var evt in events.EnumerateArray())
                        if (evt.TryGetProperty("segs", out var segs) && segs.ValueKind == JsonValueKind.Array)
                        {
                            var sb = new StringBuilder();
                            foreach (var seg in segs.EnumerateArray())
                                if (seg.TryGetProperty("utf8", out var u))
                                    sb.Append(u.GetString());

                            var text = sb.ToString();
                            if (string.IsNullOrWhiteSpace(text)) continue;
                            var startMs = evt.TryGetProperty("tStartMs", out var st) && st.TryGetInt64(out var stVal)
                                ? stVal
                                : 0;
                            var durMs = evt.TryGetProperty("dDurationMs", out var dur) &&
                                        dur.TryGetInt64(out var durVal)
                                ? durVal
                                : 0;
                            list.Add(new TranscriptCue(text.Trim(), TimeSpan.FromMilliseconds(startMs),
                                TimeSpan.FromMilliseconds(durMs)));
                        }

                    return list;
                }
            }
            else
            {
                lastException = new HttpRequestException($"Transcript request returned HTTP {(int)res.StatusCode}.");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            lastException = ex;
        }

        // XML fallback.
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, baseUrl);
            req.Headers.UserAgent.ParseAdd(InnerTubeSession.DefaultUserAgent);
            using var res = await session.HttpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
            if (res.IsSuccessStatusCode)
            {
                var xml = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var xdoc = XDocument.Parse(xml);
                foreach (var el in xdoc.Descendants("text"))
                {
                    var text = WebUtility.HtmlDecode(el.Value);
                    var startSec = double.TryParse(el.Attribute("start")?.Value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var s) ? s : 0;
                    var durSec = double.TryParse(el.Attribute("dur")?.Value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var d) ? d : 0;
                    list.Add(new TranscriptCue(text.Trim(), TimeSpan.FromSeconds(startSec),
                        TimeSpan.FromSeconds(durSec)));
                }

                return list;
            }

            lastException = new HttpRequestException($"Transcript request returned HTTP {(int)res.StatusCode}.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            lastException = ex;
        }

        throw new YouTubeRequestException(
            "Failed to retrieve transcript captions.", "transcript.captions", null, lastException);
    }

    private sealed class NextVideoData
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public ChannelId? ChannelId { get; set; }
        public string? ChannelTitle { get; set; }
        public string? ChannelHandle { get; set; }
        public IReadOnlyList<Thumbnail>? ChannelThumbnails { get; set; }
        public bool IsVerified { get; set; }
        public long? SubscriberCount { get; set; }
        public TimeSpan? Duration { get; set; }
        public IReadOnlyList<Thumbnail>? Thumbnails { get; set; }
        public string? PublishedText { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
        public DateOnly? UploadDate { get; set; }
        public long? ViewCount { get; set; }
        public long? LikeCount { get; set; }
        public long? CommentCount { get; set; }
        public IReadOnlyList<string>? Keywords { get; set; }
        public VideoPlaybackProgress? PlaybackProgress { get; set; }
        public LiveBroadcastState LiveState { get; } = LiveBroadcastState.None;
        public bool IsShort { get; set; }
    }

    private static PlayerVideoData ParsePlayerResponse(JsonElement root)
    {
        var data = new PlayerVideoData();

        if (root.TryGetProperty("playabilityStatus", out var playability))
        {
            data.PlayabilityStatus = playability.TryGetProperty("status", out var st) ? st.GetString() : null;
            data.PlayabilityReason = playability.TryGetProperty("reason", out var re)
                ? re.GetString()
                : playability.GetText("reason");
        }

        if (root.TryGetProperty("videoDetails", out var details))
        {
            data.Title = details.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null;
            data.ChannelTitle = details.TryGetProperty("author", out var authorEl) ? authorEl.GetString() : null;

            if (details.TryGetProperty("channelId", out var cidEl) &&
                ChannelId.TryParse(cidEl.GetString(), out var parsedCid))
            {
                data.ChannelId = parsedCid;
            }

            if (details.TryGetProperty("lengthSeconds", out var lsEl) &&
                long.TryParse(lsEl.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var sec) &&
                sec > 0)
            {
                data.Duration = TimeSpan.FromSeconds(sec);
            }

            data.Description = details.TryGetProperty("shortDescription", out var descEl) ? descEl.GetString() : null;

            if (details.TryGetProperty("keywords", out var kwArr) && kwArr.ValueKind == JsonValueKind.Array)
            {
                var kws = new List<string>();
                foreach (var kw in kwArr.EnumerateArray())
                {
                    var s = kw.GetString();
                    if (!string.IsNullOrEmpty(s))
                        kws.Add(s);
                }
                data.Keywords = kws;
            }

            if (details.TryGetProperty("viewCount", out var vcEl) &&
                long.TryParse(vcEl.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var vc))
            {
                data.ViewCount = vc;
            }

            var thumbs = details.GetThumbnails("thumbnail");
            if (thumbs.Count > 0)
            {
                data.Thumbnails = thumbs;
            }

            if (details.TryGetProperty("isLiveContent", out var lcEl) && lcEl.GetBoolean())
            {
                data.LiveState = LiveBroadcastState.Live;
            }
        }

        if (root.TryGetProperty("microformat", out var mf) &&
            mf.TryGetProperty("playerMicroformatRenderer", out var pmr))
        {
            if (string.IsNullOrWhiteSpace(data.Title))
                data.Title = pmr.GetText("title");

            if (string.IsNullOrWhiteSpace(data.Description))
                data.Description = pmr.GetText("description");

            if (data.Thumbnails == null || data.Thumbnails.Count == 0)
            {
                var thumbs = pmr.GetThumbnails("thumbnail");
                if (thumbs.Count > 0)
                    data.Thumbnails = thumbs;
            }

            if (data.Duration == null &&
                pmr.TryGetProperty("lengthSeconds", out var lsEl) &&
                long.TryParse(lsEl.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var sec) &&
                sec > 0)
            {
                data.Duration = TimeSpan.FromSeconds(sec);
            }

            if (pmr.TryGetProperty("uploadDate", out var udEl) &&
                DateTimeOffset.TryParse(udEl.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var udDto))
            {
                data.UploadDate = DateOnly.FromDateTime(udDto.UtcDateTime);
                data.PublishedAt ??= udDto;
            }
            else if (pmr.TryGetProperty("publishDate", out var pdEl) &&
                     DateTimeOffset.TryParse(pdEl.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var pdDto))
            {
                data.UploadDate = DateOnly.FromDateTime(pdDto.UtcDateTime);
                data.PublishedAt ??= pdDto;
            }

            if (pmr.TryGetProperty("ownerProfileUrl", out var opuEl) &&
                Uri.TryCreate(opuEl.GetString(), UriKind.Absolute, out var opuUri))
            {
                data.ChannelUrl = opuUri;
            }

            if (string.IsNullOrWhiteSpace(data.ChannelTitle))
                data.ChannelTitle = pmr.TryGetProperty("ownerChannelName", out var ocnEl) ? ocnEl.GetString() : null;

            if (data.ChannelId == null &&
                pmr.TryGetProperty("externalChannelId", out var ecidEl) &&
                ChannelId.TryParse(ecidEl.GetString(), out var parsedEcid))
            {
                data.ChannelId = parsedEcid;
            }

            if (data.LikeCount == null &&
                pmr.TryGetProperty("likeCount", out var lcEl) &&
                long.TryParse(lcEl.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var lc))
            {
                data.LikeCount = lc;
            }

            if (pmr.TryGetProperty("isLiveBroadcast", out var ilbEl) && ilbEl.GetBoolean())
            {
                data.LiveState = LiveBroadcastState.Live;
            }
            else if (pmr.TryGetProperty("liveBroadcastDetails", out var lbd))
            {
                if (lbd.TryGetProperty("isLiveNow", out var ilnEl) && ilnEl.GetBoolean())
                    data.LiveState = LiveBroadcastState.Live;
                else if (lbd.TryGetProperty("startTimestamp", out var stEl) &&
                         DateTimeOffset.TryParse(stEl.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var stDto) &&
                         stDto > DateTimeOffset.UtcNow)
                    data.LiveState = LiveBroadcastState.Upcoming;
            }
        }

        return data;
    }

    private sealed class PlayerVideoData
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public ChannelId? ChannelId { get; set; }
        public string? ChannelTitle { get; set; }
        public Uri? ChannelUrl { get; set; }
        public TimeSpan? Duration { get; set; }
        public IReadOnlyList<Thumbnail>? Thumbnails { get; set; }
        public IReadOnlyList<string>? Keywords { get; set; }
        public long? ViewCount { get; set; }
        public long? LikeCount { get; set; }
        public DateOnly? UploadDate { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
        public LiveBroadcastState LiveState { get; set; } = LiveBroadcastState.None;
        public string? PlayabilityStatus { get; set; }
        public string? PlayabilityReason { get; set; }
    }
}