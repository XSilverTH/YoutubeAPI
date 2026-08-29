using YoutubeAPI.Infrastructure;
using YoutubeAPI.Models.ValueTypes;
using YoutubeAPI.Models.Videos;

namespace YoutubeAPI.Clients;

/// <summary>
///     Provides access to YouTube video metadata, details, and transcript operations.
/// </summary>
public sealed class YouTubeVideosClient
{
    private readonly IYouTubeVideosHandler? _handler;

    internal YouTubeVideosClient(IYouTubeVideosHandler? handler = null)
    {
        _handler = handler;
    }

    /// <summary>
    ///     Gets full metadata and details for a YouTube video by ID.
    /// </summary>
    /// <param name="videoId">The unique video identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation that yields the full <see cref="Video" /> metadata.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Video> GetAsync(VideoId videoId, CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Videos handler is not configured.")
            : _handler.GetAsync(videoId, cancellationToken);
    }

    /// <summary>
    ///     Gets the list of available transcript/caption tracks for a YouTube video.
    /// </summary>
    /// <param name="videoId">The unique video identifier.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation that yields the list of <see cref="TranscriptTrack" /> items.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<IReadOnlyList<TranscriptTrack>> GetTranscriptTracksAsync(VideoId videoId,
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Videos handler is not configured.")
            : _handler.GetTranscriptTracksAsync(videoId, cancellationToken);
    }

    /// <summary>
    ///     Gets the timed transcript cues for a specific transcript track on a YouTube video.
    /// </summary>
    /// <param name="videoId">The unique video identifier.</param>
    /// <param name="trackId">The identifier of the transcript track to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation that yields the <see cref="Transcript" />.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Transcript> GetTranscriptAsync(VideoId videoId, TranscriptTrackId trackId,
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Videos handler is not configured.")
            : _handler.GetTranscriptAsync(videoId, trackId, cancellationToken);
    }
}