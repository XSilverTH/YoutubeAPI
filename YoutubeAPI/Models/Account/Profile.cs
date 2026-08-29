using YoutubeAPI.Models.Common;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Models.Account;

/// <summary>
///     Represents the authenticated user's account profile information.
/// </summary>
/// <param name="ChannelId">The primary channel identifier associated with the account, or <c>null</c>.</param>
/// <param name="DisplayName">The user's display name.</param>
/// <param name="Handle">The user's channel handle (e.g. "@username"), or <c>null</c>.</param>
/// <param name="Avatar">The user's profile avatar thumbnail, or <c>null</c>.</param>
public sealed record Profile(
    ChannelId? ChannelId,
    string DisplayName,
    string? Handle,
    Thumbnail? Avatar);