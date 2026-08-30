using System.Text.Json;
using YoutubeAPI.Exceptions;
using YoutubeAPI.Models.Channels;
using YoutubeAPI.Models.Common;
using YoutubeAPI.Models.Continuations;
using YoutubeAPI.Models.Enums;
using YoutubeAPI.Models.Playlists;
using YoutubeAPI.Models.Search;
using YoutubeAPI.Models.ValueTypes;
using YoutubeAPI.Models.Videos;

namespace YoutubeAPI.Infrastructure;

internal sealed class ChannelsHandler(InnerTubeSession session) : IYouTubeChannelsHandler
{
    public async Task<Channel> GetAsync(ChannelReference channel, CancellationToken cancellationToken)
    {
        var resolvedChannelId = await session.ResolveChannelIdAsync(channel, cancellationToken).ConfigureAwait(false);

        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer => writer.WriteString("browseId", resolvedChannelId.Value),
            cancellationToken).ConfigureAwait(false);

        var root = doc.RootElement;
        var channelSummary = ParseChannelHeader(root, channel);
        if (channelSummary == null) throw new ResourceNotFoundException($"Channel '{channel.Value}' was not found.");

        var description = ParseChannelDescription(root);
        var banners = ParseChannelBanners(root);

        return new Channel(channelSummary, description, banners);
    }

    public async Task<Page<VideoSummary, ChannelVideosContinuation>> GetVideosPageAsync(
        ChannelReference channel,
        ChannelVideoSort sort,
        CancellationToken cancellationToken)
    {
        var resolvedChannelId = await session.ResolveChannelIdAsync(channel, cancellationToken).ConfigureAwait(false);
        var browseId = resolvedChannelId.Value;
        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer =>
            {
                writer.WriteString("browseId", browseId);
                writer.WriteString("params", "EgZ2aWRlb3PyBgQKAjoA");
            },
            cancellationToken).ConfigureAwait(false);

        var root = doc.RootElement;
        if (sort == ChannelVideoSort.Newest) return ParseChannelVideosResponse(root, browseId, sort);
        {
            var sortTarget = sort == ChannelVideoSort.Popular ? "Popular" : "Oldest";
            var sortContinuationToken = FindSortContinuationToken(root, sortTarget);
            if (string.IsNullOrEmpty(sortContinuationToken)) return ParseChannelVideosResponse(root, browseId, sort);
            using var sortedDoc = await session.PostInnerTubeAsync(
                "browse",
                writer => writer.WriteString("continuation", sortContinuationToken),
                cancellationToken).ConfigureAwait(false);
            return ParseChannelVideosResponse(sortedDoc.RootElement, browseId, sort);
        }
    }

    public async Task<Page<VideoSummary, ChannelVideosContinuation>> GetVideosPageAsync(
        ChannelVideosContinuation continuation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer => { writer.WriteString("continuation", continuation.Token); },
            cancellationToken).ConfigureAwait(false);

        return ParseChannelVideosResponse(doc.RootElement, continuation.Channel, continuation.Sort);
    }

    public async Task<Page<PlaylistSummary, ChannelPlaylistsContinuation>> GetPlaylistsPageAsync(
        ChannelReference channel,
        CancellationToken cancellationToken)
    {
        var resolvedChannelId = await session.ResolveChannelIdAsync(channel, cancellationToken).ConfigureAwait(false);
        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer =>
            {
                writer.WriteString("browseId", resolvedChannelId.Value);
                writer.WriteString("params", "EglwbGF5bGlzdHPyBgQKAkIA");
            },
            cancellationToken).ConfigureAwait(false);

        return ParseChannelPlaylistsResponse(doc.RootElement, resolvedChannelId.Value);
    }

    public async Task<Page<PlaylistSummary, ChannelPlaylistsContinuation>> GetPlaylistsPageAsync(
        ChannelPlaylistsContinuation continuation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer => { writer.WriteString("continuation", continuation.Token); },
            cancellationToken).ConfigureAwait(false);

        return ParseChannelPlaylistsResponse(doc.RootElement, continuation.Channel);
    }

    internal static ChannelSummary ParseChannelHeader(JsonElement root, ChannelReference channelRef)
    {
        JsonElement header = default;
        if (root.TryGetProperty("header", out var h))
        {
            if (h.TryGetProperty("pageHeaderRenderer", out var phr))
                header = phr;
            else if (h.TryGetProperty("c4TabbedHeaderRenderer", out var c4))
                header = c4;
            else if (h.TryGetProperty("carouselHeaderRenderer", out var chr))
                header = chr;
            else
                header = h;
        }
        if (header.ValueKind == JsonValueKind.Object)
        {
            if (header.TryGetProperty("pageHeaderViewModel", out var pageHeaderViewModel))
                header = pageHeaderViewModel;
            else if (header.TryGetProperty("content", out var content) &&
                     content.TryGetProperty("pageHeaderViewModel", out pageHeaderViewModel))
                header = pageHeaderViewModel;
        }


        var title = header.GetText("pageTitle");
        if (string.IsNullOrEmpty(title))
            title = header.GetText("title");

        if (string.IsNullOrEmpty(title))
            title = channelRef.Value;

        var channelIdStr = root.TryGetProperty("metadata", out var md) &&
                           md.TryGetProperty("channelMetadataRenderer", out var cmr) &&
                           cmr.TryGetProperty("externalId", out var extId)
            ? extId.GetString()
            : channelRef.Value.StartsWith("UC", StringComparison.Ordinal)
                ? channelRef.Value
                : null;

        var channelId = ChannelId.TryParse(channelIdStr, out var parsedChId)
            ? parsedChId
            : new ChannelId("UC0000000000000000000000");

        var thumbnails = header.GetThumbnails("contentImage");
        if (thumbnails.Count == 0) thumbnails = header.GetThumbnails("avatar");
        if (thumbnails.Count == 0) thumbnails = header.GetThumbnails("thumbnail");

        string? handle = null;
        var subCountText = string.Empty;

        if (header.TryGetProperty("metadata", out var headerMd) &&
            headerMd.TryGetProperty("contentMetadataViewModel", out var cmvm) &&
            cmvm.TryGetProperty("metadataRows", out var rows) &&
            rows.ValueKind == JsonValueKind.Array)
            foreach (var row in rows.EnumerateArray())
                if (row.TryGetProperty("metadataParts", out var parts) && parts.ValueKind == JsonValueKind.Array)
                    foreach (var partText in parts.EnumerateArray().Select(part => part.GetText()))
                        if (partText.StartsWith('@'))
                            handle = partText;
                        else if (partText.Contains("subscribers", StringComparison.OrdinalIgnoreCase))
                            subCountText = partText;

        if (string.IsNullOrEmpty(subCountText)) subCountText = header.GetText("subscriberCountText");

        var subCount = InnerTubeElement.ParseCount(subCountText);
        var isVerified = header.IsVerified();

        return new ChannelSummary(
            channelId,
            title,
            handle,
            new Uri($"https://www.youtube.com/channel/{channelId}"),
            thumbnails,
            isVerified,
            subCount);
    }

    private static string ParseChannelDescription(JsonElement root)
    {
        if (root.TryGetProperty("metadata", out var md) &&
            md.TryGetProperty("channelMetadataRenderer", out var cmr))
            return cmr.GetText("description");

        if (root.TryGetProperty("header", out var h)) return h.GetText("description");

        return string.Empty;
    }

    private static IReadOnlyList<Thumbnail> ParseChannelBanners(JsonElement root)
    {
        if (!root.TryGetProperty("header", out var h)) return [];
        var banners = h.GetThumbnails("banner");
        if (banners.Count > 0)
            return banners;

        banners = h.GetThumbnails("image");
        if (banners.Count > 0)
            return banners;

        return [];
    }

    private static string? FindSortContinuationToken(JsonElement root, string targetSortTitle)
    {
        if (!root.TryGetProperty("contents", out var contents) ||
            !contents.TryGetProperty("twoColumnBrowseResultsRenderer", out var twoCol) ||
            !twoCol.TryGetProperty("tabs", out var tabs) ||
            tabs.ValueKind != JsonValueKind.Array) return null;
        foreach (var tab in tabs.EnumerateArray())
            if (tab.TryGetProperty("tabRenderer", out var tabRenderer) &&
                tabRenderer.TryGetProperty("content", out var tabContent) &&
                tabContent.TryGetProperty("richGridRenderer", out var richGrid) &&
                richGrid.TryGetProperty("header", out var gridHeader) &&
                gridHeader.TryGetProperty("chipBarViewModel", out var cbvm) &&
                cbvm.TryGetProperty("chips", out var chips) &&
                chips.ValueKind == JsonValueKind.Array)
                foreach (var chip in chips.EnumerateArray())
                    if (chip.TryGetProperty("chipViewModel", out var chipVm) &&
                        chipVm.TryGetProperty("tapCommand", out var tc) &&
                        tc.TryGetProperty("innertubeCommand", out var itc) &&
                        itc.TryGetProperty("showSheetCommand", out var ssc) &&
                        ssc.TryGetProperty("panelLoadingStrategy", out var pls) &&
                        pls.TryGetProperty("inlineContent", out var inc) &&
                        inc.TryGetProperty("sheetViewModel", out var svm) &&
                        svm.TryGetProperty("content", out var sheetContent) &&
                        sheetContent.TryGetProperty("listViewModel", out var lvm) &&
                        lvm.TryGetProperty("listItems", out var listItems) &&
                        listItems.ValueKind == JsonValueKind.Array)
                        foreach (var item in listItems.EnumerateArray())
                            if (item.TryGetProperty("listItemViewModel", out var livm))
                            {
                                var title = livm.GetPropertyOrDefault("title").GetText();
                                if (!title.Equals(targetSortTitle, StringComparison.OrdinalIgnoreCase)) continue;
                                var onTap = livm.GetPropertyOrDefault("rendererContext")
                                    .GetPropertyOrDefault("commandContext")
                                    .GetPropertyOrDefault("onTap")
                                    .GetPropertyOrDefault("innertubeCommand");

                                if (!onTap.TryGetProperty("commandExecutorCommand", out var cec) ||
                                    !cec.TryGetProperty("commands", out var commands) ||
                                    commands.ValueKind != JsonValueKind.Array) continue;
                                foreach (var cmd in commands.EnumerateArray())
                                    if (cmd.TryGetProperty("continuationCommand", out var contCmd) &&
                                        contCmd.TryGetProperty("token", out var tok))
                                        return tok.GetString();
                            }

        return null;
    }

    private static Page<VideoSummary, ChannelVideosContinuation> ParseChannelVideosResponse(
        JsonElement root,
        string? channelRef,
        ChannelVideoSort sort)
    {
        var items = new List<VideoSummary>();
        var playbackProgress = new Dictionary<VideoId, VideoPlaybackProgress>();
        string? continuationToken = null;
        string? trackingParams = null;

        CollectVideos(root, items, playbackProgress, ref continuationToken, ref trackingParams);

        if (ChannelId.TryParse(channelRef, out var fallbackId))
        {
            var fallbackChannel = new ChannelSummary(
                fallbackId,
                fallbackId.Value,
                null,
                new Uri($"https://www.youtube.com/channel/{fallbackId}"),
                [],
                false,
                null);

            for (var i = 0; i < items.Count; i++)
                if (items[i].Channel.Id.Value == "UC0000000000000000000000")
                    items[i] = items[i] with { Channel = fallbackChannel };
        }

        var next = !string.IsNullOrEmpty(continuationToken)
            ? new ChannelVideosContinuation(continuationToken, channelRef, sort, trackingParams)
            : null;

        return new Page<VideoSummary, ChannelVideosContinuation>(items, next)
        {
            PlaybackProgress = playbackProgress.Count == 0 ? null : playbackProgress
        };
    }

    private static Page<PlaylistSummary, ChannelPlaylistsContinuation> ParseChannelPlaylistsResponse(
        JsonElement root,
        string? channelRef)
    {
        var items = new List<PlaylistSummary>();
        string? continuationToken = null;
        string? trackingParams = null;

        CollectPlaylists(root, items, ref continuationToken, ref trackingParams);

        ChannelPlaylistsContinuation? next = null;
        if (!string.IsNullOrEmpty(continuationToken))
            next = new ChannelPlaylistsContinuation(continuationToken, channelRef, trackingParams);

        return new Page<PlaylistSummary, ChannelPlaylistsContinuation>(items, next);
    }

    private static void CollectVideos(
        JsonElement element,
        List<VideoSummary> items,
        Dictionary<VideoId, VideoPlaybackProgress> playbackProgress,
        ref string? continuationToken,
        ref string? trackingParams)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                CollectVideos(item, items, playbackProgress, ref continuationToken, ref trackingParams);
            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
            return;

        var (tok, trk) = element.ExtractContinuation();
        if (!string.IsNullOrEmpty(tok))
        {
            continuationToken = tok;
            trackingParams = trk;
            return;
        }

        if (element.TryGetProperty("videoRenderer", out var vr))
        {
            var summary = SearchHandler.ParseVideoSummary(vr);
            if (summary != null)
                AddVideo(summary, InnerTubeElement.ParsePlaybackProgress(vr), items, playbackProgress);
            return;
        }

        if (element.TryGetProperty("gridVideoRenderer", out var gvr))
        {
            var summary = SearchHandler.ParseVideoSummary(gvr);
            if (summary != null)
                AddVideo(summary, InnerTubeElement.ParsePlaybackProgress(gvr), items, playbackProgress);
            return;
        }

        if (element.TryGetProperty("compactVideoRenderer", out var cvr))
        {
            var summary = SearchHandler.ParseVideoSummary(cvr);
            if (summary != null)
                AddVideo(summary, InnerTubeElement.ParsePlaybackProgress(cvr), items, playbackProgress);
            return;
        }

        if (element.TryGetProperty("playlistPanelVideoRenderer", out var ppvr))
        {
            var summary = SearchHandler.ParseVideoSummary(ppvr);
            if (summary != null)
                AddVideo(summary, InnerTubeElement.ParsePlaybackProgress(ppvr), items, playbackProgress);
            return;
        }

        if (element.TryGetProperty("videoWithContextRenderer", out var vwcr))
        {
            var summary = SearchHandler.ParseVideoSummary(vwcr);
            if (summary != null)
                AddVideo(summary, InnerTubeElement.ParsePlaybackProgress(vwcr), items, playbackProgress);
            return;
        }

        if (element.TryGetProperty("lockupViewModel", out var lockup))
        {
            var res = SearchHandler.ParseLockupViewModel(lockup);
            if (res is VideoSearchResult vsr)
                AddVideo(vsr.Video, vsr.PlaybackProgress, items, playbackProgress);
            return;
        }

        if (element.TryGetProperty("contents", out var contents))
            CollectVideos(contents, items, playbackProgress, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("items", out var listItems))
            CollectVideos(listItems, items, playbackProgress, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("richItemRenderer", out var rir) && rir.TryGetProperty("content", out var rc))
            CollectVideos(rc, items, playbackProgress, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("tabRenderer", out var tr) && tr.TryGetProperty("content", out var tc))
            CollectVideos(tc, items, playbackProgress, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("twoColumnBrowseResultsRenderer", out var tcbrr))
            CollectVideos(tcbrr, items, playbackProgress, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("tabs", out var tabs))
            CollectVideos(tabs, items, playbackProgress, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("richGridRenderer", out var rgr))
            CollectVideos(rgr, items, playbackProgress, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("sectionListRenderer", out var slr))
            CollectVideos(slr, items, playbackProgress, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("itemSectionRenderer", out var isr))
            CollectVideos(isr, items, playbackProgress, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("onResponseReceivedActions", out var actions))
            CollectVideos(actions, items, playbackProgress, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("appendContinuationItemsAction", out var acia))
            CollectVideos(acia, items, playbackProgress, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("reloadContinuationItemsCommand", out var rcic))
            CollectVideos(rcic, items, playbackProgress, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("continuationItems", out var ci))
            CollectVideos(ci, items, playbackProgress, ref continuationToken, ref trackingParams);
    }

    private static void AddVideo(
        VideoSummary summary,
        VideoPlaybackProgress? progress,
        List<VideoSummary> items,
        Dictionary<VideoId, VideoPlaybackProgress> playbackProgress)
    {
        items.Add(summary);
        if (progress != null)
            playbackProgress[summary.Id] = progress;
    }

    private static void CollectPlaylists(JsonElement element, List<PlaylistSummary> items,
        ref string? continuationToken, ref string? trackingParams)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                CollectPlaylists(item, items, ref continuationToken, ref trackingParams);
            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
            return;

        var (tok, trk) = element.ExtractContinuation();
        if (!string.IsNullOrEmpty(tok))
        {
            continuationToken = tok;
            trackingParams = trk;
            return;
        }

        if (element.TryGetProperty("playlistRenderer", out var pr))
        {
            var summary = SearchHandler.ParsePlaylistSummary(pr);
            if (summary != null)
                items.Add(summary);
            return;
        }

        if (element.TryGetProperty("gridPlaylistRenderer", out var gpr))
        {
            var summary = SearchHandler.ParsePlaylistSummary(gpr);
            if (summary != null)
                items.Add(summary);
            return;
        }

        if (element.TryGetProperty("lockupViewModel", out var lockup))
        {
            var res = SearchHandler.ParseLockupViewModel(lockup);
            if (res is PlaylistSearchResult psr)
                items.Add(psr.Playlist);
            return;
        }

        if (element.TryGetProperty("contents", out var contents))
            CollectPlaylists(contents, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("items", out var listItems))
            CollectPlaylists(listItems, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("richItemRenderer", out var rir) && rir.TryGetProperty("content", out var rc))
            CollectPlaylists(rc, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("tabRenderer", out var tr) && tr.TryGetProperty("content", out var tc))
            CollectPlaylists(tc, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("twoColumnBrowseResultsRenderer", out var tcbrr))
            CollectPlaylists(tcbrr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("tabs", out var tabs))
            CollectPlaylists(tabs, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("richGridRenderer", out var rgr))
            CollectPlaylists(rgr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("sectionListRenderer", out var slr))
            CollectPlaylists(slr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("itemSectionRenderer", out var isr))
            CollectPlaylists(isr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("onResponseReceivedActions", out var actions))
            CollectPlaylists(actions, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("appendContinuationItemsAction", out var acia))
            CollectPlaylists(acia, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("reloadContinuationItemsCommand", out var rcic))
            CollectPlaylists(rcic, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("continuationItems", out var ci))
            CollectPlaylists(ci, items, ref continuationToken, ref trackingParams);
    }
}