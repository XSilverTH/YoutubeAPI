namespace YoutubeAPI.Models.Enums;

/// <summary>
///     Specifies the live broadcast state of a video.
/// </summary>
public enum LiveBroadcastState
{
    /// <summary>
    ///     Not a live broadcast (standard on-demand video).
    /// </summary>
    None,

    /// <summary>
    ///     An upcoming scheduled live stream or premiere.
    /// </summary>
    Upcoming,

    /// <summary>
    ///     Currently broadcasting live.
    /// </summary>
    Live,

    /// <summary>
    ///     A completed live stream recording.
    /// </summary>
    Ended
}