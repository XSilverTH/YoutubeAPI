namespace YoutubeAPI.Models.Continuations;

/// <summary>
///     Represents an opaque continuation token for paginating playlists within a channel.
/// </summary>
public sealed class ChannelPlaylistsContinuation
{
    private const string RouteName = "channel_playlists";

    internal ChannelPlaylistsContinuation(string token, string? channel = null, string? trackingParams = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        Token = token;
        Channel = channel;
        TrackingParams = trackingParams;
    }

    /// <summary>
    ///     Gets the underlying server continuation token.
    /// </summary>
    public string Token { get; }

    /// <summary>
    ///     Gets the channel reference associated with this continuation, if known.
    /// </summary>
    public string? Channel { get; }

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
            Channel,
            null,
            TrackingParams);

        return envelope.Encode();
    }

    /// <summary>
    ///     Imports and validates a previously exported channel playlists continuation string.
    /// </summary>
    /// <param name="value">The exported continuation string.</param>
    /// <returns>A validated <see cref="ChannelPlaylistsContinuation" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> is malformed or belongs to a different route.</exception>
    public static ChannelPlaylistsContinuation Import(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var envelope = ContinuationEnvelope.Decode(value);
        return !string.Equals(envelope.Route, RouteName, StringComparison.Ordinal)
            ? throw new FormatException($"Invalid continuation route '{envelope.Route}'. Expected '{RouteName}'.")
            : new ChannelPlaylistsContinuation(envelope.Token, envelope.Target, envelope.TrackingParams);
    }
}