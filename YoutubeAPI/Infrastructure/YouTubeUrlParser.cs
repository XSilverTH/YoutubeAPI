using System.Diagnostics.CodeAnalysis;

namespace YoutubeAPI.Infrastructure;

internal static class YouTubeUrlParser
{
    private static readonly HashSet<string> ValidExactHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "youtube.com",
        "www.youtube.com",
        "m.youtube.com",
        "music.youtube.com",
        "gaming.youtube.com",
        "tv.youtube.com",
        "youtu.be",
        "www.youtu.be"
    };

    public static bool IsValidYouTubeHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        if (ValidExactHosts.Contains(host))
            return true;

        if (host.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase))
        {
            var prefix = host[..^".youtube.com".Length];
            if (!prefix.Contains('@') && !prefix.Contains('/') && !prefix.Contains('\\'))
                return true;
        }

        if (host.EndsWith(".youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            var prefix = host[..^".youtu.be".Length];
            if (!prefix.Contains('@') && !prefix.Contains('/') && !prefix.Contains('\\'))
                return true;
        }

        return false;
    }

    public static bool TryParseUri(string input, [NotNullWhen(true)] out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var trimmed = input.Trim();
        if (!trimmed.Contains("://", StringComparison.Ordinal))
        {
            if (trimmed.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("youtube.", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("m.youtube.", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("music.youtube.", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("youtu.be/", StringComparison.OrdinalIgnoreCase))
                trimmed = "https://" + trimmed;
            else
                return false;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                uri = null;
                return false;
            }

            if (IsValidYouTubeHost(uri.Host))
                return true;
        }

        uri = null;
        return false;
    }

    public static Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query))
            return result;

        var span = query.AsSpan();
        if (span.StartsWith("?"))
            span = span[1..];

        while (!span.IsEmpty)
        {
            var ampersandIndex = span.IndexOf('&');
            var segment = ampersandIndex >= 0 ? span[..ampersandIndex] : span;
            span = ampersandIndex >= 0 ? span[(ampersandIndex + 1)..] : ReadOnlySpan<char>.Empty;

            var equalsIndex = segment.IndexOf('=');
            if (equalsIndex >= 0)
            {
                var key = Uri.UnescapeDataString(segment[..equalsIndex].ToString());
                var value = Uri.UnescapeDataString(segment[(equalsIndex + 1)..].ToString());
                result[key] = value;
            }
            else
            {
                var key = Uri.UnescapeDataString(segment.ToString());
                result[key] = string.Empty;
            }
        }

        return result;
    }
}