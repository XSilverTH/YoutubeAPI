using YoutubeAPI.Models.Common;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Models.Comments;

/// <summary>
///     Represents the author of a YouTube comment.
/// </summary>
/// <param name="ChannelId">The author's channel identifier, or <c>null</c> if unavailable.</param>
/// <param name="Name">The display name of the author.</param>
/// <param name="ChannelUrl">The URL to the author's channel page, or <c>null</c>.</param>
/// <param name="Avatar">The avatar thumbnail of the author, or <c>null</c>.</param>
public sealed record CommentAuthor(
    ChannelId? ChannelId,
    string Name,
    Uri? ChannelUrl,
    Thumbnail? Avatar);