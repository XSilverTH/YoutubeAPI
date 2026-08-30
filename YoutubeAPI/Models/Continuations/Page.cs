using System.Text.Json.Serialization;
using YoutubeAPI.Models.ValueTypes;
using YoutubeAPI.Models.Videos;

namespace YoutubeAPI.Models.Continuations;

/// <summary>
///     Represents a stateless, typed page of items with an optional route-specific continuation.
/// </summary>
/// <typeparam name="TItem">The type of item contained in the page.</typeparam>
/// <typeparam name="TContinuation">The route-specific continuation type.</typeparam>
/// <param name="Items">The items in the current page.</param>
/// <param name="Next">The continuation token for the next page, or <c>null</c> if this is the terminal page.</param>
public sealed record Page<TItem, TContinuation>(
    IReadOnlyList<TItem> Items,
    TContinuation? Next)
    where TContinuation : class
{
    /// <summary>
    ///     Gets playback state keyed by video ID when the response supplied it, or <c>null</c>.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<VideoId, VideoPlaybackProgress>? PlaybackProgress { get; init; }
}