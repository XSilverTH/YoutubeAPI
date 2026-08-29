using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Models.Comments;

/// <summary>
///     Represents a comment on a YouTube video.
/// </summary>
/// <param name="Id">The unique comment identifier.</param>
/// <param name="Author">The author of the comment.</param>
/// <param name="Text">The raw or formatted comment text.</param>
/// <param name="PublishedText">The human-readable relative timestamp text (e.g. "2 hours ago").</param>
/// <param name="PublishedAt">The approximate publication timestamp, or <c>null</c>.</param>
/// <param name="LikeCount">The number of likes on the comment, or <c>null</c>.</param>
/// <param name="IsPinned">Whether the comment is pinned by the creator.</param>
/// <param name="IsHearted">Whether the comment has received a heart from the creator.</param>
/// <param name="IsEdited">Whether the comment has been edited.</param>
public sealed record Comment(
    CommentId Id,
    CommentAuthor Author,
    string Text,
    string PublishedText,
    DateTimeOffset? PublishedAt,
    long? LikeCount,
    bool IsPinned,
    bool IsHearted,
    bool IsEdited);