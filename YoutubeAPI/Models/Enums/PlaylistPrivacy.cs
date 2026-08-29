namespace YoutubeAPI.Models.Enums;

/// <summary>
///     Specifies the privacy status of a YouTube playlist.
/// </summary>
public enum PlaylistPrivacy
{
    /// <summary>
    ///     The playlist is private and only accessible by the owner.
    /// </summary>
    Private,

    /// <summary>
    ///     The playlist is unlisted and accessible only with a direct link.
    /// </summary>
    Unlisted,

    /// <summary>
    ///     The playlist is public and discoverable by anyone.
    /// </summary>
    Public
}