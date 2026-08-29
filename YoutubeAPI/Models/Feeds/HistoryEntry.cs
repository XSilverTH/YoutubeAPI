using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Models.Feeds;

/// <summary>
///     Represents a user watch history entry.
/// </summary>
/// <param name="Id">The unique identifier of the history entry (needed for removal).</param>
/// <param name="Item">The feed item content (e.g. video) corresponding to this history entry.</param>
public sealed record HistoryEntry(
    HistoryEntryId Id,
    FeedItem Item);