namespace YoutubeAPI.Models.Continuations;

/// <summary>
///     Represents an opaque continuation token for paginating items within a playlist.
/// </summary>
public sealed class PlaylistItemsContinuation
{
    private const string RouteName = "playlist_items";

    internal PlaylistItemsContinuation(string token, string? playlistId = null, string? trackingParams = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        Token = token;
        PlaylistId = playlistId;
        TrackingParams = trackingParams;
    }

    /// <summary>
    ///     Gets the underlying server continuation token.
    /// </summary>
    public string Token { get; }

    /// <summary>
    ///     Gets the playlist identifier associated with this continuation, if known.
    /// </summary>
    public string? PlaylistId { get; }

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
            PlaylistId,
            null,
            TrackingParams);

        return envelope.Encode();
    }

    /// <summary>
    ///     Imports and validates a previously exported playlist items continuation string.
    /// </summary>
    /// <param name="value">The exported continuation string.</param>
    /// <returns>A validated <see cref="PlaylistItemsContinuation" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> is malformed or belongs to a different route.</exception>
    public static PlaylistItemsContinuation Import(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var envelope = ContinuationEnvelope.Decode(value);
        return !string.Equals(envelope.Route, RouteName, StringComparison.Ordinal)
            ? throw new FormatException($"Invalid continuation route '{envelope.Route}'. Expected '{RouteName}'.")
            : new PlaylistItemsContinuation(envelope.Token, envelope.Target, envelope.TrackingParams);
    }
}