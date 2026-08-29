namespace YoutubeAPI.Exceptions;

/// <summary>
///     Exception thrown when the authenticated user does not have permission to access or modify a resource.
/// </summary>
public class PermissionDeniedException : YouTubeException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PermissionDeniedException" /> class.
    /// </summary>
    public PermissionDeniedException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PermissionDeniedException" /> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public PermissionDeniedException(string? message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PermissionDeniedException" /> class with a specified error message and
    ///     inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public PermissionDeniedException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}