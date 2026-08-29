using YoutubeAPI.Models.Enums;

namespace YoutubeAPI.Models.Continuations;

/// <summary>
///     Represents an opaque continuation token for paginating search results.
/// </summary>
public sealed class SearchContinuation
{
    private const string RouteName = "search";

    internal SearchContinuation(string token, string? query = null, SearchKind kind = SearchKind.All,
        string? trackingParams = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        Token = token;
        Query = query;
        Kind = kind;
        TrackingParams = trackingParams;
    }

    /// <summary>
    ///     Gets the underlying server continuation token.
    /// </summary>
    public string Token { get; }

    /// <summary>
    ///     Gets the original search query associated with this continuation, if known.
    /// </summary>
    public string? Query { get; }

    /// <summary>
    ///     Gets the search kind filter applied to the original search.
    /// </summary>
    public SearchKind Kind { get; }

    /// <summary>
    ///     Gets the tracking parameter token, if available.
    /// </summary>
    public string? TrackingParams { get; }

    /// <summary>
    ///     Exports the continuation state into a durable, URL-safe base64 string.
    /// </summary>
    /// <returns>An opaque continuation string.</returns>
    public string Export()
    {
        var envelope = new ContinuationEnvelope(
            ContinuationEnvelope.CurrentVersion,
            RouteName,
            Token,
            Query,
            null,
            TrackingParams,
            Kind.ToString());

        return envelope.Encode();
    }

    /// <summary>
    ///     Imports and validates a previously exported search continuation string.
    /// </summary>
    /// <param name="value">The exported continuation string.</param>
    /// <returns>A validated <see cref="SearchContinuation" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> is malformed or belongs to a different route.</exception>
    public static SearchContinuation Import(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var envelope = ContinuationEnvelope.Decode(value);
        if (!string.Equals(envelope.Route, RouteName, StringComparison.Ordinal))
            throw new FormatException($"Invalid continuation route '{envelope.Route}'. Expected '{RouteName}'.");

        if (string.IsNullOrWhiteSpace(envelope.Target))
            throw new FormatException("Search continuation envelope is missing or empty search query.");

        if (string.IsNullOrWhiteSpace(envelope.Extra) ||
            !Enum.TryParse<SearchKind>(envelope.Extra, true, out var kind) ||
            !Enum.IsDefined(kind))
            throw new FormatException($"Search continuation envelope has invalid search kind '{envelope.Extra}'.");

        return new SearchContinuation(envelope.Token, envelope.Target, kind, envelope.TrackingParams);
    }
}