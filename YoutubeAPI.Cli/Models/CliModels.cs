using System.Text.Json.Serialization;
using YoutubeAPI.Models.Enums;
using YoutubeAPI.Models.ValueTypes;

namespace YoutubeAPI.Cli.Models;

/// <summary>
///     Output envelope for collection command pages in JSON format.
/// </summary>
public sealed record PageEnvelope<TItem>(
    [property: JsonPropertyName("items")] IReadOnlyList<TItem> Items,
    [property: JsonPropertyName("next")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    string? Next);

/// <summary>
///     Control line emitted at the end of NDJSON stream when more pages are available.
/// </summary>
public sealed record NdjsonContinuationControl(
    [property: JsonPropertyName("$continuation")]
    string Continuation);

/// <summary>
///     Error payload object.
/// </summary>
public sealed record CliError(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("message")]
    string Message);

/// <summary>
///     Error response envelope written to stderr.
/// </summary>
public sealed record CliErrorEnvelope(
    [property: JsonPropertyName("error")] CliError Error);

/// <summary>
///     Result of creating a new playlist.
/// </summary>
public sealed record PlaylistCreateResult(
    [property: JsonPropertyName("id")] PlaylistId Id);

/// <summary>
///     Result of modifying or deleting a playlist.
/// </summary>
public sealed record PlaylistActionResult(
    [property: JsonPropertyName("success")]
    bool Success,
    [property: JsonPropertyName("playlistId")]
    PlaylistId PlaylistId,
    [property: JsonPropertyName("videoId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    VideoId? VideoId = null,
    [property: JsonPropertyName("itemId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    PlaylistItemId? ItemId = null);

/// <summary>
///     Result of subscribing or unsubscribing from a channel.
/// </summary>
public sealed record AccountActionResult(
    [property: JsonPropertyName("success")]
    bool Success,
    [property: JsonPropertyName("channelId")]
    ChannelId ChannelId);

/// <summary>
///     Result of modifying or clearing history entries.
/// </summary>
public sealed record HistoryActionResult(
    [property: JsonPropertyName("success")]
    bool Success,
    [property: JsonPropertyName("entryId")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    HistoryEntryId? EntryId = null,
    [property: JsonPropertyName("cleared")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    bool? Cleared = null);

/// <summary>
///     Result of setting a video rating.
/// </summary>
public sealed record RatingActionResult(
    [property: JsonPropertyName("success")]
    bool Success,
    [property: JsonPropertyName("videoId")]
    VideoId VideoId,
    [property: JsonPropertyName("rating")] VideoRating Rating);

/// <summary>
///     Result of getting a video rating.
/// </summary>
public sealed record RatingGetResult(
    [property: JsonPropertyName("videoId")]
    VideoId VideoId,
    [property: JsonPropertyName("rating")] VideoRating Rating);