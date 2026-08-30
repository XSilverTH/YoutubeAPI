using System.Text.Json;
using YoutubeAPI.Exceptions;
using YoutubeAPI.Models.Channels;
using YoutubeAPI.Models.Continuations;
using YoutubeAPI.Models.Feeds;
using YoutubeAPI.Models.Search;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Infrastructure;

internal sealed class FeedsHandler(InnerTubeSession session) : IYouTubeFeedsHandler
{
    public async Task<Page<FeedItem, HomeContinuation>> GetHomePageAsync(CancellationToken cancellationToken)
    {
        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer => { writer.WriteString("browseId", "FEwhat_to_watch"); },
            cancellationToken).ConfigureAwait(false);

        return ParseHomeFeedResponse(doc.RootElement);
    }

    public async Task<Page<FeedItem, HomeContinuation>> GetHomePageAsync(
        HomeContinuation continuation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer => { writer.WriteString("continuation", continuation.Token); },
            cancellationToken).ConfigureAwait(false);

        return ParseHomeFeedResponse(doc.RootElement);
    }

    public async Task<Page<FeedItem, SubscriptionsContinuation>> GetSubscriptionsPageAsync(
        CancellationToken cancellationToken)
    {
        session.EnsureAuthenticated();
        var profileId = await GetActiveProfileIdAsync(cancellationToken).ConfigureAwait(false);

        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer => { writer.WriteString("browseId", "FEsubscriptions"); },
            cancellationToken).ConfigureAwait(false);

        return ParseSubscriptionsResponse(doc.RootElement, profileId);
    }

    public async Task<Page<FeedItem, SubscriptionsContinuation>> GetSubscriptionsPageAsync(
        SubscriptionsContinuation continuation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);
        await ValidateContinuationProfileAsync(continuation.ProfileId, cancellationToken).ConfigureAwait(false);

        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer => { writer.WriteString("continuation", continuation.Token); },
            cancellationToken).ConfigureAwait(false);

        return ParseSubscriptionsResponse(doc.RootElement, continuation.ProfileId);
    }

    public async Task<Page<ChannelSummary, SubscribedChannelsContinuation>> GetSubscribedChannelsPageAsync(
        CancellationToken cancellationToken)
    {
        session.EnsureAuthenticated();
        var profileId = await GetActiveProfileIdAsync(cancellationToken).ConfigureAwait(false);

        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer => { writer.WriteString("browseId", "FEchannels"); },
            cancellationToken).ConfigureAwait(false);

        return ParseSubscribedChannelsResponse(doc.RootElement, profileId);
    }

    public async Task<Page<ChannelSummary, SubscribedChannelsContinuation>> GetSubscribedChannelsPageAsync(
        SubscribedChannelsContinuation continuation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);
        await ValidateContinuationProfileAsync(continuation.ProfileId, cancellationToken).ConfigureAwait(false);

        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer => { writer.WriteString("continuation", continuation.Token); },
            cancellationToken).ConfigureAwait(false);

        return ParseSubscribedChannelsResponse(doc.RootElement, continuation.ProfileId);
    }

    public async Task<Page<HistoryEntry, HistoryContinuation>> GetHistoryPageAsync(CancellationToken cancellationToken)
    {
        session.EnsureAuthenticated();
        var profileId = await GetActiveProfileIdAsync(cancellationToken).ConfigureAwait(false);

        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer => { writer.WriteString("browseId", "FEhistory"); },
            cancellationToken).ConfigureAwait(false);

        return ParseHistoryResponse(doc.RootElement, profileId);
    }

    public async Task<Page<HistoryEntry, HistoryContinuation>> GetHistoryPageAsync(
        HistoryContinuation continuation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);
        await ValidateContinuationProfileAsync(continuation.ProfileId, cancellationToken).ConfigureAwait(false);

        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer => { writer.WriteString("continuation", continuation.Token); },
            cancellationToken).ConfigureAwait(false);

        return ParseHistoryResponse(doc.RootElement, continuation.ProfileId);
    }

    private async Task<string?> GetActiveProfileIdAsync(CancellationToken cancellationToken)
    {
        var accountHandler = new AccountHandler(session);
        var profile = await accountHandler.GetProfileAsync(cancellationToken).ConfigureAwait(false);
        return profile.ChannelId?.Value ?? profile.Handle ?? profile.DisplayName;
    }

    private async Task ValidateContinuationProfileAsync(string? continuationProfileId,
        CancellationToken cancellationToken)
    {
        session.EnsureAuthenticated();

        if (string.IsNullOrEmpty(continuationProfileId))
            return;

        var currentProfileId = await GetActiveProfileIdAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(currentProfileId) &&
            !string.Equals(continuationProfileId, currentProfileId, StringComparison.Ordinal))
            throw new PermissionDeniedException("Continuation token was issued for a different account profile.");
    }

    private static Page<FeedItem, HomeContinuation> ParseHomeFeedResponse(JsonElement root)
    {
        var items = new List<FeedItem>();
        string? continuationToken = null;
        string? trackingParams = null;

        CollectFeedItems(root, items, ref continuationToken, ref trackingParams);

        HomeContinuation? next = null;
        if (!string.IsNullOrEmpty(continuationToken)) next = new HomeContinuation(continuationToken, trackingParams);

        return new Page<FeedItem, HomeContinuation>(items, next);
    }

    private static Page<FeedItem, SubscriptionsContinuation> ParseSubscriptionsResponse(JsonElement root,
        string? profileId)
    {
        var items = new List<FeedItem>();
        string? continuationToken = null;
        string? trackingParams = null;

        CollectFeedItems(root, items, ref continuationToken, ref trackingParams);

        SubscriptionsContinuation? next = null;
        if (!string.IsNullOrEmpty(continuationToken))
            next = new SubscriptionsContinuation(continuationToken, profileId, trackingParams);

        return new Page<FeedItem, SubscriptionsContinuation>(items, next);
    }

    private static Page<ChannelSummary, SubscribedChannelsContinuation> ParseSubscribedChannelsResponse(
        JsonElement root, string? profileId)
    {
        var items = new List<ChannelSummary>();
        string? continuationToken = null;
        string? trackingParams = null;

        CollectSubscribedChannels(root, items, ref continuationToken, ref trackingParams);

        SubscribedChannelsContinuation? next = null;
        if (!string.IsNullOrEmpty(continuationToken))
            next = new SubscribedChannelsContinuation(continuationToken, profileId, trackingParams);

        return new Page<ChannelSummary, SubscribedChannelsContinuation>(items, next);
    }

    private static Page<HistoryEntry, HistoryContinuation> ParseHistoryResponse(JsonElement root, string? profileId)
    {
        var items = new List<HistoryEntry>();
        string? continuationToken = null;
        string? trackingParams = null;

        CollectHistoryEntries(root, items, ref continuationToken, ref trackingParams);

        HistoryContinuation? next = null;
        if (!string.IsNullOrEmpty(continuationToken))
            next = new HistoryContinuation(continuationToken, profileId, trackingParams);

        return new Page<HistoryEntry, HistoryContinuation>(items, next);
    }

    private static void CollectFeedItems(
        JsonElement element,
        List<FeedItem> items,
        ref string? continuationToken,
        ref string? trackingParams)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                CollectFeedItems(item, items, ref continuationToken, ref trackingParams);
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
                items.Add(new VideoFeedItem(summary)
                {
                    PlaybackProgress = InnerTubeElement.ParsePlaybackProgress(vr)
                });
            return;
        }

        if (element.TryGetProperty("gridVideoRenderer", out var gvr))
        {
            var summary = SearchHandler.ParseVideoSummary(gvr);
            if (summary != null)
                items.Add(new VideoFeedItem(summary)
                {
                    PlaybackProgress = InnerTubeElement.ParsePlaybackProgress(gvr)
                });
            return;
        }

        if (element.TryGetProperty("compactVideoRenderer", out var cvr))
        {
            var summary = SearchHandler.ParseVideoSummary(cvr);
            if (summary != null)
                items.Add(new VideoFeedItem(summary)
                {
                    PlaybackProgress = InnerTubeElement.ParsePlaybackProgress(cvr)
                });
            return;
        }

        if (element.TryGetProperty("playlistPanelVideoRenderer", out var ppvr))
        {
            var summary = SearchHandler.ParseVideoSummary(ppvr);
            if (summary != null)
                items.Add(new VideoFeedItem(summary)
                {
                    PlaybackProgress = InnerTubeElement.ParsePlaybackProgress(ppvr)
                });
            return;
        }

        if (element.TryGetProperty("videoWithContextRenderer", out var vwcr))
        {
            var summary = SearchHandler.ParseVideoSummary(vwcr);
            if (summary != null)
                items.Add(new VideoFeedItem(summary)
                {
                    PlaybackProgress = InnerTubeElement.ParsePlaybackProgress(vwcr)
                });
            return;
        }

        if (element.TryGetProperty("channelRenderer", out var cr))
        {
            var summary = SearchHandler.ParseChannelSummary(cr);
            if (summary != null)
                items.Add(new ChannelFeedItem(summary));
            return;
        }

        if (element.TryGetProperty("playlistRenderer", out var pr))
        {
            var summary = SearchHandler.ParsePlaylistSummary(pr);
            if (summary != null)
                items.Add(new PlaylistFeedItem(summary));
            return;
        }

        if (element.TryGetProperty("lockupViewModel", out var lockup))
        {
            var res = SearchHandler.ParseLockupViewModel(lockup);
            switch (res)
            {
                case VideoSearchResult vsr:
                    items.Add(new VideoFeedItem(vsr.Video)
                    {
                        PlaybackProgress = vsr.PlaybackProgress
                    });
                    break;
                case ChannelSearchResult csr:
                    items.Add(new ChannelFeedItem(csr.Channel));
                    break;
                case PlaylistSearchResult psr:
                    items.Add(new PlaylistFeedItem(psr.Playlist));
                    break;
            }

            return;
        }

        if (element.TryGetProperty("contents", out var contents))
            CollectFeedItems(contents, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("items", out var listItems))
            CollectFeedItems(listItems, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("richItemRenderer", out var rir) && rir.TryGetProperty("content", out var rc))
            CollectFeedItems(rc, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("twoColumnBrowseResultsRenderer", out var tcbrr))
            CollectFeedItems(tcbrr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("tabs", out var tabs))
            CollectFeedItems(tabs, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("tabRenderer", out var tr) && tr.TryGetProperty("content", out var tc))
            CollectFeedItems(tc, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("richGridRenderer", out var rgr))
            CollectFeedItems(rgr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("sectionListRenderer", out var slr))
            CollectFeedItems(slr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("itemSectionRenderer", out var isr))
            CollectFeedItems(isr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("onResponseReceivedActions", out var actions))
            CollectFeedItems(actions, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("appendContinuationItemsAction", out var acia))
            CollectFeedItems(acia, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("reloadContinuationItemsCommand", out var rcic))
            CollectFeedItems(rcic, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("continuationItems", out var ci))
            CollectFeedItems(ci, items, ref continuationToken, ref trackingParams);
    }

    private static void CollectSubscribedChannels(
        JsonElement element,
        List<ChannelSummary> items,
        ref string? continuationToken,
        ref string? trackingParams)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                CollectSubscribedChannels(item, items, ref continuationToken, ref trackingParams);
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

        if (element.TryGetProperty("channelRenderer", out var cr))
        {
            var summary = SearchHandler.ParseChannelSummary(cr);
            if (summary != null)
                items.Add(summary);
            return;
        }

        if (element.TryGetProperty("contents", out var contents))
            CollectSubscribedChannels(contents, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("items", out var listItems))
            CollectSubscribedChannels(listItems, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("shelfRenderer", out var sr) && sr.TryGetProperty("content", out var sc))
            CollectSubscribedChannels(sc, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("expandedShelfContentsRenderer", out var escr) &&
            escr.TryGetProperty("items", out var escrItems))
            CollectSubscribedChannels(escrItems, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("twoColumnBrowseResultsRenderer", out var tcbrr))
            CollectSubscribedChannels(tcbrr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("tabs", out var tabs))
            CollectSubscribedChannels(tabs, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("tabRenderer", out var tr) && tr.TryGetProperty("content", out var tc))
            CollectSubscribedChannels(tc, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("sectionListRenderer", out var slr))
            CollectSubscribedChannels(slr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("itemSectionRenderer", out var isr))
            CollectSubscribedChannels(isr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("onResponseReceivedActions", out var actions))
            CollectSubscribedChannels(actions, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("appendContinuationItemsAction", out var acia))
            CollectSubscribedChannels(acia, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("continuationItems", out var ci))
            CollectSubscribedChannels(ci, items, ref continuationToken, ref trackingParams);
    }

    private static void CollectHistoryEntries(
        JsonElement element,
        List<HistoryEntry> items,
        ref string? continuationToken,
        ref string? trackingParams)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                CollectHistoryEntries(item, items, ref continuationToken, ref trackingParams);
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
            if (summary == null) return;
            var feedbackToken = FindFeedbackToken(vr);
            if (string.IsNullOrEmpty(feedbackToken)) feedbackToken = summary.Id.Value;
            if (HistoryEntryId.TryParse(feedbackToken, out var entryId))
                items.Add(new HistoryEntry(entryId,
                    new VideoFeedItem(summary)
                    {
                        PlaybackProgress = InnerTubeElement.ParsePlaybackProgress(vr)
                    }));

            return;
        }

        if (element.TryGetProperty("compactVideoRenderer", out var cvr) ||
            element.TryGetProperty("videoWithContextRenderer", out cvr))
        {
            var summary = SearchHandler.ParseVideoSummary(cvr);
            if (summary == null) return;

            if (HistoryEntryId.TryParse(summary.Id.Value, out var entryId))
                items.Add(new HistoryEntry(entryId,
                    new VideoFeedItem(summary)
                    {
                        PlaybackProgress = InnerTubeElement.ParsePlaybackProgress(cvr)
                    }));

            return;
        }

        if (element.TryGetProperty("lockupViewModel", out var lockup))
        {
            var res = SearchHandler.ParseLockupViewModel(lockup);
            if (res is not VideoSearchResult vsr) return;
            var feedbackToken = FindFeedbackTokenFromLockup(lockup);
            if (string.IsNullOrEmpty(feedbackToken)) feedbackToken = vsr.Video.Id.Value;

            if (HistoryEntryId.TryParse(feedbackToken, out var entryId))
                items.Add(new HistoryEntry(entryId,
                    new VideoFeedItem(vsr.Video)
                    {
                        PlaybackProgress = vsr.PlaybackProgress
                    }));

            return;
        }

        if (element.TryGetProperty("contents", out var contents))
            CollectHistoryEntries(contents, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("items", out var listItems))
            CollectHistoryEntries(listItems, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("twoColumnBrowseResultsRenderer", out var tcbrr))
            CollectHistoryEntries(tcbrr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("tabs", out var tabs))
            CollectHistoryEntries(tabs, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("tabRenderer", out var tr) && tr.TryGetProperty("content", out var tc))
            CollectHistoryEntries(tc, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("sectionListRenderer", out var slr))
            CollectHistoryEntries(slr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("itemSectionRenderer", out var isr))
            CollectHistoryEntries(isr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("onResponseReceivedActions", out var actions))
            CollectHistoryEntries(actions, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("appendContinuationItemsAction", out var acia))
            CollectHistoryEntries(acia, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("continuationItems", out var ci))
            CollectHistoryEntries(ci, items, ref continuationToken, ref trackingParams);
    }

    private static string? FindFeedbackToken(JsonElement vr)
    {
        if (!vr.TryGetProperty("menu", out var menu) ||
            !menu.TryGetProperty("menuRenderer", out var mr) ||
            !mr.TryGetProperty("items", out var menuItems) ||
            menuItems.ValueKind != JsonValueKind.Array) return null;
        foreach (var item in menuItems.EnumerateArray())
            if (item.TryGetProperty("menuServiceItemRenderer", out var msir) &&
                msir.TryGetProperty("serviceEndpoint", out var se) &&
                se.TryGetProperty("feedbackEndpoint", out var fe) &&
                fe.TryGetProperty("feedbackToken", out var ft))
                return ft.GetString();

        return null;
    }

    private static string? FindFeedbackTokenFromLockup(JsonElement lockup)
    {
        try
        {
            var menuButton = lockup.GetPropertyOrDefault("metadata")
                .GetPropertyOrDefault("lockupMetadataViewModel")
                .GetPropertyOrDefault("menuButton");

            var listItems = menuButton.GetPropertyOrDefault("buttonViewModel")
                .GetPropertyOrDefault("onTap")
                .GetPropertyOrDefault("innertubeCommand")
                .GetPropertyOrDefault("showSheetCommand")
                .GetPropertyOrDefault("panelLoadingStrategy")
                .GetPropertyOrDefault("inlineContent")
                .GetPropertyOrDefault("sheetViewModel")
                .GetPropertyOrDefault("content")
                .GetPropertyOrDefault("listViewModel")
                .GetPropertyOrDefault("listItems");

            if (listItems.ValueKind == JsonValueKind.Array)
                foreach (var onTap in listItems.EnumerateArray().Select(item => item
                             .GetPropertyOrDefault("listItemViewModel")
                             .GetPropertyOrDefault("rendererContext")
                             .GetPropertyOrDefault("commandContext")
                             .GetPropertyOrDefault("onTap")
                             .GetPropertyOrDefault("innertubeCommand")))
                    if (onTap.TryGetProperty("feedbackEndpoint", out var fe) &&
                        fe.TryGetProperty("feedbackToken", out var ft))
                        return ft.GetString();
        }
        catch
        {
            // Ignore navigation failure
        }

        return null;
    }
}