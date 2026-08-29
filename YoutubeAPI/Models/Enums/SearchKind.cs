namespace YoutubeAPI.Models.Enums;

/// <summary>
///     Specifies the type of resources to search for.
/// </summary>
public enum SearchKind
{
    /// <summary>
    ///     Search for all supported resource types (videos, channels, playlists).
    /// </summary>
    All,

    /// <summary>
    ///     Search for videos only.
    /// </summary>
    Video,

    /// <summary>
    ///     Search for channels only.
    /// </summary>
    Channel,

    /// <summary>
    ///     Search for playlists only.
    /// </summary>
    Playlist
}