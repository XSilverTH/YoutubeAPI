using System.Net;

namespace YoutubeAPI.Exceptions;

/// <summary>
///     Exception thrown when an HTTP or API request to YouTube fails.
/// </summary>
public class YouTubeRequestException : YouTubeException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="YouTubeRequestException" /> class.
    /// </summary>
    public YouTubeRequestException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="YouTubeRequestException" /> class with a specified error message,
    ///     operation, status code, and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="operation">The operation name during which the error occurred.</param>
    /// <param name="statusCode">The HTTP status code returned by the server, if applicable.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public YouTubeRequestException(string? message, string? operation = null, HttpStatusCode? statusCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Operation = operation;
        StatusCode = statusCode;
    }

    /// <summary>
    ///     Gets the operation name during which the error occurred.
    /// </summary>
    public string? Operation { get; }

    /// <summary>
    ///     Gets the HTTP status code returned by the server, if available.
    /// </summary>
    public HttpStatusCode? StatusCode { get; }
}