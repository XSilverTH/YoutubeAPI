using YoutubeAPI.Infrastructure;
using YoutubeAPI.Models.Comments;
using YoutubeAPI.Models.Continuations;
using YoutubeAPI.Models.Enums;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Clients;

/// <summary>
///     Provides operations for querying video comment threads and nested replies.
/// </summary>
public sealed class YouTubeCommentsClient
{
    private readonly IYouTubeCommentsHandler? _handler;

    internal YouTubeCommentsClient(IYouTubeCommentsHandler? handler = null)
    {
        _handler = handler;
    }

    /// <summary>
    ///     Retrieves the first page of top-level comment threads for a YouTube video.
    /// </summary>
    /// <param name="videoId">The unique video identifier.</param>
    /// <param name="sort">The sort order for the comment threads (defaults to <see cref="CommentSort.Top" />).</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding a page of comment threads with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<CommentThread, CommentThreadsContinuation>> GetThreadsPageAsync(
        VideoId videoId,
        CommentSort sort = CommentSort.Top,
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Comments handler is not configured.")
            : _handler.GetThreadsPageAsync(videoId, sort, cancellationToken);
    }

    /// <summary>
    ///     Retrieves the next page of top-level comment threads for a YouTube video using a continuation token.
    /// </summary>
    /// <param name="continuation">The continuation token from a previous comment threads page.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding the next page of comment threads with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<CommentThread, CommentThreadsContinuation>> GetThreadsPageAsync(
        CommentThreadsContinuation continuation,
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Comments handler is not configured.")
            : _handler.GetThreadsPageAsync(continuation, cancellationToken);
    }

    /// <summary>
    ///     Retrieves a page of replies for a specific comment thread using a continuation token.
    /// </summary>
    /// <param name="continuation">
    ///     The continuation token returned in <see cref="CommentThread.NextReplies" /> or from a prior
    ///     replies page.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding a page of reply comments with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<Comment, CommentRepliesContinuation>> GetRepliesPageAsync(
        CommentRepliesContinuation continuation,
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Comments handler is not configured.")
            : _handler.GetRepliesPageAsync(continuation, cancellationToken);
    }
}