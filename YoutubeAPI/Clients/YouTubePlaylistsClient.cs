using YoutubeAPI.Infrastructure;
using YoutubeAPI.Models.Continuations;
using YoutubeAPI.Models.Playlists;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Clients;

/// <summary>
///     Provides operations for querying, managing, creating, and modifying YouTube playlists and their items.
/// </summary>
public sealed class YouTubePlaylistsClient
{
    private readonly IYouTubePlaylistsHandler? _handler;

    internal YouTubePlaylistsClient(IYouTubePlaylistsHandler? handler = null)
    {
        _handler = handler;
    }

    /// <summary>
    ///     Gets full metadata for a YouTube playlist by ID.
    /// </summary>
    /// <param name="playlistId">The unique playlist identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding the full <see cref="Playlist" /> metadata.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Playlist> GetAsync(PlaylistId playlistId, CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Playlists handler is not configured.")
            : _handler.GetAsync(playlistId, cancellationToken);
    }

    /// <summary>
    ///     Retrieves the first page of items contained within a playlist.
    /// </summary>
    /// <param name="playlistId">The unique playlist identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding a page of playlist items with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<PlaylistItem, PlaylistItemsContinuation>> GetItemsPageAsync(PlaylistId playlistId,
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Playlists handler is not configured.")
            : _handler.GetItemsPageAsync(playlistId, cancellationToken);
    }

    /// <summary>
    ///     Retrieves the next page of items contained within a playlist using a continuation token.
    /// </summary>
    /// <param name="continuation">The continuation token from a previous playlist items page.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding the next page of playlist items with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<PlaylistItem, PlaylistItemsContinuation>> GetItemsPageAsync(PlaylistItemsContinuation continuation,
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Playlists handler is not configured.")
            : _handler.GetItemsPageAsync(continuation, cancellationToken);
    }

    /// <summary>
    ///     Retrieves the first page of playlists owned by the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding a page of owned playlist summaries with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<PlaylistSummary, OwnedPlaylistsContinuation>> GetMinePageAsync(
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Playlists handler is not configured.")
            : _handler.GetMinePageAsync(cancellationToken);
    }

    /// <summary>
    ///     Retrieves the next page of playlists owned by the authenticated user using a continuation token.
    /// </summary>
    /// <param name="continuation">The continuation token from a previous owned playlists page.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding the next page of owned playlist summaries with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<PlaylistSummary, OwnedPlaylistsContinuation>> GetMinePageAsync(
        OwnedPlaylistsContinuation continuation, CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Playlists handler is not configured.")
            : _handler.GetMinePageAsync(continuation, cancellationToken);
    }

    /// <summary>
    ///     Creates a new playlist for the authenticated user.
    /// </summary>
    /// <param name="request">The creation parameters for the new playlist.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding the <see cref="PlaylistId" /> of the newly created playlist.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<PlaylistId> CreateAsync(CreatePlaylistRequest request, CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Playlists handler is not configured.")
            : _handler.CreateAsync(request, cancellationToken);
    }

    /// <summary>
    ///     Deletes a playlist owned by the authenticated user.
    /// </summary>
    /// <param name="playlistId">The unique identifier of the playlist to delete.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous delete operation.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task DeleteAsync(PlaylistId playlistId, CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Playlists handler is not configured.")
            : _handler.DeleteAsync(playlistId, cancellationToken);
    }

    /// <summary>
    ///     Appends a video to a playlist owned by the authenticated user.
    /// </summary>
    /// <param name="playlistId">The playlist identifier.</param>
    /// <param name="videoId">The video identifier to add.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous add operation.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task AddVideoAsync(PlaylistId playlistId, VideoId videoId, CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Playlists handler is not configured.")
            : _handler.AddVideoAsync(playlistId, videoId, cancellationToken);
    }

    /// <summary>
    ///     Removes an item occurrence from a playlist owned by the authenticated user using its playlist item ID.
    /// </summary>
    /// <param name="playlistId">The playlist identifier.</param>
    /// <param name="itemId">The playlist item occurrence identifier to remove.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous removal operation.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task RemoveItemAsync(PlaylistId playlistId, PlaylistItemId itemId,
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Playlists handler is not configured.")
            : _handler.RemoveItemAsync(playlistId, itemId, cancellationToken);
    }
}