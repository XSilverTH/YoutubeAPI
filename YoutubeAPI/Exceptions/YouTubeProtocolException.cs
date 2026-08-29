namespace YoutubeAPI.Exceptions;

/// <summary>
///     Exception thrown when YouTube returns an unrecognized, malformed, or unexpected protocol payload or response
///     structure.
/// </summary>
public class YouTubeProtocolException : YouTubeException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="YouTubeProtocolException" /> class.
    /// </summary>
    public YouTubeProtocolException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="YouTubeProtocolException" /> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public YouTubeProtocolException(string? message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="YouTubeProtocolException" /> class with a specified error message and
    ///     inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public YouTubeProtocolException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}