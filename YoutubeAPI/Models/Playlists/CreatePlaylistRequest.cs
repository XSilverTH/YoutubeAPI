using YoutubeAPI.Models.Enums;

namespace YoutubeAPI.Models.Playlists;

/// <summary>
///     Represents a request to create a new YouTube playlist.
/// </summary>
/// <param name="Title">The title of the new playlist.</param>
/// <param name="Description">The optional description text of the playlist.</param>
/// <param name="Privacy">The privacy setting for the playlist (defaults to <see cref="PlaylistPrivacy.Private" />).</param>
public sealed record CreatePlaylistRequest(
    string Title,
    string? Description = null,
    PlaylistPrivacy Privacy = PlaylistPrivacy.Private);