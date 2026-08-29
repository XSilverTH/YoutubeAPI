using YoutubeAPI.Models.Account;
using YoutubeAPI.Models.Channels;
using YoutubeAPI.Models.Comments;
using YoutubeAPI.Models.Continuations;
using YoutubeAPI.Models.Enums;
using YoutubeAPI.Models.Feeds;
using YoutubeAPI.Models.Playlists;
using YoutubeAPI.Models.Search;
using YoutubeAPI.Models.ValueTypes;
using YoutubeAPI.Models.Videos;

namespace YoutubeAPI.Infrastructure;

internal interface IYouTubeVideosHandler
{
    Task<Video> GetAsync(VideoId videoId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TranscriptTrack>> GetTranscriptTracksAsync(VideoId videoId, CancellationToken cancellationToken);

    Task<Transcript> GetTranscriptAsync(VideoId videoId, TranscriptTrackId trackId,
        CancellationToken cancellationToken);
}

internal interface IYouTubeSearchHandler
{
    Task<Page<SearchResult, SearchContinuation>> GetPageAsync(SearchRequest request,
        CancellationToken cancellationToken);

    Task<Page<SearchResult, SearchContinuation>> GetPageAsync(SearchContinuation continuation,
        CancellationToken cancellationToken);
}

internal interface IYouTubeSuggestionsHandler
{
    Task<IReadOnlyList<string>> GetAsync(string query, CancellationToken cancellationToken);
}

internal interface IYouTubeChannelsHandler
{
    Task<Channel> GetAsync(ChannelReference channel, CancellationToken cancellationToken);

    Task<Page<VideoSummary, ChannelVideosContinuation>> GetVideosPageAsync(ChannelReference channel,
        ChannelVideoSort sort, CancellationToken cancellationToken);

    Task<Page<VideoSummary, ChannelVideosContinuation>> GetVideosPageAsync(ChannelVideosContinuation continuation,
        CancellationToken cancellationToken);

    Task<Page<PlaylistSummary, ChannelPlaylistsContinuation>> GetPlaylistsPageAsync(ChannelReference channel,
        CancellationToken cancellationToken);

    Task<Page<PlaylistSummary, ChannelPlaylistsContinuation>> GetPlaylistsPageAsync(
        ChannelPlaylistsContinuation continuation, CancellationToken cancellationToken);
}

internal interface IYouTubePlaylistsHandler
{
    Task<Playlist> GetAsync(PlaylistId playlistId, CancellationToken cancellationToken);

    Task<Page<PlaylistItem, PlaylistItemsContinuation>> GetItemsPageAsync(PlaylistId playlistId,
        CancellationToken cancellationToken);

    Task<Page<PlaylistItem, PlaylistItemsContinuation>> GetItemsPageAsync(PlaylistItemsContinuation continuation,
        CancellationToken cancellationToken);

    Task<Page<PlaylistSummary, OwnedPlaylistsContinuation>> GetMinePageAsync(CancellationToken cancellationToken);

    Task<Page<PlaylistSummary, OwnedPlaylistsContinuation>> GetMinePageAsync(OwnedPlaylistsContinuation continuation,
        CancellationToken cancellationToken);

    Task<PlaylistId> CreateAsync(CreatePlaylistRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(PlaylistId playlistId, CancellationToken cancellationToken);
    Task AddVideoAsync(PlaylistId playlistId, VideoId videoId, CancellationToken cancellationToken);
    Task RemoveItemAsync(PlaylistId playlistId, PlaylistItemId itemId, CancellationToken cancellationToken);
}

internal interface IYouTubeCommentsHandler
{
    Task<Page<CommentThread, CommentThreadsContinuation>> GetThreadsPageAsync(VideoId videoId, CommentSort sort,
        CancellationToken cancellationToken);

    Task<Page<CommentThread, CommentThreadsContinuation>> GetThreadsPageAsync(CommentThreadsContinuation continuation,
        CancellationToken cancellationToken);

    Task<Page<Comment, CommentRepliesContinuation>> GetRepliesPageAsync(CommentRepliesContinuation continuation,
        CancellationToken cancellationToken);
}

internal interface IYouTubeFeedsHandler
{
    Task<Page<FeedItem, HomeContinuation>> GetHomePageAsync(CancellationToken cancellationToken);

    Task<Page<FeedItem, HomeContinuation>> GetHomePageAsync(HomeContinuation continuation,
        CancellationToken cancellationToken);

    Task<Page<FeedItem, SubscriptionsContinuation>> GetSubscriptionsPageAsync(CancellationToken cancellationToken);

    Task<Page<FeedItem, SubscriptionsContinuation>> GetSubscriptionsPageAsync(SubscriptionsContinuation continuation,
        CancellationToken cancellationToken);

    Task<Page<ChannelSummary, SubscribedChannelsContinuation>> GetSubscribedChannelsPageAsync(
        CancellationToken cancellationToken);

    Task<Page<ChannelSummary, SubscribedChannelsContinuation>> GetSubscribedChannelsPageAsync(
        SubscribedChannelsContinuation continuation, CancellationToken cancellationToken);

    Task<Page<HistoryEntry, HistoryContinuation>> GetHistoryPageAsync(CancellationToken cancellationToken);

    Task<Page<HistoryEntry, HistoryContinuation>> GetHistoryPageAsync(HistoryContinuation continuation,
        CancellationToken cancellationToken);
}

internal interface IYouTubeAccountHandler
{
    Task<Profile> GetProfileAsync(CancellationToken cancellationToken);
    Task SubscribeAsync(ChannelId channelId, CancellationToken cancellationToken);
    Task UnsubscribeAsync(ChannelId channelId, CancellationToken cancellationToken);
    Task RemoveHistoryEntryAsync(HistoryEntryId entryId, CancellationToken cancellationToken);
    Task ClearHistoryAsync(CancellationToken cancellationToken);
}

internal interface IYouTubeRatingsHandler
{
    Task<VideoRating> GetAsync(VideoId videoId, CancellationToken cancellationToken);
    Task SetAsync(VideoId videoId, VideoRating rating, CancellationToken cancellationToken);
}