using YoutubeAPI.Infrastructure;

namespace YoutubeAPI.Clients;

/// <summary>
///     Provides access to YouTube search query auto-completion suggestions.
/// </summary>
public sealed class YouTubeSuggestionsClient
{
    private readonly IYouTubeSuggestionsHandler? _handler;

    internal YouTubeSuggestionsClient(IYouTubeSuggestionsHandler? handler = null)
    {
        _handler = handler;
    }

    /// <summary>
    ///     Retrieves search auto-completion suggestions for the given query prefix.
    /// </summary>
    /// <param name="query">The search text prefix.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding the ordered list of suggested query completions.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<IReadOnlyList<string>> GetAsync(string query, CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Suggestions handler is not configured.")
            : _handler.GetAsync(query, cancellationToken);
    }
}