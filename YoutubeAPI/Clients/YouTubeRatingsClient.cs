using YoutubeAPI.Infrastructure;
using YoutubeAPI.Models.Enums;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Clients;

/// <summary>
///     Provides operations for querying and setting user ratings (like, dislike, none) on YouTube videos.
/// </summary>
public sealed class YouTubeRatingsClient
{
    private readonly IYouTubeRatingsHandler? _handler;

    internal YouTubeRatingsClient(IYouTubeRatingsHandler? handler = null)
    {
        _handler = handler;
    }

    /// <summary>
    ///     Gets the current rating (Like, Dislike, or None) given by the authenticated user to a video.
    /// </summary>
    /// <param name="videoId">The unique video identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding the user's current <see cref="VideoRating" /> for the video.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<VideoRating> GetAsync(VideoId videoId, CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Ratings handler is not configured.")
            : _handler.GetAsync(videoId, cancellationToken);
    }

    /// <summary>
    ///     Sets the rating (Like, Dislike, or None) for a video as the authenticated user.
    /// </summary>
    /// <param name="videoId">The unique video identifier.</param>
    /// <param name="rating">The rating to set.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous set rating operation.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task SetAsync(VideoId videoId, VideoRating rating, CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Ratings handler is not configured.")
            : _handler.SetAsync(videoId, rating, cancellationToken);
    }
}