namespace YoutubeAPI.Exceptions;

/// <summary>
///     Exception thrown when a YouTube resource exists but is unavailable (e.g. private, region-restricted, or removed).
/// </summary>
public class ResourceUnavailableException : YouTubeException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ResourceUnavailableException" /> class.
    /// </summary>
    public ResourceUnavailableException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ResourceUnavailableException" /> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ResourceUnavailableException(string? message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ResourceUnavailableException" /> class with a specified error message
    ///     and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ResourceUnavailableException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}