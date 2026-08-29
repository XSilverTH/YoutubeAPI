using YoutubeAPI.Models.Enums;

namespace YoutubeAPI.Models.Continuations;

/// <summary>
///     Represents an opaque continuation token for paginating top-level comment threads on a video.
/// </summary>
public sealed class CommentThreadsContinuation
{
    private const string RouteName = "comment_threads";

    internal CommentThreadsContinuation(string token, string? videoId = null, CommentSort sort = CommentSort.Top,
        string? trackingParams = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        Token = token;
        VideoId = videoId;
        Sort = sort;
        TrackingParams = trackingParams;
    }

    /// <summary>
    ///     Gets the underlying server continuation token.
    /// </summary>
    public string Token { get; }

    /// <summary>
    ///     Gets the video identifier associated with this continuation, if known.
    /// </summary>
    public string? VideoId { get; }

    /// <summary>
    ///     Gets the sort order applied to the comment threads.
    /// </summary>
    public CommentSort Sort { get; }

    /// <summary>
    ///     Gets the tracking parameter token, if available.
    /// </summary>
    public string? TrackingParams { get; }

    /// <summary>
    ///     Exports the continuation state into a durable, URL-safe base64 string.
    /// </summary>
    /// <returns>An opaque continuation string.</returns>
    public string Export()
    {
        var envelope = new ContinuationEnvelope(
            ContinuationEnvelope.CurrentVersion,
            RouteName,
            Token,
            VideoId,
            null,
            TrackingParams,
            Sort.ToString());

        return envelope.Encode();
    }

    /// <summary>
    ///     Imports and validates a previously exported comment threads continuation string.
    /// </summary>
    /// <param name="value">The exported continuation string.</param>
    /// <returns>A validated <see cref="CommentThreadsContinuation" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> is malformed or belongs to a different route.</exception>
    public static CommentThreadsContinuation Import(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var envelope = ContinuationEnvelope.Decode(value);
        if (!string.Equals(envelope.Route, RouteName, StringComparison.Ordinal))
            throw new FormatException($"Invalid continuation route '{envelope.Route}'. Expected '{RouteName}'.");

        var sort = CommentSort.Top;
        if (!string.IsNullOrEmpty(envelope.Extra) && Enum.TryParse<CommentSort>(envelope.Extra, out var parsedSort))
            sort = parsedSort;

        return new CommentThreadsContinuation(envelope.Token, envelope.Target, sort, envelope.TrackingParams);
    }
}