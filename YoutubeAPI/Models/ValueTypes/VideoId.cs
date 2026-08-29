using System.Diagnostics.CodeAnalysis;
using YoutubeAPI.Infrastructure;

namespace YoutubeAPI.Models.ValueTypes;

/// <summary>
///     Represents an immutable 11-character YouTube video identifier.
/// </summary>
public readonly record struct VideoId
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="VideoId" /> struct with a canonical video identifier string.
    /// </summary>
    /// <param name="value">The 11-character video identifier.</param>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid 11-character video ID.</exception>
    public VideoId(string value)
    {
        if (!TryParse(value, out var parsed))
            throw new ArgumentException($"'{value}' is not a valid YouTube video identifier or supported URL.",
                nameof(value));

        Value = parsed.Value;
    }

    private VideoId(string value, bool validated)
    {
        _ = validated;
        Value = value;
    }

    /// <summary>
    ///     Gets the canonical 11-character video identifier string.
    /// </summary>
    public string Value => field ?? string.Empty;

    /// <summary>
    ///     Parses a raw video ID string or supported YouTube URL into a <see cref="VideoId" />.
    /// </summary>
    /// <param name="value">The video ID string or URL to parse.</param>
    /// <returns>A new <see cref="VideoId" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> is not a valid video ID or supported URL.</exception>
    public static VideoId Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return !TryParse(value, out var result)
            ? throw new FormatException($"'{value}' is not a valid YouTube video identifier or supported URL.")
            : result;
    }

    /// <summary>
    ///     Tries to parse a raw video ID string or supported YouTube URL into a <see cref="VideoId" />.
    /// </summary>
    /// <param name="value">The video ID string or URL to parse.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="VideoId" /> if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? value, out VideoId result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();

        // 1. Try parsing as URL
        if (YouTubeUrlParser.TryParseUri(trimmed, out var uri))
        {
            if (!TryExtractIdFromUri(uri, out var urlId)) return false;
            result = new VideoId(urlId, true);
            return true;
        }

        // 2. Try parsing as raw 11-char ID
        if (!IsValidRawId(trimmed)) return false;
        result = new VideoId(trimmed, true);
        return true;
    }

    /// <summary>Parses a video identifier from a character span.</summary>
    public static VideoId Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return Parse(s.ToString());
    }

    /// <summary>Tries to parse a video identifier from a character span.</summary>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out VideoId result)
    {
        return TryParse(s.ToString(), out result);
    }

    private static bool TryExtractIdFromUri(Uri uri, [NotNullWhen(true)] out string? id)
    {
        id = null;

        // youtu.be/<id>
        if (uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith(".youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 1 || !IsValidRawId(segments[0])) return false;
            id = segments[0];
            return true;
        }

        // Check query string ?v=<id>
        var queryParams = YouTubeUrlParser.ParseQueryString(uri.Query);
        if (queryParams.TryGetValue("v", out var v) && IsValidRawId(v))
        {
            id = v;
            return true;
        }

        // Path formats: /embed/<id>, /v/<id>, /shorts/<id>, /live/<id>, /clip/<id>, /clips/<id>
        var pathSegments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments.Length < 2) return false;
        var prefix = pathSegments[0].ToLowerInvariant();
        if (prefix is not ("embed" or "v" or "shorts" or "live" or "clip" or "clips")) return false;
        if (!IsValidRawId(pathSegments[1])) return false;
        id = pathSegments[1];
        return true;
    }

    private static bool IsValidRawId(string s)
    {
        return s.Length == 11 && s.All(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_');
    }

    /// <summary>Returns the canonical video identifier.</summary>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>Compares this video identifier with another identifier.</summary>
    public int CompareTo(VideoId other)
    {
        return string.Compare(Value, other.Value, StringComparison.Ordinal);
    }

    /// <summary>Compares this video identifier with another object.</summary>
    public int CompareTo(object? obj)
    {
        return obj switch
        {
            null => 1,
            VideoId other => CompareTo(other),
            _ => throw new ArgumentException($"Object must be of type {nameof(VideoId)}", nameof(obj))
        };
    }

    /// <summary>
    ///     Implicitly converts a <see cref="VideoId" /> to its string representation.
    /// </summary>
    /// <param name="id">The <see cref="VideoId" /> instance.</param>
    public static implicit operator string(VideoId id)
    {
        return id.Value;
    }

    /// <summary>
    ///     Explicitly converts a string to a <see cref="VideoId" />.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    public static explicit operator VideoId(string value)
    {
        return Parse(value);
    }
}