namespace YoutubeAPI;

/// <summary>
///     Options for configuring a <see cref="YouTubeClient" /> instance.
/// </summary>
public sealed class YouTubeClientOptions
{
    /// <summary>
    ///     Gets the language code (e.g. "en") for YouTube requests. Defaults to "en".
    /// </summary>
    public string Language { get; init; } = "en";

    /// <summary>
    ///     Gets the region/country code (e.g. "US") for YouTube requests. Defaults to "US".
    /// </summary>
    public string Region { get; init; } = "US";

    /// <summary>
    ///     Gets the authentication cookie credentials, or <c>null</c> for unauthenticated operations.
    /// </summary>
    public YouTubeCookieAuthentication? Authentication { get; init; }

    /// <summary>
    ///     Gets the optional visitor data string.
    /// </summary>
    public string? VisitorData { get; init; }

    /// <summary>
    ///     Gets the optional rollout token string.
    /// </summary>
    public string? RolloutToken { get; init; }

    /// <summary>
    ///     Gets the optional Proof-of-Origin token string.
    /// </summary>
    public string? ProofOfOriginToken { get; init; }

    /// <summary>
    ///     Gets the auth user index (default 0).
    /// </summary>
    public int AuthUser { get; init; }

    /// <summary>
    ///     Gets the brand account page ID, if applicable.
    /// </summary>
    public string? PageId { get; init; }

    /// <summary>
    ///     Gets the time provider used for timestamp calculations. Defaults to <see cref="TimeProvider.System" />.
    /// </summary>
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;
}