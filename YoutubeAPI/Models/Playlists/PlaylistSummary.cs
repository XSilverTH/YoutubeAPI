using YoutubeAPI.Models.Channels;
using YoutubeAPI.Models.Common;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Models.Playlists;

/// <summary>
///     Represents a concise summary of a YouTube playlist.
/// </summary>
/// <param name="Id">The unique playlist identifier.</param>
/// <param name="Title">The title of the playlist.</param>
/// <param name="Url">The canonical URL to the playlist page.</param>
/// <param name="Author">The summary of the authoring channel, or <c>null</c> if unknown.</param>
/// <param name="ItemCount">The total number of items in the playlist, or <c>null</c> if unavailable.</param>
/// <param name="Thumbnails">The available thumbnail images for the playlist.</param>
public sealed record PlaylistSummary(
    PlaylistId Id,
    string Title,
    Uri Url,
    ChannelSummary? Author,
    int? ItemCount,
    IReadOnlyList<Thumbnail> Thumbnails);