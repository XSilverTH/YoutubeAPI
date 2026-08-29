using System.Text.Json;
using YoutubeAPI.Models.Channels;
using YoutubeAPI.Models.Common;
using YoutubeAPI.Models.Continuations;
using YoutubeAPI.Models.Enums;
using YoutubeAPI.Models.Playlists;
using YoutubeAPI.Models.Search;
using YoutubeAPI.Models.ValueTypes;
using YoutubeAPI.Models.Videos;

namespace YoutubeAPI.Infrastructure;

internal sealed class SearchHandler(InnerTubeSession session) : IYouTubeSearchHandler
{
    private static readonly string[] BylinePropertyNames = ["ownerText", "longBylineText", "shortBylineText"];

    public async Task<Page<SearchResult, SearchContinuation>> GetPageAsync(SearchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Query))
            throw new ArgumentException("Search query cannot be empty or whitespace.", nameof(request));

        var searchParams = request.Kind switch
        {
            SearchKind.Video => "EgIQAQ%3D%3D",
            SearchKind.Channel => "EgIQAg%3D%3D",
            SearchKind.Playlist => "EgIQAz%3D%3D",
            _ => null
        };

        using var doc = await session.PostInnerTubeAsync(
            "search",
            writer =>
            {
                writer.WriteString("query", request.Query);
                if (!string.IsNullOrEmpty(searchParams)) writer.WriteString("params", searchParams);
            },
            cancellationToken).ConfigureAwait(false);

        return ParseSearchResponse(doc.RootElement, request.Query, request.Kind);
    }

    public async Task<Page<SearchResult, SearchContinuation>> GetPageAsync(SearchContinuation continuation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        using var doc = await session.PostInnerTubeAsync(
            "search",
            writer => { writer.WriteString("continuation", continuation.Token); },
            cancellationToken).ConfigureAwait(false);

        return ParseSearchResponse(doc.RootElement, continuation.Query, continuation.Kind);
    }

    private static Page<SearchResult, SearchContinuation> ParseSearchResponse(JsonElement root, string? query,
        SearchKind kind)
    {
        var items = new List<SearchResult>();
        string? continuationToken = null;
        string? trackingParams = null;

        // Traverse contents
        if (root.TryGetProperty("contents", out var contents))
            ParseContents(contents, items, ref continuationToken, ref trackingParams);

        if (root.TryGetProperty("onResponseReceivedCommands", out var commands) &&
            commands.ValueKind == JsonValueKind.Array)
            foreach (var cmd in commands.EnumerateArray())
                if (cmd.TryGetProperty("appendContinuationItemsAction", out var action) &&
                    action.TryGetProperty("continuationItems", out var contItems) &&
                    contItems.ValueKind == JsonValueKind.Array)
                    ParseItemList(contItems, items, ref continuationToken, ref trackingParams);

        SearchContinuation? next = null;
        if (!string.IsNullOrEmpty(continuationToken))
            next = new SearchContinuation(continuationToken, query, kind, trackingParams);

        return new Page<SearchResult, SearchContinuation>(items, next);
    }

    private static void ParseContents(JsonElement contents, List<SearchResult> items, ref string? continuationToken,
        ref string? trackingParams)
    {
        if (!contents.TryGetProperty("twoColumnSearchResultsRenderer", out var twoCol)) return;
        if (!twoCol.TryGetProperty("primaryContents", out var primary) ||
            !primary.TryGetProperty("sectionListRenderer", out var sectionList) ||
            !sectionList.TryGetProperty("contents", out var sections) ||
            sections.ValueKind != JsonValueKind.Array) return;
        foreach (var section in sections.EnumerateArray())
            if (section.TryGetProperty("itemSectionRenderer", out var itemSection) &&
                itemSection.TryGetProperty("contents", out var sectionContents) &&
                sectionContents.ValueKind == JsonValueKind.Array)
                ParseItemList(sectionContents, items, ref continuationToken, ref trackingParams);
    }

    private static void ParseItemList(JsonElement itemList, List<SearchResult> items, ref string? continuationToken,
        ref string? trackingParams)
    {
        foreach (var item in itemList.EnumerateArray())
        {
            var (tok, trk) = item.ExtractContinuation();
            if (!string.IsNullOrEmpty(tok))
            {
                continuationToken = tok;
                trackingParams = trk;
                continue;
            }

            if (item.TryGetProperty("videoRenderer", out var vr))
            {
                var summary = ParseVideoSummary(vr);
                if (summary != null) items.Add(new VideoSearchResult(summary));
            }
            else if (item.TryGetProperty("gridVideoRenderer", out var gvr))
            {
                var summary = ParseVideoSummary(gvr);
                if (summary != null) items.Add(new VideoSearchResult(summary));
            }
            else if (item.TryGetProperty("channelRenderer", out var cr))
            {
                var channel = ParseChannelSummary(cr);
                if (channel != null) items.Add(new ChannelSearchResult(channel));
            }
            else if (item.TryGetProperty("playlistRenderer", out var pr))
            {
                var playlist = ParsePlaylistSummary(pr);
                if (playlist != null) items.Add(new PlaylistSearchResult(playlist));
            }
            else if (item.TryGetProperty("lockupViewModel", out var lockup))
            {
                var res = ParseLockupViewModel(lockup);
                if (res != null) items.Add(res);
            }
        }
    }

    public static VideoSummary? ParseVideoSummary(JsonElement vr)
    {
        var videoIdStr = vr.TryGetProperty("videoId", out var vidEl) ? vidEl.GetString() : null;
        if (string.IsNullOrEmpty(videoIdStr) || !VideoId.TryParse(videoIdStr, out var videoId))
            return null;

        var title = vr.GetText("title");
        var thumbnails = vr.GetThumbnails("thumbnail");
        var durationText = vr.GetText("lengthText");
        var duration = InnerTubeElement.ParseDuration(durationText);

        string? channelTitle = null;
        string? channelIdStr = null;
        foreach (var bylineName in BylinePropertyNames)
        {
            if (!vr.TryGetProperty(bylineName, out var byline))
                continue;

            var bylineText = byline.GetText();
            if (string.IsNullOrWhiteSpace(channelTitle) && !string.IsNullOrWhiteSpace(bylineText))
                channelTitle = bylineText;

            var extractedChannelId = ExtractChannelId(byline);
            if (!string.IsNullOrWhiteSpace(extractedChannelId) &&
                (string.IsNullOrWhiteSpace(channelIdStr) || !ChannelId.TryParse(channelIdStr, out _)))
                channelIdStr = extractedChannelId;

            if (!string.IsNullOrWhiteSpace(channelTitle) && ChannelId.TryParse(channelIdStr, out _))
                break;
        }

        channelTitle ??= "Unknown";
        channelIdStr ??= "UC_UNKNOWN";
        var channelId = ChannelId.TryParse(channelIdStr, out var parsedChId)
            ? parsedChId
            : new ChannelId("UC0000000000000000000000");
        var channelThumbs = vr.GetThumbnails("channelThumbnailSupportedRenderers");
        var isVerified = vr.IsVerified();
        var channelSummary = new ChannelSummary(
            channelId,
            channelTitle,
            null,
            new Uri($"https://www.youtube.com/channel/{channelId}"),
            channelThumbs,
            isVerified,
            null);

        var publishedText = vr.GetText("publishedTimeText");
        var publishedAt = InnerTubeElement.ParseRelativeDate(publishedText);

        var viewCountText = vr.GetText("viewCountText");
        var viewCount = InnerTubeElement.ParseCount(viewCountText);

        var stats = new VideoStatistics(viewCount, null, null);
        return new VideoSummary(
            videoId,
            title,
            channelSummary,
            duration,
            new Uri($"https://www.youtube.com/watch?v={videoId}"),
            thumbnails,
            string.IsNullOrEmpty(publishedText) ? null : publishedText,
            publishedAt,
            false,
            stats);
    }

    private static string? ExtractChannelId(JsonElement byline)
    {
        if (byline.ValueKind != JsonValueKind.Object)
            return null;

        if (byline.TryGetProperty("channelId", out var channelId) &&
            channelId.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(channelId.GetString()))
            return channelId.GetString();

        if (byline.TryGetProperty("browseEndpoint", out var directBrowseEndpoint) &&
            directBrowseEndpoint.TryGetProperty("browseId", out var directBrowseId) &&
            directBrowseId.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(directBrowseId.GetString()))
            return directBrowseId.GetString();

        if (byline.TryGetProperty("navigationEndpoint", out var endpoint) &&
            endpoint.TryGetProperty("browseEndpoint", out var browseEndpoint) &&
            browseEndpoint.TryGetProperty("browseId", out var browseId) &&
            browseId.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(browseId.GetString()))
            return browseId.GetString();

        if (!byline.TryGetProperty("runs", out var runs) || runs.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var run in runs.EnumerateArray())
        {
            var id = ExtractChannelId(run);
            if (!string.IsNullOrWhiteSpace(id))
                return id;
        }

        return null;
    }

    public static ChannelSummary? ParseChannelSummary(JsonElement cr)
    {
        var channelIdStr = cr.TryGetProperty("channelId", out var cidEl) ? cidEl.GetString() : null;
        if (string.IsNullOrEmpty(channelIdStr) || !ChannelId.TryParse(channelIdStr, out var channelId))
            return null;

        var title = cr.GetText("title");
        string? handle = null;
        var subCountText = cr.GetText("subscriberCountText");
        if (subCountText.StartsWith('@')) handle = subCountText;

        var subCount = InnerTubeElement.ParseCount(subCountText);
        var thumbnails = cr.GetThumbnails("thumbnail");
        var isVerified = cr.IsVerified();

        return new ChannelSummary(
            channelId,
            title,
            handle,
            new Uri($"https://www.youtube.com/channel/{channelId}"),
            thumbnails,
            isVerified,
            subCount);
    }

    public static PlaylistSummary? ParsePlaylistSummary(JsonElement pr)
    {
        var playlistIdStr = pr.TryGetProperty("playlistId", out var pidEl) ? pidEl.GetString() : null;
        if (string.IsNullOrEmpty(playlistIdStr) || !PlaylistId.TryParse(playlistIdStr, out var playlistId))
            return null;

        var title = pr.GetText("title");
        var thumbnails = pr.GetThumbnails();
        var countText = pr.GetText("itemCount");
        if (string.IsNullOrEmpty(countText)) countText = pr.GetText("videoCount");
        var itemCount = (int?)InnerTubeElement.ParseCount(countText);

        ChannelSummary? author = null;
        if (!pr.TryGetProperty("longBylineText", out var lbt))
            return new PlaylistSummary(
                playlistId,
                title,
                new Uri($"https://www.youtube.com/playlist?list={playlistId}"),
                author,
                itemCount,
                thumbnails);
        var authorName = lbt.GetText();
        if (!string.IsNullOrEmpty(authorName))
            author = new ChannelSummary(
                new ChannelId("UC0000000000000000000000"),
                authorName,
                null,
                new Uri("https://www.youtube.com"),
                [],
                false,
                null);

        return new PlaylistSummary(
            playlistId,
            title,
            new Uri($"https://www.youtube.com/playlist?list={playlistId}"),
            author,
            itemCount,
            thumbnails);
    }

    public static SearchResult? ParseLockupViewModel(JsonElement lockup)
    {
        var contentType = lockup.TryGetProperty("contentType", out var ct) ? ct.GetString() ?? "" : "";
        var contentId = lockup.TryGetProperty("contentId", out var cid) ? cid.GetString() : null;

        var metadata = lockup.GetPropertyOrDefault("metadata").GetPropertyOrDefault("lockupMetadataViewModel");
        var title = metadata.GetPropertyOrDefault("title").GetText();
        var publication = ParseLockupPublication(metadata);

        var thumbnails = lockup.GetPropertyOrDefault("contentImage").GetThumbnails();

        if (contentType.Contains("VIDEO", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(contentId) &&
            VideoId.TryParse(contentId, out var videoId))
        {
            var channel = ParseLockupChannel(metadata);
            var summary = new VideoSummary(
                videoId,
                title,
                channel,
                null,
                new Uri($"https://www.youtube.com/watch?v={videoId}"),
                thumbnails,
                publication.PublishedText,
                publication.PublishedAt,
                false,
                new VideoStatistics(null, null, null));
            return new VideoSearchResult(summary);
        }

        if (!contentType.Contains("PLAYLIST", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(contentId) ||
            !PlaylistId.TryParse(contentId, out var playlistId)) return null;
        var playlist = new PlaylistSummary(
            playlistId,
            title,
            new Uri($"https://www.youtube.com/playlist?list={playlistId}"),
            null,
            null,
            thumbnails);
        return new PlaylistSearchResult(playlist);
    }

    private static ChannelSummary ParseLockupChannel(JsonElement metadata)
    {
        var channelTitle = string.Empty;
        string? channelIdText = null;
        var contentMetadata = default(JsonElement);
        if (metadata.TryGetProperty("metadata", out var metadataNode) &&
            metadataNode.TryGetProperty("contentMetadataViewModel", out var contentMetadataNode))
            contentMetadata = contentMetadataNode;
        var rows = contentMetadata.ValueKind == JsonValueKind.Object &&
                   contentMetadata.TryGetProperty("metadataRows", out var nestedRows)
            ? nestedRows
            : metadata.TryGetProperty("metadataRows", out var directRows)
                ? directRows
                : default;
        if (rows.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in rows.EnumerateArray())
            {
                if (!row.TryGetProperty("metadataParts", out var parts) || parts.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var part in parts.EnumerateArray())
                {
                    if (string.IsNullOrWhiteSpace(channelTitle) &&
                        part.TryGetProperty("text", out var text))
                        channelTitle = text.GetText();
                    channelIdText ??= FindBrowseId(part);
                }

                if (!string.IsNullOrWhiteSpace(channelTitle) && !string.IsNullOrWhiteSpace(channelIdText))
                    break;
            }
        }

        channelIdText ??= FindBrowseId(metadata);

        var channelId = ChannelId.TryParse(channelIdText, out var parsed)
            ? parsed
            : new ChannelId("UC0000000000000000000000");
        return new ChannelSummary(
            channelId,
            string.IsNullOrWhiteSpace(channelTitle) ? "Unknown" : channelTitle,
            null,
            new Uri($"https://www.youtube.com/channel/{channelId}"),
            [],
            false,
            null);
    }

    private static (string? PublishedText, DateTimeOffset? PublishedAt) ParseLockupPublication(
        JsonElement metadata)
    {
        if (!metadata.TryGetProperty("metadata", out var metadataNode) ||
            !metadataNode.TryGetProperty("contentMetadataViewModel", out var contentMetadata) ||
            !contentMetadata.TryGetProperty("metadataRows", out var rows) ||
            rows.ValueKind != JsonValueKind.Array)
            return (null, null);

        foreach (var row in rows.EnumerateArray())
        {
            if (!row.TryGetProperty("metadataParts", out var parts) || parts.ValueKind != JsonValueKind.Array)
                continue;
            foreach (var part in parts.EnumerateArray())
            {
                if (!part.TryGetProperty("text", out var text))
                    continue;
                var publishedText = text.GetText();
                var publishedAt = InnerTubeElement.ParseRelativeDate(publishedText);
                if (publishedAt is not null)
                    return (publishedText, publishedAt);
            }
        }

        return (null, null);
    }

    private static string? FindBrowseId(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;
        if (element.TryGetProperty("browseEndpoint", out var browseEndpoint) &&
            browseEndpoint.TryGetProperty("browseId", out var browseId) &&
            browseId.ValueKind == JsonValueKind.String)
            return browseId.GetString();
        foreach (var property in element.EnumerateObject())
        {
            var result = FindBrowseId(property.Value);
            if (!string.IsNullOrWhiteSpace(result))
                return result;
        }

        return null;
    }
}