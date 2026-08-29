using System.Text.Json.Serialization;
using YoutubeAPI.Models.Channels;
using YoutubeAPI.Models.Playlists;
using YoutubeAPI.Models.Videos;

namespace YoutubeAPI.Models.Search;

/// <summary>
///     Abstract base record representing a polymorphic search result item.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(VideoSearchResult), "video")]
[JsonDerivedType(typeof(ChannelSearchResult), "channel")]
[JsonDerivedType(typeof(PlaylistSearchResult), "playlist")]
public abstract record SearchResult;

/// <summary>
///     Represents a video search result item.
/// </summary>
/// <param name="Video">The summary of the matched video.</param>
public sealed record VideoSearchResult(VideoSummary Video) : SearchResult;

/// <summary>
///     Represents a channel search result item.
/// </summary>
/// <param name="Channel">The summary of the matched channel.</param>
public sealed record ChannelSearchResult(ChannelSummary Channel) : SearchResult;

/// <summary>
///     Represents a playlist search result item.
/// </summary>
/// <param name="Playlist">The summary of the matched playlist.</param>
public sealed record PlaylistSearchResult(PlaylistSummary Playlist) : SearchResult;