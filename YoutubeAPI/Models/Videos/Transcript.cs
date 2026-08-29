using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Models.Videos;

/// <summary>
///     Represents the complete transcript for a video track.
/// </summary>
/// <param name="VideoId">The video identifier.</param>
/// <param name="Track">The metadata of the transcript track.</param>
/// <param name="Cues">The ordered sequence of timed transcript cues.</param>
public sealed record Transcript(
    VideoId VideoId,
    TranscriptTrack Track,
    IReadOnlyList<TranscriptCue> Cues);