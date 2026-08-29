using YoutubeAPI.Infrastructure;
using YoutubeAPI.Models.Channels;
using YoutubeAPI.Models.Continuations;
using YoutubeAPI.Models.Enums;
using YoutubeAPI.Models.Playlists;
using YoutubeAPI.Models.ValueTypes;
using YoutubeAPI.Models.Videos;

namespace YoutubeAPI.Clients;

/// <summary>
///     Provides operations for querying channel metadata, videos, and playlists.
/// </summary>
public sealed class YouTubeChannelsClient
{
    private readonly IYouTubeChannelsHandler? _handler;

    internal YouTubeChannelsClient(IYouTubeChannelsHandler? handler = null)
    {
        _handler = handler;
    }

    /// <summary>
    ///     Gets full metadata for a YouTube channel by ID, handle, or URL reference.
    /// </summary>
    /// <param name="channel">The channel reference (ID, handle, or URL).</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding the full <see cref="Channel" /> metadata.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Channel> GetAsync(ChannelReference channel, CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Channels handler is not configured.")
            : _handler.GetAsync(channel, cancellationToken);
    }

    /// <summary>
    ///     Retrieves the first page of uploaded videos for a YouTube channel.
    /// </summary>
    /// <param name="channel">The channel reference (ID, handle, or URL).</param>
    /// <param name="sort">The sort order for the video listing (defaults to <see cref="ChannelVideoSort.Newest" />).</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding a page of video summaries with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<VideoSummary, ChannelVideosContinuation>> GetVideosPageAsync(
        ChannelReference channel,
        ChannelVideoSort sort = ChannelVideoSort.Newest,
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Channels handler is not configured.")
            : _handler.GetVideosPageAsync(channel, sort, cancellationToken);
    }


    /// <summary>
    ///     Retrieves the next page of uploaded videos for a YouTube channel using a continuation token.
    /// </summary>
    /// <param name="continuation">The continuation token from a previous channel videos page.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding the next page of video summaries with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<VideoSummary, ChannelVideosContinuation>> GetVideosPageAsync(
        ChannelVideosContinuation continuation,
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Channels handler is not configured.")
            : _handler.GetVideosPageAsync(continuation, cancellationToken);
    }

    /// <summary>
    ///     Retrieves the first page of public playlists published by a YouTube channel.
    /// </summary>
    /// <param name="channel">The channel reference (ID, handle, or URL).</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding a page of playlist summaries with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<PlaylistSummary, ChannelPlaylistsContinuation>> GetPlaylistsPageAsync(
        ChannelReference channel,
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Channels handler is not configured.")
            : _handler.GetPlaylistsPageAsync(channel, cancellationToken);
    }

    /// <summary>
    ///     Retrieves the next page of public playlists published by a YouTube channel using a continuation token.
    /// </summary>
    /// <param name="continuation">The continuation token from a previous channel playlists page.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding the next page of playlist summaries with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<PlaylistSummary, ChannelPlaylistsContinuation>> GetPlaylistsPageAsync(
        ChannelPlaylistsContinuation continuation,
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Channels handler is not configured.")
            : _handler.GetPlaylistsPageAsync(continuation, cancellationToken);
    }
}