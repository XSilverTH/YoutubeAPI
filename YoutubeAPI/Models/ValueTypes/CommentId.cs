using System.Diagnostics.CodeAnalysis;
using YoutubeAPI.Infrastructure;

namespace YoutubeAPI.Models.ValueTypes;

/// <summary>
///     Represents an immutable YouTube comment or reply identifier.
/// </summary>
public readonly record struct CommentId
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="CommentId" /> struct.
    /// </summary>
    /// <param name="value">The comment identifier or supported URL with lc parameter.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not a valid comment ID.</exception>
    public CommentId(string value)
    {
        if (!TryParse(value, out var parsed))
            throw new ArgumentException($"'{value}' is not a valid YouTube comment identifier or supported URL.",
                nameof(value));

        Value = parsed.Value;
    }

    private CommentId(string value, bool validated)
    {
        _ = validated;
        Value = value;
    }

    /// <summary>
    ///     Gets the canonical comment identifier string.
    /// </summary>
    public string Value => field ?? string.Empty;

    /// <summary>
    ///     Parses a comment ID string or supported YouTube URL into a <see cref="CommentId" />.
    /// </summary>
    /// <param name="value">The comment ID string or URL to parse.</param>
    /// <returns>A new <see cref="CommentId" /> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is null.</exception>
    /// <exception cref="FormatException">Thrown when <paramref name="value" /> cannot be parsed as a comment ID.</exception>
    public static CommentId Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return !TryParse(value, out var result)
            ? throw new FormatException($"'{value}' is not a valid YouTube comment identifier or supported URL.")
            : result;
    }

    /// <summary>
    ///     Tries to parse a comment ID string or supported YouTube URL into a <see cref="CommentId" />.
    /// </summary>
    /// <param name="value">The comment ID string or URL to parse.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="CommentId" /> if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? value, out CommentId result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim();

        // 1. Try parsing as URL with ?lc=<commentId>
        if (YouTubeUrlParser.TryParseUri(trimmed, out var uri))
        {
            var queryParams = YouTubeUrlParser.ParseQueryString(uri.Query);
            if (!queryParams.TryGetValue("lc", out var lc) || string.IsNullOrWhiteSpace(lc)) return false;
            result = new CommentId(lc, true);
            return true;
        }

        // 2. Raw comment ID
        if (trimmed.Contains(' ') || trimmed.Contains('/') || trimmed.Contains('?')) return false;
        result = new CommentId(trimmed, true);
        return true;
    }

    /// <summary>Parses a comment identifier from a character span.</summary>
    public static CommentId Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        return Parse(s.ToString());
    }

    /// <summary>Tries to parse a comment identifier from a character span.</summary>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out CommentId result)
    {
        return TryParse(s.ToString(), out result);
    }

    /// <summary>Returns the canonical comment identifier.</summary>
    public override string ToString()
    {
        return Value;
    }

    /// <summary>Compares this comment identifier with another identifier.</summary>
    public int CompareTo(CommentId other)
    {
        return string.Compare(Value, other.Value, StringComparison.Ordinal);
    }

    /// <summary>Compares this comment identifier with another object.</summary>
    public int CompareTo(object? obj)
    {
        return obj switch
        {
            null => 1,
            CommentId other => CompareTo(other),
            _ => throw new ArgumentException($"Object must be of type {nameof(CommentId)}", nameof(obj))
        };
    }

    /// <summary>
    ///     Implicitly converts a <see cref="CommentId" /> to its string representation.
    /// </summary>
    /// <param name="id">The <see cref="CommentId" /> instance.</param>
    public static implicit operator string(CommentId id)
    {
        return id.Value;
    }

    /// <summary>
    ///     Explicitly converts a string to a <see cref="CommentId" />.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    public static explicit operator CommentId(string value)
    {
        return Parse(value);
    }
}