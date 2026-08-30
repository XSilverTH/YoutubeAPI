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

internal sealed class PlaylistsHandler(InnerTubeSession session) : IYouTubePlaylistsHandler
{
    public async Task<Playlist> GetAsync(PlaylistId playlistId, CancellationToken cancellationToken)
    {
        var browseId = playlistId.Value.StartsWith("VL", StringComparison.OrdinalIgnoreCase)
            ? playlistId.Value
            : "VL" + playlistId.Value;

        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer => { writer.WriteString("browseId", browseId); },
            cancellationToken).ConfigureAwait(false);

        var root = doc.RootElement;
        var summary = ParsePlaylistDetailsHeader(root, playlistId);
        if (summary == null) throw new ResourceNotFoundException($"Playlist '{playlistId.Value}' was not found.");

        var description = ParsePlaylistDescription(root);
        var privacy = ParsePlaylistPrivacy(root);

        return new Playlist(summary, description, privacy);
    }

    public async Task<Page<PlaylistItem, PlaylistItemsContinuation>> GetItemsPageAsync(
        PlaylistId playlistId,
        CancellationToken cancellationToken)
    {
        var browseId = playlistId.Value.StartsWith("VL", StringComparison.OrdinalIgnoreCase)
            ? playlistId.Value
            : "VL" + playlistId.Value;

        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer => { writer.WriteString("browseId", browseId); },
            cancellationToken).ConfigureAwait(false);

        return ParsePlaylistItemsResponse(doc.RootElement, playlistId.Value);
    }

    public async Task<Page<PlaylistItem, PlaylistItemsContinuation>> GetItemsPageAsync(
        PlaylistItemsContinuation continuation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer => { writer.WriteString("continuation", continuation.Token); },
            cancellationToken).ConfigureAwait(false);

        return ParsePlaylistItemsResponse(doc.RootElement, continuation.PlaylistId);
    }

    public async Task<Page<PlaylistSummary, OwnedPlaylistsContinuation>> GetMinePageAsync(
        CancellationToken cancellationToken)
    {
        session.EnsureAuthenticated();
        var profile = await new AccountHandler(session).GetProfileAsync(cancellationToken).ConfigureAwait(false);
        var profileId = profile.ChannelId?.Value ?? profile.Handle ?? profile.DisplayName;

        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer => writer.WriteString("browseId", "FEplaylist_aggregation"),
            cancellationToken).ConfigureAwait(false);

        return ParseOwnedPlaylistsResponse(doc.RootElement, profileId);
    }

    public async Task<Page<PlaylistSummary, OwnedPlaylistsContinuation>> GetMinePageAsync(
        OwnedPlaylistsContinuation continuation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(continuation);
        session.EnsureAuthenticated();
        var profile = await new AccountHandler(session).GetProfileAsync(cancellationToken).ConfigureAwait(false);
        var profileId = profile.ChannelId?.Value ?? profile.Handle ?? profile.DisplayName;
        if (!string.IsNullOrEmpty(continuation.ProfileId) &&
            !string.Equals(continuation.ProfileId, profileId, StringComparison.Ordinal))
            throw new PermissionDeniedException("Continuation token was issued for a different account profile.");

        using var doc = await session.PostInnerTubeAsync(
            "browse",
            writer => writer.WriteString("continuation", continuation.Token),
            cancellationToken).ConfigureAwait(false);

        return ParseOwnedPlaylistsResponse(doc.RootElement, profileId);
    }

    public async Task<PlaylistId> CreateAsync(CreatePlaylistRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Playlist title cannot be empty or whitespace.", nameof(request));

        session.EnsureAuthenticated();

        var privacyStr = request.Privacy switch
        {
            PlaylistPrivacy.Public => "PUBLIC",
            PlaylistPrivacy.Unlisted => "UNLISTED",
            _ => "PRIVATE"
        };

        using var doc = await session.PostInnerTubeAsync(
            "playlist/create",
            writer =>
            {
                writer.WriteString("title", request.Title);
                if (!string.IsNullOrEmpty(request.Description)) writer.WriteString("description", request.Description);
                writer.WriteString("privacyStatus", privacyStr);
            },
            cancellationToken).ConfigureAwait(false);

        var root = doc.RootElement;
        var newPlaylistId = root.TryGetProperty("playlistId", out var pidEl) ? pidEl.GetString() : null;

        if (string.IsNullOrEmpty(newPlaylistId) && root.TryGetProperty("actions", out var actions) &&
            actions.ValueKind == JsonValueKind.Array)
            foreach (var action in actions.EnumerateArray())
                if (action.TryGetProperty("openPopupAction", out var opa) &&
                    opa.TryGetProperty("popup", out var popup) &&
                    popup.TryGetProperty("notificationActionRenderer", out var nar) &&
                    nar.TryGetProperty("actionButton", out var ab) &&
                    ab.TryGetProperty("buttonRenderer", out var br) &&
                    br.TryGetProperty("navigationEndpoint", out var ep) &&
                    ep.TryGetProperty("watchEndpoint", out var we) &&
                    we.TryGetProperty("playlistId", out var pid))
                {
                    newPlaylistId = pid.GetString();
                    break;
                }

        if (string.IsNullOrEmpty(newPlaylistId) || !PlaylistId.TryParse(newPlaylistId, out var playlistId))
            throw new YouTubeProtocolException("Failed to create playlist: no playlistId returned by YouTube.");

        return playlistId;
    }

    public async Task DeleteAsync(PlaylistId playlistId, CancellationToken cancellationToken)
    {
        session.EnsureAuthenticated();

        using var doc = await session.PostInnerTubeAsync(
            "playlist/delete",
            writer => { writer.WriteString("playlistId", playlistId.Value); },
            cancellationToken).ConfigureAwait(false);

        ValidateDeletePlaylistAcknowledgement(doc.RootElement, playlistId.Value);
    }

    public async Task AddVideoAsync(PlaylistId playlistId, VideoId videoId, CancellationToken cancellationToken)
    {
        session.EnsureAuthenticated();

        using var doc = await session.PostInnerTubeAsync(
            "browse/edit_playlist",
            writer =>
            {
                writer.WriteString("playlistId", playlistId.Value);
                writer.WriteStartArray("actions");
                writer.WriteStartObject();
                writer.WriteString("action", "ACTION_ADD_VIDEO");
                writer.WriteString("addedVideoId", videoId.Value);
                writer.WriteEndObject();
                writer.WriteEndArray();
            },
            cancellationToken).ConfigureAwait(false);

        ValidateEditPlaylistAcknowledgement(doc.RootElement, "playlist.add", playlistId.Value);
    }

    public async Task RemoveItemAsync(PlaylistId playlistId, PlaylistItemId itemId, CancellationToken cancellationToken)
    {
        session.EnsureAuthenticated();

        using var doc = await session.PostInnerTubeAsync(
            "browse/edit_playlist",
            writer =>
            {
                writer.WriteString("playlistId", playlistId.Value);
                writer.WriteStartArray("actions");
                writer.WriteStartObject();
                writer.WriteString("action", "ACTION_REMOVE_VIDEO_BY_SET_VIDEO_ID");
                writer.WriteString("setVideoId", itemId.Value);
                writer.WriteEndObject();
                writer.WriteEndArray();
            },
            cancellationToken).ConfigureAwait(false);

        ValidateEditPlaylistAcknowledgement(doc.RootElement, "playlist.remove", playlistId.Value);
    }

    private static void ValidateEditPlaylistAcknowledgement(JsonElement root, string operation, string playlistId)
    {
        var status = root.TryGetProperty("status", out var st) ? st.GetString() : null;
        if (status != null)
        {
            if (!status.Equals("STATUS_SUCCEEDED", StringComparison.OrdinalIgnoreCase))
                throw new YouTubeRequestException(
                    $"Failed to perform '{operation}' on playlist '{playlistId}': status '{status}'.", operation);
            return;
        }

        if (root.TryGetProperty("commandProcessed", out var cp) && cp.ValueKind == JsonValueKind.True) return;

        if (root.TryGetProperty("success", out var succ) && succ.ValueKind == JsonValueKind.True) return;

        if (root.TryGetProperty("actions", out var actions) && actions.ValueKind == JsonValueKind.Array &&
            actions.GetArrayLength() > 0) return;

        if (root.TryGetProperty("playlistEditResults", out var results) && results.ValueKind == JsonValueKind.Array &&
            results.GetArrayLength() > 0) return;

        throw new YouTubeProtocolException(
            $"YouTube returned an ambiguous or unacknowledged response for '{operation}' on playlist '{playlistId}'.");
    }

    private static void ValidateDeletePlaylistAcknowledgement(JsonElement root, string playlistId)
    {
        var status = root.TryGetProperty("status", out var st) ? st.GetString() : null;
        if (status != null)
        {
            if (!status.Equals("STATUS_SUCCEEDED", StringComparison.OrdinalIgnoreCase))
                throw new YouTubeRequestException($"Failed to delete playlist '{playlistId}': status '{status}'.",
                    "playlist.delete");
            return;
        }

        if (root.TryGetProperty("commandProcessed", out var cp) && cp.ValueKind == JsonValueKind.True) return;

        if (root.TryGetProperty("actions", out var actions) && actions.ValueKind == JsonValueKind.Array &&
            actions.GetArrayLength() > 0) return;

        if (root.TryGetProperty("responseContext", out _)) return;

        throw new YouTubeProtocolException(
            $"YouTube returned an ambiguous or unacknowledged response for playlist.delete on playlist '{playlistId}'.");
    }

    private static PlaylistSummary ParsePlaylistDetailsHeader(JsonElement root, PlaylistId playlistId)
    {
        var title = string.Empty;
        ChannelSummary? author = null;
        int? itemCount = null;
        var thumbnails = new List<Thumbnail>();

        if (root.TryGetProperty("header", out var h))
        {
            if (h.TryGetProperty("pageHeaderRenderer", out var phr))
            {
                title = phr.GetText("pageTitle");
                var thumbs = phr.GetThumbnails("contentImage");
                thumbnails.AddRange(thumbs);
            }
            else if (h.TryGetProperty("playlistHeaderRenderer", out var phr2))
            {
                title = phr2.GetText("title");
                var thumbs = phr2.GetThumbnails("playlistHeaderBanner");
                thumbnails.AddRange(thumbs);
            }
        }

        if (root.TryGetProperty("sidebar", out var sidebar) &&
            sidebar.TryGetProperty("playlistSidebarRenderer", out var psr) &&
            psr.TryGetProperty("items", out var sidebarItems) &&
            sidebarItems.ValueKind == JsonValueKind.Array)
            foreach (var item in sidebarItems.EnumerateArray())
                if (item.TryGetProperty("playlistSidebarPrimaryInfoRenderer", out var primary))
                {
                    if (string.IsNullOrEmpty(title)) title = primary.GetText("title");

                    if (thumbnails.Count == 0) thumbnails.AddRange(primary.GetThumbnails("thumbnailRenderer"));

                    if (!primary.TryGetProperty("stats", out var stats) ||
                        stats.ValueKind != JsonValueKind.Array) continue;
                    foreach (var statText in stats.EnumerateArray().Select(stat => stat.GetText()).Where(statText =>
                                 statText.Contains("video", StringComparison.OrdinalIgnoreCase) ||
                                 statText.Contains("episode", StringComparison.OrdinalIgnoreCase) ||
                                 statText.Contains("item", StringComparison.OrdinalIgnoreCase)))
                        itemCount = (int?)InnerTubeElement.ParseCount(statText);
                }
                else if (item.TryGetProperty("playlistSidebarSecondaryInfoRenderer", out var secondary))
                {
                    if (!secondary.TryGetProperty("videoOwner", out var vo) ||
                        !vo.TryGetProperty("videoOwnerRenderer", out var vor)) continue;
                    var channelTitle = vor.GetText("title");
                    string? channelIdStr = null;
                    if (vor.TryGetProperty("navigationEndpoint", out var nav) &&
                        nav.TryGetProperty("browseEndpoint", out var be) &&
                        be.TryGetProperty("browseId", out var bid))
                        channelIdStr = bid.GetString();

                    var channelId = ChannelId.TryParse(channelIdStr, out var cid)
                        ? cid
                        : new ChannelId("UC0000000000000000000000");
                    var channelThumbs = vor.GetThumbnails("thumbnail");
                    var isVerified = vor.IsVerified();

                    author = new ChannelSummary(
                        channelId,
                        channelTitle,
                        null,
                        new Uri($"https://www.youtube.com/channel/{channelId}"),
                        channelThumbs,
                        isVerified,
                        null);
                }

        if (string.IsNullOrEmpty(title))
            title = "Playlist";

        return new PlaylistSummary(
            playlistId,
            title,
            new Uri($"https://www.youtube.com/playlist?list={playlistId}"),
            author,
            itemCount,
            thumbnails);
    }

    private static string? ParsePlaylistDescription(JsonElement root)
    {
        if (!root.TryGetProperty("sidebar", out var sidebar) ||
            !sidebar.TryGetProperty("playlistSidebarRenderer", out var psr) ||
            !psr.TryGetProperty("items", out var sidebarItems) ||
            sidebarItems.ValueKind != JsonValueKind.Array)
            return root.TryGetProperty("header", out var h) ? h.GetText("description") : null;
        foreach (var item in sidebarItems.EnumerateArray())
            if (item.TryGetProperty("playlistSidebarPrimaryInfoRenderer", out var primary))
            {
                var desc = primary.GetText("description");
                if (!string.IsNullOrEmpty(desc))
                    return desc;
            }

        return root.TryGetProperty("header", out var a) ? a.GetText("description") : null;
    }

    private static PlaylistPrivacy? ParsePlaylistPrivacy(JsonElement root)
    {
        if (!root.TryGetProperty("sidebar", out var sidebar) ||
            !sidebar.TryGetProperty("playlistSidebarRenderer", out var psr) ||
            !psr.TryGetProperty("items", out var sidebarItems) ||
            sidebarItems.ValueKind != JsonValueKind.Array) return null;
        foreach (var item in sidebarItems.EnumerateArray())
            if (item.TryGetProperty("playlistSidebarPrimaryInfoRenderer", out var primary) &&
                primary.TryGetProperty("privacy", out var privEl))
            {
                var priv = privEl.GetString() ?? "";
                if (priv.Equals("PUBLIC", StringComparison.OrdinalIgnoreCase))
                    return PlaylistPrivacy.Public;
                if (priv.Equals("UNLISTED", StringComparison.OrdinalIgnoreCase))
                    return PlaylistPrivacy.Unlisted;
                if (priv.Equals("PRIVATE", StringComparison.OrdinalIgnoreCase))
                    return PlaylistPrivacy.Private;
            }

        return null;
    }

    private static Page<PlaylistItem, PlaylistItemsContinuation> ParsePlaylistItemsResponse(
        JsonElement root,
        string? playlistId)
    {
        var items = new List<PlaylistItem>();
        string? continuationToken = null;
        string? trackingParams = null;

        CollectPlaylistItems(root, items, ref continuationToken, ref trackingParams);

        PlaylistItemsContinuation? next = null;
        if (!string.IsNullOrEmpty(continuationToken))
            next = new PlaylistItemsContinuation(continuationToken, playlistId, trackingParams);

        return new Page<PlaylistItem, PlaylistItemsContinuation>(items, next);
    }

    private static Page<PlaylistSummary, OwnedPlaylistsContinuation> ParseOwnedPlaylistsResponse(
        JsonElement root,
        string? profileId)
    {
        var items = new List<PlaylistSummary>();
        string? continuationToken = null;
        string? trackingParams = null;

        CollectOwnedPlaylists(root, items, ref continuationToken, ref trackingParams);

        OwnedPlaylistsContinuation? next = null;
        if (!string.IsNullOrEmpty(continuationToken))
            next = new OwnedPlaylistsContinuation(continuationToken, profileId, trackingParams);

        return new Page<PlaylistSummary, OwnedPlaylistsContinuation>(items, next);
    }

    private static void CollectPlaylistItems(
        JsonElement element,
        List<PlaylistItem> items,
        ref string? continuationToken,
        ref string? trackingParams)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                CollectPlaylistItems(item, items, ref continuationToken, ref trackingParams);
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

        if (element.TryGetProperty("playlistVideoRenderer", out var pvr))
        {
            items.Add(ParsePlaylistItem(pvr, items.Count + 1));
            return;
        }

        if (element.TryGetProperty("lockupViewModel", out var lockup))
        {
            items.Add(ParseLockupPlaylistItem(lockup, items.Count + 1));
            return;
        }

        if (element.TryGetProperty("contents", out var contents))
            CollectPlaylistItems(contents, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("items", out var listItems))
            CollectPlaylistItems(listItems, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("playlistVideoListRenderer", out var pvlr))
            CollectPlaylistItems(pvlr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("twoColumnBrowseResultsRenderer", out var tcbrr))
            CollectPlaylistItems(tcbrr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("tabs", out var tabs))
            CollectPlaylistItems(tabs, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("tabRenderer", out var tr) && tr.TryGetProperty("content", out var tc))
            CollectPlaylistItems(tc, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("sectionListRenderer", out var slr))
            CollectPlaylistItems(slr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("itemSectionRenderer", out var isr))
            CollectPlaylistItems(isr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("onResponseReceivedActions", out var actions))
            CollectPlaylistItems(actions, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("appendContinuationItemsAction", out var acia))
            CollectPlaylistItems(acia, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("continuationItems", out var ci))
            CollectPlaylistItems(ci, items, ref continuationToken, ref trackingParams);
    }

    private static PlaylistItem ParsePlaylistItem(JsonElement pvr, int position)
    {
        var videoIdStr = pvr.TryGetProperty("videoId", out var vid) ? vid.GetString() : null;
        var setVideoIdStr = pvr.TryGetProperty("setVideoId", out var svid) ? svid.GetString() : null;
        PlaylistItemId? itemId =
            !string.IsNullOrEmpty(setVideoIdStr) && PlaylistItemId.TryParse(setVideoIdStr, out var pid) ? pid : null;

        var title = pvr.GetText("title");
        var isPlayable = pvr.TryGetProperty("isPlayable", out var ip) && ip.ValueKind == JsonValueKind.True;
        VideoSummary? videoSummary = null;
        VideoPlaybackProgress? playbackProgress = null;
        if (string.IsNullOrEmpty(videoIdStr) || !VideoId.TryParse(videoIdStr, out _))
            return new PlaylistItem(
                itemId,
                position,
                videoSummary,
                string.IsNullOrEmpty(title) ? videoSummary?.Title ?? "Video" : title,
                isPlayable);
        var summary = SearchHandler.ParseVideoSummary(pvr);
        if (summary == null)
            return new PlaylistItem(
                itemId,
                position,
                videoSummary,
                string.IsNullOrEmpty(title) ? videoSummary?.Title ?? "Video" : title,
                isPlayable);
        videoSummary = summary;
        playbackProgress = InnerTubeElement.ParsePlaybackProgress(pvr);
        isPlayable = true;

        return new PlaylistItem(
            itemId,
            position,
            videoSummary,
            string.IsNullOrEmpty(title) ? videoSummary.Title : title,
            isPlayable)
        {
            PlaybackProgress = playbackProgress
        };
    }

    private static PlaylistItem ParseLockupPlaylistItem(JsonElement lockup, int position)
    {
        var contentId = lockup.TryGetProperty("contentId", out var cid) ? cid.GetString() : null;
        var metadata = lockup.GetPropertyOrDefault("metadata").GetPropertyOrDefault("lockupMetadataViewModel");
        var title = metadata.GetPropertyOrDefault("title").GetText();

        VideoSummary? videoSummary = null;
        if (string.IsNullOrEmpty(contentId) || !VideoId.TryParse(contentId, out _))
            return new PlaylistItem(
                null,
                position,
                videoSummary,
                string.IsNullOrEmpty(title) ? "Video" : title,
                videoSummary != null);
        var res = SearchHandler.ParseLockupViewModel(lockup);
        if (res is not VideoSearchResult vsr)
            return new PlaylistItem(
                null,
                position,
                videoSummary,
                string.IsNullOrEmpty(title) ? "Video" : title,
                false);
        videoSummary = vsr.Video;

        return new PlaylistItem(
            null,
            position,
            videoSummary,
            string.IsNullOrEmpty(title) ? "Video" : title,
            true)
        {
            PlaybackProgress = vsr.PlaybackProgress
        };
    }

    private static void CollectOwnedPlaylists(
        JsonElement element,
        List<PlaylistSummary> items,
        ref string? continuationToken,
        ref string? trackingParams)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                CollectOwnedPlaylists(item, items, ref continuationToken, ref trackingParams);
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

        if (element.TryGetProperty("gridPlaylistRenderer", out var gpr))
        {
            var summary = SearchHandler.ParsePlaylistSummary(gpr);
            if (summary != null)
                items.Add(summary);
            return;
        }

        if (element.TryGetProperty("playlistRenderer", out var pr))
        {
            var summary = SearchHandler.ParsePlaylistSummary(pr);
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
            CollectOwnedPlaylists(contents, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("items", out var listItems))
            CollectOwnedPlaylists(listItems, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("richItemRenderer", out var rir) && rir.TryGetProperty("content", out var rc))
            CollectOwnedPlaylists(rc, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("twoColumnBrowseResultsRenderer", out var tcbrr))
            CollectOwnedPlaylists(tcbrr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("tabs", out var tabs))
            CollectOwnedPlaylists(tabs, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("tabRenderer", out var tr) && tr.TryGetProperty("content", out var tc))
            CollectOwnedPlaylists(tc, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("richGridRenderer", out var rgr))
            CollectOwnedPlaylists(rgr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("sectionListRenderer", out var slr))
            CollectOwnedPlaylists(slr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("itemSectionRenderer", out var isr))
            CollectOwnedPlaylists(isr, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("onResponseReceivedActions", out var actions))
            CollectOwnedPlaylists(actions, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("appendContinuationItemsAction", out var acia))
            CollectOwnedPlaylists(acia, items, ref continuationToken, ref trackingParams);
        if (element.TryGetProperty("continuationItems", out var ci))
            CollectOwnedPlaylists(ci, items, ref continuationToken, ref trackingParams);
    }
}