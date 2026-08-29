using YoutubeAPI.Models.Continuations;

namespace YoutubeAPI.Models.Comments;

/// <summary>
///     Represents a top-level comment thread on a YouTube video, including initial replies and optional continuation.
/// </summary>
/// <param name="TopLevel">The top-level comment.</param>
/// <param name="ReplyCount">The total number of replies to this comment, or <c>null</c> if unavailable.</param>
/// <param name="Replies">The initial batch of reply comments loaded with the thread.</param>
/// <param name="NextReplies">The continuation token to load further replies, or <c>null</c> if no further replies exist.</param>
public sealed record CommentThread(
    Comment TopLevel,
    int? ReplyCount,
    IReadOnlyList<Comment> Replies,
    CommentRepliesContinuation? NextReplies);