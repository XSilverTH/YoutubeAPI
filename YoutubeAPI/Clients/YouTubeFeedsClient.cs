using YoutubeAPI.Infrastructure;
using YoutubeAPI.Models.Channels;
using YoutubeAPI.Models.Continuations;
using YoutubeAPI.Models.Feeds;

namespace YoutubeAPI.Clients;

/// <summary>
///     Provides operations for querying user feeds: Home, Subscriptions, Subscribed Channels, and Watch History.
/// </summary>
public sealed class YouTubeFeedsClient
{
    private readonly IYouTubeFeedsHandler? _handler;

    internal YouTubeFeedsClient(IYouTubeFeedsHandler? handler = null)
    {
        _handler = handler;
    }

    /// <summary>
    ///     Retrieves the first page of items from the YouTube home feed.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding a page of feed items with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<FeedItem, HomeContinuation>> GetHomePageAsync(CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Feeds handler is not configured.")
            : _handler.GetHomePageAsync(cancellationToken);
    }

    /// <summary>
    ///     Retrieves the next page of items from the YouTube home feed using a continuation token.
    /// </summary>
    /// <param name="continuation">The continuation token from a previous home feed page.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding the next page of feed items with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<FeedItem, HomeContinuation>> GetHomePageAsync(HomeContinuation continuation,
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Feeds handler is not configured.")
            : _handler.GetHomePageAsync(continuation, cancellationToken);
    }

    /// <summary>
    ///     Retrieves the first page of items from the authenticated user's subscriptions feed.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding a page of feed items with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<FeedItem, SubscriptionsContinuation>> GetSubscriptionsPageAsync(
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Feeds handler is not configured.")
            : _handler.GetSubscriptionsPageAsync(cancellationToken);
    }

    /// <summary>
    ///     Retrieves the next page of items from the authenticated user's subscriptions feed using a continuation token.
    /// </summary>
    /// <param name="continuation">The continuation token from a previous subscriptions feed page.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding the next page of feed items with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<FeedItem, SubscriptionsContinuation>> GetSubscriptionsPageAsync(
        SubscriptionsContinuation continuation, CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Feeds handler is not configured.")
            : _handler.GetSubscriptionsPageAsync(continuation, cancellationToken);
    }

    /// <summary>
    ///     Retrieves the first page of channels to which the authenticated user is subscribed.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding a page of subscribed channel summaries with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<ChannelSummary, SubscribedChannelsContinuation>> GetSubscribedChannelsPageAsync(
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Feeds handler is not configured.")
            : _handler.GetSubscribedChannelsPageAsync(cancellationToken);
    }

    /// <summary>
    ///     Retrieves the next page of channels to which the authenticated user is subscribed using a continuation token.
    /// </summary>
    /// <param name="continuation">The continuation token from a previous subscribed channels page.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding the next page of subscribed channel summaries with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<ChannelSummary, SubscribedChannelsContinuation>> GetSubscribedChannelsPageAsync(
        SubscribedChannelsContinuation continuation, CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Feeds handler is not configured.")
            : _handler.GetSubscribedChannelsPageAsync(continuation, cancellationToken);
    }

    /// <summary>
    ///     Retrieves the first page of the authenticated user's watch history.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding a page of history entries with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<HistoryEntry, HistoryContinuation>> GetHistoryPageAsync(
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Feeds handler is not configured.")
            : _handler.GetHistoryPageAsync(cancellationToken);
    }

    /// <summary>
    ///     Retrieves the next page of the authenticated user's watch history using a continuation token.
    /// </summary>
    /// <param name="continuation">The continuation token from a previous history page.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding the next page of history entries with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<HistoryEntry, HistoryContinuation>> GetHistoryPageAsync(HistoryContinuation continuation,
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Feeds handler is not configured.")
            : _handler.GetHistoryPageAsync(continuation, cancellationToken);
    }
}