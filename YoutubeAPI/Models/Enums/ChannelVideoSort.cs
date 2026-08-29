namespace YoutubeAPI.Models.Enums;

/// <summary>
///     Specifies the sorting order for videos within a channel.
/// </summary>
public enum ChannelVideoSort
{
    /// <summary>
    ///     Sort by newest uploaded videos first.
    /// </summary>
    Newest,

    /// <summary>
    ///     Sort by most popular videos first.
    /// </summary>
    Popular,

    /// <summary>
    ///     Sort by oldest uploaded videos first.
    /// </summary>
    Oldest
}