using YoutubeAPI.Infrastructure;
using YoutubeAPI.Models.Continuations;
using YoutubeAPI.Models.Search;

namespace YoutubeAPI.Clients;

/// <summary>
///     Provides operations for querying and paginating YouTube search results.
/// </summary>
public sealed class YouTubeSearchClient
{
    private readonly IYouTubeSearchHandler? _handler;

    internal YouTubeSearchClient(IYouTubeSearchHandler? handler = null)
    {
        _handler = handler;
    }

    /// <summary>
    ///     Executes a search request and retrieves the first page of polymorphic search results.
    /// </summary>
    /// <param name="request">The search query and filtering parameters.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding the first page of search results with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<SearchResult, SearchContinuation>> GetPageAsync(SearchRequest request,
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Search handler is not configured.")
            : _handler.GetPageAsync(request, cancellationToken);
    }

    /// <summary>
    ///     Retrieves the next page of search results using a continuation token.
    /// </summary>
    /// <param name="continuation">The continuation token returned from a prior search page.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task yielding the next page of search results with an optional continuation token.</returns>
    /// <exception cref="NotSupportedException">Thrown when no underlying handler is configured.</exception>
    public Task<Page<SearchResult, SearchContinuation>> GetPageAsync(SearchContinuation continuation,
        CancellationToken cancellationToken = default)
    {
        return _handler == null
            ? throw new NotSupportedException("Search handler is not configured.")
            : _handler.GetPageAsync(continuation, cancellationToken);
    }
}