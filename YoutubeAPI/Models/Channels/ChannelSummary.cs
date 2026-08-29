using YoutubeAPI.Models.Common;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Models.Channels;

/// <summary>
///     Represents a concise summary of a YouTube channel.
/// </summary>
/// <param name="Id">The unique channel identifier.</param>
/// <param name="Title">The title/name of the channel.</param>
/// <param name="Handle">The channel handle (e.g. "@username"), or <c>null</c> if not set.</param>
/// <param name="Url">The canonical URL to the channel page.</param>
/// <param name="Thumbnails">The available channel avatars/thumbnails.</param>
/// <param name="IsVerified">Whether the channel has a verification badge.</param>
/// <param name="SubscriberCount">The approximate subscriber count, or <c>null</c> if hidden.</param>
public sealed record ChannelSummary(
    ChannelId Id,
    string Title,
    string? Handle,
    Uri Url,
    IReadOnlyList<Thumbnail> Thumbnails,
    bool IsVerified,
    long? SubscriberCount);