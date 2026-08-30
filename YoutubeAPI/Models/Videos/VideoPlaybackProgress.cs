namespace YoutubeAPI.Models.Videos;

/// <summary>
///     Represents the authenticated viewer's playback state when YouTube includes it in a response.
/// </summary>
/// <param name="WatchedFraction">
///     The fraction of the video watched for display purposes, in the inclusive range 0..1, or <c>null</c>
///     when YouTube did not provide display progress.
/// </param>
/// <param name="ResumePosition">
///     The separately saved playback position used for resuming, or <c>null</c> when no position was provided.
/// </param>
/// <param name="IsCompleted">
///     Whether YouTube marks the video as completed. Completion is distinct from a saved resume position.
/// </param>
public sealed record VideoPlaybackProgress(
    double? WatchedFraction,
    TimeSpan? ResumePosition,
    bool IsCompleted)
{
    /// <summary>
    ///     Gets whether YouTube supplied a display watch-progress value.
    /// </summary>
    public bool HasProgress => WatchedFraction.HasValue;

    /// <summary>
    ///     Gets the display progress as a percentage, or <c>null</c> when unavailable.
    /// </summary>
    public double? WatchedPercentage => WatchedFraction * 100;

    /// <summary>
    ///     Gets whether YouTube supplied a separate saved resume position.
    /// </summary>
    public bool HasResumePosition => ResumePosition.HasValue;
}
