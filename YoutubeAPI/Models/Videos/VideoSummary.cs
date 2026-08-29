using YoutubeAPI.Models.Channels;
using YoutubeAPI.Models.Common;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Models.Videos;

/// <summary>
///     Represents a concise summary of a YouTube video.
/// </summary>
/// <param name="Id">The unique 11-character video identifier.</param>
/// <param name="Title">The title of the video.</param>
/// <param name="Channel">The authoring channel summary.</param>
/// <param name="Duration">The duration of the video, or <c>null</c> for active live streams.</param>
/// <param name="Url">The canonical URL to the watch page.</param>
/// <param name="Thumbnails">The available thumbnail images across resolutions.</param>
/// <param name="PublishedText">The human-readable relative publication text (e.g. "3 days ago"), or <c>null</c>.</param>
/// <param name="PublishedAt">The approximate or exact publication timestamp, or <c>null</c>.</param>
/// <param name="IsShort">Whether the video is categorized as a YouTube Short.</param>
/// <param name="Statistics">The public view, like, and comment statistics.</param>
public sealed record VideoSummary(
    VideoId Id,
    string Title,
    ChannelSummary Channel,
    TimeSpan? Duration,
    Uri Url,
    IReadOnlyList<Thumbnail> Thumbnails,
    string? PublishedText,
    DateTimeOffset? PublishedAt,
    bool IsShort,
    VideoStatistics Statistics);