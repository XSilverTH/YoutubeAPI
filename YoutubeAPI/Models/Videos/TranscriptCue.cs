namespace YoutubeAPI.Models.Videos;

/// <summary>
///     Represents a single timed subtitle/transcript segment.
/// </summary>
/// <param name="Text">The text content of the cue.</param>
/// <param name="Start">The start offset of the cue from the beginning of the video.</param>
/// <param name="Duration">The duration for which the cue is displayed.</param>
public sealed record TranscriptCue(
    string Text,
    TimeSpan Start,
    TimeSpan Duration);