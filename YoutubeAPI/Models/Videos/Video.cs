using YoutubeAPI.Models.Enums;

namespace YoutubeAPI.Models.Videos;

/// <summary>
///     Represents full metadata for a YouTube video.
/// </summary>
/// <param name="Summary">The core summary information for the video.</param>
/// <param name="Description">The full description text of the video.</param>
/// <param name="Keywords">The list of keyword tags associated with the video.</param>
/// <param name="UploadDate">The upload date of the video, or <c>null</c> if unavailable.</param>
/// <param name="LiveState">The live broadcast state (e.g. None, Upcoming, Live, Ended).</param>
public sealed record Video(
    VideoSummary Summary,
    string Description,
    IReadOnlyList<string> Keywords,
    DateOnly? UploadDate,
    LiveBroadcastState LiveState);