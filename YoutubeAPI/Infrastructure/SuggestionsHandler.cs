using System.Text.Json;
using YoutubeAPI.Exceptions;

namespace YoutubeAPI.Infrastructure;

internal sealed class SuggestionsHandler(InnerTubeSession session) : IYouTubeSuggestionsHandler
{
    public async Task<IReadOnlyList<string>> GetAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var options = session.Options;
        var url =
            $"https://suggestqueries-clients6.youtube.com/complete/search?client=firefox&hl={options.Language}&gl={options.Region}&q={Uri.EscapeDataString(query)}&ds=yt";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36");
        request.Headers.Accept.ParseAdd("application/json, text/javascript, */*");

        HttpResponseMessage response;
        try
        {
            response = await session.HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new YouTubeRequestException($"Failed to fetch suggestions for '{query}': {ex.Message}", "suggestions",
                null, ex);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new YouTubeRequestException($"Suggestions request failed with status {response.StatusCode}.",
                    "suggestions", response.StatusCode);

            var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return ParseSuggestions(text);
        }
    }

    private static List<string> ParseSuggestions(string jsonText)
    {
        if (string.IsNullOrWhiteSpace(jsonText))
            return [];

        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() >= 2)
            {
                var suggestionsArray = root[1];
                if (suggestionsArray.ValueKind == JsonValueKind.Array)
                {
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    return
                    [
                        .. suggestionsArray.EnumerateArray()
                            .Select(item => item.ValueKind switch
                            {
                                JsonValueKind.String => item.GetString(),
                                JsonValueKind.Array when item.GetArrayLength() > 0 &&
                                                         item[0].ValueKind == JsonValueKind.String => item[0]
                                    .GetString(),
                                _ => null
                            })
                            .Where(suggestion => !string.IsNullOrWhiteSpace(suggestion))
                            .Select(suggestion => suggestion!.Trim())
                            .Where(seen.Add)
                    ];
                }
            }
        }
        catch
        {
            // Fallback
        }

        return [];
    }
}