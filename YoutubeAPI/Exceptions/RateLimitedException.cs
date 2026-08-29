namespace YoutubeAPI.Exceptions;

/// <summary>
///     Exception thrown when YouTube returns a rate limit (429) or quota exceeded response.
/// </summary>
public class RateLimitedException : YouTubeException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RateLimitedException" /> class.
    /// </summary>
    public RateLimitedException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RateLimitedException" /> class with a specified error message and
    ///     optional retry duration.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="retryAfter">The duration to wait before retrying the request, if known.</param>
    public RateLimitedException(string? message, TimeSpan? retryAfter = null)
        : base(message)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RateLimitedException" /> class with a specified error message, inner
    ///     exception, and optional retry duration.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="retryAfter">The duration to wait before retrying the request, if known.</param>
    public RateLimitedException(string? message, Exception? innerException, TimeSpan? retryAfter = null)
        : base(message, innerException)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>
    ///     Gets the suggested duration to wait before retrying, if provided by the server.
    /// </summary>
    public TimeSpan? RetryAfter { get; }
}