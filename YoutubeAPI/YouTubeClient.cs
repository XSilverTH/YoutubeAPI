using YoutubeAPI.Clients;
using YoutubeAPI.Infrastructure;

namespace YoutubeAPI;

/// <summary>
///     The primary client entry point for interacting with YouTube data and account services.
/// </summary>
public sealed class YouTubeClient : IDisposable
{
    private readonly InnerTubeSession? _session;
    private bool _disposed;

    /// <summary>
    ///     Initializes a new instance of the <see cref="YouTubeClient" /> class with optional configuration.
    /// </summary>
    /// <param name="options">Optional client configuration settings.</param>
    public YouTubeClient(YouTubeClientOptions? options = null)
    {
        _session = new InnerTubeSession(options);

        var videosHandler = new VideosHandler(_session);
        var searchHandler = new SearchHandler(_session);
        var suggestionsHandler = new SuggestionsHandler(_session);
        var channelsHandler = new ChannelsHandler(_session);
        var playlistsHandler = new PlaylistsHandler(_session);
        var commentsHandler = new CommentsHandler(_session);
        var feedsHandler = new FeedsHandler(_session);
        var accountHandler = new AccountHandler(_session);
        var ratingsHandler = new RatingsHandler(_session);

        Videos = new YouTubeVideosClient(videosHandler);
        Search = new YouTubeSearchClient(searchHandler);
        Suggestions = new YouTubeSuggestionsClient(suggestionsHandler);
        Channels = new YouTubeChannelsClient(channelsHandler);
        Playlists = new YouTubePlaylistsClient(playlistsHandler);
        Comments = new YouTubeCommentsClient(commentsHandler);
        Feeds = new YouTubeFeedsClient(feedsHandler);
        Account = new YouTubeAccountClient(accountHandler);
        Ratings = new YouTubeRatingsClient(ratingsHandler);
    }

    /// <summary>
    ///     Gets the video metadata and transcript operations client.
    /// </summary>
    public YouTubeVideosClient Videos { get; }

    /// <summary>
    ///     Gets the search operations client.
    /// </summary>
    public YouTubeSearchClient Search { get; }

    /// <summary>
    ///     Gets the query suggestions client.
    /// </summary>
    public YouTubeSuggestionsClient Suggestions { get; }

    /// <summary>
    ///     Gets the channel metadata and tabs operations client.
    /// </summary>
    public YouTubeChannelsClient Channels { get; }

    /// <summary>
    ///     Gets the playlist operations client.
    /// </summary>
    public YouTubePlaylistsClient Playlists { get; }

    /// <summary>
    ///     Gets the comment threads and replies operations client.
    /// </summary>
    public YouTubeCommentsClient Comments { get; }

    /// <summary>
    ///     Gets the feeds (Home, Subscriptions, History) operations client.
    /// </summary>
    public YouTubeFeedsClient Feeds { get; }

    /// <summary>
    ///     Gets the account management operations client.
    /// </summary>
    public YouTubeAccountClient Account { get; }

    /// <summary>
    ///     Gets the video ratings operations client.
    /// </summary>
    public YouTubeRatingsClient Ratings { get; }

    /// <summary>
    ///     Releases all managed and unmanaged resources held by this <see cref="YouTubeClient" />.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _session?.Dispose();
    }
}