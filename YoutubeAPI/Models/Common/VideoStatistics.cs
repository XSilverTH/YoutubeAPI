namespace YoutubeAPI.Models.Common;

/// <summary>
///     Represents public engagement statistics for a YouTube video.
/// </summary>
/// <param name="ViewCount">The total number of views, or <c>null</c> if hidden/unavailable.</param>
/// <param name="LikeCount">The total number of likes, or <c>null</c> if hidden/unavailable.</param>
/// <param name="CommentCount">The total number of comments, or <c>null</c> if hidden/unavailable.</param>
public sealed record VideoStatistics(
    long? ViewCount,
    long? LikeCount,
    long? CommentCount);