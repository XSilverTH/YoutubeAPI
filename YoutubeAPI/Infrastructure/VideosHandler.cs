using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using YoutubeAPI.Exceptions;
using YoutubeAPI.Models.Channels;
using YoutubeAPI.Models.Common;
using YoutubeAPI.Models.Enums;
using YoutubeAPI.Models.ValueTypes;
using YoutubeAPI.Models.Videos;
using YoutubeExplode;
using YoutubeExplode.Exceptions;

namespace YoutubeAPI.Infrastructure;

internal sealed class VideosHandler(InnerTubeSession session) : IYouTubeVideosHandler
{
    public async Task<Video> GetAsync(VideoId videoId, CancellationToken cancellationToken)
    {
        try
        {
            var nextTask = FetchNextAsync(videoId, cancellationToken);
            var explodeTask = FetchExplodeVideoAsync(videoId, cancellationToken);

            await Task.WhenAll(nextTask, explodeTask).ConfigureAwait(false);

            var nextData = await nextTask.ConfigureAwait(false);
            var explodeVideo = await explodeTask.ConfigureAwait(false);
            var playbackProgress = nextData?.PlaybackProgress;

            if (explodeVideo != null)
            {
                var channelId = ChannelId.TryParse(explodeVideo.Author.ChannelId, out var parsedChId)
                    ? parsedChId
                    : nextData?.ChannelId ?? new ChannelId("UC0000000000000000000000");

                var channelSummary = new ChannelSummary(
                    channelId,
                    explodeVideo.Author.ChannelTitle,
                    nextData?.ChannelHandle,
                    new Uri(explodeVideo.Author.ChannelUrl),
                    nextData?.ChannelThumbnails ?? [],
                    nextData?.IsVerified ?? false,
                    nextData?.SubscriberCount);

                var thumbs = explodeVideo.Thumbnails
                    .Select(t => new Thumbnail(new Uri(t.Url), t.Resolution.Width, t.Resolution.Height)).ToList();
                if (thumbs.Count == 0 && nextData?.Thumbnails != null) thumbs.AddRange(nextData.Thumbnails);

                var stats = new VideoStatistics(
                    explodeVideo.Engagement.ViewCount,
                    explodeVideo.Engagement.LikeCount,
                    nextData?.CommentCount);

                DateOnly? uploadDate = null;
                try
                {
                    uploadDate = DateOnly.FromDateTime(explodeVideo.UploadDate.UtcDateTime);
                }
                catch
                {
                    // Ignore date conversion failure
                }

                var summary = new VideoSummary(
                    videoId,
                    explodeVideo.Title,
                    channelSummary,
                    explodeVideo.Duration,
                    new Uri(explodeVideo.Url),
                    thumbs,
                    nextData?.PublishedText,
                    explodeVideo.UploadDate,
                    false,
                    stats);

                return new Video(
                    summary,
                    explodeVideo.Description,
                    [.. explodeVideo.Keywords],
                    uploadDate,
                    nextData?.LiveState ?? LiveBroadcastState.None)
                {
                    PlaybackProgress = playbackProgress
                };
            }
            if (nextData != null)
            {
                var channelId = nextData.ChannelId ?? new ChannelId("UC0000000000000000000000");
                var channelSummary = new ChannelSummary(
                    channelId,
                    nextData.ChannelTitle ?? "Unknown",
                    nextData.ChannelHandle,
                    new Uri($"https://www.youtube.com/channel/{channelId}"),
                    nextData.ChannelThumbnails ?? [],
                    nextData.IsVerified,
                    nextData.SubscriberCount);

                var stats = new VideoStatistics(nextData.ViewCount, nextData.LikeCount, nextData.CommentCount);

                var summary = new VideoSummary(
                    videoId,
                    nextData.Title ?? "Unknown",
                    channelSummary,
                    nextData.Duration,
                    new Uri($"https://www.youtube.com/watch?v={videoId}"),
                    nextData.Thumbnails ?? [],
                    nextData.PublishedText,
                    nextData.PublishedAt,
                    false,
                    stats);
                return new Video(
                    summary,
                    nextData.Description ?? string.Empty,
                    nextData.Keywords ?? [],
                    nextData.UploadDate,
                    nextData.LiveState)
                {
                    PlaybackProgress = playbackProgress
                };
            }
            throw new ResourceNotFoundException($"Video '{videoId}' was not found or is unavailable.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (YouTubeException)
        {
            throw;
        }
        catch (VideoUnavailableException ex)
        {
            throw new ResourceUnavailableException(
                $"Video '{videoId}' is unavailable: {InnerTubeSession.Sanitize(ex.Message)}", ex);
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
            // 1. Try direct /player endpoint first
            var playerDoc = await FetchPlayerDocAsync(videoId, cancellationToken).ConfigureAwait(false);
            if (playerDoc != null)
                using (playerDoc)
                {
                    var tracks = ParseCaptionTracks(playerDoc.RootElement);
                    if (tracks.Count > 0)
                        return tracks;
                }

            try
            {
                var manifest = await CreateExplodeClient().Videos.ClosedCaptions
                    .GetManifestAsync(videoId.Value, cancellationToken).ConfigureAwait(false);

                return
                [
                    .. from trackInfo in manifest.Tracks
                    let trackId = TranscriptTrackId.Parse(trackInfo.Language.Code)
                    select new TranscriptTrack(trackId, trackInfo.Language.Code, trackInfo.Language.Name,
                        trackInfo.IsAutoGenerated)
                ];
            }
            catch (VideoUnavailableException)
            {
                return [];
            }
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
            // 1. Try direct /player endpoint first
            var playerDoc = await FetchPlayerDocAsync(videoId, cancellationToken).ConfigureAwait(false);
            if (playerDoc != null)
                using (playerDoc)
                {
                    var directTrack = FindCaptionTrackElement(playerDoc.RootElement, trackId.Value);
                    if (directTrack.HasValue)
                    {
                        var baseUrl = directTrack.Value.TryGetProperty("baseUrl", out var bu) ? bu.GetString() : null;
                        if (!string.IsNullOrEmpty(baseUrl))
                        {
                            var cues = await FetchCuesFromBaseUrlAsync(baseUrl, cancellationToken)
                                .ConfigureAwait(false);
                            var langCode = directTrack.Value.TryGetProperty("languageCode", out var lc)
                                ? lc.GetString() ?? ""
                                : "";
                            var displayName = directTrack.Value.GetText("name");
                            var kind = directTrack.Value.TryGetProperty("kind", out var k) ? k.GetString() ?? "" : "";
                            var isAuto = kind.Equals("asr", StringComparison.OrdinalIgnoreCase);

                            var track = new TranscriptTrack(trackId, langCode, displayName, isAuto);
                            return new Transcript(videoId, track, cues);
                        }
                    }
                }

            // 2. Fallback to YoutubeExplode
            var explodeClient = CreateExplodeClient();
            var manifest = await explodeClient.Videos.ClosedCaptions.GetManifestAsync(videoId.Value, cancellationToken)
                .ConfigureAwait(false);
            var trackInfo = manifest.Tracks.FirstOrDefault(t =>
                                t.Language.Code.Equals(trackId.Value, StringComparison.OrdinalIgnoreCase) ||
                                t.Url.Contains(trackId.Value, StringComparison.OrdinalIgnoreCase))
                            ?? manifest.Tracks.FirstOrDefault(t =>
                                t.Language.Code.StartsWith(trackId.Value, StringComparison.OrdinalIgnoreCase));

            if (trackInfo == null)
                throw new ResourceNotFoundException(
                    $"Transcript track '{trackId}' was not found for video '{videoId}'.");

            var ccTrack = await explodeClient.Videos.ClosedCaptions.GetAsync(trackInfo, cancellationToken)
                .ConfigureAwait(false);
            var cueList = ccTrack.Captions.Select(c => new TranscriptCue(c.Text, c.Offset, c.Duration)).ToList();

            var transcriptTrack = new TranscriptTrack(
                trackId,
                trackInfo.Language.Code,
                trackInfo.Language.Name,
                trackInfo.IsAutoGenerated);

            return new Transcript(videoId, transcriptTrack, cueList);
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

    private YoutubeClient CreateExplodeClient()
    {
        return new YoutubeClient(session.HttpClient);
    }

    private async Task<YoutubeExplode.Videos.Video?> FetchExplodeVideoAsync(VideoId videoId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await CreateExplodeClient().Videos.GetAsync(videoId.Value, cancellationToken).ConfigureAwait(false);
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

    private async Task<NextVideoData?> FetchNextAsync(VideoId videoId, CancellationToken cancellationToken)
    {
        try
        {
            using var doc = await session.PostInnerTubeAsync(
                "next",
                writer => { writer.WriteString("videoId", videoId.Value); },
                cancellationToken).ConfigureAwait(false);

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
        var data = new NextVideoData
        {
            PlaybackProgress = MergePlaybackProgress(progressFromOverlays, progressFromEndpoint)
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
                    var subText = vor.GetText("subscriberCountText");
                    data.SubscriberCount = InnerTubeElement.ParseCount(subText);
                    data.ChannelThumbnails = vor.GetThumbnails("thumbnail");
                    data.IsVerified = vor.IsVerified();

                    if (vor.TryGetProperty("navigationEndpoint", out var nav) &&
                        nav.TryGetProperty("browseEndpoint", out var be) &&
                        be.TryGetProperty("browseId", out var bid) &&
                        ChannelId.TryParse(bid.GetString(), out var cid))
                        data.ChannelId = cid;
                }

                var desc = secondary.GetText("description");
                if (string.IsNullOrEmpty(desc)) desc = secondary.GetText("attributedDescription");
                data.Description = desc;
            }

        return data;
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
                cancellationToken).ConfigureAwait(false);
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

    private static List<TranscriptTrack> ParseCaptionTracks(JsonElement root)
    {
        var list = new List<TranscriptTrack>();
        if (!root.TryGetProperty("captions", out var captions) ||
            !captions.TryGetProperty("playerCaptionsTracklistRenderer", out var pctr) ||
            !pctr.TryGetProperty("captionTracks", out var tracks) ||
            tracks.ValueKind != JsonValueKind.Array) return list;
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
        if (!root.TryGetProperty("captions", out var captions) ||
            !captions.TryGetProperty("playerCaptionsTracklistRenderer", out var pctr) ||
            !pctr.TryGetProperty("captionTracks", out var tracks) ||
            tracks.ValueKind != JsonValueKind.Array) return null;
        foreach (var track in tracks.EnumerateArray())
        {
            var vssId = track.TryGetProperty("vssId", out var vi) ? vi.GetString() : null;
            var langCode = track.TryGetProperty("languageCode", out var lc) ? lc.GetString() : null;

            if (string.Equals(vssId, trackIdValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(langCode, trackIdValue, StringComparison.OrdinalIgnoreCase))
                return track;
        }

        return null;
    }

    private async Task<IReadOnlyList<TranscriptCue>> FetchCuesFromBaseUrlAsync(string baseUrl,
        CancellationToken cancellationToken)
    {
        var list = new List<TranscriptCue>();

        // Try JSON3 format first
        var jsonUrl = baseUrl.Contains('?') ? $"{baseUrl}&fmt=json3" : $"{baseUrl}?fmt=json3";
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, jsonUrl);
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
                            var startMs =
                                evt.TryGetProperty("tStartMs", out var st) && st.TryGetInt64(out var stVal)
                                    ? stVal
                                    : 0;
                            var durMs = evt.TryGetProperty("dDurationMs", out var dur) &&
                                        dur.TryGetInt64(out var durVal)
                                ? durVal
                                : 0;
                            list.Add(new TranscriptCue(text.Trim(), TimeSpan.FromMilliseconds(startMs),
                                TimeSpan.FromMilliseconds(durMs)));
                        }

                    if (list.Count > 0)
                        return list;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Fall back to XML
        }

        // XML fallback
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, baseUrl);
            using var res = await session.HttpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
            if (res.IsSuccessStatusCode)
            {
                var xml = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var xdoc = XDocument.Parse(xml);
                foreach (var el in xdoc.Descendants("text"))
                {
                    var text = WebUtility.HtmlDecode(el.Value);
                    var startSec = double.TryParse(el.Attribute("start")?.Value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var s)
                        ? s
                        : 0;
                    var durSec = double.TryParse(el.Attribute("dur")?.Value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var d)
                        ? d
                        : 0;
                    list.Add(new TranscriptCue(text.Trim(), TimeSpan.FromSeconds(startSec),
                        TimeSpan.FromSeconds(durSec)));
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Ignore XML parse errors
        }

        return list;
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
    }
}