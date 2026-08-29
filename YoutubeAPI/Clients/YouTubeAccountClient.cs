using YoutubeAPI.Infrastructure;
using YoutubeAPI.Models.Account;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Clients;

/// <summary>
///     Provides operations for managing account profile, subscriptions, and history records.
/// </summary>
public sealed class YouTubeAccountClient
{
    private readonly IYouTubeAccountHandler? _handler;

    internal YouTubeAccountClient(IYouTubeAccountHandler? handler = null)
    {
        _handler = handler;
    }

    /// <summary>
    ///     Gets the account profile information for the authenticated user.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding the user's <see cref="Profile" />.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Profile> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Account handler is not configured.")
            : _handler.GetProfileAsync(cancellationToken);
    }

    /// <summary>
    ///     Subscribes the authenticated user to the specified channel.
    /// </summary>
    /// <param name="channelId">The identifier of the channel to subscribe to.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous subscribe operation.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task SubscribeAsync(ChannelId channelId, CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Account handler is not configured.")
            : _handler.SubscribeAsync(channelId, cancellationToken);
    }

    /// <summary>
    ///     Unsubscribes the authenticated user from the specified channel.
    /// </summary>
    /// <param name="channelId">The identifier of the channel to unsubscribe from.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous unsubscribe operation.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task UnsubscribeAsync(ChannelId channelId, CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Account handler is not configured.")
            : _handler.UnsubscribeAsync(channelId, cancellationToken);
    }

    /// <summary>
    ///     Removes a specific entry from the authenticated user's watch history.
    /// </summary>
    /// <param name="entryId">The unique identifier of the history entry to remove.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous removal operation.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task RemoveHistoryEntryAsync(HistoryEntryId entryId, CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Account handler is not configured.")
            : _handler.RemoveHistoryEntryAsync(entryId, cancellationToken);
    }


    /// <summary>
    ///     Clears the authenticated user's entire watch history.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous clear history operation.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Account handler is not configured.")
            : _handler.ClearHistoryAsync(cancellationToken);
    }
}