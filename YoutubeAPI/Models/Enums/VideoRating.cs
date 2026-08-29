namespace YoutubeAPI.Models.Enums;

/// <summary>
///     Specifies the user rating for a YouTube video.
/// </summary>
public enum VideoRating
{
    /// <summary>
    ///     No rating given (or rating removed).
    /// </summary>
    None,

    /// <summary>
    ///     The video is liked.
    /// </summary>
    Like,

    /// <summary>
    ///     The video is disliked.
    /// </summary>
    Dislike
}