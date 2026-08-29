namespace YoutubeAPI.Exceptions;

/// <summary>
///     Exception thrown when comments are disabled or unavailable for a requested video.
/// </summary>
public class CommentsUnavailableException : YouTubeException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CommentsUnavailableException" /> class.
    /// </summary>
    public CommentsUnavailableException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CommentsUnavailableException" /> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public CommentsUnavailableException(string? message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CommentsUnavailableException" /> class with a specified error message
    ///     and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public CommentsUnavailableException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}