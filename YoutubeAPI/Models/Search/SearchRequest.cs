using YoutubeAPI.Models.Enums;

namespace YoutubeAPI.Models.Search;

/// <summary>
///     Represents a query request for searching YouTube resources.
/// </summary>
/// <param name="Query">The search query text.</param>
/// <param name="Kind">The kind of resources to search for (defaults to <see cref="SearchKind.All" />).</param>
public sealed record SearchRequest(
    string Query,
    SearchKind Kind = SearchKind.All);