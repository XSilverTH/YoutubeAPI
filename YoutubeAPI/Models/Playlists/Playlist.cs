using YoutubeAPI.Models.Enums;

namespace YoutubeAPI.Models.Playlists;

/// <summary>
///     Represents full metadata for a YouTube playlist.
/// </summary>
/// <param name="Summary">The core summary information for the playlist.</param>
/// <param name="Description">The full description text of the playlist, or <c>null</c>.</param>
/// <param name="Privacy">The privacy status of the playlist, or <c>null</c> if unknown.</param>
public sealed record Playlist(
    PlaylistSummary Summary,
    string? Description,
    PlaylistPrivacy? Privacy);