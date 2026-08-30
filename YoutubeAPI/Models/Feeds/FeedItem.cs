using System.Text.Json.Serialization;
using YoutubeAPI.Models.Channels;
using YoutubeAPI.Models.Playlists;
using YoutubeAPI.Models.Videos;

namespace YoutubeAPI.Models.Feeds;

/// <summary>
///     Abstract base record representing a polymorphic feed item.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(VideoFeedItem), "video")]
[JsonDerivedType(typeof(ChannelFeedItem), "channel")]
[JsonDerivedType(typeof(PlaylistFeedItem), "playlist")]
public abstract record FeedItem;

/// <summary>
///     Represents a video feed item.
/// </summary>
/// <param name="Video">The summary of the video item.</param>
public sealed record VideoFeedItem(VideoSummary Video) : FeedItem
{
    /// <summary>
    ///     Gets the authenticated viewer's playback state when supplied by this response, or <c>null</c>.
    /// </summary>
    public VideoPlaybackProgress? PlaybackProgress { get; init; }
}

/// <summary>
///     Represents a channel feed item.
/// </summary>
/// <param name="Channel">The summary of the channel item.</param>
public sealed record ChannelFeedItem(ChannelSummary Channel) : FeedItem;

/// <summary>
///     Represents a playlist feed item.
/// </summary>
/// <param name="Playlist">The summary of the playlist item.</param>
public sealed record PlaylistFeedItem(PlaylistSummary Playlist) : FeedItem;