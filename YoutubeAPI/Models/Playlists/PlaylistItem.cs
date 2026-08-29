using YoutubeAPI.Models.ValueTypes;
using YoutubeAPI.Models.Videos;

namespace YoutubeAPI.Models.Playlists;

/// <summary>
///     Represents an occurrence of a video item within a YouTube playlist.
/// </summary>
/// <param name="Id">
///     The unique playlist item occurrence identifier (needed for removal), or <c>null</c> if
///     unavailable/unsupported.
/// </param>
/// <param name="Position">The zero-based or one-based playlist item index, or <c>null</c>.</param>
/// <param name="Video">The summary of the video at this position, or <c>null</c> if unavailable/deleted.</param>
/// <param name="DisplayTitle">The display title of the item (including placeholders for deleted/private items).</param>
/// <param name="IsAvailable">Whether the video is currently available and playable.</param>
public sealed record PlaylistItem(
    PlaylistItemId? Id,
    int? Position,
    VideoSummary? Video,
    string DisplayTitle,
    bool IsAvailable);