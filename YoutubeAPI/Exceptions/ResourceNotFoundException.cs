namespace YoutubeAPI.Exceptions;

/// <summary>
///     Exception thrown when a requested YouTube resource (e.g. video, channel, playlist) was not found.
/// </summary>
public class ResourceNotFoundException : YouTubeException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ResourceNotFoundException" /> class.
    /// </summary>
    public ResourceNotFoundException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ResourceNotFoundException" /> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ResourceNotFoundException(string? message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ResourceNotFoundException" /> class with a specified error message and
    ///     inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ResourceNotFoundException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}