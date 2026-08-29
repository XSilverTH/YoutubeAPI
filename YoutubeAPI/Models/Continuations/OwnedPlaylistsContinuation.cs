namespace YoutubeAPI.Models.Continuations;

/// <summary>
///     Represents an opaque continuation token for paginating the authenticated user's owned playlists.
/// </summary>
public sealed class OwnedPlaylistsContinuation
{
    private const string RouteName = "owned_playlists";

    internal OwnedPlaylistsContinuation(string token, string? profileId = null, string? trackingParams = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        Token = token;
        ProfileId = profileId;
        TrackingParams = trackingParams;
    }

    /// <summary>
    ///     Gets the underlying server continuation token.
    /// </summary>
    public string Token { get; }

    /// <summary>
    ///     Gets the resolved profile identifier bound to this continuation, if known.
    /// </summary>
    public string? ProfileId { get; }

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
            null,
            ProfileId,
            TrackingParams);

        return envelope.Encode();
    }

    /// <summary>
    ///     Imports and validates a previously exported owned playlists continuation string.
    /// </summary>
    /// <param name="value">The exported continuation string.</param>
    /// <returns>A validated <see cref="OwnedPlaylistsContinuation" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> is malformed or belongs to a different route.</exception>
    public static OwnedPlaylistsContinuation Import(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var envelope = ContinuationEnvelope.Decode(value);
        return !string.Equals(envelope.Route, RouteName, StringComparison.Ordinal)
            ? throw new FormatException($"Invalid continuation route '{envelope.Route}'. Expected '{RouteName}'.")
            : new OwnedPlaylistsContinuation(envelope.Token, envelope.ProfileId, envelope.TrackingParams);
    }
}