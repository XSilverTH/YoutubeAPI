using YoutubeAPI.Models.Enums;

namespace YoutubeAPI.Models.Continuations;

/// <summary>
///     Represents an opaque continuation token for paginating videos within a channel.
/// </summary>
public sealed class ChannelVideosContinuation
{
    private const string RouteName = "channel_videos";

    internal ChannelVideosContinuation(string token, string? channel = null,
        ChannelVideoSort sort = ChannelVideoSort.Newest, string? trackingParams = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        Token = token;
        Channel = channel;
        Sort = sort;
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
    ///     Gets the sort order applied to the channel video listing.
    /// </summary>
    public ChannelVideoSort Sort { get; }

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
            TrackingParams,
            Sort.ToString());

        return envelope.Encode();
    }

    /// <summary>
    ///     Imports and validates a previously exported channel videos continuation string.
    /// </summary>
    /// <param name="value">The exported continuation string.</param>
    /// <returns>A validated <see cref="ChannelVideosContinuation" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> is malformed or belongs to a different route.</exception>
    public static ChannelVideosContinuation Import(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var envelope = ContinuationEnvelope.Decode(value);
        if (!string.Equals(envelope.Route, RouteName, StringComparison.Ordinal))
            throw new FormatException($"Invalid continuation route '{envelope.Route}'. Expected '{RouteName}'.");

        var sort = ChannelVideoSort.Newest;
        if (!string.IsNullOrEmpty(envelope.Extra) &&
            Enum.TryParse<ChannelVideoSort>(envelope.Extra, out var parsedSort)) sort = parsedSort;

        return new ChannelVideosContinuation(envelope.Token, envelope.Target, sort, envelope.TrackingParams);
    }
}